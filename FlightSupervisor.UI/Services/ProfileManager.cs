using System;
using System.IO;
using System.Text.Json;
using FlightSupervisor.UI.Models;

namespace FlightSupervisor.UI.Services
{
    public class ProfileManager
    {
        private readonly string _filePath;
        public PilotProfile CurrentProfile { get; private set; }

        public ProfileManager(string basePath = null)
        {
            if (basePath == null) basePath = AppDomain.CurrentDomain.BaseDirectory;
            _filePath = Path.Combine(basePath, "Profile.json");
            LoadProfile();
        }

        public void LoadProfile()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    CurrentProfile = JsonSerializer.Deserialize<PilotProfile>(json) ?? new PilotProfile();
                }
                catch (Exception ex)
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProfileLoadError.txt"), ex.ToString());
                    CurrentProfile = new PilotProfile(); // fallback
                }
            }
            else
            {
                CurrentProfile = new PilotProfile();
                SaveProfile(); // Create initial file
            }
        }

        public void SaveProfile()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(CurrentProfile, options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving profile: " + ex.Message);
            }
        }
    }
}
