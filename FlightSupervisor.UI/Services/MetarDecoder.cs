using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using FlightSupervisor.UI.Models;

namespace FlightSupervisor.UI.Services
{
    public class MetarAnalysisResult
    {
        public string CommentaryEn { get; set; } = "";
        public string CommentaryFr { get; set; } = "";

        public string RawWind { get; set; } = "";
        public WeatherSeverity WindSeverity { get; set; } = WeatherSeverity.Normal;

        public string RawVisibility { get; set; } = "";
        public WeatherSeverity VisibilitySeverity { get; set; } = WeatherSeverity.Normal;

        public string RawClouds { get; set; } = "";
        public WeatherSeverity CloudSeverity { get; set; } = WeatherSeverity.Normal;

        public string RawTempDew { get; set; } = "";
        public string RawQnh { get; set; } = "";
    }

    public static class MetarDecoder
    {
        private static readonly Dictionary<string, string> EnPhenomena = new Dictionary<string, string>
        {
            {"DZ", "drizzle"}, {"RA", "rain"}, {"SN", "snow"}, {"SG", "snow grains"},
            {"IC", "ice crystals"}, {"PL", "ice pellets"}, {"GR", "hail"}, {"GS", "small hail"},
            {"UP", "unknown precipitation"}, {"BR", "mist"}, {"FG", "fog"}, {"FU", "smoke"},
            {"VA", "volcanic ash"}, {"DU", "dust"}, {"SA", "sand"}, {"HZ", "haze"},
            {"PO", "dust devils"}, {"SQ", "squalls"}, {"FC", "funnel cloud/tornado"},
            {"SS", "sandstorm"}, {"DS", "duststorm"}
        };

        private static readonly Dictionary<string, string> FrPhenomena = new Dictionary<string, string>
        {
            {"DZ", "bruine"}, {"RA", "pluie"}, {"SN", "neige"}, {"SG", "neige en grains"},
            {"IC", "cristaux de glace"}, {"PL", "grésil"}, {"GR", "grêle"}, {"GS", "petite grêle"},
            {"UP", "précipitations inconnues"}, {"BR", "brume"}, {"FG", "brouillard"}, {"FU", "fumée"},
            {"VA", "cendres volcaniques"}, {"DU", "poussière"}, {"SA", "sable"}, {"HZ", "brume sèche"},
            {"PO", "tourbillons de poussière"}, {"SQ", "grains"}, {"FC", "nuage en entonnoir/tornade"},
            {"SS", "tempête de sable"}, {"DS", "tempête de poussière"}
        };

        private static readonly Dictionary<string, string> EnDescriptors = new Dictionary<string, string>
        {
            {"MI", "shallow"}, {"PR", "partial"}, {"BC", "patches of"}, {"DR", "low drifting"},
            {"BL", "blowing"}, {"SH", "showers of"}, {"TS", "thunderstorm with"}, {"FZ", "freezing"}
        };

        private static readonly Dictionary<string, string> FrDescriptors = new Dictionary<string, string>
        {
            {"MI", "mince"}, {"PR", "partiel"}, {"BC", "bancs de"}, {"DR", "chasse-basse de"},
            {"BL", "chasse-neige/poussière élevée de"}, {"SH", "averses de"}, {"TS", "orage avec"}, {"FZ", "verglaçant(e)"}
        };

        private static readonly Dictionary<string, string> EnClouds = new Dictionary<string, string>
        {
            {"FEW", "few clouds"}, {"SCT", "scattered clouds"}, {"BKN", "broken clouds"}, {"OVC", "overcast sky"}, {"VV", "vertical visibility"}
        };

        private static readonly Dictionary<string, string> FrClouds = new Dictionary<string, string>
        {
            {"FEW", "quelques nuages"}, {"SCT", "nuages épars"}, {"BKN", "ciel très nuageux"}, {"OVC", "ciel couvert"}, {"VV", "visibilité verticale"}
        };

        public static MetarAnalysisResult Decode(string code, bool isTafContext, UnitPreferences unitsConfig, string contextPhaseEn, string contextPhaseFr)
        {
            var res = new MetarAnalysisResult();
            if (string.IsNullOrWhiteSpace(code)) return res;

            code = code.Replace("=", "").Replace('\n', ' ').Replace('\r', ' ').Trim();

            // Remove RMK
            int rmkIndex = code.IndexOf(" RMK ");
            if (rmkIndex != -1) code = code.Substring(0, rmkIndex);

            var tokens = code.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var enText = new StringBuilder();
            var frText = new StringBuilder();

            List<string> rvrEn = new List<string>();
            List<string> rvrFr = new List<string>();

            // Setup intro
            enText.Append($"Okay, looking at the {contextPhaseEn} weather... ");
            frText.Append($"Bon, voyons la météo pour {contextPhaseFr}... ");

            foreach (var t in tokens)
            {
                // Trend Modifiers
                if (t == "BECMG") { enText.Append("It looks like it will be becoming: "); frText.Append("Ça va évoluer pour devenir : "); continue; }
                if (t == "TEMPO") { enText.Append("Temporarily, we might expect: "); frText.Append("Temporairement, on peut s'attendre à : "); continue; }
                if (t.StartsWith("PROB30")) { enText.Append("There is a 30 percent chance of: "); frText.Append("Il y a 30% de probabilité d'avoir : "); continue; }
                if (t.StartsWith("PROB40")) { enText.Append("There is a 40 percent chance of: "); frText.Append("Il y a 40% de chances d'avoir : "); continue; }
                if (t.StartsWith("FM") && t.Length == 8) { 
                    enText.Append($"From {t.Substring(2,2)}h{t.Substring(4,2)}Z, expect: "); 
                    frText.Append($"À partir de {t.Substring(2,2)}h{t.Substring(4,2)} Zulu, attendez-vous à : "); 
                    continue; 
                }

                // Wind
                var windMatch = Regex.Match(t, @"^(VRB|[0-9]{3})([0-9]{2,3})(?:G([0-9]{2,3}))?(KT|MPS|KMH)$");
                if (windMatch.Success)
                {
                    string dir = windMatch.Groups[1].Value;
                    int spd = int.Parse(windMatch.Groups[2].Value);
                    bool gusts = windMatch.Groups[3].Success;
                    int gustSpd = gusts ? int.Parse(windMatch.Groups[3].Value) : 0;
                    string unit = windMatch.Groups[4].Value;
                    res.RawWind = $"{dir}/{spd}{(gusts ? "G" + gustSpd : "")}{unit.ToLower()}";

                    int spdKnots = unit == "MPS" ? (int)(spd * 1.94384) : unit == "KMH" ? (int)(spd / 1.852) : spd;
                    int gustKnots = unit == "MPS" ? (int)(gustSpd * 1.94384) : unit == "KMH" ? (int)(gustSpd / 1.852) : gustSpd;

                    if (spdKnots > 35 || (gusts && gustKnots > 40)) res.WindSeverity = WeatherSeverity.Danger;
                    else if (spdKnots > 20 || gusts) res.WindSeverity = WeatherSeverity.Warning;

                    if (dir == "VRB")
                    {
                        enText.Append($"Winds are variable at {spd} {unit}");
                        frText.Append($"Le vent est variable à {spd} {unit}");
                    }
                    else
                    {
                        enText.Append($"Winds are blowing from {dir} degrees at {spd} {unit}");
                        frText.Append($"Le vent vient du {dir} à {spd} {unit}");
                    }

                    if (gusts)
                    {
                        enText.Append($", with strong gusts up to {gustSpd}");
                        frText.Append($", avec des rafales jusqu'à {gustSpd}");
                    }
                    enText.Append(". ");
                    frText.Append(". ");
                    continue;
                }

                // Visibility (CAVOK, meters, SM)
                if (t == "CAVOK")
                {
                    res.RawVisibility = "CAVOK";
                    res.RawClouds = "CLR";
                    enText.Append("Visibility and clouds are CAVOK, so it's perfectly clear. ");
                    frText.Append("On est CAVOK, donc excellente visibilité et plafond dégagé. ");
                    continue;
                }

                if (Regex.IsMatch(t, @"^[0-9]{4}$") && !t.StartsWith("0000")) 
                {
                    int visMeters = int.Parse(t);
                    res.RawVisibility = $"{visMeters} m";
                    if (visMeters < 800) res.VisibilitySeverity = WeatherSeverity.Danger;
                    else if (visMeters < 2000) res.VisibilitySeverity = WeatherSeverity.Warning;

                    if (visMeters >= 9999) {
                        enText.Append("Visibility is 10 kilometers or more. ");
                        frText.Append("La visibilité est supérieure à 10 kilomètres. ");
                    } else if (visMeters < 1000) {
                        enText.Append($"Visibility is quite poor, down to {visMeters} meters. ");
                        frText.Append($"La visibilité est très réduite, tombant à {visMeters} mètres. ");
                    } else {
                        enText.Append($"Visibility is {visMeters} meters. ");
                        frText.Append($"Visibilité de {visMeters} mètres. ");
                    }
                    continue;
                }
                
                var smMatch = Regex.Match(t, @"^(P|M)?(\d{1,2}|((\d+ )?\d+/\d+))SM$");
                if (smMatch.Success)
                {
                    res.RawVisibility = t;
                    enText.Append($"Visibility is {t.Replace("SM", " statute miles")}. ");
                    frText.Append($"Visibilité de {t.Replace("SM", " miles terrestres")}. ");
                    continue;
                }

                // RVR
                var rvrMatch = Regex.Match(t, @"^R([0-9]{2}[LCR]?)/(P|M)?([0-9]{4})(V[0-9]{4})?([UDN])?$");
                if (rvrMatch.Success)
                {
                    string rwy = rvrMatch.Groups[1].Value;
                    string val = rvrMatch.Groups[3].Value;
                    string trend = rvrMatch.Groups[5].Value;
                    
                    string trendEn = trend == "U" ? "improving" : trend == "D" ? "decreasing" : "stable";
                    string trendFr = trend == "U" ? "en augmentation" : trend == "D" ? "en diminution" : "stable";

                    rvrEn.Add($"runway {rwy} RVR is {val} meters ({trendEn})");
                    rvrFr.Add($"la RVR piste {rwy} est de {val} mètres ({trendFr})");
                    continue;
                }

                // Weather Phenomenons
                var wxMatch = Regex.Match(t, @"^(-|\+|VC)?(MI|PR|BC|DR|BL|SH|TS|FZ)?(DZ|RA|SN|SG|IC|PL|GR|GS|UP|BR|FG|FU|VA|DU|SA|HZ|PO|SQ|FC|SS|DS)$");
                if (wxMatch.Success)
                {
                    string intensity = wxMatch.Groups[1].Value;
                    string desc = wxMatch.Groups[2].Value;
                    string phen = wxMatch.Groups[3].Value;

                    string intEn = intensity == "-" ? "light " : intensity == "+" ? "heavy " : intensity == "VC" ? "in the vicinity " : "";
                    string intFr = intensity == "-" ? "léger(e) " : intensity == "+" ? "fort(e) " : intensity == "VC" ? "à proximité " : "";

                    string descEn = EnDescriptors.ContainsKey(desc) ? EnDescriptors[desc] + " " : "";
                    string descFr = FrDescriptors.ContainsKey(desc) ? FrDescriptors[desc] + " " : "";

                    string pEn = EnPhenomena.ContainsKey(phen) ? EnPhenomena[phen] : phen;
                    string pFr = FrPhenomena.ContainsKey(phen) ? FrPhenomena[phen] : phen;

                    enText.Append($"We have {intEn}{descEn}{pEn}. ");
                    frText.Append($"On signale du {intFr}{descFr}{pFr}. ");
                    continue;
                }

                // Clouds
                var cloudMatch = Regex.Match(t, @"^(FEW|SCT|BKN|OVC|VV)([0-9]{3}|///)(CB|TCU)?$");
                if (cloudMatch.Success)
                {
                    if (string.IsNullOrEmpty(res.RawClouds)) res.RawClouds = t;
                    else res.RawClouds += $" {t}";

                    string type = cloudMatch.Groups[1].Value;
                    string heightStr = cloudMatch.Groups[2].Value;
                    string extra = cloudMatch.Groups[3].Value;

                    int height = heightStr == "///" ? 0 : int.Parse(heightStr) * 100;
                    
                    if ((type == "OVC" || type == "BKN") && height < 500) res.CloudSeverity = WeatherSeverity.Danger;
                    else if ((type == "OVC" || type == "BKN") && height < 1500) res.CloudSeverity = WeatherSeverity.Warning;

                    string cEn = EnClouds.ContainsKey(type) ? EnClouds[type] : type;
                    string cFr = FrClouds.ContainsKey(type) ? FrClouds[type] : type;

                    string hEn = height > 0 ? $" at {height} feet" : "";
                    string hFr = height > 0 ? $" à {height} pieds" : "";

                    string xtraEn = extra == "CB" ? " with cumulonimbus" : extra == "TCU" ? " with towering cumulus" : "";
                    string xtraFr = extra == "CB" ? " avec des cumulonimbus" : extra == "TCU" ? " avec des cumulus bourgeonnants" : "";

                    enText.Append($"There is {cEn}{hEn}{xtraEn}. ");
                    frText.Append($"Il y a {cFr}{hFr}{xtraFr}. ");
                    continue;
                }

                if (t == "NSC" || t == "SKC" || t == "CLR" || t == "NCD")
                {
                    res.RawClouds = "CLR";
                    enText.Append("The sky is clear of significant clouds. ");
                    frText.Append("Le ciel est dégagé de nuages significatifs. ");
                    continue;
                }

                // Temp / Dew
                var tempMatch = Regex.Match(t, @"^(M?[0-9]{2})/(M?[0-9]{2})$");
                if (tempMatch.Success)
                {
                    res.RawTempDew = t;
                    string tStr = tempMatch.Groups[1].Value.Replace("M", "-");
                    string dStr = tempMatch.Groups[2].Value.Replace("M", "-");
                    enText.Append($"Temperature is {tStr} degrees, dew point at {dStr}. ");
                    frText.Append($"Température {tStr} degrés, point de rosée à {dStr}. ");
                    continue;
                }

                // QNH
                var qnhMatch = Regex.Match(t, @"^(Q|A)([0-9]{4})$");
                if (qnhMatch.Success)
                {
                    res.RawQnh = t;
                    string type = qnhMatch.Groups[1].Value;
                    string val = qnhMatch.Groups[2].Value;
                    if (type == "Q")
                    {
                        enText.Append($"Altimeter QNH is {val}. ");
                        frText.Append($"Le QNH est à {val}. ");
                    }
                    else
                    {
                        double inHg = int.Parse(val) / 100.0;
                        enText.Append($"Altimeter setting is {inHg:F2} inHg. ");
                        frText.Append($"Calage altimétrique à {inHg:F2} inHg. ");
                    }
                    continue;
                }
            }

            if (rvrEn.Count > 0)
            {
                enText.Append($"Note that {string.Join(" and ", rvrEn)}. ");
                frText.Append($"Attention, {string.Join(" et ", rvrFr)}. ");
            }

            // Cleanup & Format
            string finalEn = enText.ToString().Trim().Replace(" .", ".");
            string finalFr = frText.ToString().Trim().Replace(" .", ".");

            // Polishing text to make it read perfectly
            finalEn = Regex.Replace(finalEn, @"\s+", " ");
            finalFr = Regex.Replace(finalFr, @"\s+", " ");

            res.CommentaryEn = finalEn;
            res.CommentaryFr = finalFr;

            return res;
        }
    }
}
