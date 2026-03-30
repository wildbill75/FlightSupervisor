using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using FlightSupervisor.UI.Services;

namespace FlightSupervisor.UI.Models
{
    public class GroundEvent
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequiredActivePhase { get; set; } = string.Empty;
        public List<EventChoice> Choices { get; set; } = new List<EventChoice>();
    }

    public class EventChoice
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int DelayImpactSec { get; set; } = 0;
        public int ComfortImpact { get; set; } = 0;
        public int SafetyImpact { get; set; } = 0;
        public string ResponseLog { get; set; } = string.Empty;

        /// <summary>
        /// Evaluates if this choice is aligned with the airline's policy.
        /// Returns "green" if good, "red" if bad, "neutral" if standard.
        /// </summary>
        public string EvaluatePolicyAlignment(AirlineProfile profile)
        {
            if (SafetyImpact < 0) return "error"; // Always red if it's a safety hazard

            bool hurtsPunctuality = DelayImpactSec > 0;
            bool hurtsComfort = ComfortImpact < 0;

            if (hurtsPunctuality && profile.PunctualityPriority >= 7)
            {
                return "error"; // Delaying is bad for Low Cost
            }
            if (hurtsComfort && profile.HardProductScore >= 7)
            {
                return "error"; // Sacrificing comfort is bad for Premium
            }

            if (!hurtsPunctuality && profile.PunctualityPriority >= 7)
            {
                return "success"; // Staying on time is good for Low Cost
            }
            if (!hurtsComfort && profile.HardProductScore >= 7 && hurtsPunctuality)
            {
                return "success"; // Wait to preserve comfort for Premium
            }

            return "neutral"; // Else neutral (could map to white/gray)
        }
    }

    // DTO for sending to frontend
    public class EventChoiceDTO
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
        [JsonPropertyName("colorClass")]
        public string ColorClass { get; set; } = string.Empty;
    }

    public class GroundEventDTO
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("choices")]
        public List<EventChoiceDTO> Choices { get; set; } = new List<EventChoiceDTO>();
    }
}
