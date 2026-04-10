using System;

namespace FlightSupervisor.UI.Models
{
    public class CrewProfile
    {
        public string AirlineIcao { get; set; } = "UNK";
        
        /// <summary>
        /// Overall efficiency of the crew (physical speed, crisis management, proactivity).
        /// Value is clamped realistically based on the airline tier.
        /// Elite: 90-98%, Stanadard: ~80%, Low Cost: ~70%, Danger: ~30%
        /// </summary>
        public double Efficiency { get; set; } = 50.0;
        
        public string StatusMessage { get; set; } = "Idle";
        
        // Potential future extensions
        public int NumberOfCabinCrew { get; set; } = 4;
    }
}
