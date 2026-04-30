using System;
using System.Windows;

namespace produKtiviti.Services
{
    public class ThemeManager
    {
        private static ThemeManager? _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        private bool _isDark = true;
        public bool IsDark => _isDark;

        public event Action? ThemeChanged;

        private ThemeManager() { }

        public void SetTheme(bool dark)
        {
            _isDark = dark;
            var dict = new ResourceDictionary
            {
                Source = new Uri(dark
                    ? "pack://application:,,,/Themes/Dark.xaml"
                    : "pack://application:,,,/Themes/Light.xaml")
            };

            var merged = Application.Current.Resources.MergedDictionaries;
            // Remove existing theme dict
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                var src = merged[i].Source?.ToString() ?? "";
                if (src.Contains("/Themes/"))
                {
                    merged.RemoveAt(i);
                    break;
                }
            }
            merged.Add(dict);
            ThemeChanged?.Invoke();
        }

        public void Toggle() => SetTheme(!_isDark);
    }
}
