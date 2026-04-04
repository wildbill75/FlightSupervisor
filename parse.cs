using System;
using System.IO;
using System.Text.RegularExpressions;
class Program {
    static void Main() {
        var lines = File.ReadAllLines(@"d:\FlightSupervisor\FlightSupervisor.UI\Services\SimConnectService.cs");
        int count1 = 0;
        int count2 = 0;
        Console.WriteLine("STRUCT FIELDS:");
        foreach(var line in lines) {
            if (line.Contains("public double") && !line.Contains("GForce;")) {
                Console.WriteLine((++count1) + ": " + line.Trim());
            }
        }
        Console.WriteLine("\nDEFINITIONS:");
        foreach(var line in lines) {
            if (line.Contains("AddToDataDefinition(DEFINITIONS.PlaneData")) {
                var match = Regex.Match(line, "\"([^\"]+)\"");
                Console.WriteLine((++count2) + ": " + match.Groups[1].Value);
            }
        }
    }
}
