using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using produKtiviti.Models;

namespace produKtiviti.Views.Controls
{
    public partial class FeatureTile : UserControl
    {
        public event EventHandler? FavoriteChanged;
        public event EventHandler? ConfigureRequested;

        private FeatureItem? _item;

        public FeatureTile()
        {
            InitializeComponent();
        }

        public void SetFeature(FeatureItem item)
        {
            _item = item;
            FeatureName.Text = item.Name;

            if (!string.IsNullOrEmpty(item.IconPath))
            {
                try
                {
                    FeatureIcon.Source = new BitmapImage(new Uri(item.IconPath, UriKind.RelativeOrAbsolute));
                }
                catch { }
            }

            if (TileBorder.ToolTip is ToolTip tt && tt.Content is TextBlock tb)
            {
                tb.Text = string.IsNullOrWhiteSpace(item.Description)
                    ? item.Name
                    : item.Description;
            }

            UpdateState();
        }

        private void UpdateState()
        {
            if (_item == null) return;

            bool showEnabled = _item.IsEnabled && !_item.NeedsSetup;
            bool showSetup = _item.NeedsSetup;

            EnabledBadge.Visibility = showEnabled ? Visibility.Visible : Visibility.Collapsed;
            SetupRequiredBadge.Visibility = showSetup ? Visibility.Visible : Visibility.Collapsed;
            DisabledSpacer.Visibility = (!showEnabled && !showSetup) ? Visibility.Visible : Visibility.Collapsed;

            // Filled star when favorited, outline star when not
            FavoriteButton.Content = _item.IsFavorite ? "\u2605" : "\u2606";
            FavoriteButton.Foreground = _item.IsFavorite
                ? System.Windows.Media.Brushes.Gold
                : (System.Windows.Media.Brush)FindResource("TextSecondaryBrush");
        }

        private void Tile_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_item == null) return;
            _item.IsEnabled = !_item.IsEnabled;
            UpdateState();
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_item == null) return;
            e.Handled = true; // prevent tile toggle
            _item.IsFavorite = !_item.IsFavorite;
            UpdateState();
            FavoriteChanged?.Invoke(this, EventArgs.Empty);
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            TileContextMenu.PlacementTarget = MenuButton;
            TileContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            TileContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void Configure_Click(object sender, RoutedEventArgs e)
        {
            ConfigureRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
