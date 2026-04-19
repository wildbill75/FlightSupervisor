using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public class AircraftInfo {
    [JsonPropertyName("icaocode")] public string IcaoCode { get; set; }
    [JsonPropertyName("base_type")] public string BaseType { get; set; }
}

public class SimBriefResponse {
    [JsonPropertyName("aircraft")] public AircraftInfo Aircraft { get; set; }
}

public class Program {
    public static void Main() {
        var json = File.ReadAllText(@"D:\FlightSupervisor\latest_simbrief.json");
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var resp = JsonSerializer.Deserialize<SimBriefResponse>(json, opts);
        Console.WriteLine($"IcaoCode: {resp?.Aircraft?.IcaoCode}, BaseType: {resp?.Aircraft?.BaseType}");
    }
}
