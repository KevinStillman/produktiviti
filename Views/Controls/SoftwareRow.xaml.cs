using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using produKtiviti.Models;

namespace produKtiviti.Views.Controls
{
    public partial class SoftwareRow : UserControl
    {
        private static readonly SolidColorBrush InstallingBrush = new(Color.FromRgb(0x5B, 0x9B, 0xD5));
        private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(0x4C, 0xAF, 0x50));
        private static readonly SolidColorBrush FailedBrush = new(Color.FromRgb(0xFF, 0x6B, 0x6B));

        private SoftwareItem? _item;

        public SoftwareRow()
        {
            InitializeComponent();
        }

        public void SetItem(SoftwareItem item)
        {
            _item = item;
            NameText.Text = item.Name;
            DescriptionText.Text = item.Description;
            SelectBox.IsChecked = item.IsSelected;

            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SoftwareItem.Status))
                    UpdateStatus();
                else if (e.PropertyName == nameof(SoftwareItem.IsSelected))
                    SelectBox.IsChecked = item.IsSelected;
            };

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (_item == null) return;

            switch (_item.Status)
            {
                case InstallStatus.Installing:
                    StatusText.Text = "Installing…";
                    StatusText.Foreground = InstallingBrush;
                    StatusText.Visibility = Visibility.Visible;
                    RowProgress.Visibility = Visibility.Visible;
                    break;
                case InstallStatus.Success:
                    StatusText.Text = "Installed";
                    StatusText.Foreground = SuccessBrush;
                    StatusText.Visibility = Visibility.Visible;
                    RowProgress.Visibility = Visibility.Collapsed;
                    break;
                case InstallStatus.Failed:
                    StatusText.Text = "Failed";
                    StatusText.Foreground = FailedBrush;
                    StatusText.Visibility = Visibility.Visible;
                    RowProgress.Visibility = Visibility.Collapsed;
                    break;
                default:
                    StatusText.Visibility = Visibility.Collapsed;
                    RowProgress.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        public void SetInteractive(bool enabled)
        {
            SelectBox.IsEnabled = enabled;
        }

        private void SelectBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_item != null) _item.IsSelected = true;
        }

        private void SelectBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_item != null) _item.IsSelected = false;
        }
    }
}
