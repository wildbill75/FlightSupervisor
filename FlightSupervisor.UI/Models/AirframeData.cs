using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlightSupervisor.UI.Models
{
    public class AirframeState
    {
        [JsonPropertyName("registration")]
        public string Registration { get; set; } = "";

        [JsonPropertyName("baseType")]
        public string BaseType { get; set; } = "";

        [JsonPropertyName("airline")]
        public string Airline { get; set; } = "";

        [JsonPropertyName("ageInYears")]
        public double AgeInYears { get; set; } = 0;

        [JsonPropertyName("totalHours")]
        public double TotalHours { get; set; } = 0;

        [JsonPropertyName("totalCycles")]
        public int TotalCycles { get; set; } = 0;

        [JsonPropertyName("deliveryDate")]
        public DateTime DeliveryDate { get; set; } = DateTime.Now;

        [JsonPropertyName("maintenanceGrade")]
        public string MaintenanceGrade { get; set; } = "A";

        [JsonPropertyName("engineType")]
        public string EngineType { get; set; } = "Unknown";

        [JsonPropertyName("emptyWeight")]
        public double EmptyWeight { get; set; } = 0;

        [JsonPropertyName("maxPassengers")]
        public int MaxPassengers { get; set; } = 0;

        [JsonPropertyName("maxZeroFuelWeight")]
        public double MaxZeroFuelWeight { get; set; } = 0;

        [JsonPropertyName("maxTakeoffWeight")]
        public double MaxTakeoffWeight { get; set; } = 0;

        [JsonPropertyName("maxLandingWeight")]
        public double MaxLandingWeight { get; set; } = 0;

        [JsonPropertyName("maxFuelCapacity")]
        public double MaxFuelCapacity { get; set; } = 0;

        [JsonPropertyName("engineWear")]
        public double EngineWear { get; set; } = 0.0;

        [JsonPropertyName("gearAndBrakeWear")]
        public double GearAndBrakeWear { get; set; } = 0.0;

        [JsonPropertyName("flapsWear")]
        public double FlapsWear { get; set; } = 0.0;

        [JsonPropertyName("structureWear")]
        public double StructureWear { get; set; } = 0.0;

        [JsonPropertyName("activeDefects")]
        public List<string> ActiveDefects { get; set; } = new List<string>();

        [JsonPropertyName("events")]
        public List<AirframeLogEvent> Events { get; set; } = new List<AirframeLogEvent>();
    }

    public class AirframeLogEvent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "flight"; // flight, maintenance, defect_open, defect_closed

        [JsonPropertyName("location")]
        public string Location { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "info"; // info, warn, error
    }
}
