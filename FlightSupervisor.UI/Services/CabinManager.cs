using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightSupervisor.UI.Services
{
    public enum CabinState
    {
        Idle,
        Boarding,
        SecuringForTakeoff,
        TakeoffSecured,
        ServingMeals,
        SecuringForLanding,
        LandingSecured
    }

    public class PassengerState
    {
        public string Seat { get; set; } = "1A";
        public bool IsSeatbeltFastened { get; set; } = true;
        public bool IsInjured { get; set; } = false;
        public string InjuryType { get; set; } = "";
        public double IndividualAnxiety { get; set; } = 0.0;
        public PassengerDemographic Demographic { get; set; } = PassengerDemographic.Standard;
        public bool IsBoarded { get; set; } = false;
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
        public double PassengerAnxiety { get; private set; } = 0.0;
        public double ComfortLevel { get; private set; } = 100.0;

        public double BaseAnxietySpikeMultiplier { get; set; } = 1.0;
        public double BaseComfortLossMultiplier { get; set; } = 1.0;
        public double BaseRecoveryMultiplier { get; set; } = 1.0;
        public bool IsSilencePenaltyActive { get; private set; } = false;

        public List<PassengerState> PassengerManifest { get; private set; } = new List<PassengerState>();
        public bool IsCrewSeated { get; private set; } = false;
        public double SecuringProgress { get; private set; } = 0.0;

        private FlightPhase _lastPhase = FlightPhase.AtGate;
        private DateTime _lastPhaseChangeTime = DateTime.MinValue;

        public delegate void CrewMessageEventHandler(string color, string message, List<string>? audioSequence = null);
        public event CrewMessageEventHandler? OnCrewMessage;
        public event Action<int, string>? OnPenaltyTriggered;
        public event Action<int, string>? OnOperationBonusTriggered;
        public event Action<string, CabinState>? OnPncStatusChanged;
        public event Action? OnMedicalEmergencyRequested;

        public HashSet<string> IssuedCommands => _issuedCommands;

        private Queue<double> _gForceHistory;
        private DateTime _lastSecureProgressUpdate = DateTime.MinValue;
        private DateTime _lastServiceHaltNotice = DateTime.MinValue;
        private DateTime _lastSafetyCheckNotice = DateTime.MinValue;
        private DateTime _lastTurbulenceNotice = DateTime.MinValue;
        private DateTime _lastDelayNotice = DateTime.MinValue;
        private DateTime _timeOfLastDelayPA = DateTime.MinValue;
        private bool _hasTriggeredCateringComplaint = false;
        private DateTime? _silenceTimerStart = null;
        private DateTime? _turbulenceReactionTimerStart = null;
        private bool _hasPenalizedTurbulenceReaction = false;
        private bool _hasRewardedTurbulenceReaction = false;

        private Random _rnd = new Random();
        private DateTime _lastRandomEvent = DateTime.Now;
        private DateTime _lastReportRequest = DateTime.MinValue;
        private DateTime? _strategicPenaltyEndTime = null;

        private double _currentDelayMinutes = 0;
        private double _currentSecuringRate = 0;
        private bool _isSecuring = false;
        public bool IsSecuringHalted { get; private set; } = false;
        private CabinState _targetState = CabinState.TakeoffSecured;

        public double CateringCompletion { get; set; } = 100.0;
        public double BaggageCompletion { get; set; } = 100.0;

        public bool IsServiceHalted { get; private set; } = false;
        public bool HasBoardingStarted { get; set; } = false;
        
        public double SecondsSinceLastReport => _lastReportRequest == DateTime.MinValue ? 9999 : (DateTime.Now - _lastReportRequest).TotalSeconds;
        public FlightSupervisor.UI.Models.SimBrief.SimBriefResponse? CurrentFlight { get; set; }

        private HashSet<string> _issuedCommands = new HashSet<string>();

        private double _comfortSum = 0;
        private int _comfortSamples = 0;
        public double AverageComfort => _comfortSamples == 0 ? ComfortLevel : (_comfortSum / _comfortSamples);

        public bool IsSeatbeltsOn => _seatbeltsOn;
        private bool _seatbeltsOn = true;

        public double InFlightServiceProgress { get; private set; } = 0.0;
        public bool IsSatietyActive { get; private set; } = false;

        // Audio Properties
        public string ActivePncVoiceId { get; set; } = "female_1";
        public string ActiveAirlineId { get; set; } = "air_france";
        private Dictionary<string, string> _audioExtensions = new Dictionary<string, string>();
        
        private DateTime _lastBoardingTick = DateTime.MaxValue;

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

        public CabinManager()
        {
            _gForceHistory = new Queue<double>();
            
            // Build known extensions cache manually for safety vs wav
            _audioExtensions["pa_welcome_intro"] = ".wav";
            _audioExtensions["airline_air_france"] = ".wav";
            _audioExtensions["pa_bound_for"] = ".wav";
            _audioExtensions["dest_toulouse_blagnac"] = ".wav";
            _audioExtensions["pa_welcome_luggage"] = ".wav";
            _audioExtensions["pa_welcome_seatbelts"] = ".wav";
            _audioExtensions["pa_safety_demo"] = ".mp3";
            _audioExtensions["pa_descent_intro"] = ".wav";
            _audioExtensions["pa_descent_secure"] = ".wav";
            _audioExtensions["pa_arrival_welcome"] = ".wav";
            _audioExtensions["pa_arrival_time_is"] = ".wav";
            _audioExtensions["pa_arrival_remain_seated"] = ".wav";
            _audioExtensions["pa_turbulence_warning"] = ".mp3";
            _audioExtensions["pa_service_start"] = ".mp3";
        }

        public void SetCurrentAirline(string airlineId)
        {
            if (!string.IsNullOrEmpty(airlineId))
                ActiveAirlineId = airlineId.ToLower().Replace(" ", "_");
        }

        private List<string> FormatAudioSequence(List<string> baseSequence)
        {
            if (baseSequence == null || baseSequence.Count == 0) return null;
            var formatted = new List<string>();
            string prefix = $"airlines/{ActiveAirlineId}/pnc/en/{ActivePncVoiceId}/";
            
            foreach(var snd in baseSequence)
            {
                if (snd.StartsWith("time_")) {
                    formatted.Add(prefix + snd + ".mp3"); 
                } 
                else if (snd.StartsWith("dest_")) {
                    formatted.Add(prefix + snd + ".wav");
                }
                else if (snd.StartsWith("airline_")) {
                    formatted.Add(prefix + snd + ".wav");
                }
                else 
                {
                    string ext = _audioExtensions.ContainsKey(snd) ? _audioExtensions[snd] : ".mp3";
                    formatted.Add(prefix + snd + ext);
                }
            }
            return formatted;
        }

        public void InitializeFlightDemographics(FlightSupervisor.UI.Services.AirlineProfile profile, FlightSupervisor.UI.Services.ManifestData manifestData = null)
        {
            if (profile == null) return;
            PassengerManifest.Clear();
            
            if (manifestData != null && manifestData.Passengers != null && manifestData.Passengers.Count > 0)
            {
                foreach (var pax in manifestData.Passengers)
                {
                    var p = new PassengerState() { Seat = pax.Seat, IsBoarded = false };
                    double r = _rnd.NextDouble();
                    if (r < 0.1) p.Demographic = PassengerDemographic.Grumpy;
                    else if (r < 0.25) p.Demographic = PassengerDemographic.Anxious;
                    else if (r < 0.4) p.Demographic = PassengerDemographic.Relaxed;
                    PassengerManifest.Add(p);
                }
            }
            else
            {
                int currentPax = 150;
                for (int i = 0; i < currentPax; i++)
                {
                    var p = new PassengerState() { Seat = $"{i+1}A", IsBoarded = false };
                    double r = _rnd.NextDouble();
                    if (r < 0.1) p.Demographic = PassengerDemographic.Grumpy;
                    else if (r < 0.25) p.Demographic = PassengerDemographic.Anxious;
                    else if (r < 0.4) p.Demographic = PassengerDemographic.Relaxed;
                    PassengerManifest.Add(p);
                }
            }
        }

        public void UpdateSeatbelts(bool on, FlightPhase phase)
        {
            _seatbeltsOn = on;
        }

        public void HandleCommand(string command)
        {
            switch (command)
            {
                case "SEATBELT_ON":
                    _seatbeltsOn = true;
                    OnPncStatusChanged?.Invoke("Seatbelts Validated", State);
                    break;
                case "SEATBELT_OFF":
                    _seatbeltsOn = false;
                    OnPncStatusChanged?.Invoke("Seatbelts Off", State);
                    break;
                case "PREP_TAKEOFF":
                    _isSecuring = true;
                    _currentSecuringRate = 3.3; 
                    _targetState = CabinState.TakeoffSecured;
                    State = CabinState.SecuringForTakeoff;
                    SecuringProgress = 0;
                    IsCrewSeated = false;
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("Cabin Crew, prepare cabin for takeoff.", "PNC, préparez la cabine pour le décollage."));
                    OnPncStatusChanged?.Invoke("Securing Cabin...", State);
                    break;
                case "PREP_LANDING":
                    _isSecuring = true;
                    _currentSecuringRate = 5.0; 
                    _targetState = CabinState.LandingSecured;
                    State = CabinState.SecuringForLanding;
                    SecuringProgress = 0;
                    IsCrewSeated = false;
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("Cabin Crew, prepare cabin for landing.", "PNC, préparez la cabine pour l'atterrissage."));
                    OnPncStatusChanged?.Invoke("Securing Cabin...", State);
                    break;
                case "START_SERVICE":
                    if (!_seatbeltsOn)
                    {
                        State = CabinState.ServingMeals;
                        InFlightServiceProgress = 0;
                        IsServiceHalted = false;
                        IsCrewSeated = false;
                        OnCrewMessage?.Invoke("info", LocalizationService.Translate("Cabin Crew, you may commence the in-flight service.", "PNC, vous pouvez débuter le service en vol."));
                        OnPncStatusChanged?.Invoke("Serving Meals", State);
                    }
                    else
                    {
                        OnCrewMessage?.Invoke("orange", LocalizationService.Translate("Captain, we cannot start service while the seatbelt sign is ON.", "Commandant, impossible de débuter le service avec les ceintures allumées."));
                    }
                    break;
                case "SEATS_LANDING":
                    IsCrewSeated = true;
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("Cabin Crew, seats for Landing.", "PNC, aux postes pour l'atterrissage."));
                    if (State == CabinState.LandingSecured) OnPncStatusChanged?.Invoke("Cabin Ready & Seated", State);
                    break;
                case "CANCEL_SERVICE":
                    IsCrewSeated = true;
                    State = CabinState.Idle;
                    _isSecuring = false;
                    OnPncStatusChanged?.Invoke("Service Halted & Seated", State);
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("Cabin Crew, stop service and be seated.", "PNC, arrêtez le service et regagnez vos sièges."));
                    break;
            }
        }
        
        public void Tick(double gForce, double bankAngle, bool isBoarded, DateTime currentZulu, DateTime? sobt, FlightPhase phase, double groundSpeed, double altitude, double verticalSpeed, bool isCrisisActive)
        {
            if (phase == FlightPhase.AtGate && !HasBoardingStarted) return;

            // Progressive Boarding Logic (Phase 3)
            if (phase == FlightPhase.AtGate && HasBoardingStarted)
            {
                if (_lastBoardingTick == DateTime.MaxValue) 
                {
                    _lastBoardingTick = DateTime.Now;
                    foreach(var px in PassengerManifest) { px.IsBoarded = false; px.IsSeatbeltFastened = false; }
                }

                if ((DateTime.Now - _lastBoardingTick).TotalSeconds >= 1.0)
                {
                    _lastBoardingTick = DateTime.Now;
                    var unboarded = PassengerManifest.Where(p => !p.IsBoarded).ToList();
                    if (unboarded.Count > 0)
                    {
                        int toBoard = _rnd.Next(1, 4);
                        for(int i = 0; i < Math.Min(toBoard, unboarded.Count); i++)
                        {
                            unboarded[i].IsBoarded = true;
                        }
                    }
                }
            }
            else
            {
                if (_lastBoardingTick != DateTime.MaxValue)
                {
                    foreach (var p in PassengerManifest) p.IsBoarded = true;
                    _lastBoardingTick = DateTime.MaxValue;
                }
            }

            // Idle Noise Generator
            if (PassengerAnxiety < 2.0 && ComfortLevel >= 95.0)
            {
                if (_rnd.NextDouble() < 0.05) PassengerAnxiety += (_rnd.NextDouble() * 0.2);
            }

            _comfortSum += ComfortLevel;
            _comfortSamples++;

            if (phase != _lastPhase)
            {
                if (phase == FlightPhase.Cruise)
                {
                    DecreaseAnxiety(20.0);
                    OnCrewMessage?.Invoke("green", LocalizationService.Translate("Passengers are relieved to reach cruise altitude.", "Les passagers sont soulagés d'avoir atteint l'altitude de croisière."), null);
                }
                else if (phase == FlightPhase.TaxiIn)
                {
                    DecreaseAnxiety(40.0);
                    OnCrewMessage?.Invoke("green", LocalizationService.Translate("Passengers are very relieved to be back on the ground safely.", "Les passagers sont très soulagés d'être à nouveau au sol en sécurité."), null);
                }
                _lastPhase = phase;
                _lastPhaseChangeTime = DateTime.Now;
            }

            // --- Crisis & Silence Penalty Logic ---
            bool isSevereTurbulence = phase != FlightPhase.AtGate && isCrisisActive; 
            
            if (isCrisisActive)
            {
                if (_silenceTimerStart == null) _silenceTimerStart = DateTime.Now;
                else if ((DateTime.Now - _silenceTimerStart.Value).TotalSeconds > 120)
                {
                    IsSilencePenaltyActive = true;
                }
            }
            else
            {
                ResetSilenceTimer();
            }

            UpdatePassengerStates(phase, isSevereTurbulence);
            
            if (isSevereTurbulence)
            {
                if (_turbulenceReactionTimerStart == null)
                {
                    _turbulenceReactionTimerStart = DateTime.Now;
                    _hasPenalizedTurbulenceReaction = false;
                    _hasRewardedTurbulenceReaction = false;
                }
                else
                {
                    double elapsed = (DateTime.Now - _turbulenceReactionTimerStart.Value).TotalSeconds;
                    if (elapsed > 30 && !_hasPenalizedTurbulenceReaction)
                    {
                        _hasPenalizedTurbulenceReaction = true;
                        OnPenaltyTriggered?.Invoke(-100, LocalizationService.Translate("Pilot Inaction: No PA during severe turbulence/crisis", "Inaction Pilote : Pas d'annonce PA pendant la crise/turbulence"));
                        IncreaseAnxiety(20.0, phase, isCrisisActive);
                    }
                }
            }
            else
            {
                _turbulenceReactionTimerStart = null;
            }

            if (_isSecuring)
            {
                bool speedHalt = (phase == FlightPhase.TaxiOut || phase == FlightPhase.TaxiIn) && groundSpeed > 25.0;
                bool gHalt = gForce > 1.2 || gForce < 0.8;

                IsSecuringHalted = speedHalt || gHalt;

                if (!IsSecuringHalted)
                {
                    double effectiveRate = _currentSecuringRate;
                    if (_strategicPenaltyEndTime.HasValue && DateTime.Now < _strategicPenaltyEndTime.Value)
                    {
                        effectiveRate *= 0.5; // Strategic Penalty: PNC distracted by Intercom Query
                    }
                    SecuringProgress += effectiveRate;
                    if (SecuringProgress >= 100.0)
                    {
                        SecuringProgress = 100.0;
                        _isSecuring = false;
                        
                        string msgEn = _targetState == CabinState.TakeoffSecured ? "Cabin is now secure and ready for takeoff." : "Cabin is now secure for landing.";
                        string msgFr = _targetState == CabinState.TakeoffSecured ? "La cabine est maintenant prête et sécurisée pour le décollage." : "La cabine est sécurisée pour l'atterrissage.";
                        
                        OnCrewMessage?.Invoke("green", LocalizationService.Translate(msgEn, msgFr), null);
                        OnOperationBonusTriggered?.Invoke(0, "pnc_ready_chime"); 

                        State = _targetState;

                        string uiStatus = State == CabinState.TakeoffSecured ? (IsCrewSeated ? "Cabin Ready & Seated" : "Cabin Ready (Not Seated)") : (IsCrewSeated ? "Cabin Ready & Seated" : "Cabin Ready (Not Seated)");
                        OnPncStatusChanged?.Invoke(uiStatus, State);
                    }
                }
            }

            _gForceHistory.Enqueue(gForce);
            if (_gForceHistory.Count > 20) _gForceHistory.Dequeue();
            
            double gMin = 1.0;
            double gMax = 1.0;
            if (_gForceHistory.Count > 0)
            {
                foreach (var g in _gForceHistory) { if(g < gMin) gMin=g; if(g > gMax) gMax=g; }
            }
            
            if (phase != FlightPhase.AtGate && (gMax - gMin > 0.6))
            {
                IncreaseAnxiety(0.5, phase, isCrisisActive); 
                
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
                    ), null);
                    _lastTurbulenceNotice = DateTime.Now;
                }
            }
            
            if (phase != FlightPhase.AtGate && Math.Abs(bankAngle) > 28.0)
            {
                IncreaseAnxiety(0.2, phase, isCrisisActive);
            }
            
            if (isBoarded && sobt.HasValue && currentZulu > sobt.Value && phase == FlightPhase.AtGate)
            {
                var delaySpan = currentZulu - sobt.Value;
                _currentDelayMinutes = delaySpan.TotalMinutes;
                if (_currentDelayMinutes > 5)
                {
                    double anxInc = 0.005; 
                    double comfDec = 0.005;
                    
                    if (delaySpan.TotalMinutes > 15) { anxInc = 0.01; comfDec = 0.01; }
                    
                    if ((DateTime.Now - _timeOfLastDelayPA).TotalMinutes > 15)
                    {
                        comfDec *= 2.0; 
                        anxInc *= 2.0;
                    }
                    
                    if (PassengerAnxiety < 50.0) IncreaseAnxiety(anxInc, phase, isCrisisActive);
                    if (ComfortLevel > 50.0) DecreaseComfort(comfDec);
                    
                    if (PassengerAnxiety > 30 && (DateTime.Now - _lastDelayNotice).TotalMinutes > 10)
                    {
                        var mins = Math.Round(delaySpan.TotalMinutes);
                        OnCrewMessage?.Invoke("orange", LocalizationService.Translate(
                            $"Captain, we are {mins} minutes delayed past SOBT. The passengers are getting restless, an announcement would help.",
                            $"Commandant, nous avons {mins} minutes de retard sur l'horaire. Les passagers s'impatientent, une annonce du poste aiderait."
                        ), null);
                        _lastDelayNotice = DateTime.Now;
                    }
                }
            }

            if (State == CabinState.ServingMeals)
            {
                bool isHaltedByBelts = _seatbeltsOn;
                bool isHaltedByAlt = altitude < 10000 && phase != FlightPhase.Cruise; 
                bool isHaltedByDescent = verticalSpeed < -1000 && altitude < 20000;
                bool isHaltedByTurbulence = gMax - gMin > 0.6; 

                if (isHaltedByTurbulence && !IsServiceHalted)
                {
                    IsServiceHalted = true;
                    OnCrewMessage?.Invoke("orange", LocalizationService.Translate(
                        "Captain, we are suspending the service immediately due to severe turbulence. The crew is taking their seats.",
                        "Commandant, nous suspendons le service immédiatement à cause des fortes turbulences. L'équipage regagne ses sièges."), null);
                }

                if (!IsServiceHalted && !isHaltedByBelts && !isHaltedByAlt && !isHaltedByDescent)
                {
                    double baseRate = 0.5 * (100.0 / Math.Max(1, PassengerManifest.Count));
                    InFlightServiceProgress += baseRate;
                    if (InFlightServiceProgress >= 100.0)
                    {
                        InFlightServiceProgress = 100.0;
                        State = CabinState.Idle;
                        OnCrewMessage?.Invoke("green", LocalizationService.Translate("Meal service is complete, cabin is clear.", "Le service des repas est terminé, la cabine est dégagée."), null);
                        OnPncStatusChanged?.Invoke("Idle", State);
                    }
                }
            }
            
            if (phase == FlightPhase.Cruise && !_hasTriggeredCateringComplaint && CateringCompletion < 90.0)
            {
                _hasTriggeredCateringComplaint = true;
                OnCrewMessage?.Invoke("red", LocalizationService.Translate(
                    "Captain, because the catering was aborted, we don't have enough meals for everyone. Passengers are very unhappy.",
                    "Commandant, comme le catering a été annulé, nous n'avons pas assez de repas. Les passagers sont mécontents."
                ), null);
                IncreaseAnxiety(30.0, phase, isCrisisActive);
                OnPenaltyTriggered?.Invoke(-100, LocalizationService.Translate("Aborted Catering: Meal Shortage", "Catering Annulé : Manque de repas")); 
            }

            if ((DateTime.Now - _lastRandomEvent).TotalMinutes > 20)
            {
                if (_rnd.NextDouble() < 0.05) 
                {
                    string? enMsg = null, frMsg = null;
                    if (phase == FlightPhase.Cruise) { var e = CruiseEvents[_rnd.Next(CruiseEvents.Length)]; enMsg = e.En; frMsg = e.Fr; }
                    else if (phase == FlightPhase.TaxiOut) { var e = TaxiOutEvents[_rnd.Next(TaxiOutEvents.Length)]; enMsg = e.En; frMsg = e.Fr; }
                    else if (phase == FlightPhase.Descent) { var e = DescentEvents[_rnd.Next(DescentEvents.Length)]; enMsg = e.En; frMsg = e.Fr; }

                    if (enMsg != null && frMsg != null)
                    {
                        OnCrewMessage?.Invoke("info", LocalizationService.Translate(enMsg, frMsg), null);
                    }
                    _lastRandomEvent = DateTime.Now;
                }
            }
        }
        
        public void AnnounceToCabin(string announcementType)
        {
            if (!_issuedCommands.Contains("PA_" + announcementType))
            {
                _issuedCommands.Add("PA_" + announcementType);
                OnOperationBonusTriggered?.Invoke(25, "Passenger Announcement: " + announcementType);
            }

            if (announcementType == "Turbulence")
            {
                DecreaseAnxiety(25.0);
                var audioSeq = new List<string> { "pa_turbulence_warning" };
                OnCrewMessage?.Invoke("orange", LocalizationService.Translate("PA: Please return to your seats and fasten your seatbelts.", "PA: Veuillez regagner vos sièges et attacher vos ceintures."), FormatAudioSequence(audioSeq));
            }
            else if (announcementType == "Delay")
            {
                DecreaseAnxiety(40.0);
                _timeOfLastDelayPA = DateTime.Now;
                var minsStr = Math.Round(_currentDelayMinutes).ToString();
                var audioSeq = new List<string> { "pa_delay_apology" };
                OnCrewMessage?.Invoke("orange", LocalizationService.Translate("PA: Apologies for the delay, we will be departing shortly.", "PA: Toutes nos excuses pour ce retard, nous partons bientôt."), FormatAudioSequence(audioSeq));
            }
        }

        public void AnnounceWelcome(string destName, string flightTime, bool badWeather)
        {
            if (!_issuedCommands.Contains("PA_Welcome"))
            {
                _issuedCommands.Add("PA_Welcome");
                OnOperationBonusTriggered?.Invoke(25, "Passenger Announcement: Welcome");
            }

            if (!badWeather) {
                DecreaseAnxiety(20.0);
            } else {
                IncreaseAnxiety(15.0, FlightPhase.AtGate, false);
            }

            string wxcText = !badWeather ? "looking great" : "quite poor today";
            string wxcFr = !badWeather ? "très bonne" : "assez mauvaise aujourd'hui";

            string destKey = "dest_" + destName.ToLower().Replace(" ", "_").Replace("-", "_");
            var audioSeq = new List<string> { "pa_welcome_intro", "airline_" + ActiveAirlineId, "pa_bound_for", destKey, "pa_welcome_luggage", "pa_welcome_seatbelts" };
            OnCrewMessage?.Invoke("green", LocalizationService.Translate(
                $"PA: Welcome aboard our flight to {destName}. Our flight time will be approx {flightTime}. The weather at our destination is currently {wxcText}.", 
                $"PA: Bienvenue à bord de ce vol à destination de {destName}. Notre temps de vol sera d'environ {flightTime}. La météo à l'arrivée s'annonce {wxcFr}."
            ), FormatAudioSequence(audioSeq));
        }

        public void AnnounceDescent(string destName)
        {
            if (!_issuedCommands.Contains("PA_Descent"))
            {
                _issuedCommands.Add("PA_Descent");
                OnOperationBonusTriggered?.Invoke(25, "Passenger Announcement: Descent");
            }

            DecreaseAnxiety(15.0);
            string destKey = "dest_" + destName.ToLower().Replace(" ", "_").Replace("-", "_");
            var audioSeq = new List<string> { "pa_descent_intro", destKey, "pa_descent_secure" };
            OnCrewMessage?.Invoke("green", LocalizationService.Translate(
                $"PA: We are beginning our descent into {destName}. Please return to your seats and fasten your seatbelts.", 
                $"PA: Nous débutons notre descente vers {destName}. Veuillez regagner vos sièges et attacher vos ceintures."
            ), FormatAudioSequence(audioSeq));
        }
        
        private void IncreaseAnxiety(double amount, FlightPhase phase, bool isCrisisActive)
        {
            double previousAnxiety = PassengerAnxiety;
            double multiplier = BaseAnxietySpikeMultiplier;
            if (IsSilencePenaltyActive) multiplier *= 2.0;

            double inc = (amount * multiplier);
            bool isOnGround = phase == FlightPhase.AtGate || phase == FlightPhase.Pushback || phase == FlightPhase.TaxiOut || phase == FlightPhase.TaxiIn || phase == FlightPhase.Arrived;
                              
            if (isOnGround && (PassengerAnxiety + inc) > 60.0) 
            {
                PassengerAnxiety = Math.Max(60.0, previousAnxiety); 
            }
            else if (!isCrisisActive && (PassengerAnxiety + inc) > 90.0)
            {
                 PassengerAnxiety = Math.Max(90.0, previousAnxiety);
            }
            else
            {
                PassengerAnxiety += inc;
            }

            if (IsSatietyActive && phase == FlightPhase.Cruise) amount *= 0.5; 
            
            if (PassengerAnxiety > 100.0) PassengerAnxiety = 100.0;
            DecreaseComfort((amount * BaseComfortLossMultiplier) * 0.5); 
        }

        private void DecreaseAnxiety(double amount)
        {
            PassengerAnxiety -= (amount * BaseRecoveryMultiplier);
            if (PassengerAnxiety < 0.0) PassengerAnxiety = 0.0;
            IncreaseComfort((amount * BaseRecoveryMultiplier) * 0.3); 
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

        public void ApplyComfortImpact(double delta)
        {
            if (delta < 0) DecreaseComfort(Math.Abs(delta));
            else IncreaseComfort(delta);
        }

        public void CheckLostBaggageOnArrival()
        {
            if (BaggageCompletion < 99.0)
            {
                var comp = Math.Round(BaggageCompletion);
                OnCrewMessage?.Invoke("red", LocalizationService.Translate(
                    $"Arrival: A significant amount of luggage was left behind because baggage loading was aborted at {comp}%.",
                    $"Arrivée: Des bagages ont été laissés car le chargement a été annulé à {comp}%."
                ), null);
                OnPenaltyTriggered?.Invoke(-200, LocalizationService.Translate("Aborted Baggage: Lost Luggage Claims", "Bagages Annulés : Réclamations pertes"));
            }
        }
        
        private void ResetSilenceTimer()
        {
            _silenceTimerStart = null;
            IsSilencePenaltyActive = false;
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
            _lastPhase = FlightPhase.AtGate;
            _comfortSum = 0;
            _comfortSamples = 0;
            _issuedCommands.Clear();
        }
        
        private void UpdatePassengerStates(FlightPhase phase, bool isSevere)
        {
            int injuryCount = 0;
            foreach (var p in PassengerManifest)
            {
                if (_seatbeltsOn && !p.IsSeatbeltFastened)
                {
                    if (_rnd.Next(100) < 5) p.IsSeatbeltFastened = true; 
                }

                if (isSevere && !p.IsSeatbeltFastened && !p.IsInjured)
                {
                    double injuryChance = 0.5; 
                    if (_rnd.NextDouble() < injuryChance / 100.0) 
                    {
                        p.IsInjured = true;
                        p.InjuryType = "Head trauma"; 
                        injuryCount++;
                        OnPenaltyTriggered?.Invoke(-500, $"PASSENGER INJURED: {p.Seat} ({p.InjuryType})");
                        OnCrewMessage?.Invoke("red", LocalizationService.Translate($"Captain, we have an injured passenger in seat {p.Seat}! They hit their head during the turbulence.", $"Commandant, un passager est blessé au siège {p.Seat} ! Il s'est cogné la tête pendant les turbulences."), null);
                    }
                }

                if (isSevere && !p.IsSeatbeltFastened) p.IndividualAnxiety += 2.0;
                if (p.IndividualAnxiety > 100) p.IndividualAnxiety = 100;
            }

            if (injuryCount > 0)
            {
                OnMedicalEmergencyRequested?.Invoke();
            }
        }

        public void ToggleServiceInterruption()
        {
            IsServiceHalted = !IsServiceHalted;
            string statusEn = IsServiceHalted ? "suspended" : "resumed";
            string statusFr = IsServiceHalted ? "suspendu" : "repris";
            
            OnCrewMessage?.Invoke(IsServiceHalted ? "orange" : "green", LocalizationService.Translate(
                $"Captain, the in-flight service has been {statusEn} at your request.",
                $"Commandant, le service en cabine a été {statusFr} à votre demande."), null);
        }

        public bool RequestCabinReport(FlightPhase phase, bool isCrisisActive)
        {
            if ((DateTime.Now - _lastReportRequest).TotalMinutes < 2)
            {
                return false;
            }

            _lastReportRequest = DateTime.Now;

            int boardedCount = PassengerManifest.Count(p => p.IsBoarded);
            if (phase == FlightPhase.AtGate && boardedCount < PassengerManifest.Count)
            {
                string repEn = boardedCount == 0 
                    ? "Cabin checks are complete. We are ready when you are to begin boarding." 
                    : "We are waiting for boarding to finish, Captain.";
                
                string repFr = boardedCount == 0 
                    ? "Les vérifications cabine sont terminées. Nous sommes prêts à débuter l'embarquement." 
                    : "Nous attendons la fin de l'embarquement, Commandant.";
                
                string audioId = boardedCount == 0 ? "pnc_report_preboard" : "pnc_report_boarding";
                
                OnCrewMessage?.Invoke("info", LocalizationService.Translate(repEn, repFr), FormatAudioSequence(new List<string> { audioId }));
                return true;
            }

            if (_isSecuring && SecuringProgress < 100.0)
            {
                _strategicPenaltyEndTime = DateTime.Now.AddSeconds(15);
            }

            string reportEn = "Cabin is clear and quiet, Captain.";
            string reportFr = "La cabine est calme et prête, Commandant.";
            var audioSeq = new List<string> { "pnc_report_idle" };

            if (_isSecuring && phase == FlightPhase.TaxiOut)
            {
                reportEn = "We're almost finished with the bins and oversized luggage. nearly ready.";
                reportFr = "Nous terminons de ranger les bagages volumineux. Presque prêtes.";
                audioSeq[0] = "pnc_report_taxi_out";
            }
            else if (_isSecuring && phase == FlightPhase.Descent)
            {
                reportEn = "Galley is being secured, and we're starting the final cabin check.";
                reportFr = "Le galley est en cours de sécurisation, nous débutons la vérification finale.";
                audioSeq[0] = "pnc_report_descent";
            }
            else if (PassengerManifest.Exists(p => p.IsInjured))
            {
                var injured = PassengerManifest.FindAll(p => p.IsInjured);
                reportEn = $"We are still tending to {injured.Count} injured passenger(s). The mood is very somber.";
                reportFr = $"Nous nous occupons toujours de {injured.Count} passager(s) blessé(s). L'ambiance est très lourde.";
                audioSeq[0] = "pnc_report_injured";
            }
            else if (phase == FlightPhase.Cruise)
            {
                if (InFlightServiceProgress < 20) {
                    reportEn = "We've just started preparing the service carts.";
                    reportFr = "Nous venons de commencer la préparation des chariots de service.";
                    audioSeq[0] = "pnc_report_service_start";
                } else if (InFlightServiceProgress < 80) {
                    reportEn = "The meal service is in full swing. Everyone seems satisfied.";
                    reportFr = "Le service des repas bat son plein. Tout le monde semble satisfait.";
                    audioSeq[0] = "pnc_report_service_mid";
                } else {
                    reportEn = "Service is complete, and the cabin is resting.";
                    reportFr = "Le service est terminé, la cabine se repose.";
                    audioSeq[0] = "pnc_report_service_end";
                }
            }
            
            if (!isCrisisActive)
            {
                if (PassengerAnxiety > 50.0)
                {
                    if (_currentDelayMinutes > 15 && phase == FlightPhase.AtGate)
                    {
                        reportEn = "The passengers are getting very frustrated and restless due to this long delay.";
                        reportFr = "Les passagers s'impatientent sérieusement et s'énervent à cause de l'attente prolongée.";
                        audioSeq.Add("pnc_report_anxiety_delay");
                    }
                    else if ((DateTime.Now - _lastTurbulenceNotice).TotalMinutes < 15)
                    {
                        reportEn += " It's quite bumpy, and the cabin is feeling very tense and anxious.";
                        reportFr += " Ça secoue pas mal, les passagers sont crispés et l'ambiance est tendue.";
                        audioSeq.Add("pnc_report_anxiety_turb");
                    }
                    else if (_hasTriggeredCateringComplaint)
                    {
                        reportEn = "People are very unhappy about not getting their meals. It's tough back here.";
                        reportFr = "Les gens sont très mécontents de ne pas avoir eu de repas. C'est difficile à l'arrière.";
                        audioSeq.Add("pnc_report_anxiety_food");
                    }
                    else
                    {
                        reportEn += " Note that some passengers are quite anxious about the flight.";
                        reportFr += " À noter que certains passagers sont assez anxieux par rapport au vol.";
                        audioSeq.Add("pnc_report_anxiety_gen");
                    }
                }
                else if (ComfortLevel < 40.0)
                {
                    reportEn += " Passengers are complaining about the general comfort level.";
                    reportFr += " Les passagers se plaignent du niveau de confort général.";
                    audioSeq.Add("pnc_report_comfort_low");
                }
            }

            OnCrewMessage?.Invoke("info", LocalizationService.Translate(reportEn, reportFr), FormatAudioSequence(audioSeq));
            return true;
        }
    }
}
