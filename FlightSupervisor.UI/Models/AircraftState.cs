using System;

namespace FlightSupervisor.UI.Models
{
    /// <summary>
    /// Représente l'état persistant de la machine d'une branche (leg) à l'autre.
    /// Sera manipulé par une tick loop sans interférer avec les simvars de l'avion MSFS pour le moment.
    /// </summary>
    public class AircraftState
    {
        /// <summary>
        /// Pourcentage virtuel de carburant (rempli virtuellement au sol selon SimBrief)
        /// Se dégrade mathématiquement en vol.
        /// </summary>
        public double VirtualFuelPercentage { get; set; } = 100.0;

        /// <summary>
        /// 100% = Sparkling clean. Se dégrade en vol selon pax/crew. 
        /// Low Cost = PNC cleans it. Standard = Cleaning truck.
        /// </summary>
        public double CleanlinessPercentage { get; set; } = 100.0;

        /// <summary>
        /// Eau potable de l'avion. Baisse au cours du vol.
        /// </summary>
        public double PotableWaterPercentage { get; set; } = 100.0;

        /// <summary>
        /// Réservoirs des WC. Monte au cours du vol. Si 100%, toilettes closes = crise passagers.
        /// </summary>
        public double WasteTankPercentage { get; set; } = 0.0;
        
        /// <summary>
        /// Reset the internal parameters for a fresh start (not meant to be called between mutli-legs, only on hard reboot)
        /// </summary>
        public void ResetState()
        {
            VirtualFuelPercentage = 100.0;
            CleanlinessPercentage = 100.0;
            PotableWaterPercentage = 100.0;
            WasteTankPercentage = 0.0;
        }
    }
}
