using System;
using System.Collections.Generic;

namespace FlightSupervisor.UI.Services
{
    public static class LocalizationService
    {
        public static string CurrentLanguage { get; set; } = "en";

        private static readonly Dictionary<string, Dictionary<string, string>> _dict = new()
        {
            {
                "en", new Dictionary<string, string>
                {
                    { "Boarding", "Boarding" },
                    { "Catering", "Catering" },
                    { "Refueling", "Refueling" },
                    { "Cargo", "Cargo" },
                    { "Cleaning", "Cabin Cleaning" },
                    { "FlightCancel", "Flight Cancelled: Unauthorized movement during Ground Operations!" },
                    { "BaggageLost", "Comfort Penalty: {0} delayed bags due to missing Cargo service." }
                }
            },
            {
                "fr", new Dictionary<string, string>
                {
                    { "Boarding", "Embarquement" },
                    { "Catering", "Restauration" },
                    { "Refueling", "Avitaillement" },
                    { "Cargo", "Bagages/Fret" },
                    { "Cleaning", "Nettoyage Cabine" },
                    { "FlightCancel", "Vol Annulé: Mouvement non autorisé pendant les Opérations au Sol!" },
                    { "BaggageLost", "Pénalité Confort: {0} bagages retardés car le service Soute été ignoré." }
                }
            }
        };

        public static string GetString(string key, string defaultEn = "")
        {
            if (_dict.TryGetValue(CurrentLanguage, out var langDict) && langDict.TryGetValue(key, out var val))
            {
                return val;
            }
            return string.IsNullOrEmpty(defaultEn) ? key : defaultEn;
        }

        public static string Translate(string enDefault, string frTranslation)
        {
            return CurrentLanguage == "fr" ? frTranslation : enDefault;
        }
    }
}
