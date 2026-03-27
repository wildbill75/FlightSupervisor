using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FlightSupervisor.UI.Services
{
    public class NoaaWeatherService
    {
        private readonly HttpClient _httpClient;

        public NoaaWeatherService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<string> GetMetarAsync(string icao)
        {
            if (string.IsNullOrWhiteSpace(icao)) return "";
            try
            {
                var url = $"https://aviationweather.gov/api/data/metar?ids={icao.ToUpper()}&format=raw";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return (await response.Content.ReadAsStringAsync()).Trim();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NOAA] Failed to fetch METAR for {icao}: {ex.Message}");
            }
            return "";
        }

        public async Task<string> GetTafAsync(string icao)
        {
            if (string.IsNullOrWhiteSpace(icao)) return "";
            try
            {
                var url = $"https://aviationweather.gov/api/data/taf?ids={icao.ToUpper()}&format=raw";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return (await response.Content.ReadAsStringAsync()).Trim();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NOAA] Failed to fetch TAF for {icao}: {ex.Message}");
            }
            return "";
        }
    }
}
