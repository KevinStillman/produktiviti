using System.Windows;
using produKtiviti.Services;

namespace produKtiviti
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            SettingsService.Instance.Load();
            ThemeManager.Instance.SetTheme(SettingsService.Instance.Current.DarkMode);
        }
    }
}
