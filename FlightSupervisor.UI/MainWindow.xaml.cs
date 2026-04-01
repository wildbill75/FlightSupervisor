using System;
using System.Windows;
using System.Windows.Interop;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Linq;
using Microsoft.Web.WebView2.Core;
using FlightSupervisor.UI.Services;
using FlightSupervisor.UI.Models.SimBrief;

namespace FlightSupervisor.UI
{
    public partial class MainWindow : Window
    {
        private readonly SimBriefService _simBriefService;
        private SimConnectService _simConnectService;
        private FlightPhaseManager _phaseManager;
        private SimBriefResponse? _currentResponse = null;
        private Queue<SimBriefResponse> _rotationQueue = new();
        
        private double _lastKnownGroundSpeed = 0;
        private double _lastKnownAirspeed = 0;
        private double _lastKnownAltitude = 0;
        private double _lastKnownRadioHeight = 0;
        private bool _isParkingBrakeSet = false;
        private double _lastKnownThrottle = 0;
        private double _lastKnownPitch = 0;
        private double _lastKnownVerticalSpeed = 0;
        private double _lastKnownBank = 0;
        private bool _isGearDown = true;
        private DateTime _currentSimTime = DateTime.MinValue;
        private DateTime? _aobt = null;
        private DateTime? _aibt = null;
        private bool? _lastLogGearDown = null;
        private bool? _lastLogParkingBrake = null;
        private double? _lastLogFlaps = null;
        private bool? _lastLogAutopilot = null;
        private bool? _lastLogAutothrust = null;
        private double? _lastLogThrottle = null;
        private double? _lastLogSpoilers = null;
        private bool? _lastLogLightBeacon = null;
        private bool? _lastLogLightStrobe = null;
        private bool? _lastLogLightNav = null;
        private bool? _lastLogLightLanding = null;
        private bool? _lastLogLightTaxi = null;
        private bool? _lastLogSeatbelts = null;
        private bool? _lastLogApuMaster = null;
        private bool? _lastLogApuStart = null;
        private bool? _lastLogApuBleed = null;
        private bool? _lastLogPack1 = null;
        private bool? _lastLogPack2 = null;
        private float? _lastLogTempCkpt = null;
        private float? _lastLogTempFwd = null;
        private float? _lastLogTempAft = null;
        private bool _hasReceivedFenixLvars = false;
        
        private GroundOpsManager _groundOpsManager;
        private System.Windows.Threading.DispatcherTimer _uiTimer;
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private PanelServerService? _panelServer;
        private AudioEngineService _audioEngine;
        private SuperScoreManager _scoreManager;
        
        private double _currentFobKg = 0;

        private CabinManager _cabinManager;
        private GroundOpsResourceService _groundOpsResourceService;
        private AirlineProfileManager _airlineDb;
        private FlightSupervisor.UI.Services.GroundEventEngine _eventEngine;
        private CrisisManager _crisisManager;
        public AirlineProfile? CurrentAirline { get; private set; }
        private ProfileManager _profileManager;
        private ActiveSkyService _activeSkyService;
        private NoaaWeatherService _noaaWeatherService;
        private System.Windows.Threading.DispatcherTimer _weatherUpdateTimer;

        public MainWindow()
        {
            InitializeComponent();
            _audioEngine = new AudioEngineService();
            _simBriefService = new SimBriefService();
            _activeSkyService = new ActiveSkyService();
            _noaaWeatherService = new NoaaWeatherService();
            _panelServer = new PanelServerService();
            _panelServer.StartServer();

            _airlineDb = new AirlineProfileManager(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Airlines.json"));
            _profileManager = new ProfileManager();

            _groundOpsManager = new GroundOpsManager();
            _groundOpsManager.OnOpsCompleted += () => SendToWeb(new { type = "groundOpsComplete" });
            _groundOpsManager.OnOpsLog += msg => SendToWeb(new { type = "log", message = msg });
            _groundOpsManager.OnOpsUpdated += () => {
                var services = _groundOpsManager.Services;
                Dispatcher.Invoke(() => SendToWeb(new { type = "groundOps", services = services }));
            };

            _eventEngine = new FlightSupervisor.UI.Services.GroundEventEngine();
            _eventEngine.OnEventTriggered += OnGroundEventTriggered;

            _cabinManager = new CabinManager(_audioEngine);
            _groundOpsResourceService = new GroundOpsResourceService(_groundOpsManager, _cabinManager);
            _cabinManager.OnCrewMessage += (level, msg, audioSeq) => SendToWeb(new { type = "cabinLog", level = level, message = msg, audioSequence = audioSeq });
            _cabinManager.OnPenaltyTriggered += (points, reason) => {
                if (_scoreManager != null) _scoreManager.AddScore(points, reason, ScoreCategory.Comfort);
            };
            _cabinManager.OnPncStatusChanged += (status, state) => {
                Dispatcher.Invoke(() => SendToWeb(new { type = "pncStatus", status = status, state = state.ToString() }));
            };
            _cabinManager.OnOperationBonusTriggered += (bonus, reason) => {
                if (reason == "pnc_ready_chime") {
                    SendToWeb(new { type = "playSound", sound = "chime_emergency" });
                }
            };
            _cabinManager.OnMedicalEmergencyRequested += () => {
                if (_crisisManager != null && _crisisManager.ActiveCrisis == CrisisType.None) {
                    _crisisManager.TriggerSpecificCrisis(CrisisType.MedicalEmergency);
                }
            };
            
            _cabinManager.OnDeboardingComplete += () => {
                _cabinManager.SessionFlightsCompleted++;

                if (_rotationQueue.Count > 0)
                {
                    LoadNextLeg();
                    SendToWeb(new { type = "log", message = $"[SYSTEM] Cabin secured, initiating turnaround operations. {_rotationQueue.Count} leg(s) remaining in rotation." });
                }
                else
                {
                    _groundOpsManager.PrepareNextLeg(_currentFobKg);
                    SendToWeb(new { type = "log", message = "[SYSTEM] Shift completed. No further flights scheduled." });
                    Dispatcher.Invoke(() => SendToWeb(new { type = "groundOps", services = _groundOpsManager.Services }));
                }
            };

            // Tray Icon Setup
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            _notifyIcon.Text = "Flight Supervisor";
            _notifyIcon.Visible = false;
            _notifyIcon.DoubleClick += (s, args) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                _notifyIcon.Visible = false;
            };

            // Start ACARS Live Weather updater
            _weatherUpdateTimer = new System.Windows.Threading.DispatcherTimer();
            _weatherUpdateTimer.Interval = TimeSpan.FromMinutes(15);
            _weatherUpdateTimer.Tick += async (s, e) => { await RefreshLiveWeatherAsync(); };
            _weatherUpdateTimer.Start();

            // Start Dashboard update loop
            _uiTimer = new System.Windows.Threading.DispatcherTimer();
            _uiTimer.Interval = TimeSpan.FromSeconds(1);
            _uiTimer.Tick += (s, e) => {
                _groundOpsManager.Tick(_currentSimTime);

                // --- MULTI-LEG PROGRESSIVE GROUND OP REFILLS ---
                _groundOpsResourceService.Tick((int)_uiTimer.Interval.TotalMilliseconds);
                // ------------------------------------------------

                int totalSvc = _groundOpsManager.Services.Count;
                if (totalSvc > 0)
                {
                    var activeSvc = _groundOpsManager.Services.Where(s => !s.IsPreServiced && s.State != GroundServiceState.Skipped).ToList();
                    int currentElapsed = activeSvc.Sum(s => s.ElapsedSec);
                    int totExpected = activeSvc.Sum(s => s.TotalDurationSec + s.DelayAddedSec);
                    double pct = totExpected > 0 ? ((double)currentElapsed / totExpected * 100) : 100;
                    int doneSvc = _groundOpsManager.Services.Count(s => s.State == GroundServiceState.Completed || s.State == GroundServiceState.Skipped);

                    string timeStr = "";
                    if (_groundOpsManager.TargetSobt.HasValue && _groundOpsManager.IsAnyOperationInProgress())
                    {
                        var diff = _groundOpsManager.TargetSobt.Value - _currentSimTime;
                        if (diff.TotalMinutes > 0) timeStr = $"T-Minus {Math.Ceiling(diff.TotalMinutes)}m";
                        else timeStr = "SOBT REACHED";
                    }

                    SendToWeb(new { type = "groundOpsProgress", isActive = _groundOpsManager.IsAnyOperationInProgress() || doneSvc > 0, pct = pct, status = _groundOpsManager.GetStatusString(), timeString = timeStr });
                }

                if (_groundOpsManager.IsAnyOperationInProgress() && !_groundOpsManager.IsPaused && _phaseManager.CurrentPhase == FlightPhase.AtGate)
                {
                    _eventEngine.Tick(_groundOpsManager.EventProbabilityPercent, CurrentAirline);
                }

                if (_groundOpsManager.IsAnyOperationInProgress() && (_phaseManager.GroundSpeed > 1.0 || !_phaseManager.IsOnGround))
                {
                    _groundOpsManager.AbortAllOperations();
                    _scoreManager.CancelFlight(LocalizationService.GetString("FlightCancel"));
                    SendToWeb(new { type = "flightCancelled" });
                }
                
                // Track Cabin Anxiety
                var bService = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Boarding");
                bool isBoardingComplete = bService?.State == GroundServiceState.Completed;
                _cabinManager.HasBoardingStarted = bService != null && bService.State != GroundServiceState.NotStarted;
                DateTime? sobtDate = null;
                if (_currentResponse?.Times?.SchedOut != null && long.TryParse(_currentResponse.Times.SchedOut, out long sobtUnix))
                {
                    sobtDate = DateTimeOffset.FromUnixTimeSeconds(sobtUnix).DateTime;
                }
                var boardingService = _groundOpsManager?.Services?.FirstOrDefault(s => s.Name == "Boarding");
                double boardingProg = boardingService != null ? (boardingService.ProgressPercent / 100.0) : -1.0;

                double currentCabinTemp = 22.0;

                // Fenix Cabin Temp Fwd/Aft expose the rotary knob position from 0.0 (Cold) to 1.0 (Hot). 
                // We map this into an empirical A320 range of 18°C to 30°C. If Lvar sends actual temp, we use it directly.
                double mappedTempFwd = _phaseManager.FenixCabinTempFwd > 2.0 ? _phaseManager.FenixCabinTempFwd : 18.0 + (_phaseManager.FenixCabinTempFwd * 12.0);
                double mappedTempAft = _phaseManager.FenixCabinTempAft > 2.0 ? _phaseManager.FenixCabinTempAft : 18.0 + (_phaseManager.FenixCabinTempAft * 12.0);

                if (_phaseManager.FenixCabinTempFwd > 0.01 || _phaseManager.FenixCabinTempAft > 0.01) 
                {
                    _hasReceivedFenixLvars = true; // Latch so we know the data link is active!
                }

                if (_hasReceivedFenixLvars) 
                {
                    currentCabinTemp = (mappedTempFwd + mappedTempAft) / 2.0;
                }
                else if (Math.Abs(_cabinManager.CurrentAmbientTemperature - 15.0) > 0.1)
                {
                    currentCabinTemp = _cabinManager.CurrentAmbientTemperature;
                }

                _cabinManager.Tick(_phaseManager.GForce, _lastKnownBank, isBoardingComplete,
                                   _currentSimTime, sobtDate, _phaseManager.CurrentPhase, _lastKnownGroundSpeed,
                                   _lastKnownAltitude, _lastKnownVerticalSpeed,
                                   _phaseManager.IsGoAroundActive || _phaseManager.IsSevereTurbulenceActive || _phaseManager.HasEngineFailure,
                                   currentCabinTemp, boardingProg);
                
                SendTelemetryToWeb();
                
                if (_panelServer != null)
                {
                    _panelServer.BroadcastData(new { 
                        score = _scoreManager.CurrentScore.ToString(), 
                        phase = _phaseManager.GetLocalizedPhaseName(), 
                        groundOps = _groundOpsManager.GetStatusString() 
                    });
                }
            };
            _uiTimer.Start();

            _phaseManager = new FlightPhaseManager();
            _phaseManager.OnPhaseChanged += phase => {
                Dispatcher.Invoke(() => {
                    if (phase == FlightPhase.Pushback || phase == FlightPhase.TaxiOut)
                    {
                        if (_aobt == null && _currentSimTime != DateTime.MinValue) 
                            _aobt = _currentSimTime;
                    }
                    else if (phase == FlightPhase.Takeoff)
                    {
                        if (_cabinManager.State != CabinState.TakeoffSecured || !_cabinManager.IsCrewSeated)
                        {
                            if (_scoreManager != null) _scoreManager.AddScore(-200, "SÉCURITÉ: Décollage sans cabine sécurisée !", ScoreCategory.Safety);
                        }
                    }
                    else if (phase == FlightPhase.Landing)
                    {
                        if (_cabinManager.State != CabinState.LandingSecured || !_cabinManager.IsCrewSeated)
                        {
                            if (_scoreManager != null) _scoreManager.AddScore(-200, "SÉCURITÉ: Atterrissage sans cabine sécurisée !", ScoreCategory.Safety);
                        }
                    }
                    else if (phase == FlightPhase.Arrived)
                    {
                        _cabinManager.SessionFlightsCompleted++;
                        if (_aibt == null && _currentSimTime != DateTime.MinValue) 
                            _aibt = _currentSimTime;

                        long sibtUnix = 0;
                        long rawDelaySec = 0;
                        long effectiveDelaySec = 0;
                        
                        if (_currentResponse?.Times?.SchedIn != null && long.TryParse(_currentResponse.Times.SchedIn, out sibtUnix))
                        {
                            long aibtUnix = ((DateTimeOffset)_currentSimTime).ToUnixTimeSeconds();
                            rawDelaySec = aibtUnix - sibtUnix;
                            
                            int groundOpsDelaySec = _groundOpsManager.Services.Sum(s => s.DelayAddedSec);
                            effectiveDelaySec = rawDelaySec - groundOpsDelaySec;

                            // Dynamic delay tolerance based on airline punctuality (1=30m, 10=3m)
                            int targetPunctuality = (CurrentAirline != null) ? CurrentAirline.PunctualityPriority : 5;
                            int delayToleranceSec = Math.Max(180, (11 - targetPunctuality) * 180); // e.g. 10 -> 1*180 = 180s = 3m, 1 -> 10*180 = 1800s = 30m

                            if (effectiveDelaySec > delayToleranceSec) // Pilot delay exceeds airline specific tolerance
                            {
                                int excess = (int)(effectiveDelaySec - delayToleranceSec);
                                int penalty = excess > 600 ? -100 : -50; // -100 if more than 10 mins beyond tolerance
                                string timeStr = $"{(rawDelaySec / 60)} min";
                                string groundOpsPardon = groundOpsDelaySec > 0 ? $" (Amnistie Sol: -{groundOpsDelaySec / 60}m)" : "";
                                _scoreManager.AddScore(penalty, $"Retard à l'arrivée (Tolérance dépassée): {timeStr}{groundOpsPardon}", ScoreCategory.Operations);
                            }
                            else if (effectiveDelaySec <= delayToleranceSec && rawDelaySec > delayToleranceSec)
                            {
                                _scoreManager.AddScore(50, $"Amnistie Retard : {rawDelaySec / 60}m justifiés par les Ops Sol !", ScoreCategory.Operations);
                            }
                            else if (rawDelaySec <= delayToleranceSec)
                            {
                                _scoreManager.AddScore(100, $"Ponctualité : Arrivée dans les temps (Tolérance de {delayToleranceSec / 60}m respectée) !", ScoreCategory.Operations);
                            }
                        }

                        // Generate Flight Report
                        long schedOut = 0;
                        if (_currentResponse?.Times?.SchedOut != null) long.TryParse(_currentResponse.Times.SchedOut, out schedOut);
                            
                        // Apply final customer dissatisfaction penalty if Comfort is terrible
                        if (_scoreManager.ComfortPoints < -200)
                        {
                            int dissatPenalty = _scoreManager.ComfortPoints / 2; // e.g., -300 comfort -> -150 Operations
                            _scoreManager.AddScore(dissatPenalty, "Customer Dissatisfaction: Passenger complaints filed", ScoreCategory.Operations);
                        }

                        // Cabin Arrived Check
                        _cabinManager.CheckLostBaggageOnArrival();

                        bool cateringPerformed = _groundOpsManager.Services.Any(s => s.Name.Equals("Catering", StringComparison.OrdinalIgnoreCase) && s.State == GroundServiceState.Completed);
                        bool anySkipped = _groundOpsManager.Services.Any(s => s.State == GroundServiceState.Skipped);
                        
                        // Apply Prestige Modifier
                        if (CurrentAirline != null)
                        {
                            double prestigeMod = CurrentAirline.GlobalScore / 100.0;
                            int finalScore = (int)(_scoreManager.CurrentScore * prestigeMod);
                            int prestigeDiff = finalScore - _scoreManager.CurrentScore;
                            if (prestigeDiff != 0)
                            {
                                string msg = prestigeDiff > 0 ? $"Bonus Prestige Compagnie ({CurrentAirline.GlobalScore}%)" : $"Malus Expérience Compagnie ({CurrentAirline.GlobalScore}%)";
                                _scoreManager.AddScore(prestigeDiff, msg, ScoreCategory.Operations);
                            }
                        }

                        int blockMins = _aobt.HasValue && _aibt.HasValue ? (int)(_aibt.Value - _aobt.Value).TotalMinutes : 0;
                        
                        // UPDATE PILOT PROFILE STATS
                        if (_profileManager != null && _profileManager.CurrentProfile != null)
                        {
                            var p = _profileManager.CurrentProfile;
                            p.TotalFlights++;
                            p.TotalBlockTimeMinutes += blockMins;
                            
                            double currentDist = 0;
                            if (_currentResponse?.General?.RouteDistance != null && double.TryParse(_currentResponse.General.RouteDistance, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out currentDist))
                                p.TotalDistanceFlownNM += currentDist;

                            p.PassengersTransported += int.TryParse(_currentResponse?.Weights?.PaxCount, out int pax) ? pax : 0;
                            
                            if (p.HighestSuperScore < _scoreManager.CurrentScore) p.HighestSuperScore = _scoreManager.CurrentScore;
                            
                            // Running average for SuperScore using Welford's approx or just simple cumulative for now
                            p.AverageSuperScore = p.TotalFlights == 1 ? _scoreManager.CurrentScore : ((p.AverageSuperScore * (p.TotalFlights - 1)) + _scoreManager.CurrentScore) / p.TotalFlights;

                            p.AverageDelayMinutes = p.TotalFlights == 1 ? (effectiveDelaySec / 60.0) : ((p.AverageDelayMinutes * (p.TotalFlights - 1)) + (effectiveDelaySec / 60.0)) / p.TotalFlights;
                            
                            // Punctuality rating (Percentage of flights with <= 0 delay)
                            int onTimeCount = (int)((p.PunctualityRatingPercentage / 100.0) * (p.TotalFlights - 1));
                            if (effectiveDelaySec <= 0) onTimeCount++;
                            p.PunctualityRatingPercentage = (onTimeCount / (double)p.TotalFlights) * 100.0;
                            
                            if (p.SmoothestTouchdownFpm == 0 || _phaseManager.TouchdownFpm > p.SmoothestTouchdownFpm) p.SmoothestTouchdownFpm = _phaseManager.TouchdownFpm;
                            if (p.HardestImpactFpm == 0 || _phaseManager.TouchdownFpm < p.HardestImpactFpm) p.HardestImpactFpm = _phaseManager.TouchdownFpm;
                            
                            // Fake Manual Flying Time tracking for now (just take 5% of block time or 10 mins)
                            int manualMins = Math.Min(blockMins / 5, 15);
                            p.ManualFlyingTimeMinutes += manualMins;

                            // Evaluate Achievements
                            var engine = new FlightSupervisor.UI.Services.AchievementEngine();
                            bool isNight = _currentSimTime.Hour > 20 || _currentSimTime.Hour < 6;
                            bool landingLightsOff = !_phaseManager.IsLandingLightOn;
                            
                            var newBadges = engine.EvaluateFlightEnd(p, 
                                _scoreManager.CurrentScore, 
                                _scoreManager.SafetyPoints, 
                                effectiveDelaySec, 
                                manualMins, 
                                _phaseManager.TouchdownFpm, 
                                0, // crosswind
                                _scoreManager.SafetyPoints < 1000, 
                                anySkipped, 
                                !cateringPerformed, 
                                _scoreManager.ComfortPoints, 
                                isNight && landingLightsOff, 
                                false, // go around
                                blockMins, 
                                true);

                            p.LastFlightDate = DateTime.Now;
                            _profileManager.SaveProfile();

                            var report = new FlightSupervisor.UI.Models.FlightArchive
                            {
                                Score = _scoreManager.CurrentScore,
                                SafetyPoints = _scoreManager.SafetyPoints,
                                ComfortPoints = _scoreManager.ComfortPoints,
                                MaintenancePoints = _scoreManager.MaintenancePoints,
                                OperationsPoints = _scoreManager.OperationsPoints,
                                FlightEvents = _scoreManager.FlightEvents.Cast<object>().ToList(),
                                Objectives = new List<object>(), // Retired Legacy Contract System
                                NewAchievements = newBadges,
                                Dep = _currentResponse?.Origin?.IcaoCode ?? "N/A",
                                Arr = _currentResponse?.Destination?.IcaoCode ?? "N/A",
                                FlightNo = _currentResponse?.General?.FlightNumber ?? "UNK",
                                Airline = _currentResponse?.General?.Airline ?? "",
                                BlockTime = blockMins,
                                SchedBlockTime = schedOut > 0 && sibtUnix > 0 ? (sibtUnix - schedOut) / 60 : 0,
                                TouchdownFpm = _phaseManager.TouchdownFpm,
                                TouchdownGForce = _phaseManager.TouchdownGForce,
                                Zfw = _currentResponse?.Weights?.EstZfw ?? "0",
                                Tow = _currentResponse?.Weights?.EstTow ?? "0",
                                BlockFuel = _currentResponse?.Fuel?.PlanRamp ?? "0",
                                DelaySec = effectiveDelaySec,
                                RawDelaySec = rawDelaySec
                            };

                            FlightSupervisor.UI.Services.FlightLogger.ArchiveFlight(report);
                            SendToWeb(new { type = "flightReport", report });
                            
                            // Send updated profile to UI immediately to reflect new stats
                            SendToWeb(new { type = "InitProfile", payload = p });
                        }
                        else
                        {
                            var fallbackReport = new FlightSupervisor.UI.Models.FlightArchive
                            {
                                Score = _scoreManager.CurrentScore,
                                SafetyPoints = _scoreManager.SafetyPoints,
                                ComfortPoints = _scoreManager.ComfortPoints,
                                MaintenancePoints = _scoreManager.MaintenancePoints,
                                OperationsPoints = _scoreManager.OperationsPoints,
                                FlightEvents = _scoreManager.FlightEvents.Cast<object>().ToList(),
                                Objectives = new List<object>(),
                                NewAchievements = new List<FlightSupervisor.UI.Services.BadgeDefinition>(),
                                Dep = _currentResponse?.Origin?.IcaoCode ?? "N/A",
                                Arr = _currentResponse?.Destination?.IcaoCode ?? "N/A",
                                FlightNo = _currentResponse?.General?.FlightNumber ?? "UNK",
                                Airline = _currentResponse?.General?.Airline ?? "",
                                BlockTime = blockMins,
                                SchedBlockTime = schedOut > 0 && sibtUnix > 0 ? (sibtUnix - schedOut) / 60 : 0,
                                TouchdownFpm = _phaseManager.TouchdownFpm,
                                TouchdownGForce = _phaseManager.TouchdownGForce,
                                Zfw = _currentResponse?.Weights?.EstZfw ?? "0",
                                Tow = _currentResponse?.Weights?.EstTow ?? "0",
                                BlockFuel = _currentResponse?.Fuel?.PlanRamp ?? "0",
                                DelaySec = effectiveDelaySec,
                                RawDelaySec = rawDelaySec
                            };

                            FlightSupervisor.UI.Services.FlightLogger.ArchiveFlight(fallbackReport);
                            SendToWeb(new { type = "flightReport", report = fallbackReport });
                        }

                        // Persist multi-leg session state
                        string arrIcao = _currentResponse?.Destination?.IcaoCode ?? "UNK";
                        string arrAirline = _currentResponse?.General?.Airline ?? "UNK";
                        ShiftStateManager.SaveState(_cabinManager, arrIcao, arrAirline);
                    }
                    SendToWeb(new { type = "phaseUpdate", phase = phase.ToString(), 
                                    aobt = _aobt != null ? _aobt.Value.ToString("HH:mm") + "z" : null, 
                                    aibt = _aibt != null ? _aibt.Value.ToString("HH:mm") + "z" : null,
                                    aobtUnix = _aobt != null ? new DateTimeOffset(_aobt.Value).ToUnixTimeSeconds() : (long?)null,
                                    aibtUnix = _aibt != null ? new DateTimeOffset(_aibt.Value).ToUnixTimeSeconds() : (long?)null });
                });
            };
            _phaseManager.OnPenaltyTriggered += msg => {
                Dispatcher.Invoke(() => SendToWeb(new { type = "penalty", message = msg }));
            };



            _simConnectService = new SimConnectService();
            
            _crisisManager = new CrisisManager(_phaseManager, _simConnectService);
            _crisisManager.OnCrisisTriggered += crisis => {
                Dispatcher.Invoke(() => SendToWeb(new { type = "crisisTriggered", crisisType = crisis.ToString() }));
            };
            
            _crisisManager.OnCabinMessage += (level, msg) => {
                Dispatcher.Invoke(() => SendToWeb(new { type = "cabinLog", level = level, message = msg }));
            };
            _crisisManager.OnCrisisResolved += (crisis, success, isManual) => {
                if (!isManual && crisis == CrisisType.MedicalEmergency)
                {
                    _scoreManager.AddScore(-500, "Medical Emergency Ignored", ScoreCategory.Safety);
                    Dispatcher.Invoke(() => SendToWeb(new { type = "cabinLog", level = "red", message = "[PNC] We had a medical emergency and you ignored it. The passenger is in critical condition." }));
                }
                else if (!isManual && crisis == CrisisType.UnrulyPassenger)
                {
                    _scoreManager.AddScore(-200, "Unruly Passenger Ignored", ScoreCategory.Safety);
                    _cabinManager.ApplyComfortImpact(-50);
                    Dispatcher.Invoke(() => SendToWeb(new { type = "cabinLog", level = "red", message = "[PNC] The unruly passenger escalated because you did nothing. Cabin comfort is plummeting." }));
                }
                else if (crisis == CrisisType.Depressurization)
                {
                    if (success)
                    {
                        _scoreManager.AddScore(250, "Emergency Descent Executed", ScoreCategory.Safety);
                        Dispatcher.Invoke(() => SendToWeb(new { type = "cabinLog", level = "cyan", message = "[PNC] We are below 10,000 ft. Passengers are breathing normally again. Great job." }));
                    }
                    else
                    {
                        _scoreManager.AddScore(-1000, "Depressurization Ignored", ScoreCategory.Safety);
                        _cabinManager.ApplyComfortImpact(-100);
                        Dispatcher.Invoke(() => SendToWeb(new { type = "cabinLog", level = "red", message = "[CRITICAL] Oxygen masks are depleted! Passengers are losing consciousness!" }));
                    }
                }
                Dispatcher.Invoke(() => SendToWeb(new { type = "crisisResolved", crisisType = crisis.ToString(), success = success }));
            };

            _scoreManager = new SuperScoreManager(_phaseManager, _simConnectService);
            _scoreManager.OnScoreChanged += (score, delta, reason) => {
                Dispatcher.Invoke(() => SendToWeb(new { type = "scoreUpdate", score = score, delta = delta, msg = reason }));
            };

            _cabinManager.OnOperationBonusTriggered += (amount, msg) => {
                _scoreManager.AddScore(amount, msg, ScoreCategory.Operations);
            };

            _simConnectService.OnConnectionStateChanged += state => {
                Dispatcher.Invoke(() => SendToWeb(new { type = "simConnectStatus", status = state }));
            };
            _simConnectService.OnAltitudeReceived += alt => {
                _lastKnownAltitude = alt;
                _phaseManager.UpdateTelemetry(_lastKnownGroundSpeed, _lastKnownAirspeed, alt, _lastKnownRadioHeight, _isParkingBrakeSet, _isGearDown, _lastKnownThrottle, _lastKnownPitch, _lastKnownBank);
            };
            _simConnectService.OnVerticalSpeedReceived += vs => {
                _lastKnownVerticalSpeed = vs;
            };
            _simConnectService.OnGearDownReceived += gd => { 
                _isGearDown = gd; 
                if (_lastLogGearDown != null && _lastLogGearDown != gd) SendToWeb(new { type = "log", message = gd ? "Landing Gear DOWN" : "Landing Gear UP" });
                _lastLogGearDown = gd;
            };
            _simConnectService.OnRadioHeightReceived += rh => { _lastKnownRadioHeight = rh; };
            _simConnectService.OnGroundSpeedReceived += gs => { _lastKnownGroundSpeed = gs; };
            _simConnectService.OnAirspeedReceived += ias => { _lastKnownAirspeed = ias; };
            _simConnectService.OnParkingBrakeReceived += pb => { 
                _isParkingBrakeSet = pb; 
                if (_lastLogParkingBrake != null && _lastLogParkingBrake != pb) SendToWeb(new { type = "log", message = pb ? "Parking Brake SET" : "Parking Brake RELEASED" });
                _lastLogParkingBrake = pb;
            };
            _simConnectService.OnFlapsReceived += flaps => {
                if (_lastLogFlaps != null && _lastLogFlaps != flaps) SendToWeb(new { type = "log", message = $"Flaps Position Changed -> {flaps}" });
                _lastLogFlaps = flaps;
            };
            _simConnectService.OnAutopilotReceived += ap => {
                if (_lastLogAutopilot != null && _lastLogAutopilot != ap) SendToWeb(new { type = "log", message = ap ? "Autopilot ENGAGED" : "Autopilot DISENGAGED" });
                _lastLogAutopilot = ap;
                _phaseManager.UpdateAutopilot(ap);
            };
            _simConnectService.OnAutothrustReceived += at => {
                if (_lastLogAutothrust != null && _lastLogAutothrust != at) SendToWeb(new { type = "log", message = at ? "Autothrust ARMED" : "Autothrust DISENGAGED" });
                _lastLogAutothrust = at;
                _phaseManager.UpdateAutothrust(at);
            };
            _simConnectService.OnThrottleReceived += thr => {
                _lastKnownThrottle = thr;
                if (_lastLogThrottle != null && Math.Abs(_lastLogThrottle.Value - thr) > 5.0) 
                    SendToWeb(new { type = "log", message = $"Throttle ENG 1: {thr:F0}%" });
                _lastLogThrottle = thr;
            };
            _simConnectService.OnSpoilersReceived += spl => {
                if (_lastLogSpoilers != null && Math.Abs(_lastLogSpoilers.Value - spl) > 5.0) 
                    SendToWeb(new { type = "log", message = $"Spoilers Handle: {spl:F0}%" });
                _lastLogSpoilers = spl;
            };
            _simConnectService.OnLightBeaconReceived += l => { if (_lastLogLightBeacon != null && _lastLogLightBeacon != l) SendToWeb(new { type = "log", message = l ? "Beacon Lights ON" : "Beacon Lights OFF" }); _lastLogLightBeacon = l; };
            _simConnectService.OnLightStrobeReceived += l => { _phaseManager.IsStrobeLightOn = l; if (_lastLogLightStrobe != null && _lastLogLightStrobe != l) SendToWeb(new { type = "log", message = l ? "Strobe Lights ON" : "Strobe Lights OFF" }); _lastLogLightStrobe = l; };
            _simConnectService.OnLightNavReceived += l => { if (_lastLogLightNav != null && _lastLogLightNav != l) SendToWeb(new { type = "log", message = l ? "Nav Lights ON" : "Nav Lights OFF" }); _lastLogLightNav = l; };
            _simConnectService.OnLightTaxiReceived += l => { 
                _phaseManager.IsTaxiLightOn = l;
                if (_lastLogLightTaxi != null && _lastLogLightTaxi != l) SendToWeb(new { type = "log", message = l ? "Taxi Lights ON" : "Taxi Lights OFF" }); 
                _lastLogLightTaxi = l; 
            };
            _simConnectService.OnSimOnGroundReceived += g => { _phaseManager.IsOnGround = g; };
            _simConnectService.OnVerticalSpeedReceived += vs => { _phaseManager.VerticalSpeed = vs; };
            _simConnectService.OnGForceReceived += gf => { _phaseManager.GForce = gf; };
            _simConnectService.OnHeadingReceived += h => { _phaseManager.UpdateHeading(h); };
            _simConnectService.OnWindReceived += (wd, wv) => { _phaseManager.UpdateWind(wd, wv); };
            _simConnectService.OnNavigationReceived += (locErr, gpsErr, hasLoc) => { _phaseManager.UpdateNavigation(locErr, gpsErr, hasLoc); };
            _simConnectService.OnEngineCombustionReceived += (eng1, eng2) => {
                _phaseManager.UpdateEngineCombustion(eng1, eng2);
            };
            _simConnectService.OnLightLandingReceived += l => { 
                _phaseManager.IsLandingLightOn = l;
                if (_lastLogLightLanding != null && _lastLogLightLanding != l) SendToWeb(new { type = "log", message = l ? "Landing Lights ON" : "Landing Lights OFF" }); 
                _lastLogLightLanding = l; 
            };
            _simConnectService.OnPitchReceived += p => { _lastKnownPitch = p; };
            _simConnectService.OnBankReceived += b => { _lastKnownBank = b; };

            // Unified Telemetry Hooks (Handled by Overrider)
            _simConnectService.OnCabinSeatbeltsChanged += sb => { 
                _cabinManager.UpdateSeatbelts(sb, _phaseManager.CurrentPhase); 
                _phaseManager.IsSeatbeltsOn = sb;

                if (sb && _groundOpsManager != null)
                {
                    var refSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Refueling");
                    if (refSvc != null && refSvc.State == GroundServiceState.InProgress && !_cabinManager.HasPenalizedRefuelingSeatbelts)
                    {
                        _cabinManager.TriggerRefuelingSeatbeltPenalty();
                    }
                }

                if (_lastLogSeatbelts != null && _lastLogSeatbelts != sb) SendToWeb(new { type = "log", message = sb ? "Fasten Seatbelts ON" : "Fasten Seatbelts OFF" });
                _lastLogSeatbelts = sb;
            };
            _simConnectService.OnApuStateChanged += (mst, start, bleed) => {
                _phaseManager.FenixApuMaster = mst;
                _phaseManager.FenixApuStart = start;
                _phaseManager.FenixApuBleed = bleed;
                
                if (_lastLogApuMaster != null && _lastLogApuMaster != mst) SendToWeb(new { type = "log", message = mst ? "APU Master SW ON" : "APU Master SW OFF" });
                if (_lastLogApuStart != null && _lastLogApuStart != start) SendToWeb(new { type = "log", message = start ? "APU Start SW ON" : "APU Start SW OFF" });
                if (_lastLogApuBleed != null && _lastLogApuBleed != bleed) SendToWeb(new { type = "log", message = bleed ? "APU Bleed SW ON" : "APU Bleed SW OFF" });
                
                _lastLogApuMaster = mst;
                _lastLogApuStart = start;
                _lastLogApuBleed = bleed;
            };
            
            _simConnectService.OnPacksChanged += (pack1, pack2) => {
                _phaseManager.FenixPack1 = pack1;
                _phaseManager.FenixPack2 = pack2;
                
                if (_lastLogPack1 != null && _lastLogPack1 != pack1) SendToWeb(new { type = "log", message = pack1 ? "PACK 1 ON" : "PACK 1 OFF" });
                if (_lastLogPack2 != null && _lastLogPack2 != pack2) SendToWeb(new { type = "log", message = pack2 ? "PACK 2 ON" : "PACK 2 OFF" });
                
                _lastLogPack1 = pack1;
                _lastLogPack2 = pack2;
            };

            _simConnectService.OnCabinTemperatureTargetsChanged += (ckpt, fwd, aft) => {
                _phaseManager.FenixCabinTempCockpit = ckpt;
                _phaseManager.FenixCabinTempFwd = fwd;
                _phaseManager.FenixCabinTempAft = aft;
                
                if (_lastLogTempCkpt != null && Math.Abs(_lastLogTempCkpt.Value - ckpt) > 0.05f) SendToWeb(new { type = "log", message = $"CKPT Temp Tgt: {ckpt:F2}" });
                if (_lastLogTempFwd != null && Math.Abs(_lastLogTempFwd.Value - fwd) > 0.05f) SendToWeb(new { type = "log", message = $"FWD Temp Tgt: {fwd:F2}" });
                if (_lastLogTempAft != null && Math.Abs(_lastLogTempAft.Value - aft) > 0.05f) SendToWeb(new { type = "log", message = $"AFT Temp Tgt: {aft:F2}" });
                
                _lastLogTempCkpt = ckpt;
                _lastLogTempFwd = fwd;
                _lastLogTempAft = aft;
            };


            int? _lastLogNoseLight = null;
            _simConnectService.OnNoseLightChanged += nl => { 
                _phaseManager.FenixNoseLight = nl;
                if (_lastLogNoseLight != null && _lastLogNoseLight != nl)
                {
                    string state = nl == 0 ? "OFF" : (nl == 1 ? "TAXI" : "TAKE-OFF");
                    SendToWeb(new { type = "log", message = $"Nose Light {state} (Aircraft)" });
                }
                _lastLogNoseLight = nl;
            };
            _simConnectService.OnRunwayTurnoffChanged += rwy => { _phaseManager.IsRunwayTurnoffLightOn = rwy; };

            _simConnectService.OnSimTimeReceived += time => { 
                _currentSimTime = time;
                _cabinManager.CurrentSimZuluTime = time;
                Dispatcher.Invoke(() => SendToWeb(new { type = "simTime", time = time.ToString("HH:mm") + "z", rawUnix = ((DateTimeOffset)time).ToUnixTimeSeconds() }));
            };

            _simConnectService.OnSimLocalTimeReceived += localTime => {
                _cabinManager.CurrentSimLocalTime = localTime;
            };

            _simConnectService.OnAmbientTemperatureReceived += temp => {
                _cabinManager.CurrentAmbientTemperature = temp;
            };

            _simConnectService.OnFuelTotalReceived += fuel => {
                _currentFobKg = fuel;
            };

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
            
            InitializeWebViewAsync();
        }

        private async void InitializeWebViewAsync()
        {
            var env = await CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FlightSupervisorWebView"));
            await MainWebView.EnsureCoreWebView2Async(env);
            
            MainWebView.CoreWebView2.SetVirtualHostNameToFolderMapping("app.local", "wwwroot", CoreWebView2HostResourceAccessKind.Allow);
            MainWebView.CoreWebView2.SetVirtualHostNameToFolderMapping("fsv.local", AppDomain.CurrentDomain.BaseDirectory, CoreWebView2HostResourceAccessKind.Allow);
            MainWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            MainWebView.CoreWebView2.Navigate("https://app.local/index.html");

            // Navigation completed is now handled securely inside uiReady IPC message
        }

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private async void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try 
            {
                var msg = e.WebMessageAsJson;
                var doc = JsonDocument.Parse(msg);
                var action = doc.RootElement.GetProperty("action").GetString();
                
                if (action == "drag") 
                {
                    Dispatcher.Invoke(() => {
                        var helper = new WindowInteropHelper(this);
                        ReleaseCapture();
                        SendMessage(helper.Handle, 0xA1, 2, 0);
                    });
                }
                else if (action == "openSimbriefWindow")
                {
                    try
                    {
                        var simbriefWin = new Window
                        {
                            Title = "SimBrief Dispatch (Navigraph)",
                            Width = 1200,
                            Height = 900,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = this,
                            Background = System.Windows.Media.Brushes.Black
                        };
                        simbriefWin.Closed += (s, e) => {
                            SendToWeb(new { type = "simbriefWindowClosed" });
                        };

                        var webView = new Microsoft.Web.WebView2.Wpf.WebView2();
                        simbriefWin.Content = webView;
                        simbriefWin.Show();

                        // Fire and forget initialization to avoid blocking the message loop
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await Dispatcher.InvokeAsync(async () =>
                            {
                                await webView.EnsureCoreWebView2Async(MainWebView.CoreWebView2.Environment);
                                webView.SourceChanged += (s, e) =>
                                {
                                    if (webView.Source != null && (webView.Source.ToString().Contains("dispatch.simbrief.com/options/briefing") || webView.Source.ToString().Contains("/briefing")))
                                    {
                                        simbriefWin.Close();
                                    }
                                };
                                webView.Source = new System.Uri("https://dispatch.simbrief.com/options/custom");
                            });
                        });
                    }
                    catch (System.Exception ex)
                    {
                        System.Windows.MessageBox.Show("Error opening Simbrief: " + ex.Message);
                    }
                }
                else if (action == "cancelLastLeg")
                {
                    if (_rotationQueue.Count > 0)
                    {
                        var list = _rotationQueue.ToList();
                        list.RemoveAt(list.Count - 1);
                        _rotationQueue = new System.Collections.Generic.Queue<SimBriefResponse>(list);
                    }
                }
                else if (action == "intercomQuery")
                {
                    bool canReport = _cabinManager.RequestCabinReport(_phaseManager.CurrentPhase, _crisisManager?.ActiveCrisis != CrisisType.None);
                    if (!canReport) 
                    {
                        SendToWeb(new { type = "playSound", sound = "intercom_busy" });
                    }
                    else 
                    {
                        SendToWeb(new { type = "playSound", sound = "intercom_ding" });
                    }
                }
                else if (action == "toggleService")
                {
                    _cabinManager.ToggleServiceInterruption();
                }
                else if (action == "updateAvatar")
                {
                    var payloadStr = doc.RootElement.GetProperty("payload").GetString();
                    System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProfileAvatar.b64"), payloadStr);
                }
                else if (action == "updateProfileField")
                {
                    if (_profileManager != null && _profileManager.CurrentProfile != null)
                    {
                        var field = doc.RootElement.GetProperty("field").GetString();
                        var val = doc.RootElement.GetProperty("value").GetString();
                        switch (field)
                        {
                            case "CallSign": _profileManager.CurrentProfile.CallSign = val; break;
                            case "AvatarPosition": _profileManager.CurrentProfile.AvatarPosition = val; break;
                            case "FullName": 
                                var parts = val.Split(' ');
                                _profileManager.CurrentProfile.FirstName = parts.Length > 0 ? parts[0] : val;
                                _profileManager.CurrentProfile.LastName = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "";
                                break;
                            case "HomeBaseIcao": _profileManager.CurrentProfile.HomeBaseIcao = val; break;
                            case "CountryCode": _profileManager.CurrentProfile.CountryCode = val; break;
                        }
                        _profileManager.SaveProfile();
                    }
                }

                else if (action == "fetchLogbook")
                {
                    var history = FlightSupervisor.UI.Services.FlightLogger.GetLogbook();
                    SendToWeb(new { type = "logbookData", history });
                }
                else if (action == "resolveCrisis")
                {
                    var crisisTypeStr = doc.RootElement.GetProperty("crisisType").GetString();
                    if (_crisisManager.ActiveCrisis.ToString() == crisisTypeStr)
                    {
                        var elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _crisisManager.CrisisStartTimeSeconds;
                        bool success = false;
                        if (crisisTypeStr == "MedicalEmergency")
                        {
                            success = elapsed < 300; // Resolved under 5 minutes
                            _scoreManager.AddScore(success ? 150 : -500, success ? "Medical Emergency Handled Promptly" : "Medical Emergency Handled Too Late", ScoreCategory.Safety);
                            SendToWeb(new { type = "cabinLog", level = success ? "cyan" : "red", message = success ? "[PNC] We've secured a doctor on board! The passenger has been stabilized." : "[PNC] It took too long, the passenger's condition has severely worsened." });
                            _crisisManager.ResolveCrisis(success);
                        }
                        else if (crisisTypeStr == "UnrulyPassenger")
                        {
                            success = elapsed < 300;
                            _scoreManager.AddScore(success ? 100 : -300, success ? "Unruly Passenger Restrained" : "Unruly Passenger Ignored", ScoreCategory.Safety);
                            if (!success) _cabinManager.ApplyComfortImpact(-50);
                            SendToWeb(new { type = "cabinLog", level = success ? "cyan" : "red", message = success ? "[PNC] Passenger successfully restrained. Thanks for the quick PA command." : "[PNC] The situation escalated into a full brawl because of no captain intervention! Total chaos in the cabin." });
                            _crisisManager.ResolveCrisis(success);
                        }
                    }
                }
                else if (action == "resolveGroundEvent")
                {
                    var eventId = doc.RootElement.GetProperty("eventId").GetString() ?? "";
                    var choiceId = doc.RootElement.GetProperty("choiceId").GetString() ?? "";
                    
                    var evt = _eventEngine.GetEventById(eventId);
                    if (evt != null)
                    {
                        var choice = evt.Choices.Find(c => c.Id == choiceId);
                        if (choice != null)
                        {
                            // Immersive feedback
                            string feedbackText = string.IsNullOrEmpty(choice.ResponseLog) 
                                ? $"[RAMP] Décision Cdt reçue pour '{evt.Title}'. On s'en occupe... en espérant que ça n'ait pas de mauvaises répercussions." 
                                : $"[RAMP] {choice.ResponseLog}";
                                
                            SendToWeb(new { type = "cabinLog", level = "cyan", message = feedbackText });

                            // Apply Impacts
                            if (choice.DelayImpactSec > 0)
                            {
                                // Delay the targeted SOBT
                                foreach(var srv in _groundOpsManager.Services)
                                {
                                    if (srv.State != GroundServiceState.Completed && srv.State != GroundServiceState.Skipped)
                                    {
                                        srv.DelayAddedSec += choice.DelayImpactSec;
                                    }
                                }
                                SendToWeb(new { type = "cabinLog", level = "orange", message = $"[SYSTEM] Délai supplémentaire estimé suite à l'action sur le sol." });
                            }

                            if (choice.ComfortImpact != 0) _scoreManager.AddScore(choice.ComfortImpact, $"Event: {evt.Title}", ScoreCategory.Comfort);
                            if (choice.SafetyImpact != 0) _scoreManager.AddScore(choice.SafetyImpact, $"Event: {evt.Title}", ScoreCategory.Safety);
                        }
                    }

                    // Resume operations
                    _groundOpsManager.IsPaused = false;
                }
                else if (action == "startService")
                {
                    var serviceName = doc.RootElement.GetProperty("service").GetString();
                    if (!string.IsNullOrEmpty(serviceName))
                    {
                        _groundOpsManager.StartManualService(serviceName);
                    }
                }
                else if (action == "requestTimeWarp")
                {
                    _groundOpsManager.ForceCompleteAllServices();
                    if (_groundOpsManager.TargetSobt != null)
                    {
                        _simConnectService.SendTimeWarpCommand(_groundOpsManager.TargetSobt.Value);
                        SendToWeb(new { type = "cabinLog", level = "orange", message = $"[SYSTEM] Time Warp engaged. MSFS Clock synchronized to SOBT." });
                    }
                    _scoreManager.AddScore(-50, "Time Warp Convenience Used", ScoreCategory.Operations);
                }
                else if (action == "debugForcePhase")
                {
                    var targetStr = doc.RootElement.GetProperty("phase").GetString();
                    if (Enum.TryParse<FlightPhase>(targetStr, out var targetPhase))
                    {
                        int simAdvanceSec = 0;
                        if (targetPhase == FlightPhase.Takeoff) simAdvanceSec = 900; // 15 mins for taxi
                        else if (targetPhase == FlightPhase.Cruise) simAdvanceSec = 1800; // 30 mins climb
                        else if (targetPhase == FlightPhase.Descent) 
                        {
                            if (_currentResponse?.Times?.SchedBlock != null)
                            {
                                int.TryParse(_currentResponse.Times.SchedBlock, out int blockSec);
                                simAdvanceSec = blockSec - 2700; // block time minus taxi & arrival
                                if (simAdvanceSec < 0) simAdvanceSec = 1800;
                            }
                            else simAdvanceSec = 3600; // Default 1 hour
                        }
                        else if (targetPhase == FlightPhase.Arrived) simAdvanceSec = 900; // 15 taxi in

                        if (simAdvanceSec > 0)
                        {
                            _currentSimTime = _currentSimTime.AddSeconds(simAdvanceSec);
                            if (_cabinManager.CurrentSimLocalTime != DateTime.MinValue)
                                _cabinManager.CurrentSimLocalTime = _cabinManager.CurrentSimLocalTime.AddSeconds(simAdvanceSec);
                            
                            // Let the CabinManager consume resources
                            _cabinManager.FastForward(simAdvanceSec, targetPhase);
                            
                            SendToWeb(new { type = "cabinLog", level = "orange", message = $"[DEBUG] Simulated Time Jump: +{simAdvanceSec/60}m." });
                        }

                        _phaseManager.ForcePhase(targetPhase);
                        SendToWeb(new { type = "cabinLog", level = "cyan", message = $"[DEBUG] Force transitioned phase to: {targetPhase}" });
                    }
                }
                else if (action == "uiReady")
                {
                    var saveFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor_User.txt");
                    var username = System.IO.File.Exists(saveFilePath) ? System.IO.File.ReadAllText(saveFilePath) : "";

                    if (!string.IsNullOrEmpty(username))
                        SendToWeb(new { type = "savedUsername", username = username });
                    
                    var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    SendToWeb(new { type = "appVersion", version = $"BUILD {version?.ToString(3)}" });

                    if (_profileManager != null && _profileManager.CurrentProfile != null)
                    {
                        SendToWeb(new { type = "InitProfile", payload = _profileManager.CurrentProfile });
                    }

                    // Lancer SimConnect SEULEMENT quand la page JS est prête à intercepter les messages !
                    var helper = new WindowInteropHelper(this);
                    _simConnectService.Connect(helper.Handle);

                    var savedState = ShiftStateManager.LoadState();
                    if (savedState != null)
                    {
                        SendToWeb(new { 
                            type = "shiftResumeAvailable", 
                            icao = savedState.LastArrivalIcao, 
                            airline = savedState.LastAirlineName, 
                            morale = savedState.CrewMorale, 
                            date = savedState.SavedAtLocal.ToString("g") 
                        });
                    }
                }
                else if (action == "updateProfileField")
                {
                    if (_profileManager != null && _profileManager.CurrentProfile != null)
                    {
                        string field = doc.RootElement.GetProperty("field").GetString() ?? "";
                        string value = doc.RootElement.GetProperty("value").GetString() ?? "";

                        switch (field)
                        {
                            case "FullName":
                                var parts = value.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length > 0) _profileManager.CurrentProfile.FirstName = parts[0];
                                if (parts.Length > 1) _profileManager.CurrentProfile.LastName = parts[1];
                                else _profileManager.CurrentProfile.LastName = "";
                                break;
                            case "CallSign":
                                _profileManager.CurrentProfile.CallSign = value;
                                break;
                            case "HomeBaseIcao":
                                _profileManager.CurrentProfile.HomeBaseIcao = value.ToUpper();
                                break;
                            case "CountryCode":
                                _profileManager.CurrentProfile.CountryCode = value.ToUpper();
                                break;
                            case "AvatarPosition":
                                _profileManager.CurrentProfile.AvatarPosition = value;
                                break;
                        }
                        _profileManager.SaveProfile();
                    }
                }
                else if (action == "resumeShift")
                {
                    var state = ShiftStateManager.LoadState();
                    if (state != null)
                    {
                        _cabinManager.LoadShiftState(state);
                        SendToWeb(new { type = "shiftResumed" });
                    }
                }
                else if (action == "clearShift")
                {
                    ShiftStateManager.ClearState();
                    SendToWeb(new { type = "shiftCleared" });
                }
                else if (action == "requestAcarsUpdate")
                {
                    _ = RefreshLiveWeatherAsync();
                    SendToWeb(new { type = "cabinLog", level = "cyan", message = "[ACARS] Requested Manual Weather Update via SATCOM." });
                }
                else if (action == "fetch") 
                {
                    var username = doc.RootElement.GetProperty("username").GetString() ?? "";
                    var remember = doc.RootElement.GetProperty("remember").GetBoolean();
                    
                    var weatherSource = "simbrief";
                    if (doc.RootElement.TryGetProperty("weatherSource", out var wsProp))
                        weatherSource = wsProp.GetString() ?? "simbrief";

                    var prof = _profileManager.CurrentProfile;
                    prof.WeatherSource = weatherSource;
                    _profileManager.SaveProfile();
                    
                    if (doc.RootElement.TryGetProperty("groundSpeed", out var gsProp))
                    {
                        if (Enum.TryParse<GroundOpsSpeed>(gsProp.GetString(), true, out var speed))
                            _groundOpsManager.SpeedSetting = speed;
                    }
                    if (doc.RootElement.TryGetProperty("groundProb", out var gpProp))
                    {
                        if (int.TryParse(gpProp.GetString(), out var prob))
                            _groundOpsManager.EventProbabilityPercent = prob;
                    }
                    if (doc.RootElement.TryGetProperty("firstFlightClean", out var ffcProp))
                    {
                        if (ffcProp.ValueKind == System.Text.Json.JsonValueKind.True)
                        {
                            _cabinManager.FirstFlightClean = true;
                            _cabinManager.SessionFlightsCompleted = 0;
                        }
                        else if (ffcProp.ValueKind == System.Text.Json.JsonValueKind.False)
                            _cabinManager.FirstFlightClean = false;
                    }
                    
                    var units = new FlightSupervisor.UI.Models.UnitPreferences();
                    if (doc.RootElement.TryGetProperty("units", out var unitsProp))
                    {
                        units.Weight = unitsProp.GetProperty("weight").GetString() ?? "LBS";
                        units.Temp = unitsProp.GetProperty("temp").GetString() ?? "C";
                        units.Alt = unitsProp.GetProperty("alt").GetString() ?? "FT";
                        units.Speed = unitsProp.GetProperty("speed").GetString() ?? "KTS";
                        units.Press = unitsProp.GetProperty("press").GetString() ?? "HPA";
                        units.Time = unitsProp.GetProperty("time").GetString() ?? "24H";
                    }
                    
                    var syncMsfsTime = false;
                    if (doc.RootElement.TryGetProperty("syncMsfsTime", out var smProp))
                        syncMsfsTime = smProp.ValueKind == System.Text.Json.JsonValueKind.True;

                    await FetchFlightPlan(username, remember, units, weatherSource, syncMsfsTime);
                }
                else if (action == "finishDispatch")
                {
                    Dispatcher.Invoke(() => {
                        if (_currentResponse == null && _rotationQueue.Count > 0)
                        {
                            LoadNextLeg();
                        }
                    });
                }
                else if (action == "acknowledgeDebrief")
                {
                    _cabinManager.StartDeboarding();
                    Dispatcher.Invoke(() => {
                        SendToWeb(new { type = "switchTab", target = "groundops" });
                    });
                }
                else if (action == "connectSim")
                {
                    Dispatcher.Invoke(() => {
                        var helper = new WindowInteropHelper(this);
                        _simConnectService.Connect(helper.Handle);
                    });
                }
                else if (action == "disconnectSim")
                {
                    Dispatcher.Invoke(() => {
                        _simConnectService.Disconnect();
                    });
                }
                else if (action == "minimizeApp")
                {
                    Dispatcher.Invoke(() => {
                        this.Hide();
                        _notifyIcon.Visible = true;
                    });
                }
                else if (action == "maximizeApp")
                {
                    Dispatcher.Invoke(() => {
                        if (this.WindowState == WindowState.Maximized)
                            this.WindowState = WindowState.Normal;
                        else
                            this.WindowState = WindowState.Maximized;
                    });
                }
                else if (action == "closeApp")
                {
                    Dispatcher.Invoke(() => {
                        if (_notifyIcon != null) _notifyIcon.Dispose();
                        System.Windows.Application.Current.Shutdown();
                    });
                }
                else if (action == "skipService")
                {
                    var srvName = doc.RootElement.GetProperty("service").GetString() ?? "";
                    if (!string.IsNullOrEmpty(srvName))
                    {
                        _groundOpsManager.SkipService(srvName!);
                    }
                }
                else if (action == "startService")
                {
                    var srvName = doc.RootElement.GetProperty("service").GetString();
                    if (!string.IsNullOrEmpty(srvName))
                    {
                        _groundOpsManager.StartManualService(srvName!);
                    }
                }
                else if (action == "warpToDeparture")
                {
                    _groundOpsManager.ForceCompleteAllServices();
                    _scoreManager.AddScore(-300, "Time Warp (Fast Travel Penalty)", ScoreCategory.Operations);
                    if (_groundOpsManager.TargetSobt.HasValue)
                    {
                        var target = _groundOpsManager.TargetSobt.Value;
                        _simConnectService.SendTimeWarpCommand(target);
                    }
                }
                else if (action == "acarsWeatherRequest")
                {
                    Dispatcher.InvokeAsync(async () => {
                        await RefreshLiveWeatherAsync();
                    });
                }
                else if (action == "startGroundOps")
                {
                    Dispatcher.Invoke(() => {
                        _groundOpsManager.StartOps();
                    });
                }
                else if (action == "setAlwaysOnTop")
                {
                    Dispatcher.Invoke(() => {
                        this.Topmost = doc.RootElement.GetProperty("value").GetBoolean();
                    });
                }
                else if (action == "setCrisisFrequency")
                {
                    if (doc.RootElement.TryGetProperty("value", out var valProp) && valProp.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        if (Enum.TryParse<CrisisFrequency>(valProp.GetString(), out var freq))
                        {
                            _crisisManager.Frequency = freq;
                            System.Diagnostics.Debug.WriteLine($"[CRISIS] Frequency configured to: {freq}");
                        }
                    }
                }
                else if (action == "announceCabin")
                {
                    var annType = doc.RootElement.GetProperty("annType").GetString();
                    if (annType != null) 
                    {
                        if (annType == "Welcome")
                        {
                            string destName = _currentResponse?.Destination?.Name ?? _currentResponse?.Destination?.IcaoCode ?? "our destination";
                            int.TryParse(_currentResponse?.Times?.EstTimeEnroute, out int timeSecs);
                            int timeMins = timeSecs / 60;
                            string timeStr = timeMins >= 60 ? $"{timeMins / 60} hour(s) and {timeMins % 60} minutes" : $"{timeMins} minutes";
                            
                            bool badWeather = false;
                            string metar = _currentResponse?.Weather?.DestMetar?.ToUpper() ?? "";
                            if (metar.Contains(" TS") || metar.Contains(" RA") || metar.Contains(" SN") || metar.Contains(" FG")) badWeather = true;

                            _cabinManager.AnnounceWelcome(destName, timeStr, badWeather);
                        }
                        else if (annType == "Approach")
                        {
                            string destName = _currentResponse?.Destination?.Name ?? _currentResponse?.Destination?.IcaoCode ?? "our destination";
                            string metar = _currentResponse?.Weather?.DestMetar?.ToUpper() ?? "";
                            string wxc = "good";
                            if (metar.Contains(" TS") || metar.Contains(" CB")) wxc = "stormy";
                            else if (metar.Contains(" RA") || metar.Contains(" SH")) wxc = "rainy";
                            else if (metar.Contains(" SN")) wxc = "snowy";
                            else if (metar.Contains(" FG") || metar.Contains(" BKN") || metar.Contains(" OVC")) wxc = "cloudy";
                            _cabinManager.AnnounceApproach(destName, wxc);
                        }
                        else if (annType == "Delay")
                        {
                            string reason = "ATC";
                            if (doc.RootElement.TryGetProperty("reason", out var reasonProperty))
                            {
                                reason = reasonProperty.GetString() ?? "ATC";
                            }
                            string destName = _currentResponse?.Destination?.Name ?? _currentResponse?.Destination?.IcaoCode ?? "our destination";
                            _cabinManager.AnnounceDelay(reason, destName);
                        }
                        else
                        {
                            _cabinManager.AnnounceToCabin(annType);
                        }
                    }
                }
                else if (action == "pncCommand")
                {
                    var cmd = doc.RootElement.GetProperty("command").GetString();
                    if (cmd != null) _cabinManager.HandleCommand(cmd);
                }
                else if (action == "cancelFlight")
                {
                    _phaseManager.Reset();
                    _scoreManager.Reset();
                    _cabinManager.Reset();
                    _currentResponse = null;
                    _groundOpsManager.AbortAllOperations();
                    _groundOpsManager.Services.Clear();
                    _aobt = null;
                    _aibt = null;
                    SendToWeb(new { type = "flightReset" });
                }
                else if (action == "acarsWeatherRequest")
                {
                    _ = RefreshLiveWeatherAsync();
                }
                else if (action == "changeLanguage")
                {
                    var lang = doc.RootElement.GetProperty("language").GetString() ?? "en";
                    FlightSupervisor.UI.Services.LocalizationService.CurrentLanguage = lang;
                    
                    if (_currentResponse != null)
                    {
                        var weatherService = new FlightSupervisor.UI.Services.WeatherBriefingService(new FlightSupervisor.UI.Models.UnitPreferences());
                        var briefingData = weatherService.GenerateBriefing(_currentResponse);
                        SendToWeb(new { type = "briefingUpdate", briefing = briefingData });
                    }
                }
                else if (action == "fenixExport")
                {
                    try
                    {
                        var path = doc.RootElement.GetProperty("path").GetString();
                        var payload = doc.RootElement.GetProperty("jsonPayload").GetString();

                        if (string.IsNullOrWhiteSpace(path))
                        {
                            MainWebView?.CoreWebView2?.ExecuteScriptAsync("alert('Please configure the Fenix Export Path in the Settings tab first.');");
                        }
                        else
                        {
                            if (!System.IO.Directory.Exists(path))
                            {
                                MainWebView?.CoreWebView2?.ExecuteScriptAsync($"alert('The directory does not exist:\\n{path.Replace("\\", "\\\\")}');");
                            }
                            else
                            {
                                string fullPath = System.IO.Path.Combine(path, "simbrief.json");
                                System.IO.File.WriteAllText(fullPath, payload);
                                MainWebView?.CoreWebView2?.ExecuteScriptAsync($"alert('Successfully exported Leg to:\\n{fullPath.Replace("\\", "\\\\")}');");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MainWebView?.CoreWebView2?.ExecuteScriptAsync($"alert('Failed to export to Fenix:\\n{ex.Message.Replace("'", "\\'").Replace("\n", " ")}');");
                    }
                }
            } catch { }
        }

        private void SendToWeb(object data)
        {
            if (MainWebView?.CoreWebView2 != null)
            {
                MainWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(data));
            }
        }

        private void OnGroundEventTriggered(FlightSupervisor.UI.Models.GroundEventDTO evt)
        {
            _groundOpsManager.IsPaused = true;
            SendToWeb(new { type = "showGroundEvent", eventData = evt });
            SendToWeb(new { type = "log", message = $"[RAMP] INCIDENT: {evt.Title}. Operations halted." });
        }

        private void SendTelemetryToWeb()
        {
            SendToWeb(new 
            {
                type = "telemetry",
                phase = _phaseManager.GetLocalizedPhaseName(),
                phaseEnum = _phaseManager.CurrentPhase.ToString(),
                altitude = _lastKnownAltitude,
                groundSpeed = _lastKnownGroundSpeed,
                radioHeight = _lastKnownRadioHeight,
                sessionFlightsCompleted = _cabinManager.SessionFlightsCompleted,
                isGearDown = _isGearDown,
                seatbeltsOn = _cabinManager.IsSeatbeltsOn,
                isDelayed = _groundOpsManager.TargetSobt != null && (_aobt != null ? _aobt.Value > _groundOpsManager.TargetSobt.Value.AddMinutes(5) : _currentSimTime > _groundOpsManager.TargetSobt.Value.AddMinutes(5)),
                isBoardingComplete = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Boarding")?.State == GroundServiceState.Completed,
                anxiety = Math.Round(_cabinManager.PassengerAnxiety, 1),
                comfort = Math.Round(_cabinManager.ComfortLevel, 1),
                satisfaction = Math.Round(_cabinManager.Satisfaction, 1),
                crewProactivity = Math.Round(_cabinManager.CrewProactivity, 1),
                crewEfficiency = Math.Round(_cabinManager.CrewEfficiency, 1),
                crewMorale = Math.Round(_cabinManager.CrewMorale, 1),
                serviceProgress = Math.Round(_cabinManager.InFlightServiceProgress, 1),
                securingProgress = Math.Round(_cabinManager.SecuringProgress, 1),
                isSecuringHalted = _cabinManager.IsSecuringHalted,
                satietyActive = _cabinManager.IsSatietyActive,
                cabinState = _cabinManager.State.ToString(),
                isServiceHalted = _cabinManager.IsServiceHalted,
                cabinReportCooldownElapsed = _cabinManager.SecondsSinceLastReport,
                airline = CurrentAirline,
                issuedCommands = _cabinManager.IssuedCommands.ToList(),
                activeCrisis = _crisisManager.ActiveCrisis.ToString(),
                isGoAroundActive = _phaseManager.IsGoAroundActive,
                isSevereTurbulenceActive = _phaseManager.IsSevereTurbulenceActive,
                cateringCompletion = Math.Round(_cabinManager.CateringCompletion, 1),
                cateringRations = _cabinManager.CateringRations,
                thermalDissatisfaction = Math.Round(_cabinManager.ThermalDissatisfaction, 1),
                cabinTemp = Math.Round(_cabinManager.LastKnownCabinTemp, 1),
                cabinCleanliness = Math.Round(_cabinManager.CabinCleanliness, 1),
                waterLevel = Math.Round(_cabinManager.WaterLevel, 1),
                wasteLevel = Math.Round(_cabinManager.WasteLevel, 1),
                baggageCompletion = Math.Round(_cabinManager.BaggageCompletion, 1),
                isSilencePenaltyActive = _cabinManager.IsSilencePenaltyActive,
                crisisElapsed = _crisisManager.CrisisStartTimeSeconds > 0 ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _crisisManager.CrisisStartTimeSeconds : 0,
                passengers = _cabinManager.PassengerManifest,
                turbulenceSeverity = (int)_phaseManager.TurbulenceSeverity
            });
            
            if (_groundOpsManager.Services.Count > 0)
            {
                SendToWeb(new 
                {
                    type = "groundOps",
                    services = _groundOpsManager.Services
                });
            }
        }

        private void LoadNextLeg()
        {
            if (_rotationQueue.Count == 0) return;

            var response = _rotationQueue.Dequeue();
            _currentResponse = response;
            _cabinManager.CurrentFlight = _currentResponse;

            _phaseManager.Reset(true); // true = Turnaround state
            _scoreManager.Reset();
            _cabinManager.Reset();
            _eventEngine.Reset();
            CurrentAirline = _airlineDb.GetProfileFor(response.General?.Airline ?? "");

            var passengerService = new FlightSupervisor.UI.Services.PassengerManifestService();
            var manifestData = passengerService.GenerateManifest(response);

            _cabinManager.InitializeFlightDemographics(CurrentAirline, manifestData);

            if (CurrentAirline != null)
            {
                if (CurrentAirline.SafetyRecord >= 9) _crisisManager.Frequency = FlightSupervisor.UI.Services.CrisisFrequency.Realistic;
                else if (CurrentAirline.SafetyRecord >= 6) _crisisManager.Frequency = FlightSupervisor.UI.Services.CrisisFrequency.Frequent;
                else _crisisManager.Frequency = FlightSupervisor.UI.Services.CrisisFrequency.Chaos;
                System.Diagnostics.Debug.WriteLine($"[CRISIS] Safety Score: {CurrentAirline.SafetyRecord} -> Frequency set to: {_crisisManager.Frequency}");
            }

            DateTime? nextSobt = null;
            if (_aibt.HasValue)
            {
                int tatMinutes = _airlineDb.GetStandardTurnaroundTimeMinutes(CurrentAirline?.Tier);
                DateTime minimumSobt = _aibt.Value.AddMinutes(tatMinutes);
                
                DateTime scheduledSobt = DateTime.MinValue;
                if (response.Times?.SchedOut != null && long.TryParse(response.Times.SchedOut, out long unixSobt))
                {
                    scheduledSobt = DateTimeOffset.FromUnixTimeSeconds(unixSobt).UtcDateTime;
                }
                
                if (scheduledSobt > DateTime.MinValue && minimumSobt > scheduledSobt)
                {
                    nextSobt = minimumSobt;
                    SendToWeb(new { type = "log", message = $"[DISPATCH] Late Arrival Detected. Turnaround Time ({tatMinutes} min) enforces new SOBT: {nextSobt.Value:HH:mm}Z" });
                }
            }

            _groundOpsManager.InitializeFromSimBrief(response, _cabinManager.SessionFlightsCompleted == 0 && _cabinManager.FirstFlightClean, _currentFobKg, nextSobt);

            _ = RefreshLiveWeatherAsync();

            SendToWeb(new { type = "groundOpsReady" });
            SendToWeb(new { type = "groundOps", services = _groundOpsManager.Services });

            if (!string.IsNullOrEmpty(response.General?.InitialAlt))
            {
                var digits = new string(response.General.InitialAlt.Where(char.IsDigit).ToArray());
                if (double.TryParse(digits, out double alt))
                {
                    _phaseManager.TargetCruiseAltitude = alt < 1000 ? alt * 100 : alt;
                }
            }

            string acType = response.Aircraft?.BaseType ?? response.Aircraft?.IcaoCode ?? "";
            if (acType.StartsWith("A33") || acType.StartsWith("A34") || acType.StartsWith("A35") || acType.StartsWith("A38") || 
                acType.StartsWith("B74") || acType.StartsWith("B76") || acType.StartsWith("B77") || acType.StartsWith("B78") || acType.StartsWith("MD1"))
                _phaseManager.AircraftCategory = "Heavy";
            else if (acType.StartsWith("C1") || acType.StartsWith("SR") || acType.StartsWith("DA") || acType.StartsWith("PA") || acType.StartsWith("P28"))
                _phaseManager.AircraftCategory = "Light";
            else
                _phaseManager.AircraftCategory = "Medium";

            SendToWeb(new { type = "simulationState", msg = $"Leg Loaded: {response.Origin?.IcaoCode} to {response.Destination?.IcaoCode}. {(_rotationQueue.Count)} leg(s) remaining." });
        }

        private async System.Threading.Tasks.Task FetchFlightPlan(string username, bool remember, FlightSupervisor.UI.Models.UnitPreferences? units = null, string weatherSource = "simbrief", bool syncMsfsTime = false)
        {
            if (units == null) units = new FlightSupervisor.UI.Models.UnitPreferences();
            if (string.IsNullOrEmpty(username)) return;

            if (remember)
            {
                var saveFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor_User.txt");
                System.IO.File.WriteAllText(saveFilePath, username);
            }

            // Silent fetch

            try
            {
                var response = await _simBriefService.FetchFlightPlanAsync(username);

                if (response != null && response.Fetch?.Status == "Success")
                {
                    if (response.Weather != null)
                    {
                        if (weatherSource == "activesky")
                        {
                            var (destMetar, destTaf) = await _activeSkyService.GetWeatherAsync(response.Destination?.IcaoCode ?? "");
                            if (!string.IsNullOrEmpty(destMetar)) response.Weather.DestMetar = destMetar;
                            if (!string.IsNullOrEmpty(destTaf)) response.Weather.DestTaf = destTaf;

                            var (origMetar, _) = await _activeSkyService.GetWeatherAsync(response.Origin?.IcaoCode ?? "");
                            if (!string.IsNullOrEmpty(origMetar)) response.Weather.OrigMetar = origMetar;

                            var altnIcao = response.Alternate?.IcaoCode ?? "";
                            if (!string.IsNullOrEmpty(altnIcao))
                            {
                                var (altnMetar, altnTaf) = await _activeSkyService.GetWeatherAsync(altnIcao);
                                if (!string.IsNullOrEmpty(altnMetar)) 
                                {
                                    using var doc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(altnMetar));
                                    response.Weather.AltnMetar = doc.RootElement.Clone();
                                }
                                if (!string.IsNullOrEmpty(altnTaf)) 
                                {
                                    using var doc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(altnTaf));
                                    response.Weather.AltnTaf = doc.RootElement.Clone();
                                }
                            }
                        }
                        else if (weatherSource == "noaa")
                        {
                            var destIcao = response.Destination?.IcaoCode ?? "";
                            var destMetar = await _noaaWeatherService.GetMetarAsync(destIcao);
                            if (!string.IsNullOrEmpty(destMetar)) response.Weather.DestMetar = destMetar;
                            var destTaf = await _noaaWeatherService.GetTafAsync(destIcao);
                            if (!string.IsNullOrEmpty(destTaf)) response.Weather.DestTaf = destTaf;

                            var origIcao = response.Origin?.IcaoCode ?? "";
                            var origMetar = await _noaaWeatherService.GetMetarAsync(origIcao);
                            if (!string.IsNullOrEmpty(origMetar)) response.Weather.OrigMetar = origMetar;

                            var altnIcao = response.Alternate?.IcaoCode ?? "";
                            if (!string.IsNullOrEmpty(altnIcao))
                            {
                                var altnMetar = await _noaaWeatherService.GetMetarAsync(altnIcao);
                                if (!string.IsNullOrEmpty(altnMetar)) 
                                {
                                    using var doc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(altnMetar));
                                    response.Weather.AltnMetar = doc.RootElement.Clone();
                                }
                                var altnTaf = await _noaaWeatherService.GetTafAsync(altnIcao);
                                if (!string.IsNullOrEmpty(altnTaf)) 
                                {
                                    using var doc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(altnTaf));
                                    response.Weather.AltnTaf = doc.RootElement.Clone();
                                }
                            }
                        }
                    }

                    if (_rotationQueue.Count == 0 && syncMsfsTime && _simConnectService != null && _simConnectService.IsConnected && response.Times != null)
                    {
                        if (long.TryParse(response.Times.SchedOut, out long originalOut) && long.TryParse(response.Times.SchedIn, out long originalIn))
                        {
                            long blockSecs = originalIn - originalOut;
                            if (blockSecs <= 0) blockSecs = 3600; // fallback 1h

                            long simNowUnix = ((DateTimeOffset)_simConnectService.CurrentSimZuluTime).ToUnixTimeSeconds();
                            
                            long newOut = simNowUnix + (30 * 60); 
                            long newIn = newOut + blockSecs;
                            
                            response.Times.SchedOut = newOut.ToString();
                            response.Times.SchedIn = newIn.ToString();
                        }
                    }
                    else if (_rotationQueue.Count > 0 && response.Times != null)
                    {
                        var lastLeg = _rotationQueue.Last();
                        if (lastLeg.Times != null && long.TryParse(lastLeg.Times.SchedIn, out long lastIn) && 
                            long.TryParse(response.Times.SchedOut, out long currentOut) && 
                            long.TryParse(response.Times.SchedIn, out long currentIn))
                        {
                            long blockSecs = currentIn - currentOut;
                            if (blockSecs <= 0) blockSecs = 3600; // fallback 1h

                            long turnaroundSecs = 45 * 60; // default medium 45 min
                            string acType = response.Aircraft?.BaseType ?? response.Aircraft?.IcaoCode ?? "";
                            if (acType.StartsWith("A33") || acType.StartsWith("A34") || acType.StartsWith("A35") || acType.StartsWith("A38") || 
                                acType.StartsWith("B74") || acType.StartsWith("B76") || acType.StartsWith("B77") || acType.StartsWith("B78") || acType.StartsWith("MD1"))
                                turnaroundSecs = 70 * 60;
                            else if (acType.StartsWith("C1") || acType.StartsWith("SR") || acType.StartsWith("DA") || acType.StartsWith("PA") || acType.StartsWith("P28"))
                                turnaroundSecs = 25 * 60;

                            long newOut = lastIn + turnaroundSecs;
                            long newIn = newOut + blockSecs;

                            response.Times.SchedOut = newOut.ToString();
                            response.Times.SchedIn = newIn.ToString();
                        }
                    }

                    _rotationQueue.Enqueue(response);
                    
                    var weatherService = new WeatherBriefingService(units);
                    var briefingData = weatherService.GenerateBriefing(response);

                    var passengerService = new FlightSupervisor.UI.Services.PassengerManifestService();
                    var manifestData = passengerService.GenerateManifest(response);

                    SendToWeb(new { 
                        type = "flightData", 
                        data = response,
                        briefing = briefingData,
                        manifest = manifestData
                    });
                    
                    SendToWeb(new { type = "fetchStatus", status = "success", message = "Flight plan parsed and added to rotation." });
                }
                else
                {
                    SendToWeb(new { type = "fetchStatus", status = "error", message = "Could not parse flight plan." });
                }
            }
            catch (Exception ex)
            {
                SendToWeb(new { type = "fetchStatus", status = "error", message = ex.Message });
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            var hwndScreen = HwndSource.FromHwnd(helper.Handle);
            hwndScreen.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_USER_SIMCONNECT = 0x0402;
            if (msg == WM_USER_SIMCONNECT)
            {   
                _simConnectService.ReceiveMessage();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _simConnectService?.Disconnect();
            _panelServer?.StopServer();
        }

        // --- WIN32 STUFF TO FIX TASKBAR OVERLAP WHEN MAXIMIZED ---
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle).AddHook(WindowProc);
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0024) // WM_GETMINMAXINFO
            {
                WmGetMinMaxInfo(hwnd, lParam);
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO))!;
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                GetMonitorInfo(monitor, ref monitorInfo);
                
                mmi.ptMaxPosition.x = Math.Abs(monitorInfo.rcWork.left - monitorInfo.rcMonitor.left);
                mmi.ptMaxPosition.y = Math.Abs(monitorInfo.rcWork.top - monitorInfo.rcMonitor.top);
                mmi.ptMaxSize.x = Math.Abs(monitorInfo.rcWork.right - monitorInfo.rcWork.left);
                mmi.ptMaxSize.y = Math.Abs(monitorInfo.rcWork.bottom - monitorInfo.rcWork.top);
                
                Marshal.StructureToPtr(mmi, lParam, true);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO { public POINT ptReserved; public POINT ptMaxSize; public POINT ptMaxPosition; public POINT ptMinTrackSize; public POINT ptMaxTrackSize; }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int left; public int top; public int right; public int bottom; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        private async System.Threading.Tasks.Task RefreshLiveWeatherAsync()
        {
            if (_currentResponse?.Weather == null) return;
            string weatherSource = _profileManager.CurrentProfile.WeatherSource ?? "simbrief";

            if (weatherSource == "activesky")
            {
                var destIcao = _currentResponse.Destination?.IcaoCode;
                if (!string.IsNullOrEmpty(destIcao))
                {
                    var (destMetar, destTaf) = await _activeSkyService.GetWeatherAsync(destIcao);
                    if (!string.IsNullOrEmpty(destMetar)) _currentResponse.Weather.DestMetar = destMetar;
                    if (!string.IsNullOrEmpty(destTaf)) _currentResponse.Weather.DestTaf = destTaf;
                }

                var origIcao = _currentResponse.Origin?.IcaoCode;
                if (!string.IsNullOrEmpty(origIcao))
                {
                    var (origMetar, _) = await _activeSkyService.GetWeatherAsync(origIcao);
                    if (!string.IsNullOrEmpty(origMetar)) _currentResponse.Weather.OrigMetar = origMetar;
                }

                var altnIcao = _currentResponse.Alternate?.IcaoCode;
                if (!string.IsNullOrEmpty(altnIcao))
                {
                    var (altnMetar, altnTaf) = await _activeSkyService.GetWeatherAsync(altnIcao);
                    if (!string.IsNullOrEmpty(altnMetar)) 
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(altnMetar));
                        _currentResponse.Weather.AltnMetar = doc.RootElement.Clone();
                    }
                    if (!string.IsNullOrEmpty(altnTaf)) 
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(altnTaf));
                        _currentResponse.Weather.AltnTaf = doc.RootElement.Clone();
                    }
                }
            }
            else if (weatherSource == "noaa")
            {
                var destIcao = _currentResponse.Destination?.IcaoCode;
                if (!string.IsNullOrEmpty(destIcao))
                {
                    var destMetar = await _noaaWeatherService.GetMetarAsync(destIcao);
                    if (!string.IsNullOrEmpty(destMetar)) _currentResponse.Weather.DestMetar = destMetar;
                    var destTaf = await _noaaWeatherService.GetTafAsync(destIcao);
                    if (!string.IsNullOrEmpty(destTaf)) _currentResponse.Weather.DestTaf = destTaf;
                }

                var origIcao = _currentResponse.Origin?.IcaoCode;
                if (!string.IsNullOrEmpty(origIcao))
                {
                    var origMetar = await _noaaWeatherService.GetMetarAsync(origIcao);
                    if (!string.IsNullOrEmpty(origMetar)) _currentResponse.Weather.OrigMetar = origMetar;
                }

                var altnIcao = _currentResponse.Alternate?.IcaoCode;
                if (!string.IsNullOrEmpty(altnIcao))
                {
                    var altnMetar = await _noaaWeatherService.GetMetarAsync(altnIcao);
                    if (!string.IsNullOrEmpty(altnMetar)) 
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(altnMetar));
                        _currentResponse.Weather.AltnMetar = doc.RootElement.Clone();
                    }
                    var altnTaf = await _noaaWeatherService.GetTafAsync(altnIcao);
                    if (!string.IsNullOrEmpty(altnTaf)) 
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(altnTaf));
                        _currentResponse.Weather.AltnTaf = doc.RootElement.Clone();
                    }
                }
            }
            
            if (weatherSource == "activesky" || weatherSource == "noaa")
            {
                var weatherService = new FlightSupervisor.UI.Services.WeatherBriefingService();
                var briefingData = weatherService.GenerateBriefing(_currentResponse);
                SendToWeb(new { type = "briefingUpdate", briefing = briefingData });
            }
        }
    }
}