using System;
using GeoTimeZone;
using TimeZoneConverter;
class Program {
    static void Main() {
        try {
            var tz = TimeZoneLookup.GetTimeZone(48.723, 2.37);
            Console.WriteLine("TZ: " + tz.Result);
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex);
        }
    }
}
