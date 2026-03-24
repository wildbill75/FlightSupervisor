using System;
using System.Collections.Generic;

namespace FlightSupervisor.UI.Services
{
    public class CabinManager
    {
        public double PassengerAnxiety { get; private set; } = 0.0; // 0 to 100
        
        public event Action<string, string>? OnCrewMessage;
        public event Action<int, string>? OnPenaltyTriggered;
        
        private DateTime _lastTurbulenceNotice = DateTime.MinValue;
        private Queue<double> _gForceHistory = new Queue<double>();
        private DateTime _lastDelayNotice = DateTime.MinValue;
        private bool _hasTriggeredCateringComplaint = false;
        
        private Random _rnd = new Random();
        private DateTime _lastRandomEvent = DateTime.Now;

        private static readonly string[] CruiseEvents = new[] {
            "Captain, a passenger is feeling a bit airsick. We are taking care of it.",
            "Captain, the duty-free service has been completed.",
            "Captain, some passengers are asking if they can see the flight map.",
            "Captain, meal service is complete. The cabin is clear.",
            "Captain, a passenger is complaining about the seat recline, but we resolved it.",
            "Captain, passengers are resting and the cabin is quiet.",
            "Captain, someone lost their phone but we found it under the seat."
        };

        private static readonly string[] TaxiOutEvents = new[] {
            "Captain, the cabin is secure. Passengers are settled in.",
            "Captain, we've started preparing the carts for the first service."
        };

        private static readonly string[] DescentEvents = new[] {
            "Captain, we are securing the cabin for arrival.",
            "Captain, passengers are asking about connecting flights.",
            "Captain, all galley equipment is stowed and locked."
        };
        
        // Ground Ops tracking
        public double CateringCompletion { get; set; } = 100.0;
        public double BaggageCompletion { get; set; } = 100.0;
        
        public void Tick(double gForce, double bankAngle, bool isBoarded, DateTime currentZulu, DateTime? sobt, FlightPhase phase)
        {
            // 1. Turbulence / Manoeuvres Anxiety
            _gForceHistory.Enqueue(gForce);
            if (_gForceHistory.Count > 20) _gForceHistory.Dequeue();
            
            double gMin = 1.0;
            double gMax = 1.0;
            if (_gForceHistory.Count > 0)
            {
                foreach (var g in _gForceHistory) { if(g < gMin) gMin=g; if(g > gMax) gMax=g; }
            }
            
            // If G-Force swings wildly (e.g. < 0.6 or > 1.4 repeatedly), anxiety spikes
            if (gMax - gMin > 0.6)
            {
                IncreaseAnxiety(0.5); // Add 0.5% per tick of severe turbulence
                if (PassengerAnxiety > 30 && (DateTime.Now - _lastTurbulenceNotice).TotalMinutes > 5)
                {
                    OnCrewMessage?.Invoke("orange", "Captain, it's getting really bumpy back here. The passengers are getting anxious.");
                    _lastTurbulenceNotice = DateTime.Now;
                }
            }
            
            // If bank angle is steep (> 28 degrees)
            if (Math.Abs(bankAngle) > 28.0)
            {
                IncreaseAnxiety(0.2);
            }
            
            // 2. Delay Anxiety (SOBT passed)
            if (isBoarded && sobt.HasValue && currentZulu > sobt.Value && phase == FlightPhase.AtGate)
            {
                var delaySpan = currentZulu - sobt.Value;
                if (delaySpan.TotalMinutes > 5)
                {
                    IncreaseAnxiety(0.02); // Slowly creeps up every tick
                    
                    if (PassengerAnxiety > 40 && (DateTime.Now - _lastDelayNotice).TotalMinutes > 10)
                    {
                        OnCrewMessage?.Invoke("orange", $"Captain, we are {Math.Round(delaySpan.TotalMinutes)} minutes delayed past SOBT. The passengers are complaining.");
                        _lastDelayNotice = DateTime.Now;
                    }
                }
            }
            
            // 3. Natural decay of anxiety over time if things are smooth
            if (PassengerAnxiety > 0 && gMax - gMin < 0.2 && Math.Abs(bankAngle) < 15.0)
            {
                PassengerAnxiety = Math.Max(0, PassengerAnxiety - 0.05);
            }

            // 4. Ground Ops Abort Consequences
            if (phase == FlightPhase.Cruise && !_hasTriggeredCateringComplaint && CateringCompletion < 90.0)
            {
                _hasTriggeredCateringComplaint = true;
                OnCrewMessage?.Invoke("red", "Captain, because the catering was aborted, we don't have enough meals for everyone. Passengers are very unhappy.");
                IncreaseAnxiety(30.0);
                OnPenaltyTriggered?.Invoke(-100, "Aborted Catering: Meal Shortage"); // Triggers a SuperScore penalty
            }

            // 5. Random Macroscopic Events (Every ~20 mins minimum spacing)
            if ((DateTime.Now - _lastRandomEvent).TotalMinutes > 20)
            {
                if (_rnd.NextDouble() < 0.05) // Low probability per tick once window is open
                {
                    string[]? eventPool = null;
                    if (phase == FlightPhase.Cruise) eventPool = CruiseEvents;
                    else if (phase == FlightPhase.TaxiOut) eventPool = TaxiOutEvents;
                    else if (phase == FlightPhase.Descent) eventPool = DescentEvents;

                    if (eventPool != null && eventPool.Length > 0)
                    {
                        var msg = eventPool[_rnd.Next(eventPool.Length)];
                        OnCrewMessage?.Invoke("info", msg);
                    }
                    _lastRandomEvent = DateTime.Now;
                }
            }
        }
        
        public void AnnounceToCabin(string announcementType)
        {
            if (announcementType == "Turbulence")
            {
                DecreaseAnxiety(25.0);
            }
            else if (announcementType == "Delay")
            {
                DecreaseAnxiety(40.0);
            }
            else if (announcementType == "Welcome")
            {
                DecreaseAnxiety(15.0);
            }
        }
        
        private void IncreaseAnxiety(double amount)
        {
            PassengerAnxiety += amount;
            if (PassengerAnxiety > 100.0) PassengerAnxiety = 100.0;
        }

        private void DecreaseAnxiety(double amount)
        {
            PassengerAnxiety -= amount;
            if (PassengerAnxiety < 0.0) PassengerAnxiety = 0.0;
        }

        public void CheckLostBaggageOnArrival()
        {
            if (BaggageCompletion < 99.0)
            {
                OnCrewMessage?.Invoke("red", $"Arrival: A significant amount of luggage was left behind because baggage loading was aborted at {Math.Round(BaggageCompletion)}%.");
                OnPenaltyTriggered?.Invoke(-200, "Aborted Baggage: Lost Luggage Claims");
            }
        }

        public void Reset()
        {
            PassengerAnxiety = 0.0;
            _gForceHistory.Clear();
            _lastTurbulenceNotice = DateTime.MinValue;
            _lastDelayNotice = DateTime.MinValue;
            _lastRandomEvent = DateTime.Now;
            _hasTriggeredCateringComplaint = false;
            CateringCompletion = 100.0;
            BaggageCompletion = 100.0;
        }
    }
}
