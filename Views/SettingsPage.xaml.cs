using System.Windows;
using System.Windows.Controls;
using produKtiviti.Services;

namespace produKtiviti.Views
{
    public partial class SettingsPage : UserControl
    {
        private bool _suppressEvent = false;

        public SettingsPage()
        {
            InitializeComponent();
            _suppressEvent = true;
            DarkModeToggle.IsChecked = ThemeManager.Instance.IsDark;
            _suppressEvent = false;
        }

        private void DarkModeToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressEvent) return;
            bool dark = DarkModeToggle.IsChecked == true;
            ThemeManager.Instance.SetTheme(dark);
            SettingsService.Instance.Current.DarkMode = dark;
            SettingsService.Instance.Save();
        }
    }
}
