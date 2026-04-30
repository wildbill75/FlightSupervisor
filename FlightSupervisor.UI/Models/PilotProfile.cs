using System;
using System.Collections.Generic;

namespace FlightSupervisor.UI.Models
{
    public class PilotProfile
    {
        public string FirstName { get; set; } = "John";
        public string LastName { get; set; } = "Doe";
        public string CallSign { get; set; } = "MAVERICK";
        
        // Identity & Roleplay
        public string SimBriefUsername { get; set; } = "";
        public string WeatherSource { get; set; } = "simbrief"; // Valid: "simbrief", "noaa", "activesky"
        public string HomeBaseIcao { get; set; } = "LFPG";
        public string CountryCode { get; set; } = "fr"; // ISO 2-letter for flags
        public string FavoriteAircraft { get; set; } = "A320 Family";
        public bool GsxAutoSyncEnabled { get; set; } = false;
        
        [System.Text.Json.Serialization.JsonIgnore]
        public string ProfileImageBase64 { get; set; } = ""; // Stored separately via virtual host to bypass IPC

        public string AvatarPosition { get; set; } = "50% 50%"; // CSS object-position

        // Dynamic Rank calculation based on flight hours
        public string CalculatedRank 
        {
            get
            {
                double hours = TotalBlockTimeMinutes / 60.0;
                if (hours < 10) return "Trainee";
                if (hours < 50) return "First Officer";
                if (hours < 200) return "Senior First Officer";
                if (hours < 500) return "Captain";
                if (hours < 1000) return "Senior Captain";
                return "Chief Pilot";
            }
        }
        
        // Global Career Stats
        public int TotalFlights { get; set; } = 0;
        public int TotalBlockTimeMinutes { get; set; } = 0;
        public double TotalDistanceFlownNM { get; set; } = 0;
        public int PassengersTransported { get; set; } = 0;
        public double CargoHauledKgs { get; set; } = 0;
        public double FuelBurnedKgs { get; set; } = 0;

        // Performance Stats
        public double AverageSuperScore { get; set; } = 0;
        public int HighestSuperScore { get; set; } = 0;
        public double PunctualityRatingPercentage { get; set; } = 0;
        public double AverageDelayMinutes { get; set; } = 0;
        public double SmoothestTouchdownFpm { get; set; } = 0;
        public double HardestImpactFpm { get; set; } = 0;
        public int ManualFlyingTimeMinutes { get; set; } = 0;

        // Operational Stats
        public int TotalGoArounds { get; set; } = 0;
        public int TotalDiversions { get; set; } = 0;
        public int GroundOpsAborted { get; set; } = 0;
        public int SafetyInfractions { get; set; } = 0;

        // Achievements / Badges
        // A list of achievement IDs that the player has unlocked
        public List<string> UnlockedAchievements { get; set; } = new List<string>();

        public DateTime LastFlightDate { get; set; } = DateTime.MinValue;
    }
}
