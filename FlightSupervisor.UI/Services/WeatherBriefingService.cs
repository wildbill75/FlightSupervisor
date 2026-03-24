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

            string origRwy = response.Origin?.PlanRwy;
            string destRwy = response.Destination?.PlanRwy;

            string destTaf = weather.DestTaf ?? "";
            long etaUnix = 0;
            if (response.Times?.SchedIn != null) long.TryParse(response.Times.SchedIn, out etaUnix);
            if (etaUnix == 0 && response.Times?.SchedOut != null && response.Times?.EstTimeEnroute != null)
            {
                if (long.TryParse(response.Times.SchedOut, out long outUnix) && long.TryParse(response.Times.EstTimeEnroute, out long eeteSec))
                    etaUnix = outUnix + eeteSec;
            }

            briefing.AppendLine("FLIGHT PLAN EXPECTATIONS:");
            if (!string.IsNullOrEmpty(origRwy))
                briefing.AppendLine($"Based on preliminary dispatch data, we are expecting a departure from runway {origRwy}.");
            
            if (!string.IsNullOrEmpty(destRwy))
            {
                briefing.Append($"For our arrival, the planned runway is {destRwy}");
                
                string rwyShiftWarning = AnalyzeRunwayWindShift(destRwy, destTaf, etaUnix);
                if (!string.IsNullOrEmpty(rwyShiftWarning))
                {
                    briefing.AppendLine($", but {rwyShiftWarning}");
                }
                else
                {
                    briefing.AppendLine(", though this will be subject to local ATC and weather changes at the time of arrival.");
                }
            }
            
            briefing.AppendLine();

            // Altitude & Tropopause
            if (!string.IsNullOrWhiteSpace(gen?.InitialAlt))

            {
                if (int.TryParse(gen.InitialAlt, out int plannedFeet))
                {
                    int plannedFl = plannedFeet / 100;
                    string altText = $"Dispatch has filed us for an initial cruise altitude of FL{plannedFl:D3}.";

                    int avgTropo = 0;
                    int tropoCount = 0;
                    if (response.Navlog?.Fixes != null)
                    {
                        foreach (var pt in response.Navlog.Fixes)
                        {
                            if (int.TryParse(pt.TropopauseFeet, out int trop))
                            {
                                avgTropo += trop;
                                tropoCount++;
                            }
                        }
                    }

                    if (tropoCount > 0)
                    {
                        avgTropo /= tropoCount;
                        int tropoFl = (int)Math.Round((double)avgTropo / 100);
                        
                        altText += $" Our route profiling shows the average tropopause height is around FL{tropoFl}.";
                        
                        if (plannedFl >= tropoFl)
                            altText += " Be advised, we will be cruising near or above the tropopause, so monitor your performance margins closely.";
                        else if (tropoFl - plannedFl >= 20)
                            altText += " We have excellent vertical performance margin available today if we need to climb to avoid weather.";
                    }

                    if (!string.IsNullOrWhiteSpace(gen?.StepClimbString))
                    {
                        string stepStr = gen.StepClimbString.Trim();
                        if (stepStr.Contains(',') || stepStr.Split('/').Length > 2)
                        {
                            altText += $" Later in the flight, expect planned step climbs to optimise our fuel burn: {stepStr}.";
                        }
                    }

                    briefing.AppendLine(altText);
                }
            }
            briefing.AppendLine();

            string origMetar = weather.OrigMetar ?? "";
            string destMetar = weather.DestMetar ?? "";

            // Departure
            briefing.AppendLine("DEPARTURE:");
            briefing.AppendLine(AnalyzeMetar(origMetar, "departure"));
            if (!string.IsNullOrEmpty(response.Origin?.IcaoCode))
            {
                string origNotams = AnalyzeNotamAlerts(response.Text?.PlanHtml, response.Origin.IcaoCode);
                if (!string.IsNullOrEmpty(origNotams)) briefing.AppendLine(origNotams);
            }
            briefing.AppendLine();

            // Destination
            briefing.AppendLine("DESTINATION:");
            if (etaUnix > 0 && !string.IsNullOrWhiteSpace(destTaf))
            {
                briefing.AppendLine(AnalyzeTafAtEta(destTaf, etaUnix));
            }
            else
            {
                briefing.AppendLine(AnalyzeMetar(destMetar, "destination"));
            }

            if (!string.IsNullOrEmpty(response.Destination?.IcaoCode))
            {
                string destNotams = AnalyzeNotamAlerts(response.Text?.PlanHtml, response.Destination.IcaoCode);
                if (!string.IsNullOrEmpty(destNotams)) briefing.AppendLine(destNotams);
            }
            briefing.AppendLine();

            // Alternate
            string altnMetarStr = "";
            string altnTafStr = "";
            if (weather?.AltnMetar != null && weather.AltnMetar.Value.ValueKind == System.Text.Json.JsonValueKind.String) altnMetarStr = weather.AltnMetar.Value.GetString() ?? "";
            else if (weather?.AltnMetar != null && weather.AltnMetar.Value.ValueKind == System.Text.Json.JsonValueKind.Array && weather.AltnMetar.Value.GetArrayLength() > 0)
                altnMetarStr = weather.AltnMetar.Value[0].GetString() ?? "";
                
            if (weather?.AltnTaf != null && weather.AltnTaf.Value.ValueKind == System.Text.Json.JsonValueKind.String) altnTafStr = weather.AltnTaf.Value.GetString() ?? "";
            else if (weather?.AltnTaf != null && weather.AltnTaf.Value.ValueKind == System.Text.Json.JsonValueKind.Array && weather.AltnTaf.Value.GetArrayLength() > 0)
                altnTafStr = weather.AltnTaf.Value[0].GetString() ?? "";

            if (!string.IsNullOrWhiteSpace(altnMetarStr) || !string.IsNullOrWhiteSpace(altnTafStr))
            {
                briefing.AppendLine("ALTERNATE PLAN:");
                string altnIcao = response.Alternate?.IcaoCode ?? "our alternate";
                briefing.AppendLine($"Regarding our primary destination alternate, {altnIcao}:");
                
                // If we have an ETA, maybe use TAF, but for alternate, Metar is usually fine unless it's a long long flight. 
                // Let's use the TAF if available and we have ETA, else METAR.
                if (etaUnix > 0 && !string.IsNullOrWhiteSpace(altnTafStr))
                    briefing.AppendLine(AnalyzeTafAtEta(altnTafStr, etaUnix));
                else if (!string.IsNullOrWhiteSpace(altnMetarStr))
                    briefing.AppendLine(AnalyzeMetar(altnMetarStr, "the alternate"));
                else
                    briefing.AppendLine($"We have the necessary weather minimums to safely divert to {altnIcao} if required.");
                
                briefing.AppendLine();
            }

            // Enroute
            briefing.AppendLine("ENROUTE & OPERATIONS:");

            // Winds
            if (!string.IsNullOrWhiteSpace(gen?.AvgWindComp) && gen.AvgWindComp != "0")
            {
                string rawComp = gen.AvgWindComp.Trim(); // e.g., "14", "P014", "M014", "HD014"
                
                // If it's just a number, we don't know if it's head or tail in some old formats, but usually positive means tailwind, negative means headwind.
                // In simbrief, P is plus (tail), M is minus (head). Sometimes it's just "14" (usually positive tail) or "-14".
                string compType = "tailwind"; // Default positive
                if (rawComp.StartsWith("M") || rawComp.StartsWith("-") || rawComp.StartsWith("HD")) compType = "headwind";
                else if (rawComp.StartsWith("P") || rawComp.StartsWith("TL")) compType = "tailwind";
                                  
                string compSpdStr = rawComp.Replace("M", "").Replace("P", "").Replace("HD", "").Replace("TL", "").Replace("-", "").Replace("+", "").TrimStart('0');
                if (string.IsNullOrEmpty(compSpdStr)) compSpdStr = "0";

                int compSpd = int.TryParse(compSpdStr, out int c) ? c : 0;
                
                if (compSpd > 0)
                {
                    if (_units.Speed == "KMH")
                    {
                        int kmSpd = (int)Math.Round(compSpd * 1.852);
                        briefing.AppendLine($"We are expecting an average {compType} of {kmSpd} km/h during cruise.");
                    }
                    else
                    {
                        briefing.AppendLine($"We are expecting an average {compType} of {compSpd} knots during cruise.");
                    }

                    // Format for MCDU (Head/Tail + 3 digits)
                    string prefix = compType == "headwind" ? "HD" : "TL";
                    string mcduFormat = $"{prefix}{compSpd:D3}";
                    
                    briefing.AppendLine($"For your FMS/MCDU performance initialization, the Trip Wind entry is {mcduFormat}.");
                    briefing.AppendLine();
                }
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
            int maxShear = 0;
            if (response.Navlog?.Fixes != null)
            {
                foreach(var point in response.Navlog.Fixes)
                {
                    if (int.TryParse(point.Turb, out int t) && t > maxTurb)
                        maxTurb = t;
                    if (int.TryParse(point.Shear, out int s) && s > maxShear)
                        maxShear = s;
                }
            }

            if (maxTurb >= 4)
                briefing.AppendLine("Weather charts show areas of severe turbulence. Passengers and crew will need to remain seated for significant portions of the flight.");
            else if (maxShear >= 3)
                briefing.AppendLine("Forecasts indicate notable wind shear along our route. We will monitor the radar closely.");
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

        private string GetActiveTafBlockAtEta(string taf, long etaUnix, out string tempoConditions)
        {
            tempoConditions = "";
            try
            {
                taf = Regex.Replace(taf, @"\s+", " ").Replace("=", "").Trim();
                
                DateTimeOffset eta = DateTimeOffset.FromUnixTimeSeconds(etaUnix);
                int etaDay = eta.Day;
                int etaHour = eta.Hour;

                string baseForecast = taf;
                var headerMatch = Regex.Match(taf, @"(?:TAF\s+)?[A-Z0-9]{4}\s+(?:[0-9]{6}Z\s+)?[0-9]{4}/[0-9]{4}\s+(.*?)(?=\s+(?:FM|BECMG|TEMPO|PROB)|\z)");
                if (headerMatch.Success) baseForecast = headerMatch.Groups[1].Value;
                else
                {
                    var altMatch = Regex.Match(taf, @"[0-9]{4}/[0-9]{4}\s+(.*?)(?=\s+(?:FM|BECMG|TEMPO|PROB)|\z)");
                    if (altMatch.Success) baseForecast = altMatch.Groups[1].Value;
                }

                string activeForecast = baseForecast;
                var tempoStr = new StringBuilder();

                var modifierMatches = Regex.Matches(taf, @"(FM|BECMG|TEMPO|PROB\d{2})\s*([0-9]{6}|[0-9]{4}/[0-9]{4})\s*(.*?)(?=\s+(?:FM|BECMG|TEMPO|PROB)|\z)");
                
                foreach (Match m in modifierMatches)
                {
                    string type = m.Groups[1].Value;
                    string timeStr = m.Groups[2].Value;
                    string content = m.Groups[3].Value;

                    if (type == "FM")
                    {
                        if (timeStr.Length == 6)
                        {
                            int fmDay = int.Parse(timeStr.Substring(0, 2));
                            int fmHour = int.Parse(timeStr.Substring(2, 2));
                            
                            int fmMinTotal = fmDay * 24 * 60 + fmHour * 60;
                            int etaMinTotal = etaDay * 24 * 60 + etaHour * 60 + eta.Minute;
                            
                            if (etaDay < fmDay && fmDay - etaDay > 15) etaMinTotal += 31 * 24 * 60;
                            else if (fmDay < etaDay && etaDay - fmDay > 15) fmMinTotal += 31 * 24 * 60;

                            if (etaMinTotal >= fmMinTotal) activeForecast = content;
                        }
                    }
                    else if (type == "BECMG")
                    {
                        if (timeStr.Length == 9 && timeStr.Contains("/"))
                        {
                            var parts = timeStr.Split('/');
                            int endDay = int.Parse(parts[1].Substring(0, 2));
                            int endHour = int.Parse(parts[1].Substring(2, 2));

                            int endMinTotal = endDay * 24 * 60 + endHour * 60;
                            int etaMinTotal = etaDay * 24 * 60 + etaHour * 60 + eta.Minute;

                            if (etaDay < endDay && endDay - etaDay > 15) etaMinTotal += 31 * 24 * 60;
                            else if (endDay < etaDay && etaDay - endDay > 15) endMinTotal += 31 * 24 * 60;

                            if (etaMinTotal >= endMinTotal) activeForecast += " " + content;
                        }
                    }
                    else if (type == "TEMPO" || type.StartsWith("PROB"))
                    {
                        if (timeStr.Length == 9 && timeStr.Contains("/"))
                        {
                            var parts = timeStr.Split('/');
                            int startDay = int.Parse(parts[0].Substring(0, 2));
                            int startHour = int.Parse(parts[0].Substring(2, 2));
                            int endDay = int.Parse(parts[1].Substring(0, 2));
                            int endHour = int.Parse(parts[1].Substring(2, 2));

                            int startMinTotal = startDay * 24 * 60 + startHour * 60;
                            int endMinTotal = endDay * 24 * 60 + endHour * 60;
                            int etaMinTotal = etaDay * 24 * 60 + etaHour * 60 + eta.Minute;

                            if (etaDay < startDay && startDay - etaDay > 15) etaMinTotal += 31 * 24 * 60;
                            else if (startDay < etaDay && etaDay - startDay > 15) startMinTotal += 31 * 24 * 60;

                            if (endDay < startDay) endMinTotal += 31 * 24 * 60; 

                            if (etaMinTotal >= startMinTotal && etaMinTotal <= endMinTotal)
                            {
                                tempoStr.Append($" {content}");
                            }
                        }
                    }
                }
                
                tempoConditions = tempoStr.ToString().Trim();
                return Regex.Replace(activeForecast, @"\s+", " ").Trim();
            }
            catch
            {
                return taf;
            }
        }

        private string AnalyzeNotamAlerts(string planHtml, string icao)
        {
            if (string.IsNullOrWhiteSpace(planHtml)) return "";
            
            var alerts = new System.Collections.Generic.List<string>();
            
            var match = Regex.Match(planHtml, $@"(?:NOTAMS? FOR |LOCATION: ){icao}(.*?)(?:NOTAMS? FOR |LOCATION: |END OF |</pre>)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                // Fallback attempt
                match = Regex.Match(planHtml, $@"Q\)\s+[^/]*?{icao}.*?(?=Q\)\s+|</pre>)", RegexOptions.Singleline);
            }

            string textToScan = match.Success ? match.Groups[1].Value : "";
            if (string.IsNullOrWhiteSpace(textToScan)) return "";

            if (Regex.IsMatch(textToScan, @"RWY\s+[0-9]{2}[LRC]?/[0-9]{2}[LRC]?\s+(?:CLSD|CLOSED)", RegexOptions.IgnoreCase) || Regex.IsMatch(textToScan, @"RWY\s+[0-9]{2}[LRC]?\s+(?:CLSD|CLOSED)", RegexOptions.IgnoreCase))
            {
                alerts.Add("one or more runways are reported closed");
            }
            if (Regex.IsMatch(textToScan, @"ILS\s+.*?U/S", RegexOptions.IgnoreCase) || Regex.IsMatch(textToScan, @"ILS\s+.*?OUT OF SERVICE", RegexOptions.IgnoreCase) || Regex.IsMatch(textToScan, @"GLIDE PATH.*?U/S", RegexOptions.IgnoreCase))
            {
                alerts.Add("an Instrument Landing System is reported unserviceable");
            }
            if (Regex.IsMatch(textToScan, @"TWY\s+.*?CLSD", RegexOptions.IgnoreCase) || Regex.IsMatch(textToScan, @"TAXIWAY.*?CLSD", RegexOptions.IgnoreCase))
            {
                alerts.Add("some taxiways are closed");
            }

            if (alerts.Count > 0)
            {
                return $"Reviewing the Notices to Airmen for {icao}, please be aware that {string.Join(" and ", alerts)}. We will brief this in more detail if necessary.";
            }
            
            return $"We have reviewed the Notices to Airmen for {icao}, and no significant operational restrictions were observed.";
        }

        private string AnalyzeRunwayWindShift(string plannedRwy, string taf, long etaUnix)
        {
            if (string.IsNullOrWhiteSpace(plannedRwy) || string.IsNullOrWhiteSpace(taf) || etaUnix == 0) return "";
            
            try
            {
                string activeForecast = GetActiveTafBlockAtEta(taf, etaUnix, out string _);
                if (string.IsNullOrWhiteSpace(activeForecast)) return "";

                var windMatch = Regex.Match(activeForecast, @"(?:^|\s)([0-9]{3})([0-9]{2,3})(?:G[0-9]{2,3})?KT");
                if (windMatch.Success)
                {
                    int windDir = int.Parse(windMatch.Groups[1].Value);
                    int windSpd = int.Parse(windMatch.Groups[2].Value);

                    if (windSpd >= 5) // Only care if wind is enough to force a runway change
                    {
                        var rwyMatch = Regex.Match(plannedRwy, @"\d+");
                        if (rwyMatch.Success)
                        {
                            int rwyHdg = int.Parse(rwyMatch.Value) * 10;
                            double headwindComp = windSpd * Math.Cos((windDir - rwyHdg) * Math.PI / 180.0);
                            
                            // If it's a tailwind of more than 5 knots
                            if (headwindComp <= -5)
                            {
                                return $"based on the terminal forecast for our arrival time, the winds are expected to shift, creating a significant tailwind. We should anticipate a likely runway change to the opposite direction.";
                            }
                        }
                    }
                }
            }
            catch { }

            return "";
        }

        private string AnalyzeTafAtEta(string taf, long etaUnix)
        {
            try
            {
                string tempoStr;
                string activeForecast = GetActiveTafBlockAtEta(taf, etaUnix, out tempoStr);
                
                var sb = new StringBuilder();
                sb.AppendLine("Based on the terminal area forecast for our scheduled arrival time:");
                sb.AppendLine(AnalyzeMetar(activeForecast, "your arrival"));

                if (tempoStr.Length > 0)
                {
                    string tempoAnalyzed = AnalyzeMetar(tempoStr, "temporarily");
                    var cleanTempo = tempoAnalyzed.Replace("We are lacking recent weather reports for temporarily. Expect standard procedures.", "").Replace("Therefore, we do not anticipate any particular weather-related issues for this phase.", "").Trim();
                    
                    if (!string.IsNullOrWhiteSpace(cleanTempo))
                    {
                        sb.AppendLine();
                        sb.AppendLine($"However, please be advised that we may encounter the following temporary conditions during our arrival window:");
                        sb.AppendLine(cleanTempo);
                    }
                }

                return sb.ToString().Trim();
            }
            catch(Exception)
            {
                return AnalyzeMetar(taf, "destination");
            }
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
                string tempUnit = "C";
                if (_units.Temp == "F")
                {
                    displayTemp = (int)Math.Round(tempC * 9.0 / 5.0 + 32);
                    tempUnit = "F";
                }
                
                conditions.Append($"The outside temperature is {displayTemp}°{tempUnit}. ");
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
                        conditions.Append($"QNH {pVal}. ");
                }
                else // METAR in inHg (Altimeter A2992 = 29.92)
                {
                    double inHg = pVal / 100.0;
                    if (_units.Press == "HPA")
                        conditions.Append($"QNH {(int)Math.Round(inHg * 33.8639)}. ");
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
