using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using FlightSupervisor.UI.Models;

namespace FlightSupervisor.UI.Services
{
    public class AirframeManager
    {
        private readonly string _baseDirPath;

        public AirframeState? CurrentAirframe { get; private set; }

        public AirframeManager(string basePath = null)
        {
            if (basePath == null) 
            {
                basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor");
            }
            _baseDirPath = Path.Combine(basePath, "Airframes");
            if (!Directory.Exists(_baseDirPath)) 
            {
                Directory.CreateDirectory(_baseDirPath);
            }
        }

        public AirframeState GetOrCreateAirframe(string registration, string baseType, string airline, string currentIcao)
        {
            if (string.IsNullOrEmpty(registration)) 
                registration = "UNKNOWN";

            string sanitizedReg = string.Concat(registration.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(_baseDirPath, $"{sanitizedReg}.json");

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    CurrentAirframe = JsonSerializer.Deserialize<AirframeState>(json) ?? GenerateSeededAirframe(registration, baseType, airline, currentIcao);
                    return CurrentAirframe;
                }
                catch
                {
                    return GenerateSeededAirframe(registration, baseType, airline, currentIcao);
                }
            }
            else
            {
                var newAirframe = GenerateSeededAirframe(registration, baseType, airline, currentIcao);
                SaveAirframe(newAirframe);
                CurrentAirframe = newAirframe;
                return newAirframe;
            }
        }

        public void SaveAirframe(AirframeState state)
        {
            if (state == null || string.IsNullOrEmpty(state.Registration)) return;

            try
            {
                string sanitizedReg = string.Concat(state.Registration.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(_baseDirPath, $"{sanitizedReg}.json");
                
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(state, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving airframe: " + ex.Message);
            }
        }

        public void SyncLocation(AirframeState state, string currentDispatchOrigin)
        {
            if (state.Events != null && state.Events.Count > 0)
            {
                var firstEvent = state.Events.First(); // Because we reversed them, index 0 is the most recent flight
                if (firstEvent.Location != currentDispatchOrigin)
                {
                    state.Events.Insert(0, new AirframeLogEvent
                    {
                        Timestamp = DateTime.UtcNow.AddMinutes(-30),
                        Type = "flight",
                        Location = currentDispatchOrigin,
                        Description = $"Ferry / Repositioning flight from {firstEvent.Location} to {currentDispatchOrigin}. Block Time: 0h45m"
                    });
                    SaveAirframe(state);
                }
            }
        }

        private AirframeState GenerateSeededAirframe(string reg, string type, string airline, string currentIcao)
        {
            // Seed logic: Hash the string registration
            int seed = reg.GetHashCode();
            var rand = new Random(seed);

            double age = Math.Round(rand.NextDouble() * 14.0 + 1.0, 1); // 1.0 to 15.0 years
            double hours = Math.Round(age * rand.Next(2800, 3500), 1);
            int cycles = (int)(hours / (1.2 + rand.NextDouble() * 0.8)); 

            var state = new AirframeState
            {
                Registration = reg,
                BaseType = type,
                Airline = airline,
                AgeInYears = age,
                DeliveryDate = DateTime.Now.AddDays(-age * 365),
                TotalHours = hours,
                TotalCycles = cycles,
                MaintenanceGrade = rand.NextDouble() > 0.85 ? "B" : "A",
                Events = new List<AirframeLogEvent>()
            };

            var lastLocation = GetRandomEuropeanIcao(rand);
            var now = DateTime.Now;

            string[] incidentPool = { "Coffee maker INOP", "Lavatory flush restricted", "Cabin scratch row 14", "Minor dent on cargo door", "Captain display flickers", "PA System intermittent", "Seat 12A recline broken" };

            for (int i = 5; i >= 1; i--)
            {
                DateTime flightDate = now.AddDays(-i).AddHours(rand.Next(-4, 4));
                string dest = (i == 1 && !string.IsNullOrEmpty(currentIcao)) ? currentIcao : GetRandomEuropeanIcao(rand);

                state.Events.Add(new AirframeLogEvent
                {
                    Timestamp = flightDate,
                    Type = "flight",
                    Location = dest,
                    Description = $"Flight from {lastLocation} to {dest}. Block Time: {rand.Next(1, 4)}h{rand.Next(10, 59):00}m"
                });

                if (rand.NextDouble() > 0.8)
                {
                    state.Events.Add(new AirframeLogEvent
                    {
                        Timestamp = flightDate.AddHours(1),
                        Type = "defect_closed",
                        Location = dest,
                        Severity = "warn",
                        Description = "Minor maintenance check completed."
                    });
                }

                lastLocation = dest;
            }

            AirframeHistoryGenerator.GenerateHistory(state, rand);
            state.Events.Reverse();

            return state;
        }

        private string GetRandomEuropeanIcao(Random rand)
        {
            string[] icaos = { "LFPG", "LEPA", "EGLL", "EHAM", "EDDF", "LEMD", "LIRF", "LPPT", "LFSB", "LSZH", "ENGM", "EIDW", "LROP", "LFMN" };
            return icaos[rand.Next(icaos.Length)];
        }
    }
}
