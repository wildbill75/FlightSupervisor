using System;
using System.Collections.Generic;
using FlightSupervisor.UI.Services; // For BadgeDefinition

namespace FlightSupervisor.UI.Models
{
    public class FlightArchive
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime FlightDate { get; set; } = DateTime.Now;

        // Scores
        public int Score { get; set; }
        public int SafetyPoints { get; set; }
        public int ComfortPoints { get; set; }
        public int MaintenancePoints { get; set; }
        public int OperationsPoints { get; set; }

        public List<object> FlightEvents { get; set; } = new List<object>();
        public List<object> Objectives { get; set; } = new List<object>();
        public List<BadgeDefinition> NewAchievements { get; set; } = new List<BadgeDefinition>();

        // Flight Info
        public string Dep { get; set; }
        public string Arr { get; set; }
        public string FlightNo { get; set; }
        public string Airline { get; set; }

        // Metrics
        public int BlockTime { get; set; }
        public long SchedBlockTime { get; set; }
        public double TouchdownFpm { get; set; }
        public double TouchdownGForce { get; set; }
        
        public string Zfw { get; set; }
        public string Tow { get; set; }
        public string BlockFuel { get; set; }
        
        public long DelaySec { get; set; }
        public long RawDelaySec { get; set; }
    }
}
