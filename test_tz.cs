using System;
using GeoTimeZone;
using TimeZoneConverter;
class Program {
    static void Main() {
        var tz = TimeZoneLookup.GetTimeZone(48.723, 2.37);
        Console.WriteLine("TZID: " + tz.Result);
        var tzi = TZConvert.GetTimeZoneInfo(tz.Result);
        Console.WriteLine("TZI: " + tzi.Id);
    }
}
