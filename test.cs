using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
.
public class TestClass {
    [JsonPropertyName("known_prop")]
    public string Expected { get; set; } = "test1";

    [JsonExtensionData]
    public Dictionary<string, JsonElement> Ext { get; set; }
}

public class Program {
    public static void Main() {
        string raw = "{ "known_prop": "test1", "unknown": "test2" }";
        var obj = JsonSerializer.Deserialize<TestClass>(raw);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        string reserialized = JsonSerializer.Serialize(obj, options);
        Console.WriteLine(reserialized);
    }
}
