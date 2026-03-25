using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using GameMover.Models;
using GameMover.ViewModels;
using Playnite.SDK;

namespace GameMover.Views
{
    public class GameMoverView : UserControl
    {
        private GameMoverViewModel _viewModel;

        // Color palette
        private static readonly Color AccentColor = Color.FromRgb(0x6C, 0x5C, 0xE7);
        private static readonly Color AccentHoverColor = Color.FromRgb(0xA2, 0x9B, 0xFE);
        private static readonly Color PanelBgColor = Color.FromRgb(0x2D, 0x2D, 0x44);
        private static readonly Color CardBgColor = Color.FromRgb(0x38, 0x38, 0x54);
        private static readonly Color CardHoverColor = Color.FromRgb(0x44, 0x44, 0x6A);
        private static readonly Color TextPrimaryColor = Color.FromRgb(0xEC, 0xF0, 0xF1);
        private static readonly Color TextSecondaryColor = Color.FromRgb(0xB2, 0xBE, 0xC3);
        private static readonly Color DangerColor = Color.FromRgb(0xE7, 0x4C, 0x3C);
        private static readonly Color WarningColor = Color.FromRgb(0xFD, 0xCB, 0x6E);
        private static readonly Color BorderColor = Color.FromRgb(0x55, 0x55, 0x77);

        private static SolidColorBrush Brush(Color c) => new SolidColorBrush(c);

        public GameMoverView(IPlayniteAPI playniteApi)
        {
            _viewModel = new GameMoverViewModel(playniteApi);
            DataContext = _viewModel;
            Background = Brushes.Transparent;
            BuildUI();
        }

        private void BuildUI()
        {
            var mainBorder = new Border
            {
                Background = Brush(PanelBgColor),
                Padding = new Thickness(24)
            };

            var dock = new DockPanel();
            mainBorder.Child = dock;

            // === HEADER ===
            var header = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
            DockPanel.SetDock(header, Dock.Top);
            header.Children.Add(new TextBlock
            {
                Text = "\u21C4  Game Mover",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brush(TextPrimaryColor),
                Margin = new Thickness(0, 0, 0, 4)
            });
            header.Children.Add(new TextBlock
            {
                Text = "Mova jogos entre discos de forma f\u00E1cil",
                FontSize = 13,
                Foreground = Brush(TextSecondaryColor)
            });
            dock.Children.Add(header);

            // === DETAILED PROGRESS PANEL (Bottom) ===
            var statusPanel = new StackPanel { Margin = new Thickness(0, 16, 0, 0) };
            DockPanel.SetDock(statusPanel, Dock.Bottom);

            // Byte-level progress bar (the main one users care about)
            var byteProgressBar = new ProgressBar
            {
                Height = 8,
                Margin = new Thickness(0, 0, 0, 6),
                Foreground = Brush(AccentColor),
                Background = Brush(CardBgColor),
                BorderThickness = new Thickness(0)
            };
            byteProgressBar.SetBinding(ProgressBar.ValueProperty, new Binding("ByteProgress") { Mode = BindingMode.OneWay });
            byteProgressBar.SetBinding(ProgressBar.MaximumProperty, new Binding("ByteProgressMax") { Mode = BindingMode.OneWay });
            byteProgressBar.SetBinding(VisibilityProperty, new Binding("IsMoving") { Converter = new BooleanToVisibilityConverter() });
            statusPanel.Children.Add(byteProgressBar);

            // Progress info row: speed | transferred | ETA
            var progressInfoGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            progressInfoGrid.SetBinding(VisibilityProperty, new Binding("IsMoving") { Converter = new BooleanToVisibilityConverter() });
            progressInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            progressInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            progressInfoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Speed display (left)
            var speedText = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brush(AccentHoverColor)
            };
            speedText.SetBinding(TextBlock.TextProperty, new Binding("CopySpeedDisplay"));
            Grid.SetColumn(speedText, 0);
            progressInfoGrid.Children.Add(speedText);

            // Transferred / Total (center)
            var transferredText = new TextBlock
            {
                FontSize = 12,
                Foreground = Brush(TextSecondaryColor),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            transferredText.SetBinding(TextBlock.TextProperty, new Binding("OverallProgressDisplay"));
            Grid.SetColumn(transferredText, 1);
            progressInfoGrid.Children.Add(transferredText);

            // ETA (right)
            var etaText = new TextBlock
            {
                FontSize = 12,
                Foreground = Brush(WarningColor)
            };
            etaText.SetBinding(TextBlock.TextProperty, new Binding("EtaDisplay"));
            Grid.SetColumn(etaText, 2);
            progressInfoGrid.Children.Add(etaText);

            statusPanel.Children.Add(progressInfoGrid);

            // Current file display
            var currentFileText = new TextBlock
            {
                FontSize = 11,
                Foreground = Brush(TextSecondaryColor),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 0, 4)
            };
            currentFileText.SetBinding(TextBlock.TextProperty, new Binding("CurrentFileDisplay"));
            currentFileText.SetBinding(VisibilityProperty, new Binding("IsMoving") { Converter = new BooleanToVisibilityConverter() });
            statusPanel.Children.Add(currentFileText);

            // General status text (always visible)
            var statusText = new TextBlock { FontSize = 12, Foreground = Brush(TextSecondaryColor) };
            statusText.SetBinding(TextBlock.TextProperty, new Binding("StatusText"));
            statusPanel.Children.Add(statusText);
            dock.Children.Add(statusPanel);

            // === ACTION PANEL (Bottom) ===
            var actionBorder = new Border
            {
                Background = Brush(CardBgColor),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 12, 0, 0)
            };
            DockPanel.SetDock(actionBorder, Dock.Bottom);

            var actionGrid = new Grid();
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Label
            var moveLabel = new TextBlock
            {
                Text = "Mover para:",
                FontSize = 14,
                Foreground = Brush(TextPrimaryColor),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(moveLabel, 0);
            actionGrid.Children.Add(moveLabel);

            // Destination combo
            var destCombo = new ComboBox
            {
                FontSize = 14,
                Padding = new Thickness(10, 8, 10, 8),
                DisplayMemberPath = "DisplayText"
            };
            destCombo.SetBinding(ComboBox.ItemsSourceProperty, new Binding("DestinationDisks"));
            destCombo.SetBinding(ComboBox.SelectedItemProperty, new Binding("DestinationDisk"));
            destCombo.SetBinding(IsEnabledProperty, new Binding("IsNotMoving"));
            Grid.SetColumn(destCombo, 1);
            actionGrid.Children.Add(destCombo);

            // Selected size display
            var sizeDisplay = new TextBlock
            {
                FontSize = 12,
                Foreground = Brush(WarningColor),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 14, 0)
            };
            sizeDisplay.SetBinding(TextBlock.TextProperty, new Binding("SelectedSizeDisplay"));
            Grid.SetColumn(sizeDisplay, 2);
            actionGrid.Children.Add(sizeDisplay);

            // Move button
            var moveBtn = CreateStyledButton("\u2726  Mover Selecionados", AccentColor, AccentHoverColor, 14, true);
            moveBtn.Margin = new Thickness(0, 0, 8, 0);
            moveBtn.SetBinding(ButtonBase.CommandProperty, new Binding("MoveCommand"));
            moveBtn.SetBinding(IsEnabledProperty, new Binding("IsNotMoving"));
            Grid.SetColumn(moveBtn, 3);
            actionGrid.Children.Add(moveBtn);

            // Cancel button
            var cancelBtn = CreateStyledButton("Cancelar", DangerColor, Color.FromRgb(0xFF, 0x6B, 0x6B), 13, false);
            cancelBtn.SetBinding(ButtonBase.CommandProperty, new Binding("CancelCommand"));
            cancelBtn.SetBinding(VisibilityProperty, new Binding("IsMoving") { Converter = new BooleanToVisibilityConverter() });
            Grid.SetColumn(cancelBtn, 4);
            actionGrid.Children.Add(cancelBtn);

            actionBorder.Child = actionGrid;
            dock.Children.Add(actionBorder);

            // === MAIN CONTENT ===
            var mainGrid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // --- Disk selector ---
            var diskPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            Grid.SetRow(diskPanel, 0);

            var diskHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            diskHeader.Children.Add(new TextBlock
            {
                Text = "DISCOS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brush(TextSecondaryColor),
                VerticalAlignment = VerticalAlignment.Center
            });
            var refreshBtn = CreateStyledButton("\u21BB Atualizar", CardBgColor, CardHoverColor, 11, false);
            refreshBtn.Padding = new Thickness(12, 4, 12, 4);
            refreshBtn.HorizontalAlignment = HorizontalAlignment.Right;
            refreshBtn.SetBinding(ButtonBase.CommandProperty, new Binding("RefreshCommand"));
            DockPanel.SetDock(refreshBtn, Dock.Right);
            diskHeader.Children.Add(refreshBtn);
            diskPanel.Children.Add(diskHeader);

            // Disk items control
            var diskItemsControl = new ItemsControl();
            diskItemsControl.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Disks"));
            diskItemsControl.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(WrapPanel)));

            var diskTemplate = new DataTemplate();
            var diskBtnFactory = new FrameworkElementFactory(typeof(Button));
            diskBtnFactory.SetValue(Button.CursorProperty, Cursors.Hand);
            diskBtnFactory.SetValue(Button.MarginProperty, new Thickness(0, 0, 8, 8));
            diskBtnFactory.SetValue(Button.PaddingProperty, new Thickness(14, 10, 14, 10));
            diskBtnFactory.SetValue(Button.BackgroundProperty, Brush(CardBgColor));
            diskBtnFactory.SetValue(Button.BorderBrushProperty, Brush(BorderColor));
            diskBtnFactory.SetValue(Button.BorderThicknessProperty, new Thickness(1));
            diskBtnFactory.SetValue(Button.ForegroundProperty, Brush(TextPrimaryColor));
            diskBtnFactory.SetValue(Button.MinWidthProperty, 160.0);

            // Disk button content
            var diskContentFactory = new FrameworkElementFactory(typeof(StackPanel));

            var driveTextFactory = new FrameworkElementFactory(typeof(TextBlock));
            driveTextFactory.SetValue(TextBlock.FontSizeProperty, 15.0);
            driveTextFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            driveTextFactory.SetValue(TextBlock.ForegroundProperty, Brush(TextPrimaryColor));
            driveTextFactory.SetBinding(TextBlock.TextProperty, new Binding("DisplayText"));
            diskContentFactory.AppendChild(driveTextFactory);

            var freeTextFactory = new FrameworkElementFactory(typeof(TextBlock));
            freeTextFactory.SetValue(TextBlock.FontSizeProperty, 11.0);
            freeTextFactory.SetValue(TextBlock.ForegroundProperty, Brush(TextSecondaryColor));
            freeTextFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 4, 0, 0));
            freeTextFactory.SetBinding(TextBlock.TextProperty, new Binding("ShortDisplay"));
            diskContentFactory.AppendChild(freeTextFactory);

            // Usage bar
            var usageBarFactory = new FrameworkElementFactory(typeof(ProgressBar));
            usageBarFactory.SetValue(ProgressBar.HeightProperty, 4.0);
            usageBarFactory.SetValue(ProgressBar.MarginProperty, new Thickness(0, 6, 0, 0));
            usageBarFactory.SetValue(ProgressBar.MaximumProperty, 100.0);
            usageBarFactory.SetValue(ProgressBar.ForegroundProperty, Brush(AccentColor));
            usageBarFactory.SetValue(ProgressBar.BackgroundProperty, Brush(PanelBgColor));
            usageBarFactory.SetValue(ProgressBar.BorderThicknessProperty, new Thickness(0));
            usageBarFactory.SetBinding(ProgressBar.ValueProperty, new Binding("UsagePercent") { Mode = BindingMode.OneWay });
            diskContentFactory.AppendChild(usageBarFactory);

            diskBtnFactory.AppendChild(diskContentFactory);
            diskBtnFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(DiskButton_Click));

            diskTemplate.VisualTree = diskBtnFactory;
            diskItemsControl.ItemTemplate = diskTemplate;
            diskPanel.Children.Add(diskItemsControl);

            mainGrid.Children.Add(diskPanel);

            // --- Games list ---
            var gamesListBorder = new Border
            {
                Background = Brush(CardBgColor),
                CornerRadius = new CornerRadius(10)
            };
            Grid.SetRow(gamesListBorder, 1);

            var gamesListDock = new DockPanel();

            // List header
            var listHeaderBorder = new Border
            {
                Padding = new Thickness(16, 10, 16, 10),
                BorderBrush = Brush(BorderColor),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            DockPanel.SetDock(listHeaderBorder, Dock.Top);

            var listHeaderGrid = new Grid();
            listHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            listHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            listHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            listHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });

            var selectAllCb = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            selectAllCb.SetBinding(CheckBox.IsCheckedProperty, new Binding("SelectAll"));
            selectAllCb.SetBinding(IsEnabledProperty, new Binding("IsNotMoving"));
            Grid.SetColumn(selectAllCb, 0);
            listHeaderGrid.Children.Add(selectAllCb);

            AddSortableHeader(listHeaderGrid, "NameSortIndicator", "SortByNameCommand", 1, TextAlignment.Left);
            AddColumnHeader(listHeaderGrid, "ORIGEM", 2);
            AddSortableHeader(listHeaderGrid, "SizeSortIndicator", "SortBySizeCommand", 3, TextAlignment.Right);

            listHeaderBorder.Child = listHeaderGrid;
            gamesListDock.Children.Add(listHeaderBorder);

            // Game items ListView
            var gamesListView = new ListView
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            ScrollViewer.SetHorizontalScrollBarVisibility(gamesListView, ScrollBarVisibility.Disabled);
            gamesListView.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Games"));

            // Item container style
            var itemStyle = new Style(typeof(ListViewItem));
            itemStyle.Setters.Add(new Setter(ListViewItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            itemStyle.Setters.Add(new Setter(ListViewItem.PaddingProperty, new Thickness(0)));
            itemStyle.Setters.Add(new Setter(ListViewItem.MarginProperty, new Thickness(0)));
            itemStyle.Setters.Add(new Setter(ListViewItem.BackgroundProperty, Brushes.Transparent));
            itemStyle.Setters.Add(new Setter(ListViewItem.BorderThicknessProperty, new Thickness(0)));

            // Hover trigger
            var hoverTrigger = new Trigger { Property = ListViewItem.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(ListViewItem.BackgroundProperty, new SolidColorBrush(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF))));
            itemStyle.Triggers.Add(hoverTrigger);
            gamesListView.ItemContainerStyle = itemStyle;

            // Item template
            var gameTemplate = new DataTemplate();
            var itemBorderFactory = new FrameworkElementFactory(typeof(Border));
            itemBorderFactory.SetValue(Border.PaddingProperty, new Thickness(16, 8, 16, 8));
            itemBorderFactory.SetValue(Border.BorderBrushProperty, Brush(BorderColor));
            itemBorderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(0, 0, 0, 0.5));

            var itemGridFactory = new FrameworkElementFactory(typeof(Grid));
            var colDef1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            colDef1.SetValue(ColumnDefinition.WidthProperty, new GridLength(40));
            var colDef2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            colDef2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            var colDef3 = new FrameworkElementFactory(typeof(ColumnDefinition));
            colDef3.SetValue(ColumnDefinition.WidthProperty, new GridLength(120));
            var colDef4 = new FrameworkElementFactory(typeof(ColumnDefinition));
            colDef4.SetValue(ColumnDefinition.WidthProperty, new GridLength(140));
            itemGridFactory.AppendChild(colDef1);
            itemGridFactory.AppendChild(colDef2);
            itemGridFactory.AppendChild(colDef3);
            itemGridFactory.AppendChild(colDef4);

            // Checkbox
            var cbFactory = new FrameworkElementFactory(typeof(CheckBox));
            cbFactory.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            cbFactory.SetValue(CheckBox.CursorProperty, Cursors.Hand);
            cbFactory.SetBinding(CheckBox.IsCheckedProperty, new Binding("IsSelected"));
            cbFactory.SetValue(Grid.ColumnProperty, 0);
            itemGridFactory.AppendChild(cbFactory);

            // Name
            var nameFactory = new FrameworkElementFactory(typeof(TextBlock));
            nameFactory.SetValue(TextBlock.FontSizeProperty, 14.0);
            nameFactory.SetValue(TextBlock.ForegroundProperty, Brush(TextPrimaryColor));
            nameFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            nameFactory.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            nameFactory.SetBinding(TextBlock.TextProperty, new Binding("Name"));
            nameFactory.SetValue(Grid.ColumnProperty, 1);
            itemGridFactory.AppendChild(nameFactory);

            // Source
            var sourceFactory = new FrameworkElementFactory(typeof(TextBlock));
            sourceFactory.SetValue(TextBlock.FontSizeProperty, 12.0);
            sourceFactory.SetValue(TextBlock.ForegroundProperty, Brush(TextSecondaryColor));
            sourceFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            sourceFactory.SetBinding(TextBlock.TextProperty, new Binding("SourcePlugin"));
            sourceFactory.SetValue(Grid.ColumnProperty, 2);
            itemGridFactory.AppendChild(sourceFactory);

            // Size
            var sizeFactory = new FrameworkElementFactory(typeof(TextBlock));
            sizeFactory.SetValue(TextBlock.FontSizeProperty, 13.0);
            sizeFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            sizeFactory.SetValue(TextBlock.ForegroundProperty, Brush(AccentHoverColor));
            sizeFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            sizeFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
            sizeFactory.SetBinding(TextBlock.TextProperty, new Binding("SizeDisplay"));
            sizeFactory.SetValue(Grid.ColumnProperty, 3);
            itemGridFactory.AppendChild(sizeFactory);

            itemBorderFactory.AppendChild(itemGridFactory);
            gameTemplate.VisualTree = itemBorderFactory;
            gamesListView.ItemTemplate = gameTemplate;

            gamesListDock.Children.Add(gamesListView);
            gamesListBorder.Child = gamesListDock;
            mainGrid.Children.Add(gamesListBorder);

            dock.Children.Add(mainGrid);
            Content = mainBorder;
        }

        private void DiskButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null && btn.DataContext is DiskInfo disk)
            {
                _viewModel.SelectedDisk = disk;
            }
        }

        private void AddColumnHeader(Grid grid, string text, int column, TextAlignment align = TextAlignment.Left)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brush(TextSecondaryColor),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = align
            };
            Grid.SetColumn(tb, column);
            grid.Children.Add(tb);
        }

        private void AddSortableHeader(Grid grid, string indicatorProperty, string commandProperty, int column, TextAlignment align)
        {
            var btn = new Button
            {
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalContentAlignment = align == TextAlignment.Right ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            // Flat template (no chrome)
            var template = new ControlTemplate(typeof(Button));
            var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            cpFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty,
                align == TextAlignment.Right ? HorizontalAlignment.Right : HorizontalAlignment.Left);
            cpFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            template.VisualTree = cpFactory;
            btn.Template = template;

            var tb = new TextBlock
            {
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brush(TextSecondaryColor),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = align
            };
            tb.SetBinding(TextBlock.TextProperty, new Binding(indicatorProperty));
            btn.Content = tb;

            btn.SetBinding(ButtonBase.CommandProperty, new Binding(commandProperty));

            Grid.SetColumn(btn, column);
            grid.Children.Add(btn);
        }

        private Button CreateStyledButton(string text, Color bgColor, Color hoverColor, double fontSize, bool bold)
        {
            var btn = new Button
            {
                Content = text,
                FontSize = fontSize,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = Brushes.White,
                Background = Brush(bgColor),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(24, 10, 24, 10),
                Cursor = Cursors.Hand
            };

            // Simple rounded template
            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(24, 10, 24, 10));
            borderFactory.Name = "border";
            borderFactory.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent) });

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);

            template.VisualTree = borderFactory;

            // Hover trigger
            var hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, Brush(hoverColor), "border"));
            template.Triggers.Add(hoverTrigger);

            // Disabled trigger
            var disabledTrigger = new Trigger { Property = Button.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.5, "border"));
            template.Triggers.Add(disabledTrigger);

            btn.Template = template;
            return btn;
        }
    }
}
