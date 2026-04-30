using System;
using System.IO;
using System.Text.Json;
using produKtiviti.Models;

namespace produKtiviti.Services
{
    public class SettingsService
    {
        private static SettingsService? _instance;
        public static SettingsService Instance => _instance ??= new SettingsService();

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "produKtiviti",
            "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public AppSettings Current { get; private set; } = new();

        private SettingsService() { }

        public void Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                }
            }
            catch
            {
                Current = new AppSettings();
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                var json = JsonSerializer.Serialize(Current, JsonOptions);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
