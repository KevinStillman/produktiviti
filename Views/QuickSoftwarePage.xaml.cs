using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using produKtiviti.Models;
using produKtiviti.Services;
using produKtiviti.Views.Controls;

namespace produKtiviti.Views
{
    public partial class QuickSoftwarePage : UserControl
    {
        private readonly List<SoftwareItem> _software = SoftwareCatalog.GetAll();
        private readonly SoftwareInstallerService _installer = new();
        private bool _isInstalling;
        private CancellationTokenSource? _cancelSource;

        public QuickSoftwarePage()
        {
            InitializeComponent();
            LoadRows();
            _ = CheckWingetAsync();
        }

        private async Task CheckWingetAsync()
        {
            InstallButton.IsEnabled = false;
            InstallButton.Content = "Checking…";
            WingetWarning.Visibility = Visibility.Collapsed;
            WingetActions.Visibility = Visibility.Collapsed;

            bool available = await SoftwareInstallerService.IsWingetAvailableAsync();

            InstallButton.Content = "Download and Install";
            InstallButton.IsEnabled = available;
            WingetWarning.Visibility = available ? Visibility.Collapsed : Visibility.Visible;
            WingetActions.Visibility = available ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LoadRows()
        {
            SoftwareList.Children.Clear();
            foreach (var item in _software)
            {
                var row = new SoftwareRow();
                row.SetItem(item);
                SoftwareList.Children.Add(row);
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _software) item.IsSelected = true;
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _software) item.IsSelected = false;
        }

        private void OpenStore_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-windows-store://pdp/?productid=9NBLGGH4NNS1")
                {
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private async void CheckAgain_Click(object sender, RoutedEventArgs e) => await CheckWingetAsync();

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling) return;

            var selected = _software.Where(s => s.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(Window.GetWindow(this), "Select at least one app to install.", "Quick Software",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _isInstalling = true;
            _cancelSource = new CancellationTokenSource();
            SetControlsEnabled(false);
            CancelButton.IsEnabled = true;
            CancelButton.Visibility = Visibility.Visible;
            LogBox.Clear();
            LogPanel.Visibility = Visibility.Visible;
            ProgressText.Visibility = Visibility.Visible;
            OverallProgress.Visibility = Visibility.Visible;
            OverallProgress.Maximum = selected.Count;
            OverallProgress.Value = 0;

            int done = 0, failed = 0;
            for (int i = 0; i < selected.Count; i++)
            {
                if (_cancelSource.IsCancellationRequested)
                {
                    AppendLog("Cancelled — remaining apps were not started.");
                    break;
                }

                var item = selected[i];
                ProgressText.Text = $"Installing {i + 1} of {selected.Count}: {item.Name}";
                bool ok = await _installer.InstallAsync(item, AppendLog, _cancelSource.Token);
                if (ok) done++; else failed++;
                OverallProgress.Value = done + failed;
            }

            ProgressText.Text = failed == 0
                ? $"Done — installed {done} app(s)."
                : $"Done — installed {done} app(s), {failed} failed.";

            CancelButton.Visibility = Visibility.Collapsed;
            SetControlsEnabled(true);
            _isInstalling = false;
            _cancelSource = null;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancelSource?.Cancel();
            CancelButton.IsEnabled = false;
        }

        private void SetControlsEnabled(bool enabled)
        {
            InstallButton.IsEnabled = enabled;
            foreach (var child in SoftwareList.Children)
            {
                if (child is SoftwareRow row) row.SetInteractive(enabled);
            }
        }

        private void AppendLog(string text)
        {
            LogBox.AppendText(text + Environment.NewLine);
            LogBox.ScrollToEnd();
        }
    }
}
