using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace FlightSupervisor.UI.Models.SimBrief
{
    public class SimBriefResponse
    {
        [JsonPropertyName("isDummy")]
        public bool IsDummy { get; set; } = false;

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
        [JsonConverter(typeof(SingleOrArrayConverter<AirportInfo>))]
        public List<AirportInfo>? Alternates { get; set; }

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

        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
    }

    public class TextInfo
    {
        [JsonPropertyName("plan_html")]
        public string? PlanHtml { get; set; }

        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
    }

    public class NavlogContainer
    {
        [JsonPropertyName("fix")]
        public System.Collections.Generic.List<NavlogInfo>? Fixes { get; set; }

        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
    }

    public class NavlogInfo
    {
        [JsonPropertyName("turb")]
        public string? Turb { get; set; }

        [JsonPropertyName("shear")]
        public string? Shear { get; set; }

        [JsonPropertyName("tropopause_feet")]
        public string? TropopauseFeet { get; set; }

        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
    }

    public class FetchInfo
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
    }

    public class ParamsInfo
    {
        [JsonPropertyName("units")]
        public string? Units { get; set; }

        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
    }

    public class GeneralInfo
    {
        [JsonPropertyName("icao_airline")]
        public string? Airline { get; set; }

        [JsonPropertyName("iata_airline")]
        public string? IataAirline { get; set; }

        [JsonPropertyName("flight_number")]
        public string? FlightNumber { get; set; }
        
        [JsonPropertyName("costindex")]
        public string? CostIndex { get; set; }
        
        [JsonPropertyName("initial_altitude")]
        public string? InitialAlt { get; set; }

        [JsonPropertyName("stepclimb_string")]
        public string? StepClimbString { get; set; }

        [JsonPropertyName("route")]
        public string? Route { get; set; }

        [JsonPropertyName("avg_wind_comp")]
        public string? AvgWindComp { get; set; }

        [JsonPropertyName("route_distance")]
        public string? RouteDistance { get; set; }

        [JsonPropertyName("etops")]
        public string? Etops { get; set; }

        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
    }

    public class AircraftInfo
    {
        [JsonPropertyName("icaocode")]
        public string? IcaoCode { get; set; }

        [JsonPropertyName("internal_id")]
        public string? InternalId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("reg")]
        public string? Reg { get; set; }

        [JsonPropertyName("base_type")]
        public string? BaseType { get; set; }

        [JsonPropertyName("max_passengers")]
        public string? MaxPassengers { get; set; }

        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
    }

    public class AirportInfo
    {
        [JsonPropertyName("icao_code")]
        public string? IcaoCode { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("pos_lat")]
        public string? PosLat { get; set; }

        [JsonPropertyName("pos_long")]
        public string? PosLong { get; set; }

        [JsonPropertyName("plan_rwy")]
        public string? PlanRwy { get; set; }

        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
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

        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
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

        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
    }

    public class FuelInfo
    {
        [JsonPropertyName("plan_ramp")]
        public string? PlanRamp { get; set; }

        [JsonPropertyName("plan_landing")]
        public string? PlanLanding { get; set; }

        [JsonPropertyName("extra")]
        public string? Extra { get; set; }

        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
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

        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? ExtensionData { get; set; }
    }

    public class SingleOrArrayConverter<T> : JsonConverter<List<T>>
    {
        public override List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var list = new List<T>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    list.Add(JsonSerializer.Deserialize<T>(ref reader, options)!);
                }
                return list;
            }
            else
            {
                var singleItem = JsonSerializer.Deserialize<T>(ref reader, options);
                return singleItem != null ? new List<T> { singleItem } : new List<T>();
            }
        }

        public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
        {
            if (value.Count == 1)
            {
                JsonSerializer.Serialize(writer, value[0], options);
            }
            else
            {
                writer.WriteStartArray();
                foreach (var item in value)
                {
                    JsonSerializer.Serialize(writer, item, options);
                }
                writer.WriteEndArray();
            }
        }
    }
}
