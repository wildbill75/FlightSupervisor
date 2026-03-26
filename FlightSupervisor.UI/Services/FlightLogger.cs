using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FlightSupervisor.UI.Models;

namespace FlightSupervisor.UI.Services
{
    public static class FlightLogger
    {
        private static readonly string _logsDirectory;

        static FlightLogger()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logsDirectory = Path.Combine(appDataPath, "FlightSupervisor", "Logs");
            if (!Directory.Exists(_logsDirectory))
            {
                Directory.CreateDirectory(_logsDirectory);
            }
        }

        public static void ArchiveFlight(FlightArchive record)
        {
            try
            {
                var fileName = $"Flight_{record.FlightDate:yyyyMMdd_HHmm}_{record.Dep}_{record.Arr}.json";
                var filePath = Path.Combine(_logsDirectory, fileName);
                
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(record, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to archive flight: {ex.Message}");
            }
        }

        public static List<FlightArchive> GetLogbook()
        {
            var archives = new List<FlightArchive>();
            try
            {
                var files = Directory.GetFiles(_logsDirectory, "Flight_*.json");
                foreach (var file in files)
                {
                    var json = File.ReadAllText(file);
                    var record = JsonSerializer.Deserialize<FlightArchive>(json);
                    if (record != null)
                    {
                        archives.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load logbook: {ex.Message}");
            }
            return archives.OrderByDescending(f => f.FlightDate).ToList();
        }
    }
}
