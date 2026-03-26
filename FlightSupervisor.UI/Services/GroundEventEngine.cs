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
        private bool _eventFiredThisFlight = false;

        public GroundEventEngine()
        {
            _eventPool = new List<GroundEvent>
            {
                new GroundEvent
                {
                    Id = "EVT_MISSING_PAX",
                    Title = "Missing Passenger",
                    Description = "A checked-in passenger is missing at the gate.",
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
                    Choices = new List<EventChoice>
                    {
                        new EventChoice { Id = "C1", Text = "Wait for replacement truck", DelayImpactSec = 1200, ComfortImpact = 0, SafetyImpact = 0, ResponseLog = "On appelle un autre camion de restauration immédiatement. Désolé pour le retard occasionné." },
                        new EventChoice { Id = "C2", Text = "Depart without meals", DelayImpactSec = 0, ComfortImpact = -50, SafetyImpact = 0, ResponseLog = "Reçu. On shunte le traiteur, fermeture des portes imminente... les PAX risquent de tirer la tronche par contre." }
                    }
                },
                new GroundEvent
                {
                    Id = "EVT_DRUNK_PAX",
                    Title = "Unruly Passenger",
                    Description = "Gate agent reports an aggressive passenger.",
                    Choices = new List<EventChoice>
                    {
                        new EventChoice { Id = "C1", Text = "Deny Boarding (Call Security)", DelayImpactSec = 600, ComfortImpact = 0, SafetyImpact = 100, ResponseLog = "Compris, c'est mort pour lui. La sécurité aéroportuaire est en route pour le sortir du terminal." },
                        new EventChoice { Id = "C2", Text = "Let them board and hope for the best", DelayImpactSec = 0, ComfortImpact = -100, SafetyImpact = -500, ResponseLog = "Vous êtes sûr ? OK... On le fait monter à bord. Bonne chance à vos PNC en cabine." }
                    }
                }
            };
        }

        public void Reset()
        {
            _eventFiredThisFlight = false;
        }

        public void Tick(int probabilityPercent, AirlineProfile? currentAirline)
        {
            if (_eventFiredThisFlight || currentAirline == null) return;
            if (probabilityPercent <= 0) return;

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
            _eventFiredThisFlight = true;

            var evt = _eventPool[_rnd.Next(_eventPool.Count)];

            // Create payload DTO
            var payload = new GroundEventDTO
            {
                Id = evt.Id,
                Title = evt.Title,
                Description = evt.Description,
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
