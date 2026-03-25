using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GameMover.Models;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace GameMover.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }

    public class GameMoverViewModel : INotifyPropertyChanged
    {
        private readonly IPlayniteAPI _playniteApi;
        private readonly ILogger _logger;
        private DiskInfo _selectedDisk;
        private DiskInfo _destinationDisk;
        private bool _isMoving;
        private double _progress;
        private double _progressMax;
        private string _statusText;
        private bool _selectAll;
        private CancellationTokenSource _cancellationTokenSource;

        // Byte-level progress tracking
        private long _bytesCopied;
        private long _totalBytesToCopy;
        private DateTime _copyStartTime;
        private double _byteProgress;
        private double _byteProgressMax;
        private string _copySpeedDisplay;
        private string _etaDisplay;
        private string _currentFileDisplay;
        private DateTime _lastProgressUpdate;

        // Sorting
        private string _sortField = "Name";
        private bool _sortAscending = true;

        public ObservableCollection<DiskInfo> Disks { get; set; }
        public ObservableCollection<GameEntry> Games { get; set; }
        public ObservableCollection<DiskInfo> DestinationDisks { get; set; }

        public ICommand MoveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand SortByNameCommand { get; private set; }
        public ICommand SortBySizeCommand { get; private set; }

        public string NameSortIndicator
        {
            get
            {
                if (_sortField != "Name") return "  JOGO";
                return _sortAscending ? "▲ JOGO" : "▼ JOGO";
            }
        }

        public string SizeSortIndicator
        {
            get
            {
                if (_sortField != "Size") return "  TAMANHO";
                return _sortAscending ? "▲ TAMANHO" : "▼ TAMANHO";
            }
        }

        public DiskInfo SelectedDisk
        {
            get { return _selectedDisk; }
            set
            {
                if (_selectedDisk != value)
                {
                    _selectedDisk = value;
                    OnPropertyChanged("SelectedDisk");
                    LoadGamesForDisk();
                    UpdateDestinationDisks();
                }
            }
        }

        public DiskInfo DestinationDisk
        {
            get { return _destinationDisk; }
            set
            {
                if (_destinationDisk != value)
                {
                    _destinationDisk = value;
                    OnPropertyChanged("DestinationDisk");
                }
            }
        }

        public bool IsMoving
        {
            get { return _isMoving; }
            set
            {
                _isMoving = value;
                OnPropertyChanged("IsMoving");
                OnPropertyChanged("IsNotMoving");
            }
        }

        public bool IsNotMoving
        {
            get { return !_isMoving; }
        }

        public double Progress
        {
            get { return _progress; }
            set { _progress = value; OnPropertyChanged("Progress"); }
        }

        public double ProgressMax
        {
            get { return _progressMax; }
            set { _progressMax = value; OnPropertyChanged("ProgressMax"); }
        }

        public string StatusText
        {
            get { return _statusText; }
            set { _statusText = value; OnPropertyChanged("StatusText"); }
        }

        public double ByteProgress
        {
            get { return _byteProgress; }
            set { _byteProgress = value; OnPropertyChanged("ByteProgress"); }
        }

        public double ByteProgressMax
        {
            get { return _byteProgressMax; }
            set { _byteProgressMax = value; OnPropertyChanged("ByteProgressMax"); }
        }

        public string CopySpeedDisplay
        {
            get { return _copySpeedDisplay ?? ""; }
            set { _copySpeedDisplay = value; OnPropertyChanged("CopySpeedDisplay"); }
        }

        public string EtaDisplay
        {
            get { return _etaDisplay ?? ""; }
            set { _etaDisplay = value; OnPropertyChanged("EtaDisplay"); }
        }

        public string CurrentFileDisplay
        {
            get { return _currentFileDisplay ?? ""; }
            set { _currentFileDisplay = value; OnPropertyChanged("CurrentFileDisplay"); }
        }

        public string OverallProgressDisplay
        {
            get
            {
                if (!_isMoving || _totalBytesToCopy == 0) return "";
                return string.Format("{0} / {1}",
                    DiskInfo.FormatSize(_bytesCopied), DiskInfo.FormatSize(_totalBytesToCopy));
            }
        }

        public bool SelectAll
        {
            get { return _selectAll; }
            set
            {
                _selectAll = value;
                OnPropertyChanged("SelectAll");
                if (Games != null)
                {
                    foreach (var game in Games)
                        game.IsSelected = value;
                }
            }
        }

        public int SelectedCount
        {
            get { return Games != null ? Games.Count(g => g.IsSelected) : 0; }
        }

        public string SelectedSizeDisplay
        {
            get
            {
                if (Games == null) return "";
                long total = Games.Where(g => g.IsSelected).Sum(g => g.Size);
                if (total == 0) return "";
                return string.Format("{0} selecionado(s) — {1}",
                    Games.Count(g => g.IsSelected), DiskInfo.FormatSize(total));
            }
        }

        public GameMoverViewModel(IPlayniteAPI playniteApi)
        {
            _playniteApi = playniteApi;
            _logger = LogManager.GetLogger();
            Disks = new ObservableCollection<DiskInfo>();
            Games = new ObservableCollection<GameEntry>();
            DestinationDisks = new ObservableCollection<DiskInfo>();

            MoveCommand = new RelayCommand(async () => await MoveSelectedGames(), () => !IsMoving);
            CancelCommand = new RelayCommand(CancelMove, () => IsMoving);
            RefreshCommand = new RelayCommand(Refresh, () => !IsMoving);
            SortByNameCommand = new RelayCommand(() => ToggleSort("Name"));
            SortBySizeCommand = new RelayCommand(() => ToggleSort("Size"));

            StatusText = "Selecione um disco para ver os jogos instalados.";
            LoadDisks();
        }

        public void LoadDisks()
        {
            Disks.Clear();
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady && (drive.DriveType == DriveType.Fixed || drive.DriveType == DriveType.Removable))
                    {
                        Disks.Add(new DiskInfo
                        {
                            DriveLetter = drive.Name.Substring(0, 2),
                            VolumeLabel = drive.VolumeLabel,
                            TotalSize = drive.TotalSize,
                            FreeSpace = drive.AvailableFreeSpace
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Erro ao listar discos");
            }

            if (Disks.Count > 0)
                SelectedDisk = Disks[0];
        }

        private void UpdateDestinationDisks()
        {
            DestinationDisks.Clear();
            foreach (var disk in Disks)
            {
                if (_selectedDisk == null || disk.DriveLetter != _selectedDisk.DriveLetter)
                    DestinationDisks.Add(disk);
            }

            if (DestinationDisks.Count > 0)
                DestinationDisk = DestinationDisks[0];
        }

        private void LoadGamesForDisk()
        {
            Games.Clear();
            _selectAll = false;
            OnPropertyChanged("SelectAll");

            if (_selectedDisk == null) return;

            try
            {
                var allGames = _playniteApi.Database.Games;
                string driveLetter = _selectedDisk.DriveLetter.ToUpper();
                var gamesList = new List<GameEntry>();

                foreach (var game in allGames)
                {
                    if (!game.IsInstalled) continue;
                    if (string.IsNullOrEmpty(game.InstallDirectory)) continue;

                    string installDir = game.InstallDirectory;
                    if (!installDir.ToUpper().StartsWith(driveLetter)) continue;
                    if (!Directory.Exists(installDir)) continue;

                    long size = 0;
                    try
                    {
                        size = GetDirectorySize(new DirectoryInfo(installDir));
                    }
                    catch { }

                    string source = "";
                    if (game.PluginId != Guid.Empty)
                    {
                        try
                        {
                            var plugin = _playniteApi.Addons.Plugins
                                .FirstOrDefault(p => p.Id == game.PluginId);
                            if (plugin != null)
                            {
                                source = plugin.GetType().Name
                                    .Replace("Plugin", "")
                                    .Replace("Library", "");
                            }
                        }
                        catch { }
                    }

                    var entry = new GameEntry
                    {
                        GameId = game.Id,
                        Name = game.Name,
                        InstallDirectory = installDir,
                        Size = size,
                        SourcePlugin = source
                    };

                    entry.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == "IsSelected")
                        {
                            OnPropertyChanged("SelectedCount");
                            OnPropertyChanged("SelectedSizeDisplay");
                        }
                    };

                    gamesList.Add(entry);
                }

                foreach (var g in gamesList.OrderBy(g => g.Name))
                    Games.Add(g);

                OnPropertyChanged("Games");
                ApplySort();

                StatusText = string.Format("{0} jogo(s) encontrado(s) em {1}",
                    Games.Count, _selectedDisk.DriveLetter);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Erro ao listar jogos");
                StatusText = "Erro ao listar jogos: " + ex.Message;
            }
        }

        private async Task MoveSelectedGames()
        {
            var selected = Games.Where(g => g.IsSelected).ToList();
            if (selected.Count == 0)
            {
                _playniteApi.Dialogs.ShowMessage(
                    "Nenhum jogo selecionado!", "Game Mover",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_destinationDisk == null)
            {
                _playniteApi.Dialogs.ShowMessage(
                    "Selecione um disco de destino!", "Game Mover",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            long totalSize = selected.Sum(g => g.Size);
            if (totalSize > _destinationDisk.FreeSpace)
            {
                _playniteApi.Dialogs.ShowMessage(
                    string.Format("Espaço insuficiente no destino!\nNecessário: {0}\nDisponível: {1}",
                        DiskInfo.FormatSize(totalSize), DiskInfo.FormatSize(_destinationDisk.FreeSpace)),
                    "Game Mover", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var result = _playniteApi.Dialogs.ShowMessage(
                string.Format("Mover {0} jogo(s) ({1}) para {2}?",
                    selected.Count, DiskInfo.FormatSize(totalSize), _destinationDisk.DriveLetter),
                "Game Mover — Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsMoving = true;
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            int completed = 0;
            int failed = 0;
            ProgressMax = selected.Count;
            Progress = 0;

            // Byte-level progress setup
            _totalBytesToCopy = totalSize;
            _bytesCopied = 0;
            _copyStartTime = DateTime.Now;
            _lastProgressUpdate = DateTime.MinValue;
            ByteProgressMax = totalSize;
            ByteProgress = 0;
            CopySpeedDisplay = "";
            EtaDisplay = "";
            CurrentFileDisplay = "";

            await Task.Run(() =>
            {
                foreach (var game in selected)
                {
                    if (token.IsCancellationRequested) break;

                    try
                    {
                        UpdateStatus(string.Format("Movendo: {0} ({1}/{2})",
                            game.Name, completed + 1, selected.Count));

                        string sourcePath = game.InstallDirectory;
                        string folderName = new DirectoryInfo(sourcePath).Name;
                        string destPath = Path.Combine(_destinationDisk.DriveLetter + "\\", folderName);

                        // Avoid collision
                        if (Directory.Exists(destPath))
                        {
                            destPath = destPath + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                        }

                        // Copy recursively with byte tracking
                        CopyDirectory(sourcePath, destPath, token, game.Name);

                        if (token.IsCancellationRequested) break;

                        // Update Playnite database
                        _playniteApi.MainView.UIDispatcher.Invoke((Action)(() =>
                        {
                            var dbGame = _playniteApi.Database.Games.Get(game.GameId);
                            if (dbGame != null)
                            {
                                dbGame.InstallDirectory = destPath;
                                _playniteApi.Database.Games.Update(dbGame);

                                // Also update play actions that reference the old path
                                if (dbGame.GameActions != null)
                                {
                                    foreach (var action in dbGame.GameActions)
                                    {
                                        if (!string.IsNullOrEmpty(action.Path) &&
                                            action.Path.StartsWith(sourcePath, StringComparison.OrdinalIgnoreCase))
                                        {
                                            action.Path = action.Path.Replace(sourcePath, destPath);
                                        }
                                        if (!string.IsNullOrEmpty(action.WorkingDir) &&
                                            action.WorkingDir.StartsWith(sourcePath, StringComparison.OrdinalIgnoreCase))
                                        {
                                            action.WorkingDir = action.WorkingDir.Replace(sourcePath, destPath);
                                        }
                                    }
                                    _playniteApi.Database.Games.Update(dbGame);
                                }
                            }
                        }));

                        // Delete original
                        CurrentFileDisplay = string.Format("Apagando original: {0}", game.Name);
                        Directory.Delete(sourcePath, true);

                        completed++;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Erro ao mover " + game.Name);
                        failed++;
                        _playniteApi.MainView.UIDispatcher.Invoke((Action)(() =>
                        {
                            _playniteApi.Dialogs.ShowMessage(
                                string.Format("Erro ao mover '{0}':\n{1}", game.Name, ex.Message),
                                "Game Mover — Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        }));
                    }

                    _playniteApi.MainView.UIDispatcher.Invoke((Action)(() =>
                    {
                        Progress = completed + failed;
                    }));
                }
            });

            IsMoving = false;
            _cancellationTokenSource = null;
            CopySpeedDisplay = "";
            EtaDisplay = "";
            CurrentFileDisplay = "";

            string msg;
            if (token.IsCancellationRequested)
                msg = string.Format("Operação cancelada. {0} jogo(s) movido(s).", completed);
            else
                msg = string.Format("Concluído! {0} jogo(s) movido(s) com sucesso.", completed);

            if (failed > 0)
                msg += string.Format(" {0} erro(s).", failed);

            StatusText = msg;
            _playniteApi.Dialogs.ShowMessage(msg, "Game Mover",
                MessageBoxButton.OK, MessageBoxImage.Information);

            Refresh();
        }

        private void CancelMove()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                StatusText = "Cancelando...";
            }
        }

        public void Refresh()
        {
            LoadDisks();
        }

        private void ToggleSort(string field)
        {
            if (_sortField == field)
                _sortAscending = !_sortAscending;
            else
            {
                _sortField = field;
                _sortAscending = true;
            }

            OnPropertyChanged("NameSortIndicator");
            OnPropertyChanged("SizeSortIndicator");
            ApplySort();
        }

        private void ApplySort()
        {
            if (Games == null || Games.Count == 0) return;

            List<GameEntry> sorted;
            if (_sortField == "Size")
                sorted = _sortAscending
                    ? Games.OrderBy(g => g.Size).ToList()
                    : Games.OrderByDescending(g => g.Size).ToList();
            else
                sorted = _sortAscending
                    ? Games.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase).ToList()
                    : Games.OrderByDescending(g => g.Name, StringComparer.OrdinalIgnoreCase).ToList();

            Games.Clear();
            foreach (var g in sorted)
                Games.Add(g);
        }

        private void UpdateStatus(string text)
        {
            _playniteApi.MainView.UIDispatcher.Invoke((Action)(() =>
            {
                StatusText = text;
            }));
        }

        private void CopyDirectory(string sourceDir, string destDir, CancellationToken token, string gameName)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException();

                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);

                var fileInfo = new FileInfo(file);
                long fileSize = fileInfo.Length;

                // Copy with buffered read for progress tracking
                using (var sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024))
                using (var destStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024))
                {
                    byte[] buffer = new byte[1024 * 1024]; // 1MB buffer
                    int bytesRead;
                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (token.IsCancellationRequested)
                            throw new OperationCanceledException();

                        destStream.Write(buffer, 0, bytesRead);
                        _bytesCopied += bytesRead;

                        // Throttle UI updates to every 250ms
                        var now = DateTime.Now;
                        if ((now - _lastProgressUpdate).TotalMilliseconds >= 250)
                        {
                            _lastProgressUpdate = now;
                            UpdateByteProgress(gameName, fileName);
                        }
                    }
                }

                // Preserve file attributes
                File.SetAttributes(destFile, fileInfo.Attributes);
            }

            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException();

                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir, token, gameName);
            }
        }

        private void UpdateByteProgress(string gameName, string fileName)
        {
            double elapsed = (DateTime.Now - _copyStartTime).TotalSeconds;
            double speedBytesPerSec = elapsed > 0.5 ? _bytesCopied / elapsed : 0;
            double speedMBps = speedBytesPerSec / (1024.0 * 1024.0);

            string eta = "";
            if (speedBytesPerSec > 0)
            {
                long remaining = _totalBytesToCopy - _bytesCopied;
                double secondsLeft = remaining / speedBytesPerSec;

                if (secondsLeft < 60)
                    eta = string.Format("{0:F0}s restante(s)", secondsLeft);
                else if (secondsLeft < 3600)
                    eta = string.Format("{0:F0}m {1:F0}s restante(s)", Math.Floor(secondsLeft / 60), secondsLeft % 60);
                else
                    eta = string.Format("{0:F0}h {1:F0}m restante(s)", Math.Floor(secondsLeft / 3600), Math.Floor((secondsLeft % 3600) / 60));
            }

            _playniteApi.MainView.UIDispatcher.Invoke((Action)(() =>
            {
                ByteProgress = _bytesCopied;
                CopySpeedDisplay = speedMBps > 0 ? string.Format("{0:F1} MB/s", speedMBps) : "";
                EtaDisplay = eta;
                CurrentFileDisplay = string.Format("{0} — {1}", gameName, fileName);
                OnPropertyChanged("OverallProgressDisplay");
            }));
        }

        private static long GetDirectorySize(DirectoryInfo dirInfo)
        {
            long size = 0;
            try
            {
                foreach (var file in dirInfo.GetFiles())
                    size += file.Length;
                foreach (var subDir in dirInfo.GetDirectories())
                    size += GetDirectorySize(subDir);
            }
            catch { }
            return size;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                if (_playniteApi != null && _playniteApi.MainView != null &&
                    _playniteApi.MainView.UIDispatcher != null &&
                    !_playniteApi.MainView.UIDispatcher.CheckAccess())
                {
                    _playniteApi.MainView.UIDispatcher.Invoke((Action)(() =>
                    {
                        handler(this, new PropertyChangedEventArgs(propertyName));
                    }));
                }
                else
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }
    }
}
