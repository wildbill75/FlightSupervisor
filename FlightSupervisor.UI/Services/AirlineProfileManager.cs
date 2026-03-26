using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlightSupervisor.UI.Services
{
    public class AirlineObjectives
    {
        [JsonPropertyName("maxDelaySec")]
        public int MaxDelaySec { get; set; } = 900;

        [JsonPropertyName("minComfort")]
        public int MinComfort { get; set; } = 50;

        [JsonPropertyName("maxTouchdownFpm")]
        public int MaxTouchdownFpm { get; set; } = -500;

        [JsonPropertyName("mustPerformCatering")]
        public bool MustPerformCatering { get; set; } = false;
    }

    /// <summary>
    /// Représente le profil complet d'une compagnie aérienne, définissant sa réputation,
    /// ses scores d'infrastructure et ses directives opérationnelles.
    /// </summary>
    public class AirlineProfile
    {
        [JsonPropertyName("icao")]
        public string Icao { get; set; } = "UNK";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "Unknown Airlines";

        [JsonPropertyName("globalScore")]
        public int GlobalScore { get; set; } = 50; // Note globale sur 100

        [JsonPropertyName("hardProductScore")]
        public int HardProductScore { get; set; } = 5; // Qualité cabine, espace (1-10)

        [JsonPropertyName("softProductScore")]
        public int SoftProductScore { get; set; } = 5; // Service, catering (1-10)

        [JsonPropertyName("safetyRecord")]
        public int SafetyRecord { get; set; } = 5; // Historique de fiabilité (1-10)
        
        [JsonPropertyName("punctualityPriority")]
        public int PunctualityPriority { get; set; } = 5; // 1 = Confort d'abord, 10 = A l'heure coûte que coûte

        [JsonPropertyName("directives")]
        public List<string> Directives { get; set; } = new List<string>();

        [JsonPropertyName("objectives")]
        public AirlineObjectives Objectives { get; set; } = new AirlineObjectives();
    }

    /// <summary>
    /// Gestionnaire central du Tycoon. Charge et distribue les profils de compagnies.
    /// Implémenté pour permettre une mise à jour externe via un fichier JSON (Airlines.json).
    /// </summary>
    public class AirlineProfileManager
    {
        private readonly string _databasePath;
        private Dictionary<string, AirlineProfile> _profiles = new Dictionary<string, AirlineProfile>(StringComparer.OrdinalIgnoreCase);

        // Profil générique de secours
        private readonly AirlineProfile _defaultProfile = new AirlineProfile
        {
            Icao = "DEF",
            Name = "Charter Airlines",
            GlobalScore = 50,
            HardProductScore = 5,
            SoftProductScore = 5,
            SafetyRecord = 5,
            PunctualityPriority = 5,
            Directives = new List<string> { "La sécurité avant tout.", "Respectez les horaires raisonnables." },
            Objectives = new AirlineObjectives()
        };

        /// <summary>
        /// Instancie le gestionnaire et tente de charger la base de données.
        /// </summary>
        /// <param name="databasePath">Chemin complet vers le fichier Airlines.json</param>
        public AirlineProfileManager(string databasePath)
        {
            _databasePath = databasePath;
            LoadDatabase();
        }

        /// <summary>
        /// Charge la liste des compagnies depuis le fichier JSON. 
        /// En cas d'erreur de lecture ou de parsing, log le problème et garde le fallback.
        /// </summary>
        private void LoadDatabase()
        {
            try
            {
                if (!File.Exists(_databasePath))
                {
                    GenerateDefaultDatabase();
                }

                string jsonString = File.ReadAllText(_databasePath);
                var loadedList = JsonSerializer.Deserialize<List<AirlineProfile>>(jsonString);
                
                if (loadedList != null)
                {
                    _profiles = loadedList.ToDictionary(p => p.Icao, StringComparer.OrdinalIgnoreCase);
                    Console.WriteLine($"[AirlineProfileManager] Succès: {loadedList.Count} profils de compagnies chargés depuis {_databasePath}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AirlineProfileManager] ERREUR critique lors du chargement de {_databasePath}: {ex.Message}");
                // Le dictionnaire reste vide, le fallback s'appliquera.
            }
        }

        /// <summary>
        /// Génère un fichier de base si la base de données JSON n'existe pas encore.
        /// </summary>
        private void GenerateDefaultDatabase()
        {
            try
            {
                var defaults = new List<AirlineProfile>
                {
                    new AirlineProfile {
                        Icao = "AFR", Name = "Air France", GlobalScore = 82, HardProductScore = 7, SoftProductScore = 8, SafetyRecord = 9, PunctualityPriority = 6,
                        Directives = new List<string> { "Maintenir l'élégance du service client", "Eviter les grèves passagers" }
                    },
                    new AirlineProfile {
                        Icao = "RYR", Name = "Ryanair", GlobalScore = 45, HardProductScore = 2, SoftProductScore = 2, SafetyRecord = 8, PunctualityPriority = 10,
                        Directives = new List<string> { "Le turnaround en 25 minutes est ABSOLU", "Le confort n'est pas contractuel" }
                    },
                    new AirlineProfile {
                        Icao = "UAE", Name = "Emirates", GlobalScore = 95, HardProductScore = 9, SoftProductScore = 9, SafetyRecord = 9, PunctualityPriority = 7,
                        Directives = new List<string> { "Garantir une expérience Premium 5-Étoiles", "Priorité absolue aux passagers Première" }
                    }
                };

                string directory = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string jsonTemplate = JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_databasePath, jsonTemplate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AirlineProfileManager] ERREUR lors de la création de la base par défaut: {ex.Message}");
            }
        }

        /// <summary>
        /// Renvoie le graphe de notation complet pour une compagnie donnée.
        /// </summary>
        /// <param name="icao">Code ICAO de la compagnie (ex: AFR)</param>
        /// <returns>Objet AirlineProfile, jamais nul (utilise un fallback en cas d'ICAO inconnu).</returns>
        public AirlineProfile GetProfileFor(string icao)
        {
            if (string.IsNullOrWhiteSpace(icao))
                return _defaultProfile;

            if (_profiles.TryGetValue(icao, out var profile))
                return profile;

            // Retour de sécurité si la compagnie n'existe pas dans le JSON
            var fallback = new AirlineProfile
            {
                Icao = icao,
                Name = $"Flight {icao}",
                GlobalScore = _defaultProfile.GlobalScore,
                HardProductScore = _defaultProfile.HardProductScore,
                SoftProductScore = _defaultProfile.SoftProductScore,
                SafetyRecord = _defaultProfile.SafetyRecord,
                PunctualityPriority = _defaultProfile.PunctualityPriority,
                Directives = new List<string>(_defaultProfile.Directives)
            };
            
            return fallback;
        }
    }
}
