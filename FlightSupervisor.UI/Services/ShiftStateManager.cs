using System;
using System.IO;
using System.Text.Json;

namespace FlightSupervisor.UI.Services
{
    public class ShiftState
    {
        public int SessionFlightsCompleted { get; set; }
        public double CabinCleanliness { get; set; }
        public double WaterLevel { get; set; }
        public double WasteLevel { get; set; }
        public double VirtualFuelPercentage { get; set; }
        public int CateringRations { get; set; }
        public double CrewProactivity { get; set; }
        public double CrewEfficiency { get; set; }
        public double CrewMorale { get; set; }
        public double CrewEsteem { get; set; }
        public string LastArrivalIcao { get; set; } = string.Empty;
        public string LastAirlineName { get; set; } = string.Empty;
        public DateTime SavedAtLocal { get; set; } = DateTime.Now;
    }

    public class ShiftStateManager
    {
        private static string StateFilePath 
        {
            get 
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "ShiftState.json");
            }
        }

        public static void SaveState(CabinManager cabinManager, string arrivalIcao, string airlineName)
        {
            try
            {
                var state = new ShiftState
                {
                    SessionFlightsCompleted = cabinManager.SessionFlightsCompleted,
                    CabinCleanliness = cabinManager.CabinCleanliness,
                    WaterLevel = cabinManager.WaterLevel,
                    WasteLevel = cabinManager.WasteLevel,
                    VirtualFuelPercentage = cabinManager.VirtualFuelPercentage,
                    CateringRations = cabinManager.CateringRations,
                    CrewProactivity = cabinManager.CrewProactivity,
                    CrewEfficiency = cabinManager.CrewEfficiency,
                    CrewMorale = cabinManager.CrewMorale,
                    CrewEsteem = cabinManager.CrewEsteem,
                    LastArrivalIcao = arrivalIcao,
                    LastAirlineName = airlineName,
                    SavedAtLocal = DateTime.Now
                };

                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StateFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save ShiftState: {ex.Message}");
            }
        }

        public static ShiftState? LoadState()
        {
            try
            {
                if (!File.Exists(StateFilePath))
                    return null;

                string json = File.ReadAllText(StateFilePath);
                var state = JsonSerializer.Deserialize<ShiftState>(json);
                return state;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load ShiftState: {ex.Message}");
                return null;
            }
        }

        public static void ClearState()
        {
            try
            {
                if (File.Exists(StateFilePath))
                    File.Delete(StateFilePath);
            }
            catch { }
        }
    }
}

