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
            if (basePath == null) 
            {
                basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor");
            }
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
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
                    File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor", "ProfileLoadError.txt"), ex.ToString());
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
                File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor", "ProfileError.log"), 
                    $"[{DateTime.Now}] Error saving profile: {ex}\n");
            }
        }
    }
}
