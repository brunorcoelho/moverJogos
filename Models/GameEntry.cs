using System;
using System.ComponentModel;

namespace GameMover.Models
{
    public class GameEntry : INotifyPropertyChanged
    {
        private bool _isSelected;

        public Guid GameId { get; set; }
        public string Name { get; set; }
        public string InstallDirectory { get; set; }
        public long Size { get; set; }
        public string SourcePlugin { get; set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        public string SizeDisplay
        {
            get { return DiskInfo.FormatSize(Size); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
