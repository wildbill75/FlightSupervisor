using System;
using System.Collections.Generic;
using FlightSupervisor.UI.Models;

namespace FlightSupervisor.UI.Services
{
    public static class AirframeHistoryGenerator
    {
        private static readonly Dictionary<string, MaintenanceQuality> AirlineQualityMap = new Dictionary<string, MaintenanceQuality>(StringComparer.OrdinalIgnoreCase)
        {
            { "RYR", MaintenanceQuality.Low },
            { "EZY", MaintenanceQuality.Low },
            { "WZZ", MaintenanceQuality.Low },
            { "SWA", MaintenanceQuality.Low },
            { "NKS", MaintenanceQuality.Low },
            { "AFR", MaintenanceQuality.Medium },
            { "BAW", MaintenanceQuality.Medium },
            { "DLH", MaintenanceQuality.Medium },
            { "AAL", MaintenanceQuality.Medium },
            { "DAL", MaintenanceQuality.Medium },
            { "SIA", MaintenanceQuality.High },
            { "QTR", MaintenanceQuality.High },
            { "UAE", MaintenanceQuality.High },
            { "ANA", MaintenanceQuality.High },
            { "JAL", MaintenanceQuality.High }
        };

        private static readonly string[] SoftDefects = 
        {
            "LAV_1_INOP", "LAV_2_INOP", "WIFI_INOP", "IFE_SYSTEM_FAULT", 
            "PA_SYSTEM_STATIC", "COFFEE_MAKER_FWD_INOP", "OVEN_AFT_INOP",
            "SEAT_12A_RECLINE", "CABIN_LIGHTING_ZONE_A_DIM", "WATER_SYSTEM_LEAK"
        };

        private static readonly string[] HardDefects = 
        {
            "GEAR_DOOR_SCRATCH", "FLAPS_TRACK_WEAR", "BRAKE_FAN_INOP", "NOSE_GEAR_STEERING_SLUGGISH",
            "SPOILER_ACTUATOR_LEAK", "TIRE_WEAR_LIMIT", "HYDRAULIC_MINOR_SEEPAGE"
        };

        public enum MaintenanceQuality
        {
            High,
            Medium,
            Low
        }

        public static void GenerateHistory(AirframeState state, Random rand)
        {
            var quality = AirlineQualityMap.ContainsKey(state.Airline) 
                ? AirlineQualityMap[state.Airline] 
                : MaintenanceQuality.Medium;

            double qualityFactor = quality switch
            {
                MaintenanceQuality.High => 0.5,
                MaintenanceQuality.Medium => 1.0,
                MaintenanceQuality.Low => 2.0,
                _ => 1.0
            };

            // Age is in years. The older it is, and the lower the quality, the more wear.
            double ageFactor = Math.Max(1.0, state.AgeInYears / 5.0); 

            state.EngineWear = Math.Min(100.0, rand.NextDouble() * 10 * ageFactor * qualityFactor);
            state.StructureWear = Math.Min(100.0, rand.NextDouble() * 5 * ageFactor * qualityFactor);
            state.FlapsWear = Math.Min(100.0, rand.NextDouble() * 8 * ageFactor * qualityFactor);
            state.GearAndBrakeWear = Math.Min(100.0, rand.NextDouble() * 15 * ageFactor * qualityFactor);

            // Generate active defects
            int maxDefects = (int)(3 * ageFactor * qualityFactor);
            int defectCount = rand.Next(0, maxDefects + 1);

            for (int i = 0; i < defectCount; i++)
            {
                bool isSoft = rand.NextDouble() > 0.3;
                string defect = isSoft 
                    ? SoftDefects[rand.Next(SoftDefects.Length)] 
                    : HardDefects[rand.Next(HardDefects.Length)];

                if (!state.ActiveDefects.Contains(defect))
                {
                    state.ActiveDefects.Add(defect);
                    
                    state.Events.Add(new AirframeLogEvent
                    {
                        Timestamp = DateTime.Now.AddDays(-rand.Next(1, 30)),
                        Type = "defect_open",
                        Location = "",
                        Description = $"Defect recorded: {defect}",
                        Severity = isSoft ? "warn" : "error"
                    });
                }
            }
        }

        public static void CatchUpHistory(AirframeState state, string currentIcao)
        {
            if (state.Events == null || state.Events.Count == 0) return;

            var rand = new Random(state.Registration.GetHashCode() ^ DateTime.Now.DayOfYear);
            
            // Events are stored with newest at index 0
            var lastEvent = state.Events[0];
            DateTime lastDate = lastEvent.Timestamp;
            DateTime now = DateTime.UtcNow;

            double daysGap = (now - lastDate).TotalDays;
            if (daysGap < 1.0) return; // No need to catch up if less than 24 hours have passed

            string lastLocation = lastEvent.Location;
            if (string.IsNullOrEmpty(lastLocation)) lastLocation = "UNKNOWN";

            // If the gap is massive (> 14 days), we insert a prolonged storage event
            if (daysGap > 14.0)
            {
                state.Events.Insert(0, new AirframeLogEvent
                {
                    Timestamp = now.AddDays(-14),
                    Type = "maintenance",
                    Location = lastLocation,
                    Description = $"Prolonged storage and heavy maintenance check completed. Airframe preserved for {(int)daysGap} days.",
                    Severity = "info"
                });
                
                // Clear some active defects as they would have been fixed
                state.ActiveDefects.Clear();

                // Reset the simulation start point to 14 days ago
                lastDate = now.AddDays(-14);
                daysGap = 14.0;
            }

            // Simulate the active flights day by day
            int daysToSimulate = (int)Math.Floor(daysGap);
            for (int d = daysToSimulate; d >= 1; d--)
            {
                DateTime simDate = now.AddDays(-d).AddHours(rand.Next(6, 12));
                int flightsToday = rand.Next(1, 5); // 1 to 4 flights

                for (int f = 0; f < flightsToday; f++)
                {
                    simDate = simDate.AddHours(rand.Next(2, 5));
                    
                    string dest = GetRandomEuropeanIcao(rand);
                    
                    // If this is the absolute last generated flight, it MUST arrive at currentIcao
                    if (d == 1 && f == flightsToday - 1 && !string.IsNullOrEmpty(currentIcao))
                    {
                        dest = currentIcao;
                    }

                    double blockHours = rand.Next(1, 4) + (rand.Next(10, 59) / 60.0);
                    
                    state.Events.Insert(0, new AirframeLogEvent
                    {
                        Timestamp = simDate,
                        Type = "flight",
                        Location = dest,
                        Description = $"Flight from {lastLocation} to {dest}. Block Time: {(int)blockHours}h{(int)((blockHours % 1) * 60):00}m"
                    });

                    state.TotalHours += Math.Round(blockHours, 1);
                    state.TotalCycles += 1;
                    
                    // Minor wear & tear addition
                    state.EngineWear = Math.Min(100.0, state.EngineWear + (blockHours * 0.05));
                    state.GearAndBrakeWear = Math.Min(100.0, state.GearAndBrakeWear + 0.1);

                    lastLocation = dest;
                }
            }
            
            // Re-sort the list just to be absolutely sure index 0 is newest
            state.Events.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
        }

        public static string GetRandomEuropeanIcao(Random rand)
        {
            string[] icaos = { "LFPG", "LEPA", "EGLL", "EHAM", "EDDF", "LEMD", "LIRF", "LPPT", "LFSB", "LSZH", "ENGM", "EIDW", "LROP", "LFMN" };
            return icaos[rand.Next(icaos.Length)];
        }
    }
}
