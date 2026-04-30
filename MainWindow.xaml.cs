using System.Windows;
using System.Windows.Controls;
using produKtiviti.Views;

namespace produKtiviti
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            NavigateTo("Features");
        }

        private void NavigateTo(string page)
        {
            NavFeatures.Style = (Style)Resources["NavButtonStyle"];
            NavSettings.Style = (Style)Resources["NavButtonStyle"];

            switch (page)
            {
                case "Features":
                    PageContent.Content = new FeaturesPage();
                    NavFeatures.Style = (Style)Resources["NavButtonActiveStyle"];
                    break;
                case "Settings":
                    PageContent.Content = new SettingsPage();
                    NavSettings.Style = (Style)Resources["NavButtonActiveStyle"];
                    break;
            }
        }

        private void NavFeatures_Click(object sender, RoutedEventArgs e) => NavigateTo("Features");
        private void NavSettings_Click(object sender, RoutedEventArgs e) => NavigateTo("Settings");
    }
}
