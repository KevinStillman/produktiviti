using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace produKtiviti.Models
{
    public class FeatureItem : INotifyPropertyChanged
    {
        private bool _isEnabled;

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;

        private bool _needsSetup;
        public bool NeedsSetup
        {
            get => _needsSetup;
            set
            {
                if (_needsSetup == value) return;
                _needsSetup = value;
                OnPropertyChanged();
            }
        }

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite == value) return;
                _isFavorite = value;
                OnPropertyChanged();
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
