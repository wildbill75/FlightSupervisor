using System.Collections.Generic;

namespace FlightSupervisor.UI.Models
{
    public class BriefingData
    {
        public string HeaderText { get; set; } = "";
        public string EnrouteText { get; set; } = "";
        public List<string> OralCommentary { get; set; } = new List<string>();
        public List<string> AlertMessages { get; set; } = new List<string>();
        
        public List<BriefingStation> Stations { get; set; } = new List<BriefingStation>();

        // Dispatch Recommendations (Company Policy)
        public int RecommendedExtraFuel { get; set; } = 0;
        public string PolicyNarrative { get; set; } = "";
        public int RecommendedCostIndex { get; set; } = 0;
        public int RecommendedAltitude { get; set; } = 0;
    }

    public enum WeatherSeverity
    {
        Normal,
        Warning,
        Danger
    }

    public class BriefingStation
    {
        public string Id { get; set; } = ""; // origin, destination, alternate
        public string Icao { get; set; } = "";
        public string Label { get; set; } = ""; 
        public string RawMetar { get; set; } = "";
        public string RawTaf { get; set; } = "";
        
        // Highlight Variables
        public string Wind { get; set; } = "";
        public WeatherSeverity WindSeverity { get; set; } = WeatherSeverity.Normal;

        public string Visibility { get; set; } = "";
        public WeatherSeverity VisibilitySeverity { get; set; } = WeatherSeverity.Normal;

        public string CloudBase { get; set; } = "";
        public WeatherSeverity CloudSeverity { get; set; } = WeatherSeverity.Normal;

        public string TempDew { get; set; } = "";
        public string Qnh { get; set; } = "";
        
        // Narrative / Comments
        public string RunwayAdvice { get; set; } = "";
        public string Commentary { get; set; } = "";
        public string Notams { get; set; } = "";
    }
}
