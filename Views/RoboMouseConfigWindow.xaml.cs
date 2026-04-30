using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using produKtiviti.Models;

namespace produKtiviti.Views
{
    public partial class RoboMouseConfigWindow : Window
    {
        public RoboMouseConfig? Result { get; private set; }

        private TextBox? _focusedKeyBox;

        public RoboMouseConfigWindow(RoboMouseConfig existing)
        {
            InitializeComponent();

            // Populate from existing config
            StartKeyBox.Text = existing.StartKey;
            StopKeyBox.Text = existing.StopKey;
            IntervalBox.Text = existing.IntervalMs.ToString();
            RandomizeCheck.IsChecked = existing.RandomizeInterval;
            RandomMinBox.Text = existing.RandomMinMs.ToString();
            RandomMaxBox.Text = existing.RandomMaxMs.ToString();
            RightButton.IsChecked = existing.UseRightButton;
            LeftButton.IsChecked = !existing.UseRightButton;

            UpdateRandomPanel();
        }

        // ── Key capture ──────────────────────────────────────────────────────

        private void KeyBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _focusedKeyBox = sender as TextBox;
            if (_focusedKeyBox != null)
                _focusedKeyBox.Text = "Press a key...";
        }

        private void KeyBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _focusedKeyBox = null;
        }

        private void KeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox box) return;
            e.Handled = true;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ignore modifier-only presses
            if (key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin)
                return;

            box.Text = key.ToString();
            Keyboard.ClearFocus();
        }

        // ── Randomize toggle ─────────────────────────────────────────────────

        private void RandomizeCheck_Changed(object sender, RoutedEventArgs e)
        {
            UpdateRandomPanel();
        }

        private void UpdateRandomPanel()
        {
            if (RandomPanel == null) return;
            RandomPanel.IsEnabled = RandomizeCheck.IsChecked == true;
            RandomPanel.Opacity = RandomizeCheck.IsChecked == true ? 1.0 : 0.4;
        }

        // ── Save / Cancel ────────────────────────────────────────────────────

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ValidationText.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(StartKeyBox.Text) || StartKeyBox.Text == "Press a key...")
            {
                ShowError("Please set a Start Key.");
                return;
            }
            if (string.IsNullOrWhiteSpace(StopKeyBox.Text) || StopKeyBox.Text == "Press a key...")
            {
                ShowError("Please set a Stop Key.");
                return;
            }
            if (!int.TryParse(IntervalBox.Text, out int interval) || interval < 1)
            {
                ShowError("Click interval must be a positive number.");
                return;
            }

            int randomMin = 0, randomMax = 0;
            if (RandomizeCheck.IsChecked == true)
            {
                if (!int.TryParse(RandomMinBox.Text, out randomMin) || randomMin < 1)
                {
                    ShowError("Random min must be a positive number.");
                    return;
                }
                if (!int.TryParse(RandomMaxBox.Text, out randomMax) || randomMax <= randomMin)
                {
                    ShowError("Random max must be greater than min.");
                    return;
                }
            }

            Result = new RoboMouseConfig
            {
                StartKey = StartKeyBox.Text,
                StopKey = StopKeyBox.Text,
                IntervalMs = interval,
                RandomizeInterval = RandomizeCheck.IsChecked == true,
                RandomMinMs = randomMin,
                RandomMaxMs = randomMax,
                UseRightButton = RightButton.IsChecked == true
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string msg)
        {
            ValidationText.Text = msg;
            ValidationText.Visibility = Visibility.Visible;
        }
    }
}
