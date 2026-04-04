using System;
using System.Text.Json;

public class GroundService {
    public int ElapsedSec { get; set; } = 5;
    public int TotalDurationSec { get; set; } = 15;
    public int DelayAddedSec { get; set; } = 0;
    public int RemainingSec => Math.Max(0, (TotalDurationSec + DelayAddedSec) - ElapsedSec);
}

public class Program {
    public static void Main() {
        var s = new GroundService();
        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        Console.WriteLine(JsonSerializer.Serialize(s));
        Console.WriteLine(JsonSerializer.Serialize(s, opts));
    }
}
