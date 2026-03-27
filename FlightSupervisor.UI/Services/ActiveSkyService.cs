using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FlightSupervisor.UI.Services
{
    public class ActiveSkyService
    {
        private readonly HttpClient _httpClient;
        private const string ActiveSkyApiUrl = "http://localhost:19285/ActiveSky/API/GetWeatherInfo?StationId={0}";

        public ActiveSkyService()
        {
            _httpClient = new HttpClient();
            // Short timeout to avoid freezing the app if Active Sky is not running
            _httpClient.Timeout = TimeSpan.FromSeconds(3);
        }

        public async Task<(string Metar, string Taf)> GetWeatherAsync(string icao)
        {
            if (string.IsNullOrWhiteSpace(icao)) return ("", "");

            try
            {
                var url = string.Format(ActiveSkyApiUrl, icao.ToUpper());
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var xmlContent = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(xmlContent))
                    {
                        var xmlData = XDocument.Parse(xmlContent);
                        var metarNode = xmlData.Descendants("Metar").FirstOrDefault();
                        var tafNode = xmlData.Descendants("Taf").FirstOrDefault();

                        string metar = metarNode?.Value?.Trim() ?? "";
                        string taf = tafNode?.Value?.Trim() ?? "";

                        return (metar, taf);
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Active Sky is likely not running on port 19285
                System.Diagnostics.Debug.WriteLine($"[ACTIVESKY] Connection failed for {icao}. Ensure ASFS/ASP3D is running.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ACTIVESKY] Error parsing weather for {icao}: {ex.Message}");
            }
            
            return ("", "");
        }
    }
}
