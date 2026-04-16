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
            "APU_BLEED_FAULT", "PACK_1_FAULT", "PACK_2_FAULT", "ANTI_ICE_VALVE_STUCK",
            "ELEC_GEN_1_WARN", "BRAKE_FAN_INOP", "PUMP_Y_ELEC_INOP"
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
    }
}
