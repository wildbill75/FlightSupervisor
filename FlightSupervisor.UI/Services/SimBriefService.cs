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
        private const string SimBriefApiUrl = "https://www.simbrief.com/api/xml.fetcher.php?username={0}&json=1&_nocache={1}";

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
                string nocache = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                var url = string.Format(SimBriefApiUrl, Uri.EscapeDataString(username), nocache);
                var response = await _httpClient.GetAsync(url);
                
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                
                // SimBrief API often replies with values as strings instead of proper numbers
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<SimBriefResponse>(jsonContent, options);
                
                // Tighten SIBT by 10 minutes (600 seconds) to remove excessive SimBrief padding and increase difficulty
                if (result?.Times?.SchedIn != null && long.TryParse(result.Times.SchedIn, out long schedInUnix))
                {
                    result.Times.SchedIn = (schedInUnix - 600).ToString();
                }
                
                if (result?.Times?.SchedBlock != null && long.TryParse(result.Times.SchedBlock, out long schedBlockSec))
                {
                    result.Times.SchedBlock = Math.Max(0, schedBlockSec - 600).ToString();
                }
                
                if (result?.Weights?.EstBlock != null && long.TryParse(result.Weights.EstBlock, out long estBlockSec))
                {
                    result.Weights.EstBlock = Math.Max(0, estBlockSec - 600).ToString();
                }

                return result;
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
