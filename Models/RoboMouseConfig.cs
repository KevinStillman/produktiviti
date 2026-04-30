namespace produKtiviti.Models
{
    public class RoboMouseConfig
    {
        public string StartKey { get; set; } = string.Empty;
        public string StopKey { get; set; } = string.Empty;
        public int IntervalMs { get; set; } = 1000;
        public bool RandomizeInterval { get; set; } = false;
        public int RandomMinMs { get; set; } = 800;
        public int RandomMaxMs { get; set; } = 1200;
        public bool UseRightButton { get; set; } = false;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(StartKey) &&
            !string.IsNullOrWhiteSpace(StopKey) &&
            IntervalMs > 0;
    }
}
