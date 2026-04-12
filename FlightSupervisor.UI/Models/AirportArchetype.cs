namespace FlightSupervisor.UI.Models
{
    public enum AirportTier
    {
        S_MegaHub,
        A_Large,
        B_Medium,
        F_Small
    }

    public enum AirportDestinationType
    {
        Business,
        Holiday
    }

    public class AirportArchetype
    {
        public string Icao { get; set; } = "XXXX";
        
        public AirportDestinationType Type { get; set; } = AirportDestinationType.Business;

        
        /// <summary>
        /// Defines the size of the airport, which will directly impact Ground Ops approach delays.
        /// </summary>
        public AirportTier Tier { get; set; } = AirportTier.B_Medium;

        /// <summary>
        /// S/A Tiers have shorter approach times (trucks are numerous and nearby).
        /// F/B Tiers have longer approach times (sharing fewer trucks).
        /// For testing/debugging, we override the calculated delay to 0.
        /// </summary>
        public int GetGroundServiceApproachDelayMinutes(bool isDebugFlow = false)
        {
            if (isDebugFlow) return 0; // Forced user requirement during testing

            switch (Tier)
            {
                case AirportTier.S_MegaHub:
                case AirportTier.A_Large:
                    return 2; // Very fast
                case AirportTier.B_Medium:
                    return 5; // Moderate Wait
                case AirportTier.F_Small:
                    return 10; // Long Wait (single truck)
                default:
                    return 5;
            }
        }
    }
}
