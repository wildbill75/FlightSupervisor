using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FlightSupervisor.UI.Models.SimBrief;

namespace FlightSupervisor.UI.Services
{
    public class SimBriefService
    {
        private readonly HttpClient _httpClient;
        private const string SimBriefApiUrl = "https://www.simbrief.com/api/xml.fetcher.php?username={0}&json=1";

        public SimBriefService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<SimBriefResponse?> FetchFlightPlanAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("SimBrief username cannot be empty.");

            try
            {
                var url = string.Format(SimBriefApiUrl, Uri.EscapeDataString(username));
                var response = await _httpClient.GetAsync(url);
                
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                
                // SimBrief API often replies with values as strings instead of proper numbers
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<SimBriefResponse>(jsonContent, options);
            }
            catch (Exception ex)
            {
                // Simple debug output for now
                System.Diagnostics.Debug.WriteLine($"Error fetching SimBrief data: {ex.Message}");
                return null;
            }
        }
    }
}
