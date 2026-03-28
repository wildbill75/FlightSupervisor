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

        public WeatherBriefingService(UnitPreferences? units = null)
        {
            _units = units ?? new UnitPreferences();
        }

        public BriefingData GenerateBriefing(SimBriefResponse? response)
        {
            var data = new BriefingData();
            if (response == null) 
            {
                data.HeaderText = LocalizationService.Translate("No flight data available for briefing.", "Aucune donnée de vol disponible pour le briefing.");
                return data;
            }
            var weather = response.Weather;
            var gen = response.General;
            if (weather == null) 
            {
                data.HeaderText = LocalizationService.Translate("No weather data available for briefing.", "Aucune donnée météo disponible pour le briefing.");
                return data;
            }

            var header = new StringBuilder();
            header.AppendLine(LocalizationService.Translate("Ladies and gentlemen of the flight deck crew, here is our briefing for today.", "Mesdames et messieurs l'équipage, voici notre briefing pour aujourd'hui."));
            header.AppendLine();

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

            var briefing = new StringBuilder();
            briefing.AppendLine(LocalizationService.Translate("FLIGHT PLAN EXPECTATIONS:", "APERÇU DU VOL :"));
            if (!string.IsNullOrEmpty(origRwy))
                briefing.AppendLine(LocalizationService.Translate($"Based on preliminary dispatch data, we are expecting a departure from runway {origRwy}.", $"Selon le dispatch, nous prévoyons un départ de la piste {origRwy}."));
            
            if (!string.IsNullOrEmpty(destRwy))
            {
                briefing.Append(LocalizationService.Translate($"For our arrival, the planned runway is {destRwy}", $"Pour notre arrivée, la piste prévue est la {destRwy}"));
                
                string rwyShiftWarning = AnalyzeRunwayWindShift(destRwy, destTaf, etaUnix);
                if (!string.IsNullOrEmpty(rwyShiftWarning))
                {
                    briefing.AppendLine(LocalizationService.Translate($", but {rwyShiftWarning}", $", mais {rwyShiftWarning}"));
                }
                else
                {
                    briefing.AppendLine(LocalizationService.Translate(", though this will be subject to local ATC and weather changes at the time of arrival.", ", bien que cela soit soumis à l'ATC et à la météo du moment."));
                }
            }
            
            briefing.AppendLine();

            // Altitude & Tropopause
            if (!string.IsNullOrWhiteSpace(gen?.InitialAlt))
            {
                if (int.TryParse(gen.InitialAlt, out int plannedFeet))
                {
                    int plannedFl = plannedFeet / 100;
                    string altTextEn = $"Dispatch has filed us for an initial cruise altitude of FL{plannedFl:D3}.";
                    string altTextFr = $"Le dispatch a prévu une altitude de croisière initiale au niveau FL{plannedFl:D3}.";

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
                        
                        altTextEn += $" Our route profiling shows the average tropopause height is around FL{tropoFl}.";
                        altTextFr += $" Le profil de notre route indique une tropopause moyenne vers le niveau FL{tropoFl}.";
                        
                        if (plannedFl >= tropoFl) {
                            altTextEn += " Be advised, we will be cruising near or above the tropopause, so monitor your performance margins closely.";
                            altTextFr += " Attention, nous volerons près ou au-dessus de la tropopause, surveillez bien vos marges de performance.";
                        }
                        else if (tropoFl - plannedFl >= 20) {
                            altTextEn += " We have excellent vertical performance margin available today if we need to climb to avoid weather.";
                            altTextFr += " Nous disposons d'une excellente marge de montée aujourd'hui si nous devons éviter la météo.";
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(gen?.StepClimbString))
                    {
                        string stepStr = gen.StepClimbString.Trim();
                        if (stepStr.Contains(',') || stepStr.Split('/').Length > 2)
                        {
                            altTextEn += $" Later in the flight, expect planned step climbs to optimise our fuel burn: {stepStr}.";
                            altTextFr += $" Plus tard dans le vol, des montées par paliers sont prévues pour optimiser le carburant : {stepStr}.";
                        }
                    }

                    header.AppendLine(LocalizationService.Translate(altTextEn, altTextFr));
                }
            }
            
            data.HeaderText = header.ToString().Trim();

            string origMetar = weather.OrigMetar ?? "";
            string destMetar = weather.DestMetar ?? "";

            // Departure Station
            var depStation = new BriefingStation { Id = "departure", Label = LocalizationService.Translate("DEPARTURE:", "DÉPART :"), RawMetar = origMetar };
            depStation.Commentary = AnalyzeMetar(origMetar, LocalizationService.Translate("departure", "le départ"), depStation);
            if (!string.IsNullOrEmpty(response.Origin?.IcaoCode))
            {
                depStation.Icao = response.Origin.IcaoCode;
                depStation.Notams = AnalyzeNotamAlerts(response.Text?.PlanHtml, response.Origin.IcaoCode);
            }
            if (!string.IsNullOrEmpty(origRwy)) depStation.RunwayAdvice = LocalizationService.Translate($"Exp. Runway {origRwy}", $"Piste prévue {origRwy}");
            data.Stations.Add(depStation);

            // Destination Station
            var destStation = new BriefingStation { Id = "destination", Label = LocalizationService.Translate("DESTINATION:", "DESTINATION :"), RawMetar = destMetar, RawTaf = destTaf };
            if (etaUnix > 0 && !string.IsNullOrWhiteSpace(destTaf))
                destStation.Commentary = AnalyzeTafAtEta(destTaf, etaUnix, destStation);
            else
                destStation.Commentary = AnalyzeMetar(destMetar, LocalizationService.Translate("destination", "la destination"), destStation);

            if (!string.IsNullOrEmpty(response.Destination?.IcaoCode))
            {
                destStation.Icao = response.Destination.IcaoCode;
                destStation.Notams = AnalyzeNotamAlerts(response.Text?.PlanHtml, response.Destination.IcaoCode);
            }
            
            if (!string.IsNullOrEmpty(destRwy))
            {
                string rwyShiftWarning = AnalyzeRunwayWindShift(destRwy, destTaf, etaUnix);
                destStation.RunwayAdvice = LocalizationService.Translate($"Exp. Runway {destRwy}", $"Piste prévue {destRwy}");
                if (!string.IsNullOrEmpty(rwyShiftWarning))
                {
                    destStation.RunwayAdvice += LocalizationService.Translate($" (Warning: {rwyShiftWarning})", $" (Alerte: {rwyShiftWarning})");
                }
            }
            data.Stations.Add(destStation);

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
                string altnIcao = response.Alternate?.IcaoCode ?? LocalizationService.Translate("our alternate", "notre aéroport de dégagement");
                
                var altnStation = new BriefingStation { Id = "alternate", Label = LocalizationService.Translate("ALTERNATE PLAN:", "PLAN DE DÉGAGEMENT :"), RawMetar = altnMetarStr, RawTaf = altnTafStr, Icao = altnIcao };
                
                var altComm = new StringBuilder();
                altComm.AppendLine(LocalizationService.Translate($"Regarding our primary destination alternate, {altnIcao}:", $"Concernant notre dégagement principal, {altnIcao} :"));
                
                if (etaUnix > 0 && !string.IsNullOrWhiteSpace(altnTafStr))
                    altComm.AppendLine(AnalyzeTafAtEta(altnTafStr, etaUnix, altnStation));
                else if (!string.IsNullOrWhiteSpace(altnMetarStr))
                    altComm.AppendLine(AnalyzeMetar(altnMetarStr, LocalizationService.Translate("the alternate", "le dégagement"), altnStation));
                else
                    altComm.AppendLine(LocalizationService.Translate($"We have the necessary weather minimums to safely divert to {altnIcao} if required.", $"Nous avons les minimums météorologiques requis pour nous dérouter vers {altnIcao} en toute sécurité si besoin."));
                
                altnStation.Commentary = altComm.ToString().Trim();
                data.Stations.Add(altnStation);
            }

            // Enroute
            var enroute = new StringBuilder();
            enroute.AppendLine(LocalizationService.Translate("ENROUTE & OPERATIONS:", "EN ROUTE & OPÉRATIONS :"));

            // Winds
            if (!string.IsNullOrWhiteSpace(gen?.AvgWindComp) && gen.AvgWindComp != "0")
            {
                string rawComp = gen.AvgWindComp.Trim(); // e.g., "14", "P014", "M014", "HD014"
                
                // If it's just a number, we don't know if it's head or tail in some old formats, but usually positive means tailwind, negative means headwind.
                // In simbrief, P is plus (tail), M is minus (head). Sometimes it's just "14" (usually positive tail) or "-14".
                string compTypeEn = "tailwind"; // Default positive
                string compTypeFr = "vent arrière";
                if (rawComp.StartsWith("M") || rawComp.StartsWith("-") || rawComp.StartsWith("HD")) { compTypeEn = "headwind"; compTypeFr = "vent de face"; }
                else if (rawComp.StartsWith("P") || rawComp.StartsWith("TL")) { compTypeEn = "tailwind"; compTypeFr = "vent arrière"; }
                                  
                string compSpdStr = rawComp.Replace("M", "").Replace("P", "").Replace("HD", "").Replace("TL", "").Replace("-", "").Replace("+", "").TrimStart('0');
                if (string.IsNullOrEmpty(compSpdStr)) compSpdStr = "0";

                int compSpd = int.TryParse(compSpdStr, out int c) ? c : 0;
                
                if (compSpd > 0)
                {
                    if (_units.Speed == "KMH")
                    {
                        int kmSpd = (int)Math.Round(compSpd * 1.852);
                        briefing.AppendLine(LocalizationService.Translate($"We are expecting an average {compTypeEn} of {kmSpd} km/h during cruise.", $"Nous prévoyons un {compTypeFr} moyen de {kmSpd} km/h en croisière."));
                    }
                    else
                    {
                        briefing.AppendLine(LocalizationService.Translate($"We are expecting an average {compTypeEn} of {compSpd} knots during cruise.", $"Nous prévoyons un {compTypeFr} moyen de {compSpd} nœuds en croisière."));
                    }

                    // Format for MCDU (Head/Tail + 3 digits)
                    string prefix = compTypeEn == "headwind" ? "HD" : "TL";
                    string mcduFormat = $"{prefix}{compSpd:D3}";
                    
                    briefing.AppendLine(LocalizationService.Translate($"For your FMS/MCDU performance initialization, the Trip Wind entry is {mcduFormat}.", $"Pour l'initialisation de vos performances FMS/MCDU, l'entrée Vent est {mcduFormat}."));
                    briefing.AppendLine();
                }
            }

            // ETOPS / Oceanic
            bool isOceanic = gen?.Etops == "1" || (gen?.Route != null && (gen.Route.Contains(" NAT") || gen.Route.Contains(" ETP")));
            if (isOceanic)
            {
                briefing.AppendLine(LocalizationService.Translate("We are operating this flight under ETOPS regulations for our oceanic crossing. We've verified our Equal-Time Point (ETP) alternates, and weather minimums currently remain well within legal limits for a safe diversion if necessary.", "Ce vol est opéré sous la réglementation ETOPS pour notre traversée océanique. Nous avons vérifié nos aéroports de dégagement ETP, et la météo reste dans les marges légales pour un déroutement sûr si besoin."));
            }
            else
            {
                briefing.AppendLine(LocalizationService.Translate("We will be closely monitoring our enroute alternates along the flight path for any required diversions.", "Nous surveillerons étroitement nos aéroports de dégagement en route pour tout besoin éventuel de déroutement."));
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
                enroute.AppendLine(LocalizationService.Translate("Weather charts show areas of severe turbulence. Passengers and crew will need to remain seated for significant portions of the flight.", "Les cartes météo montrent des zones de turbulences sévères. Les passagers et l'équipage devront rester attachés pendant une grande partie du vol."));
            else if (maxShear >= 3)
                enroute.AppendLine(LocalizationService.Translate("Forecasts indicate notable wind shear along our route. We will monitor the radar closely.", "Les prévisions indiquent un cisaillement de vent notable sur notre trajet. Nous garderons un œil attentif sur le radar."));
            else if (maxTurb >= 2)
                enroute.AppendLine(LocalizationService.Translate("We might encounter occasional light to moderate turbulence, but overall conditions are acceptable.", "Nous pourrions rencontrer quelques turbulences légères à modérées, mais les conditions globales sont correctes."));
            else
                enroute.AppendLine(LocalizationService.Translate("Enroute winds are stable, expecting a very smooth ride today.", "Les vents en route sont stables, nous nous attendons à un vol très calme aujourd'hui."));
            enroute.AppendLine();

            enroute.AppendLine(LocalizationService.Translate("Please secure the cabin whenever the seatbelt sign is illuminated. Let's have a great flight.", "Veuillez vérifier la cabine lorsque la consigne ceintures est allumée. Bon vol à tous."));

            data.EnrouteText = enroute.ToString().Trim();
            
            var lines = briefing.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines) data.OralCommentary.Add(line.Trim());

            return data;
        }

        public string GenerateSandboxBriefing(string metar, string taf)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(metar))
            {
                sb.AppendLine(LocalizationService.Translate("CURRENT CONDITIONS:", "CONDITIONS ACTUELLES :"));
                sb.AppendLine(AnalyzeMetar(metar, LocalizationService.Translate("the local area", "la zone locale")));
                sb.AppendLine();
            }
            if (!string.IsNullOrWhiteSpace(taf))
            {
                sb.AppendLine(LocalizationService.Translate("FORECAST (TAF):", "PRÉVISONS (TAF) :"));
                sb.AppendLine(AnalyzeMetar(taf, LocalizationService.Translate("the forecast", "les prévisions")));
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
            
            var alertsEn = new System.Collections.Generic.List<string>();
            var alertsFr = new System.Collections.Generic.List<string>();
            
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
                alertsEn.Add("one or more runways are reported closed");
                alertsFr.Add("une ou plusieurs pistes sont fermées");
            }
            if (Regex.IsMatch(textToScan, @"ILS\s+.*?U/S", RegexOptions.IgnoreCase) || Regex.IsMatch(textToScan, @"ILS\s+.*?OUT OF SERVICE", RegexOptions.IgnoreCase) || Regex.IsMatch(textToScan, @"GLIDE PATH.*?U/S", RegexOptions.IgnoreCase))
            {
                alertsEn.Add("an Instrument Landing System is reported unserviceable");
                alertsFr.Add("un système d'atterrissage aux instruments est inopérant");
            }
            if (Regex.IsMatch(textToScan, @"TWY\s+.*?CLSD", RegexOptions.IgnoreCase) || Regex.IsMatch(textToScan, @"TAXIWAY.*?CLSD", RegexOptions.IgnoreCase))
            {
                alertsEn.Add("some taxiways are closed");
                alertsFr.Add("certains taxiways sont fermés");
            }

            if (alertsEn.Count > 0)
            {
                string jointEn = string.Join(" and ", alertsEn);
                string jointFr = string.Join(" et ", alertsFr);
                return LocalizationService.Translate(
                    $"Reviewing the Notices to Airmen for {icao}, please be aware that {jointEn}. We will brief this in more detail if necessary.",
                    $"À la lecture des NOTAM de {icao}, notez que {jointFr}. Nous approfondirons cela si nécessaire."
                );
            }
            
            return LocalizationService.Translate(
                $"We have reviewed the Notices to Airmen for {icao}, and no significant operational restrictions were observed.",
                $"Nous avons vérifié les NOTAM de {icao}, et aucune restriction opérationnelle majeure n'a été relevée."
            );
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
                                return LocalizationService.Translate(
                                    $"based on the terminal forecast for our arrival time, the winds are expected to shift, creating a significant tailwind. We should anticipate a likely runway change to the opposite direction.",
                                    $"selon les prévisions à notre arrivée, le vent devrait tourner, créant un fort vent arrière. Attendez-vous à un probable changement de piste dans la direction opposée."
                                );
                            }
                        }
                    }
                }
            }
            catch { }

            return "";
        }

        private string AnalyzeTafAtEta(string taf, long etaUnix, BriefingStation station = null)
        {
            try
            {
                string tempoStr;
                string activeForecast = GetActiveTafBlockAtEta(taf, etaUnix, out tempoStr);
                
                var sb = new StringBuilder();
                sb.AppendLine(LocalizationService.Translate("Based on the terminal area forecast for our scheduled arrival time:", "D'après les prévisions pour notre heure d'arrivée prévue :"));
                sb.AppendLine(AnalyzeMetar(activeForecast, LocalizationService.Translate("your arrival", "votre arrivée"), station));

                if (tempoStr.Length > 0)
                {
                    string tempoAnalyzed = AnalyzeMetar(tempoStr, LocalizationService.Translate("temporarily", "temporairement"), null);
                    var cleanTempo = tempoAnalyzed.Replace("We are lacking recent weather reports for temporarily. Expect standard procedures.", "")
                                                .Replace("Therefore, we do not anticipate any particular weather-related issues for this phase.", "")
                                                .Replace("Nous manquons de bulletins météo récents pour temporairement. Attendez-vous aux procédures standards.", "")
                                                .Replace("Par conséquent, nous n'anticipons aucun problème météorologique particulier pour cette phase.", "")
                                                .Trim();
                    
                    if (!string.IsNullOrWhiteSpace(cleanTempo))
                    {
                        sb.AppendLine();
                        sb.AppendLine(LocalizationService.Translate("However, please be advised that we may encounter the following temporary conditions during our arrival window:", "Cependant, notez que nous pourrions rencontrer les conditions temporaires suivantes durant notre fenêtre d'arrivée :"));
                        sb.AppendLine(cleanTempo);
                    }
                }

                return sb.ToString().Trim();
            }
            catch(Exception)
            {
                return AnalyzeMetar(taf, LocalizationService.Translate("destination", "la destination"), station);
            }
        }

        private string AnalyzeMetar(string metar, string phase, BriefingStation station = null)
        {
            if (string.IsNullOrWhiteSpace(metar)) return LocalizationService.Translate($"We are lacking recent weather reports for {phase}. Expect standard procedures.", $"Nous manquons de bulletins météo récents pour {phase}. Attendez-vous aux procédures standards.");

            var conditions = new StringBuilder();
            var upperMetar = metar.ToUpperInvariant();
            bool hasBadWeather = false;

            // Parse Wind
            var windMatch = Regex.Match(upperMetar, @"(VRB|[0-9]{3})([0-9]{2,3})(G[0-9]{2,3})?KT");
            if (windMatch.Success)
            {
                if (station != null) station.Wind = windMatch.Value;
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
                
                // Severity calculation
                if (station != null)
                {
                    if (speed > 35 || (hasGusts && speed > 25)) station.WindSeverity = WeatherSeverity.Danger;
                    else if (speed > 25 || hasGusts) station.WindSeverity = WeatherSeverity.Warning;
                    else station.WindSeverity = WeatherSeverity.Normal;
                }

                string intensityEn = displaySpeed > thresholdStrong ? "strong" : displaySpeed > thresholdMod ? "moderate" : "light";
                string intensityFr = displaySpeed > thresholdStrong ? "fort" : displaySpeed > thresholdMod ? "modéré" : "léger";
                
                if (dirStr == "VRB")
                {
                    conditions.Append(LocalizationService.Translate($"We have variable {intensityEn} winds at {displaySpeed} {speedUnit}. ", $"Nous avons un vent variable {intensityFr} à {displaySpeed} {speedUnit}. "));
                }
                else if (int.TryParse(dirStr, out int dir))
                {
                    string cardinal = GetCardinalDirection(dir);
                    conditions.Append(cardinal);
                }
            }
            else
            {
                if (station != null) station.Wind = "CALM";
                conditions.Append(LocalizationService.Translate("Wind conditions are calm or unavailable. ", "Le vent est calme ou les données sont indisponibles. "));
            }

            // Parse Temp
            var tempMatch = Regex.Match(upperMetar, @"\s(M?[0-9]{2})/(M?[0-9]{2})\s");
            if (tempMatch.Success)
            {
                if (station != null) station.TempDew = $"{tempMatch.Groups[1].Value.Replace("M","-")} / {tempMatch.Groups[2].Value.Replace("M","-")}";
                string tempStr = tempMatch.Groups[1].Value;
                int tempC = int.Parse(tempStr.Replace("M", "-"));
                
                int displayTemp = tempC;
                string tempUnit = "C";
                if (_units.Temp == "F")
                {
                    displayTemp = (int)Math.Round(tempC * 9.0 / 5.0 + 32);
                    tempUnit = "F";
                }
                
                conditions.Append(LocalizationService.Translate($"The outside temperature is {displayTemp}°{tempUnit}. ", $"La température extérieure est de {displayTemp}°{tempUnit}. "));
                if (tempC <= 3 && (upperMetar.Contains(" BR") || upperMetar.Contains(" FG") || upperMetar.Contains(" SN")))
                {
                    conditions.Append(LocalizationService.Translate("Icing conditions are possible, anti-ice systems might be required. ", "Des conditions givrantes sont possibles, nos systèmes d'anti-givrage pourraient être nécessaires. "));
                    hasBadWeather = true;
                }
            }

            // Parse QNH
            var qnhMatch = Regex.Match(upperMetar, @"\s(Q|A)([0-9]{4})\s");
            if (qnhMatch.Success)
            {
                if (station != null) station.Qnh = $"{qnhMatch.Groups[1].Value}{qnhMatch.Groups[2].Value}";
                string pType = qnhMatch.Groups[1].Value;
                int pVal = int.Parse(qnhMatch.Groups[2].Value);
                
                if (pType == "Q") // METAR in hPa
                {
                    if (_units.Press == "INHG")
                        conditions.Append(LocalizationService.Translate($"Altimeter setting is {(pVal * 0.0295300):F2} inHg. ", $"Le calage altimétrique est de {(pVal * 0.0295300):F2} inHg. "));
                    else
                        conditions.Append($"QNH {pVal}. ");
                }
                else // METAR in inHg (Altimeter A2992 = 29.92)
                {
                    double inHg = pVal / 100.0;
                    if (_units.Press == "HPA")
                        conditions.Append($"QNH {(int)Math.Round(inHg * 33.8639)}. ");
                    else
                        conditions.Append(LocalizationService.Translate($"Altimeter setting is {inHg:F2} inHg. ", $"Le calage altimétrique est de {inHg:F2} inHg. "));
                }
            }

            // Visibility
            if (upperMetar.Contains("CAVOK"))
            {
                if (station != null) station.Visibility = "CAVOK";
                conditions.Append(LocalizationService.Translate("Visibility is excellent (CAVOK). ", "La visibilité est excellente (CAVOK). "));
            }
            else
            {
                var visMatch = Regex.Match(upperMetar, @"\s([0-9]{4})\s");
                if (visMatch.Success && int.TryParse(visMatch.Groups[1].Value, out int visMeters))
                {
                    if (station != null) 
                    {
                        station.Visibility = $"{visMeters} m";
                        if (visMeters < 600) station.VisibilitySeverity = WeatherSeverity.Danger;
                        else if (visMeters < 1500) station.VisibilitySeverity = WeatherSeverity.Warning;
                        else station.VisibilitySeverity = WeatherSeverity.Normal;
                    }
                    if (visMeters < 1000) { conditions.Append(LocalizationService.Translate("Visibility is extremely low (less than 1km). ", "La visibilité est extrêmement faible (moins de 1km). ")); hasBadWeather = true; }
                    else if (visMeters < 5000) { conditions.Append(LocalizationService.Translate("Visibility is reduced. ", "La visibilité est réduite. ")); }
                    else conditions.Append(LocalizationService.Translate("Visibility is generally good (over 5km). ", "La visibilité est bonne (plus de 5km). "));
                }
            }
            
            // Clouds
            if (station != null)
            {
                var cloudMatches = Regex.Matches(upperMetar, @"(FEW|SCT|BKN|OVC)([0-9]{3})(CB|TCU)?");
                var cloudList = new System.Collections.Generic.List<string>();
                int lowestCeiling = 999;

                foreach (Match m in cloudMatches)
                {
                    cloudList.Add(m.Value);
                    string type = m.Groups[1].Value;
                    if ((type == "BKN" || type == "OVC") && int.TryParse(m.Groups[2].Value, out int height))
                    {
                        if (height < lowestCeiling) lowestCeiling = height;
                    }
                }

                if (cloudList.Count > 0) station.CloudBase = string.Join(" ", cloudList);
                else if (upperMetar.Contains("CAVOK") || upperMetar.Contains("NSC")) station.CloudBase = "CLR";

                // Severity calculation (height is in hundreds of feet)
                if (lowestCeiling < 2) station.CloudSeverity = WeatherSeverity.Danger; // < 200ft
                else if (lowestCeiling < 5) station.CloudSeverity = WeatherSeverity.Warning; // < 500ft
                else station.CloudSeverity = WeatherSeverity.Normal;
            }

            // Weather phenomena
            if (upperMetar.Contains(" TS") || upperMetar.Contains("TSRA") || upperMetar.Contains("VCTS"))
            {
                conditions.Append(LocalizationService.Translate("Thunderstorms are reported in the vicinity. ", "Des orages sont signalés à proximité. "));
                hasBadWeather = true;
            }
            if (upperMetar.Contains(" -RA") || Regex.IsMatch(upperMetar, @"\sRA\s") || upperMetar.Contains(" +RA"))
            {
                conditions.Append(LocalizationService.Translate(
                    upperMetar.Contains(" +RA") ? "Heavy rain is expected. " : "Expect some light to moderate rain. ",
                    upperMetar.Contains(" +RA") ? "De fortes pluies sont attendues. " : "Attendez-vous à de la pluie légère à modérée. "
                ));
                hasBadWeather = true;
            }
            if (upperMetar.Contains(" SN"))
            {
                conditions.Append(LocalizationService.Translate("Snow is reported, de-icing might be required. ", "De la neige est signalée, un dégivrage pourrait être nécessaire. "));
                hasBadWeather = true;
            }
            if (upperMetar.Contains(" FG") || upperMetar.Contains(" BR") || upperMetar.Contains("LIFR") || upperMetar.Contains("VV00") || upperMetar.Contains(" R0"))
            {
                conditions.Append(LocalizationService.Translate("Fog or mist is reducing visibility, be prepared for low visibility operations. ", "Brouillard ou brume réduisant la visibilité, préparons-nous à des opérations par faible visibilité. "));
                hasBadWeather = true;
            }
            if (upperMetar.Contains(" CB"))
            {
                conditions.Append(LocalizationService.Translate("Cumulonimbus clouds are present, potential for heavy turbulence. ", "Nuages cumulonimbus repérés, possible présence de fortes turbulences. "));
                hasBadWeather = true;
            }
            if (upperMetar.Contains(" WS"))
            {
                conditions.Append(LocalizationService.Translate("Wind shear is reported on the runways. ", "Cisaillement de vent signalé sur les pistes. "));
                hasBadWeather = true;
            }

            if (!hasBadWeather && !upperMetar.Contains("CAVOK"))
            {
                conditions.Append(LocalizationService.Translate("No significant adverse weather phenomena reported. ", "Aucun phénomène météorologique défavorable majeur signalé. "));
            }

            // Conclusion
            if (hasBadWeather)
            {
                conditions.Append(LocalizationService.Translate("In conclusion, expect some challenging conditions and heightened vigilance.", "En conclusion, attendez-vous à des conditions délicates exigeant une vigilance accrue."));
            }
            else
            {
                conditions.Append(LocalizationService.Translate("Therefore, we do not anticipate any particular weather-related issues for this phase.", "Par conséquent, nous n'anticipons aucun problème météorologique particulier pour cette phase."));
            }

            return conditions.ToString().Trim();
        }

        private string GetCardinalDirection(int degrees)
        {
            string[] directionsEn = { "North", "North-East", "East", "South-East", "South", "South-West", "West", "North-West" };
            string[] directionsFr = { "Nord", "Nord-Est", "Est", "Sud-Est", "Sud", "Sud-Ouest", "Ouest", "Nord-Ouest" };
            int index = (int)Math.Round(((double)degrees % 360) / 45);
            if (index == 8) index = 0;
            return LocalizationService.Translate($"Winds are coming from the {directionsEn[index]}. ", $"Le vent vient du {directionsFr[index]}. ");
        }
    }
}
