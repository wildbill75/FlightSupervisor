using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FlightSupervisor.UI.Services;
using FlightSupervisor.UI.Models;
using FlightSupervisor.UI.Models.SimBrief;

class Program { static void Main() { Test.Run(); } }

public class Test
{
    public static void Run()
    {
        string json = File.ReadAllText(@"d:\FlightSupervisor\simbrief.json");
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var sbData = JsonSerializer.Deserialize<SimBriefResponse>(json, opts);
        
        var service = new WeatherBriefingService();
        var briefing = service.GenerateBriefing(sbData);

        foreach (var station in briefing.Stations)
        {
            Console.WriteLine($"{station.Id}: QNH='{station.Qnh}' METAR='{station.RawMetar}'");
        }
    }
}
