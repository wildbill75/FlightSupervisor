using System;
using System.Collections.Generic;
using System.Linq;
using FlightSupervisor.UI.Models;
using FlightSupervisor.UI.Models.SimBrief;

namespace FlightSupervisor.UI.Services
{
    public class CrewMember
    {
        public string Name { get; set; }
        public string Role { get; set; }
    }

    public class Passenger
    {
        public string Name { get; set; }
        public string Seat { get; set; }
        public string Nationality { get; set; }
        public int Age { get; set; }
    }

    public class ManifestData
    {
        public List<CrewMember> FlightCrew { get; set; } = new List<CrewMember>();
        public List<Passenger> Passengers { get; set; } = new List<Passenger>();
    }

    public class PassengerManifestService
    {
        private Random _rnd = new Random();

        public ManifestData GenerateManifest(SimBriefResponse plan, PilotProfile profile = null, string purserGender = null, List<CrewMember> activeCrew = null)
        {
            var manifest = new ManifestData();

            int paxCount = 0;
            if (!string.IsNullOrEmpty(plan?.Weights?.PaxCount))
                int.TryParse(plan.Weights.PaxCount, out paxCount);

            int maxPax = paxCount;
            if (!string.IsNullOrEmpty(plan?.Aircraft?.MaxPassengers))
                int.TryParse(plan.Aircraft.MaxPassengers, out maxPax);
            else if (!string.IsNullOrEmpty(plan?.Weights?.MaxPax))
                int.TryParse(plan.Weights.MaxPax, out maxPax);

            if (maxPax <= 0 || maxPax < paxCount) maxPax = paxCount;

            // --- FLIGHT CREW ---
            if (activeCrew != null && activeCrew.Count > 0)
            {
                manifest.FlightCrew = new List<CrewMember>(activeCrew);
            }
            else
            {
                GenerateCrew(manifest, maxPax, plan?.Aircraft?.BaseType ?? plan?.Aircraft?.IcaoCode ?? "", plan?.Origin?.IcaoCode ?? "", plan?.General?.Airline ?? "", profile, purserGender);
            }

            // --- PASSENGERS ---
            if (paxCount > 0)
            {
                GeneratePassengers(manifest, paxCount, maxPax, plan?.Origin?.IcaoCode ?? "", plan?.Destination?.IcaoCode ?? "");
            }

            return manifest;
        }

        private void GenerateCrew(ManifestData manifest, int maxPax, string aircraftType, string originIcao, string airline, PilotProfile profile, string purserGender = null)
        {
            int cabinCrewCount = Math.Max(1, (int)Math.Ceiling(maxPax / 50.0));

            // Hardcoded overrides for common aircraft types
            if (!string.IsNullOrEmpty(aircraftType))
            {
                if (aircraftType.StartsWith("A319") || aircraftType.StartsWith("B737")) cabinCrewCount = Math.Max(cabinCrewCount, 3);
                else if (aircraftType.StartsWith("A321") || aircraftType.StartsWith("B739")) cabinCrewCount = Math.Max(cabinCrewCount, 5);
                else if (aircraftType.StartsWith("A32") || aircraftType.StartsWith("A20N") || aircraftType.StartsWith("B738")) cabinCrewCount = Math.Max(cabinCrewCount, 4);
                else if (aircraftType.StartsWith("A33") || aircraftType.StartsWith("B78") || aircraftType.StartsWith("A34")) cabinCrewCount = Math.Max(cabinCrewCount, 6);
                else if (aircraftType.StartsWith("B77") || aircraftType.StartsWith("A35") || aircraftType.StartsWith("A38") || aircraftType.StartsWith("B74")) cabinCrewCount = Math.Max(cabinCrewCount, 8);
            }

            int totalCrew = cabinCrewCount + 2;
            string crewNat = GetAirlineNationality(airline, originIcao);
            
            // Generate names individually so we can force purser gender
            string captainName = GenerateNames(crewNat, 1)[0];
            string firstOfficerName = GenerateNames(crewNat, 1)[0];
            string purserName = GenerateNames(crewNat, 1, purserGender)[0];

            if (profile != null && !string.IsNullOrWhiteSpace(profile.FirstName))
            {
                captainName = $"{profile.FirstName} {profile.LastName}".Trim();
            }

            manifest.FlightCrew.Add(new CrewMember { Role = "Captain", Name = captainName });
            manifest.FlightCrew.Add(new CrewMember { Role = "First Officer", Name = firstOfficerName });

            if (cabinCrewCount > 0)
                manifest.FlightCrew.Add(new CrewMember { Role = "Purser", Name = purserName });

            for (int i = 3; i < totalCrew; i++)
            {
                manifest.FlightCrew.Add(new CrewMember { Role = "Flight Attendant", Name = GenerateNames(crewNat, 1)[0] });
            }
        }

        private void GeneratePassengers(ManifestData manifest, int paxCount, int maxPax, string originIcao, string destIcao)
        {
            var seats = GenerateSeats(maxPax);
            var mixedNationalities = GetNationalitiesMix(paxCount, originIcao, destIcao);

            for (int i = 0; i < paxCount; i++)
            {
                string nat = mixedNationalities[i];
                string name = GenerateNames(nat, 1)[0];
                string seat = (i < seats.Count) ? seats[i] : "UNASSIGNED";
                
                // Base age distribution: 5% Kids(2-12), 10% Teens(13-19), 65% Adults(20-60), 20% Seniors(61-85)
                int ageRoll = _rnd.Next(100);
                int age = 30; // default
                if (ageRoll < 5) age = _rnd.Next(2, 13);
                else if (ageRoll < 15) age = _rnd.Next(13, 20);
                else if (ageRoll < 80) age = _rnd.Next(20, 61);
                else age = _rnd.Next(61, 86);

                manifest.Passengers.Add(new Passenger
                {
                    Name = name,
                    Seat = seat,
                    Nationality = GetNationalityDisplayName(nat),
                    Age = age
                });
            }

            // Sort passengers by seat roughly
            manifest.Passengers = manifest.Passengers
                .OrderBy(p => {
                    var numPart = new string(p.Seat.Where(char.IsDigit).ToArray());
                    return int.TryParse(numPart, out int num) ? num : 0;
                })
                .ThenBy(p => p.Seat)
                .ToList();
        }

        public List<string> GenerateSeats(int maxPax)
        {
            var seats = new List<string>();
            char[] columns;
            
            // Basic estimation : > 200 is often a widebody (2 aisles)
            if (maxPax > 200)
                columns = new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K' }; // 10 abreast usually
            else
                columns = new[] { 'A', 'B', 'C', 'D', 'E', 'F' }; // 6 abreast (narrowbody)

            int numRows = (int)Math.Ceiling((double)maxPax / columns.Length);
            if (numRows >= 13) numRows++; // Add an extra row to compensate for skipping 13

            for (int r = 1; r <= numRows; r++)
            {
                if (r == 13) continue; // Skip superstition row
                foreach (char c in columns)
                {
                    seats.Add($"{r}{c}");
                }
            }

            // Shuffle the seats so passengers are scattered randomly across the plane
            int n = seats.Count;
            while (n > 1)
            {
                n--;
                int k = _rnd.Next(n + 1);
                var value = seats[k];
                seats[k] = seats[n];
                seats[n] = value;
            }

            return seats;
        }

        private List<string> GetNationalitiesMix(int paxCount, string origin, string dest)
        {
            string origNat = GetRegionFromIcao(origin);
            string destNat = GetRegionFromIcao(dest);
            
            int origCount = (int)(paxCount * 0.45);
            int destCount = (int)(paxCount * 0.45);
            int intCount = paxCount - origCount - destCount;

            var list = new List<string>();
            for (int i = 0; i < origCount; i++) list.Add(origNat);
            for (int i = 0; i < destCount; i++) list.Add(destNat);
            for (int i = 0; i < intCount; i++) list.Add("INT");

            // Shuffle
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rnd.Next(n + 1);
                var v = list[k];
                list[k] = list[n];
                list[n] = v;
            }
            return list;
        }

        public string GetAirlineNationality(string airlineIcao, string departureIcao)
        {
            if (string.IsNullOrEmpty(airlineIcao)) return GetRegionFromIcao(departureIcao);

            switch (airlineIcao.ToUpperInvariant())
            {
                case "AFR": case "TVF": case "CRL": return "FR"; // France
                case "BAW": case "EZY": case "RYR": case "EXS": case "VIR": return "UK"; // UK/Ireland
                case "DLH": case "CFG": case "EWG": return "DE"; // Germany
                case "IBE": case "VLG": case "AEA": return "ES"; // Spain
                case "SWR": case "EZS": return "LS"; // Switzerland
                case "AZA": case "ITY": return "IT"; // Italy
                case "DAL": case "AAL": case "UAL": case "SWA": case "JBU": case "NKS": return "US"; // USA
                case "ACA": case "WJA": case "TSC": return "CA"; // Canada
                case "KLM": case "TRA": case "CND": return "NL"; // Netherlands
                default:
                    return GetRegionFromIcao(departureIcao);
            }
        }

        private string GetRegionFromIcao(string icao)
        {
            if (string.IsNullOrEmpty(icao)) return "INT";
            if (icao.StartsWith("LF")) return "FR";
            if (icao.StartsWith("EG")) return "UK";
            if (icao.StartsWith("K")) return "US";
            if (icao.StartsWith("C")) return "CA";
            if (icao.StartsWith("LE")) return "ES";
            if (icao.StartsWith("ED")) return "DE";
            if (icao.StartsWith("LI")) return "IT";
            if (icao.StartsWith("E") || icao.StartsWith("L")) return "EU";
            return "INT";
        }

        private string GetNationalityDisplayName(string nat)
        {
            return nat switch
            {
                "FR" => "🇫🇷 FRA",
                "UK" => "🇬🇧 GBR",
                "US" => "🇺🇸 USA",
                "CA" => "🇨🇦 CAN",
                "ES" => "🇪🇸 ESP",
                "DE" => "🇩🇪 DEU",
                "IT" => "🇮🇹 ITA",
                "LS" => "🇨🇭 CHE",
                "NL" => "🇳🇱 NLD",
                "EU" => "🇪🇺 EUR",
                _ => "🌐 INT"
            };
        }

        private List<string> GenerateNames(string nationality, int count, string gender = null)
        {
            var (maleFirstNames, femaleFirstNames, lastNames) = GetNameDictionaries(nationality);
            var result = new List<string>();
            for (int i = 0; i < count; i++)
            {
                string[] firstNames;
                if (gender == "Male") firstNames = maleFirstNames;
                else if (gender == "Female") firstNames = femaleFirstNames;
                else firstNames = _rnd.Next(2) == 0 ? maleFirstNames : femaleFirstNames;

                string first = firstNames[_rnd.Next(firstNames.Length)];
                string last = lastNames[_rnd.Next(lastNames.Length)];
                result.Add($"{first} {last}");
            }
            return result;
        }

        private (string[], string[], string[]) GetNameDictionaries(string nat)
        {
            string[] fFR_M = { "Jean", "Pierre", "Antoine", "Julien", "Thomas", "Paul", "Hugo", "Arthur" };
            string[] fFR_F = { "Marie", "Camille", "Sophie", "Lucie", "Chloé", "Léa", "Mathilde" };
            string[] lFR = { "Martin", "Bernard", "Thomas", "Petit", "Richard", "Durand", "Dubois", "Moreau", "Laurent", "Simon", "Michel", "Lefebvre", "Leroy" };

            string[] fUK_M = { "James", "Oliver", "Harry", "George", "Noah", "Jack" };
            string[] fUK_F = { "Amelia", "Olivia", "Isla", "Emily", "Poppy", "Ava", "Isabella", "Jessica", "Lily" };
            string[] lUK = { "Smith", "Jones", "Williams", "Taylor", "Brown", "Davies", "Evans", "Wilson", "Thomas", "Roberts", "Johnson", "Lewis", "Walker" };

            string[] fUS_M = { "John", "Michael", "David", "William", "James" };
            string[] fUS_F = { "Emma", "Olivia", "Ava", "Isabella", "Sophia", "Mia", "Charlotte", "Amelia", "Harper", "Evelyn" };
            string[] lUS = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Garcia", "Rodriguez", "Wilson", "Martinez", "Anderson", "Taylor" };
            
            string[] fES_M = { "Jose", "Antonio", "Juan", "Manuel", "Francisco", "Carlos", "David", "Javier", "Daniel" };
            string[] fES_F = { "Maria", "Carmen", "Ana", "Isabel", "Laura" }; // Fixed cyrillic M to Maria
            string[] lES = { "Garcia", "Gonzalez", "Rodriguez", "Fernandez", "Lopez", "Martinez", "Sanchez", "Perez", "Gomez", "Martin", "Jimenez", "Ruiz" };

            string[] fDE_M = { "Maximilian", "Alexander", "Paul", "Leon", "Louis", "Lukas", "Felix", "David" };
            string[] fDE_F = { "Mia", "Emma", "Hannah", "Sofia", "Anna" };
            string[] lDE = { "Müller", "Schmidt", "Schneider", "Fischer", "Weber", "Meyer", "Wagner", "Becker", "Schulz", "Hoffmann", "Schäfer" };

            string[] fIT_M = { "Francesco", "Alessandro", "Lorenzo", "Leonardo", "Andrea", "Matteo", "Gabriele" };
            string[] fIT_F = { "Sofia", "Aurora", "Giulia", "Ginevra", "Alice" };
            string[] lIT = { "Rossi", "Russo", "Ferrari", "Esposito", "Bianchi", "Romano", "Colombo", "Ricci", "Marino", "Greco", "Bruno", "Gallo" };

            // Default mix
            string[] fINT_M = fFR_M.Concat(fUK_M).Concat(fUS_M).Concat(fES_M).Concat(fDE_M).Concat(fIT_M).ToArray();
            string[] fINT_F = fFR_F.Concat(fUK_F).Concat(fUS_F).Concat(fES_F).Concat(fDE_F).Concat(fIT_F).ToArray();
            string[] lINT = lFR.Concat(lUK).Concat(lUS).Concat(lES).Concat(lDE).Concat(lIT).ToArray();

            return nat switch
            {
                "FR" => (fFR_M, fFR_F, lFR),
                "UK" => (fUK_M, fUK_F, lUK),
                "US" => (fUS_M, fUS_F, lUS),
                "CA" => (fUS_M, fUS_F, lUS), // roughly matching generic anglo/US for ease
                "ES" => (fES_M, fES_F, lES),
                "DE" => (fDE_M, fDE_F, lDE),
                "IT" => (fIT_M, fIT_F, lIT),
                "EU" => (fINT_M, fINT_F, lINT), // General European mix
                _ => (fINT_M, fINT_F, lINT)
            };
        }
    }
}
