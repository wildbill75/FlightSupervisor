using System;
using GeoTimeZone;
using TimeZoneConverter;

class Program {
    static void Main() {
        string tz = TimeZoneLookup.GetTimeZone(48.8, 2.3).Result;
        TimeZoneInfo tzi = TZConvert.GetTimeZoneInfo(tz);
        Console.WriteLine(tzi.Id);
    }
}
