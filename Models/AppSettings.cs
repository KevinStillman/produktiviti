namespace produKtiviti.Models
{
    public class AppSettings
    {
        public bool DarkMode { get; set; } = true;

        // Feature enabled states
        public bool SmartTabsEnabled { get; set; } = false;
        public bool RoboMouseEnabled { get; set; } = false;

        // Feature favorite states
        public bool SmartTabsFavorite { get; set; } = false;
        public bool RoboMouseFavorite { get; set; } = false;

        // RoboMouse configuration
        public RoboMouseConfig RoboMouse { get; set; } = new();
    }
}
