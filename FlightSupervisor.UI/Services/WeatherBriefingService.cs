using System;
using System.Text;
using System.Text.RegularExpressions;
using FlightSupervisor.UI.Models;
using FlightSupervisor.UI.Models.SimBrief;

namespace FlightSupervisor.UI.Services
{
    public class WeatherBriefingService
    {
        private readonly UnitPreferences _units;

        public WeatherBriefingService(UnitPreferences units = null)
        {
            _units = units ?? new UnitPreferences();
        }

        public string GenerateBriefing(SimBriefResponse? response)
        {
            if (response == null) return "No flight data available for briefing.";
            var weather = response.Weather;
            var gen = response.General;
            if (weather == null) return "No weather data available for briefing.";

            var briefing = new StringBuilder();
            briefing.AppendLine("Ladies and gentlemen of the flight deck crew, here is our briefing for today.");
            briefing.AppendLine();

            string origMetar = weather.OrigMetar ?? "";
            string destMetar = weather.DestMetar ?? "";

            // Departure
            briefing.AppendLine("DEPARTURE:");
            briefing.AppendLine(AnalyzeMetar(origMetar, "departure"));
            briefing.AppendLine();

            // Destination
            briefing.AppendLine("DESTINATION:");
            briefing.AppendLine(AnalyzeMetar(destMetar, "destination"));
            briefing.AppendLine();

            // Enroute
            briefing.AppendLine("ENROUTE & OPERATIONS:");

            // Winds
            if (!string.IsNullOrWhiteSpace(gen?.AvgWindComp) && gen.AvgWindComp.Length >= 3)
            {
                string compType = gen.AvgWindComp.StartsWith("HD") ? "headwind" : gen.AvgWindComp.StartsWith("TL") ? "tailwind" : "crosswind";
                string compSpdStr = gen.AvgWindComp.Substring(2).TrimStart('0');
                if (string.IsNullOrEmpty(compSpdStr)) compSpdStr = "0";

                int compSpd = int.TryParse(compSpdStr, out int c) ? c : 0;
                string spdUnit = "knots";
                if (_units.Speed == "KMH")
                {
                    compSpd = (int)Math.Round(compSpd * 1.852);
                    spdUnit = "km/h";
                }

                briefing.AppendLine($"We are expecting an average {compType} of {compSpd} {spdUnit} during cruise.");
            }

            // ETOPS / Oceanic
            bool isOceanic = gen?.Etops == "1" || (gen?.Route != null && (gen.Route.Contains(" NAT") || gen.Route.Contains(" ETP")));
            if (isOceanic)
            {
                briefing.AppendLine("We are operating this flight under ETOPS regulations for our oceanic crossing. We've verified our Equal-Time Point (ETP) alternates, and weather minimums currently remain well within legal limits for a safe diversion if necessary.");
            }
            else
            {
                briefing.AppendLine("We will be closely monitoring our enroute alternates along the flight path for any required diversions.");
            }

            int maxTurb = 0;
            if (response.Navlog?.Fixes != null)
            {
                foreach(var point in response.Navlog.Fixes)
                {
                    if (int.TryParse(point.Turb, out int t) && t > maxTurb)
                        maxTurb = t;
                }
            }

            if (maxTurb >= 4)
                briefing.AppendLine("Weather charts show areas of severe turbulence. Passengers and crew will need to remain seated for significant portions of the flight.");
            else if (maxTurb >= 2)
                briefing.AppendLine("We might encounter occasional light to moderate turbulence, but overall conditions are acceptable.");
            else
                briefing.AppendLine("Enroute winds are stable, expecting a very smooth ride today.");
            briefing.AppendLine();

            briefing.AppendLine("Please secure the cabin whenever the seatbelt sign is illuminated. Let's have a great flight.");

            return briefing.ToString();
        }

        public string GenerateSandboxBriefing(string metar, string taf)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(metar))
            {
                sb.AppendLine("CURRENT CONDITIONS:");
                sb.AppendLine(AnalyzeMetar(metar, "the local area"));
                sb.AppendLine();
            }
            if (!string.IsNullOrWhiteSpace(taf))
            {
                sb.AppendLine("FORECAST (TAF):");
                sb.AppendLine(AnalyzeMetar(taf, "the forecast"));
            }
            return sb.ToString().TrimEnd();
        }

        private string AnalyzeMetar(string metar, string phase)
        {
            if (string.IsNullOrWhiteSpace(metar)) return $"We are lacking recent weather reports for {phase}. Expect standard procedures.";

            var conditions = new StringBuilder();
            var upperMetar = metar.ToUpperInvariant();
            bool hasBadWeather = false;

            // Parse Wind
            var windMatch = Regex.Match(upperMetar, @"(VRB|[0-9]{3})([0-9]{2,3})(G[0-9]{2,3})?KT");
            if (windMatch.Success)
            {
                string dirStr = windMatch.Groups[1].Value;
                string spdStr = windMatch.Groups[2].Value;
                bool hasGusts = windMatch.Groups[3].Success;
                
                int speed = int.TryParse(spdStr, out int s) ? s : 0;
                
                string speedUnit = "knots";
                int displaySpeed = speed;
                int thresholdStrong = 20;
                int thresholdMod = 10;

                if (_units.Speed == "KMH")
                {
                    displaySpeed = (int)Math.Round(speed * 1.852);
                    speedUnit = "km/h";
                    thresholdStrong = 37;
                    thresholdMod = 18;
                }

                if (displaySpeed > thresholdStrong || hasGusts) hasBadWeather = true;
                string intensity = displaySpeed > thresholdStrong ? "strong" : displaySpeed > thresholdMod ? "moderate" : "light";
                
                if (dirStr == "VRB")
                {
                    conditions.Append($"We have variable {intensity} winds at {displaySpeed} {speedUnit}. ");
                }
                else if (int.TryParse(dirStr, out int dir))
                {
                    string cardinal = GetCardinalDirection(dir);
                    conditions.Append($"Winds are {intensity}, coming from the {cardinal} at {displaySpeed} {speedUnit}. ");
                }
            }
            else
            {
                conditions.Append("Wind conditions are calm or unavailable. ");
            }

            // Parse Temp
            var tempMatch = Regex.Match(upperMetar, @"\s(M?[0-9]{2})/(M?[0-9]{2})\s");
            if (tempMatch.Success)
            {
                string tempStr = tempMatch.Groups[1].Value;
                int tempC = int.Parse(tempStr.Replace("M", "-"));
                
                int displayTemp = tempC;
                string tempUnit = "Celsius";
                if (_units.Temp == "F")
                {
                    displayTemp = (int)Math.Round(tempC * 9.0 / 5.0 + 32);
                    tempUnit = "Fahrenheit";
                }
                
                conditions.Append($"The outside temperature is {displayTemp} degrees {tempUnit}. ");
                if (tempC <= 3 && (upperMetar.Contains(" BR") || upperMetar.Contains(" FG") || upperMetar.Contains(" SN")))
                {
                    conditions.Append("Icing conditions are possible, anti-ice systems might be required. ");
                    hasBadWeather = true;
                }
            }

            // Parse QNH
            var qnhMatch = Regex.Match(upperMetar, @"\s(Q|A)([0-9]{4})\s");
            if (qnhMatch.Success)
            {
                string pType = qnhMatch.Groups[1].Value;
                int pVal = int.Parse(qnhMatch.Groups[2].Value);
                
                if (pType == "Q") // METAR in hPa
                {
                    if (_units.Press == "INHG")
                        conditions.Append($"Altimeter setting is {(pVal * 0.0295300):F2} inHg. ");
                    else
                        conditions.Append($"QNH is {pVal} hectopascals. ");
                }
                else // METAR in inHg (Altimeter A2992 = 29.92)
                {
                    double inHg = pVal / 100.0;
                    if (_units.Press == "HPA")
                        conditions.Append($"QNH is {(int)Math.Round(inHg * 33.8639)} hectopascals. ");
                    else
                        conditions.Append($"Altimeter setting is {inHg:F2} inHg. ");
                }
            }

            // Visibility
            if (upperMetar.Contains("CAVOK"))
            {
                conditions.Append("Visibility is excellent (CAVOK). ");
            }
            else
            {
                var visMatch = Regex.Match(upperMetar, @"\s([0-9]{4})\s");
                if (visMatch.Success && int.TryParse(visMatch.Groups[1].Value, out int visMeters))
                {
                    if (visMeters < 1000) { conditions.Append("Visibility is extremely low (less than 1km). "); hasBadWeather = true; }
                    else if (visMeters < 5000) { conditions.Append("Visibility is reduced. "); }
                    else conditions.Append("Visibility is generally good (over 5km). ");
                }
            }

            // Weather phenomena
            if (upperMetar.Contains(" TS") || upperMetar.Contains("TSRA") || upperMetar.Contains("VCTS"))
            {
                conditions.Append("Thunderstorms are reported in the vicinity. ");
                hasBadWeather = true;
            }
            if (upperMetar.Contains(" -RA") || Regex.IsMatch(upperMetar, @"\sRA\s") || upperMetar.Contains(" +RA"))
            {
                conditions.Append(upperMetar.Contains(" +RA") ? "Heavy rain is expected. " : "Expect some light to moderate rain. ");
                hasBadWeather = true;
            }
            if (upperMetar.Contains(" SN"))
            {
                conditions.Append("Snow is reported, de-icing might be required. ");
                hasBadWeather = true;
            }
            if (upperMetar.Contains(" FG") || upperMetar.Contains(" BR") || upperMetar.Contains("LIFR") || upperMetar.Contains("VV00") || upperMetar.Contains(" R0"))
            {
                conditions.Append("Fog or mist is reducing visibility, be prepared for low visibility operations. ");
                hasBadWeather = true;
            }
            if (upperMetar.Contains(" CB"))
            {
                conditions.Append("Cumulonimbus clouds are present, potential for heavy turbulence. ");
                hasBadWeather = true;
            }
            if (upperMetar.Contains(" WS"))
            {
                conditions.Append("Wind shear is reported on the runways. ");
                hasBadWeather = true;
            }

            if (!hasBadWeather && !upperMetar.Contains("CAVOK"))
            {
                conditions.Append("No significant adverse weather phenomena reported. ");
            }

            // Conclusion
            if (hasBadWeather)
            {
                conditions.Append("In conclusion, expect some challenging conditions and heightened vigilance.");
            }
            else
            {
                conditions.Append("Therefore, we do not anticipate any particular weather-related issues for this phase.");
            }

            return conditions.ToString().Trim();
        }

        private string GetCardinalDirection(int degrees)
        {
            string[] directions = { "North", "North-East", "East", "South-East", "South", "South-West", "West", "North-West" };
            int index = (int)Math.Round(((double)degrees % 360) / 45);
            if (index == 8) index = 0;
            return directions[index];
        }
    }
}
