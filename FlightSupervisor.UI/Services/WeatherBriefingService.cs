using System;
using System.Text;
using System.Text.RegularExpressions;
using FlightSupervisor.UI.Models.SimBrief;

namespace FlightSupervisor.UI.Services
{
    public class WeatherBriefingService
    {
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
                string compSpd = gen.AvgWindComp.Substring(2).TrimStart('0');
                if (string.IsNullOrEmpty(compSpd)) compSpd = "0";
                briefing.AppendLine($"We are expecting an average {compType} of {compSpd} knots during cruise.");
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
                if (speed > 20 || hasGusts) hasBadWeather = true;

                string intensity = speed > 20 ? "strong" : speed > 10 ? "moderate" : "light";
                
                if (dirStr == "VRB")
                {
                    conditions.Append($"We have variable {intensity} winds at {speed} knots. ");
                }
                else if (int.TryParse(dirStr, out int dir))
                {
                    string cardinal = GetCardinalDirection(dir);
                    conditions.Append($"Winds are {intensity}, coming from the {cardinal} at {speed} knots. ");
                }
            }
            else
            {
                conditions.Append("Wind conditions are calm or unavailable. ");
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
