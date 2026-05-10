using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;

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
        LandingSecured,
        Deboarding
    }

        public class PassengerState
    {
        public string Seat { get; set; } = "1A";
        public bool IsSeatbeltFastened { get; set; } = true;
        public bool IsInjured { get; set; } = false;
        public string InjuryType { get; set; } = "";
        public double IndividualAnxiety { get; set; } = 0.0;
        public double MinAnxietyFloor { get; set; } = 0.0;
        public double IndividualComfort { get; set; } = 100.0;
        public double MaxComfortCap { get; set; } = 100.0;
        public double IndividualSatisfaction { get; set; } = 100.0;
        public double MaxSatisfactionCap { get; set; } = 100.0;
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
        public FlightSupervisor.UI.Models.AirportDestinationType CurrentDestinationType { get; set; } = FlightSupervisor.UI.Models.AirportDestinationType.Business;
        public DateTime CurrentSimLocalTime { get; set; } = DateTime.MinValue;
        public DateTime CurrentSimZuluTime { get; set; } = DateTime.MinValue;
        public bool IsLowCost { get; set; } = false;
        public bool IsServiceHurried { get; set; } = false;
        public string CaptainName { get; set; } = "the Captain";
        
        public bool HasActiveSoftFailure { get; private set; } = false;
        public string ActiveSoftFailureReason { get; private set; } = "";
        
        private int _delayPaCount = 0;
        private DateTime? _delayCooldownEndTime = null;
        public bool IsDelayCooldownActive => _delayCooldownEndTime.HasValue && DateTime.Now < _delayCooldownEndTime.Value;
        
        private bool _isTempInitialized = false;
        private double _currentAmbientTemperature = 15.0;
        public double CurrentAmbientTemperature 
        { 
            get => _currentAmbientTemperature; 
            set 
            {
                // Protect against MSFS Fahrenheit bug or crazy tarmac boundary thermal layers
                double safeTemp = Math.Max(-30.0, Math.Min(32.0, value));
                
                _currentAmbientTemperature = safeTemp;
                if (!_isTempInitialized) 
                {
                    // Initialization starts at a standard 22.0°C instead of pure ambient to match ready-for-flight states
                    LastKnownCabinTemp = 22.0;
                    _isTempInitialized = true;
                }
            } 
        }

        private Random _renderRnd = new Random();

        public string AnxietyReason { get; set; } = "Normal flight conditions.";
        public string ComfortReason { get; set; } = "Cabin conditions are nominal.";
        public string SatisfactionReason { get; set; } = "Passengers are satisfied with the service.";

        public double PassengerAnxiety 
        {
            get {
                if (_lastBoardingTick != DateTime.MaxValue || !PassengerManifest.Where(p => p != null && p.IsBoarded).Any()) return 5.0;
                double avg = PassengerManifest.Where(p => p != null && p.IsBoarded).Average(p => p.IndividualAnxiety);
                return Math.Max(5.0, Math.Min(95.0, Math.Round(avg + (_renderRnd.NextDouble() * 10.0 - 5.0), 1)));
            }
        }
        
        public double ComfortLevel 
        {
            get {
                double maxCap = IsLowCost ? 65.0 : 95.0;
                if (_lastBoardingTick != DateTime.MaxValue || !PassengerManifest.Where(p => p != null && p.IsBoarded).Any()) return maxCap;
                double avg = PassengerManifest.Where(p => p != null && p.IsBoarded).Average(p => p.IndividualComfort);
                return Math.Max(5.0, Math.Min(maxCap, Math.Round(avg + (_renderRnd.NextDouble() * 10.0 - 5.0), 1)));
            }
        }
        
        public bool IsInCloud { get; set; } = false;

        public double Satisfaction 
        {
            get {
                if (_lastBoardingTick != DateTime.MaxValue || !PassengerManifest.Where(p => p != null && p.IsBoarded).Any()) return 95.0;
                double avg = PassengerManifest.Where(p => p != null && p.IsBoarded).Average(p => p.IndividualSatisfaction);
                return Math.Max(5.0, Math.Min(95.0, Math.Round(avg + (_renderRnd.NextDouble() * 10.0 - 5.0), 1)));
            }
        }

        public void ApplyEventPenalty(double immediateDamage, double capDamage, string statType, bool isDelayRelated = false)
        {
            double reputationMultiplier = 1.0;
            if (IsLowCost)
            {
                // Low cost: High tolerance for bad comfort, low tolerance for delays
                if (statType == "Comfort") reputationMultiplier = 0.5;
                if (isDelayRelated) reputationMultiplier = 2.0;
            }
            else
            {
                // Legacy: Low tolerance for bad comfort, higher tolerance for delays
                if (statType == "Comfort") reputationMultiplier = 1.5;
                if (isDelayRelated) reputationMultiplier = 0.5;
            }

            foreach (var p in PassengerManifest.Where(x => x != null && x.IsBoarded))
            {
                if (statType == "Satisfaction")
                {
                    double multiplier = p.Demographic == PassengerDemographic.Grumpy ? 1.5 : (p.Demographic == PassengerDemographic.Relaxed ? 0.8 : 1.0);
                    multiplier *= reputationMultiplier;
                    p.IndividualSatisfaction -= immediateDamage * multiplier;
                    p.MaxSatisfactionCap -= capDamage * multiplier;
                    if (p.MaxSatisfactionCap < 1.0) p.MaxSatisfactionCap = 1.0;
                    if (p.IndividualSatisfaction < 0.0) p.IndividualSatisfaction = 0.0;
                }
                else if (statType == "Anxiety")
                {
                    double multiplier = p.Demographic == PassengerDemographic.Anxious ? 1.5 : (p.Demographic == PassengerDemographic.Relaxed ? 0.5 : 1.0);
                    multiplier *= reputationMultiplier;
                    p.IndividualAnxiety += immediateDamage * multiplier;
                    p.MinAnxietyFloor += capDamage * multiplier;
                    if (p.MinAnxietyFloor > 99.0) p.MinAnxietyFloor = 99.0;
                    if (p.IndividualAnxiety > 100.0) p.IndividualAnxiety = 100.0;
                }
                else if (statType == "Comfort")
                {
                    double multiplier = p.Demographic == PassengerDemographic.Grumpy ? 1.5 : 1.0;
                    multiplier *= reputationMultiplier;
                    p.IndividualComfort -= immediateDamage * multiplier;
                    p.MaxComfortCap -= capDamage * multiplier;
                    if (p.MaxComfortCap < 1.0) p.MaxComfortCap = 1.0;
                    if (p.IndividualComfort < 0.0) p.IndividualComfort = 0.0;
                }
            }
        }

        private void ModifySatisfaction(double amount)
        {
            if (amount < 0) 
            {
                SatisfactionFatigue = Math.Min(100.0, SatisfactionFatigue + Math.Abs(amount) * 0.5); // Accrue fatigue
            }
            else 
            {
                double fatiguePenalty = Math.Max(0.1, 1.0 - (SatisfactionFatigue / 100.0));
                amount *= fatiguePenalty; // Slow down recovery if fatigued
            }

            foreach (var p in PassengerManifest.Where(x => x != null && x.IsBoarded))
            {
                double multiplier = p.Demographic == PassengerDemographic.Grumpy ? 1.5 : (p.Demographic == PassengerDemographic.Relaxed ? 0.8 : 1.0);
                p.IndividualSatisfaction += amount * (amount < 0 ? multiplier : (1 / multiplier));
                if (p.IndividualSatisfaction < 5.0) p.IndividualSatisfaction = 5.0;
                if (p.IndividualSatisfaction > Math.Min(MaxSatisfaction, p.MaxSatisfactionCap)) p.IndividualSatisfaction = Math.Min(MaxSatisfaction, p.MaxSatisfactionCap);
            }
        }
        
        private void SetSatisfaction(double target)
        {
            foreach (var p in PassengerManifest) p.IndividualSatisfaction = Math.Min(Math.Min(target, p.MaxSatisfactionCap), MaxSatisfaction);
        }

        private void ModifyAnxiety(double amount)
        {
            if (amount > 0) 
            {
                AnxietyFatigue = Math.Min(100.0, AnxietyFatigue + amount * 0.5); // Accrue fatigue
            }
            else 
            {
                double fatiguePenalty = Math.Max(0.1, 1.0 - (AnxietyFatigue / 100.0));
                amount *= fatiguePenalty; // Slow down recovery if fatigued
            }

            foreach (var p in PassengerManifest.Where(x => x != null && x.IsBoarded))
            {
                double multiplier = 1.0;
                if (amount > 0)
                {
                    if (p.Demographic == PassengerDemographic.Anxious) multiplier = 1.5;       // Flippés paniquent vite
                    else if (p.Demographic == PassengerDemographic.Relaxed) multiplier = 0.2;  // Gens détendus s'en fichent
                    else if (p.Demographic == PassengerDemographic.Grumpy) multiplier = 0.5;   // Les chieurs s'agacent (confort) plus qu'ils n'ont peur
                    else multiplier = 0.5;                                                     // Les passagers normaux sont modérés
                }
                else
                {
                    // For recovery (negative amount), Relaxed recover quickly, Anxious recover slowly
                    if (p.Demographic == PassengerDemographic.Anxious) multiplier = 0.5;
                    else if (p.Demographic == PassengerDemographic.Relaxed) multiplier = 2.0;
                    else multiplier = 1.0;
                }

                p.IndividualAnxiety += amount * multiplier;
                if (p.IndividualAnxiety < Math.Max(5.0, p.MinAnxietyFloor)) p.IndividualAnxiety = Math.Max(5.0, p.MinAnxietyFloor);
                if (p.IndividualAnxiety > 95.0) p.IndividualAnxiety = 95.0;
            }
        }

        private void SetAnxiety(double minRatio)
        {
            foreach (var p in PassengerManifest.Where(x => x != null && x.IsBoarded))
            {
                p.IndividualAnxiety = Math.Max(Math.Max(Math.Max(5.0, minRatio), p.MinAnxietyFloor), p.IndividualAnxiety);
                if (p.IndividualAnxiety > 95.0) p.IndividualAnxiety = 95.0;
            }
        }

        private void ClearAnxiety() { foreach (var p in PassengerManifest) p.IndividualAnxiety = Math.Max(5.0, p.MinAnxietyFloor); }

        private void ModifyComfort(double amount)
        {
            if (amount < 0) 
            {
                ComfortFatigue = Math.Min(100.0, ComfortFatigue + Math.Abs(amount) * 0.5); // Accrue fatigue
            }
            else 
            {
                double fatiguePenalty = Math.Max(0.1, 1.0 - (ComfortFatigue / 100.0));
                amount *= fatiguePenalty; // Slow down recovery if fatigued
            }

            foreach (var p in PassengerManifest.Where(x => x != null && x.IsBoarded))
            {
                double multiplier = p.Demographic == PassengerDemographic.Grumpy ? 1.5 : 1.0;
                p.IndividualComfort += amount * (amount < 0 ? multiplier : (1 / multiplier));
                
                double actualCap = Math.Min(MaxComfort, p.MaxComfortCap);
                if (p.IndividualComfort < 5.0) p.IndividualComfort = 5.0;
                if (p.IndividualComfort > actualCap) p.IndividualComfort = actualCap;
            }
        }

        private void ClearComfort() 
        { 
            foreach (var p in PassengerManifest) p.IndividualComfort = Math.Min(MaxComfort, p.MaxComfortCap); 
        }

        // Virtual Crew Stats
        public double CrewProactivity { get; private set; } = 100.0;
        public double CrewEfficiency { get; private set; } = 100.0;
        public double CrewMorale { get; private set; } = 100.0;
        public double CrewEsteem { get; private set; } = 100.0;
        private int _manualApologyCount = 0;

        public double BaseAnxietySpikeMultiplier { get; set; } = 1.0;
        public double BaseComfortLossMultiplier { get; set; } = 1.0;
        public double BaseRecoveryMultiplier { get; set; } = 1.0;
        public bool IsSilencePenaltyActive { get; private set; } = false;

        public List<PassengerState> PassengerManifest { get; private set; } = new List<PassengerState>();
        public List<PassengerState> PreviousLegManifest { get; private set; } = new List<PassengerState>();
        public FlightSupervisor.UI.Services.ManifestData CurrentManifest { get; private set; }
        public bool IsCrewSeated { get; private set; } = false;
        public double SecuringProgress { get; private set; } = 0.0;

        private FlightPhase _lastPhase = FlightPhase.AtGate;
        private DateTime _lastPhaseChangeTime = DateTime.MinValue;

        public delegate void CrewMessageEventHandler(string color, string message, List<string>? audioSequence = null);
        public event CrewMessageEventHandler? OnCrewMessage;
        public event Action<int, string>? OnPenaltyTriggered;
        public event Action<int, string>? OnOperationBonusTriggered;
        public event Action<string, CabinState>? OnPncStatusChanged;
        public event Action<string>? OnCabinCallIncoming;

        // Pending events for Incoming Calls
        private ConcurrentQueue<(string CallType, string AudioFolder, string FallbackMessage, DateTime TriggerTime)> _pendingCabinCalls = new();
        private bool _isCabinCallBlinking = false;

        public bool IsCabinCallIncoming => _isCabinCallBlinking;
        public string PendingCabinCallReason { get; private set; } = "";
        private DateTime? _lastCabinCallTime = null;
        private DateTime? _cabinCallCooldownEnd = null;

        public void TriggerIncomingCabinCall(string callType, string audioFolder, string fallbackMessage)
        {
            FlightSupervisor.UI.Services.DebugLogger.Log("INTERCOM", $"[Enqueue] CallType: {callType}, Folder: {audioFolder}");
            _pendingCabinCalls.Enqueue((callType, audioFolder, fallbackMessage, DateTime.Now));
            if (!_isCabinCallBlinking && _cabinCallCooldownEnd == null)
            {
                _isCabinCallBlinking = true;
                OnCabinCallIncoming?.Invoke("start");
            }
        }

        public void AnswerCabinCall()
        {
            if (_pendingCabinCalls.TryDequeue(out var call))
            {
                FlightSupervisor.UI.Services.DebugLogger.Log("INTERCOM", $"[Answer] CallType: {call.CallType}, WaitTime: {(DateTime.Now - call.TriggerTime).TotalSeconds:F1}s");
                // Unanswered calls might pile up, we process the oldest
                string macroName = call.AudioFolder.Contains('/') ? "pnc_" + System.Text.RegularExpressions.Regex.Replace(call.AudioFolder.Split('/').Last(), "([a-z])([A-Z])", "$1_$2").ToLower() : call.AudioFolder;
                PlayDynamicAsPurser(macroName, call.FallbackMessage, false, "to_fd", false);
                OnCrewMessage?.Invoke("orange", LocalizationService.Translate(call.FallbackMessage, call.FallbackMessage), null);

                if (call.CallType == "DelayPax")
                {
                    ModifySatisfaction(-5.0);
                    OnPenaltyTriggered?.Invoke(-15, LocalizationService.Translate("Poor CRM: Passive Crew & Unmanaged Delay", "Mauvais CRM : Équipage Passif et Retard Non Géré"));
                }

                var delay = (DateTime.Now - call.TriggerTime).TotalSeconds;
                if (delay <= 15)
                {
                    CrewEsteem = Math.Min(100.0, CrewEsteem + 5.0);
                    OnOperationBonusTriggered?.Invoke(20, LocalizationService.Translate("Crew CRM: Promptly answered cabin intercom", "CRM Équipage: Réponse rapide à l'intercom"));
                }
                else if (delay > 60)
                {
                    CrewEsteem = Math.Max(0.0, CrewEsteem - 10.0);
                    OnPenaltyTriggered?.Invoke(-20, LocalizationService.Translate("Crew CRM: Ignored cabin intercom for over a minute", "CRM Équipage: Intercom cabine ignoré plus d'une minute"));
                }

                if (!_pendingCabinCalls.IsEmpty)
                {
                    _isCabinCallBlinking = false;
                    OnCabinCallIncoming?.Invoke("stop");
                    _cabinCallCooldownEnd = DateTime.Now.AddSeconds(5);
                }
                else
                {
                    _isCabinCallBlinking = false;
                    OnCabinCallIncoming?.Invoke("stop");
                }
            }
            else
            {
                // If it wasn't blinking, the player initiated the call: Status Report
                TriggerStatusReport();
            }
        }

        private void TriggerStatusReport()
        {
            // Will be implemented in task 4
            string msg = "Captain, everything is fine in the cabin.";
            PlayDynamicAsPurser("pnc_calm", msg, false, "to_fd", false);
            OnCrewMessage?.Invoke("info", LocalizationService.Translate(msg, msg), null);
            CrewEsteem = Math.Min(100.0, CrewEsteem + 2.0); // Simple interaction boost
        }

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
        
        private double _thermalDissatisfactionGauge = 0.0;
        public double ThermalDissatisfaction => _thermalDissatisfactionGauge;
        public double LastKnownCabinTemp { get; private set; } = 22.0;
        private bool _hasWarnedThermal = false;
        private bool _hasPenalizedTurbulenceReaction = false;

        private bool _hasAppliedDepartureWeatherAnxiety = false;
        private bool _hasAppliedArrivalWeatherAnxiety = false;
        private bool _hasTriggeredThrustReductionAnxiety = false;
        private DateTime _lastCabinBankPenalty = DateTime.MinValue;
        private bool _hasWarnedToiletsFull = false;
        
        // Flight Progression & Hold Mechanics
        private DateTime? _actualTakeoffTime = null;
        private DateTime _lastHoldDecay = DateTime.MinValue;
        private double _cumulativeHoldSeconds = 0;
        private bool _isHolding = false;
        
        private Random _rnd = new Random();
        private DateTime _lastRandomEvent = DateTime.Now;
        private DateTime _lastReportRequest = DateTime.MinValue;
        private DateTime? _strategicPenaltyEndTime = null;
        private DateTime _lastPncCleanlinessComplaint = DateTime.MinValue;

        private bool _hasPlayedSeatbeltOffPA = false;
        private bool _hasTriggeredServicePrep = false;
        private bool _hasPlayedDescentPA = false;
        private bool _hasPlayedApproachPncPA = false;
        private bool _isPreparingService = false;
        private DateTime? _servicePrepTimerStart = null;
        private double _rngServiceBufferSeconds = 0;

        private DateTime _lastTickTime = DateTime.MinValue;
        private double _holdTurnAccumulator = 0.0;
        private DateTime _lastHoldPenaltyTime = DateTime.MinValue;

        private bool _isSeatingForTakeoffOrLanding = false;
        private DateTime? _seatingTimerStart = null;
        private CabinState _seatingTargetState = CabinState.Idle;
        private const double SeatingDuration = 12.0;

        private bool _isPlayingSafetyDemo = false;
        // ---- Passenger Fatigue and Dynamic Limits ----
        public double ComfortFatigue { get; private set; } = 0.0;
        public double AnxietyFatigue { get; private set; } = 0.0;
        public double SatisfactionFatigue { get; private set; } = 0.0;
        
        public double MaxComfort { get; private set; } = 100.0;
        public double MaxSatisfaction { get; private set; } = 100.0;
        // ----------------------------------------------

        private DateTime? _safetyDemoTimerStart = null;
        private const double SafetyDemoDuration = 45.0;
        public bool IsPlayingSafetyDemo => _isPlayingSafetyDemo;

        private double _currentDelayMinutes = 0;
        public double CurrentDelayMinutes => _currentDelayMinutes;
        private double _currentSecuringRate = 0;
        private double _pncActionDelaySeconds = 0.0;
        private bool _isSecuring = false;
        public bool IsSecuringHalted { get; private set; } = false;
        public bool IsSecuringHurried { get; private set; } = false;
        private CabinState _targetState = CabinState.TakeoffSecured;

        public int SessionFlightsCompleted { get; set; } = 0;
        public bool FirstFlightClean { get; set; } = true;

        public double BaggageCompletion { get; set; } = 100.0;

        public int MaxCateringRations { get; set; } = 165;
        
        private int _cateringRations = 165;
        private double _cateringFractionalDrain = 0.0;
        public int CateringRations 
        { 
            get { return _cateringRations; } 
            set 
            { 
                _cateringRations = Math.Max(0, Math.Min(MaxCateringRations, value));
                _cateringCompletion = MaxCateringRations > 0 ? ((double)_cateringRations / MaxCateringRations) * 100.0 : 0.0;
            } 
        }
        
        private double _cateringCompletion = 100.0;
        public double CateringCompletion 
        { 
            get { return _cateringCompletion; } 
            set 
            { 
                _cateringCompletion = Math.Max(0.0, Math.Min(100.0, value));
                _cateringRations = (int)Math.Round(MaxCateringRations * (_cateringCompletion / 100.0)); 
            } 
        }

        public FlightSupervisor.UI.Models.AircraftState StateOfAircraft { get; private set; } = new FlightSupervisor.UI.Models.AircraftState();

        public double CabinCleanliness
        {
            get => StateOfAircraft.CleanlinessPercentage;
            set => StateOfAircraft.CleanlinessPercentage = value;
        }

        public double WaterLevel
        {
            get => StateOfAircraft.PotableWaterPercentage;
            set => StateOfAircraft.PotableWaterPercentage = value;
        }

        public double WasteLevel
        {
            get => StateOfAircraft.WasteTankPercentage;
            set => StateOfAircraft.WasteTankPercentage = value;
        }

        public double VirtualFuelPercentage
        {
            get => StateOfAircraft.VirtualFuelPercentage;
            set => StateOfAircraft.VirtualFuelPercentage = value;
        }

        public bool IsServiceHalted { get; private set; } = false;
        public bool HasBoardingStarted { get; set; } = false;
        
        public double SecondsSinceLastReport => _lastReportRequest == DateTime.MinValue ? 9999 : (DateTime.Now - _lastReportRequest).TotalSeconds;
        public FlightSupervisor.UI.Models.SimBrief.SimBriefResponse? CurrentFlight { get; set; }

        private HashSet<string> _issuedCommands = new HashSet<string>();

        // System scoring communications
        public bool HasPlayedWelcomePA { get; set; } = false;
        public bool HasPlayedPrepareTakeoffPA { get; set; } = false;
        public bool HasPlayedDescentPA { get; set; } = false;
        public bool HasPlayedPrepareLandingPA { get; set; } = false;
        public bool HasAnnouncedGoAroundPA { get; set; } = false;

        private double _comfortSum = 0;
        private int _comfortSamples = 0;
        public double AverageComfort => _comfortSamples == 0 ? ComfortLevel : (_comfortSum / _comfortSamples);

        public bool IsSeatbeltsOn => _seatbeltsOn;
        private bool _seatbeltsOn = true;
        private double _seatbeltsOnDurationInCruise = 0;
        private bool _hasComplainedAboutSeatbelts = false;

        private bool _hasWarnedTempHot = false;
        private bool _hasWarnedTempCold = false;
        private bool _hasWarnedPushbackNoSeatbelts = false;
        public bool HasPenalizedRefuelingSeatbelts { get; private set; } = false;
        public bool AreEnginesRunning { get; set; } = false;
        public bool IsApuRunning { get; set; } = false;

        public void TriggerRefuelingSeatbeltPenalty()
        {
            if (HasPenalizedRefuelingSeatbelts) return;
            HasPenalizedRefuelingSeatbelts = true;
            OnPenaltyTriggered?.Invoke(-100, LocalizationService.Translate("Safety Breach: Seatbelts ON during refueling", "Violation Sécurité: Ceintures attachées au ravitaillement"));
            OnCrewMessage?.Invoke("red", LocalizationService.Translate("Captain, refueling is still in progress. Seatbelts should remain OFF for evacuation purposes.", "Commandant, le ravitaillement est en cours. Les ceintures doivent rester détachées pour l'évacuation."), null);
        }

        public double InFlightServiceProgress { get; private set; } = 0.0;
        public bool IsSatietyActive { get; private set; } = false;

        public string ActiveCaptainLanguage { get; set; } = "gb";
        public string ActiveCaptainVoiceId { get; set; } = "rowan_(gb)";
        public string ActivePncLanguage { get; set; } = "gb";
        public string ActivePncVoiceId { get; set; } = "beth_(gb)";
        public string ActiveAirlineId { get; set; } = "air_france";
        private Dictionary<string, string> _audioExtensions = new Dictionary<string, string>();
        
        private DateTime _lastBoardingTick = DateTime.MaxValue;
        private bool _hasAnnouncedBoardingComplete = false;
        private bool _hasAnnouncedGalleySecured = false;


        private AudioEngineService _audio;

        public CabinManager(AudioEngineService audioEngine)
        {
            _audio = audioEngine;
            _gForceHistory = new Queue<double>();
            
            // Randomize Crew Stats initially
            CrewProactivity = Math.Round(30.0 + (_rnd.NextDouble() * 70.0));
            CrewEfficiency = Math.Round(60.0 + (_rnd.NextDouble() * 40.0));
            CrewMorale = 100.0;
            CrewEsteem = Math.Round((CrewProactivity + CrewEfficiency + CrewMorale) / 3.0);
            
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

        public void AutoSelectVoicesForAirline(string nationality)
        {
            string targetLang = "gb"; // Default
            if (!string.IsNullOrEmpty(nationality))
            {
                string norm = nationality.ToLowerInvariant();
                if (norm == "fr" || norm == "france") targetLang = "fr";
                else if (norm == "de" || norm == "germany") targetLang = "de";
                else if (norm == "es" || norm == "spain") targetLang = "es";
                else if (norm == "it" || norm == "italy") targetLang = "it";
                else if (norm == "us" || norm == "usa") targetLang = "us";
            }

            // Find first available captain voice for targetLang
            string wwwroot = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "assets", "sounds", "airlines");
            string captLangDir = System.IO.Path.Combine(wwwroot, "captain", targetLang);
            if (!System.IO.Directory.Exists(captLangDir))
            {
                targetLang = "gb"; // Fallback if no specific language pack exists
                captLangDir = System.IO.Path.Combine(wwwroot, "captain", targetLang);
            }

            if (System.IO.Directory.Exists(captLangDir))
            {
                var dirs = System.IO.Directory.GetDirectories(captLangDir);
                if (dirs.Length > 0)
                {
                    ActiveCaptainLanguage = targetLang;
                    var defaultDir = dirs.FirstOrDefault(d => new System.IO.DirectoryInfo(d).Name.StartsWith("rowan", StringComparison.OrdinalIgnoreCase));
                    ActiveCaptainVoiceId = defaultDir != null ? new System.IO.DirectoryInfo(defaultDir).Name : new System.IO.DirectoryInfo(dirs[0]).Name;
                }
            }

            string pncLangDir = System.IO.Path.Combine(wwwroot, "pnc", targetLang);
            if (System.IO.Directory.Exists(pncLangDir))
            {
                var dirs = System.IO.Directory.GetDirectories(pncLangDir);
                if (dirs.Length > 0)
                {
                    ActivePncLanguage = targetLang;
                    var defaultDir = dirs.FirstOrDefault(d => new System.IO.DirectoryInfo(d).Name.StartsWith("beth", StringComparison.OrdinalIgnoreCase));
                    ActivePncVoiceId = defaultDir != null ? new System.IO.DirectoryInfo(defaultDir).Name : new System.IO.DirectoryInfo(dirs[0]).Name;
                }
            }
        }

        public string GetActivePncGender()
        {
            if (string.IsNullOrEmpty(ActivePncVoiceId)) return "Female";
            string lowerId = ActivePncVoiceId.ToLowerInvariant();
            if (lowerId.Contains("rudy") || lowerId.Contains("rowan") || lowerId.Contains("jean") || lowerId.Contains("pierre") || lowerId.Contains("henri") || lowerId.Contains("guy") || lowerId.Contains("brian")) return "Male";
            return "Female"; // Default to Female for all other voices
        }

        public void InitializeFlightDemographics(FlightSupervisor.UI.Services.AirlineProfile profile, FlightSupervisor.UI.Services.ManifestData manifestData = null)
        {
            _hasAnnouncedBoardingComplete = false;
            _hasAnnouncedGalleySecured = false;
            _hasWarnedPushbackNoSeatbelts = false;
            HasPenalizedRefuelingSeatbelts = false;
            HasBoardingStarted = false;
            _lastBoardingTick = DateTime.MaxValue;
            
            if (SessionFlightsCompleted == 0)
            {
                State = CabinState.Idle;
                ComfortFatigue = 0.0;
                AnxietyFatigue = 0.0;
                SatisfactionFatigue = 0.0;
            }
            
            if (profile != null)
            {
                // Convert 1-10 scores to percentages. Minimum 50% for commercial baseline.
                MaxComfort = Math.Max(50.0, profile.HardProductScore * 10.0);
                MaxSatisfaction = Math.Max(50.0, profile.SoftProductScore * 10.0);
            }
            else
            {
                MaxComfort = 100.0;
                MaxSatisfaction = 100.0;
            }
            
            // System scoring resets
            HasPlayedWelcomePA = false;
            HasPlayedPrepareTakeoffPA = false;
            HasPlayedDescentPA = false;
            HasPlayedPrepareLandingPA = false;
            HasAnnouncedGoAroundPA = false;

            CurrentManifest = manifestData;

            if (profile == null) return;
            
            ActiveAirlineId = profile.Name;
            IsLowCost = profile.Tier != null && profile.Tier.ToLower() == "lowcost";
            
            if (SessionFlightsCompleted > 0 && PassengerManifest.Any(p => p != null && p.IsBoarded))
            {
                PreviousLegManifest.Clear();
                PreviousLegManifest.AddRange(PassengerManifest);
            }
            PassengerManifest.Clear();
            
            double baseComfort = 50.0 + (profile.HardProductScore * 5.0);
            double baseSatisfaction = 50.0 + (profile.SoftProductScore * 5.0);
            double baseAnxiety = (10.0 - profile.SafetyRecord) * 2.0;
            
            if (manifestData != null && manifestData.Passengers != null && manifestData.Passengers.Count > 0)
            {
                foreach (var pax in manifestData.Passengers)
                {
                    var p = new PassengerState() { 
                        Seat = pax.Seat, 
                        IsBoarded = false,
                        IndividualComfort = Math.Min(100.0, Math.Max(0.0, baseComfort + (_rnd.NextDouble() * 10 - 5))),
                        IndividualSatisfaction = Math.Min(100.0, Math.Max(0.0, baseSatisfaction + (_rnd.NextDouble() * 10 - 5))),
                        IndividualAnxiety = Math.Min(100.0, Math.Max(0.0, baseAnxiety + (_rnd.NextDouble() * 5)))
                    };
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
                    var p = new PassengerState() { 
                        Seat = $"{i+1}A", 
                        IsBoarded = false,
                        IndividualComfort = Math.Min(100.0, Math.Max(0.0, baseComfort + (_rnd.NextDouble() * 10 - 5))),
                        IndividualSatisfaction = Math.Min(100.0, Math.Max(0.0, baseSatisfaction + (_rnd.NextDouble() * 10 - 5))),
                        IndividualAnxiety = Math.Min(100.0, Math.Max(0.0, baseAnxiety + (_rnd.NextDouble() * 5)))
                    };
                    double r = _rnd.NextDouble();
                    if (r < 0.1) p.Demographic = PassengerDemographic.Grumpy;
                    else if (r < 0.25) p.Demographic = PassengerDemographic.Anxious;
                    else if (r < 0.4) p.Demographic = PassengerDemographic.Relaxed;
                    PassengerManifest.Add(p);
                }
            }
            
            int savedRations = CateringRations;
            if (profile.Tier.ToLower() == "lowcost")
            {
                MaxCateringRations = PassengerManifest.Count + 5; // Very strict for LCC (Buy on Board)
            }
            else
            {
                MaxCateringRations = Math.Max(PassengerManifest.Count + 10, (int)(PassengerManifest.Count * 1.15)); // 15% safety margin for Legacy
            }
            CateringRations = savedRations; // Preserve physical rations and recalculate percentage for the new leg
            
            // Adjust Crew Stats based on Tier - ONLY for the first flight to allow persistence
            if (SessionFlightsCompleted == 0)
            {
                switch (profile.Tier.ToLower())
                {
                    case "elite":
                        CrewProactivity = Math.Round(90.0 + (_rnd.NextDouble() * 10.0));
                        CrewEfficiency = Math.Round(90.0 + (_rnd.NextDouble() * 10.0));
                        CrewMorale = 100.0;
                        break;
                    case "standard":
                        CrewProactivity = Math.Round(70.0 + (_rnd.NextDouble() * 19.0));
                        CrewEfficiency = Math.Round(70.0 + (_rnd.NextDouble() * 19.0));
                        CrewMorale = Math.Round(90.0 + (_rnd.NextDouble() * 10.0));
                        break;
                    case "lowcost":
                        CrewProactivity = Math.Round(50.0 + (_rnd.NextDouble() * 20.0));
                        CrewEfficiency = Math.Round(80.0 + (_rnd.NextDouble() * 15.0)); // Highly efficient turns
                        CrewMorale = Math.Round(70.0 + (_rnd.NextDouble() * 20.0));
                        break;
                    case "struggling":
                        CrewProactivity = Math.Round(30.0 + (_rnd.NextDouble() * 20.0));
                        CrewEfficiency = Math.Round(40.0 + (_rnd.NextDouble() * 20.0));
                        CrewMorale = Math.Round(40.0 + (_rnd.NextDouble() * 20.0));
                        break;
                    case "danger":
                        CrewProactivity = Math.Round(10.0 + (_rnd.NextDouble() * 20.0));
                        CrewEfficiency = Math.Round(20.0 + (_rnd.NextDouble() * 20.0));
                        CrewMorale = Math.Round(10.0 + (_rnd.NextDouble() * 20.0));
                        break;
                    default:
                        CrewProactivity = Math.Round(50.0 + (_rnd.NextDouble() * 50.0));
                        CrewEfficiency = Math.Round(50.0 + (_rnd.NextDouble() * 50.0));
                        CrewMorale = 80.0;
                        break;
                }
                CrewEsteem = Math.Round((CrewProactivity + CrewEfficiency + CrewMorale) / 3.0);
            }
        }

        public void LoadShiftState(ShiftState state)
        {
            SessionFlightsCompleted = state.SessionFlightsCompleted;
            CabinCleanliness = state.CabinCleanliness;
            WaterLevel = state.WaterLevel;
            WasteLevel = state.WasteLevel;
            VirtualFuelPercentage = state.VirtualFuelPercentage == 0.0 ? 100.0 : state.VirtualFuelPercentage;
            CateringRations = state.CateringRations;
            CrewProactivity = state.CrewProactivity;
            CrewEfficiency = state.CrewEfficiency;
            CrewMorale = state.CrewMorale;
            CrewEsteem = state.CrewEsteem == 0.0 ? Math.Round((CrewProactivity + CrewEfficiency + CrewMorale) / 3.0) : state.CrewEsteem;
        }

        public void UpdateSeatbelts(bool on, FlightPhase phase)
        {
            _seatbeltsOn = on;
        }

        private void SetPncActionDelay()
        {
            double delayMin = 5.0;
            double delayMax = 10.0;
            double esteemFactor = 1.0 - (Math.Max(0.0, CrewEsteem) / 100.0);
            _pncActionDelaySeconds = Math.Max(0.0, delayMin + ((delayMax - delayMin) * esteemFactor) + (_rnd.NextDouble() * 2.0 - 1.0));
        }

        public void HandleCommand(string command)
        {
            if (_issuedCommands.Contains(command)) return; // Prevent duplicates
            _issuedCommands.Add(command);

            switch (command)
            {
                case "TOP_DESCENT":
                    PlayDynamicAsCaptain("pa_nearing_top_descent", "Cabin crew, we are nearing the top of descent.");
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("PA: Cabin crew, nearing top of descent.", "PA: PNC, début de descente imminent."), null);
                    if (State == CabinState.ServingMeals)
                    {
                        IsServiceHurried = true;
                        OnPncStatusChanged?.Invoke("Service securing (HURRIED)...", State);
                    }
                    break;
                case "ARM_DOORS":
                    PlayDynamicAsCaptain("pa_arm_doors", "Cabin crew, prepare doors for departure and cross-check.");
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("PA: Cabin Crew, arm doors and cross check.", "PA: PNC aux portes, armement des toboggans, vérification de la porte opposée."), null);
                    break;
                case "SEATBELT_ON":
                    _seatbeltsOn = true;
                    OnPncStatusChanged?.Invoke("Seatbelts Validated", State);
                    break;
                case "SEATBELT_OFF":
                    _seatbeltsOn = false;
                    OnPncStatusChanged?.Invoke("Seatbelts Off", State);
                    
                    // Unbuckle majority of passengers, but nervous/standard ones might keep it on.
                    foreach (var px in PassengerManifest)
                    {
                        if (px != null && px.IsBoarded)
                        {
                            double keepChance = px.Demographic == PassengerDemographic.Anxious ? 0.90 :
                                                px.Demographic == PassengerDemographic.Standard ? 0.30 : 0.05;
                                                
                            if (_rnd.NextDouble() > keepChance) 
                            {
                                px.IsSeatbeltFastened = false;
                            }
                        }
                    }
                    break;
                case "PREPARE_TAKEOFF":
                    _isSecuring = true;
                    IsSecuringHurried = false;
                    _currentSecuringRate = 0.55; // Approx 3 minutes to reach 100%
                    _targetState = CabinState.TakeoffSecured;
                    State = CabinState.SecuringForTakeoff;
                    SecuringProgress = 0;
                    IsCrewSeated = false;
                    HasPlayedPrepareTakeoffPA = true; // SCORING
                    SetPncActionDelay();
                    PlayDynamicAsCaptain("pa_prepare_takeoff", "Cabin Crew, prepare for takeoff.", true);
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("INTERCOM: Cabin Crew, prepare for takeoff.", "INTERCOM: PNC, préparez la cabine pour le décollage."), null);
                    OnPncStatusChanged?.Invoke("Securing Cabin...", State);
                    break;
                case "SEATS_TAKEOFF":
                    if (_isSecuring && SecuringProgress < 100.0) {
                        OnPenaltyTriggered?.Invoke(-30, LocalizationService.Translate("Force Seats: Crew forced to sit before cabin was secure", "Force Seats: PNC forcés de s'asseoir avant sécurisation"));
                        IncreaseAnxiety(15.0, FlightPhase.AtGate, false, "Passengers are stressed by the rushed departure preparations.", 40.0);
                        CrewEsteem = Math.Max(0.0, CrewEsteem - 5.0);
                        _isSecuring = false; // Interrupted
                    }
                    PlayDynamicAsCaptain("pa_seats_takeoff", "Cabin Crew, seats for takeoff.", true);
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("INTERCOM: Cabin Crew, please be seated for takeoff.", "INTERCOM: PNC, aux postes pour le décollage."), null);
                    
                    _isSeatingForTakeoffOrLanding = true;
                    _seatingTimerStart = DateTime.Now;
                    _seatingTargetState = CabinState.TakeoffSecured;
                    OnPncStatusChanged?.Invoke("Crew taking seats...", State);
                    break;
                case "PREPARE_LANDING":
                    _isSecuring = true;
                    IsSecuringHurried = false;
                    _currentSecuringRate = 0.50; 
                    _targetState = CabinState.LandingSecured;
                    State = CabinState.SecuringForLanding;
                    SecuringProgress = 0;
                    IsCrewSeated = false;
                    HasPlayedPrepareLandingPA = true; // SCORING
                    SetPncActionDelay();
                    PlayDynamicAsCaptain("pa_prepare_landing", "Cabin Crew, prepare for landing.", true);
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("INTERCOM: Cabin Crew, prepare for landing.", "INTERCOM: PNC, préparez la cabine pour l'atterrissage."), null);
                    OnPncStatusChanged?.Invoke("Securing Cabin...", State);
                    break;
                case "START_SERVICE":
                    double gMin = 1.0; double gMax = 1.0;
                    if (_gForceHistory.Count > 0) { foreach (var g in _gForceHistory) { if(g < gMin) gMin=g; if(g > gMax) gMax=g; } }
                    bool isHaltedByTurbulence = gMax - gMin > 0.6;

                    if (!isHaltedByTurbulence)
                    {
                        State = CabinState.ServingMeals;
                        InFlightServiceProgress = 0;
                        IsServiceHalted = false;
                        IsCrewSeated = false;
                        SetPncActionDelay();
                        OnCrewMessage?.Invoke("info", LocalizationService.Translate("Cabin Crew, you may commence the in-flight service.", "PNC, vous pouvez débuter le service en vol."), null);
                        OnPncStatusChanged?.Invoke("Serving Meals", State);
                    }
                    else
                    {
                        OnCrewMessage?.Invoke("orange", LocalizationService.Translate("Captain, we cannot start service due to severe turbulence.", "Commandant, impossible de débuter le service avec ces fortes turbulences."), null);
                    }
                    break;
                case "SEATS_LANDING":
                    if (_isSecuring && SecuringProgress < 100.0) {
                        OnPenaltyTriggered?.Invoke(-30, LocalizationService.Translate("Force Seats: Crew forced to sit before cabin was secure", "Force Seats: PNC forcés de s'asseoir avant sécurisation"));
                        IncreaseAnxiety(15.0, FlightPhase.Cruise, false, "Passengers are stressed by the rushed landing preparations.", 40.0);
                        CrewEsteem = Math.Max(0.0, CrewEsteem - 5.0);
                        _isSecuring = false; // Interrupted
                    }
                    PlayDynamicAsCaptain("pa_seats_landing", "Cabin Crew, seats for landing.", true);
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("INTERCOM: Cabin Crew, please be seated for landing.", "INTERCOM: PNC, aux postes pour l'atterrissage."), null);
                    
                    _isSeatingForTakeoffOrLanding = true;
                    _seatingTimerStart = DateTime.Now;
                    _seatingTargetState = CabinState.LandingSecured;
                    OnPncStatusChanged?.Invoke("Crew taking seats...", State);
                    break;
                case "HURRY_SECURING":
                    if (_isSecuring && !IsSecuringHurried) {
                        IsSecuringHurried = true;
                        _currentSecuringRate *= 4.0; // 4x speed!
                        
                        string actMsgEn = _targetState == CabinState.TakeoffSecured ? "PA: Cabin Crew, HURRY up and secure cabin, takeoff imminent!" : "PA: Cabin Crew, HURRY up and secure cabin for landing!";
                        string actMsgFr = _targetState == CabinState.TakeoffSecured ? "PA: PNC, DÉPÊCHEZ-VOUS de préparer la cabine !" : "PA: PNC, DÉPÊCHEZ-VOUS de préparer la cabine pour l'atterrissage !";
                        
                        OnCrewMessage?.Invoke("orange", LocalizationService.Translate(actMsgEn, actMsgFr), null);
                        OnPncStatusChanged?.Invoke("HURRYING...", State);
                    }
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
        
        public event Action? OnDeboardingComplete;

        public void StartBoarding()
        {
            HasBoardingStarted = true;
            State = CabinState.Boarding;
            _hasAnnouncedBoardingComplete = false;
            _hasAnnouncedGalleySecured = false;
            OnPncStatusChanged?.Invoke("Boarding...", State);
        }

        public void StartDeboarding()
        {
            if (State != CabinState.Idle && State != CabinState.LandingSecured) return; // Prevent double trigger
            
            State = CabinState.Deboarding;
            _lastBoardingTick = DateTime.Now; // Reuse boarding tick for deboarding pacing
            
            // Mess left by passengers upon departure
            CabinCleanliness -= 5.0;
            if (CabinCleanliness < 0) CabinCleanliness = 0.0;
            
            PlayDynamicAsCaptain("pa_disarm_doors", "Cabin crew, disarm doors and cross-check.");
            OnCrewMessage?.Invoke("cyan", LocalizationService.Translate("Cabin Crew, disarm doors and cross check.", "PNC aux portes, désarmement des toboggans et vérification de la porte opposée."), new List<string> { "intercom_ding", "pa_chime" });
            OnPncStatusChanged?.Invoke("Deboarding...", State);
        }

        public void FastForward(double deltaSeconds, FlightPhase phase)
        {
            if (deltaSeconds <= 0) return;
            
            // Artificial consumption matching EXACTLY the standard Tick pace
            // Per user request, NEVER consume cabin resources on the ground. Only in flight.
            bool isFlyingPhase = phase == FlightPhase.Cruise || phase == FlightPhase.Climb || phase == FlightPhase.Descent || phase == FlightPhase.Approach;

            if (PassengerManifest.Count > 0 && isFlyingPhase)
            {
                int paxCount = Math.Max(1, PassengerManifest.Count(p => p != null && p.IsBoarded));
                double paxMultiplier = paxCount / 150.0;

                double seatbeltFactor = _seatbeltsOn ? 0.20 : 1.0; 
                
                WaterLevel = Math.Max(0, WaterLevel - (0.004 * paxMultiplier * seatbeltFactor * deltaSeconds));
                WasteLevel = Math.Min(100, WasteLevel + (0.005 * paxMultiplier * seatbeltFactor * deltaSeconds));
                CabinCleanliness = Math.Max(0, CabinCleanliness - (0.003 * paxMultiplier * seatbeltFactor * deltaSeconds));

            }

            if (deltaSeconds >= 300)
            {
                // Removed LastKnownCabinTemp = CurrentAmbientTemperature 
                // to prevent instant temperature jumps when AC is actually running during time skip.
            }
        }

        public void ProcessLandingImpact(double fpm, double gforce)
        {
            if (fpm > -150 && gforce < 1.3)
            {
                // Kiss Landing / Butter
                IncreaseComfort(15.0);
                ModifyAnxiety(-30.0);
            }
            else if (fpm < -600 || gforce > 1.8)
            {
                // Severe Hard Landing
                DecreaseComfort(40.0);
                IncreaseAnxiety(60.0, FlightPhase.Landing, true, "Passengers are screaming due to the terrifying impact!", 95.0);
                OnCrewMessage?.Invoke("red", UI.Services.LocalizationService.Translate("That was a terrifying impact! Many passengers are screaming in the back!", "C'était un atterrissage extrêmement violent ! Beaucoup de passagers crient à l'arrière !"), null);
            }
            else if (fpm < -450 || gforce > 1.5)
            {
                // Hard Landing
                DecreaseComfort(20.0);
                IncreaseAnxiety(30.0, FlightPhase.Landing, false, "Passengers were scared by the hard landing.", 95.0);
                OnCrewMessage?.Invoke("orange", UI.Services.LocalizationService.Translate("That was a very hard landing! The cabin shook violently.", "C'était un atterrissage très dur ! La cabine a secoué violemment."), null);
            }
            else 
            {
                // Normal
                ModifyAnxiety(-10.0);
            }
        }

        public void ProcessBounce(int bounceCount)
        {
            DecreaseComfort(30.0);
            IncreaseAnxiety(50.0, FlightPhase.Landing, true, GetDynamicFeedback("LandingBounce", FlightPhase.Landing), 90.0);
            OnCrewMessage?.Invoke("red", UI.Services.LocalizationService.Translate($"Bounce detected! Passengers are terrified! (Count: {bounceCount})", $"Rebond détecté ! L'avion a rebondi, les passagers sont terrifiés ! (Compte : {bounceCount})"), null);
        }

        public void Tick(double gForce, double bankAngle, bool isBoarded, DateTime currentZulu, DateTime? sobt, FlightPhase phase, double groundSpeed, double altitude, double verticalSpeed, bool isCrisisActive, double cabinTemperature = 22.0, double boardingProgress = -1.0)
        {
            double deltaTimeSeconds = _lastTickTime == DateTime.MinValue ? 1.0 : (DateTime.Now - _lastTickTime).TotalSeconds;
            _lastTickTime = DateTime.Now;
            if (deltaTimeSeconds <= 0 || deltaTimeSeconds > 10) deltaTimeSeconds = 1.0;

            if (_pncActionDelaySeconds > 0)
            {
                _pncActionDelaySeconds -= deltaTimeSeconds;
                if (_pncActionDelaySeconds < 0) _pncActionDelaySeconds = 0;
            }


            // Handle Cabin Call Queue Cooldown & Timeout
            if (_cabinCallCooldownEnd.HasValue)
            {
                if (DateTime.Now >= _cabinCallCooldownEnd.Value)
                {
                    _cabinCallCooldownEnd = null;
                    if (!_pendingCabinCalls.IsEmpty && !_isCabinCallBlinking)
                    {
                        // Reset TriggerTime for the top call so it has a fresh 60s
                        var list = _pendingCabinCalls.ToList();
                        _pendingCabinCalls.Clear();
                        for (int i = 0; i < list.Count; i++)
                        {
                            var item = list[i];
                            if (i == 0) item.TriggerTime = DateTime.Now;
                            _pendingCabinCalls.Enqueue(item);
                        }
                        
                        _isCabinCallBlinking = true;
                        OnCabinCallIncoming?.Invoke("start");
                    }
                }
            }
            else if (_isCabinCallBlinking && _pendingCabinCalls.TryPeek(out var topCall))
            {
                if ((DateTime.Now - topCall.TriggerTime).TotalSeconds > 60)
                {
                    if (_pendingCabinCalls.TryDequeue(out var expiredCall))
                    {
                        FlightSupervisor.UI.Services.DebugLogger.Log("INTERCOM", $"[Expired] CallType: {expiredCall.CallType}");
                        CrewEsteem = Math.Max(0.0, CrewEsteem - 10.0);
                        OnPenaltyTriggered?.Invoke(-20, LocalizationService.Translate("Crew CRM: Ignored cabin intercom for over a minute", "CRM Équipage: Intercom cabine ignoré plus d'une minute"));
                        
                        _isCabinCallBlinking = false;
                        OnCabinCallIncoming?.Invoke("stop");
                        _cabinCallCooldownEnd = DateTime.Now.AddSeconds(5);
                    }
                }
            }

            if (State == CabinState.Deboarding)
            {
                var activeManifest = PreviousLegManifest.Any(p => p != null && p.IsBoarded) ? PreviousLegManifest : PassengerManifest;
                var boardedCount = activeManifest.Count(p => p != null && p.IsBoarded);
                
                if (boardedCount == 0)
                {
                    State = CabinState.Idle;
                    HasBoardingStarted = false;
                    OnCrewMessage?.Invoke("cyan", LocalizationService.Translate("Cabin makes are complete. All passengers have disembarked.", "La cabine est débarrassée. Tous les passagers ont débarqué."), null);
                    
                    PlayDynamicAsPurser("pnc_deboarding_complete", 
                        LocalizationService.Translate("Captain, deboarding is complete. We'll start preparing the cabin for the next leg.", "Commandant, le débarquement est terminé. Nous allons préparer la cabine pour la suite."), 
                        false, "to_fd", false);

                    OnDeboardingComplete?.Invoke();
                    OnPncStatusChanged?.Invoke("Standing By", State);
                }
                return;
            }

            // Thermal monitoring still runs before boarding, so we don't completely return.
            // Progressive Boarding Logic (Phase 3)
            if ((phase == FlightPhase.AtGate || phase == FlightPhase.Turnaround) && HasBoardingStarted && !isBoarded)
            {
                if (!HasActiveSoftFailure && (DateTime.Now - _lastRandomEvent).TotalMinutes >= 4.0)
                {
                    _lastRandomEvent = DateTime.Now;
                    if (_rnd.NextDouble() < 0.25) // 25% chance every 4 mins of boarding/turnaround
                    {
                        HasActiveSoftFailure = true;
                        ActiveSoftFailureReason = "Technical";
                        OnCrewMessage?.Invoke("orange", LocalizationService.Translate("[PNC] Captain, we have a technical issue in the cabin (defective passenger seat). Please make a PA to inform the passengers of a technical delay.", "[PNC] Commandant, nous avons un souci technique en cabine (siège passager défectueux). Pouvez-vous faire une annonce de retard technique ?"), null);
                    }
                }

                if (_lastBoardingTick == DateTime.MaxValue) 
                {
                    _lastBoardingTick = DateTime.Now;
                    foreach(var px in PassengerManifest) { px.IsBoarded = false; px.IsSeatbeltFastened = false; }
                }

                if (boardingProgress >= 0)
                {
                    // Boarding is orchestrated by GroundOpsResourceService via BoardPassenger()
                }
                else
                {
                    // Fallback purely time-based behavior if no ground ops sync provided
                    if ((DateTime.Now - _lastBoardingTick).TotalSeconds >= 1.0)
                    {
                        _lastBoardingTick = DateTime.Now;
                        var unboarded = PassengerManifest.Where(p => p != null && !p.IsBoarded).ToList();
                        if (unboarded.Count > 0)
                        {
                            BoardPassenger(_rnd.Next(1, 4));
                        }
                    }
                }
            }
            
            // Check if boarding just completed (either via ground ops or moving to Taxi without GroundOps)
            bool isAircraftMoving = phase >= FlightPhase.Pushback && phase <= FlightPhase.Arrived;
            if ((isBoarded || isAircraftMoving) && !_hasAnnouncedBoardingComplete && State != CabinState.Deboarding)
            {
                var remainingUnboarded = PassengerManifest.Where(x => x != null && !x.IsBoarded).ToList();
                foreach (var p in remainingUnboarded)
                {
                    p.IsBoarded = true;
                    p.IsSeatbeltFastened = _seatbeltsOn ? (_rnd.Next(100) < 98) : (_rnd.Next(100) < 33);
                }
                
                _lastBoardingTick = DateTime.MaxValue; // Set to MaxValue to stop progressive logic
                State = CabinState.Idle;
                _hasAnnouncedBoardingComplete = true;
                TriggerIncomingCabinCall("BoardingComplete", "pnc_boarding_complete_fd", "Boarding is complete Captain.");
                OnPncStatusChanged?.Invoke("Standing By", State);
            }

            // Automatic Galley Secured Event after boarding is complete
            if (!_hasAnnouncedGalleySecured && _hasAnnouncedBoardingComplete && phase == FlightPhase.AtGate)
            {
                _hasAnnouncedGalleySecured = true;
                TriggerIncomingCabinCall("GalleySecured", "pnc_galley_secured_fd", "Galleys secured and we're starting the final cabin checks.");
            }

            // --- THERMAL COMFORT (Physical Simulation) ---
            if (cabinTemperature > 0.0) 
            {
                double inertiaRate = 0.02 * deltaTimeSeconds; // ~1.2 degrees per minute (realistic thermal drift)
                if (LastKnownCabinTemp < cabinTemperature) {
                    LastKnownCabinTemp += Math.Min(inertiaRate, cabinTemperature - LastKnownCabinTemp);
                } else if (LastKnownCabinTemp > cabinTemperature) {
                    LastKnownCabinTemp -= Math.Min(inertiaRate, LastKnownCabinTemp - cabinTemperature);
                }
            }

            // GATING CONDITION: Disable all stress, comfort, and thermal decay while boarding is in progress.
            // Points 1 & 2: Monitoring and Thermal effects should not apply while doors are open and boarding.
            if (!isBoarded && (phase == FlightPhase.AtGate || phase == FlightPhase.Turnaround))
            {
                _thermalDissatisfactionGauge = 0.0;
                return; 
            }

            // --- Ground Movement Safety Check ---
            if (phase == FlightPhase.AtGate || phase == FlightPhase.Turnaround || phase == FlightPhase.Pushback || phase == FlightPhase.TaxiOut || phase == FlightPhase.Takeoff || phase == FlightPhase.Landing || phase == FlightPhase.TaxiIn)
            {
                if (groundSpeed >= 1.0 && !_seatbeltsOn && !_hasWarnedPushbackNoSeatbelts)
                {
                    _hasWarnedPushbackNoSeatbelts = true;
                    ModifySatisfaction(-25.0);
                    IncreaseAnxiety(30.0, phase, isCrisisActive, GetDynamicFeedback("MovementNoSeatbelts", phase), 40.0);
                    
                    string msgEn = "Safety Breach: Aircraft in motion with Seatbelts OFF!";
                    string msgFr = "Violation Sécurité: Avion en mouvement sans ceintures !";

                    OnCrewMessage?.Invoke("orange", LocalizationService.Translate("Captain, the seatbelt sign is still off and we are moving!", "Commandant, le signal des ceintures est toujours éteint et nous bougeons !"), null);
                    OnPenaltyTriggered?.Invoke(-100, LocalizationService.Translate(msgEn, msgFr));
                }
            }
            
            // Jauge Virtuelle de Fuel
            if (CurrentFlight != null && CurrentFlight.Times != null)
            {
                if (long.TryParse(CurrentFlight.Times.SchedOut, out long outTime) && long.TryParse(CurrentFlight.Times.SchedIn, out long inTime))
                {
                    double flightSeconds = inTime - outTime;
                    if (flightSeconds > 0)
                    {
                        if (AreEnginesRunning)
                        {
                            // Estimate burn: 90% over block time (assuming 10% remaining at engines off).
                            double fuelBurnRate = 90.0 / flightSeconds;
                            VirtualFuelPercentage -= fuelBurnRate * deltaTimeSeconds;
                        }
                        else if (IsApuRunning)
                        {
                            // Estimate burn: APU burns considerably less (approx 5% of normal engines)
                            double fuelBurnRate = (90.0 / flightSeconds) * 0.05;
                            VirtualFuelPercentage -= fuelBurnRate * deltaTimeSeconds;
                        }
                        
                        if (VirtualFuelPercentage < 2.0) VirtualFuelPercentage = 2.0; // Prevent stalling at 0%
                    }
                }
            }

            // Continuous Consumption of Water, Cleanliness, and Waste (Multi-Leg)
            if (phase == FlightPhase.Cruise || phase == FlightPhase.Climb || phase == FlightPhase.Descent || phase == FlightPhase.Takeoff || phase == FlightPhase.InitialClimb || phase == FlightPhase.Approach)
            {
                int paxCount = Math.Max(1, PassengerManifest.Count(p => p != null && p.IsBoarded));
                double paxMultiplier = paxCount / 150.0; // Baseline normalized

                // Seatbelt duration tracking in Cruise
                if (phase == FlightPhase.Cruise)
                {
                    if (_seatbeltsOn)
                    {
                        _seatbeltsOnDurationInCruise += deltaTimeSeconds;
                        if (_seatbeltsOnDurationInCruise > 2700 && !_hasComplainedAboutSeatbelts) // 45 mins
                        {
                            _hasComplainedAboutSeatbelts = true;
                            OnPncStatusChanged?.Invoke(LocalizationService.Translate("Captain, passengers are asking to use the restrooms. It's been a long time with seatbelts on.", "Commandant, les passagers demandent à utiliser les toilettes. Cela fait longtemps que les ceintures sont attachées."), State);
                            IncreaseAnxiety(15.0, phase, isCrisisActive, GetDynamicFeedback("ProlongedSeatbelt", phase), 40.0);
                            ModifySatisfaction(-15.0);
                        }
                    }
                    else
                    {
                        _seatbeltsOnDurationInCruise = 0; // Reset if turned off
                        _hasComplainedAboutSeatbelts = false;
                    }
                }

                // Passenger resource consumption (Water, Waste, Dirt)
                double seatbeltFactor = _seatbeltsOn ? 0.20 : 1.0; 

                // Cleanliness degrades faster with Grumpy passengers
                int grumpyCount = PassengerManifest.Count(p => p != null && p.IsBoarded && p.Demographic == PassengerDemographic.Grumpy);
                double grumpyMultiplier = 1.0 + (grumpyCount / (double)paxCount) * 1.5; // Up to 2.5x if all grumpy
                CabinCleanliness -= 0.003 * paxMultiplier * grumpyMultiplier * seatbeltFactor * deltaTimeSeconds; 
                if (CabinCleanliness < 0) CabinCleanliness = 0;

                // Water consumption peaks if Stress > 50%
                double waterStressMultiplier = PassengerAnxiety > 50.0 ? 3.0 : 1.0;
                WaterLevel -= 0.004 * paxMultiplier * waterStressMultiplier * seatbeltFactor * deltaTimeSeconds; 
                if (WaterLevel < 0) WaterLevel = 0;

                // Waste generation +40% for Holiday destination
                double wasteDestMultiplier = CurrentDestinationType == FlightSupervisor.UI.Models.AirportDestinationType.Holiday ? 1.4 : 1.0;
                double stressMultiplier = PassengerAnxiety > 60.0 ? 3.0 : 1.0; // Keep existing stress multiplier for waste
                WasteLevel += 0.005 * paxMultiplier * stressMultiplier * wasteDestMultiplier * seatbeltFactor * deltaTimeSeconds;
                if (WasteLevel > 100) WasteLevel = 100;
                
                // Cateringrations is handled solely by the explicit InFlightService in the state machine.
                
                if (WasteLevel >= 100) 
                {
                    WasteLevel = 100;
                    if (!_hasWarnedToiletsFull)
                    {
                        _hasWarnedToiletsFull = true;
                        string msg = "Captain, the waste tanks are full. All lavatories are now condemned! Passengers are furious.";
                        PlayDynamicAsPurser("pnc_uncomfortable_pax", msg, false, "to_fd", false);
                        OnCrewMessage?.Invoke("red", LocalizationService.Translate(msg, "Commandant, les cuves à déchets sont pleines. Les toilettes sont condamnées ! Les passagers sont furieux."), null);
                        OnPenaltyTriggered?.Invoke(-100, LocalizationService.Translate("Cabin Resource Failure: Lavatories Full", "Échec Ressource Cabine : Toilettes Pleines"));
                    }
                    if (ComfortLevel > 20) DecreaseComfort(0.5 * deltaTimeSeconds);
                    ModifySatisfaction(-0.1 * deltaTimeSeconds);
                }
                else if (WasteLevel < 90)
                {
                     _hasWarnedToiletsFull = false;
                }
            }

            // Idle Noise Generator & Continuous Decay
            if (PassengerAnxiety < 2.0 && ComfortLevel >= 95.0)
            {
                ComfortReason = "Cabin conditions are nominal.";
                if (_rnd.NextDouble() < 0.05) ModifyAnxiety(_rnd.NextDouble() * 0.2);
            }
            else if (phase != FlightPhase.AtGate && phase != FlightPhase.Turnaround && phase != FlightPhase.TaxiOut && phase != FlightPhase.TaxiIn)
            {
                if ((DateTime.Now - _lastTurbulenceNotice).TotalSeconds > 120 && !isCrisisActive) 
                {
                    DecreaseAnxiety(0.05 * deltaTimeSeconds); // Gradual peace recovery
                    double maxRecov = IsLowCost ? 65.0 : 95.0;
                    if (ComfortLevel < maxRecov) 
                    {
                        IncreaseComfort(0.1 * deltaTimeSeconds); // Gradual comfort recovery
                        if (ComfortLevel > 80.0) ComfortReason = "Cabin conditions are stabilizing.";
                    }
                }
            }

            // Cloud penetration anxiety
            if (IsInCloud && (phase == FlightPhase.InitialClimb || phase == FlightPhase.Climb || phase == FlightPhase.Cruise || phase == FlightPhase.Descent || phase == FlightPhase.Approach))
            {
                // Passengers get slightly anxious when flying through clouds, especially if seatbelts are off
                // User requested: weather alone should not push anxiety past 20%
                if (PassengerAnxiety < 20.0)
                {
                    double cloudStressMultiplier = _seatbeltsOn ? 0.01 : 0.03;
                    IncreaseAnxiety(cloudStressMultiplier * deltaTimeSeconds, phase, isCrisisActive, GetDynamicFeedback("CloudAnxiety", phase), 20.0);
                }
            }

            // --- MORALE PASSIVE AURA ---
            if (CrewEsteem >= 80.0 && phase != FlightPhase.AtGate && phase != FlightPhase.Turnaround)
            {
                DecreaseAnxiety(0.01); // Smiling proactive crew gently reassures passengers continuously
            }
            else if (CrewEsteem < 40.0 && phase != FlightPhase.AtGate && phase != FlightPhase.Turnaround)
            {
                DecreaseComfort(0.01); // Stressed, overwhelmed or grumpy crew passively annoys passengers
            }


            // Removed old `if (!isBoarded...)` thermal bypass as we already return earlier.
            if (cabinTemperature > 0.0) // Valid sensor data
            {
                    double agitationIncrement = 0.0;
                    double currentTemp = LastKnownCabinTemp;

                    if (currentTemp > 28.0)
                    {
                        double delta = currentTemp - 28.0;
                        agitationIncrement = (1.0 + (delta * 0.5)) * deltaTimeSeconds * 0.5;
                    }
                    else if (currentTemp > 26.0)
                    {
                        double delta = currentTemp - 26.0;
                        agitationIncrement = delta * deltaTimeSeconds * 0.2;
                    }
                    else if (currentTemp > 25.0)
                    {
                        double delta = currentTemp - 25.0;
                        agitationIncrement = delta * deltaTimeSeconds * 0.05;
                    }
                    else if (currentTemp < 19.0)
                    {
                        double delta = 19.0 - currentTemp;
                        agitationIncrement = (1.0 + (delta * 0.5)) * deltaTimeSeconds * 0.5;
                    }
                    else if (currentTemp < 21.0)
                    {
                        double delta = 21.0 - currentTemp;
                        agitationIncrement = delta * deltaTimeSeconds * 0.2;
                    }
                    else if (currentTemp < 22.0)
                    {
                        double delta = 22.0 - currentTemp;
                        agitationIncrement = delta * deltaTimeSeconds * 0.05;
                    }
                    else
                    {
                        // Zone de confort Idéale (22.0 - 25.0)
                        agitationIncrement = -deltaTimeSeconds * 2.0; // Drainage rapide
                    }

                    _thermalDissatisfactionGauge += agitationIncrement;

                    if (_thermalDissatisfactionGauge < 0.0) _thermalDissatisfactionGauge = 0.0;
                    if (_thermalDissatisfactionGauge > 100.0) _thermalDissatisfactionGauge = 100.0;

                    // Automation PNC push alert si l'agitation dépasse un seuil
                    if (_thermalDissatisfactionGauge >= 80.0 && !_hasWarnedThermal)
                    {
                        _hasWarnedThermal = true;
                        FlightSupervisor.UI.Services.DebugLogger.Log("THERMAL", $"Threshold reached (Gauge={_thermalDissatisfactionGauge:F1}, Temp={LastKnownCabinTemp:F1}°C). Triggering Warning.");
                        
                        if (LastKnownCabinTemp > 25.0) 
                        {
                            string msg = "Captain, it's getting really hot back here, passengers are complaining. Can you adjust the temperature?";
                            TriggerIncomingCabinCall("TempHot", "TO_FD/Incoming_Calls/TempHot", msg);
                        }
                        else 
                        {
                            string msg = "Captain, a few passengers are complaining about the cold. Please consider turning up the AC.";
                            TriggerIncomingCabinCall("TempCold", "TO_FD/Incoming_Calls/TempCold", msg);
                        }
                        
                        // Softened penalties
                        ModifySatisfaction(-5.0);
                        DecreaseComfort(5.0);
                        OnPenaltyTriggered?.Invoke(-10, LocalizationService.Translate($"Comfort Violation: Critical Cabin Temperature ({LastKnownCabinTemp:F1}°C)", $"Alerte Confort : Température Critique ({LastKnownCabinTemp:F1}°C)"));
                    }
                    // Message PNC de résolution quand le pilote corrige complètement la température
                    else if (_thermalDissatisfactionGauge < 20.0 && _hasWarnedThermal)
                    {
                        _hasWarnedThermal = false;
                        FlightSupervisor.UI.Services.DebugLogger.Log("THERMAL", $"Warning resolved (Gauge={_thermalDissatisfactionGauge:F1}, Temp={LastKnownCabinTemp:F1}°C). Triggering TempFixed.");
                        string msg = "Captain, the temperature is much better now, thank you.";
                        TriggerIncomingCabinCall("TempFixed", "TO_FD/Incoming_Calls/TempFixed", msg);
                        
                        if (ComfortLevel < 90) ModifyComfort(5.0); // Léger gain de satisfaction pour la résolution
                    }

                    // Constante dégradation si la situation inconfortable perdure (Fortement adoucie)
                    if (_thermalDissatisfactionGauge > 50.0)
                    {
                        double penaltyFactor = (_thermalDissatisfactionGauge / 100.0);
                        DecreaseComfort(penaltyFactor * 0.05 * deltaTimeSeconds);
                        ComfortReason = $"Cabin temperature is uncomfortable ({LastKnownCabinTemp:F1}°C).";
                    }
                }

            // --- CLEANLINESS & WATER PENALTIES ---
            if (isBoarded && State != CabinState.Deboarding)
            {
                if (CabinCleanliness < 50.0)
                {
                    double dirtyFactor = (50.0 - CabinCleanliness) / 50.0;
                    DecreaseComfort(0.01 * dirtyFactor * deltaTimeSeconds);
                    
                    if (CabinCleanliness < 40.0 && _rnd.NextDouble() < (0.001 * deltaTimeSeconds) && (DateTime.Now - _lastPncCleanlinessComplaint).TotalMinutes > 20)
                    {
                        _lastPncCleanlinessComplaint = DateTime.Now;
                        string msg = "Captain, passengers are complaining about the disgusting state of the cabin...";
                        PlayDynamicAsPurser("pnc_cabin_dirty", msg, false, "to_fd", false);
                        OnCrewMessage?.Invoke("orange", LocalizationService.Translate(msg, "Commandant, les passagers se plaignent de l'état absolument dégoûtant de la cabine..."), null);
                        ModifySatisfaction(-2.0);
                    }
                }
                
                if (WaterLevel <= 0.0 && _rnd.NextDouble() < (0.002 * deltaTimeSeconds))
                {
                    OnCrewMessage?.Invoke("orange", LocalizationService.Translate("Captain, we are completely out of potable water.", "Commandant, nous n'avons plus du tout d'eau potable."), null);
                    DecreaseComfort(1.0);
                    ModifySatisfaction(-1.0);
                }
            }

            // --- DYNAMIC COMFORT (Vertical Speed) ---
            if (phase == FlightPhase.Takeoff || phase == FlightPhase.InitialClimb || phase == FlightPhase.Climb)
            {
                if (verticalSpeed > 2500.0)
                {
                    DecreaseComfort(0.01 * (verticalSpeed / 2500.0));
                }
            }

            _comfortSum += ComfortLevel;
            _comfortSamples++;

            if (phase != _lastPhase)
            {
                if (phase == FlightPhase.Landing)
                {
                    _isSeatingForTakeoffOrLanding = false;
                    _isSecuring = false;
                    IsSecuringHurried = false;
                    SecuringProgress = 0.0;
                    State = CabinState.LandingSecured;
                }
                if (phase == FlightPhase.TaxiOut)
                {
                    // HasBoardingStarted is true when boarding starts, but here we just ensure it's not a flight reset.
                    // Actually, let's just trigger Safety Demo on TaxiOut
                    string[] safetyVariations = new[] {
                        "Ladies and gentlemen, may we have your attention for the safety instructions. Please ensure your seatbelt is securely fastened, your seat back is upright, and your tray table is stowed. Smoking, including electronic cigarettes, is strictly prohibited on board. Emergency exits are located at the front, middle, and rear of the cabin. In the event of a sudden loss of cabin pressure, pull the oxygen mask towards you and place it over your nose and mouth before helping others. Thank you.",
                        "Your attention please for a brief safety demonstration. Fasten your seatbelt by inserting the metal fitting into the buckle. Take a moment to locate your nearest emergency exit, keeping in mind it might be behind you. Smoking is not allowed at any time during this flight. All electronic devices must now be switched to airplane mode. We are currently preparing the cabin for departure.",
                        "Ladies and gentlemen, Federal Aviation regulations require your compliance with all crew instructions and lighted signs. Please fasten your seatbelt and keep it fastened whenever the sign is illuminated. There are marked emergency exits along the cabin; identify your closest one now. Smoking and vaping are federal offenses in the lavatories and the cabin. Thank you for your full cooperation as we prepare for takeoff."
                    };
                    string safetyDemo = safetyVariations[_rnd.Next(safetyVariations.Length)];
                    PlayDynamicAsPurser("safety_briefing", safetyDemo, true, "to_pa", true);
                    OnCrewMessage?.Invoke("sky", LocalizationService.Translate("PA: Safety Demonstration in progress.", "PA: Démonstration de sécurité en cours."), null);
                    
                    _isPlayingSafetyDemo = true;
                    _safetyDemoTimerStart = DateTime.Now;
                    OnPncStatusChanged?.Invoke("Safety Briefing in progress...", State);
                }
                else if (phase == FlightPhase.Takeoff)
                {
                    if (_actualTakeoffTime == null) _actualTakeoffTime = DateTime.Now;
                    
                    IncreaseAnxiety(10.0, phase, isCrisisActive, GetDynamicFeedback("TakeoffAccel", phase), 30.0);
                    OnCrewMessage?.Invoke("orange", LocalizationService.Translate("Passengers feel the pressure of takeoff acceleration.", "Les passagers ressentent la pression et le bruit de l'accélération."), null);
                }
                else if (phase == FlightPhase.Climb && _lastPhase == FlightPhase.InitialClimb && !_hasTriggeredThrustReductionAnxiety)
                {
                    _hasTriggeredThrustReductionAnxiety = true;
                    IncreaseAnxiety(20.0, phase, isCrisisActive, GetDynamicFeedback("ThrustReduct", phase), 40.0);
                    OnCrewMessage?.Invoke("orange", LocalizationService.Translate("Thrust reduction felt in cabin. Passengers experienced a brief moment of anxiety.", "Réduction de poussée ressentie. Les passagers ont eu un bref moment d'anxiété (sensation de chute)."), null);
                }
                else if (phase == FlightPhase.Cruise)
                {
                    DecreaseAnxiety(20.0);
                    OnCrewMessage?.Invoke("green", LocalizationService.Translate("Passengers are relieved to reach cruise altitude.", "Les passagers sont soulagés d'avoir atteint l'altitude de croisière."), null);
                }
                else if (phase == FlightPhase.Approach && altitude <= 10000)
                {
                    if (!_hasPlayedApproachPncPA)
                    {
                        _hasPlayedApproachPncPA = true;
                        DecreaseAnxiety(15.0);
                        string destName = CurrentFlight?.Destination?.Name ?? CurrentFlight?.Destination?.IcaoCode ?? "our destination";
                        string spokenText = $"Ladies and gentlemen, we are now approaching {destName}. Please return to your seats, fasten your seatbelts, and make sure your large electronic devices are stowed away.";
                        PlayDynamicAsPurser("arrival", spokenText, true, "to_pa", true);

                        OnCrewMessage?.Invoke("sky", LocalizationService.Translate(
                            $"PA: We are approaching {destName}. Please return to your seats and fasten your seatbelts.", 
                            $"PA: Nous sommes en approche vers {destName}. Veuillez regagner vos sièges et attacher vos ceintures."
                        ), null);
                        
                        OnOperationBonusTriggered?.Invoke(25, LocalizationService.Translate("Passenger Announcement: Approach Securing (PNC)", "Annonce Passagers : Sous les 10 000 pieds (PNC)"));
                    }
                }
                else if (phase == FlightPhase.TaxiIn)
                {
                    DecreaseAnxiety(40.0);
                    OnCrewMessage?.Invoke("green", LocalizationService.Translate("Passengers are very relieved to be back on the ground safely.", "Les passagers sont très soulagés d'être à nouveau au sol en sécurité."), null);
                    
                    string destName = CurrentFlight?.Destination?.Name ?? CurrentFlight?.Destination?.IcaoCode ?? "your destination";
                    string arrTime = CurrentSimLocalTime != DateTime.MinValue ? CurrentSimLocalTime.ToString("HH:mm") : DateTime.Now.ToString("HH:mm");
                    
                    // 1. Start PA & City (ICAO)
                    string destIcao = CurrentFlight?.Destination?.IcaoCode ?? "eddf";
                    PlayDynamicAsPurser($"pnc_arr_dest_{destIcao.ToLower()}", null, playChime: false);

                    // 2. Local Time
                    DateTime destLocalTime = CurrentSimLocalTime != DateTime.MinValue ? CurrentSimLocalTime : DateTime.Now;
                    
                    PlayDynamicAsPurser("pnc_block_local_time", null, playChime: false);

                    // Determine if we should use 24h format (French) or 12h format (English)
                    bool isFrench = ActiveAirlineId.ToLower().Contains("afr") || 
                                    ActiveAirlineId.ToLower().Contains("air_france") || 
                                    ActiveAirlineId.ToLower().Contains("easyjet") || 
                                    (CurrentFlight?.Origin?.IcaoCode?.StartsWith("LF") == true);

                    if (isFrench)
                    {
                        PlayDynamicAsPurser($"hour_{destLocalTime.Hour}", $"{destLocalTime.Hour}", playChime: false);
                        if (destLocalTime.Minute > 0)
                        {
                            PlayDynamicAsPurser($"num_{destLocalTime.Minute}", $"{destLocalTime.Minute}", playChime: false);
                        }
                    }
                    else
                    {
                        int destHour12 = destLocalTime.Hour % 12;
                        if (destHour12 == 0) destHour12 = 12;
                        PlayDynamicAsPurser($"hour_{destHour12}", $"{destHour12}", playChime: false);
                        
                        if (destLocalTime.Minute > 0)
                        {
                            PlayDynamicAsPurser($"num_{destLocalTime.Minute}", $"{destLocalTime.Minute}", playChime: false);
                        }
                        
                        string ampm = destLocalTime.Hour < 12 ? "am" : "pm";
                        PlayDynamicAsPurser($"pnc_block_time_{ampm}", null, playChime: false);
                    }
                    
                    PlayDynamicAsPurser("pnc_block_time_outro", null, playChime: false);

                    // 3. Airline / Outro
                    string normAirline = ActiveAirlineId.ToLower();
                    if (normAirline.Contains("eju") || normAirline.Contains("ezs") || normAirline.Contains("ezy") || normAirline.Contains("easyjet")) 
                    {
                        normAirline = "easyjet";
                    }
                    else if (normAirline.Contains("afr") || normAirline.Contains("air_france") || normAirline.Contains("airfrance"))
                    {
                        normAirline = "airfrance";
                    }
                    PlayDynamicAsPurser($"pnc_arrival_outro_{normAirline}", null, playChime: false);

                    OnCrewMessage?.Invoke("sky", LocalizationService.Translate($"PA: Welcome to {destName}. Local time is {arrTime}.", $"PA: Bienvenue à {destName}. Heure locale : {arrTime}."), null);
                }
                _lastPhase = phase;
                _lastPhaseChangeTime = DateTime.Now;
            }

            // --- Flight Duration Fatigue (C.1) ---
            if (_actualTakeoffTime.HasValue && (phase == FlightPhase.Takeoff || phase == FlightPhase.InitialClimb || phase == FlightPhase.Climb || phase == FlightPhase.Cruise || phase == FlightPhase.Descent || phase == FlightPhase.Approach))
            {
                double elapsedSeconds = (currentZulu - _actualTakeoffTime.Value).TotalSeconds;
                
                // Base slow degradation
                DecreaseComfort(0.0005 * deltaTimeSeconds); 
                
                if (CurrentFlight?.Times?.EstTimeEnroute != null)
                {
                    if (double.TryParse(CurrentFlight.Times.EstTimeEnroute, out double eteSeconds))
                    {
                        if (elapsedSeconds > eteSeconds)
                        {
                            // Surpassed ETE ! Comfort drops significantly over time
                            DecreaseComfort(0.005 * deltaTimeSeconds);
                            // Also slight anxiety increase because they feel it takes too long
                            IncreaseAnxiety(0.001 * deltaTimeSeconds, phase, isCrisisActive, GetDynamicFeedback("FlightDelay", phase), 40.0);
                        }
                    }
                }
            }

            // --- Holding Pattern Detection (C.2) ---
            if (phase == FlightPhase.Cruise || phase == FlightPhase.Descent || phase == FlightPhase.Approach)
            {
                if (Math.Abs(bankAngle) > 12.0)
                {
                    _holdTurnAccumulator += deltaTimeSeconds;
                }
                else
                {
                    _holdTurnAccumulator = 0; // Instant decay to eliminate false positives in approach S-turns
                }

                if (_holdTurnAccumulator > 240) // Allow up to 4 mins of continuous bank to avoid penalizing wide 180° teardrop turns
                {
                    if ((DateTime.Now - _timeOfLastDelayPA).TotalMinutes > 15 && (DateTime.Now - _lastHoldPenaltyTime).TotalMinutes > 5)
                    {
                        _lastHoldPenaltyTime = DateTime.Now;
                        _holdTurnAccumulator = 0; // Clear it out to require another full 360
                        
                        ModifySatisfaction(-15.0);
                        IncreaseAnxiety(15.0, phase, false, GetDynamicFeedback("HoldingPattern", phase), 40.0);

                        OnCrewMessage?.Invoke("orange", LocalizationService.Translate(
                            "Captain, passengers are noticing we're flying in circles and getting anxious. An announcement would help.",
                            "Commandant, les passagers remarquent qu'on tourne en rond et s'angoissent. Une annonce aiderait."
                        ), null);
                        
                        OnPenaltyTriggered?.Invoke(-50, LocalizationService.Translate("Poor CRM: Unexplained Holding Pattern", "Mauvais CRM : Attente en vol inexpliquée"));
                    }
                }
            }

            // --- Service Start Auto Sequence ---
            if ((phase == FlightPhase.Climb || phase == FlightPhase.Cruise) && altitude > 10000 && !_seatbeltsOn && !_hasPlayedSeatbeltOffPA)
            {
                _hasPlayedSeatbeltOffPA = true;
                PlayDynamicAsPurser("seatbelts_off", "Ladies and gentlemen, the captain has turned off the fasten seatbelt sign. You are now free to move about the cabin. However, we do recommend keeping your seatbelt fastened while seated, in case we experience any unexpected turbulence.", true, "to_pa", true);
                OnCrewMessage?.Invoke("sky", LocalizationService.Translate("PA: Seatbelts off announcement.", "PA: Annonce de libération des ceintures."), null);
            }

            if ((phase == FlightPhase.Climb || phase == FlightPhase.Cruise) && altitude > 10000 && !_hasTriggeredServicePrep)
            {
                _hasTriggeredServicePrep = true;
                if (!IsServiceHalted && CabinCleanliness > 30) // Just making sure the plane isn't a dumpster
                {
                    _isPreparingService = true;
                    _servicePrepTimerStart = DateTime.Now;
                    _rngServiceBufferSeconds = 10.0 + _rnd.Next(60, 120); // 10s incompressible + 1-2 min RNG
                    OnPncStatusChanged?.Invoke("Preparing In-Flight Service...", CabinState.Idle);
                }
            }

            if (_isPreparingService && _servicePrepTimerStart.HasValue)
            {
                double elapsed = (DateTime.Now - _servicePrepTimerStart.Value).TotalSeconds;
                SecuringProgress = Math.Min(100.0, (elapsed / _rngServiceBufferSeconds) * 100.0); // Re-use the securing progress bar for now

                if (elapsed >= _rngServiceBufferSeconds)
                {
                    _isPreparingService = false;
                    _servicePrepTimerStart = null;
                    SecuringProgress = 0.0;

                    if (!IsServiceHalted)
                    {
                        string servicePA = "Ladies and gentlemen, we are pleased to inform you that our in-flight service is about to begin. We will be passing through the cabin shortly with complimentary beverages and snacks. Keep your seatbelts fastened even when the sign is off. Thank you.";
                        PlayDynamicAsPurser("service_start", servicePA, true, "to_pa", true);
                        OnCrewMessage?.Invoke("sky", LocalizationService.Translate("PA: In-flight service is starting.", "PA: Le service en vol commence."), null);
                        State = CabinState.ServingMeals;
                        InFlightServiceProgress = 0.0;
                        OnPncStatusChanged?.Invoke("Serving Meals", State);
                    }
                    else
                    {
                        OnPncStatusChanged?.Invoke("Service Halted", CabinState.Idle);
                    }
                }
            }

            // --- Queue Checks & Progressive Audio Timers ---
            
            if (_isPlayingSafetyDemo && _safetyDemoTimerStart.HasValue)
            {
                double elapsed = (DateTime.Now - _safetyDemoTimerStart.Value).TotalSeconds;
                SecuringProgress = Math.Min(100.0, (elapsed / SafetyDemoDuration) * 100.0);

                if (elapsed >= SafetyDemoDuration)
                {
                    _isPlayingSafetyDemo = false;
                    _safetyDemoTimerStart = null;
                    SecuringProgress = 0.0;
                    OnPncStatusChanged?.Invoke("Safety Briefing completed.", State);
                }
            }

            if (_isSeatingForTakeoffOrLanding && _seatingTimerStart.HasValue)
            {
                double elapsed = (DateTime.Now - _seatingTimerStart.Value).TotalSeconds;
                SecuringProgress = Math.Min(100.0, (elapsed / SeatingDuration) * 100.0);

                if (elapsed >= SeatingDuration)
                {
                    _isSeatingForTakeoffOrLanding = false;
                    _seatingTimerStart = null;
                    SecuringProgress = 0.0;
                    IsCrewSeated = true;
                    
                    if (State == _seatingTargetState) OnPncStatusChanged?.Invoke("Cabin Ready & Seated", State);
                    else OnPncStatusChanged?.Invoke("Crew Seated.", State);
                }
            }

            // --- Crisis & Silence Penalty Logic ---
            bool isSevereTurbulence = phase != FlightPhase.AtGate && phase != FlightPhase.Turnaround && isCrisisActive; 
            
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
                }
                else
                {
                    double elapsed = (DateTime.Now - _turbulenceReactionTimerStart.Value).TotalSeconds;
                    if (elapsed > 30 && !_hasPenalizedTurbulenceReaction)
                    {
                        _hasPenalizedTurbulenceReaction = true;
                        OnPenaltyTriggered?.Invoke(-100, LocalizationService.Translate("Pilot Inaction: No PA during severe turbulence/crisis", "Inaction Pilote : Pas d'annonce PA pendant la crise/turbulence"));
                        IncreaseAnxiety(20.0, phase, isCrisisActive, GetDynamicFeedback("SevereTurbulenceNoComm", phase), 95.0);
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
                    double effectiveRate = _currentSecuringRate * (Math.Max(5.0, CrewEsteem) / 100.0) * deltaTimeSeconds;
                    if (_strategicPenaltyEndTime.HasValue && DateTime.Now < _strategicPenaltyEndTime.Value)
                    {
                        effectiveRate *= 0.5; // Strategic Penalty: PNC distracted by Intercom Query
                    }
                    if (_pncActionDelaySeconds <= 0)
                    {
                        SecuringProgress += effectiveRate;
                    }
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
            
            if (phase != FlightPhase.AtGate && phase != FlightPhase.Turnaround && (gMax - gMin > 0.6))
            {
                IncreaseAnxiety(0.5, phase, isCrisisActive, GetDynamicFeedback("NoticeableTurbulence", phase), 85.0); 
                DecreaseComfort(0.5); // B.1 task: Turbulence vibrates cabin, dropping comfort
                ComfortReason = "Comfort degraded due to turbulence.";
                
                // Spillages cause cleanliness drop
                CabinCleanliness -= 0.1 * deltaTimeSeconds;
                if (CabinCleanliness < 0) CabinCleanliness = 0;
                
                if (!_seatbeltsOn && (DateTime.Now - _lastTurbulenceNotice).TotalSeconds > 30)
                {
                    OnPenaltyTriggered?.Invoke(-50, LocalizationService.Translate("Safety Violation: Severe Turbulence with Seatbelts OFF!", "Violation Sécurité: Fortes turbulences avec Ceintures DÉTACHÉES!"));
                    _lastTurbulenceNotice = DateTime.Now;
                }
                
                if (!_seatbeltsOn && PassengerAnxiety > 30 && (DateTime.Now - _lastTurbulenceNotice).TotalMinutes > 1 && altitude > 10000)
                {
                    OnCrewMessage?.Invoke("orange", LocalizationService.Translate(
                        "Captain, it's getting really bumpy back here. Please turn on the seatbelt sign!",
                        "Commandant, ça secoue vraiment derrière. Allumez le signal des ceintures s'il vous plaît !"
                    ), null);
                    _lastTurbulenceNotice = DateTime.Now;
                }
                else if (_seatbeltsOn && PassengerAnxiety > 30 && (DateTime.Now - _lastTurbulenceNotice).TotalMinutes > 5 && altitude > 10000)
                {
                    if (CrewEsteem >= 75)
                    {
                        OnCrewMessage?.Invoke("info", LocalizationService.Translate(
                            "Captain, it's getting really bumpy. I am proactively calling the passengers to sit down.",
                            "Commandant, ça secoue vraiment. J'annonce de suite aux passagers de s'asseoir."
                        ), null);
                        
                        // SeatbeltOn PA removed per user request
                        OnCrewMessage?.Invoke("orange", LocalizationService.Translate("PA: Please return to your seats and fasten your seatbelts.", "PA: Veuillez regagner vos sièges et attacher vos ceintures."), null);

                        CrewEsteem = Math.Min(100.0, CrewEsteem + 5.0); // Valorisation de la prise d'initiative
                        OnOperationBonusTriggered?.Invoke(50, LocalizationService.Translate("Proactive Crew Initiative (Turbulence PA)", "Initiative PNC Proactif (PA Turbulences)"));
                    }
                    else
                    {
                        OnCrewMessage?.Invoke("orange", LocalizationService.Translate(
                            "Captain, it's getting really bumpy back here. The passengers are getting anxious. Can you make an announcement?",
                            "Commandant, ça secoue vraiment derrière. Les passagers sont anxieux. Pouvez-vous faire une annonce ?"
                        ), null);
                        // Low proactivity: crew waits for captain and passenger satisfaction hurts
                        ModifySatisfaction(-2.0);
                    }
                    _lastTurbulenceNotice = DateTime.Now;
                }
            }
            
            if (phase != FlightPhase.AtGate && phase != FlightPhase.Turnaround && Math.Abs(bankAngle) > 33.0)
            {
                if ((DateTime.Now - _lastCabinBankPenalty).TotalSeconds > 5)
                {
                    _lastCabinBankPenalty = DateTime.Now;
                    IncreaseAnxiety(0.5, phase, isCrisisActive, GetDynamicFeedback("SteepBank", phase), 80.0);
                    if (ComfortLevel > 40.0) DecreaseComfort(0.2);
                }
            }
            
            if (sobt.HasValue && phase == FlightPhase.AtGate)
            {
                if (currentZulu > sobt.Value)
                {
                    var delaySpan = currentZulu - sobt.Value;
                    _currentDelayMinutes = delaySpan.TotalMinutes;
                }
                else
                {
                    _currentDelayMinutes = 0;
                }
                
                bool inCooldown = IsDelayCooldownActive;
                
                // Point 5: Pénalité de Retard Extrême (Fatigue d'Attente)
                // If delay is extreme, the apology cooldown "shield" breaks.
                if (inCooldown && _currentDelayMinutes > 45)
                {
                    inCooldown = false;
                    SatisfactionReason = "Passengers are exasperated. Apologies are no longer effective.";
                }
                
                // Point 6: Dégradation du Moral de l'Équipage (PNC Fatigue)
                // Prolonged delays drain the crew's esteem as they deal with angry passengers.
                if (_currentDelayMinutes > 45)
                {
                    CrewEsteem = Math.Max(0.0, CrewEsteem - (0.5 * (deltaTimeSeconds / 60.0)));
                }

                if (!inCooldown)
                {
                    // Continuous degradation based on how late we are
                    double satDropRate = 0; // per minute
                    double anxRiseRate = 0; // per minute
                    double comfDropRate = 0; // per minute
                    
                    if (_currentDelayMinutes > 60)
                    {
                        satDropRate = 4.0;
                        anxRiseRate = 2.0;
                        comfDropRate = 1.0;
                    }
                    else if (_currentDelayMinutes > 45)
                    {
                        satDropRate = 3.0;
                        anxRiseRate = 1.0;
                        comfDropRate = 0.8;
                    }
                    else if (_currentDelayMinutes > 30)
                    {
                        satDropRate = 2.0;
                        anxRiseRate = 0.5;
                        comfDropRate = 0.5;
                    }
                    else if (_currentDelayMinutes > 15)
                    {
                        satDropRate = 1.0;
                        anxRiseRate = 0.3;
                        comfDropRate = 0.3;
                    }
                    else if (_currentDelayMinutes > 5)
                    {
                        satDropRate = 0.5;
                        anxRiseRate = 0.1;
                        comfDropRate = 0.1;
                    }

                    if (satDropRate > 0)
                    {
                        if (Satisfaction > 5.0) ModifySatisfaction(-(satDropRate / 60.0) * deltaTimeSeconds);
                        if (_currentDelayMinutes <= 45) SatisfactionReason = $"Satisfaction decreasing due to prolonged departure delay ({Math.Round(_currentDelayMinutes)} mins).";
                    }
                    if (anxRiseRate > 0)
                    {
                        IncreaseAnxiety((anxRiseRate / 60.0) * deltaTimeSeconds, phase, isCrisisActive, GetDynamicFeedback("DepartureDelay", phase), 90.0);
                    }
                    if (comfDropRate > 0)
                    {
                        if (ComfortLevel > 5.0) DecreaseComfort((comfDropRate / 60.0) * deltaTimeSeconds);
                        ComfortReason = $"Comfort dropping due to departure delay ({Math.Round(_currentDelayMinutes)} mins).";
                    }
                }
                else
                {
                    SatisfactionReason = "Satisfaction stable: Delay was explained by the crew.";
                    AnxietyReason = "Anxiety stable: Passengers are reassured by crew announcements.";
                }

                // Point 7: Escalade des Messages d'Alerte PNC (Retards)
                if (_currentDelayMinutes > 15 && (DateTime.Now - _lastDelayNotice).TotalMinutes > 10 && !inCooldown)
                {
                    var mins = Math.Round(_currentDelayMinutes);
                    
                    if (_currentDelayMinutes > 90) 
                    {
                        // Phase 4: Extreme Delay
                        string alarmEn = $"Captain, this is unacceptable. It's been {mins} minutes, passengers are furious and we are exhausted.";
                        string alarmFr = $"Commandant, la situation est ingérable. Ça fait {mins} minutes, les passagers sont furieux et on est épuisés.";
                        
                        OnCrewMessage?.Invoke("red", LocalizationService.Translate(alarmEn, alarmFr), null);
                        TriggerIncomingCabinCall("Delay90m", "intercom_delay_warning_90m", alarmEn);
                        CrewEsteem = Math.Max(0.0, CrewEsteem - 10.0); // Huge hit to crew morale when getting yelled at for 90 mins
                    }
                    else if (_currentDelayMinutes > 60) 
                    {
                        // Phase 3: Alarme Critique
                        string alarmEn = $"Captain, we are {mins} minutes delayed. People are getting very upset in the back. We need an update NOW.";
                        string alarmFr = $"Commandant, nous avons {mins} minutes de retard. Les gens s'énervent vraiment à l'arrière. Il nous faut une info MAINTENANT.";
                        
                        OnCrewMessage?.Invoke("red", LocalizationService.Translate(alarmEn, alarmFr), null);
                        TriggerIncomingCabinCall("Delay60m", "intercom_delay_warning_60m", alarmEn);
                        CrewEsteem = Math.Max(0.0, CrewEsteem - 5.0); 
                    } 
                    else if (_currentDelayMinutes > 45) 
                    {
                        // Phase 2: Avertissement
                        string warnEn = $"Captain, it's been {mins} minutes. Passengers are starting to complain. Can we offer water?";
                        string warnFr = $"Commandant, ça fait {mins} minutes. Les passagers commencent à se plaindre. On peut leur proposer de l'eau ?";
                        
                        OnCrewMessage?.Invoke("orange", LocalizationService.Translate(warnEn, warnFr), null);
                        if (CrewEsteem >= 60)
                        {
                            AnnounceToCabin("Delay");
                            OnOperationBonusTriggered?.Invoke(20, LocalizationService.Translate("Proactive Crew Initiative (Delay PA)", "Initiative PNC Proactif (PA Retard)"));
                        }
                        else
                        {
                            TriggerIncomingCabinCall("Delay45m", "intercom_delay_warning_45m", warnEn);
                        }
                    }
                    else
                    {
                        // Phase 1: Standard (25-45 mins)
                        string stdEn = $"Captain, passengers are getting quite anxious with this delay. Any updates we can give them?";
                        string stdFr = $"Commandant, les passagers commencent à s'impatienter avec ce retard. Avez-vous des informations à leur communiquer ?";
                        
                        if (CrewEsteem >= 80)
                        {
                            OnCrewMessage?.Invoke("info", LocalizationService.Translate(stdEn, stdFr), null);
                            AnnounceToCabin("Delay");
                            CrewEsteem = Math.Min(100.0, CrewEsteem + 2.0);
                            OnOperationBonusTriggered?.Invoke(50, LocalizationService.Translate("Proactive Crew Initiative (Delay PA)", "Initiative PNC Proactif (PA Retard)"));
                        }
                        else
                        {
                            TriggerIncomingCabinCall("AnxiousPax", "pnc_anxious_pax", stdEn);
                        }
                    }
                    
                    _lastDelayNotice = DateTime.Now;
                }
            }

            // Environmental Anxiety (Night & Low Altitude Approach)
            if (phase != FlightPhase.AtGate && phase != FlightPhase.Turnaround && CurrentSimLocalTime != DateTime.MinValue)
            {
                if (CurrentSimLocalTime.Hour <= 5 || CurrentSimLocalTime.Hour >= 20)
                {
                    IncreaseAnxiety(0.002 * deltaTimeSeconds * (IsLowCost ? 1.5 : 1.0), phase, isCrisisActive, GetDynamicFeedback("NightFlight", phase), 15.0);
                }
                if (altitude < 1000 && phase == FlightPhase.Approach)
                {
                    IncreaseAnxiety(0.02 * deltaTimeSeconds * (IsLowCost ? 1.5 : 1.0), phase, isCrisisActive, GetDynamicFeedback("LowAltitudeApproach", phase), 25.0);
                }
            }

            if (phase == FlightPhase.TaxiOut && groundSpeed < 1.0 && isBoarded)
            {
                _cumulativeHoldSeconds += deltaTimeSeconds;
                if (_cumulativeHoldSeconds > 600 && (DateTime.Now - _lastHoldPenaltyTime).TotalMinutes > 10)
                {
                    _lastHoldPenaltyTime = DateTime.Now;
                    ModifySatisfaction(-10.0);
                    IncreaseAnxiety(10.0, phase, false, GetDynamicFeedback("TaxiPause", phase), 90.0);
                    OnPenaltyTriggered?.Invoke(-30, LocalizationService.Translate("Ground Delay: Aircraft immobilized > 10 min", "Retard au sol : Avion immobilisé > 10 min"));
                    OnCrewMessage?.Invoke("orange", LocalizationService.Translate("Captain, we've been sitting here without moving for over 10 minutes. Passengers are getting impatient.", "Commandant, on est immobiles depuis plus de 10 minutes. L'impatience monte."), null);
                }
            }
            else if (phase == FlightPhase.TaxiOut) 
            {
                _cumulativeHoldSeconds = 0;
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
                    int totalPax = Math.Max(1, PassengerManifest.Count);
                    double lccMultiplier = IsLowCost ? 0.6 : 1.0;
                    double hurryMultiplier = IsServiceHurried ? 0.5 : 1.0;
                    
                    // Target 15-20 mins for full cabin. 1080 seconds max (18 mins)
                    double passTimeCost = totalPax * 6.5; // ~6.5 seconds per pax
                    double efficiencyFactor = 100.0 / Math.Max(5.0, CrewEsteem);
                    double projectedSeconds = passTimeCost * efficiencyFactor * lccMultiplier * hurryMultiplier;
                    
                    double baseRate = (100.0 / Math.Max(1.0, projectedSeconds)) * deltaTimeSeconds;
                    
                    double prevProgress = InFlightServiceProgress;
                    if (_pncActionDelaySeconds <= 0)
                    {
                        InFlightServiceProgress += baseRate;
                    }
                    
                    double prevRatio = prevProgress / 100.0;
                    double currentRatio = InFlightServiceProgress / 100.0;
                    
                    int prevExpectedEaten = (int)Math.Round(prevRatio * totalPax * 0.95);
                    int currentExpectedEaten = (int)Math.Round(currentRatio * totalPax * 0.95);
                    int eatenThisTick = currentExpectedEaten - prevExpectedEaten;
                    
                    if (eatenThisTick > 0)
                    {
                        CateringRations = Math.Max(0, CateringRations - eatenThisTick);
                    }

                    if (InFlightServiceProgress >= 100.0)
                    {
                        InFlightServiceProgress = 100.0;
                        State = CabinState.Idle;
                        IsServiceHurried = false;
                        OnCrewMessage?.Invoke("green", LocalizationService.Translate("Meal service is complete, cabin is clear.", "Le service des repas est terminé, la cabine est dégagée."), null);
                        OnPncStatusChanged?.Invoke("Idle", State);
                    }
                }
            }
            
            // --- TICKET: PREVIOUS LEGS CLEANUP ---
            if (State == CabinState.Deboarding && PassengerManifest.Count == 0 && PreviousLegManifest.Count > 0)
            {
                PreviousLegManifest.Clear();
            }
            
            // Validate Meal Shortage (Catering Stock empty during cruise or service)
            if ((phase == FlightPhase.Cruise || State == CabinState.ServingMeals) && !_hasTriggeredCateringComplaint && CateringCompletion <= 0.0 && InFlightServiceProgress < 100.0 && InFlightServiceProgress > 0.0)
            {
                _hasTriggeredCateringComplaint = true;
                string msg = "Captain, we have totally run out of meals for the remaining passengers. They are very unhappy.";
                TriggerIncomingCabinCall("CateringMissing", "TO_FD/Incoming_Calls/CateringMissing", msg);
                IncreaseAnxiety(30.0, phase, isCrisisActive, GetDynamicFeedback("CateringShortage", phase), 60.0);
                ModifySatisfaction(-50.0);
                CrewEsteem = Math.Max(0.0, CrewEsteem - 20.0);
                OnPenaltyTriggered?.Invoke(-100, LocalizationService.Translate("Catering Shortage: Out of meals", "Rupture Catering : Plus de repas disponibles")); 

                if (State == CabinState.ServingMeals)
                {
                    InFlightServiceProgress = 100.0;
                    State = CabinState.Idle;
                    IsServiceHurried = false;
                    OnPncStatusChanged?.Invoke("Idle", State);
                }
            }


        }
        
        public void AnnounceToCabin(string announcementType)
        {
            // Only Turbulence and DelayApology are allowed to be played multiple times (e.g., multiple turbulences)
            bool isRepeatable = announcementType == "Turbulence" || announcementType == "DelayApology";

            if (!isRepeatable && _issuedCommands.Contains("PA_" + announcementType))
            {
                return; // One-shot PA already played
            }

            if (!_issuedCommands.Contains("PA_" + announcementType))
            {
                _issuedCommands.Add("PA_" + announcementType);
                OnOperationBonusTriggered?.Invoke(25, "Passenger Announcement: " + announcementType);
                
                // --- SCORING TRACKERS ---
                if (announcementType == "Welcome") HasPlayedWelcomePA = true;
                if (announcementType == "Descent") HasPlayedDescentPA = true;
                if (announcementType == "GoAround" || announcementType == "Abnormal") HasAnnouncedGoAroundPA = true;
                // -------------------------
            }

            // Manual PA implies micromanagement by the pilot, lowering crew morale slightly
            CrewEsteem = Math.Max(0.0, CrewEsteem - 2.0);

            if (announcementType == "Turbulence")
            {
                DecreaseAnxiety(25.0);
                PlayDynamicAsCaptain("pa_turbulence", null);
                OnCrewMessage?.Invoke("orange", LocalizationService.Translate("PA: Please return to your seats and fasten your seatbelts.", "PA: Veuillez regagner vos sièges et attacher vos ceintures."), null);
            }
            else if (announcementType == "TurbulenceApology")
            {
                DecreaseAnxiety(20.0);
                ModifySatisfaction(5.0);
                PlayDynamicAsCaptain("pa_turbulence_apology", null);
                OnCrewMessage?.Invoke("info", LocalizationService.Translate("PA: Apologies for the rough ride.", "PA: Excuses suite aux turbulences."), null);
            }
            else if (announcementType == "CruiseStatus")
            {
                _timeOfLastDelayPA = DateTime.Now;
                DecreaseAnxiety(5.0);
                ModifySatisfaction(10.0);
                string destName = CurrentFlight?.Destination?.Name ?? CurrentFlight?.Destination?.IcaoCode ?? "our destination";
                PlayDynamicAsCaptain("pa_update", null);
                OnCrewMessage?.Invoke("info", LocalizationService.Translate($"PA: Cruising smoothly towards {destName}.", $"PA: Nous croisons paisiblement vers {destName}."), null);
            }
            else if (announcementType == "DelayApology")
            {
                _timeOfLastDelayPA = DateTime.Now;
                DecreaseAnxiety(15.0);
                ModifySatisfaction(5.0);
                PlayDynamicAsCaptain("pa_delay_apology", null);
                OnCrewMessage?.Invoke("info", LocalizationService.Translate("PA: Apology for the earlier delay.", "PA: Nouvelles excuses pour le retard passé."), null);
            }

        }

        public void AnnounceDelay(string reason, string destName)
        {
            if (!_issuedCommands.Contains("PA_Delay"))
            {
                _issuedCommands.Add("PA_Delay");
            }
            
            if (HasActiveSoftFailure && reason == ActiveSoftFailureReason)
            {
                HasActiveSoftFailure = false;
                ActiveSoftFailureReason = "";
                OnCrewMessage?.Invoke("green", LocalizationService.Translate("[PNC] Thanks Captain. Maintenance is on it, the issue will be resolved soon.", "[PNC] Merci Commandant. La maintenance s'en occupe, ce sera vite réglé."), null);
                ModifySatisfaction(15.0);
            }
            
            _delayPaCount++;
            _timeOfLastDelayPA = DateTime.Now;
            
            int cooldownMinutes = 0;
            if (_delayPaCount == 1) cooldownMinutes = 15;
            else if (_delayPaCount == 2) cooldownMinutes = 10;
            else if (_delayPaCount == 3) cooldownMinutes = 5;

            if (cooldownMinutes > 0)
            {
                _delayCooldownEndTime = DateTime.Now.AddMinutes(cooldownMinutes);
                DecreaseAnxiety(5.0);
                ModifySatisfaction(5.0);
                
                string spokenReason = reason.ToLower() switch {
                    "atc" => "ATC clearance",
                    "traffic" => "ATC clearance",
                    "luggage" => "luggage loading",
                    "bags" => "luggage loading",
                    "weather" => "bad weather",
                    "passengers" => "late connecting passengers",
                    "pax" => "late connecting passengers",
                    "technical" => "technical checks",
                    "cargo" => "cargo loading",
                    "catering" => "catering supplies",
                    _ => "ATC clearance"
                };

                string subfolder = reason.ToLower() switch {
                    "atc" => "ATC",
                    "traffic" => "ATC",
                    "weather" => "Weather",
                    "luggage" => "GroundOps",
                    "bags" => "GroundOps",
                    "cargo" => "GroundOps",
                    "catering" => "GroundOps",
                    "technical" => "Technical",
                    "passengers" => "Boarding",
                    "pax" => "Boarding",
                    _ => "ATC"
                };

                string spokenText = $"Ladies and gentlemen from the flight deck, I'd like to apologize for the delay. We are currently waiting for {spokenReason} and expect to be moving in about {Math.Max(10, Math.Round(_currentDelayMinutes))} minutes. Thank you for your patience.";
                PlayDynamicAsCaptain($"pa_delay_{subfolder.ToLower()}", spokenText);

                OnCrewMessage?.Invoke("orange", LocalizationService.Translate($"PA: Apologies for the delay ({spokenReason}), we will be departing shortly. (Passengers pacified for {cooldownMinutes} min)", $"PA: Toutes nos excuses pour ce retard ({spokenReason}), nous partons bientôt. (Passagers calmés pour {cooldownMinutes} min)"), null);
            }
            else
            {
                ModifySatisfaction(-15.0);
                IncreaseAnxiety(10.0, FlightPhase.AtGate, false, GetDynamicFeedback("RepeatedDelayExcuses", FlightPhase.AtGate), 90.0);
                OnCrewMessage?.Invoke("red", LocalizationService.Translate("PA: Apologies for the delay... (Passengers are groaning, the excuses are no longer working!)", "PA: Toutes nos excuses... (Les passagers râlent, vos excuses ne marchent plus !)"), null);
            }
        }

        private string NumberToExactWord(int number)
        {
            if (number == 0) return "Zero";
            string[] ones = {"", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"};
            string[] tens = {"", "", "Twenty", "Thirty", "Forty", "Fifty"};
            if (number < 20) return ones[number];
            return tens[number / 10] + ones[number % 10];
        }



        private string GetTempMacro(int tempC)
        {
            if (tempC < 0) return $"m{Math.Abs(tempC)}";
            return tempC.ToString();
        }

        public void AnnounceWelcome(string destIcao, string destName, int flightTimeMinutes, FlightSupervisor.UI.Models.BriefingData briefingData, int destTempC, DateTime destLocalTime, DateTime departureLocalTime, string maxTurbStr)
        {
            if (!_issuedCommands.Contains("PA_Welcome"))
            {
                _issuedCommands.Add("PA_Welcome");
                OnOperationBonusTriggered?.Invoke(25, "Passenger Announcement: Welcome");
            }

            var destStation = briefingData?.Stations?.FirstOrDefault(s => s.Id == "destination");
            bool badWeather = destStation != null && (destStation.WindSeverity == FlightSupervisor.UI.Models.WeatherSeverity.Danger || destStation.VisibilitySeverity == FlightSupervisor.UI.Models.WeatherSeverity.Danger || destStation.CloudSeverity == FlightSupervisor.UI.Models.WeatherSeverity.Danger || destStation.RawMetar.Contains(" TS") || destStation.RawMetar.Contains(" SN"));

            if (!badWeather) {
                DecreaseAnxiety(20.0);
            } else {
                IncreaseAnxiety(15.0, FlightPhase.AtGate, false, GetDynamicFeedback("WeatherArr", FlightPhase.AtGate), 25.0);
            }

            string wxcText = !badWeather ? "looking great" : "quite poor today";
            string wxcFr = !badWeather ? "très bonne" : "assez mauvaise aujourd'hui";
            string aircraftType = "a320"; // Safe default
            if (CurrentFlight?.Aircraft != null)
            {
                if (!string.IsNullOrWhiteSpace(CurrentFlight.Aircraft.BaseType)) aircraftType = CurrentFlight.Aircraft.BaseType.ToLower();
            }

            string greeting = "morning";
            if (departureLocalTime.Hour >= 12 && departureLocalTime.Hour < 18) greeting = "afternoon";
            else if (departureLocalTime.Hour >= 18) greeting = "evening";

            string normAirline = ActiveAirlineId?.ToLower() ?? "generic";
            if (normAirline.Contains("eju") || normAirline.Contains("ezs") || normAirline.Contains("ezy") || normAirline.Contains("easyjet")) normAirline = "easyjet";
            else if (normAirline.Contains("afr") || normAirline.Contains("air_france") || normAirline.Contains("airfrance")) normAirline = "airfrance";

            string destIcaoStr = destIcao?.ToLower() ?? "generic";

            PlayDynamicAsCaptain($"pa_welcome_intro_01_{greeting}_{aircraftType}_{normAirline}_{destIcaoStr}", null, playChime: true);

            int eH = flightTimeMinutes / 60;
            int eM = (flightTimeMinutes % 60);
            eM = (int)(Math.Round(eM / 5.0) * 5);
            if (eM == 60) { eM = 0; eH++; }
            PlayDynamicAsCaptain($"pa_welcome_flight_time_{eH}h{eM:D2}", null, playChime: false);

            int turb = 0;
            int.TryParse(maxTurbStr, out turb);
            int wIdxEnroute = 1;
            if (turb >= 4) wIdxEnroute = 4;
            else if (turb >= 3) wIdxEnroute = 3;
            else if (turb >= 2) wIdxEnroute = 2;

            int wIdxArrival = 1;
            if (destStation != null)
            {
                string metar = destStation.RawMetar.ToUpper();
                if (metar.Contains(" TS") || metar.Contains(" CB")) wIdxArrival = 5;
                else if (metar.Contains(" SN") || metar.Contains(" SG") || metar.Contains(" FZ") || metar.Contains(" IC") || metar.Contains(" GR")) wIdxArrival = 6;
                else if (metar.Contains(" RA") || metar.Contains(" DZ") || metar.Contains(" SH")) wIdxArrival = 4;
                else if (destStation.CloudSeverity >= FlightSupervisor.UI.Models.WeatherSeverity.Warning || destStation.VisibilitySeverity >= FlightSupervisor.UI.Models.WeatherSeverity.Warning || metar.Contains(" OVC") || metar.Contains(" BKN") || metar.Contains(" FG")) wIdxArrival = 3;
                else if (metar.Contains(" SCT") || metar.Contains(" FEW")) wIdxArrival = 2;
            }

            PlayDynamicAsCaptain($"pa_welcome_enroute_weather_{wIdxEnroute:D2}", null, playChime: false);
            PlayDynamicAsCaptain($"pa_welcome_arrival_weather_{wIdxArrival:D2}", null, playChime: false);
            
            PlayDynamicAsCaptain("pa_welcome_outro_01", null, playChime: false);

            string timeStr = $"{eH}h {eM:D2}m";
            string notifText = $"PA: Welcome aboard our flight to {destName}. Our flight time will be approx {timeStr}. The weather at our destination is currently {wxcText}.";
            string notifTextFr = $"PA: Bienvenue à bord de notre vol vers {destName}. Notre temps de vol sera d'environ {timeStr}. La météo à notre destination est {wxcFr}.";
            
            OnCrewMessage?.Invoke("info", LocalizationService.Translate(notifText, notifTextFr), null);
        }

        public void AnnounceDescent(string destIcao, string destName, string metar, int destTempC)
        {
            if (!_issuedCommands.Contains("PA_Descent"))
            {
                _issuedCommands.Add("PA_Descent");
                OnOperationBonusTriggered?.Invoke(25, "Passenger Announcement: Descent");
                HasPlayedDescentPA = true;
            }

            DecreaseAnxiety(10.0);
            
            PlayDynamicAsCaptain("pa_descent_intro_01", null, playChime: true);
            PlayDynamicAsCaptain($"pa_descent_temp_{GetTempMacro(destTempC)}", null, playChime: false);
            PlayDynamicAsCaptain("pa_descent_outro_01", null, playChime: false);

            OnCrewMessage?.Invoke("info", LocalizationService.Translate($"PA: Descent update for {destName}.", $"PA: Point sur la descente vers {destName}."), null);
        }



        public void AnnounceCruise(int altitude, string avgWindComp, string destIcao, string destName, string destMetar, string enrtMetar, int destTempC, DateTime currentLocalTime)
        {
            if (!_issuedCommands.Contains("PA_CruiseStatus"))
            {
                _issuedCommands.Add("PA_CruiseStatus");
                OnOperationBonusTriggered?.Invoke(15, "Passenger Announcement: Cruise Update");
            }

            int roundedAlt = (int)(Math.Round(altitude / 1000.0) * 1000);
            if (roundedAlt < 20000) roundedAlt = 20000;
            if (roundedAlt > 43000) roundedAlt = 43000;
            
            PlayDynamicAsCaptain($"pa_cruise_altitude_{roundedAlt}", null, playChime: true);

            OnCrewMessage?.Invoke("green", LocalizationService.Translate("PA: Cruise Update (Altitude & Enroute Weather)", "PA: Informations de Croisière (Altitude et météo)"), null);
        }
        
        private string GetDynamicFeedback(string category, FlightPhase phase)
        {
            Random r = new Random();
            string[] variations;
            switch (category)
            {
                case "LandingBounce":
                    variations = new[] { 
                        "Passengers are terrified by the aircraft bouncing!", 
                        "The harsh bounce upon landing has terrified the cabin.", 
                        "Passengers let out gasps of fear after the violent bounce.",
                        "The heavy impact and bounce sent shockwaves through the cabin.",
                        "A brutal touchdown causing the plane to bounce has sparked panic.",
                        "People are screaming as the plane bounces violently off the runway!"
                    };
                    break;
                case "MovementNoSeatbelts":
                    variations = new[] { 
                        "Passengers are nervous because the aircraft is moving without seatbelts.", 
                        "Moving the aircraft while the seatbelt sign is off is causing severe confusion.", 
                        "Some standing passengers nearly fell! Anxiety is spiking.",
                        "Cabin crew are shouting at passengers to sit down as the aircraft lurches.",
                        "The unannounced movement caught everyone off guard, causing a brief panic."
                    };
                    break;
                case "ProlongedSeatbelt":
                    variations = new[] { 
                        "Anxiety rising due to prolonged seatbelt sign and inability to use restrooms.", 
                        "Passengers are very uncomfortable and anxious about the endless seatbelt restriction.", 
                        "The cabin is restless; people desperately need to stretch and use the lavatories.",
                        "Children are crying as the seatbelt sign remains on indefinitely.",
                        "Growing frustration as the endless seatbelt restriction causes discomfort."
                    };
                    break;
                case "CloudAnxiety":
                    if (phase == FlightPhase.Climb || phase == FlightPhase.InitialClimb)
                        variations = new[] { "Passengers feel uneasy climbing through the thick cloud layer.", "The bumpy ascent through the clouds is raising anxiety.", "Disorientation as the aircraft climbs blindly through thick clouds.", "A grey soup outside the window is making flyers very nervous." };
                    else if (phase == FlightPhase.Cruise)
                        variations = new[] { "Flying through persistent clouds is making some passengers nervous.", "Lack of ground visibility in the clouds is causing slight unease.", "The endless expanse of grey clouds outside is causing claustrophobia.", "A long stretch inside the clouds has quieted the cabin nervously." };
                    else
                        variations = new[] { "The low visibility and thick clouds during approach are raising anxiety.", "Descending through heavy clouds makes passengers grip their armrests.", "A blind descent into the clouds is terrifying some passengers.", "You can't even see the wings! Approach anxiety is spiking." };
                    break;
                case "TakeoffAccel":
                    variations = new[] { 
                        "Passengers feel the pressure of takeoff acceleration.", 
                        "The immense acceleration of takeoff is overwhelming some passengers.", 
                        "The roar of the engines and sudden speed is causing tension in the cabin.",
                        "Being pinned to their seats by the raw takeoff power has caused some gasps.",
                        "The sheer force of the takeoff roll is making nervous flyers shake."
                    };
                    break;
                case "ThrustReduct":
                    variations = new[] { 
                        "Thrust reduction felt in cabin. Passengers experienced a brief moment of anxiety.", 
                        "The sudden engine thrust reduction caused a brief sensation of falling.", 
                        "A sudden quietness from the engines made some passengers hold their breath.",
                        "The noticeable drop in engine noise led to panicked whispers.",
                        "Some passengers gasped as they felt the aircraft temporarily level off."
                    };
                    break;
                case "FlightDelay":
                    variations = new[] { 
                        "Passengers are getting anxious because the flight is taking longer than expected.", 
                        "The prolonged flight time is wearing down passenger patience and comfort.", 
                        "People are checking their watches; the delay in the air is raising tension.",
                        "Frustration mounts as the scheduled arrival time passes by.",
                        "The endlessly stretching flight time is causing cabin-wide fatigue."
                    };
                    break;
                case "HoldingPattern":
                    variations = new[] { 
                        "Passengers are anxious because the aircraft is flying in circles.", 
                        "The continuous holding pattern is causing dizziness and frustration.", 
                        "Circling around the destination without landing is making the cabin nervous.",
                        "Looking out the window and seeing the same terrain over and over is causing unease.",
                        "The endless right turns in the hold are inducing motion sickness."
                    };
                    break;
                case "SevereTurbulenceNoComm":
                    variations = new[] { 
                        "Passengers are terrified by severe turbulence and lack of communication from the cockpit.", 
                        "The violent shaking combined with radio silence has induced sheer panic!", 
                        "People are screaming; the lack of a reassuring PA is making the severe turbulence unbearable.",
                        "A massive jolt with no pilot update has convinced some passengers this is the end.",
                        "The terrifying bumps in dead silence are causing mass panic."
                    };
                    break;
                case "NoticeableTurbulence":
                    variations = new[] { 
                        "Anxiety elevated due to noticeable turbulence.", 
                        "Noticeable turbulence is causing a stir in the cabin.", 
                        "Passengers are uncomfortable with the continuous bumps.",
                        "Coffee is spilling; the steady chop is eroding cabin comfort.",
                        "The constant shaking is making it impossible to relax or sleep."
                    };
                    break;
                case "SteepBank":
                    variations = new[] { 
                        "Passengers are anxious due to steep banking maneuvers.", 
                        "The sharp turn made several passengers grip their armrests tightly.", 
                        "An unusually steep bank angle has caused a wave of unease in the cabin.",
                        "Looking straight down at the ground through the window caused a panic.",
                        "The aggressive turn maneuver triggered gasps from the aisle seats."
                    };
                    break;
                case "DepartureDelay":
                    variations = new[] { 
                        $"Anxiety rising due to departure delay ({Math.Round(_currentDelayMinutes)} mins).", 
                        $"Passengers are growing increasingly frustrated by the {Math.Round(_currentDelayMinutes)} minute delay.", 
                        $"The extended wait on the ground is causing significant discomfort and tension.",
                        $"The stagnant cabin air and long delay is boiling over into anger.",
                        $"Mutterings of complaint echo as the delay continues."
                    };
                    break;
                case "NightFlight":
                    variations = new[] { 
                        "Slight unease due to flying late at night.", 
                        "The pitch black darkness outside the windows makes some passengers nervous.", 
                        "Flying at this late hour is contributing to a restless cabin atmosphere.",
                        "The spooky darkness of the red-eye flight is causing unease.",
                        "It is completely black outside, amplifying any bumps or noises."
                    };
                    break;
                case "LowAltitudeApproach":
                    variations = new[] { 
                        "Anxiety rising during low altitude approach.", 
                        "Flying so low to the ground for an extended period is making passengers nervous.", 
                        "The ground seems too close! Passengers are eagerly awaiting touchdown.",
                        "Skimming the treetops for minutes on end is terrifying the window seats.",
                        "The loud roar of the flaps and low altitude is raising pulses."
                    };
                    break;
                case "TaxiPause":
                    variations = new[] { 
                        "Passengers are getting impatient and anxious sitting motionless on the taxiway.", 
                        "The unexplained stop on the taxiway is causing murmurs of discontent.", 
                        "Being stuck on the ground without moving is draining passenger morale.",
                        "The engines are idle but we aren't moving; confusion reigns.",
                        "People are starting to unbuckle prematurely during this long taxi delay."
                    };
                    break;
                case "CateringShortage":
                    variations = new[] { 
                        "Passengers are upset and anxious due to food and water shortages.", 
                        "The lack of meals has caused anger and anxiety to ripple through the cabin.", 
                        "Hunger and thirst are leading to open complaints and severe dissatisfaction.",
                        "A fight almost broke out over the last bottle of water.",
                        "The missing catering has ruined the flight experience for many."
                    };
                    break;
                case "RepeatedDelayExcuses":
                    variations = new[] { 
                        "Passengers are losing patience with the repeated delay excuses.", 
                        "The captain's repeated apologies are no longer working; anger is rising.", 
                        "Empty promises about departure times are destroying passenger trust.",
                        "Another excuse from the flight deck is met with collective groans.",
                        "Passengers no longer believe the crew; frustration is at an all-time high."
                    };
                    break;
                case "WeatherArr":
                    variations = new[] { 
                        "Passengers anxious due to poor weather at destination.", 
                        "Hearing about the bad weather at the arrival airport has raised concerns.", 
                        "The forecast for the destination is making passengers nervous about the landing.",
                        "The PA about thunderstorms at our destination has caused widespread worry.",
                        "Passengers are dreading a bumpy approach after hearing the weather update."
                    };
                    break;
                case "WeatherDep":
                    variations = new[] { 
                        "Passengers anxious due to poor weather at departure.", 
                        "The heavy rain and poor visibility outside are making passengers uneasy.", 
                        "Departing in such bad weather conditions has elevated cabin anxiety.",
                        "The violent wind rocking the plane at the gate is terrifying everyone.",
                        "Looking at the dark storm clouds before takeoff has rattled their nerves."
                    };
                    break;
                default:
                    return "Passenger comfort or anxiety levels have changed.";
            }
            return variations[r.Next(variations.Length)];
        }

        private void IncreaseAnxiety(double amount, FlightPhase phase, bool isCrisisActive, string reason = null, double? maxAverageCap = null)
        {
            double previousAnxiety = PassengerAnxiety;
            double multiplier = BaseAnxietySpikeMultiplier;
            if (IsSilencePenaltyActive) multiplier *= 2.0;

            double inc = (amount * multiplier);
            bool isOnGround = phase == FlightPhase.AtGate || phase == FlightPhase.Pushback || phase == FlightPhase.TaxiOut || phase == FlightPhase.TaxiIn || phase == FlightPhase.Arrived;
                              
            if (isOnGround && (PassengerAnxiety + inc) > 60.0) 
            {
                inc = Math.Max(0, 60.0 - PassengerAnxiety);
            }
            else if (!isCrisisActive && (PassengerAnxiety + inc) > 90.0)
            {
                inc = Math.Max(0, 90.0 - PassengerAnxiety);
            }
            
            if (maxAverageCap.HasValue && (PassengerAnxiety + inc) > maxAverageCap.Value)
            {
                inc = Math.Max(0, maxAverageCap.Value - PassengerAnxiety);
            }

            if (IsSatietyActive && phase == FlightPhase.Cruise) inc *= 0.5; 
            
            if (inc > 0)
            {
                ModifyAnxiety(inc);
                DecreaseComfort((inc * BaseComfortLossMultiplier) * 0.5); 
                if (!string.IsNullOrEmpty(reason))
                {
                    AnxietyReason = reason;
                }
            }
        }
        private void DecreaseAnxiety(double amount) { ModifyAnxiety(-amount); }

        private void DecreaseComfort(double amount) { ModifyComfort(-amount); }

        private void IncreaseComfort(double amount) { ModifyComfort(amount); }

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

        public void Reset(bool isTurnaround = false)
        {
            ClearAnxiety();
            
            if (!isTurnaround)
            {
                State = CabinState.Idle;
                HasBoardingStarted = false;
                _lastBoardingTick = DateTime.MaxValue;
            }

            _hasAnnouncedBoardingComplete = false;
            _hasAnnouncedGalleySecured = false;
            _issuedCommands.Clear();

            _thermalDissatisfactionGauge = 0.0;
            _hasWarnedPushbackNoSeatbelts = false;
            HasPenalizedRefuelingSeatbelts = false;
            _hasWarnedThermal = false;
            
            InFlightServiceProgress = 0.0;
            IsSatietyActive = false;
            IsServiceHalted = false;
            IsServiceHurried = false;
            _isPreparingService = false;
            _servicePrepTimerStart = null;
            
            if (SessionFlightsCompleted == 0)
            {
                SetSatisfaction(Math.Round(80.0 + (_rnd.NextDouble() * 16.0), 1));
                _manualApologyCount = 0;
                double proactivity = Math.Round(30.0 + (_rnd.NextDouble() * 70.0));
                double efficiency = Math.Round(60.0 + (_rnd.NextDouble() * 40.0));
                double morale = 100.0;
                CrewEsteem = Math.Round((proactivity + efficiency + morale) / 3.0);
                VirtualFuelPercentage = 0.0;
            }
            else
            {
                // Slight morale recovery during turnaround (+10%)
                CrewEsteem = Math.Min(100.0, CrewEsteem + 10.0);
            }

            _gForceHistory.Clear();
            _lastTurbulenceNotice = DateTime.MinValue;
            _lastDelayNotice = DateTime.MinValue;
            _lastRandomEvent = DateTime.Now;
            _hasTriggeredCateringComplaint = false;
            _hasAppliedDepartureWeatherAnxiety = false;
            _hasAppliedArrivalWeatherAnxiety = false;
            _hasPlayedDescentPA = false;
            _hasPlayedApproachPncPA = false;
            _hasPlayedSeatbeltOffPA = false;
            _hasTriggeredServicePrep = false;
            _hasWarnedToiletsFull = false;
            _hasWarnedTempHot = false;
            _hasWarnedTempCold = false;
            _hasWarnedPushbackNoSeatbelts = false;
            _hasPenalizedTurbulenceReaction = false;
            _hasTriggeredThrustReductionAnxiety = false;
            HasPenalizedRefuelingSeatbelts = false;
            _actualTakeoffTime = null;
            
            if (SessionFlightsCompleted == 0 && FirstFlightClean)
            {
                CateringCompletion = 0.0;
                CabinCleanliness = 100.0;
                WaterLevel = 100.0;
                WasteLevel = 0.0;
                VirtualFuelPercentage = 15.0;
            }
            else if (SessionFlightsCompleted == 0 && !FirstFlightClean)
            {
                // Catering ALWAYS empty on a fresh session, even if plane is dirty from previous day
                CateringCompletion = 0.0; 
                
                // Fixed dirtiness baseline
                double minCleanliness = 60.0;
                CabinCleanliness = Math.Round(_rnd.NextDouble() * (100.0 - minCleanliness) + minCleanliness, 1);
                
                WaterLevel = Math.Round(_rnd.NextDouble() * 30.0 + 70.0, 1); // 70-100%
                WasteLevel = Math.Round(_rnd.NextDouble() * 20.0 + 10.0, 1); // 10-30%
                VirtualFuelPercentage = 15.0; // 3 tons
            }
            
            BaggageCompletion = 100.0;
            _lastPhase = FlightPhase.AtGate;
            _comfortSum = 0;
            _comfortSamples = 0;
        }
        
        private void UpdatePassengerStates(FlightPhase phase, bool isSevere)
        {
            foreach (var p in PassengerManifest)
            {
                if (p == null) continue;
                if (!p.IsBoarded) continue;

                bool shouldFasten = _seatbeltsOn;

                if (p.Demographic == PassengerDemographic.Grumpy)
                {
                    if (shouldFasten && p.IsSeatbeltFastened && _rnd.Next(1000) < 2) 
                        p.IsSeatbeltFastened = false;
                    else if (shouldFasten && !p.IsSeatbeltFastened && _rnd.Next(100) < 5) 
                        p.IsSeatbeltFastened = true;
                    else if (!shouldFasten && p.IsSeatbeltFastened && _rnd.Next(100) < 20) 
                        p.IsSeatbeltFastened = false;
                }
                else if (p.Demographic == PassengerDemographic.Anxious)
                {
                    if (!p.IsSeatbeltFastened && _rnd.Next(100) < 15)
                        p.IsSeatbeltFastened = true;
                }
                else 
                {
                    if (shouldFasten)
                    {
                        if (!p.IsSeatbeltFastened && _rnd.Next(100) < 15) p.IsSeatbeltFastened = true;
                    }
                    else 
                    {
                        if (phase == FlightPhase.AtGate || phase == FlightPhase.Turnaround) 
                        {
                             // Maintain around 33% fastened when boarding/at gate
                             if (p.IsSeatbeltFastened && _rnd.Next(100) < 5) p.IsSeatbeltFastened = false;
                             else if (!p.IsSeatbeltFastened && _rnd.Next(100) < 2) p.IsSeatbeltFastened = true;
                        }
                        else
                        {
                             if (!p.IsSeatbeltFastened && _rnd.Next(1000) < 10) p.IsSeatbeltFastened = true;
                             else if (p.IsSeatbeltFastened && _rnd.Next(1000) < 20) p.IsSeatbeltFastened = false;
                        }
                    }
                }

                /* [URGENCES SCOPE - TEMPORARILY DISABLED]
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
                */
            }

            /* [URGENCES SCOPE - TEMPORARILY DISABLED]
            if (injuryCount > 0)
            {
                OnMedicalEmergencyRequested?.Invoke();
            }
            */
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

            int boardedCount = PassengerManifest.Count(p => p != null && p.IsBoarded);
            if (phase == FlightPhase.AtGate && boardedCount < PassengerManifest.Count)
            {
                string repEn = boardedCount == 0 
                    ? "Cabin checks are complete. We are ready when you are to begin boarding." 
                    : "We are still waiting for boarding to finish, Captain.";
                
                string repFr = boardedCount == 0 
                    ? "Les vérifications cabine sont terminées. Nous sommes prêts à débuter l'embarquement." 
                    : "Nous attendons la fin de l'embarquement, Commandant.";
                
                if (boardedCount == 0 && PassengerManifest.Count > 0)
                {
                    PlayDynamicAsPurser("pnc_cabin_secure_takeoff", repEn, false, "to_fd", false);
                }
                else
                {
                    PlayDynamicAsPurser("pnc_boarding_in_progress", repEn, false, "to_fd", false);
                }
                
                OnCrewMessage?.Invoke("info", LocalizationService.Translate(repEn, repFr), null);
                return true;
            }

            if (_isSecuring && SecuringProgress < 100.0)
            {
                _strategicPenaltyEndTime = DateTime.Now.AddSeconds(15);
                string reportEn = "Captain, we're still securing the cabin. We cannot provide a status update right now.";
                string reportFr = "Commandant, nous sécurisons actuellement la cabine. Impossible de faire un point pour le moment.";
                
                PlayDynamicAsPurser("pnc_securing_in_progress", reportEn, false, "to_fd", false);
                OnCrewMessage?.Invoke("warning", LocalizationService.Translate(reportEn, reportFr), null);
                return true;
            }

            return ExecuteStandardFlightReport(phase, isCrisisActive);
        }

        public void PlayCabinReady()
        {
            string repEn = "Cabin checks are complete. We are ready when you are to begin boarding.";
            TriggerIncomingCabinCall("CabinReady", "pnc_cabin_secure_takeoff", repEn);
        }

        private bool ExecuteStandardFlightReport(FlightPhase phase, bool isCrisisActive)
        {
            string reportEn = "Captain, everything is fine in the cabin.";
            string reportFr = "Commandant, tout se passe bien en cabine.";
            string macroName = "pnc_calm";

            if (State == CabinState.Idle)
            {
                if (phase == FlightPhase.Climb || phase == FlightPhase.InitialClimb)
                {
                    reportEn = "We're currently waiting for the seatbelt sign to be turned off so we can prepare for service.";
                    reportFr = "Nous attendons l'extinction de la consigne des ceintures pour pouvoir préparer le service.";
                    macroName = "pnc_calm";
                }
                else if (phase == FlightPhase.Cruise)
                {
                    if (InFlightServiceProgress == 0 && !IsServiceHalted) 
                    {
                        reportEn = "We are organizing the galleys for the upcoming service.";
                        reportFr = "Nous organisons les galleys en vue du service à bord.";
                        macroName = "pnc_calm";
                    }
                    else if (InFlightServiceProgress >= 100)
                    {
                        reportEn = "The meal service is completed and we've cleared the trays.";
                        reportFr = "Le service est terminé et les plateaux ont été débarrassés.";
                        macroName = "pnc_calm";
                    }
                }
                else if (phase == FlightPhase.Descent)
                {
                    reportEn = "We are doing a final pass to collect trash and prepare the cabin for arrival.";
                    reportFr = "Nous faisons un dernier passage pour débarrasser les détritus et préparer l'arrivée.";
                    macroName = "pnc_calm";
                }
                else if (phase == FlightPhase.TaxiIn)
                {
                    reportEn = "Everyone is seated and waiting to arrive at the gate, Captain.";
                    reportFr = "Tout le monde est assis et attend l'arrivée au point de stationnement.";
                    macroName = "pnc_calm";
                }
            }

            // Operational State Mapping
            if (State == CabinState.Deboarding)
            {
                reportEn = "We're currently deboarding the passengers. Almost done back here.";
                reportFr = "Le débarquement est en cours. Nous avons presque terminé.";
                macroName = "pnc_calm";
            }
            else if (State == CabinState.Boarding)
            {
                reportEn = "We are waiting for the boarding to complete, Captain.";
                reportFr = "Nous attendons la fin de l'embarquement, Commandant.";
                macroName = "pnc_boarding_in_progress";
            }
            else if (State == CabinState.SecuringForTakeoff || (_isSecuring && phase == FlightPhase.TaxiOut))
            {
                reportEn = "Captain, we are currently securing the cabin...";
                reportFr = "Commandant, nous sécurisons actuellement la cabine...";
                macroName = "pnc_securing_in_progress";
            }
            else if (State == CabinState.TakeoffSecured)
            {
                reportEn = "Cabin is fully secured and ready for takeoff. Everyone is seated.";
                reportFr = "La cabine est entièrement préparée et prête pour le décollage.";
                macroName = "pnc_cabin_secure_takeoff";
            }
            else if (State == CabinState.ServingMeals)
            {
                if (InFlightServiceProgress < 20) {
                    reportEn = "We've just started preparing the service carts.";
                    reportFr = "Nous venons de commencer la préparation des chariots de service.";
                    macroName = "pnc_calm";
                } else if (InFlightServiceProgress < 80) {
                    reportEn = "The meal service is in full swing. Everyone seems satisfied.";
                    reportFr = "Le service des repas bat son plein. Tout le monde semble satisfait.";
                    macroName = "pnc_calm";
                } else {
                    reportEn = "We are currently picking up the trays and clearing the aisles.";
                    reportFr = "Nous ramassons actuellement les plateaux et débarrassons les allées.";
                    macroName = "pnc_calm";
                }
            }
            else if (State == CabinState.SecuringForLanding || (_isSecuring && (phase == FlightPhase.Descent || phase == FlightPhase.Approach)))
            {
                reportEn = "Captain, we are currently securing the cabin...";
                reportFr = "Commandant, nous sécurisons actuellement la cabine...";
                macroName = "pnc_securing_in_progress";
            }
            else if (State == CabinState.LandingSecured)
            {
                reportEn = "Cabin is secure for landing. The crew is seated.";
                reportFr = "La cabine est sécurisée pour l'atterrissage. L'équipage est assis.";
                macroName = "pnc_cabin_secure_landing";
            }

            // Situational & Crisis Overrides (Highest Priority)
            if (isCrisisActive)
            {
                reportEn = "Captain, the passengers are panicking! What's going on?!";
                reportFr = "Commandant, les passagers paniquent ! Que se passe-t-il ?!";
                macroName = "pnc_uncomfortable_pax";
            }
            else if (PassengerManifest.Exists(p => p != null && p.IsInjured))
            {
                var injured = PassengerManifest.FindAll(p => p != null && p.IsInjured);
                reportEn = $"We are still tending to {injured.Count} injured passenger(s). The mood is very somber.";
                reportFr = $"Nous nous occupons toujours de {injured.Count} passager(s) blessé(s). L'ambiance est très lourde.";
                macroName = "pnc_uncomfortable_pax";
            }
            else if (WasteLevel > 80.0)
            {
                reportEn = "Captain, the waste tanks are full...";
                reportFr = "Commandant, les cuves à déchets sont pleines...";
                macroName = "pnc_uncomfortable_pax";
            }
            else if (CabinCleanliness < 40.0)
            {
                reportEn = "Captain, the passengers are complaining about the absolutely disgusting state...";
                reportFr = "Commandant, les passagers se plaignent de l'état absolument dégoûtant...";
                macroName = "pnc_cabin_dirty";
            }
            else if (macroName == "pnc_calm") 
            {
                if (_currentDelayMinutes > 15 && (phase == FlightPhase.AtGate || phase == FlightPhase.Turnaround))
                {
                    if (PassengerAnxiety > 50.0)
                    {
                        reportEn = "The passengers are getting very frustrated and restless due to this long delay, Captain.";
                        reportFr = "Les passagers s'impatientent sérieusement et s'énervent à cause de cette longue attente, Commandant.";
                    }
                    else
                    {
                        reportEn = "Captain, passengers are starting to ask questions about the delay. It would be good to update them.";
                        reportFr = "Commandant, les passagers commencent à poser des questions sur le retard. Il serait bon de les informer.";
                    }
                    macroName = "pnc_uncomfortable_pax";
                }
                else if (PassengerAnxiety > 50.0)
                {
                    if ((DateTime.Now - _lastTurbulenceNotice).TotalMinutes < 15)
                    {
                        reportEn = "It's been quite bumpy, and the cabin is feeling very tense and anxious right now.";
                        reportFr = "Ça a secoué pas mal, l'ambiance est très tendue et anxieuse en cabine.";
                        macroName = "pnc_uncomfortable_pax";
                    }
                    else if (_hasTriggeredCateringComplaint)
                    {
                        reportEn = "People are very unhappy about not getting their meals yet. It's tough back here.";
                        reportFr = "Les gens sont très mécontents de ne pas avoir eu de repas. C'est difficile à l'arrière.";
                        macroName = "pnc_uncomfortable_pax";
                    }
                    else
                    {
                        reportEn = "Note that some passengers are quite anxious about the flight.";
                        reportFr = "À noter que certains passagers sont assez anxieux par rapport au vol.";
                        macroName = "pnc_uncomfortable_pax";
                    }
                }
                else if (_thermalDissatisfactionGauge > 30.0)
                {
                    if (LastKnownCabinTemp > 26)
                    {
                        reportEn = "It's getting a bit warm in the back. Passengers are complaining about the general comfort level.";
                        reportFr = "Ça commence à chauffer à l'arrière. Les passagers se plaignent du confort.";
                    }
                    else if (LastKnownCabinTemp < 19)
                    {
                        reportEn = "A few complaints about the cold. Passengers are complaining about the general comfort level.";
                        reportFr = "Quelques plaintes concernant le froid. Les passagers se plaignent du confort.";
                    }
                    macroName = "pnc_uncomfortable_pax";
                }
                else if (ComfortLevel < 40.0)
                {
                    reportEn = "Passengers are complaining about the general comfort level.";
                    reportFr = "Les passagers se plaignent du niveau de confort général.";
                    macroName = "pnc_uncomfortable_pax";
                }
                else
                {
                    reportEn = "Captain, everything is fine in the cabin.";
                    reportFr = "Commandant, tout se passe bien en cabine.";
                    macroName = "pnc_calm";
                }
            }

            PlayDynamicAsPurser(macroName, reportEn, false, "to_fd", false);
            OnCrewMessage?.Invoke("info", LocalizationService.Translate(reportEn, reportFr), null);
            return true;
        }

        public void EvaluateWeatherAnxiety(FlightSupervisor.UI.Models.BriefingData weatherData, FlightPhase phase)
        {
            if (weatherData == null || weatherData.Stations == null) return;

            // Departure
            if (!_hasAppliedDepartureWeatherAnxiety && (phase == FlightPhase.AtGate || phase == FlightPhase.Pushback || phase == FlightPhase.TaxiOut))
            {
                var dep = weatherData.Stations.FirstOrDefault(s => s.Id.Equals("origin", StringComparison.OrdinalIgnoreCase));
                if (dep != null)
                {
                    double addAnx = 0;
                    if (dep.WindSeverity == FlightSupervisor.UI.Models.WeatherSeverity.Danger) addAnx += 10.0;
                    else if (dep.WindSeverity == FlightSupervisor.UI.Models.WeatherSeverity.Warning) addAnx += 5.0;

                    if (dep.VisibilitySeverity == FlightSupervisor.UI.Models.WeatherSeverity.Danger) addAnx += 8.0;
                    else if (dep.VisibilitySeverity == FlightSupervisor.UI.Models.WeatherSeverity.Warning) addAnx += 4.0;

                    if (addAnx > 0)
                    {
                        IncreaseAnxiety(addAnx, phase, false, GetDynamicFeedback("WeatherDep", phase), 30.0);
                        _hasAppliedDepartureWeatherAnxiety = true;
                    }
                }
            }

            // Arrival anxiety is now exclusively handled during the PA announcements (AnnounceWelcome / AnnounceApproach)
            // to ensure passengers do not magically react to destination weather before the captain informs them.
        }

        public void BoardPassenger(int count)
        {
            var unboarded = PassengerManifest.Where(p => p != null && !p.IsBoarded).Take(count).ToList();
            foreach (var p in unboarded)
            {
                p.IsBoarded = true;
                if (IsSeatbeltsOn) 
                {
                    // 90% chance to put seatbelt on immediately during boarding, leaving 10% for the crew to hound later
                    p.IsSeatbeltFastened = _rnd.NextDouble() < 0.90;
                }
            }
        }

        public void DeboardPassenger(int count)
        {
            var targetManifest = PreviousLegManifest.Any(p => p != null && p.IsBoarded) ? PreviousLegManifest : PassengerManifest;
            var boarded = targetManifest.Where(p => p != null && p.IsBoarded).TakeLast(count).ToList();
            foreach (var p in boarded)
            {
                p.IsBoarded = false;
                p.IsSeatbeltFastened = false; // They definitely unfasten to leave
            }
        }

        public void TriggerTurnaroundUnloadingCompleteAudio()
        {
            PlayDynamicAsPurser("pnc_turnaround_unloading_complete", 
                LocalizationService.Translate("Captain, the cabin and cargo are now completely empty. We are ready for the next flight.", "Commandant, la cabine et les soutes sont vides. Nous sommes prêts pour le prochain vol."), 
                false, "to_fd", false);
        }

        private void PlayDynamicAsCaptain(string filename, string fallbackText, bool playChime = true)
        {
            string subFolder = GetVoiceStudioSubfolder(filename);
            
            string routing = "to_pa";
            if (filename == "pa_nearing_top_descent" || filename == "pa_arm_doors" || filename == "pa_disarm_doors" || filename == "pa_prepare_landing" || filename == "pa_prepare_takeoff" || filename == "pa_seats_takeoff" || filename == "pa_seats_landing")
                routing = "to_pnc";
                
            string exactPath = $"airlines/captain/{ActiveCaptainLanguage}/{ActiveCaptainVoiceId}/{routing}/{subFolder}/{filename}.wav";
            string basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "assets", "sounds", "airlines", "captain", ActiveCaptainLanguage, ActiveCaptainVoiceId, routing, subFolder);
            string folderRel = $"airlines/captain/{ActiveCaptainLanguage}/{ActiveCaptainVoiceId}/{routing}/{subFolder}";
            
            try
            {
                string fullPathWav = System.IO.Path.Combine(basePath, filename + ".wav");
                string fullPathMp3 = System.IO.Path.Combine(basePath, filename + ".mp3");

                if (!System.IO.File.Exists(fullPathWav) && !System.IO.File.Exists(fullPathMp3))
                {
                    string normAirline = ActiveAirlineId?.ToLower() ?? "generic";
                    if (normAirline.Contains("eju") || normAirline.Contains("ezs") || normAirline.Contains("ezy") || normAirline.Contains("easyjet")) normAirline = "easyjet";
                    else if (normAirline.Contains("afr") || normAirline.Contains("air_france") || normAirline.Contains("airfrance")) normAirline = "airfrance";

                    string destIcao = CurrentFlight?.Destination?.IcaoCode?.ToLower() ?? "generic";

                    string genericDest = filename.Replace($"_{destIcao}", "_generic");
                    string genericAir = filename.Replace($"_{normAirline}", "_generic");
                    string genericBoth = genericAir.Replace($"_{destIcao}", "_generic");

                    if (System.IO.File.Exists(System.IO.Path.Combine(basePath, genericAir + ".wav"))) { exactPath = exactPath.Replace(filename, genericAir); filename = genericAir; }
                    else if (System.IO.File.Exists(System.IO.Path.Combine(basePath, genericDest + ".wav"))) { exactPath = exactPath.Replace(filename, genericDest); filename = genericDest; }
                    else if (System.IO.File.Exists(System.IO.Path.Combine(basePath, genericBoth + ".wav"))) { exactPath = exactPath.Replace(filename, genericBoth); filename = genericBoth; }
                }
                
                // If it STILL doesn't exist, fallback to Rowan as a safety net
                if (!System.IO.File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "assets", "sounds", exactPath.Replace('/', '\\'))) && !System.IO.File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "assets", "sounds", exactPath.Replace(".wav", ".mp3").Replace('/', '\\'))))
                {
                    exactPath = exactPath.Replace($"{ActiveCaptainLanguage}/{ActiveCaptainVoiceId}", "gb/rowan_(gb)");
                }
            }
            catch { }

            _audio?.PlayVariantWithPrefixAsCaptain(folderRel, filename, fallbackText, playChime);
        }

        private void PlayDynamicAsPurser(string filename, string fallbackText, bool playChime = true, string routing = "to_pa", bool isVariant = false)
        {
            string subFolder = GetVoiceStudioSubfolder(filename);
            
            if (isVariant)
            {
                string folderRel = $"airlines/pnc/{ActivePncLanguage}/{ActivePncVoiceId}/{routing}/{subFolder}";
                _audio?.PlayVariantAsPurser(folderRel, fallbackText, filename, playChime);
            }
            else
            {
                string folderRel = $"airlines/pnc/{ActivePncLanguage}/{ActivePncVoiceId}/{routing}/{subFolder}";
                _audio?.PlayVariantAsPurser(folderRel, fallbackText, filename, playChime);
            }
        }

        private string GetVoiceStudioSubfolder(string fileName)
        {
            fileName = fileName.ToLower();
            if (fileName.StartsWith("alt_") || fileName.StartsWith("pa_cruise_alt")) return "altitudes";
            if (fileName.StartsWith("temp_") || fileName.StartsWith("pa_descent_temp") || fileName.StartsWith("pa_block_temp")) return "temperatures";
            if (fileName.StartsWith("time_") || fileName.StartsWith("pa_welcome_flight_time") || fileName.StartsWith("pa_arrival_time") || fileName.StartsWith("pnc_time_") || fileName.StartsWith("hour_") || fileName.StartsWith("num_") || fileName.StartsWith("pnc_block_")) return "times";
            if (fileName.StartsWith("dest_") || fileName.StartsWith("pa_bound_for") || fileName.StartsWith("pnc_arr_dest_")) return "destinations";
            if (fileName.StartsWith("dep_")) return "departures";
            if (fileName.StartsWith("airline_")) return "airlines";
            if (fileName.Contains("delay"))
            {
                if (fileName.Contains("atc")) return "delay/ATC";
                if (fileName.Contains("boarding")) return "delay/Boarding";
                if (fileName.Contains("ground") || fileName.Contains("groundops")) return "delay/GroundOps";
                if (fileName.Contains("tech")) return "delay/Technical";
                if (fileName.Contains("weather")) return "delay/Weather";
                return "delay/General";
            }
            if (fileName.Contains("weather") || fileName.Contains("turbulence")) return "weather";
            if (fileName.Contains("welcome") || fileName.Contains("boarding")) return "boarding";
            if (fileName.StartsWith("pnc_arrival_outro")) return "general";
            if (fileName.StartsWith("pa_prepare_takeoff") || fileName.StartsWith("pa_prepare_landing") || fileName.StartsWith("pa_seats_takeoff") || fileName.StartsWith("pa_seats_landing")) return "general";
            if (fileName.Contains("descent") || fileName.Contains("landing") || fileName.Contains("arrival")) return "approach";
            return "general";
        }
    }
}



