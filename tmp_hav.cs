using System;

class Program {
    static double ToRadians(double angle) { return Math.PI * angle / 180.0; }
    static double CalculateHaversineDistanceNM(double lat1, double lon1, double lat2, double lon2)
    {
        var r = 3440.065; // Radius of earth in Nautical Miles
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }
    static void Main() {
        double latM = 48.725278;
        double lonM = 2.359444;
        
        // Scenario 1: MSFS returned Radians, multiplied by 180/PI
        double lat1 = (latM * Math.PI / 180.0) * (180.0 / Math.PI);
        double lon1 = (lonM * Math.PI / 180.0) * (180.0 / Math.PI);
        Console.WriteLine($"Scenario 1: {CalculateHaversineDistanceNM(lat1, lon1, latM, lonM)}");

        // Scenario 2: MSFS returned Degrees, multiplied by 180/PI
        double lat2 = latM * (180.0 / Math.PI);
        double lon2 = lonM * (180.0 / Math.PI);
        Console.WriteLine($"Scenario 2: {CalculateHaversineDistanceNM(lat2, lon2, latM, lonM)}");

        // Scenario 3: 0,0 
        Console.WriteLine($"Scenario 3: {CalculateHaversineDistanceNM(0, 0, latM, lonM)}");
    }
}
