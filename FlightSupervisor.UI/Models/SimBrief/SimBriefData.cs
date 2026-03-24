using System.Text.Json.Serialization;

namespace FlightSupervisor.UI.Models.SimBrief
{
    public class SimBriefResponse
    {
        [JsonPropertyName("fetch")]
        public FetchInfo? Fetch { get; set; }

        [JsonPropertyName("params")]
        public ParamsInfo? Params { get; set; }

        [JsonPropertyName("fuel")]
        public FuelInfo? Fuel { get; set; }

        [JsonPropertyName("general")]
        public GeneralInfo? General { get; set; }

        [JsonPropertyName("aircraft")]
        public AircraftInfo? Aircraft { get; set; }

        [JsonPropertyName("origin")]
        public AirportInfo? Origin { get; set; }

        [JsonPropertyName("destination")]
        public AirportInfo? Destination { get; set; }

        [JsonPropertyName("alternate")]
        public AirportInfo? Alternate { get; set; }

        [JsonPropertyName("times")]
        public TimesInfo? Times { get; set; }

        [JsonPropertyName("weights")]
        public WeightsInfo? Weights { get; set; }
        
        [JsonPropertyName("weather")]
        public WeatherInfo? Weather { get; set; }

        [JsonPropertyName("text")]
        public TextInfo? Text { get; set; }

        [JsonPropertyName("navlog")]
        public NavlogContainer? Navlog { get; set; }
    }

    public class TextInfo
    {
        [JsonPropertyName("plan_html")]
        public string? PlanHtml { get; set; }
    }

    public class NavlogContainer
    {
        [JsonPropertyName("fix")]
        public System.Collections.Generic.List<NavlogInfo>? Fixes { get; set; }
    }

    public class NavlogInfo
    {
        [JsonPropertyName("turb")]
        public string? Turb { get; set; }

        [JsonPropertyName("shear")]
        public string? Shear { get; set; }

        [JsonPropertyName("tropopause_feet")]
        public string? TropopauseFeet { get; set; }
    }

    public class FetchInfo
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    public class ParamsInfo
    {
        [JsonPropertyName("units")]
        public string? Units { get; set; }
    }

    public class GeneralInfo
    {
        [JsonPropertyName("icao_airline")]
        public string? Airline { get; set; }

        [JsonPropertyName("flight_number")]
        public string? FlightNumber { get; set; }
        
        [JsonPropertyName("initial_altitude")]
        public string? InitialAlt { get; set; }

        [JsonPropertyName("stepclimb_string")]
        public string? StepClimbString { get; set; }

        [JsonPropertyName("route")]
        public string? Route { get; set; }

        [JsonPropertyName("avg_wind_comp")]
        public string? AvgWindComp { get; set; }

        [JsonPropertyName("etops")]
        public string? Etops { get; set; }
    }

    public class AircraftInfo
    {
        [JsonPropertyName("icaocode")]
        public string? IcaoCode { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("base_type")]
        public string? BaseType { get; set; }

        [JsonPropertyName("max_passengers")]
        public string? MaxPassengers { get; set; }
    }

    public class AirportInfo
    {
        [JsonPropertyName("icao_code")]
        public string? IcaoCode { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("plan_rwy")]
        public string? PlanRwy { get; set; }
    }

    public class TimesInfo
    {
        [JsonPropertyName("est_time_enroute")]
        public string? EstTimeEnroute { get; set; } // in seconds

        [JsonPropertyName("sched_block")]
        public string? SchedBlock { get; set; } // in seconds

        [JsonPropertyName("sched_out")]
        public string? SchedOut { get; set; } // unix timestamp

        [JsonPropertyName("sched_in")]
        public string? SchedIn { get; set; } // unix timestamp
    }

    public class WeightsInfo
    {
        [JsonPropertyName("pax_count")]
        public string? PaxCount { get; set; }

        [JsonPropertyName("max_pax")]
        public string? MaxPax { get; set; }

        [JsonPropertyName("est_zfw")]
        public string? EstZfw { get; set; }

        [JsonPropertyName("est_ldw")]
        public string? EstLdw { get; set; }

        [JsonPropertyName("est_tow")]
        public string? EstTow { get; set; }

        [JsonPropertyName("est_cg")]
        public string? EstCg { get; set; }

        [JsonPropertyName("mac")]
        public string? Mac { get; set; }
        
        [JsonPropertyName("tocg")]
        public string? ToCg { get; set; }

        [JsonPropertyName("est_block")]
        public string? EstBlock { get; set; }

        [JsonPropertyName("block_fuel")]
        public string? BlockFuel { get; set; }
    }

    public class FuelInfo
    {
        [JsonPropertyName("plan_ramp")]
        public string? PlanRamp { get; set; }
    }
    
    public class WeatherInfo
    {
        [JsonPropertyName("orig_metar")]
        public string? OrigMetar { get; set; }
        
        [JsonPropertyName("dest_metar")]
        public string? DestMetar { get; set; }
        
        [JsonPropertyName("orig_taf")]
        public string? OrigTaf { get; set; }
        
        [JsonPropertyName("dest_taf")]
        public string? DestTaf { get; set; }

        [JsonPropertyName("altn_metar")]
        public System.Text.Json.JsonElement? AltnMetar { get; set; }

        [JsonPropertyName("altn_taf")]
        public System.Text.Json.JsonElement? AltnTaf { get; set; }

        [JsonPropertyName("enrt_metar")]
        public System.Text.Json.JsonElement? EnrtMetar { get; set; }

        [JsonPropertyName("enrt_taf")]
        public System.Text.Json.JsonElement? EnrtTaf { get; set; }
    }
}
