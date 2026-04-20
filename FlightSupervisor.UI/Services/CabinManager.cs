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
        public double IndividualComfort { get; set; } = 100.0;
        public double IndividualSatisfaction { get; set; } = 100.0;
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

        public double PassengerAnxiety 
        {
            get {
                if (_lastBoardingTick != DateTime.MaxValue || !PassengerManifest.Where(p => p != null && p.IsBoarded).Any()) return 0.0;
                double avg = PassengerManifest.Where(p => p != null && p.IsBoarded).Average(p => p.IndividualAnxiety);
                return Math.Max(0.0, Math.Round(avg + (_renderRnd.NextDouble() * 1.2 - 0.6), 1));
            }
        }
        
        public double ComfortLevel 
        {
            get {
                double maxCap = IsLowCost ? 65.0 : 100.0;
                if (_lastBoardingTick != DateTime.MaxValue || !PassengerManifest.Where(p => p != null && p.IsBoarded).Any()) return maxCap;
                double avg = PassengerManifest.Where(p => p != null && p.IsBoarded).Average(p => p.IndividualComfort);
                double renderCap = IsLowCost ? 65.0 : 98.0;
                return Math.Min(Math.Round(renderCap - 3.0 + (_renderRnd.NextDouble() * 3.0), 1), Math.Round(avg - (_renderRnd.NextDouble() * 1.5), 1));
            }
        }
        
        public double Satisfaction 
        {
            get {
                if (_lastBoardingTick != DateTime.MaxValue || !PassengerManifest.Where(p => p != null && p.IsBoarded).Any()) return 100.0;
                double avg = PassengerManifest.Where(p => p != null && p.IsBoarded).Average(p => p.IndividualSatisfaction);
                return Math.Min(Math.Round(95.0 + (_renderRnd.NextDouble() * 3.0), 1), Math.Round(avg - (_renderRnd.NextDouble() * 1.8), 1));
            }
        }

        private void ModifySatisfaction(double amount)
        {
            foreach (var p in PassengerManifest.Where(x => x != null && x.IsBoarded))
            {
                double multiplier = p.Demographic == PassengerDemographic.Grumpy ? 1.5 : (p.Demographic == PassengerDemographic.Relaxed ? 0.8 : 1.0);
                p.IndividualSatisfaction += amount * (amount < 0 ? multiplier : (1 / multiplier));
                if (p.IndividualSatisfaction < 0.0) p.IndividualSatisfaction = 0.0;
                if (p.IndividualSatisfaction > 100.0) p.IndividualSatisfaction = 100.0;
            }
        }
        
        private void SetSatisfaction(double target)
        {
            foreach (var p in PassengerManifest) p.IndividualSatisfaction = target;
        }

        private void ModifyAnxiety(double amount)
        {
            foreach (var p in PassengerManifest.Where(x => x != null && x.IsBoarded))
            {
                double multiplier = p.Demographic == PassengerDemographic.Anxious ? 1.5 : (p.Demographic == PassengerDemographic.Relaxed ? 0.5 : 1.0);
                p.IndividualAnxiety += amount * (amount > 0 ? multiplier : (1 / multiplier));
                if (p.IndividualAnxiety < 0.0) p.IndividualAnxiety = 0.0;
                if (p.IndividualAnxiety > 100.0) p.IndividualAnxiety = 100.0;
            }
        }

        private void SetAnxiety(double minRatio)
        {
            foreach (var p in PassengerManifest.Where(x => x != null && x.IsBoarded))
            {
                p.IndividualAnxiety = Math.Max(minRatio, p.IndividualAnxiety);
            }
        }

        private void ClearAnxiety() { foreach (var p in PassengerManifest) p.IndividualAnxiety = 0.0; }

        private void ModifyComfort(double amount)
        {
            double maxCap = IsLowCost ? 65.0 : 100.0;
            foreach (var p in PassengerManifest.Where(x => x != null && x.IsBoarded))
            {
                double multiplier = p.Demographic == PassengerDemographic.Grumpy ? 1.5 : 1.0;
                p.IndividualComfort += amount * (amount < 0 ? multiplier : (1 / multiplier));
                if (p.IndividualComfort < 0.0) p.IndividualComfort = 0.0;
                if (p.IndividualComfort > maxCap) p.IndividualComfort = maxCap;
            }
        }

        private void ClearComfort() 
        { 
            double maxCap = IsLowCost ? 65.0 : 100.0;
            foreach (var p in PassengerManifest) p.IndividualComfort = maxCap; 
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

        public void TriggerIncomingCabinCall(string callType, string audioFolder, string fallbackMessage)
        {
            _pendingCabinCalls.Enqueue((callType, audioFolder, fallbackMessage, DateTime.Now));
            if (!_isCabinCallBlinking)
            {
                _isCabinCallBlinking = true;
                OnCabinCallIncoming?.Invoke("start");
            }
        }

        public void AnswerCabinCall()
        {
            if (_pendingCabinCalls.TryDequeue(out var call))
            {
                // Unanswered calls might pile up, we process the oldest
                _audio?.PlayVariantAsPurser(call.AudioFolder, call.FallbackMessage);
                OnCrewMessage?.Invoke("orange", LocalizationService.Translate(call.FallbackMessage, call.FallbackMessage), null);

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

                if (_pendingCabinCalls.IsEmpty)
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
            _audio?.PlayVariantAsPurser("TO_FD/Status_Reports/Calm", msg);
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
        private DateTime? _safetyDemoTimerStart = null;
        private const double SafetyDemoDuration = 45.0;

        private double _currentDelayMinutes = 0;
        private double _currentSecuringRate = 0;
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

        // Audio Properties
        public string ActivePncVoiceId { get; set; } = "female_1";
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

        public void InitializeFlightDemographics(FlightSupervisor.UI.Services.AirlineProfile profile, FlightSupervisor.UI.Services.ManifestData manifestData = null)
        {
            _hasAnnouncedBoardingComplete = false;
            _hasAnnouncedGalleySecured = false;
            _hasWarnedPushbackNoSeatbelts = false;
            HasPenalizedRefuelingSeatbelts = false;
            HasBoardingStarted = false;
            _lastBoardingTick = DateTime.MaxValue;
            State = CabinState.Idle;
            
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

        public void HandleCommand(string command)
        {
            _issuedCommands.Add(command);

            switch (command)
            {
                case "TOP_DESCENT":
                    _audio?.PlayExactAsCaptain("TO_PNC/EN_Rowan_Nearing_TOP_Descent.mp3", "Cabin Crew, we are nearing top of descent.");
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("PA: Cabin crew, nearing top of descent.", "PA: PNC, début de descente imminent."), null);
                    if (State == CabinState.ServingMeals)
                    {
                        IsServiceHurried = true;
                        OnPncStatusChanged?.Invoke("Service securing (HURRIED)...", State);
                    }
                    break;
                case "ARM_DOORS":
                    _audio?.PlayExactAsCaptain("TO_PNC/EN_Rowan_Arm_Doors.mp3", "Cabin Crew, arm doors and cross check.");
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
                    _audio?.PlayExactAsCaptain("TO_PNC/EN_Rowan_Prepare_TakeOff.mp3", "Cabin Crew, prepare for takeoff.");
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("PA: Cabin Crew, prepare for takeoff.", "PA: PNC, préparez la cabine pour le décollage."), null);
                    OnPncStatusChanged?.Invoke("Securing Cabin...", State);
                    break;
                case "SEATS_TAKEOFF":
                    if (_isSecuring && SecuringProgress < 100.0) {
                        OnPenaltyTriggered?.Invoke(-30, LocalizationService.Translate("Force Seats: Crew forced to sit before cabin was secure", "Force Seats: PNC forcés de s'asseoir avant sécurisation"));
                        IncreaseAnxiety(15.0, FlightPhase.AtGate, false);
                        CrewEsteem = Math.Max(0.0, CrewEsteem - 5.0);
                        _isSecuring = false; // Interrupted
                    }
                    _audio?.PlayExactAsCaptain("TO_PNC/EN_Rowan_Seats_TakeOff.mp3", "Cabin Crew, please be seated for takeoff.");
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("PA: Cabin Crew, please be seated for takeoff.", "PA: PNC, aux postes pour le décollage."), null);
                    
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
                    _audio?.PlayExactAsCaptain("TO_PNC/EN_Rowan_Prepare_Landing.mp3", "Cabin Crew, prepare for landing.");
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("PA: Cabin Crew, prepare for landing.", "PA: PNC, préparez la cabine pour l'atterrissage."), null);
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
                        IncreaseAnxiety(15.0, FlightPhase.Cruise, false);
                        CrewEsteem = Math.Max(0.0, CrewEsteem - 5.0);
                        _isSecuring = false; // Interrupted
                    }
                    _audio?.PlayExactAsCaptain("TO_PNC/EN_Rowan_Seats_Landing.mp3", "Cabin Crew, please be seated for landing.");
                    OnCrewMessage?.Invoke("info", LocalizationService.Translate("PA: Cabin Crew, please be seated for landing.", "PA: PNC, aux postes pour l'atterrissage."), null);
                    
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
                        
                        _audio?.PlayExactAsCaptain(_targetState == CabinState.TakeoffSecured ? "TO_PNC/EN_Rowan_Prepare_TakeOff.mp3" : "TO_PNC/EN_Rowan_Prepare_Landing.mp3", _targetState == CabinState.TakeoffSecured ? "Cabin crew, expedite your duties, takeoff is imminent." : "Cabin crew, please expedite your duties and prepare for landing.");
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
            
            // French: PNC aux portes, désarmement des toboggans et vérification de la porte opposée
            // English: Cabin Crew, disarm doors and cross check
            _audio?.PlayExactAsCaptain("TO_PNC/EN_Rowan_Disarm_Doors.mp3", "Cabin crew, disarm doors and cross check.");
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

        public void ProcessLandingImpact(double fpm)
        {
            if (fpm > -150)
            {
                // Kiss Landing / Butter
                IncreaseComfort(15.0);
                ModifyAnxiety(-30.0);
            }
            else if (fpm < -600)
            {
                // Severe Hard Landing
                DecreaseComfort(40.0);
                IncreaseAnxiety(60.0, FlightPhase.Landing, true);
                OnCrewMessage?.Invoke("red", UI.Services.LocalizationService.Translate("That was a terrifying impact! Many passengers are screaming in the back!", "C'était un atterrissage extrêmement violent ! Beaucoup de passagers crient à l'arrière !"), null);
            }
            else if (fpm < -450)
            {
                // Hard Landing
                DecreaseComfort(20.0);
                IncreaseAnxiety(30.0, FlightPhase.Landing, false);
                OnCrewMessage?.Invoke("orange", UI.Services.LocalizationService.Translate("That was a very hard landing! The cabin shook violently.", "C'était un atterrissage très dur ! La cabine a secoué violemment."), null);
            }
            else 
            {
                // Normal
                ModifyAnxiety(-10.0);
            }
        }

        public void Tick(double gForce, double bankAngle, bool isBoarded, DateTime currentZulu, DateTime? sobt, FlightPhase phase, double groundSpeed, double altitude, double verticalSpeed, bool isCrisisActive, double cabinTemperature = 22.0, double boardingProgress = -1.0)
        {
            double deltaTimeSeconds = _lastTickTime == DateTime.MinValue ? 1.0 : (DateTime.Now - _lastTickTime).TotalSeconds;
            _lastTickTime = DateTime.Now;
            if (deltaTimeSeconds <= 0 || deltaTimeSeconds > 10) deltaTimeSeconds = 1.0;

            if (State == CabinState.Deboarding)
            {
                var activeManifest = PreviousLegManifest.Any(p => p != null && p.IsBoarded) ? PreviousLegManifest : PassengerManifest;
                var boardedCount = activeManifest.Count(p => p != null && p.IsBoarded);
                
                if (boardedCount == 0)
                {
                    State = CabinState.Idle;
                    HasBoardingStarted = false;
                    OnCrewMessage?.Invoke("cyan", LocalizationService.Translate("Cabin makes are complete. All passengers have disembarked.", "La cabine est débarrassée. Tous les passagers ont débarqué."), null);
                    OnDeboardingComplete?.Invoke();
                    OnPncStatusChanged?.Invoke("Standing By", State);
                }
                return;
            }

            // Thermal monitoring still runs before boarding, so we don't completely return.
            // Progressive Boarding Logic (Phase 3)
            if ((phase == FlightPhase.AtGate || phase == FlightPhase.Turnaround) && HasBoardingStarted && !isBoarded)
            {
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
                _audio?.PlayVariantAsPurser("TO_FD/Checklist_Reports/BoardingComplete", "Boarding is complete Captain.");
                OnCrewMessage?.Invoke("cyan", LocalizationService.Translate("PA: Boarding is complete.", "PA: Embarquement terminé."), null);
                OnPncStatusChanged?.Invoke("Standing By", State);
            }

            // Automatic Galley Secured Event on Pushback initiation
            if (!_hasAnnouncedGalleySecured && phase == FlightPhase.Pushback)
            {
                _hasAnnouncedGalleySecured = true;
                TriggerIncomingCabinCall("GalleySecured", "TO_FD/Incoming_Calls/GalleySecured", "Galleys secured and we're starting the final cabin checks.");
            }

            // --- THERMAL COMFORT (Physical Simulation) ---
            if (cabinTemperature > 0.0) 
            {
                double inertiaRate = 0.2 * deltaTimeSeconds; // ~12 degrees per minute (fast response to simulator)
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
                    IncreaseAnxiety(30.0, phase, isCrisisActive);
                    
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
                            OnPncStatusChanged?.Invoke("Captain, passengers are asking to use the restrooms. It's been a long time with seatbelts on.", State);
                            OnPenaltyTriggered?.Invoke(-30, LocalizationService.Translate("Passenger Impatience: Seatbelts left on excessively during cruise.", "Impatience des passagers : Ceintures laissées allumées trop longtemps."));
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
                        _audio?.PlayVariantAsPurser("TO_FD/Status_Reports/UncomfortablePax", msg);
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
                if (_rnd.NextDouble() < 0.05) ModifyAnxiety(_rnd.NextDouble() * 0.2);
            }
            else if (phase != FlightPhase.AtGate && phase != FlightPhase.Turnaround && phase != FlightPhase.TaxiOut && phase != FlightPhase.TaxiIn)
            {
                if ((DateTime.Now - _lastTurbulenceNotice).TotalSeconds > 120 && !isCrisisActive) 
                {
                    DecreaseAnxiety(0.02); // Gradual peace recovery
                    double maxRecov = IsLowCost ? 65.0 : 95.0;
                    if (ComfortLevel < maxRecov) IncreaseComfort(0.03); // Gradual comfort recovery
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

                    if (currentTemp > 30.0)
                    {
                        // Extrême chaud (exponentiel lissé)
                        double delta = currentTemp - 30.0;
                        agitationIncrement = (1.0 + (delta * 0.5)) * deltaTimeSeconds * 0.1;
                    }
                    else if (currentTemp > 26.0)
                    {
                        // Chaud graduel
                        double delta = currentTemp - 26.0;
                        agitationIncrement = delta * deltaTimeSeconds * 0.08;
                    }
                    else if (currentTemp < 18.0)
                    {
                        // Extrême froid (exponentiel lissé)
                        double delta = 18.0 - currentTemp;
                        agitationIncrement = (1.0 + (delta * 0.5)) * deltaTimeSeconds * 0.1;
                    }
                    else if (currentTemp < 19.5)
                    {
                        // Froid graduel
                        double delta = 19.5 - currentTemp;
                        agitationIncrement = delta * deltaTimeSeconds * 0.08;
                    }
                    else
                    {
                        // Zone de confort Idéale (19.5 - 26.0)
                        agitationIncrement = -deltaTimeSeconds * 2.0; // Drainage plus rapide quand corrigé
                    }

                    _thermalDissatisfactionGauge += agitationIncrement;

                    if (_thermalDissatisfactionGauge < 0.0) _thermalDissatisfactionGauge = 0.0;
                    if (_thermalDissatisfactionGauge > 100.0) _thermalDissatisfactionGauge = 100.0;

                    // Automation PNC push alert si l'agitation dépasse un seuil
                    if (_thermalDissatisfactionGauge >= 80.0 && !_hasWarnedThermal)
                    {
                        _hasWarnedThermal = true;
                        
                        if (LastKnownCabinTemp > 26.0) 
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
                        string msg = "Captain, the temperature is much better now, thank you.";
                        _audio?.PlayVariantAsPurser("TO_FD/Incoming_Calls/TempFixed", msg);
                        OnCrewMessage?.Invoke("green", LocalizationService.Translate(
                            msg, 
                            "Commandant, la température est bien meilleure maintenant, merci."), null);
                        
                        if (ComfortLevel < 90) ModifyComfort(5.0); // Léger gain de satisfaction pour la résolution
                    }

                    // Constante dégradation si la situation inconfortable perdure (Fortement adoucie)
                    if (_thermalDissatisfactionGauge > 50.0)
                    {
                        double penaltyFactor = (_thermalDissatisfactionGauge / 100.0);
                        DecreaseComfort(penaltyFactor * 0.05 * deltaTimeSeconds);
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
                        _audio?.PlayVariantAsPurser("TO_FD/Incoming_Calls/CabinDirty", msg);
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
                    _audio?.PlayVariantAsPurser("TO_PA/SafetyDemo", safetyDemo);
                    OnCrewMessage?.Invoke("sky", LocalizationService.Translate("PA: Safety Demonstration in progress.", "PA: Démonstration de sécurité en cours."), null);
                    
                    _isPlayingSafetyDemo = true;
                    _safetyDemoTimerStart = DateTime.Now;
                    OnPncStatusChanged?.Invoke("Safety Briefing in progress...", State);
                }
                else if (phase == FlightPhase.Takeoff)
                {
                    if (_actualTakeoffTime == null) _actualTakeoffTime = DateTime.Now;
                    
                    IncreaseAnxiety(10.0, phase, isCrisisActive);
                    OnCrewMessage?.Invoke("orange", LocalizationService.Translate("Passengers feel the pressure of takeoff acceleration.", "Les passagers ressentent la pression et le bruit de l'accélération."), null);
                }
                else if (phase == FlightPhase.Climb && _lastPhase == FlightPhase.InitialClimb && !_hasTriggeredThrustReductionAnxiety)
                {
                    _hasTriggeredThrustReductionAnxiety = true;
                    IncreaseAnxiety(20.0, phase, isCrisisActive);
                    OnCrewMessage?.Invoke("orange", LocalizationService.Translate("Thrust reduction felt in cabin. Passengers experienced a brief moment of anxiety.", "Réduction de poussée ressentie. Les passagers ont eu un bref moment d'anxiété (sensation de chute)."), null);
                }
                else if (phase == FlightPhase.Cruise)
                {
                    DecreaseAnxiety(20.0);
                    OnCrewMessage?.Invoke("green", LocalizationService.Translate("Passengers are relieved to reach cruise altitude.", "Les passagers sont soulagés d'avoir atteint l'altitude de croisière."), null);
                }
                else if (phase == FlightPhase.Approach && (_lastPhase == FlightPhase.Descent || _lastPhase == FlightPhase.Cruise))
                {
                    if (!_hasPlayedApproachPncPA)
                    {
                        _hasPlayedApproachPncPA = true;
                        DecreaseAnxiety(15.0);
                        string destName = CurrentFlight?.Destination?.Name ?? CurrentFlight?.Destination?.IcaoCode ?? "our destination";
                        string spokenText = $"Ladies and gentlemen, we are now approaching {destName}. Please return to your seats, fasten your seatbelts, and make sure your large electronic devices are stowed away.";
                        _audio?.PlayVariantAsPurser("TO_PA/Approach", spokenText);

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
                    
                    // 1. Start PA
                    string introPA = $"Ladies and gentlemen, welcome to {destName}.";
                    _audio?.PlayExactAsPurser("TO_PA/ArrivalGate/EN_PNC_Beth_ArrivalGate_Start.mp3", introPA);

                    // 2. City (ICAO)
                    string destIcao = CurrentFlight?.Destination?.IcaoCode ?? "";
                    if (!string.IsNullOrEmpty(destIcao))
                        _audio?.PlayExactAsPurser($"TO_PA/ICAO/EN_PNC_Beth_{destIcao}.mp3", null, playChime: false);

                    // 3. Middle PA
                    string middlePA = "For your safety and the safety of those around you, please remain seated with your seatbelt fastened until the captain has turned off the seatbelt sign at the gate. As you leave the aircraft, please check around your seat for any personal items. On behalf of the entire crew,";
                    _audio?.PlayExactAsPurser("TO_PA/ArrivalGate/EN_PNC_Beth_ArrivalGate_End.mp3", middlePA, playChime: false);

                    // 4. Airline 
                    string normAirline = ActiveAirlineId.ToUpper();
                    if (normAirline.StartsWith("EJU") || normAirline.StartsWith("EZS") || normAirline.StartsWith("EZY") || normAirline.Contains("EASYJET")) 
                    {
                        normAirline = "EZY";
                    }
                    else if (normAirline.StartsWith("AFR") || normAirline.Contains("AIR_FRANCE") || normAirline.Contains("AIRFRANCE"))
                    {
                        normAirline = "AFR";
                    }
                    if (!string.IsNullOrEmpty(normAirline))
                        _audio?.PlayExactAsPurser($"TO_PA/Airlines/EN_PNC_Beth_{normAirline}.mp3", null, playChime: false);

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
                            IncreaseAnxiety(0.001 * deltaTimeSeconds, phase, isCrisisActive);
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

                if (_holdTurnAccumulator > 120) // Exactly 2 minutes (360 degrees continuous at standard rate)
                {
                    if ((DateTime.Now - _timeOfLastDelayPA).TotalMinutes > 15 && (DateTime.Now - _lastHoldPenaltyTime).TotalMinutes > 5)
                    {
                        _lastHoldPenaltyTime = DateTime.Now;
                        _holdTurnAccumulator = 0; // Clear it out to require another full 360
                        
                        ModifySatisfaction(-15.0);
                        IncreaseAnxiety(15.0, phase, false);

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
                _audio?.PlayVariantAsPurser("TO_PA/SeatbeltOff", "Ladies and gentlemen, the captain has turned off the fasten seatbelt sign. You are now free to move about the cabin. However, we do recommend keeping your seatbelt fastened while seated, in case we experience any unexpected turbulence.");
                OnCrewMessage?.Invoke("sky", LocalizationService.Translate("PA: Seatbelts off announcement.", "PA: Annonce de libération des ceintures."), null);
                
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
                        _audio?.PlayVariantAsPurser("TO_PA/ServiceStart", servicePA);
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
                    double effectiveRate = _currentSecuringRate * (Math.Max(5.0, CrewEsteem) / 100.0) * deltaTimeSeconds;
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
            
            if (phase != FlightPhase.AtGate && phase != FlightPhase.Turnaround && (gMax - gMin > 0.6))
            {
                IncreaseAnxiety(0.5, phase, isCrisisActive); 
                DecreaseComfort(0.5); // B.1 task: Turbulence vibrates cabin, dropping comfort
                
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
                        
                        _audio?.PlayVariantAsPurser("TO_PA/SeatbeltOn", "Ladies and gentlemen, the fasten seatbelt sign has been illuminated due to turbulence. Please return to your seats immediately and ensure your seatbelts are securely fastened.");
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
                    IncreaseAnxiety(0.5, phase, isCrisisActive);
                    if (ComfortLevel > 40.0) DecreaseComfort(0.2);
                }
            }
            
            if (sobt.HasValue && currentZulu > sobt.Value && phase == FlightPhase.AtGate)
            {
                var delaySpan = currentZulu - sobt.Value;
                _currentDelayMinutes = delaySpan.TotalMinutes;
                if (_currentDelayMinutes > 5)
                {
                    double anxInc = 0.005 * deltaTimeSeconds; 
                    double comfDec = 0.005 * deltaTimeSeconds;
                    double satDec = 0.015 * deltaTimeSeconds; // Satisfaction drops faster on delay
                    
                    if (delaySpan.TotalMinutes > 15) { anxInc = 0.01 * deltaTimeSeconds; comfDec = 0.01 * deltaTimeSeconds; satDec = 0.03 * deltaTimeSeconds; }
                    
                    if ((DateTime.Now - _timeOfLastDelayPA).TotalMinutes > 15)
                    {
                        comfDec *= 1.5; 
                        anxInc *= 1.5;
                        satDec *= 1.5;
                    }
                    else
                    {
                        satDec *= 0.2; // PA Apology slows down satisfaction drop
                    }
                    
                    if (PassengerAnxiety < 50.0) IncreaseAnxiety(anxInc, phase, isCrisisActive);
                    if (ComfortLevel > 50.0) DecreaseComfort(comfDec);
                    ModifySatisfaction(-satDec);
                    
                    // Layered Delay Warnings from PNC
                    if (_currentDelayMinutes > 10 && (DateTime.Now - _lastDelayNotice).TotalMinutes > 10)
                    {
                        var mins = Math.Round(_currentDelayMinutes);
                        string severityEng = "asking questions about the delay";
                        string severityFre = "commencent à poser des questions sur le retard";
                        
                        if (_currentDelayMinutes > 60) {
                            severityEng = "getting genuinely angry and complaining loudly";
                            severityFre = "sont vraiment en colère et se plaignent bruyamment";
                        } else if (_currentDelayMinutes > 30) {
                            severityEng = "getting very restless and impatient";
                            severityFre = "s'impatientent sérieusement et s'agitent";
                        }

                        if (CrewEsteem >= 80)
                        {
                            OnCrewMessage?.Invoke("info", LocalizationService.Translate(
                                $"Captain, we are {mins} minutes delayed. Passengers are {severityEng}. I am making an announcement to reassure them.",
                                $"Commandant, nous avons {mins} minutes de retard. Les passagers {severityFre}. Je fais une annonce pour les rassurer."
                            ), null);
                            AnnounceToCabin("Delay");
                            CrewEsteem = Math.Min(100.0, CrewEsteem + 5.0);
                            OnOperationBonusTriggered?.Invoke(50, LocalizationService.Translate("Proactive Crew Initiative (Delay PA)", "Initiative PNC Proactif (PA Retard)"));
                        }
                        else
                        {
                            OnCrewMessage?.Invoke("orange", LocalizationService.Translate(
                                $"Captain, we are {mins} minutes delayed past SOBT and passengers are {severityEng}. An announcement from the flight deck would help.",
                                $"Commandant, nous avons {mins} minutes de retard et les passagers {severityFre}. Une annonce du poste de pilotage aiderait."
                            ), null);
                            // Crew is passive, extra hit to satisfaction
                            ModifySatisfaction(-5.0);
                            OnPenaltyTriggered?.Invoke(-15, LocalizationService.Translate("Poor CRM: Passive Crew & Unmanaged Delay", "Mauvais CRM : Équipage Passif et Retard Non Géré"));
                        }
                        _lastDelayNotice = DateTime.Now;
                    }
                }
            }

            // Environmental Anxiety (Night & Low Altitude Approach)
            if (phase != FlightPhase.AtGate && phase != FlightPhase.Turnaround && CurrentSimLocalTime != DateTime.MinValue)
            {
                if (CurrentSimLocalTime.Hour <= 5 || CurrentSimLocalTime.Hour >= 20)
                {
                    IncreaseAnxiety(0.002 * deltaTimeSeconds * (IsLowCost ? 1.5 : 1.0), phase, isCrisisActive);
                }
                if (altitude < 1000 && phase == FlightPhase.Approach)
                {
                    IncreaseAnxiety(0.02 * deltaTimeSeconds * (IsLowCost ? 1.5 : 1.0), phase, isCrisisActive);
                }
            }

            if (phase == FlightPhase.TaxiOut && groundSpeed < 1.0 && isBoarded)
            {
                _cumulativeHoldSeconds += deltaTimeSeconds;
                if (_cumulativeHoldSeconds > 600 && (DateTime.Now - _lastHoldPenaltyTime).TotalMinutes > 10)
                {
                    _lastHoldPenaltyTime = DateTime.Now;
                    ModifySatisfaction(-10.0);
                    IncreaseAnxiety(10.0, phase, false);
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
                    InFlightServiceProgress += baseRate;
                    
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
                IncreaseAnxiety(30.0, phase, isCrisisActive);
                ModifySatisfaction(-50.0);
                CrewEsteem = Math.Max(0.0, CrewEsteem - 20.0);
                OnPenaltyTriggered?.Invoke(-100, LocalizationService.Translate("Catering Shortage: Out of meals", "Rupture Catering : Plus de repas disponibles")); 
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
                string spokenText = "Ladies and gentlemen, the captain has turned on the fasten seatbelt sign due to some turbulence ahead. Please return to your seats immediately and ensure your seatbelts are securely fastened.";
                _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/EnRoute", "EN_Rowan_Turbulence", spokenText);
                OnCrewMessage?.Invoke("orange", LocalizationService.Translate("PA: Please return to your seats and fasten your seatbelts.", "PA: Veuillez regagner vos sièges et attacher vos ceintures."), null);
            }
            else if (announcementType == "TurbulenceApology")
            {
                DecreaseAnxiety(20.0);
                ModifySatisfaction(5.0);
                string spokenText = "Ladies and gentlemen from the flight deck, apologies for the bumpy ride earlier. We've navigated clear of the rough air, and the rest of our flight should be relatively smooth.";
                _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/EnRoute", "EN_Rowan_TurbulenceApology", spokenText);
                OnCrewMessage?.Invoke("info", LocalizationService.Translate("PA: Apologies for the rough ride.", "PA: Excuses suite aux turbulences."), null);
            }
            else if (announcementType == "CruiseStatus")
            {
                _timeOfLastDelayPA = DateTime.Now;
                DecreaseAnxiety(5.0);
                ModifySatisfaction(10.0);
                string destName = CurrentFlight?.Destination?.Name ?? CurrentFlight?.Destination?.IcaoCode ?? "our destination";
                string delayText = "exactly on schedule";
                if (_currentDelayMinutes > 15) {
                    delayText = $"with a delay of {Math.Round(_currentDelayMinutes)} minutes";
                } else if (_currentDelayMinutes < -15) {
                    delayText = $"ahead of schedule by {Math.Abs(Math.Round(_currentDelayMinutes))} minutes";
                }
                string spokenText = $"Ladies and gentlemen, this is your Captain speaking. We have reached our cruising altitude and the flight is progressing {delayText}. We expect a smooth ride for the remainder of our journey. Sit back, relax, and enjoy the rest of the flight.";
                _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/EnRoute", "EN_Rowan_Update", spokenText);
                OnCrewMessage?.Invoke("info", LocalizationService.Translate($"PA: Cruising smoothly towards {destName}.", $"PA: Nous croisons paisiblement vers {destName}."), null);
            }
            else if (announcementType == "DelayApology")
            {
                _timeOfLastDelayPA = DateTime.Now;
                DecreaseAnxiety(15.0);
                ModifySatisfaction(5.0);
                string spokenText = "Ladies and gentlemen, this is the captain speaking. I'd like to extend another apology for our earlier delay. We are doing everything we can to make up some time in the air. Thank you for your continued patience.";
                _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/EnRoute", "EN_Rowan_Delay_Apology", spokenText);
                OnCrewMessage?.Invoke("info", LocalizationService.Translate("PA: Apology for the earlier delay.", "PA: Nouvelles excuses pour le retard passé."), null);
            }
            else if (announcementType == "Descent")
            {
                DecreaseAnxiety(10.0);
                string destName = CurrentFlight?.Destination?.Name ?? CurrentFlight?.Destination?.IcaoCode ?? "our destination";
                string spokenText = $"Ladies and gentlemen, we'll be starting our descent for {destName} shortly. Weather at our destination is currently looking favorable. We'll have another update for you right before landing.";
                _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/FlightDeck to PA", "EN_Rowan_Descent", spokenText);
                OnCrewMessage?.Invoke("info", LocalizationService.Translate($"PA: Descent update for {destName}.", $"PA: Point sur la descente vers {destName}."), null);
            }
        }

        public void AnnounceDelay(string reason, string destName)
        {
            if (!_issuedCommands.Contains("PA_Delay"))
            {
                _issuedCommands.Add("PA_Delay");
            }
            
            _manualApologyCount++;
            _timeOfLastDelayPA = DateTime.Now;
            
            if (_manualApologyCount <= 2)
            {
                DecreaseAnxiety(40.0);
                ModifySatisfaction(10.0);
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

                string spokenText = $"Ladies and gentlemen from the flight deck, I'd like to apologize for the delay. We are currently waiting for {spokenReason} and expect to be moving in about {Math.Max(10, Math.Round(_currentDelayMinutes))} minutes. Thank you for your patience.";
                _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/FlightDeck to PA", "EN_Rowan_Delay", spokenText);

                OnCrewMessage?.Invoke("orange", LocalizationService.Translate($"PA: Apologies for the delay ({spokenReason}), we will be departing shortly.", $"PA: Toutes nos excuses pour ce retard ({spokenReason}), nous partons bientôt."), null);
            }
            else
            {
                ModifySatisfaction(-15.0);
                IncreaseAnxiety(10.0, FlightPhase.AtGate, false);
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

        private void SequenceWeatherPA(string metar, int destTempC, bool isApproach, bool playChime = true)
        {
            metar = metar.ToUpper();
            
            if (isApproach) {
                _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Weather_Intro_Approach_01.mp3", null, playChime);
            } else {
                _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/Weather", "EN_Rowan_Weather_Intro_0", null, playChime);
            }

            // 1. Sky cover
            if (metar.Contains(" OVC")) _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Sky_Overcast.mp3", null, false);
            else if (metar.Contains(" BKN") || metar.Contains(" SCT")) _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Sky_Scattered.mp3", null, false);
            else if (metar.Contains(" FEW")) _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Sky_Few.mp3", null, false);
            else _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Sky_Clear.mp3", null, false);

            // 2. Precipitation
            if (metar.Contains(" TS") || metar.Contains(" CB")) _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Precip_SevereStorm.mp3", null, false);
            else if (metar.Contains(" SN") || metar.Contains(" SG"))
            {
                if (metar.Contains("+SN")) _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Precip_Snow.mp3", null, false);
                else _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Precip_LightSnow.mp3", null, false);
            }
            else if (metar.Contains(" FZ") || metar.Contains(" IC") || metar.Contains(" GR")) _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Precip_Ice.mp3", null, false);
            else if (metar.Contains(" RA") || metar.Contains(" DZ") || metar.Contains(" SH"))
            {
                if (metar.Contains("+RA")) _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Precip_HeavyRain.mp3", null, false);
                else if (metar.Contains("-RA") || metar.Contains("-DZ")) _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Precip_LightRain.mp3", null, false);
                else _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Precip_Rain.mp3", null, false);
            }

            // 3. Visibility and Wind
            if (metar.Contains(" FG") || metar.Contains(" BR") || metar.Contains(" HZ")) {
                _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Vis_Fog.mp3", null, false);
            }
            
            var windMatch = System.Text.RegularExpressions.Regex.Match(metar, @"\d{3}(\d{2})G");
            if (windMatch.Success && int.TryParse(windMatch.Groups[1].Value, out int windSpd) && windSpd > 25) {
                _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Wind_Strong.mp3", null, false);
            }

            // 4. Temp
            _audio?.PlayExactAsCaptain("TO_PA/Weather/EN_Rowan_Temp_Intro.mp3", null, false);
            string tempSign = destTempC < 0 ? "M" : "P";
            string tempVal = Math.Abs(destTempC).ToString("D2");
            _audio?.PlayExactAsCaptain($"TO_PA/Temperatures/EN_Rowan_{tempSign}{tempVal}.mp3", null, false);
        }

        public void AnnounceWelcome(string destIcao, string destName, int flightTimeMinutes, bool badWeather, int destTempC, DateTime destLocalTime, DateTime departureLocalTime, string metar)
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
            string airlineName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ActiveAirlineId.Replace("_", " "));
            string aircraftType = "A320"; // Safe default
            if (CurrentFlight?.Aircraft != null)
            {
                if (!string.IsNullOrWhiteSpace(CurrentFlight.Aircraft.BaseType)) aircraftType = CurrentFlight.Aircraft.BaseType;
                else if (!string.IsNullOrWhiteSpace(CurrentFlight.Aircraft.IcaoCode)) aircraftType = CurrentFlight.Aircraft.IcaoCode;
                else if (!string.IsNullOrWhiteSpace(CurrentFlight.Aircraft.Name)) aircraftType = CurrentFlight.Aircraft.Name;
            }
            
            int hr = departureLocalTime.Hour;
            string greeting = hr < 12 ? "Morning" : (hr < 18 ? "Afternoon" : "Evening");
            string wxcConditions = !badWeather ? "smooth flight" : "few bumps along the way";

            string timeStr = flightTimeMinutes >= 60 ? $"{flightTimeMinutes / 60} hour(s) and {flightTimeMinutes % 60} minutes" : $"{flightTimeMinutes} minutes";
            string spokenText = $"Ladies and gentlemen, good {greeting} from the flightdeck. My name is {CaptainName} and I am your captain today. On behalf of {airlineName} I would like to welcome you all on board this {aircraftType} on our flight to {destName}. Today flight time will be approximately {timeStr} and we're expecting a {wxcConditions}. We're just finishing the last paper work and once completed we will start our pushback. The weather at our destination is currently {destTempC} degrees Celsius. Thank you very much for being our guests. Sit back, relax, and enjoy this flight with us.";
            
            // --- NORMALISE AIRLINE & AIRCRAFT TYPE ---
            string rawAirline = ActiveAirlineId.ToUpper();
            string normAirline = rawAirline;
            if (rawAirline.StartsWith("EJU") || rawAirline.StartsWith("EZS") || rawAirline.StartsWith("EZY") || rawAirline.Contains("EASYJET")) 
            {
                normAirline = "EZY";
            }
            else if (rawAirline.StartsWith("AFR") || rawAirline.Contains("AIR_FRANCE") || rawAirline.Contains("AIRFRANCE"))
            {
                normAirline = "AFR";
            }

            string normAircraft = aircraftType.ToUpper();
            if (normAircraft == "A20N" || normAircraft == "A32N") normAircraft = "A320";
            if (normAircraft == "A21N") normAircraft = "A321";
            if (normAircraft == "A19N") normAircraft = "A319";

            // --- DYNAMIC AUDIO SEQUENCE ---
            // 1. Greeting
            _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/FlightDeck to PA", $"EN_Rowan_Welcome_Onboard_This_{greeting}", spokenText, playChime: true);
            
            // 2. Airline
            if (!string.IsNullOrEmpty(normAirline)) {
                _audio?.PlayExactAsCaptain($"TO_PA/Airlines/EN_Rowan_{normAirline}_{normAircraft}.mp3", null, playChime: false);
            }

            // 3. Destination (ICAO)
            _audio?.PlayExactAsCaptain($"TO_PA/ICAO/EN_Rowan_{destIcao}_01.mp3", null, playChime: false);

            // 4. Flight Time
            _audio?.PlayExactAsCaptain("TO_PA/FlightDeck to PA/EN_Rowan_Welcome_Onboard_Flight_Time.mp3", null, playChime: false);
            int eH = flightTimeMinutes / 60;
            int eM = flightTimeMinutes % 60;
            if (eH > 0 && eH <= 12) _audio?.PlayExactAsCaptain($"TO_PA/Hours/EN_Rowan_{eH}hour.mp3", null, playChime: false);
            if (eM > 0 && eM <= 59) _audio?.PlayExactAsCaptain($"TO_PA/Minutes/EN_Rowan_{eM}min.mp3", null, playChime: false);

            // 5. Weather
            SequenceWeatherPA(metar, destTempC, isApproach: false, playChime: false);

            // 6. Local Time
            _audio?.PlayExactAsCaptain("TO_PA/FlightDeck to PA/EN_Rowan_Welcome_Onboard_Local_Time.mp3", null, playChime: false);
            
            int destHour12 = destLocalTime.Hour % 12;
            if (destHour12 == 0) destHour12 = 12; // 12 AM / 12 PM format
            
            _audio?.PlayExactAsCaptain($"TO_PA/Time_Minutes/EN_Rowan_{NumberToExactWord(destHour12)}.mp3", null, playChime: false);
            
            if (destLocalTime.Minute == 0) {
                _audio?.PlayExactAsCaptain("TO_PA/Time_Modifiers/EN_Rowan_O_Clock.mp3", null, playChime: false);
            } else {
                _audio?.PlayExactAsCaptain($"TO_PA/Time_Minutes/EN_Rowan_{NumberToExactWord(destLocalTime.Minute)}.mp3", null, playChime: false);
            }

            string ampm = destLocalTime.Hour < 12 ? "AM" : "PM";
            _audio?.PlayExactAsCaptain($"TO_PA/Time_Modifiers/EN_Rowan_{ampm}.mp3", null, playChime: false);

            // 7. Outro (Paperwork)
            _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/FlightDeck to PA", "EN_Rowan_Welcome_Onboard_Paper_Work_", null, playChime: false);

            // UI Dispatch
            string notifText = $"PA: Welcome aboard our flight to {destName}. Our flight time will be approx {timeStr}. The weather at our destination is currently {wxcText}.";
            string notifFr = $"PA: Bienvenue à bord de ce vol à destination de {destName}. Notre temps de vol sera d'environ {timeStr}. La météo à l'arrivée s'annonce {wxcFr}.";
            OnCrewMessage?.Invoke("green", LocalizationService.Translate(notifText, notifFr), null);
        }

        public void AnnounceApproach(string destName, string metar, int destTempC, DateTime destLocalTime)
        {
            if (!_issuedCommands.Contains("PA_Approach"))
            {
                _issuedCommands.Add("PA_Approach");
                OnOperationBonusTriggered?.Invoke(25, "Passenger Announcement: Approach");
            }

            // 1. Initial Greeting
            string introFallback = $"Ladies and gentlemen, we've just been cleared for our approach to {destName} and we will be landing shortly.";
            _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/FlightDeck to PA", "EN_Rowan_Approach_ClearedToLand", introFallback, playChime: true);

            // 2. Local Time at Dest
            _audio?.PlayExactAsCaptain("TO_PA/FlightDeck to PA/EN_Rowan_Welcome_Onboard_Local_Time.mp3", null, playChime: false);
            int destHour12 = destLocalTime.Hour % 12;
            if (destHour12 == 0) destHour12 = 12;
            _audio?.PlayExactAsCaptain($"TO_PA/Time_Minutes/EN_Rowan_{NumberToExactWord(destHour12)}.mp3", null, playChime: false);
            if (destLocalTime.Minute == 0) {
                _audio?.PlayExactAsCaptain("TO_PA/Time_Modifiers/EN_Rowan_O_Clock.mp3", null, playChime: false);
            } else {
                _audio?.PlayExactAsCaptain($"TO_PA/Time_Minutes/EN_Rowan_{NumberToExactWord(destLocalTime.Minute)}.mp3", null, playChime: false);
            }
            string ampm = destLocalTime.Hour < 12 ? "AM" : "PM";
            _audio?.PlayExactAsCaptain($"TO_PA/Time_Modifiers/EN_Rowan_{ampm}.mp3", null, playChime: false);

            // 3. Weather
            SequenceWeatherPA(metar, destTempC, isApproach: true, playChime: false);

            // 4. Outro
            _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/FlightDeck to PA", "EN_Rowan_Approach_CabinCrewPrepare", "Cabin crew, prepare for landing.", playChime: false);

            bool badWeather = metar.Contains(" TS") || metar.Contains(" CB") || metar.Contains(" FG") || metar.Contains(" SN") || metar.Contains(" GR");

            if (!badWeather) {
                DecreaseAnxiety(20.0);
            } else {
                IncreaseAnxiety(10.0, FlightPhase.Approach, false);
            }

            string notifText = $"PA: We are descending towards {destName}. Cabin crew is preparing for landing.";
            string notifFr = $"PA: Nous descendons vers {destName}. L'équipage prépare la cabine.";
            OnCrewMessage?.Invoke("green", LocalizationService.Translate(notifText, notifFr), null);
        }

        public void AnnounceCruise(int altitude, string avgWindComp, string destName, string destMetar, string enrtMetar, int destTempC, DateTime currentLocalTime)
        {
            if (!_issuedCommands.Contains("PA_CruiseStatus"))
            {
                _issuedCommands.Add("PA_CruiseStatus");
                OnOperationBonusTriggered?.Invoke(15, "Passenger Announcement: Cruise Update");
            }

            // 1. Initial Greeting (Time of day)
            string greetingPrefix = "Morn";
            if (currentLocalTime.Hour >= 12 && currentLocalTime.Hour < 18) greetingPrefix = "Aftn";
            else if (currentLocalTime.Hour >= 18) greetingPrefix = "Eve";

            _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/Cruise_Intro", $"{greetingPrefix}_", "Ladies and gentlemen from the flight deck, we have reached our cruising altitude.", playChime: true);

            // 2. Altitude (nearest thousand)
            int roundedAlt = (int)(Math.Round(altitude / 1000.0) * 1000);
            if (roundedAlt < 30000) roundedAlt = 30000;
            if (roundedAlt > 43000) roundedAlt = 43000;
            
            // Bug #5 Fix: Resolving TTS fallback by formatting string exactly like actual files (e.g., "EN_Rowan_FL300.mp3")
            _audio?.PlayExactAsCaptain($"TO_PA/Altitude/EN_Rowan_FL{roundedAlt / 100}.mp3", $"We are currently at {roundedAlt} feet.", playChime: false);

            // 3. Winds
            int windSpeed = 0;
            bool isTailwind = false;
            bool hasWind = false;

            if (!string.IsNullOrEmpty(avgWindComp) && avgWindComp.Length >= 4)
            {
                string type = avgWindComp.Substring(0, 1); // P or M
                if (int.TryParse(avgWindComp.Substring(1), out windSpeed))
                {
                    if (windSpeed >= 20)
                    {
                        hasWind = true;
                        isTailwind = (type == "P");
                    }
                }
            }

            if (hasWind)
            {
                string windDirPrefix = isTailwind ? "Tailwind" : "Headwind";
                string fallbackDir = isTailwind ? "tailwind" : "headwind";
                _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/Winds", $"{windDirPrefix}_", $"We are experiencing a {fallbackDir}.", playChime: false);

                // Round speed to nearest 20 
                int roundedWspd = (int)(Math.Round(windSpeed / 20.0) * 20);
                if (roundedWspd < 20) roundedWspd = 20;
                if (roundedWspd > 140) roundedWspd = 140;

                _audio?.PlayExactAsCaptain($"TO_PA/Winds/EN_Rowan_Spd_{roundedWspd}.mp3", $"of about {roundedWspd} knots.", playChime: false);
            }
            else
            {
                _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/Winds", "Calm_", "The winds aloft are relatively calm today.", playChime: false);
            }

            // 4. Enroute Weather
            bool hasStorms = enrtMetar.Contains(" TS") || enrtMetar.Contains(" CB");
            if (hasStorms) {
                _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/EnRoute", "Stormy_", "We do have some significant weather to navigate around coming up, so I will be keeping the seatbelt sign on.", playChime: false);
            } else if (enrtMetar.Contains(" RA") || enrtMetar.Contains(" SN") || destMetar.Contains(" TS")) {
                 _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/EnRoute", "Bumpy_", "Looking ahead, we are seeing a few weather systems along our route, so we might experience a few bumps.", playChime: false);
            } else {
                _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/EnRoute", "Smooth_", "Looking ahead at the weather radar, the route is completely clear, so it should be a very smooth ride all the way.", playChime: false);
            }

            // 5. Arrival Transition
            _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/Arrival", "ArrTrans_", $"As for our destination in {destName}...", playChime: false);

            // 6. Destination Weather (reuse weather sequencer)
            SequenceWeatherPA(destMetar, destTempC, isApproach: true, playChime: false);

            // 7. Outro
            _audio?.PlayVariantWithPrefixAsCaptain("TO_PA/Cruise_Outro", "Outro_", "I will get back to you with an updated ETA before our descent. Until then, sit back, relax, and enjoy the rest of the flight.", playChime: false);

            OnCrewMessage?.Invoke("green", LocalizationService.Translate("PA: Cruise Update (Altitude & Enroute Weather)", "PA: Informations de Croisière (Altitude et météo)"), null);
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
                inc = Math.Max(0, 60.0 - PassengerAnxiety);
            }
            else if (!isCrisisActive && (PassengerAnxiety + inc) > 90.0)
            {
                inc = Math.Max(0, 90.0 - PassengerAnxiety);
            }

            if (IsSatietyActive && phase == FlightPhase.Cruise) inc *= 0.5; 
            
            ModifyAnxiety(inc);
            DecreaseComfort((inc * BaseComfortLossMultiplier) * 0.5); 
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

        public void Reset()
        {
            ClearAnxiety();
            
            State = CabinState.Idle;
            HasBoardingStarted = false;
            _hasAnnouncedBoardingComplete = false;
            _hasAnnouncedGalleySecured = false;
            _lastBoardingTick = DateTime.MaxValue;
            _thermalDissatisfactionGauge = 0.0;
            _hasWarnedPushbackNoSeatbelts = false;
            HasPenalizedRefuelingSeatbelts = false;
            
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
            _issuedCommands.Clear();
            HasBoardingStarted = false;
            _lastBoardingTick = DateTime.MaxValue;
            _hasAnnouncedBoardingComplete = false;
            _hasAnnouncedGalleySecured = false;
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

                if (isSevere && !p.IsSeatbeltFastened) p.IndividualAnxiety += 2.0;
                if (p.IndividualAnxiety > 100) p.IndividualAnxiety = 100;
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
                    _audio?.PlayVariantAsPurser("TO_FD/Checklist_Reports/CabinReady", repEn);
                }
                else
                {
                    _audio?.PlayVariantAsPurser("TO_FD/Status_Reports/BoardingInProgress", repEn);
                }
                
                OnCrewMessage?.Invoke("info", LocalizationService.Translate(repEn, repFr), null);
                return true;
            }

            if (_isSecuring && SecuringProgress < 100.0)
            {
                _strategicPenaltyEndTime = DateTime.Now.AddSeconds(15);
                OnCrewMessage?.Invoke("warning", LocalizationService.Translate(
                    "Captain, we're still securing the cabin. We cannot provide a status update right now.",
                    "Commandant, nous sécurisons actuellement la cabine. Impossible de faire un point pour le moment."), null);
                return false;
            }

            return ExecuteStandardFlightReport(phase, isCrisisActive);
        }

        public void PlayCabinReady()
        {
            string repEn = "Cabin checks are complete. We are ready when you are to begin boarding.";
            string repFr = "Les vérifications cabine sont terminées. Nous sommes prêts à débuter l'embarquement.";
            _audio?.PlayVariantAsPurser("TO_FD/Checklist_Reports/CabinReady", repEn);
            OnCrewMessage?.Invoke("info", LocalizationService.Translate(repEn, repFr), null);
        }

        private bool ExecuteStandardFlightReport(FlightPhase phase, bool isCrisisActive)
        {
            string reportEn = "Cabin is clear and quiet, Captain. Everyone is relaxed.";
            string reportFr = "La cabine est calme et prête, Commandant. Tout le monde est détendu.";
            string audioFolder = "TO_FD/Status_Reports/CabinCalm";

            if (State == CabinState.Idle)
            {
                if (phase == FlightPhase.Climb || phase == FlightPhase.InitialClimb)
                {
                    reportEn = "We're currently waiting for the seatbelt sign to be turned off so we can prepare for service.";
                    reportFr = "Nous attendons l'extinction de la consigne des ceintures pour pouvoir préparer le service.";
                    audioFolder = "TO_FD/Status_Reports/CabinCalm"; // TTS fallback
                }
                else if (phase == FlightPhase.Cruise)
                {
                    if (InFlightServiceProgress == 0 && !IsServiceHalted) 
                    {
                        reportEn = "We are organizing the galleys for the upcoming service.";
                        reportFr = "Nous organisons les galleys en vue du service à bord.";
                        audioFolder = "TO_FD/Status_Reports/CabinCalm"; // TTS fallback
                    }
                    else if (InFlightServiceProgress >= 100)
                    {
                        reportEn = "The meal service is completed and we've cleared the trays.";
                        reportFr = "Le service est terminé et les plateaux ont été débarrassés.";
                        audioFolder = "TO_FD/Status_Reports/ServiceFinished";
                    }
                }
                else if (phase == FlightPhase.Descent)
                {
                    reportEn = "We are doing a final pass to collect trash and prepare the cabin for arrival.";
                    reportFr = "Nous faisons un dernier passage pour débarrasser les détritus et préparer l'arrivée.";
                    audioFolder = "TO_FD/Status_Reports/CabinCalm"; // TTS fallback
                }
                else if (phase == FlightPhase.TaxiIn)
                {
                    reportEn = "Everyone is seated and waiting to arrive at the gate, Captain.";
                    reportFr = "Tout le monde est assis et attend l'arrivée au point de stationnement.";
                    audioFolder = "TO_FD/Status_Reports/CabinCalm"; // TTS fallback
                }
            }

            // Operational State Mapping
            if (State == CabinState.Deboarding)
            {
                reportEn = "We're currently deboarding the passengers. Almost done back here.";
                reportFr = "Le débarquement est en cours. Nous avons presque terminé.";
                audioFolder = "TO_FD/Status_Reports/CabinCalm"; // TTS fallback
            }
            else if (State == CabinState.Boarding)
            {
                reportEn = "We are still getting passengers settled in their seats, Captain.";
                reportFr = "Nous installons encore les passagers à leurs places, Commandant.";
                audioFolder = "TO_FD/Status_Reports/BoardingInProgress";
            }
            else if (State == CabinState.SecuringForTakeoff || (_isSecuring && phase == FlightPhase.TaxiOut))
            {
                reportEn = "Captain, we haven't finished securing the galleys yet, we need a few more minutes!";
                reportFr = "Commandant, nous n'avons pas fini de préparer la cabine, il nous faut quelques minutes de plus !";
                audioFolder = "TO_FD/Status_Reports/SecuringInProgress";
            }
            else if (State == CabinState.TakeoffSecured)
            {
                reportEn = "Cabin is fully secured and ready for takeoff. Everyone is seated.";
                reportFr = "La cabine est entièrement préparée et prête pour le décollage.";
                audioFolder = "TO_FD/Status_Reports/CabinSecured";
            }
            else if (State == CabinState.ServingMeals)
            {
                if (InFlightServiceProgress < 20) {
                    reportEn = "We've just started preparing the service carts.";
                    reportFr = "Nous venons de commencer la préparation des chariots de service.";
                    audioFolder = "TO_FD/Status_Reports/ServiceStarting";
                } else if (InFlightServiceProgress < 80) {
                    reportEn = "The meal service is in full swing. Everyone seems satisfied.";
                    reportFr = "Le service des repas bat son plein. Tout le monde semble satisfait.";
                    audioFolder = "TO_FD/Status_Reports/ServiceInProgress";
                } else {
                    reportEn = "We are currently picking up the trays and clearing the aisles.";
                    reportFr = "Nous ramassons actuellement les plateaux et débarrassons les allées.";
                    audioFolder = "TO_FD/Status_Reports/ServiceFinished";
                }
            }
            else if (State == CabinState.SecuringForLanding || (_isSecuring && (phase == FlightPhase.Descent || phase == FlightPhase.Approach)))
            {
                reportEn = "Galley is being secured, and we're starting the final cabin check.";
                reportFr = "Le galley est en cours de sécurisation, nous débutons la vérification finale.";
                audioFolder = "TO_FD/Status_Reports/SecuringInProgress";
            }
            else if (State == CabinState.LandingSecured)
            {
                reportEn = "Cabin is secure for landing. The crew is seated.";
                reportFr = "La cabine est sécurisée pour l'atterrissage. L'équipage est assis.";
                audioFolder = "TO_FD/Status_Reports/CabinSecured";
            }

            // Situational & Crisis Overrides (Highest Priority)
            if (isCrisisActive)
            {
                reportEn = "Captain, the passengers are panicking! What's going on?!";
                reportFr = "Commandant, les passagers paniquent ! Que se passe-t-il ?!";
                audioFolder = "TO_FD/Status_Reports/AnxiousPax";
            }
            else if (PassengerManifest.Exists(p => p != null && p.IsInjured))
            {
                var injured = PassengerManifest.FindAll(p => p != null && p.IsInjured);
                reportEn = $"We are still tending to {injured.Count} injured passenger(s). The mood is very somber.";
                reportFr = $"Nous nous occupons toujours de {injured.Count} passager(s) blessé(s). L'ambiance est très lourde.";
                audioFolder = "TO_FD/Status_Reports/SevereTurbulence";
            }
            else if (WasteLevel > 80.0)
            {
                reportEn = "Captain, the passengers are getting uncomfortable. The lavatories are becoming an issue with the waste tanks almost full.";
                reportFr = "Commandant, les passagers commencent à se plaindre des toilettes. Les réservoirs sont presque pleins.";
                audioFolder = "TO_FD/Status_Reports/UncomfortablePax";
            }
            else if (audioFolder == "TO_FD/Status_Reports/CabinCalm" || audioFolder == "TO_FD/Status_Reports/ServiceFinished") // Only apply gauge logic if calm or service finished
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
                    audioFolder = "TO_FD/Status_Reports/AnxiousPax";
                }
                else if (PassengerAnxiety > 50.0)
                {
                    if ((DateTime.Now - _lastTurbulenceNotice).TotalMinutes < 15)
                    {
                        reportEn = "It's been quite bumpy, and the cabin is feeling very tense and anxious right now.";
                        reportFr = "Ça a secoué pas mal, l'ambiance est très tendue et anxieuse en cabine.";
                        audioFolder = "TO_FD/Status_Reports/TurbulenceReport";
                    }
                    else if (_hasTriggeredCateringComplaint)
                    {
                        reportEn = "People are very unhappy about not getting their meals yet. It's tough back here.";
                        reportFr = "Les gens sont très mécontents de ne pas avoir eu de repas. C'est difficile à l'arrière.";
                        audioFolder = "TO_FD/Status_Reports/AnxiousPax";
                    }
                    else
                    {
                        reportEn = "Note that some passengers are quite anxious about the flight.";
                        reportFr = "À noter que certains passagers sont assez anxieux par rapport au vol.";
                        audioFolder = "TO_FD/Status_Reports/AnxiousPax";
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
                    audioFolder = "TO_FD/Status_Reports/UncomfortablePax";
                }
                else if (ComfortLevel < 40.0)
                {
                    reportEn = "Passengers are complaining about the general comfort level.";
                    reportFr = "Les passagers se plaignent du niveau de confort général.";
                    audioFolder = "TO_FD/Status_Reports/UncomfortablePax";
                }
                else
                {
                    reportEn = "Cabin is clear and quiet, Captain. Everyone is relaxed.";
                    reportFr = "Cabine calme et tranquille, Commandant. Tout le monde est détendu.";
                }
            }

            _audio?.PlayVariantAsPurser(audioFolder, reportEn);
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
                        IncreaseAnxiety(addAnx, phase, false);
                        _hasAppliedDepartureWeatherAnxiety = true;
                    }
                }
            }

            // Arrival
            if (!_hasAppliedArrivalWeatherAnxiety && (phase == FlightPhase.Descent || phase == FlightPhase.Approach || phase == FlightPhase.Landing))
            {
                var arr = weatherData.Stations.FirstOrDefault(s => s.Id.Equals("destination", StringComparison.OrdinalIgnoreCase));
                if (arr != null)
                {
                    double addAnx = 0;
                    if (arr.WindSeverity == FlightSupervisor.UI.Models.WeatherSeverity.Danger) addAnx += 12.0;
                    else if (arr.WindSeverity == FlightSupervisor.UI.Models.WeatherSeverity.Warning) addAnx += 6.0;

                    if (arr.VisibilitySeverity == FlightSupervisor.UI.Models.WeatherSeverity.Danger) addAnx += 10.0;
                    else if (arr.VisibilitySeverity == FlightSupervisor.UI.Models.WeatherSeverity.Warning) addAnx += 5.0;

                    if (addAnx > 0)
                    {
                        IncreaseAnxiety(addAnx, phase, false);
                        _hasAppliedArrivalWeatherAnxiety = true;
                    }
                }
            }
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
    }
}



