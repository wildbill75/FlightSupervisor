using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlightSupervisor.UI.Services
{
    public class AirlineDatabaseRoot
    {
        [JsonPropertyName("airlines")]
        public List<AirlineProfile> Airlines { get; set; } = new List<AirlineProfile>();
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

        [JsonPropertyName("global_score")]
        public int GlobalScore { get; set; } = 50; // Note globale sur 100

        [JsonPropertyName("hard_product")]
        public int HardProductScore { get; set; } = 5; // Qualité cabine, espace (1-10)

        [JsonPropertyName("soft_product")]
        public int SoftProductScore { get; set; } = 5; // Service, catering (1-10)

        [JsonPropertyName("safety_rec")]
        public int SafetyRecord { get; set; } = 5; // Historique de fiabilité (1-10)
        
        [JsonPropertyName("punctuality")]
        public int PunctualityPriority { get; set; } = 5; // 1 = Confort d'abord, 10 = A l'heure coûte que coûte

        [JsonPropertyName("tier")]
        public string Tier { get; set; } = "Standard"; // Elite, Standard, LowCost, Struggling, Danger

        [JsonPropertyName("directives")]
        public List<string> Directives { get; set; } = new List<string>();

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
            Directives = new List<string> { "La sécurité avant tout.", "Respectez les horaires raisonnables." }
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
                var rootData = JsonSerializer.Deserialize<AirlineDatabaseRoot>(jsonString);
                
                if (rootData != null && rootData.Airlines != null)
                {
                    _profiles = rootData.Airlines.ToDictionary(p => p.Icao, StringComparer.OrdinalIgnoreCase);
                    Console.WriteLine($"[AirlineProfileManager] Succès: {rootData.Airlines.Count} profils de compagnies chargés depuis {_databasePath}.");
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
        /// Construit avec la liste officielle requise par le Flight Supervisor.
        /// </summary>
        private void GenerateDefaultDatabase()
        {
            try
            {
                string directory = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string jsonTemplate = @"{
  ""airlines"": [
    { ""name"": ""Singapore Airlines"", ""icao"": ""SIA"", ""hard_product"": 10, ""soft_product"": 10, ""safety_rec"": 10, ""punctuality"": 9, ""global_score"": 98, ""tier"": ""Elite"" },
    { ""name"": ""Qatar Airways"", ""icao"": ""QTR"", ""hard_product"": 10, ""soft_product"": 10, ""safety_rec"": 10, ""punctuality"": 8, ""global_score"": 96, ""tier"": ""Elite"" },
    { ""name"": ""ANA (All Nippon)"", ""icao"": ""ANA"", ""hard_product"": 9, ""soft_product"": 9, ""safety_rec"": 10, ""punctuality"": 10, ""global_score"": 95, ""tier"": ""Elite"" },
    { ""name"": ""Emirates"", ""icao"": ""UAE"", ""hard_product"": 10, ""soft_product"": 9, ""safety_rec"": 9, ""punctuality"": 8, ""global_score"": 92, ""tier"": ""Elite"" },
    { ""name"": ""Air France"", ""icao"": ""AFR"", ""hard_product"": 9, ""soft_product"": 10, ""safety_rec"": 9, ""punctuality"": 8, ""global_score"": 91, ""tier"": ""Elite"" },
    { ""name"": ""Delta Air Lines"", ""icao"": ""DAL"", ""hard_product"": 7, ""soft_product"": 7, ""safety_rec"": 9, ""punctuality"": 9, ""global_score"": 82, ""tier"": ""Standard"" },
    { ""name"": ""Lufthansa"", ""icao"": ""DLH"", ""hard_product"": 7, ""soft_product"": 6, ""safety_rec"": 10, ""punctuality"": 7, ""global_score"": 78, ""tier"": ""Standard"" },
    { ""name"": ""Iberia"", ""icao"": ""IBE"", ""hard_product"": 6, ""soft_product"": 6, ""safety_rec"": 9, ""punctuality"": 10, ""global_score"": 77, ""tier"": ""Standard"" },
    { ""name"": ""Corsair"", ""icao"": ""CRL"", ""hard_product"": 7, ""soft_product"": 7, ""safety_rec"": 9, ""punctuality"": 7, ""global_score"": 75, ""tier"": ""Standard"" },
    { ""name"": ""Transavia"", ""icao"": ""TVF"", ""hard_product"": 6, ""soft_product"": 6, ""safety_rec"": 9, ""punctuality"": 8, ""global_score"": 72, ""tier"": ""Standard"" },
    { ""name"": ""British Airways"", ""icao"": ""BAW"", ""hard_product"": 7, ""soft_product"": 6, ""safety_rec"": 9, ""punctuality"": 5, ""global_score"": 70, ""tier"": ""Standard"" },
    { ""name"": ""Southwest Airlines"", ""icao"": ""SWA"", ""hard_product"": 5, ""soft_product"": 5, ""safety_rec"": 9, ""punctuality"": 8, ""global_score"": 68, ""tier"": ""LowCost"" },
    { ""name"": ""Volotea"", ""icao"": ""VOE"", ""hard_product"": 5, ""soft_product"": 5, ""safety_rec"": 8, ""punctuality"": 7, ""global_score"": 62, ""tier"": ""LowCost"" },
    { ""name"": ""Ryanair"", ""icao"": ""RYR"", ""hard_product"": 2, ""soft_product"": 2, ""safety_rec"": 9, ""punctuality"": 9, ""global_score"": 58, ""tier"": ""LowCost"" },
    { ""name"": ""Royal Air Maroc"", ""icao"": ""RAM"", ""hard_product"": 5, ""soft_product"": 5, ""safety_rec"": 7, ""punctuality"": 4, ""global_score"": 53, ""tier"": ""Standard"" },
    { ""name"": ""Spirit Airlines"", ""icao"": ""NKS"", ""hard_product"": 2, ""soft_product"": 2, ""safety_rec"": 8, ""punctuality"": 6, ""global_score"": 48, ""tier"": ""LowCost"" },
    { ""name"": ""Egyptair"", ""icao"": ""MSR"", ""hard_product"": 4, ""soft_product"": 4, ""safety_rec"": 5, ""punctuality"": 4, ""global_score"": 43, ""tier"": ""Standard"" },
    { ""name"": ""Tunisair"", ""icao"": ""TAR"", ""hard_product"": 3, ""soft_product"": 3, ""safety_rec"": 6, ""punctuality"": 1, ""global_score"": 33, ""tier"": ""Struggling"" },
    { ""name"": ""Air Algérie"", ""icao"": ""DAH"", ""hard_product"": 3, ""soft_product"": 3, ""safety_rec"": 5, ""punctuality"": 2, ""global_score"": 32, ""tier"": ""Struggling"" },
    { ""name"": ""Pakistan International"", ""icao"": ""PIA"", ""hard_product"": 3, ""soft_product"": 2, ""safety_rec"": 2, ""punctuality"": 2, ""global_score"": 22, ""tier"": ""Danger"" },
    { ""name"": ""Air Koryo"", ""icao"": ""KOR"", ""hard_product"": 1, ""soft_product"": 1, ""safety_rec"": 1, ""punctuality"": 3, ""global_score"": 15, ""tier"": ""Danger"" }
  ]
}";
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

            icao = icao.ToUpperInvariant();
            if (icao == "EJU" || icao == "EZS") icao = "EZY";

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

        /// <summary>
        /// Renvoie le temps de turnaround (TAT) standard en minutes basé sur le Tier de la compagnie.
        /// </summary>
        public int GetStandardTurnaroundTimeMinutes(string tier)
        {
            switch (tier?.ToLowerInvariant())
            {
                case "elite": return 45; // Refueling, catering complet, nettoyage profond
                case "standard": return 35; // Standard legacy carrier TAT
                case "lowcost": return 25; // TAT optimisé et agressif
                case "struggling":
                case "danger": return 40; // Mauvaise organisation et logistique
                default: return 35;
            }
        }
    }
}
