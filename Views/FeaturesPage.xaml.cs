using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using produKtiviti.Models;
using produKtiviti.Services;
using produKtiviti.Views.Controls;

namespace produKtiviti.Views
{
    public partial class FeaturesPage : UserControl
    {
        private readonly SmartTabsService _smartTabsService = new();
        private readonly RoboMouseService _roboMouseService = new();

        private readonly FeatureItem _smartTabsItem;
        private readonly FeatureItem _roboMouseItem;
        private readonly List<FeatureItem> _features;

        public FeaturesPage()
        {
            InitializeComponent();

            var settings = SettingsService.Instance.Current;

            _smartTabsItem = new FeatureItem
            {
                Name = "Smart Tabs",
                Description = "Open windows to the monitor you click them on.",
                IconPath = "pack://application:,,,/Assets/Icons/smarttabs.png",
                IsEnabled = settings.SmartTabsEnabled,
                IsFavorite = settings.SmartTabsFavorite
            };

            _roboMouseItem = new FeatureItem
            {
                Name = "RoboMouse",
                Description = "Configurable Auto Clicker",
                IconPath = "pack://application:,,,/Assets/Icons/robomouse.png",
                IsEnabled = settings.RoboMouseEnabled,
                IsFavorite = settings.RoboMouseFavorite,
                NeedsSetup = settings.RoboMouseEnabled && !settings.RoboMouse.IsConfigured
            };

            _features = new List<FeatureItem> { _smartTabsItem, _roboMouseItem };

            // Apply saved state
            if (_smartTabsItem.IsEnabled)
                _smartTabsService.Enable();

            if (settings.RoboMouse.IsConfigured)
            {
                _roboMouseService.Configure(settings.RoboMouse);
                if (_roboMouseItem.IsEnabled)
                    _roboMouseService.StartHotkeyListener();
            }
            else if (_roboMouseItem.IsEnabled)
            {
                _roboMouseItem.NeedsSetup = true;
                _roboMouseItem.IsEnabled = false;
            }

            // Wire Smart Tabs toggle
            _smartTabsItem.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(FeatureItem.IsEnabled)) return;
                if (_smartTabsItem.IsEnabled) _smartTabsService.Enable();
                else _smartTabsService.Disable();
                SaveSettings();
            };

            // Wire RoboMouse toggle
            _roboMouseItem.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != nameof(FeatureItem.IsEnabled)) return;
                if (_roboMouseItem.IsEnabled)
                {
                    if (!SettingsService.Instance.Current.RoboMouse.IsConfigured)
                    {
                        _roboMouseItem.NeedsSetup = true;
                        _roboMouseItem.IsEnabled = false;
                        return;
                    }
                    _roboMouseService.StartHotkeyListener();
                }
                else
                {
                    _roboMouseService.StopHotkeyListener();
                }
                SaveSettings();
            };

            // Save favorites on change
            _smartTabsItem.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FeatureItem.IsFavorite)) SaveSettings();
            };
            _roboMouseItem.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FeatureItem.IsFavorite)) SaveSettings();
            };

            LoadTiles();
        }

        private void LoadTiles()
        {
            TilePanel.Children.Clear();
            FavoritesTilePanel.Children.Clear();

            foreach (var feature in _features)
            {
                var tile = CreateTile(feature);
                TilePanel.Children.Add(tile);
            }

            RefreshFavorites();
        }

        private FeatureTile CreateTile(FeatureItem feature)
        {
            var tile = new FeatureTile();
            tile.SetFeature(feature);
            tile.FavoriteChanged += (s, e) => RefreshFavorites();

            // Wire Configure menu item for RoboMouse
            if (feature == _roboMouseItem)
                tile.ConfigureRequested += (s, e) => OpenRoboMouseConfig();

            // Refresh tile when NeedsSetup changes
            feature.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FeatureItem.NeedsSetup) ||
                    e.PropertyName == nameof(FeatureItem.IsEnabled))
                    tile.SetFeature(feature);
            };

            return tile;
        }

        private void RefreshFavorites()
        {
            FavoritesTilePanel.Children.Clear();
            bool hasAny = false;

            foreach (var feature in _features)
            {
                if (!feature.IsFavorite) continue;
                hasAny = true;
                var favTile = CreateTile(feature);
                FavoritesTilePanel.Children.Add(favTile);
            }

            NoFavoritesHint.Visibility = hasAny ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OpenRoboMouseConfig()
        {
            var existing = SettingsService.Instance.Current.RoboMouse;
            var win = new RoboMouseConfigWindow(existing)
            {
                Owner = Window.GetWindow(this)
            };

            if (win.ShowDialog() == true && win.Result != null)
            {
                SettingsService.Instance.Current.RoboMouse = win.Result;
                _roboMouseService.Configure(win.Result);
                _roboMouseItem.NeedsSetup = false;
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            var s = SettingsService.Instance.Current;
            s.SmartTabsEnabled = _smartTabsItem.IsEnabled;
            s.SmartTabsFavorite = _smartTabsItem.IsFavorite;
            s.RoboMouseEnabled = _roboMouseItem.IsEnabled;
            s.RoboMouseFavorite = _roboMouseItem.IsFavorite;
            SettingsService.Instance.Save();
        }
    }
}
