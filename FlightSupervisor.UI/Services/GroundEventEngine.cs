using System;
using System.Collections.Generic;
using FlightSupervisor.UI.Models;

namespace FlightSupervisor.UI.Services
{
    public class GroundEventEngine
    {
        private Random _rnd = new Random();
        public event Action<GroundEventDTO>? OnEventTriggered;

        private List<GroundEvent> _eventPool;
        private DateTime _lastEventTime = DateTime.MinValue;

        public GroundEventEngine()
        {
            _eventPool = new List<GroundEvent>
            {
                new GroundEvent
                {
                    Id = "EVT_MISSING_PAX",
                    Title = "Missing Passenger",
                    Description = "A checked-in passenger is missing at the gate.",
                    RequiredActivePhase = "Boarding",
                    Choices = new List<EventChoice>
                    {
                        new EventChoice { Id = "C1", Text = "Wait and search for passenger", DelayImpactSec = 900, ComfortImpact = 0, SafetyImpact = 0, ResponseLog = "Copie Commandant, on envoie le personnel au sol à la recherche du passager. En attente..." },
                        new EventChoice { Id = "C2", Text = "Offload baggage and depart", DelayImpactSec = 300, ComfortImpact = -10, SafetyImpact = 0, ResponseLog = "Bien reçu. On applique la procédure de sûreté, fouille de soute en cours pour débarquer ses bagages." }
                    }
                },
                new GroundEvent
                {
                    Id = "EVT_CATERING_BROKEN",
                    Title = "Catering Truck Issue",
                    Description = "The catering truck won't start. Departure will be delayed to get a replacement.",
                    RequiredActivePhase = "Catering",
                    Choices = new List<EventChoice>
                    {
                        new EventChoice { Id = "C1", Text = "Wait for replacement truck", DelayImpactSec = 1200, ComfortImpact = 0, SafetyImpact = 0, ResponseLog = "On appelle un autre camion de restauration immédiatement. Désolé pour le retard occasionné." },
                        new EventChoice { Id = "C2", Text = "Depart without meals", DelayImpactSec = 0, ComfortImpact = -50, SafetyImpact = 0, ResponseLog = "Reçu. On shunte le traiteur, fermeture des portes imminente... les PAX risquent de tirer la tronche par contre." }
                    }
                }
            };
        }

        public void Reset()
        {
            _lastEventTime = DateTime.MinValue;
        }

        public void Tick(int probabilityPercent, AirlineProfile? currentAirline)
        {
            if (currentAirline == null) return;
            if (probabilityPercent <= 0) return;
            if ((DateTime.Now - _lastEventTime).TotalMinutes < 5) return;

            // At 100% probability => 1 in 150 chance per second -> approx 2.5 mins average.
            // At default 20% => 1 in 750 chance per second -> approx 12.5 mins average.
            int threshold = 15000 / Math.Max(1, probabilityPercent); 
            if (_rnd.Next(0, threshold) == 0)
            {
                FireRandomEvent(currentAirline);
            }
        }

        public void ForceEvent(AirlineProfile currentAirline)
        {
            FireRandomEvent(currentAirline);
        }

        private void FireRandomEvent(AirlineProfile airline)
        {
            if (_eventPool.Count == 0 || airline == null) return;
            _lastEventTime = DateTime.Now;

            var evt = _eventPool[_rnd.Next(_eventPool.Count)];

            var payload = new GroundEventDTO
            {
                Id = evt.Id,
                Title = evt.Title,
                Description = evt.Description,
                ServiceName = evt.RequiredActivePhase, // Add the mapping
                Choices = new List<EventChoiceDTO>()
            };

            foreach (var c in evt.Choices)
            {
                payload.Choices.Add(new EventChoiceDTO
                {
                    Id = c.Id,
                    Text = c.Text,
                    ColorClass = c.EvaluatePolicyAlignment(airline)
                });
            }

            OnEventTriggered?.Invoke(payload);
        }

        public GroundEvent? GetEventById(string id)
        {
            return _eventPool.Find(x => x.Id == id);
        }
    }
}
