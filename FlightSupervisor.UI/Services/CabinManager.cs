using System;
using System.Collections.Generic;

namespace FlightSupervisor.UI.Services
{
    public enum CabinState
    {
        Idle,
        SecuringForTakeoff,
        TakeoffSecured,
        ServingMeals,
        SecuringForLanding,
        LandingSecured
    }

    public enum PassengerDemographic
    {
        Standard,
        Grumpy,
        Anxious,
        Relaxed
    }

    public class CabinManager
    {
        public CabinState State { get; private set; } = CabinState.Idle;
        public bool IsCrewSeated { get; private set; } = false;
        public event Action<string, CabinState>? OnPncStatusChanged;
        private DateTime? _securingEndTime = null;
        private CabinState _targetState = CabinState.Idle;
        public double PassengerAnxiety { get; private set; } = 0.0; // 0 to 100
        public double ComfortLevel { get; private set; } = 100.0; // 100 to 0

        public PassengerDemographic FlightDemographic { get; private set; } = PassengerDemographic.Standard;
        

        public double BaseComfortLossMultiplier { get; private set; } = 1.0;
        public double BaseAnxietySpikeMultiplier { get; private set; } = 1.0;
        public double BaseRecoveryMultiplier { get; private set; } = 1.0;
        
        public event Action<string, string>? OnCrewMessage;
        public event Action<int, string>? OnPenaltyTriggered;
        
        private DateTime _lastTurbulenceNotice = DateTime.MinValue;
        private Queue<double> _gForceHistory = new Queue<double>();
        private DateTime _lastDelayNotice = DateTime.MinValue;
        private DateTime _timeOfLastDelayPA = DateTime.MinValue;
        private bool _hasTriggeredCateringComplaint = false;
        
        private Random _rnd = new Random();
        private DateTime _lastRandomEvent = DateTime.Now;

        private static readonly (string En, string Fr)[] CruiseEvents = new[] {
            ("Captain, a passenger is feeling a bit airsick. We are taking care of it.", "Commandant, un passager a le mal de l'air. Nous nous en occupons."),
            ("Captain, the duty-free service has been completed.", "Commandant, les ventes hors-taxes sont terminées."),
            ("Captain, some passengers are asking if they can see the flight map.", "Commandant, des passagers demandent à voir la carte du vol."),
            ("Captain, meal service is complete. The cabin is clear.", "Commandant, le service des repas est terminé. La cabine est prête."),
            ("Captain, a passenger is complaining about the seat recline, but we resolved it.", "Commandant, un passager se plaignait de son siège, problème résolu."),
            ("Captain, passengers are resting and the cabin is quiet.", "Commandant, les passagers se reposent, la cabine est calme."),
            ("Captain, someone lost their phone but we found it under the seat.", "Commandant, un téléphone perdu a été retrouvé sous un siège.")
        };

        private static readonly (string En, string Fr)[] TaxiOutEvents = new[] {
            ("Captain, the cabin is secure. Passengers are settled in.", "Commandant, la cabine est prête. Les passagers sont installés."),
            ("Captain, we've started preparing the carts for the first service.", "Commandant, nous préparons les chariots pour le premier service.")
        };

        private static readonly (string En, string Fr)[] DescentEvents = new[] {
            ("Captain, we are securing the cabin for arrival.", "Commandant, nous préparons la cabine pour l'arrivée."),
            ("Captain, passengers are asking about connecting flights.", "Commandant, des passagers s'informent sur leurs correspondances."),
            ("Captain, all galley equipment is stowed and locked.", "Commandant, tout l'équipement du galley est rangé et verrouillé.")
        };
        
        // Ground Ops tracking
        public double CateringCompletion { get; set; } = 100.0;
        public double BaggageCompletion { get; set; } = 100.0;
        public bool HasBoardingStarted { get; set; } = false;

        private bool _seatbeltsOn = true;

        public void InitializeFlightDemographics(AirlineProfile profile)
        {
            if (profile == null) return;

            // HardProduct (Espace, Siège): Détermine le confort de base et la vitesse de perte
            BaseComfortLossMultiplier = 1.0 + ((5.0 - profile.HardProductScore) * 0.1); 
            
            ComfortLevel = 50.0 + (profile.HardProductScore * 5); // Max 100, Moy 75, Min 55
            
            // SoftProduct (Service, Accueil): Détermine la tolérance passager et la vitesse de recuperação
            BaseAnxietySpikeMultiplier = 1.0 + ((5.0 - profile.SoftProductScore) * 0.1);
            BaseRecoveryMultiplier = 1.0 + ((profile.SoftProductScore - 5.0) * 0.2);

            // SafetyRecord (Historique): Impacte l'anxiété latente au démarrage
            PassengerAnxiety = (10.0 - profile.SafetyRecord) * 3.0;

            int roll = _rnd.Next(100);
            if (roll < 70) FlightDemographic = PassengerDemographic.Standard;
            else if (roll < 85) FlightDemographic = PassengerDemographic.Grumpy;
            else if (roll < 95) FlightDemographic = PassengerDemographic.Anxious;
            else FlightDemographic = PassengerDemographic.Relaxed;

            switch (FlightDemographic)
            {
                case PassengerDemographic.Grumpy:
                    BaseComfortLossMultiplier *= 1.5;
                    BaseRecoveryMultiplier *= 0.5;
                    break;
                case PassengerDemographic.Anxious:
                    BaseAnxietySpikeMultiplier *= 1.5;
                    break;
                case PassengerDemographic.Relaxed:
                    BaseRecoveryMultiplier *= 1.5;
                    BaseAnxietySpikeMultiplier *= 0.5;
                    BaseComfortLossMultiplier *= 0.5;
                    break;
            }

            // Cap the gauges
            if (ComfortLevel > 100) ComfortLevel = 100;
            if (ComfortLevel < 0) ComfortLevel = 0;
            if (PassengerAnxiety < 0) PassengerAnxiety = 0;
            if (PassengerAnxiety > 100) PassengerAnxiety = 100;
        }

        public void UpdateSeatbelts(bool isOn, FlightPhase phase)
        {
            if (_seatbeltsOn != isOn)
            {
                _seatbeltsOn = isOn;
                if (phase != FlightPhase.AtGate || HasBoardingStarted)
                {
                    if (isOn)
                    {
                        OnCrewMessage?.Invoke("info", LocalizationService.Translate("Captain turned ON the Fasten Seatbelt sign.", "Le Commandant a ALLUMÉ le signal Attachez vos Ceintures."));
                        DecreaseAnxiety(10.0);
                    }
                    else
                    {
                        OnCrewMessage?.Invoke("info", LocalizationService.Translate("Captain turned OFF the Fasten Seatbelt sign.", "Le Commandant a ÉTEINT le signal Attachez vos Ceintures."));
                    }
                }
            }
        }
        
        public void HandleCommand(string command)
        {
            switch (command)
            {
                case "PREPARE_TAKEOFF":
                    IsCrewSeated = false;
                    State = CabinState.SecuringForTakeoff;
                    _targetState = CabinState.TakeoffSecured;
                    _securingEndTime = DateTime.Now.AddMinutes(2);
                    OnPncStatusChanged?.Invoke("Securing cabin for T.O...", State);
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("Cabin Crew, prepare for Takeoff.", "PNC, préparez la cabine pour le décollage."));
                    break;
                case "SEATS_TAKEOFF":
                    IsCrewSeated = true;
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("Cabin Crew, seats for Takeoff.", "PNC, aux postes pour le décollage."));
                    if (State == CabinState.TakeoffSecured) OnPncStatusChanged?.Invoke("Cabin Ready & Seated", State);
                    break;
                case "START_SERVICE":
                    IsCrewSeated = false;
                    State = CabinState.ServingMeals;
                    _securingEndTime = null;
                    OnPncStatusChanged?.Invoke("Serving Meals", State);
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("Cabin Crew, you may start the service.", "PNC, vous pouvez débuter le service."));
                    break;
                case "PREPARE_LANDING":
                    IsCrewSeated = false;
                    State = CabinState.SecuringForLanding;
                    _targetState = CabinState.LandingSecured;
                    _securingEndTime = DateTime.Now.AddMinutes(3); // Takes longer to secure for landing
                    OnPncStatusChanged?.Invoke("Securing cabin for LDG...", State);
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("Cabin Crew, prepare for Landing.", "PNC, préparez la cabine pour l'atterrissage."));
                    break;
                case "SEATS_LANDING":
                    IsCrewSeated = true;
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("Cabin Crew, seats for Landing.", "PNC, aux postes pour l'atterrissage."));
                    if (State == CabinState.LandingSecured) OnPncStatusChanged?.Invoke("Cabin Ready & Seated", State);
                    break;
                case "CANCEL_SERVICE":
                    IsCrewSeated = true;
                    State = CabinState.Idle;
                    _securingEndTime = null;
                    OnPncStatusChanged?.Invoke("Service Halted & Seated", State);
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("Cabin Crew, stop service and be seated.", "PNC, arrêtez le service et regagnez vos sièges."));
                    break;
                case "REQUEST_STATUS":
                    string reportFr;
                    string reportEn;
                    
                    if (PassengerAnxiety > 70)
                    {
                        reportEn = "Captain, it's horrible back here! A bag fell from the bin, passengers are terrified.";
                        reportFr = "Commandant, c'est l'enfer ! Une valise est tombée, des passagers sont terrifiés.";
                    }
                    else if (PassengerAnxiety > 40)
                    {
                        reportEn = "Captain, the passengers are quite nervous with the turbulence and maneuvers.";
                        reportFr = "Commandant, les passagers sont assez nerveux avec la turbulence et les manœuvres.";
                    }
                    else if (ComfortLevel < 50)
                    {
                        reportEn = "Captain, the ride is quite uncomfortable. People are complaining a bit.";
                        reportFr = "Commandant, le vol est plutôt inconfortable. Ça râle un peu en cabine.";
                    }
                    else
                    {
                        reportEn = "Captain, everything is fine in the cabin. Passengers are relaxed.";
                        reportFr = "Commandant, tout va bien. Les passagers sont détendus.";
                    }
                    
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate(reportEn, reportFr));
                    break;
            }
        }
        
        public void Tick(double gForce, double bankAngle, bool isBoarded, DateTime currentZulu, DateTime? sobt, FlightPhase phase)
        {
            if (phase == FlightPhase.AtGate && !HasBoardingStarted) return;

            if (_securingEndTime.HasValue && DateTime.Now >= _securingEndTime.Value)
            {
                State = _targetState;
                _securingEndTime = null;
                
                string msgEn = State == CabinState.TakeoffSecured ? "Captain, cabin is secure for takeoff." : "Captain, cabin is secure for landing.";
                string msgFr = State == CabinState.TakeoffSecured ? "Commandant, la cabine est prête pour le décollage." : "Commandant, cabine prête pour l'atterrissage.";
                string uiStatus = State == CabinState.TakeoffSecured ? (IsCrewSeated ? "Cabin Ready & Seated" : "Cabin Ready (Not Seated)") : (IsCrewSeated ? "Cabin Ready & Seated" : "Cabin Ready (Not Seated)");
                
                OnCrewMessage?.Invoke("green", LocalizationService.Translate(msgEn, msgFr));
                OnPncStatusChanged?.Invoke(uiStatus, State);
            }

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
                
                if (!_seatbeltsOn && (DateTime.Now - _lastTurbulenceNotice).TotalSeconds > 30)
                {
                    OnPenaltyTriggered?.Invoke(-50, LocalizationService.Translate("Safety Violation: Severe Turbulence with Seatbelts OFF!", "Violation Sécurité: Fortes turbulences avec Ceintures DÉTACHÉES!"));
                    _lastTurbulenceNotice = DateTime.Now;
                }
                else if (PassengerAnxiety > 30 && (DateTime.Now - _lastTurbulenceNotice).TotalMinutes > 5)
                {
                    OnCrewMessage?.Invoke("orange", LocalizationService.Translate(
                        "Captain, it's getting really bumpy back here. The passengers are getting anxious.",
                        "Commandant, ça secoue vraiment derrière. Les passagers sont anxieux."
                    ));
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
                    
                    if ((DateTime.Now - _timeOfLastDelayPA).TotalMinutes > 15)
                    {
                        DecreaseComfort(0.05); // Faster comfort drop due to lack of info
                        IncreaseAnxiety(0.03); // Extra anxiety
                    }
                    
                    if (PassengerAnxiety > 40 && (DateTime.Now - _lastDelayNotice).TotalMinutes > 10)
                    {
                        var mins = Math.Round(delaySpan.TotalMinutes);
                        OnCrewMessage?.Invoke("orange", LocalizationService.Translate(
                            $"Captain, we are {mins} minutes delayed past SOBT. The passengers are getting restless, an announcement would help.",
                            $"Commandant, nous avons {mins} minutes de retard sur l'horaire. Les passagers s'impatientent, une annonce du poste aiderait."
                        ));
                        _lastDelayNotice = DateTime.Now;
                    }
                }
            }
            
            // 3. Natural decay of anxiety over time if things are smooth
            if (gMax - gMin < 0.2 && Math.Abs(bankAngle) < 15.0)
            {
                if (PassengerAnxiety > 0)
                    PassengerAnxiety = Math.Max(0, PassengerAnxiety - 0.05);
                
                if (ComfortLevel < 100.0)
                    IncreaseComfort(0.02);
            }

            if (phase == FlightPhase.Cruise && !_hasTriggeredCateringComplaint && CateringCompletion < 90.0)
            {
                _hasTriggeredCateringComplaint = true;
                OnCrewMessage?.Invoke("red", LocalizationService.Translate(
                    "Captain, because the catering was aborted, we don't have enough meals for everyone. Passengers are very unhappy.",
                    "Commandant, comme le catering a été annulé, nous n'avons pas assez de repas. Les passagers sont mécontents."
                ));
                IncreaseAnxiety(30.0);
                OnPenaltyTriggered?.Invoke(-100, LocalizationService.Translate("Aborted Catering: Meal Shortage", "Catering Annulé : Manque de repas")); // Triggers a SuperScore penalty
            }

            // 5. Random Macroscopic Events (Every ~20 mins minimum spacing)
            if ((DateTime.Now - _lastRandomEvent).TotalMinutes > 20)
            {
                if (_rnd.NextDouble() < 0.05) // Low probability per tick once window is open
                {
                    string? enMsg = null, frMsg = null;
                    if (phase == FlightPhase.Cruise) { var e = CruiseEvents[_rnd.Next(CruiseEvents.Length)]; enMsg = e.En; frMsg = e.Fr; }
                    else if (phase == FlightPhase.TaxiOut) { var e = TaxiOutEvents[_rnd.Next(TaxiOutEvents.Length)]; enMsg = e.En; frMsg = e.Fr; }
                    else if (phase == FlightPhase.Descent) { var e = DescentEvents[_rnd.Next(DescentEvents.Length)]; enMsg = e.En; frMsg = e.Fr; }

                    if (enMsg != null && frMsg != null)
                    {
                        OnCrewMessage?.Invoke("info", LocalizationService.Translate(enMsg, frMsg));
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
                _timeOfLastDelayPA = DateTime.Now;
            }
            else if (announcementType == "Welcome")
            {
                DecreaseAnxiety(15.0);
            }
        }
        
        private void IncreaseAnxiety(double amount)
        {
            PassengerAnxiety += (amount * BaseAnxietySpikeMultiplier);
            if (PassengerAnxiety > 100.0) PassengerAnxiety = 100.0;
            DecreaseComfort((amount * BaseComfortLossMultiplier) * 0.5); // Anxiety directly impacts comfort
        }

        private void DecreaseAnxiety(double amount)
        {
            PassengerAnxiety -= (amount * BaseRecoveryMultiplier);
            if (PassengerAnxiety < 0.0) PassengerAnxiety = 0.0;
            IncreaseComfort((amount * BaseRecoveryMultiplier) * 0.3); // Comfort recovers slightly when anxiety drops
        }

        private void DecreaseComfort(double amount)
        {
            ComfortLevel -= (amount * BaseComfortLossMultiplier);
            if (ComfortLevel < 0.0) ComfortLevel = 0.0;
        }

        private void IncreaseComfort(double amount)
        {
            ComfortLevel += (amount * BaseRecoveryMultiplier);
            if (ComfortLevel > 100.0) ComfortLevel = 100.0;
        }

        public void CheckLostBaggageOnArrival()
        {
            if (BaggageCompletion < 99.0)
            {
                var comp = Math.Round(BaggageCompletion);
                OnCrewMessage?.Invoke("red", LocalizationService.Translate(
                    $"Arrival: A significant amount of luggage was left behind because baggage loading was aborted at {comp}%.",
                    $"Arrivée: Des bagages ont été laissés car le chargement a été annulé à {comp}%."
                ));
                OnPenaltyTriggered?.Invoke(-200, LocalizationService.Translate("Aborted Baggage: Lost Luggage Claims", "Bagages Annulés : Réclamations pertes"));
            }
        }

        public void Reset()
        {
            PassengerAnxiety = 0.0;
            ComfortLevel = 100.0;
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
