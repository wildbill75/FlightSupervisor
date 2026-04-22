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
        private List<SimBriefResponse> _rotationQueue = new();
        
        private double _lastKnownGroundSpeed = 0;
        private double _lastKnownAirspeed = 0;
        private double _lastKnownAltitude = 0;
        private double _lastKnownRadioHeight = 0;
        private bool _isFenixStrobeSyncActive = false;
        private DateTime _lastWindingTrigger = DateTime.MinValue;

        private bool _ghostFuelTrackerActive = false;
        private double _virtualFobKg = 0;
        private DateTime _lastFuelFlowUpdate = DateTime.MinValue;
        private bool _isApuRunning = false;
        private bool _isFenixEng1MasterOn = false;
        private bool _isFenixEng2MasterOn = false;
        private bool _gsxAutoSyncEnabled = false;
        // -------------------------------

        private bool _isParkingBrakeSet = false;
        private double _lastKnownThrottle = 0;
        private double _lastKnownPitch = 0;
        private double _lastKnownVerticalSpeed = 0;
        private double _lastKnownBank = 0;
        private bool _isGearDown = true;
        private DateTime _currentSimTime = DateTime.MinValue;
        private DateTime _ignoreSimTimeUntil = DateTime.MinValue;
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
        private int _turnaroundEfficiencySec = 0;
        private DateTime? _nextSobtOverride = null;
        
        private GroundOpsManager _groundOpsManager;
        private System.Windows.Threading.DispatcherTimer _uiTimer;
        private System.Windows.Threading.DispatcherTimer _msfsWatcherTimer;
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private PanelServerService? _panelServer;
        private AudioEngineService _audioEngine;
        private SuperScoreManager _scoreManager;
        private FlowTrackerService _flowTrackerService;
        private ScoreFlowEvaluator _scoreFlowEvaluator;
        private List<FlightSupervisor.UI.Models.FlightArchive> _sessionArchives = new List<FlightSupervisor.UI.Models.FlightArchive>();
        
        private double _currentFobKg = 3000;

        private CabinManager _cabinManager;
        private GroundOpsResourceService _groundOpsResourceService;
        private AirlineProfileManager _airlineDb;
        private FlightSupervisor.UI.Services.GroundEventEngine _eventEngine;
        private CrisisManager _crisisManager;
        public AirlineProfile? CurrentAirline { get; private set; }
        private ProfileManager _profileManager;
        private AirframeManager _airframeManager;
        private WearAndTearManager _wearAndTearManager;
        private ActiveSkyService _activeSkyService;
        private NoaaWeatherService _noaaWeatherService;
        private System.Windows.Threading.DispatcherTimer _weatherUpdateTimer;

        private Microsoft.Web.WebView2.Wpf.WebView2 _manifestWebView;
        private Window _manifestWindow;
        private Microsoft.Web.WebView2.Wpf.WebView2 _groundOpsWebView;
        private Window _groundOpsWindow;
        private Microsoft.Web.WebView2.Wpf.WebView2 _fuelSheetWebView;
        private Window _fuelSheetWindow;
        private Microsoft.Web.WebView2.Wpf.WebView2 _airframeWebView;
        private Window _airframeWindow;
        
        private Microsoft.Web.WebView2.Wpf.WebView2 _logsWebView;
        private Window _logsWindow;
        private Microsoft.Web.WebView2.Wpf.WebView2 _sandboxWebView;
        private Window _sandboxWindow;
        private string _cachedLogsScore = "";
        private string _cachedLogsHtml = "";

        private void RefreshPassengerDemographics()
        {
            if (_currentResponse == null) return;

            CurrentAirline = _airlineDb.GetProfileFor(_currentResponse?.General?.Airline ?? "");
            _groundOpsManager.IsFuelSheetValidated = false;

            var passengerService = new FlightSupervisor.UI.Services.PassengerManifestService();
            var manifestData = passengerService.GenerateManifest(_currentResponse, _profileManager.CurrentProfile);

            _cabinManager.InitializeFlightDemographics(CurrentAirline, manifestData);
            
            SendToWeb(new { type = "manifestUpdate", manifest = manifestData });

            if (CurrentAirline != null)
            {
                if (CurrentAirline.SafetyRecord >= 9) _crisisManager.Frequency = FlightSupervisor.UI.Services.CrisisFrequency.Realistic;
                else if (CurrentAirline.SafetyRecord >= 6) _crisisManager.Frequency = FlightSupervisor.UI.Services.CrisisFrequency.Frequent;
                else _crisisManager.Frequency = FlightSupervisor.UI.Services.CrisisFrequency.Chaos;
                System.Diagnostics.Debug.WriteLine($"[CRISIS] Safety Score: {CurrentAirline.SafetyRecord} -> Frequency set to: {_crisisManager.Frequency}");
            }
        }

        private bool _isAtWrongAirport = false;
        private string _lastNotifiedMismatchIcao = "";

        private bool IsAircraftAtOrigin()
        {
            // Safety gate: If sim data is not yet available (initial state 0,0), don't trigger mismatch
            if (Math.Abs(_phaseManager.Latitude) < 0.001 && Math.Abs(_phaseManager.Longitude) < 0.001)
                return true;

            // STORY 38: Simplified Logic
            // We only ever check against the CURRENT active flight's origin.
            // Since we load the next leg immediately at arrival, this is always consistent.
            var originToCheck = _currentResponse?.Origin;
            
            if (originToCheck == null) return true;
            
            if (double.TryParse(originToCheck.PosLat, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double expLat) &&
                double.TryParse(originToCheck.PosLong, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double expLon))
            {
                double dist = CalculateHaversineDistanceNM(_phaseManager.Latitude, _phaseManager.Longitude, expLat, expLon);
                return dist < 10.0; // Increased threshold to 10.0 NM for large airports and flexibility.
            }
            return true;
        }
        private Dictionary<string, double> _lastGsxStates = new();

        private void TryAutoStartGsxService(string serviceName, double gsxState)
        {
            // Log changes to frontend for user to debug
            if (!_lastGsxStates.ContainsKey(serviceName) || _lastGsxStates[serviceName] != gsxState)
            {
                _lastGsxStates[serviceName] = gsxState;
                SendToWeb(new { type = "opsLog", message = $"[GSX DEBUG] {serviceName} LVar changed to: {gsxState}" });
            }

            if (!_gsxAutoSyncEnabled) return;

            // GSX state: 1 = Requested, 2 = Moving to Aircraft, 3 = In Progress, 4 = Move away, 5 = Completed
            // We trigger when GSX is actively doing something (>= 2).
            if (gsxState >= 2.0 && gsxState <= 5.0)
            {
                var svc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == serviceName);
                if (svc != null && (svc.State == GroundServiceState.NotStarted || svc.State == GroundServiceState.WaitingForAction))
                {
                    _groundOpsManager.StartManualService(serviceName);
                    // Force immediate UI refresh
                    SendToWeb(new { type = "groundOps", services = _groundOpsManager.Services });
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            FlightSupervisor.UI.Services.WindowSettingsManager.ApplySettings(this, "MainWindow", 1928, 1197);
            
            _audioEngine = new AudioEngineService();
            _simBriefService = new SimBriefService();
            _activeSkyService = new ActiveSkyService();
            _noaaWeatherService = new NoaaWeatherService();
            _panelServer = new PanelServerService();
            _panelServer.StartServer();

            _airlineDb = new AirlineProfileManager(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Airlines.json"));
            _profileManager = new ProfileManager();
            _airframeManager = new AirframeManager();

            _groundOpsManager = new GroundOpsManager();
            _groundOpsManager.OnPenaltyTriggered += (points, reason) => {
                if (_scoreManager != null) _scoreManager.AddScore(points, reason, ScoreCategory.FlightPhaseFlows);
            };
            _groundOpsManager.OnOperationBonusTriggered += (points, reason) => {
                if (_scoreManager != null) _scoreManager.AddScore(points, reason, ScoreCategory.FlightPhaseFlows);
            };
            _groundOpsManager.OnOpsCompleted += () => {
                if (_groundOpsManager.TargetSobt.HasValue && _currentSimTime != DateTime.MinValue && _aobt == null)
                {
                    _turnaroundEfficiencySec = (int)(_groundOpsManager.TargetSobt.Value - _currentSimTime).TotalSeconds;
                }
                SendToWeb(new { type = "groundOpsComplete" });

                if (_phaseManager.CurrentPhase == FlightPhase.Turnaround)
                {
                    Dispatcher.Invoke(() => {
                        SendToWeb(new { type = "log", message = $"[SYSTEM] Turnaround Unloading Complete. Awaiting 'Prepare Next Leg'." });
                    });
                }
            };
            _groundOpsManager.OnOpsLog += msg => SendToWeb(new { type = "log", message = msg });
            _groundOpsManager.OnOpsUpdated += () => {
                var services = _groundOpsManager.Services;
                var planRampStr = _currentResponse?.Fuel?.PlanRamp ?? "3000"; // Fallback to 3000 if not available
                double planRamp = 0;
                double.TryParse(planRampStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out planRamp);
                
                Dispatcher.Invoke(() => SendToWeb(new { 
                    type = "groundOps", 
                    services = services,
                    isSeatbeltsOn = _groundOpsManager.IsSeatbeltsOn,
                    isBeaconOn = _groundOpsManager.IsBeaconOn,
                    isFuelValidated = _groundOpsManager.IsFuelSheetValidated,
                    planRampKg = planRamp
                }));
            };
            _groundOpsManager.OnServiceStarted += srvName => {
                if (srvName.Equals("Boarding", StringComparison.OrdinalIgnoreCase))
                {
                    _cabinManager.StartBoarding();
                }
                else if (srvName.Equals("Deboarding", StringComparison.OrdinalIgnoreCase))
                {
                    _cabinManager.StartDeboarding();
                }
            };
            
            _groundOpsManager.OnServiceCompleted += srvName => {
                if (srvName.Contains("Clean", StringComparison.OrdinalIgnoreCase) || srvName.Contains("PNC Chores", StringComparison.OrdinalIgnoreCase))
                {
                    _cabinManager?.PlayCabinReady();
                }
            };

            FlightSupervisor.UI.Services.DebugLogger.Initialize();
            FlightSupervisor.UI.Services.DebugLogger.Log("SYSTEM", "MainWindow initialized");

            _eventEngine = new FlightSupervisor.UI.Services.GroundEventEngine();
            _eventEngine.OnEventTriggered += OnGroundEventTriggered;

            _cabinManager = new CabinManager(_audioEngine);
            _cabinManager.CaptainName = $"{_profileManager.CurrentProfile.FirstName} {_profileManager.CurrentProfile.LastName}".Trim();
            if (string.IsNullOrEmpty(_cabinManager.CaptainName)) _cabinManager.CaptainName = "Pilot";
            _groundOpsResourceService = new GroundOpsResourceService(_groundOpsManager, _cabinManager);
            _cabinManager.OnCrewMessage += (level, msg, audioSeq) => {
                FlightSupervisor.UI.Services.DebugLogger.Log("CREW_MSG", $"[{level}] {msg}");
                SendToWeb(new { type = "cabinLog", level = level, message = msg, audioSequence = audioSeq });
            };
            _cabinManager.OnPenaltyTriggered += (points, reason) => {
                FlightSupervisor.UI.Services.DebugLogger.Log("PENALTY", $"[{points} pts] {reason}");
                if (_scoreManager != null) _scoreManager.AddScore(points, reason, ScoreCategory.Airmanship);
                SendToWeb(new { type = "cabinLog", level = "red", message = $"[PENALTY] {reason} ({points} pts)", audioSequence = (string)null });
            };
            _cabinManager.OnPncStatusChanged += (status, state) => {
                FlightSupervisor.UI.Services.DebugLogger.Log("PNC_STATE", $"Status: {status}, State: {state}");
                Dispatcher.Invoke(() => SendToWeb(new { type = "pncStatus", status = status, state = state.ToString() }));
            };
            _cabinManager.OnOperationBonusTriggered += (bonus, reason) => {
                FlightSupervisor.UI.Services.DebugLogger.Log("BONUS", $"[{bonus} pts] {reason}");
                if (reason == "pnc_ready_chime") {
                    SendToWeb(new { type = "playSound", sound = "chime_emergency" });
                } else {
                    SendToWeb(new { type = "cabinLog", level = "cyan", message = $"[BONUS] {reason} (+{bonus} pts)", audioSequence = (string)null });
                }
            };

            
            _cabinManager.OnCabinCallIncoming += (callState) => {
                FlightSupervisor.UI.Services.DebugLogger.Log("INTERCOM", $"Cabin Call UI State: {callState}");
                SendToWeb(new { type = "incoming_pnc_call", state = callState });
            };
            _cabinManager.OnDeboardingComplete += () => {
                SendToWeb(new { type = "log", message = "[SYSTEM] Cabin secured, passengers have disembarked." });
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
                if (_phaseManager.IsPaused) return;

                if (_phaseManager.IsSimulationMode)
                {
                    if (_currentSimTime == DateTime.MinValue || _currentSimTime.Year < 2000) 
                    {
                        _currentSimTime = DateTime.UtcNow;
                        if (_cabinManager.CurrentSimLocalTime == DateTime.MinValue)
                        {
                            _cabinManager.CurrentSimLocalTime = DateTime.Now;
                        }
                    }
                    _currentSimTime = _currentSimTime.AddSeconds(1);
                    if (_cabinManager.CurrentSimLocalTime != DateTime.MinValue)
                        _cabinManager.CurrentSimLocalTime = _cabinManager.CurrentSimLocalTime.AddSeconds(1);
                    
                    // Always disconnect FSUIPC if we turned on Simulation Mode so it doesn't fight.
                    if (_simConnectService != null && _simConnectService.IsConnected)
                    {
                        _simConnectService.Disconnect();
                    }
                }
                
                _groundOpsManager.CurrentPhase = _phaseManager.CurrentPhase;
                _groundOpsManager.IsSeatbeltsOn = _phaseManager.IsSeatbeltsOn;
                _groundOpsManager.IsBeaconOn = _phaseManager.IsBeaconLightOn;
                _groundOpsManager.EngineMaxN1 = Math.Max(_phaseManager.Eng1N1, _phaseManager.Eng2N1);
                _groundOpsManager.Tick(_currentSimTime);

                // --- TURNAROUND NEXT LEG AUTO-ADVANCE ---
                if (_phaseManager.CurrentPhase == FlightPhase.Turnaround && !_groundOpsManager.IsFuelSheetValidated)
                {
                    bool tasksPending = _groundOpsManager.Services.Any(s => (s.Name == "Deboarding" || s.Name.StartsWith("Cargo")) && s.State != GroundServiceState.Completed && s.State != GroundServiceState.Skipped);
                    if (!tasksPending)
                    {
                        Dispatcher.Invoke(() => {
                            LoadNextLeg();
                        });
                    }
                }

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
                    // [BUG-FIX] Disable Random Ground Events temporarily as requested
                    // _eventEngine.Tick(_groundOpsManager.EventProbabilityPercent, CurrentAirline);
                }

                if (_groundOpsManager.IsAnyOperationInProgress() && (_phaseManager.GroundSpeed > 1.0 || !_phaseManager.IsOnGround))
                {
                    _groundOpsManager.AbortAllOperations();
                    _scoreManager.CancelFlight(LocalizationService.GetString("FlightCancel"));
                    SendToWeb(new { type = "flightCancelled" });
                }
                
                // Track Cabin Anxiety
                var bService = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Boarding");
                bool isBoardingComplete = bService?.State == GroundServiceState.Completed || bService?.State == GroundServiceState.Skipped;
                _cabinManager.HasBoardingStarted = bService != null && bService.State != GroundServiceState.NotStarted && bService.State != GroundServiceState.WaitingForAction;
                DateTime? sobtDate = null;
                if (_currentResponse?.Times?.SchedOut != null && long.TryParse(_currentResponse.Times.SchedOut, out long sobtUnix))
                {
                    sobtDate = DateTimeOffset.FromUnixTimeSeconds(sobtUnix).DateTime;
                }
                var boardingService = _groundOpsManager?.Services?.FirstOrDefault(s => s.Name == "Boarding");
                double boardingProg = boardingService != null ? (boardingService.ProgressPercent / 100.0) : -1.0;

                double targetTemp = _cabinManager.CurrentAmbientTemperature;
                double variance = (_currentSimTime.Second % 10) / 20.0; // 0.0 to 0.45 pseudo-random drift

                if (_phaseManager.FenixCabinTempFwd > 0.01 || _phaseManager.FenixCabinTempAft > 0.01) 
                {
                    _hasReceivedFenixLvars = true; // Latch so we know the data link is active!
                }

                if (_hasReceivedFenixLvars) 
                {
                    bool isBleedFlowing = _phaseManager.FenixApuBleed || _phaseManager.Eng1Combustion || _phaseManager.Eng2Combustion;
                    bool isPackRunning = _phaseManager.FenixPack1 || _phaseManager.FenixPack2;
                    bool isAcRunning = isBleedFlowing && isPackRunning;

                    if (isAcRunning)
                    {
                        // The Fenix LVARs (A_OH_PNEUMATIC_FWD_TEMP and AFT_TEMP) represent the selector knob position.
                        // A value of 0.5 represents the 12 o'clock position (24°C). The range is strictly 0.0 to 1.0.
                        // We map the 0.0-1.0 range to 18-30°C.
                        double normFwd = Math.Min(1.0, Math.Max(0.0, _phaseManager.FenixCabinTempFwd));
                        double normAft = Math.Min(1.0, Math.Max(0.0, _phaseManager.FenixCabinTempAft));

                        double mappedTempFwd = 18.0 + (normFwd * 12.0);
                        double mappedTempAft = 18.0 + (normAft * 12.0);

                        targetTemp = ((mappedTempFwd + mappedTempAft) / 2.0) + variance;
                    }
                    else
                    {
                        // Without AC running, cabin slowly gravitates towards the outside ambient temperature!
                        targetTemp = _cabinManager.CurrentAmbientTemperature;
                    }
                }
                else 
                {
                    // Without explicit Fenix LVar, we gently trend towards a comfortable 22.5 +/- 0.5 if engines/APU provide conditioning
                    if (_phaseManager.Eng1Combustion || _phaseManager.Eng2Combustion || _phaseManager.FenixApuBleed)
                    {
                        targetTemp = 22.0 + variance;
                    }
                    else
                    {
                        targetTemp = _cabinManager.CurrentAmbientTemperature;
                    }
                }
                
                // THERMAL DEBUG
                if (_currentSimTime.Second % 5 == 0 && _currentSimTime.Millisecond < 500) {
                    // SendToWeb(new { type = "log", message = $"DEBUG THERMO - Target: {targetTemp:F1}°C | Ambient: {_cabinManager.CurrentAmbientTemperature:F1}°C | Cabin: {_cabinManager.LastKnownCabinTemp:F1}°C" });
                }

                _cabinManager.Tick(_phaseManager.GForce, _lastKnownBank, isBoardingComplete,
                                   _currentSimTime, sobtDate, _phaseManager.CurrentPhase, _lastKnownGroundSpeed,
                                   _lastKnownAltitude, _lastKnownVerticalSpeed,
                                   _phaseManager.IsGoAroundActive || _phaseManager.IsSevereTurbulenceActive || _phaseManager.HasEngineFailure,
                                   targetTemp, boardingProg);
                
                // Continuous Check for Refueling rules
                if (_phaseManager.IsSeatbeltsOn && _groundOpsManager != null && !_cabinManager.HasPenalizedRefuelingSeatbelts)
                {
                    bool hasPassengers = _cabinManager.PassengerManifest != null && _cabinManager.PassengerManifest.Any(p => p.IsBoarded);
                    if (hasPassengers)
                    {
                        var refSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Refueling");
                        if (refSvc != null && (refSvc.State == GroundServiceState.InProgress || refSvc.State == GroundServiceState.Delayed))
                        {
                            _cabinManager.TriggerRefuelingSeatbeltPenalty();
                        }
                    }
                }

                // Les ressources Cabine (Water, Waste, Cleanliness, Fuel) sont désormais gérées de manière
                // progressive et validées à la fin de leur progression par GroundOpsResourceService.Tick();

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
                FlightSupervisor.UI.Services.DebugLogger.Log("FLIGHT_PHASE", $"Transitioned to {phase}");
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
                            if (_scoreManager != null) _scoreManager.AddScore(-200, "SÉCURITÉ: Décollage sans cabine sécurisée !", ScoreCategory.Airmanship);
                        }
                    }
                    // Bug #5 Fix: Inhibited automatic Cruise PA. Must be manually triggered by Captain from the PA panel.
                    /* 
                    else if (phase == FlightPhase.Cruise)
                    {
                        Task.Run(async () => {
                            await Task.Delay(30000); // Wait 30 seconds after leveling off
                            _cabinManager.AnnounceToCabin("CruiseStatus");
                        });
                    }
                    */
                    /* 
                    else if (phase == FlightPhase.Descent)
                    {
                        _cabinManager.AnnounceToCabin("Descent");
                    }
                    */
                    else if (phase == FlightPhase.Landing)
                    {
                        if (_cabinManager.State != CabinState.LandingSecured || !_cabinManager.IsCrewSeated)
                        {
                            if (_scoreManager != null) _scoreManager.AddScore(-200, "SÉCURITÉ: Atterrissage sans cabine sécurisée !", ScoreCategory.Airmanship);
                        }
                    }
                    else if (phase == FlightPhase.Arrived)
                    {
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
                                _scoreManager.AddScore(penalty, $"Retard à l'arrivée (Tolérance dépassée): {timeStr}{groundOpsPardon}", ScoreCategory.FlightPhaseFlows);
                            }
                            else if (effectiveDelaySec <= delayToleranceSec && rawDelaySec > delayToleranceSec)
                            {
                                _scoreManager.AddScore(50, $"Amnistie Retard : {rawDelaySec / 60}m justifiés par les Ops Sol !", ScoreCategory.FlightPhaseFlows);
                            }
                            else if (rawDelaySec <= delayToleranceSec)
                            {
                                _scoreManager.AddScore(100, $"Ponctualité : Arrivée dans les temps (Tolérance de {delayToleranceSec / 60}m respectée) !", ScoreCategory.FlightPhaseFlows);
                            }
                        }

                        // Generate Flight Report
                        long schedOut = 0;
                        if (_currentResponse?.Times?.SchedOut != null) long.TryParse(_currentResponse.Times.SchedOut, out schedOut);
                            
                        // Apply final customer dissatisfaction penalty if Comfort is terrible
                        if (_scoreManager.PassengerExperiencePoints < -200)
                        {
                            int dissatPenalty = _scoreManager.PassengerExperiencePoints / 2; // e.g., -300 comfort -> -150 Operations
                            _scoreManager.AddScore(dissatPenalty, "Customer Dissatisfaction: Passenger complaints filed", ScoreCategory.PassengerExperience);
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
                                _scoreManager.AddScore(prestigeDiff, msg, ScoreCategory.PassengerExperience);
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
                                _scoreManager.AirmanshipPoints, 
                                effectiveDelaySec, 
                                manualMins, 
                                _phaseManager.TouchdownFpm, 
                                0, // crosswind
                                _scoreManager.AirmanshipPoints < 1000, 
                                anySkipped, 
                                !cateringPerformed, 
                                _scoreManager.PassengerExperiencePoints, 
                                isNight && landingLightsOff, 
                                false, // go around
                                blockMins, 
                                true);

                            p.LastFlightDate = DateTime.Now;
                            _profileManager.SaveProfile();

                            double expectedBurn = 0;
                            double actualBurn = 0;
                            double initialFob = _cabinManager?.StateOfAircraft?.InitialFobKg ?? 0;
                            if (initialFob > 0)
                            {
                                actualBurn = (initialFob - _currentFobKg) + 200;
                            }
                            if (_currentResponse?.Fuel != null && double.TryParse(_currentResponse.Fuel.PlanRamp, out double pr) && double.TryParse(_currentResponse.Fuel.PlanLanding, out double pl))
                            {
                                expectedBurn = pr - pl;
                                if (actualBurn == 0) actualBurn = pr - _currentFobKg;
                            }

                            var report = new FlightSupervisor.UI.Models.FlightArchive
                            {
                                Score = _scoreManager.CurrentScore,
                                FlightPhaseFlowsPoints = _scoreManager.FlightPhaseFlowsPoints,
                                CommunicationPoints = _scoreManager.CommunicationPoints,
                                AirmanshipPoints = _scoreManager.AirmanshipPoints,
                                MaintenancePoints = _scoreManager.MaintenancePoints,
                                AbnormalOperationsPoints = _scoreManager.AbnormalOperationsPoints,
                                PassengerExperiencePoints = _scoreManager.PassengerExperiencePoints,
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
                                ExpectedBlockBurnKg = expectedBurn,
                                ActualBlockBurnKg = actualBurn,
                                DelaySec = effectiveDelaySec,
                                RawDelaySec = rawDelaySec,
                                TurnaroundEfficiencySec = _turnaroundEfficiencySec
                            };

                            FlightSupervisor.UI.Services.FlightLogger.ArchiveFlight(report);
                            _sessionArchives.Add(report);
                            bool isFinal = _rotationQueue.Count == 0;
                            SendToWeb(new { type = "flightReport", report, isFinal, allReports = _sessionArchives });
                            
                            // Send updated profile to UI immediately to reflect new stats
                            SendToWeb(new { type = "InitProfile", payload = p });
                        }
                        else
                        {
                            double expectedBurn = 0;
                            double actualBurn = 0;
                            double initialFob = _cabinManager?.StateOfAircraft?.InitialFobKg ?? 0;
                            if (initialFob > 0)
                            {
                                actualBurn = (initialFob - _currentFobKg) + 200;
                            }
                            if (_currentResponse?.Fuel != null && double.TryParse(_currentResponse.Fuel.PlanRamp, out double pr) && double.TryParse(_currentResponse.Fuel.PlanLanding, out double pl))
                            {
                                expectedBurn = pr - pl;
                                if (actualBurn == 0) actualBurn = pr - _currentFobKg;
                            }

                            var fallbackReport = new FlightSupervisor.UI.Models.FlightArchive
                            {
                                Score = _scoreManager.CurrentScore,
                                FlightPhaseFlowsPoints = _scoreManager.FlightPhaseFlowsPoints,
                                CommunicationPoints = _scoreManager.CommunicationPoints,
                                AirmanshipPoints = _scoreManager.AirmanshipPoints,
                                MaintenancePoints = _scoreManager.MaintenancePoints,
                                AbnormalOperationsPoints = _scoreManager.AbnormalOperationsPoints,
                                PassengerExperiencePoints = _scoreManager.PassengerExperiencePoints,
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
                                ExpectedBlockBurnKg = expectedBurn,
                                ActualBlockBurnKg = actualBurn,
                                DelaySec = effectiveDelaySec,
                                RawDelaySec = rawDelaySec,
                                TurnaroundEfficiencySec = _turnaroundEfficiencySec
                            };

                            FlightSupervisor.UI.Services.FlightLogger.ArchiveFlight(fallbackReport);
                            _sessionArchives.Add(fallbackReport);
                            bool isFinal = _rotationQueue.Count == 0;
                            SendToWeb(new { type = "flightReport", report = fallbackReport, isFinal, allReports = _sessionArchives });
                        }

                        // Persist multi-leg session state
                        string arrIcao = _currentResponse?.Destination?.IcaoCode ?? "UNK";
                        string arrAirline = _currentResponse?.General?.Airline ?? "UNK";
                        ShiftStateManager.SaveState(_cabinManager, arrIcao, arrAirline);
                    }
                    else if (phase == FlightPhase.Turnaround)
                    {
                        GenerateAgentDebugLog();

                        if (_aibt.HasValue)
                        {
                            int tatMinutes = _airlineDb.GetStandardTurnaroundTimeMinutes(CurrentAirline?.Tier);
                            _nextSobtOverride = _aibt.Value.AddMinutes(tatMinutes);
                            SendToWeb(new { type = "log", message = $"[DISPATCH] Turnaround Global Timer started. Target SOBT is {_nextSobtOverride.Value:HH:mm}Z ({tatMinutes} mins allotted)." });
                        }

                        _cabinManager.SessionFlightsCompleted++;
                        int remainingLegs = _rotationQueue.Count;
                        SendToWeb(new { type = "log", message = $"[SYSTEM] Flight secured. Initiating turnaround deboarding. {remainingLegs} leg(s) remaining." });
                        
                        // STORY 43 : DEFERRED LOAD
                        // Instead of LoadNextLeg(), we just add the deboarding/unloading tasks to the current set.
                        // The user will click "Prepare Next Leg" in UI to trigger the fetch.
                        _groundOpsManager.IsFuelSheetValidated = false; // MUST reset the validation from Leg 1 so it doesn't auto-load the next leg
                        _groundOpsManager.StopOps();
                        _groundOpsManager.Services.Clear();
                        
                        int paxCount = _cabinManager.PassengerManifest?.Count ?? 0;
                        bool lcc = _groundOpsManager.IsLowCost;
                        int deboardDur = Math.Max(120, (int)(paxCount * (lcc ? 3.0 : 3.8))); // Max ~11 mins for 180pax
                        int cargoDur = Math.Max(180, (int)(paxCount * 3.5));

                        _groundOpsManager.Services.Add(new GroundService { Name = "Deboarding", TotalDurationSec = deboardDur, StatusMessage = "Pending", State = GroundServiceState.NotStarted, ElapsedSec = 0, RequiresManualStart = true, IsAvailable = true });
                        _groundOpsManager.Services.Add(new GroundService { Name = "Cargo Unloading", TotalDurationSec = cargoDur, StatusMessage = "Pending", State = GroundServiceState.NotStarted, ElapsedSec = 0, RequiresManualStart = true, IsAvailable = true });
                        _groundOpsManager.StartOps();

                        if (_rotationQueue.Count == 0)
                        {
                            // Bug 37 Fix: Build a perfectly blank response template for Leg 2 to await SimBrief data.
                            _currentResponse = new SimBriefResponse {
                                Origin = _currentResponse?.Destination ?? new FlightSupervisor.UI.Models.SimBrief.AirportInfo { IcaoCode = "----" },
                                Destination = new FlightSupervisor.UI.Models.SimBrief.AirportInfo { IcaoCode = "----", Name = "AWAITING NEW PLAN" },
                                General = new FlightSupervisor.UI.Models.SimBrief.GeneralInfo { Route = "WAITING FOR SIMBRIEF DATA", FlightNumber = "XXXX", Airline = _currentResponse?.General?.Airline ?? "" },
                                Aircraft = new FlightSupervisor.UI.Models.SimBrief.AircraftInfo { IcaoCode = _currentResponse?.Aircraft?.IcaoCode ?? "", BaseType = _currentResponse?.Aircraft?.BaseType ?? "", Reg = _currentResponse?.Aircraft?.Reg ?? "" },
                                Weights = new FlightSupervisor.UI.Models.SimBrief.WeightsInfo { BlockFuel = "0", PaxCount = "0" },
                                Fuel = new FlightSupervisor.UI.Models.SimBrief.FuelInfo { PlanRamp = "0" },
                                Times = new FlightSupervisor.UI.Models.SimBrief.TimesInfo { SchedIn = "0", SchedOut = "0" },
                                IsDummy = true
                            };

                            SendToWeb(new
                            {
                                type = "flightPlan",
                                hasSimBrief = false,
                                flightPlan = new
                                {
                                    origin = _currentResponse.Origin.IcaoCode,
                                    destination = _currentResponse.Destination.IcaoCode,
                                    airline = _currentResponse.General.Airline,
                                    flightNumber = _currentResponse.General.FlightNumber,
                                    aircraft = _currentResponse.Aircraft.IcaoCode,
                                    pax = "0",
                                    blockFuel = "0",
                                    initialAlt = "",
                                    route = _currentResponse.General.Route
                                }
                            });
                            
                            string simUser = _profileManager.CurrentProfile.SimBriefUsername ?? "";
                            if (!string.IsNullOrEmpty(simUser)) {
                                _ = FetchFlightPlan(simUser, false, null, _profileManager.CurrentProfile.WeatherSource ?? "simbrief", false);
                            }
                        }
                        else
                        {
                            _currentResponse = _rotationQueue[0];
                            SendToWeb(new
                            {
                                type = "flightPlan",
                                hasSimBrief = true,
                                flightPlan = new
                                {
                                    origin = _currentResponse?.Origin?.IcaoCode ?? "----",
                                    destination = _currentResponse?.Destination?.IcaoCode ?? "----",
                                    airline = _currentResponse?.General?.Airline ?? "",
                                    flightNumber = _currentResponse?.General?.FlightNumber ?? "",
                                    aircraft = _currentResponse?.Aircraft?.IcaoCode ?? "",
                                    pax = _currentResponse?.Weights?.PaxCount ?? "0",
                                    blockFuel = _currentResponse?.Weights?.BlockFuel ?? "0",
                                    initialAlt = _currentResponse?.General?.InitialAlt ?? "",
                                    route = _currentResponse?.General?.Route ?? ""
                                }
                            });
                        }
                        
                    }
                    SendToWeb(new { type = "phaseUpdate", phase = phase.ToString(), 
                                    sessionFlightsCompleted = _cabinManager.SessionFlightsCompleted,
                                    aobt = _aobt != null ? _aobt.Value.ToString("HH:mm") + "z" : null, 
                                    aibt = _aibt != null ? _aibt.Value.ToString("HH:mm") + "z" : null,
                                    aobtUnix = _aobt != null ? new DateTimeOffset(_aobt.Value).ToUnixTimeSeconds() : (long?)null,
                                    aibtUnix = _aibt != null ? new DateTimeOffset(_aibt.Value).ToUnixTimeSeconds() : (long?)null });
                });
            };
            // _phaseManager.OnPenaltyTriggered enlevé d'ici car déjà géré par SuperScoreManager (OnScoreChanged)
            _phaseManager.OnFoMessage += (file, msg) => {
                Dispatcher.Invoke(() => {
                    _audioEngine?.PlayExactAsFO($"EN_FO_Lucie/{file}", msg);
                    SendToWeb(new { type = "flightUpdate", message = $"[FO] {msg}" });
                });
            };
            
            _phaseManager.OnTouchdown += (fpm) => {
                _cabinManager?.ProcessLandingImpact(fpm);
            };



            _simConnectService = new SimConnectService();
            
            _crisisManager = new CrisisManager(_phaseManager, _simConnectService);
            _crisisManager.OnCrisisTriggered += crisis => {
                if (crisis == CrisisType.UnrulyPassenger) {
                    _cabinManager?.TriggerIncomingCabinCall("UnrulyPassenger", "TO_FD/Incoming_Calls/UnrulyPassenger", "Captain, we have a passenger who is extremely drunk and disruptive. We need you to be aware.");
                }
                Dispatcher.Invoke(() => SendToWeb(new { type = "crisisTriggered", crisisType = crisis.ToString() }));
            };
            
            _crisisManager.OnCabinMessage += (level, msg) => {
                Dispatcher.Invoke(() => SendToWeb(new { type = "cabinLog", level = level, message = msg }));
            };
            _crisisManager.OnCrisisResolved += (crisis, success, isManual) => {
                if (!isManual && crisis == CrisisType.MedicalEmergency)
                {
                    _scoreManager.AddScore(-500, "Medical Emergency Ignored", ScoreCategory.PassengerExperience);
                    Dispatcher.Invoke(() => SendToWeb(new { type = "cabinLog", level = "red", message = "[PNC] We had a medical emergency and you ignored it. The passenger is in critical condition." }));
                }
                else if (!isManual && crisis == CrisisType.UnrulyPassenger)
                {
                    _scoreManager.AddScore(-200, "Unruly Passenger Ignored", ScoreCategory.PassengerExperience);
                    _cabinManager.ApplyComfortImpact(-50);
                    Dispatcher.Invoke(() => SendToWeb(new { type = "cabinLog", level = "red", message = "[PNC] The unruly passenger escalated because you did nothing. Cabin comfort is plummeting." }));
                }
                else if (crisis == CrisisType.Depressurization)
                {
                    if (success)
                    {
                        _scoreManager.AddScore(250, "Emergency Descent Executed", ScoreCategory.Airmanship);
                        Dispatcher.Invoke(() => SendToWeb(new { type = "cabinLog", level = "cyan", message = "[PNC] We are below 10,000 ft. Passengers are breathing normally again. Great job." }));
                    }
                    else
                    {
                        _scoreManager.AddScore(-1000, "Depressurization Ignored", ScoreCategory.Airmanship);
                        _cabinManager.ApplyComfortImpact(-100);
                        Dispatcher.Invoke(() => SendToWeb(new { type = "cabinLog", level = "red", message = "[CRITICAL] Oxygen masks are depleted! Passengers are losing consciousness!" }));
                    }
                }
                Dispatcher.Invoke(() => SendToWeb(new { type = "crisisResolved", crisisType = crisis.ToString(), success = success }));
            };

            _scoreManager = new SuperScoreManager(_phaseManager, _simConnectService);
            _flowTrackerService = new FlowTrackerService(_simConnectService, _phaseManager, _scoreManager);
            _scoreFlowEvaluator = new ScoreFlowEvaluator(_flowTrackerService, _phaseManager, _scoreManager, _cabinManager);
            _scoreManager.OnScoreChanged += (score, delta, reason) => {
                Dispatcher.Invoke(() => SendToWeb(new { 
                    type = "scoreUpdate", 
                    score = score, 
                    delta = delta, 
                    msg = reason,
                    flightPhaseFlows = _scoreManager.FlightPhaseFlowsPoints,
                    communication = _scoreManager.CommunicationPoints,
                    airmanship = _scoreManager.AirmanshipPoints,
                    maintenance = _scoreManager.MaintenancePoints,
                    abnormalOperations = _scoreManager.AbnormalOperationsPoints,
                    passengerExperience = _scoreManager.PassengerExperiencePoints
                }));
            };

            _wearAndTearManager = new WearAndTearManager(_simConnectService, _airframeManager);

            _cabinManager.OnOperationBonusTriggered += (amount, msg) => {
                _scoreManager.AddScore(amount, msg, ScoreCategory.FlightPhaseFlows);
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
                if (_lastLogGearDown != null && _lastLogGearDown != gd) 
                    _scoreManager?.AddScore(0, gd ? "Landing Gear DOWN" : "Landing Gear UP", ScoreCategory.FlightPhaseFlows);
                _lastLogGearDown = gd;
            };
            _simConnectService.OnRadioHeightReceived += rh => { _lastKnownRadioHeight = rh; };
            _simConnectService.OnGroundSpeedReceived += gs => { _lastKnownGroundSpeed = gs; };
            _simConnectService.OnAirspeedReceived += ias => { _lastKnownAirspeed = ias; };
            _simConnectService.OnParkingBrakeReceived += pb => { 
                _isParkingBrakeSet = pb; 
                if (_lastLogParkingBrake != null && _lastLogParkingBrake != pb) 
                    _scoreManager?.AddScore(0, pb ? "Parking Brake SET" : "Parking Brake RELEASED", ScoreCategory.FlightPhaseFlows);
                _lastLogParkingBrake = pb;
            };
            _simConnectService.OnFlapsReceived += flaps => {
                if (_lastLogFlaps != null && _lastLogFlaps != flaps) 
                    _scoreManager?.AddScore(0, $"Flaps Position Changed -> {flaps}", ScoreCategory.FlightPhaseFlows);
                _lastLogFlaps = flaps;
                _phaseManager.FlapsPosition = flaps;
            };
            _simConnectService.OnAutopilotReceived += ap => {
                if (_lastLogAutopilot != null && _lastLogAutopilot != ap) 
                    _scoreManager?.AddScore(0, ap ? "Autopilot ENGAGED" : "Autopilot DISENGAGED", ScoreCategory.FlightPhaseFlows);
                _lastLogAutopilot = ap;
                _phaseManager.UpdateAutopilot(ap);
            };
            _simConnectService.OnAutothrustReceived += at => {
                if (_lastLogAutothrust != null && _lastLogAutothrust != at) 
                    _scoreManager?.AddScore(0, at ? "Autothrust ARMED" : "Autothrust DISENGAGED", ScoreCategory.FlightPhaseFlows);
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
            _simConnectService.OnLightBeaconReceived += l => { _phaseManager.IsBeaconLightOn = l; if (_lastLogLightBeacon != null && _lastLogLightBeacon != l) _scoreManager?.AddScore(0, l ? "Beacon Lights ON" : "Beacon Lights OFF", ScoreCategory.FlightPhaseFlows); _lastLogLightBeacon = l; };
            _simConnectService.OnLightStrobeReceived += l => { _phaseManager.IsStrobeLightOn = l; if (_lastLogLightStrobe != null && _lastLogLightStrobe != l) _scoreManager?.AddScore(0, l ? "Strobe Lights ON" : "Strobe Lights OFF", ScoreCategory.FlightPhaseFlows); _lastLogLightStrobe = l; };
            _simConnectService.OnFenixStrobeStateChanged += s => { _phaseManager.FenixStrobeLight = s; };
            _simConnectService.OnLightNavReceived += l => { if (_lastLogLightNav != null && _lastLogLightNav != l) _scoreManager?.AddScore(0, l ? "Nav Lights ON" : "Nav Lights OFF", ScoreCategory.FlightPhaseFlows); _lastLogLightNav = l; };
            _simConnectService.OnLightTaxiReceived += l => { 
                _phaseManager.IsTaxiLightOn = l;
                if (_lastLogLightTaxi != null && _lastLogLightTaxi != l) _scoreManager?.AddScore(0, l ? "Taxi Lights ON" : "Taxi Lights OFF", ScoreCategory.FlightPhaseFlows); 
                _lastLogLightTaxi = l; 
            };
            _simConnectService.OnSimOnGroundReceived += g => { _phaseManager.IsOnGround = g; };
            _simConnectService.OnVerticalSpeedReceived += vs => { _phaseManager.VerticalSpeed = vs; };
            _simConnectService.OnGForceReceived += gf => { _phaseManager.GForce = gf; };
            _simConnectService.OnAircraftTitleReceived += t => { _phaseManager.AircraftTitle = t; };
            _simConnectService.OnHeadingReceived += h => { _phaseManager.UpdateHeading(h); };
            _simConnectService.OnWindReceived += (wd, wv) => { _phaseManager.UpdateWind(wd, wv); };
            _simConnectService.OnPositionReceived += (lat, lon) => { _phaseManager.UpdatePosition(lat, lon); };
            _simConnectService.OnNavigationReceived += (locErr, gpsErr, hasLoc) => { _phaseManager.UpdateNavigation(locErr, gpsErr, hasLoc); };
            _simConnectService.OnEngineCombustionReceived += (eng1, eng2) => {
                _phaseManager.UpdateEngineCombustion(eng1, eng2);
                _cabinManager.AreEnginesRunning = eng1 || eng2;
            };
            _simConnectService.OnEngineN1Received += (eng1n1, eng2n1) => {
                _phaseManager.Eng1N1 = eng1n1;
                _phaseManager.Eng2N1 = eng2n1;
            };
            _simConnectService.OnDoorsReceived += (mainDoor, jetway) => {
                _phaseManager.IsMainDoorOpen = mainDoor;
                _phaseManager.IsJetwayConnected = jetway;
            };
            _simConnectService.OnGsxBoardingStateReceived += (b, db) => {
                _phaseManager.GsxBoardingState = b;
                _phaseManager.GsxDeboardingState = db;
                TryAutoStartGsxService("Boarding", b);
                TryAutoStartGsxService("Deboarding", db);
            };
            _simConnectService.OnGsxRefuelingStateReceived += (r) => {
                _phaseManager.GsxRefuelingState = r;
                TryAutoStartGsxService("Refueling", r);
            };
            _simConnectService.OnGsxCateringStateReceived += (c) => {
                _phaseManager.GsxCateringState = c;
                TryAutoStartGsxService("Catering", c);
            };
            _simConnectService.OnLightLandingReceived += l => { 
                _phaseManager.IsLandingLightOn = l;
                if (_lastLogLightLanding != null && _lastLogLightLanding != l) 
                    _scoreManager?.AddScore(0, l ? "Landing Lights ON" : "Landing Lights OFF", ScoreCategory.FlightPhaseFlows); 
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

                if (_lastLogSeatbelts != null && _lastLogSeatbelts != sb) 
                {
                    _scoreManager?.AddScore(0, sb ? "Fasten Seatbelts ON" : "Fasten Seatbelts OFF", ScoreCategory.FlightPhaseFlows);
                }
                _lastLogSeatbelts = sb;
            };

            _simConnectService.OnEngineSwitchesChanged += (mode, m1, m2) => {
                _isFenixEng1MasterOn = m1;
                _isFenixEng2MasterOn = m2;
            };

            _simConnectService.OnApuStateChanged += (mst, start, bleed) => {
                _phaseManager.FenixApuMaster = mst;
                _phaseManager.FenixApuStart = start;
                _phaseManager.FenixApuBleed = bleed;
                _cabinManager.IsApuRunning = mst;
                
                if (_lastLogApuMaster != null && _lastLogApuMaster != mst) 
                    _scoreManager?.AddScore(0, mst ? "APU Master SW ON" : "APU Master SW OFF", ScoreCategory.FlightPhaseFlows);
                    
                if (_lastLogApuStart != null && _lastLogApuStart != start) 
                    _scoreManager?.AddScore(0, start ? "APU Start SW ON" : "APU Start SW OFF", ScoreCategory.FlightPhaseFlows);
                    
                if (_lastLogApuBleed != null && _lastLogApuBleed != bleed)
                    _scoreManager?.AddScore(0, bleed ? "APU Bleed SW ON" : "APU Bleed SW OFF", ScoreCategory.FlightPhaseFlows);
                
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
                var locTimeStr = _cabinManager.CurrentSimLocalTime != DateTime.MinValue ? _cabinManager.CurrentSimLocalTime.ToString("HH:mm") : "--:--";
                var locDateStr = _cabinManager.CurrentSimLocalTime != DateTime.MinValue ? _cabinManager.CurrentSimLocalTime.ToString("dd/MM/yyyy") : "--/--/----";
                Dispatcher.Invoke(() => SendToWeb(new { type = "simTime", time = time.ToString("HH:mm") + "z", localTime = locTimeStr, date = time.ToString("dd/MM/yyyy"), localDate = locDateStr, rawUnix = ((DateTimeOffset)time).ToUnixTimeSeconds() }));
            };

            _simConnectService.OnSimLocalTimeReceived += localTime => {
                _cabinManager.CurrentSimLocalTime = localTime;
            };

            _simConnectService.OnAmbientTemperatureReceived += temp => {
                _cabinManager.CurrentAmbientTemperature = temp;
            };

            _simConnectService.OnFuelTotalReceived += fuel => {
                if (fuel > 0)
                {
                    // Protection against WASM telemetry blackouts during TimeWarp:
                    // If fuel drops instantly by an impossible amount (> 500kg in <1s), ignore the tick.
                    if (_currentFobKg > 500 && (_currentFobKg - fuel) > 500)
                    {
                        return;
                    }

                    _currentFobKg = fuel;
                    _ghostFuelTrackerActive = false; // We have positive native fuel, disable ghost tracker safely.
                }

                // For the very first leg, if the user hasn't yet validated the load sheet (locked it),
                // continuously track the simulator's live fuel as the "Initial" FOB so it populates correctly
                // even if MSFS connects AFTER the user fetches the flight plan.
                if (_cabinManager != null && _cabinManager.SessionFlightsCompleted == 0 && _groundOpsManager != null && !_groundOpsManager.IsFuelSheetValidated)
                {
                    if (fuel > 0)
                    {
                        if (_cabinManager.StateOfAircraft.InitialFobKg == 0) // log the first discovery attempt continuously
                        {
                            Dispatcher.InvokeAsync(() => {
                               SendToWeb(new { type = "log", message = $"[SYSTEM] Lien Carburant msfs/fenix rétabli: {fuel} KG." });
                            });
                        }
                        _cabinManager.StateOfAircraft.InitialFobKg = fuel;
                    }
                }
            };

            _simConnectService.OnEngineFuelFlowReceived += (eng1, eng2) => {
                if (_ghostFuelTrackerActive)
                {
                    DateTime now = DateTime.UtcNow;
                    if (_lastFuelFlowUpdate != DateTime.MinValue)
                    {
                        double deltaSec = (now - _lastFuelFlowUpdate).TotalSeconds;
                        if (deltaSec > 0 && deltaSec < 5) // normal tick
                        {
                            bool isWasm = _simConnectService?.IsWasmOverriding ?? false;
                            bool eng1Running = isWasm ? _isFenixEng1MasterOn : (_cabinManager?.AreEnginesRunning ?? false);
                            bool eng2Running = isWasm ? _isFenixEng2MasterOn : (_cabinManager?.AreEnginesRunning ?? false);

                            double flow1 = eng1Running ? Math.Max(0, eng1) * 0.453592 / 3600.0 : 0.0; 
                            double flow2 = eng2Running ? Math.Max(0, eng2) * 0.453592 / 3600.0 : 0.0;
                            
                            // A320 APU nominal fuel flow without pack demand is around 130 kg/hr
                            double apuFlow = (_cabinManager?.IsApuRunning ?? false) ? (130.0 / 3600.0) : 0.0;
                            
                            double totalConsumedKg = (flow1 + flow2 + apuFlow) * deltaSec;

                            if (totalConsumedKg > 0)
                            {
                                _virtualFobKg -= totalConsumedKg;
                                if (_virtualFobKg < 0) _virtualFobKg = 0;
                                _currentFobKg = Math.Round(_virtualFobKg);
                            }
                        }
                    }
                    _lastFuelFlowUpdate = now;
                }
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
            var appDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor");
            if (!System.IO.Directory.Exists(appDataFolder)) System.IO.Directory.CreateDirectory(appDataFolder);
            MainWebView.CoreWebView2.SetVirtualHostNameToFolderMapping("fsv.local", appDataFolder, CoreWebView2HostResourceAccessKind.Allow);
            MainWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            
            // --- SIMBRIEF INTEGRATION: HEADER BYPASS & NAVIGATION MONITORING ---
            MainWebView.CoreWebView2.AddWebResourceRequestedFilter("https://dispatch.simbrief.com/*", CoreWebView2WebResourceContext.All);
            MainWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            
            MainWebView.CoreWebView2.FrameNavigationStarting += (s, args) => {
                CheckSimbriefNavigation(args.Uri);
            };
            MainWebView.CoreWebView2.NavigationStarting += (s, args) => {
                CheckSimbriefNavigation(args.Uri);
            };
            // ------------------------------------------------------------------

            MainWebView.CoreWebView2.Navigate("https://app.local/index.html");

            // Navigation completed is now handled securely inside uiReady IPC message
        }

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void CheckSimbriefNavigation(string uri)
        {
            if (string.IsNullOrEmpty(uri)) return;
            if (uri.Contains("dispatch.simbrief.com/options/briefing") || uri.Contains("/briefing"))
            {
                Dispatcher.Invoke(() => {
                    SendToWeb(new { type = "simbriefPlanReady" });
                });
            }
        }

        private async void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (e.Request.Uri.Contains("dispatch.simbrief.com"))
            {
                var deferral = e.GetDeferral();
                try 
                {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var request = new System.Net.Http.HttpRequestMessage(new System.Net.Http.HttpMethod(e.Request.Method), e.Request.Uri);
                        
                        // Copy headers
                        foreach (var header in e.Request.Headers)
                        {
                            if (!header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }

                        if (e.Request.Content != null)
                        {
                            var ms = new System.IO.MemoryStream();
                            e.Request.Content.CopyTo(ms);
                            ms.Position = 0;
                            request.Content = new System.Net.Http.StreamContent(ms);
                            foreach (var header in e.Request.Headers.Where(h => h.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)))
                                request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }

                        var response = await client.SendAsync(request);
                        var responseStream = await response.Content.ReadAsStreamAsync();
                        
                        var headersString = "";
                        foreach (var header in response.Headers)
                        {
                            if (header.Key.Equals("X-Frame-Options", StringComparison.OrdinalIgnoreCase) || 
                                header.Key.Equals("Content-Security-Policy", StringComparison.OrdinalIgnoreCase))
                                continue;
                            headersString += $"{header.Key}: {string.Join(", ", header.Value)}\r\n";
                        }
                        foreach (var header in response.Content.Headers)
                        {
                            headersString += $"{header.Key}: {string.Join(", ", header.Value)}\r\n";
                        }

                        e.Response = MainWebView.CoreWebView2.Environment.CreateWebResourceResponse(responseStream, (int)response.StatusCode, response.ReasonPhrase, headersString);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Simbrief Proxy Error: {ex.Message}");
                }
                finally
                {
                    deferral.Complete();
                }
            }
        }

        private async void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            await ProcessWebMessage(e.WebMessageAsJson, MainWebView.CoreWebView2, this);
        }

        private void OpenFuelSheetWindow()
        {
            if (_fuelSheetWindow != null)
            {
                _fuelSheetWindow.Activate();
                return;
            }
            try
            {
                var fuelWin = new Window
                {
                    Title = "Fuel Dispatch",
                    Width = 480,
                    Height = 780,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#141414"),
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.CanResize
                };
                System.Windows.Shell.WindowChrome.SetWindowChrome(fuelWin, new System.Windows.Shell.WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new System.Windows.Thickness(8), GlassFrameThickness = new System.Windows.Thickness(0), CornerRadius = new System.Windows.CornerRadius(0), UseAeroCaptionButtons = false });
                
                fuelWin.Closed += (s, ev) => {
                    _fuelSheetWebView = null;
                    _fuelSheetWindow = null;
                };

                var webView = new Microsoft.Web.WebView2.Wpf.WebView2();
                webView.Margin = new System.Windows.Thickness(0);
                _fuelSheetWebView = webView;
                _fuelSheetWindow = fuelWin;
                fuelWin.Content = webView;
                
                fuelWin.Show();

                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FlightSupervisorFuelSheet"));
                        await webView.EnsureCoreWebView2Async(env);
                        webView.CoreWebView2.SetVirtualHostNameToFolderMapping("app.local", System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"), Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
                        
                        webView.CoreWebView2.WebMessageReceived += async (s, ev) => {
                            await ProcessWebMessage(ev.WebMessageAsJson, webView.CoreWebView2, fuelWin);
                        };

                        webView.CoreWebView2.Navigate("https://app.local/fuelsheet_window.html");
                        
                        // Send the complete flight data for the Fuel/Load Sheet window
                        webView.CoreWebView2.NavigationCompleted += (s, ev) => {
                            var targetRes = _phaseManager.CurrentPhase == FlightPhase.Turnaround && _rotationQueue.Count > 0 
                                ? _rotationQueue[0] 
                                : _currentResponse;
                            int effectiveLegIndex = _cabinManager.SessionFlightsCompleted;
                            bool isVal = _phaseManager.CurrentPhase == FlightPhase.Turnaround 
                                ? false 
                                : (_groundOpsManager?.IsFuelSheetValidated ?? false);
                            
                            var msgObj = new {
                                type = "initFuelSheet",
                                blockFuel = targetRes?.Fuel?.PlanRamp ?? "0",
                                fob = _currentFobKg,
                                legIndex = effectiveLegIndex,
                                isValidated = isVal,
                                flightData = targetRes
                            };
                            webView.CoreWebView2.PostWebMessageAsJson(System.Text.Json.JsonSerializer.Serialize(msgObj));
                        };
                    });
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error opening Fuel Sheet Window: " + ex.Message);
            }
        }

        private void OpenAirframeWindow()
        {
            if (_airframeWindow != null)
            {
                _airframeWindow.Activate();
                return;
            }
            try
            {
                var afWin = new Window
                {
                    Title = "Airframe Tech Log",
                    Width = 1100,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#141414"),
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.CanResize
                };
                System.Windows.Shell.WindowChrome.SetWindowChrome(afWin, new System.Windows.Shell.WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new System.Windows.Thickness(8), GlassFrameThickness = new System.Windows.Thickness(0), CornerRadius = new System.Windows.CornerRadius(0), UseAeroCaptionButtons = false });
                
                afWin.Closed += (s, ev) => {
                    _airframeWebView = null;
                    _airframeWindow = null;
                };

                var webView = new Microsoft.Web.WebView2.Wpf.WebView2();
                webView.Margin = new System.Windows.Thickness(0);
                _airframeWebView = webView;
                _airframeWindow = afWin;
                
                afWin.Content = new System.Windows.Controls.Border {
                    BorderThickness = new System.Windows.Thickness(4),
                    BorderBrush = System.Windows.Media.Brushes.Transparent,
                    Background = System.Windows.Media.Brushes.Transparent,
                    Child = webView
                };
                
                afWin.Show();

                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FlightSupervisorAirframe"));
                        await webView.EnsureCoreWebView2Async(env);
                        webView.CoreWebView2.SetVirtualHostNameToFolderMapping("app.local", System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"), Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
                        
                        webView.CoreWebView2.WebMessageReceived += async (s, ev) => {
                            await ProcessWebMessage(ev.WebMessageAsJson, webView.CoreWebView2, afWin);
                        };

                        webView.CoreWebView2.Navigate("https://app.local/airframe_window.html");
                        
                        webView.CoreWebView2.NavigationCompleted += async (s, ev) => {
                            await System.Threading.Tasks.Task.Delay(500);
                            try {
                                string reg = _currentResponse?.Aircraft?.Reg ?? "F-HBNJ";
                                string btype = _currentResponse?.Aircraft?.BaseType ?? "A320neo";
                                string op = _currentResponse?.General?.Airline ?? "True Airmanship";
                                string icao = _currentResponse?.Origin?.IcaoCode ?? "LFPG";
                                var state = _airframeManager.GetOrCreateAirframe(reg, btype, op, icao);

                                // Auto-populate technical specs from SimBrief if available
                                if (_currentResponse?.Aircraft?.ExtensionData != null)
                                {
                                    if (state.EngineType == "Unknown" && _currentResponse.Aircraft.ExtensionData.TryGetValue("engines", out var engType)) state.EngineType = engType.ToString();
                                }
                                if (state.MaxFuelCapacity == 0 && _currentResponse?.Fuel?.ExtensionData != null && _currentResponse.Fuel.ExtensionData.TryGetValue("max_tanks", out var maxTanks) && double.TryParse(maxTanks.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double mf)) state.MaxFuelCapacity = mf;
                                
                                if (_currentResponse?.Aircraft?.MaxPassengers != null && state.MaxPassengers == 0 && int.TryParse(_currentResponse.Aircraft.MaxPassengers, out int mpax))
                                {
                                    state.MaxPassengers = mpax;
                                }

                                _airframeManager.SyncLocation(state, icao);
                                if (_currentResponse?.Weights?.ExtensionData != null)
                                {
                                    if (state.MaxTakeoffWeight == 0 && _currentResponse.Weights.ExtensionData.TryGetValue("max_tow", out var maxTow) && double.TryParse(maxTow.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double mtow)) state.MaxTakeoffWeight = mtow;
                                    if (state.MaxZeroFuelWeight == 0 && _currentResponse.Weights.ExtensionData.TryGetValue("max_zfw", out var maxZfw) && double.TryParse(maxZfw.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double mzfw)) state.MaxZeroFuelWeight = mzfw;
                                    if (state.MaxLandingWeight == 0 && _currentResponse.Weights.ExtensionData.TryGetValue("max_ldw", out var maxLdw) && double.TryParse(maxLdw.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double mldw)) state.MaxLandingWeight = mldw;
                                    if (state.EmptyWeight == 0) {
                                        if (_currentResponse.Weights.ExtensionData.TryGetValue("oew", out var oew) && double.TryParse(oew.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double oewd)) state.EmptyWeight = oewd;
                                        else if (_currentResponse.Weights.ExtensionData.TryGetValue("bow", out var bow) && double.TryParse(bow.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double bowd)) state.EmptyWeight = bowd;
                                    }
                                    
                                    _airframeManager.SaveAirframe(state);
                                }

                                var msgObj = new { type = "initAirframeLog", airframeData = state };
                                webView.CoreWebView2.PostWebMessageAsJson(System.Text.Json.JsonSerializer.Serialize(msgObj));
                            } catch (Exception ex) {
                                var errObj = new { type = "initAirframeLog", airframeData = new { registration = "ERR: " + ex.Message }};
                                webView.CoreWebView2.PostWebMessageAsJson(System.Text.Json.JsonSerializer.Serialize(errObj));
                            }
                        };
                    });
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error opening Airframe Window: " + ex.Message);
            }
        }

        private async Task ProcessWebMessage(string json, Microsoft.Web.WebView2.Core.CoreWebView2 senderWebView, Window parentWindow)
        {
            try 
            {
                var doc = JsonDocument.Parse(json);
                var action = doc.RootElement.GetProperty("action").GetString();
                
                if (action == "drag") 
                {
                    Dispatcher.Invoke(() => {
                        var helper = new WindowInteropHelper(parentWindow);
                        ReleaseCapture();
                        SendMessage(helper.Handle, 0xA1, 2, 0);
                    });
                }
                else if (action == "ready" && parentWindow.Title == "Airframe Tech Log")
                {
                    string reg = _currentResponse?.Aircraft?.Reg ?? "F-HBNJ";
                    string btype = _currentResponse?.Aircraft?.BaseType ?? "A320neo";
                    string op = _currentResponse?.General?.Airline ?? "True Airmanship";
                    string icao = _currentResponse?.Origin?.IcaoCode ?? "LFPG";
                    var state = _airframeManager.GetOrCreateAirframe(reg, btype, op, icao);

                    if (_currentResponse?.Aircraft?.ExtensionData != null)
                    {
                        if (state.EngineType == "Unknown" && _currentResponse.Aircraft.ExtensionData.TryGetValue("engines", out var engType)) state.EngineType = engType.ToString();
                    }
                    if (state.MaxFuelCapacity == 0 && _currentResponse?.Fuel?.ExtensionData != null && _currentResponse.Fuel.ExtensionData.TryGetValue("max_tanks", out var maxTanks) && double.TryParse(maxTanks.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double mf)) state.MaxFuelCapacity = mf;

                    if (_currentResponse?.Aircraft?.MaxPassengers != null && state.MaxPassengers == 0 && int.TryParse(_currentResponse.Aircraft.MaxPassengers, out int mpax))
                    {
                        state.MaxPassengers = mpax;
                    }

                    _airframeManager.SyncLocation(state, icao);
                    if (_currentResponse?.Weights?.ExtensionData != null)
                    {
                        if (state.MaxTakeoffWeight == 0 && _currentResponse.Weights.ExtensionData.TryGetValue("max_tow", out var maxTow) && double.TryParse(maxTow.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double mtow)) state.MaxTakeoffWeight = mtow;
                        if (state.MaxZeroFuelWeight == 0 && _currentResponse.Weights.ExtensionData.TryGetValue("max_zfw", out var maxZfw) && double.TryParse(maxZfw.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double mzfw)) state.MaxZeroFuelWeight = mzfw;
                        if (state.MaxLandingWeight == 0 && _currentResponse.Weights.ExtensionData.TryGetValue("max_ldw", out var maxLdw) && double.TryParse(maxLdw.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double mldw)) state.MaxLandingWeight = mldw;
                        if (state.EmptyWeight == 0) {
                            if (_currentResponse.Weights.ExtensionData.TryGetValue("oew", out var oew) && double.TryParse(oew.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double oewd)) state.EmptyWeight = oewd;
                            else if (_currentResponse.Weights.ExtensionData.TryGetValue("bow", out var bow) && double.TryParse(bow.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double bowd)) state.EmptyWeight = bowd;
                        }
                        
                        _airframeManager.SaveAirframe(state);
                    }

                    var msgObj = new { type = "initAirframeLog", airframeData = state };
                    senderWebView.PostWebMessageAsJson(System.Text.Json.JsonSerializer.Serialize(msgObj));
                }
                else if (action == "requestManifest")
                {
                    var cMan = _cabinManager;
                    if (cMan?.CurrentManifest != null) {
                        var payload = new { type = "manifestUpdate", manifest = cMan.CurrentManifest };
                        senderWebView.PostWebMessageAsJson(JsonSerializer.Serialize(payload));
                    }
                }
                else if (action == "requestGroundOps")
                {
                    var gMan = _groundOpsManager;
                    if (gMan?.Services != null) {
                        var payload = new { type = "groundOps", services = gMan.Services, isDispatchSignedOff = gMan.IsFuelSheetValidated };
                        senderWebView.PostWebMessageAsJson(JsonSerializer.Serialize(payload));
                    }
                }
                else if (action == "answerPncCall")
                {
                    _cabinManager?.AnswerCabinCall();
                }
                else if (action == "openManifestWindow")
                {
                    if (_manifestWindow != null)
                    {
                        _manifestWindow.Activate();
                        return;
                    }
                    try
                    {
                        var manifestWin = new Window
                        {
                            Title = "Flight & Cabin Manifest",
                            Width = 1150,
                            Height = 850,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = this,
                            Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#141414"),
                            WindowStyle = WindowStyle.None
                        };
                        System.Windows.Shell.WindowChrome.SetWindowChrome(manifestWin, new System.Windows.Shell.WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new System.Windows.Thickness(8), GlassFrameThickness = new System.Windows.Thickness(0), CornerRadius = new System.Windows.CornerRadius(0), UseAeroCaptionButtons = false });
                        
                        manifestWin.Closed += (s, e) => {
                            FlightSupervisor.UI.Services.WindowSettingsManager.SaveWindowState(manifestWin, "ManifestWindow");
                            _manifestWebView = null;
                            _manifestWindow = null;
                        };

                        var webView = new Microsoft.Web.WebView2.Wpf.WebView2();
                        webView.Margin = new System.Windows.Thickness(6, 0, 6, 6);
                        _manifestWebView = webView;
                        _manifestWindow = manifestWin;
                        manifestWin.Content = webView;
                        
                        FlightSupervisor.UI.Services.WindowSettingsManager.ApplySettings(manifestWin, "ManifestWindow", 1150, 850);
                        manifestWin.Show();

                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await Dispatcher.InvokeAsync(async () =>
                            {
                                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FlightSupervisorManifest"));
                                await webView.EnsureCoreWebView2Async(env);

                                webView.CoreWebView2.SetVirtualHostNameToFolderMapping("app.local", System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"), Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
                                
                                webView.CoreWebView2.WebMessageReceived += async (s, e) => {
                                    await ProcessWebMessage(e.WebMessageAsJson, webView.CoreWebView2, manifestWin);
                                };

                                webView.CoreWebView2.Navigate("https://app.local/manifest.html");
                            });
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Error opening Manifest Window: " + ex.Message);
                    }
                }
                else if (action == "requestSimbriefReload")
                {
                    string simUser = _profileManager.CurrentProfile.SimBriefUsername ?? "";
                    if (!string.IsNullOrEmpty(simUser)) {
                        _ = FetchFlightPlan(simUser, false, null, _profileManager.CurrentProfile.WeatherSource ?? "simbrief", false).ContinueWith(t => {
                            Dispatcher.Invoke(() => {
                                if (parentWindow is Window w && w.Title == "Fuel Dispatch") {
                                    w.Close();
                                    OpenFuelSheetWindow(); // Forces state refresh for JS by respawning
                                }
                            });
                        });
                    } else {
                        SendToWeb(new { type = "fetchStatus", status = "error", message = "Missing SimBrief Username in Profile." });
                    }
                }
                else if (action == "openSandboxWindow")
                {
                    if (_sandboxWindow != null)
                    {
                        _sandboxWindow.Activate();
                        return;
                    }

                    try
                    {
                        var sandboxWin = new Window
                        {
                            Title = "Simulator Sandbox Mode",
                            Width = 600,
                            Height = 700,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = this,
                            Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#141414"),
                            WindowStyle = WindowStyle.None
                        };
                        System.Windows.Shell.WindowChrome.SetWindowChrome(sandboxWin, new System.Windows.Shell.WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new System.Windows.Thickness(8), GlassFrameThickness = new System.Windows.Thickness(0), CornerRadius = new System.Windows.CornerRadius(0), UseAeroCaptionButtons = false });
                        
                        sandboxWin.Closed += (s, e) => {
                            FlightSupervisor.UI.Services.WindowSettingsManager.SaveWindowState(sandboxWin, "SandboxWindow");
                            _sandboxWebView = null;
                            _sandboxWindow = null;
                        };

                        var webView = new Microsoft.Web.WebView2.Wpf.WebView2();
                        webView.Margin = new System.Windows.Thickness(6, 0, 6, 6);
                        _sandboxWebView = webView;
                        _sandboxWindow = sandboxWin;
                        sandboxWin.Content = webView;

                        FlightSupervisor.UI.Services.WindowSettingsManager.ApplySettings(sandboxWin, "SandboxWindow", 600, 700);
                        sandboxWin.Show();

                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await Dispatcher.InvokeAsync(async () =>
                            {
                                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FlightSupervisorSandbox"));
                                await webView.EnsureCoreWebView2Async(env);

                                webView.CoreWebView2.SetVirtualHostNameToFolderMapping("app.local", System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"), Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
                                
                                webView.CoreWebView2.WebMessageReceived += async (s, e) => {
                                    await ProcessWebMessage(e.WebMessageAsJson, webView.CoreWebView2, sandboxWin);
                                };

                                webView.CoreWebView2.Navigate("https://app.local/sandbox_window.html");
                            });
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Error opening Sandbox Window: " + ex.Message);
                    }
                }
                else if (action == "closeSandboxWindow")
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        _sandboxWindow?.Close();
                    });
                }
                else if (action == "openLogsWindow")
                {
                    if (_logsWindow != null)
                    {
                        _logsWindow.Activate();
                        return;
                    }
                    try
                    {
                        if (doc.RootElement.TryGetProperty("payload", out var pl))
                        {
                            if (pl.TryGetProperty("score", out var sProp)) _cachedLogsScore = sProp.GetString();
                            if (pl.TryGetProperty("history", out var hProp)) _cachedLogsHtml = hProp.GetString();
                        }

                        var logsWin = new Window
                        {
                            Title = "Flight Logs",
                            Width = 500,
                            Height = 750,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = this,
                            Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#141414"),
                            WindowStyle = WindowStyle.None
                        };
                        System.Windows.Shell.WindowChrome.SetWindowChrome(logsWin, new System.Windows.Shell.WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new System.Windows.Thickness(8), GlassFrameThickness = new System.Windows.Thickness(0), CornerRadius = new System.Windows.CornerRadius(0), UseAeroCaptionButtons = false });
                        
                        logsWin.Closed += (s, e) => {
                            FlightSupervisor.UI.Services.WindowSettingsManager.SaveWindowState(logsWin, "LogsWindow");
                            _logsWebView = null;
                            _logsWindow = null;
                        };

                        var webView = new Microsoft.Web.WebView2.Wpf.WebView2();
                        webView.Margin = new System.Windows.Thickness(6, 0, 6, 6);
                        _logsWebView = webView;
                        _logsWindow = logsWin;
                        logsWin.Content = webView;

                        FlightSupervisor.UI.Services.WindowSettingsManager.ApplySettings(logsWin, "LogsWindow", 650, 750);
                        logsWin.Show();

                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await Dispatcher.InvokeAsync(async () =>
                            {
                                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FlightSupervisorLogs"));
                                await webView.EnsureCoreWebView2Async(env);

                                webView.CoreWebView2.SetVirtualHostNameToFolderMapping("app.local", System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"), Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
                                
                                webView.CoreWebView2.WebMessageReceived += async (s, e) => {
                                    await ProcessWebMessage(e.WebMessageAsJson, webView.CoreWebView2, logsWin);
                                };

                                webView.CoreWebView2.Navigate("https://app.local/logs_window.html");
                            });
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error opening Logs Window: {ex.Message}");
                    }
                }
                else if (action == "logsWindowReady")
                {
                    if (_logsWebView?.CoreWebView2 != null)
                    {
                        _logsWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { 
                            type = "initLogs", 
                            score = _cachedLogsScore, 
                            history = _cachedLogsHtml 
                        }));
                    }
                }
                else if (action == "minimizeLogsWindow")
                {
                    Dispatcher.Invoke(() => { if (_logsWindow != null) _logsWindow.WindowState = WindowState.Minimized; });
                }
                else if (action == "maximizeLogsWindow")
                {
                    Dispatcher.Invoke(() => { 
                        if (_logsWindow != null) _logsWindow.WindowState = _logsWindow.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; 
                    });
                }
                else if (action == "closeLogsWindow")
                {
                    Dispatcher.Invoke(() => { if (_logsWindow != null) _logsWindow.Close(); });
                }
                else if (action == "openGroundOpsWindow")
                {
                    if (_groundOpsWindow != null)
                    {
                        _groundOpsWindow.Activate();
                        return;
                    }
                    try
                    {
                        var groundOpsWin = new Window
                        {
                            Title = "Ground Operations",
                            Width = 1200,
                            Height = 800,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = this,
                            Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#141414"),
                            WindowStyle = WindowStyle.None
                        };
                        System.Windows.Shell.WindowChrome.SetWindowChrome(groundOpsWin, new System.Windows.Shell.WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new System.Windows.Thickness(8), GlassFrameThickness = new System.Windows.Thickness(0), CornerRadius = new System.Windows.CornerRadius(0), UseAeroCaptionButtons = false });
                        
                        groundOpsWin.Closed += (s, e) => {
                            FlightSupervisor.UI.Services.WindowSettingsManager.SaveWindowState(groundOpsWin, "GroundOpsWindow");
                            _groundOpsWebView = null;
                            _groundOpsWindow = null;
                        };

                        var webView = new Microsoft.Web.WebView2.Wpf.WebView2();
                        webView.Margin = new System.Windows.Thickness(6, 0, 6, 6);
                        _groundOpsWebView = webView;
                        _groundOpsWindow = groundOpsWin;
                        groundOpsWin.Content = webView;
                        
                        FlightSupervisor.UI.Services.WindowSettingsManager.ApplySettings(groundOpsWin, "GroundOpsWindow", 1200, 800);
                        groundOpsWin.Show();

                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await Dispatcher.InvokeAsync(async () =>
                            {
                                var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(null, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FlightSupervisorGroundOps"));
                                await webView.EnsureCoreWebView2Async(env);

                                webView.CoreWebView2.SetVirtualHostNameToFolderMapping("app.local", System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot"), Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

                                webView.CoreWebView2.WebMessageReceived += async (s, e) => {
                                    await ProcessWebMessage(e.WebMessageAsJson, webView.CoreWebView2, groundOpsWin);
                                };

                                webView.CoreWebView2.Navigate("https://app.local/groundops_window.html");
                            });
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("Error opening Ground Ops Window: " + ex.Message);
                    }
                }
                else if (action == "openFuelSheetWindow")
                {
                    OpenFuelSheetWindow();
                }
                else if (action == "openAirframeWindow")
                {
                    OpenAirframeWindow();
                }
                else if (action == "openSimbriefWindow")
                {
                    try
                    {
                        var origStr = "";
                        var destStr = "";
                        var airlineStr = "";
                        var fltnumStr = "";
                        if (doc.RootElement.TryGetProperty("orig", out var origProp)) origStr = origProp.GetString() ?? "";
                        if (doc.RootElement.TryGetProperty("dest", out var destProp)) destStr = destProp.GetString() ?? "";
                        if (doc.RootElement.TryGetProperty("airline", out var airlineProp)) airlineStr = airlineProp.GetString() ?? "";
                        if (doc.RootElement.TryGetProperty("fltnum", out var fltnumProp)) fltnumStr = fltnumProp.GetString() ?? "";

                        var targetUrl = "https://dispatch.simbrief.com/options/custom";
                        if (!string.IsNullOrEmpty(origStr) && !string.IsNullOrEmpty(destStr))
                        {
                            targetUrl += $"?orig={origStr}&dest={destStr}";
                            if (!string.IsNullOrEmpty(airlineStr)) targetUrl += $"&airline={airlineStr}";
                            if (!string.IsNullOrEmpty(fltnumStr)) targetUrl += $"&fltnum={fltnumStr}";
                        }

                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = targetUrl,
                            UseShellExecute = true
                        });
                    }
                    catch (System.Exception ex)
                    {
                        System.Windows.MessageBox.Show("Error opening Simbrief: " + ex.Message);
                    }
                }
                else if (action == "acknowledgeDebrief")
                {
                    if (_rotationQueue.Count > 0)
                    {
                        Dispatcher.Invoke(() => {
                            SendToWeb(new { type = "log", message = $"[SYSTEM] Acknowledged Debrief. Rotation Leg {_cabinManager.SessionFlightsCompleted + 1} will start when turnaround is complete." });
                        });
                    }
                }
                else if (action == "cancelLastLeg")
                {
                    if (_rotationQueue.Count > 0)
                    {
                        _rotationQueue.RemoveAt(_rotationQueue.Count - 1);
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
                else if (action == "systemPause")
                {
                    if (_phaseManager != null)
                    {
                        bool newState = !_phaseManager.IsPaused;
                        //_simConnectService?.SetPause(newState);
                        if (newState)
                        {
                            _phaseManager.IsPaused = true;
                        }
                        else
                        {
                            _phaseManager.ResumeWithImmunity(5);
                        }
                        SendToWeb(new { type = "pauseStateUpdate", isPaused = newState });
                    }
                }
                else if (action == "timeSkip")
                {
                    if (doc.RootElement.TryGetProperty("minutes", out var minProp) && minProp.TryGetInt32(out int minutes))
                    {
                        ExecuteTimeSkip(minutes);
                    }
                }
                else if (action == "validateFuel")
                {
                    if (doc.RootElement.TryGetProperty("payload", out var payload))
                    {
                        string blockFuel = payload.TryGetProperty("blockFuel", out var bProp) ? bProp.GetString() ?? "0" : "0";
                        int legIndex = payload.TryGetProperty("legIndex", out var lProp) ? lProp.GetInt32() : 0;

                        if (_currentResponse != null && _currentResponse.General?.FlightNumber == "XXXX")
                        {
                            SendToWeb(new { type = "fuelValidationRejected", message = "Validation impossible : Pas de plan de vol. Veuillez rafraîchir depuis SimBrief avant de valider." });
                            return;
                        }

                        bool isFirstFlightValidation = _cabinManager.SessionFlightsCompleted == 0 && _phaseManager.CurrentPhase == FlightPhase.AtGate;
                        if (isFirstFlightValidation) legIndex = 0; // Force it to 0 for initial flight regardless of HTML hardcoding

                        // STORY 38: LEG VALIDATION GUARD (Anti-Cheat)
                        // We must prevent validating a strictly future leg before the current flight ends.
                        // legIndex represents the absolute session flight index.
                        // If legIndex > _cabinManager.SessionFlightsCompleted, it's a future unseen leg being previewed.
                        if (legIndex > _cabinManager.SessionFlightsCompleted && 
                            _phaseManager.CurrentPhase != FlightPhase.Arrived && 
                            _phaseManager.CurrentPhase != FlightPhase.Turnaround)
                        {
                            SendToWeb(new { 
                                type = "fuelValidationRejected", 
                                message = "Validation impossible : Vous ne pouvez pas signer la load sheet d'un vol futur tant que le vol actuel n'est pas arrivé à destination." 
                            });
                            return;
                        }

                        if (legIndex > 0)
                        {
                            var deboarding = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Deboarding");
                            if (deboarding != null && deboarding.State != GroundServiceState.Completed)
                            {
                                SendToWeb(new { 
                                    type = "fuelValidationRejected", 
                                    message = "Validation impossible : Le débarquement du vol précédent n'est pas terminé." 
                                });
                                return;
                            }
                        }



                        _groundOpsManager.IsFuelSheetValidated = true;

                        // -- GHOST FUEL TRACKER INITIATION --
                        // If simulator lacks native fuel telemetry, or is cold and dark,
                        // this will act as the master baseline and simulate the burn natively via Flow Integration.
                        _ghostFuelTrackerActive = true;
                        _virtualFobKg = double.TryParse(blockFuel, out double bf) ? bf : 3000.0;
                        _currentFobKg = Math.Round(_virtualFobKg);
                        
                        var targetResponse = _currentResponse;
                        if (legIndex > 0 && _rotationQueue.Count > 0)
                        {
                            targetResponse = _rotationQueue[0];
                        }

                        if (targetResponse?.Fuel != null)
                        {
                            targetResponse.Fuel.PlanRamp = Math.Round(_currentFobKg).ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                        // -----------------------------------
                        
                        // POINT 1.2: SILENT RELOAD SIMBRIEF DISPATCH FOR THE NEXT LEG BEFORE INITIALIZATION
                        if (legIndex > 0)
                        {
                            string simbriefUser = _profileManager.CurrentProfile.SimBriefUsername ?? "";
                            if (!string.IsNullOrEmpty(simbriefUser))
                            {
                                try {
                                    // Wait up to a few seconds, the UI spinner "Please Wait" handles the delay for the pilot.
                                    await FetchFlightPlan(simbriefUser, false, null, _profileManager.CurrentProfile.WeatherSource ?? "simbrief", false);
                                    
                                    // Refresh targetResponse in case it was updated by the fetch
                                    if (_rotationQueue.Count > 0)
                                    {
                                        targetResponse = _rotationQueue[0];
                                    }
                                } catch (Exception innerEx) {
                                    System.IO.File.AppendAllText("sync_debug.txt", $"\n[ERROR] FetchFlightPlan threw: {innerEx.Message}\n");
                                }
                            }
                        }

                        if (targetResponse != null)
                        {
                            _groundOpsManager.InitializeFromSimBrief(targetResponse, _cabinManager.SessionFlightsCompleted == 0 && _cabinManager.FirstFlightClean, _currentFobKg, _cabinManager.CabinCleanliness, _cabinManager.CateringCompletion, _cabinManager.WaterLevel, _cabinManager.WasteLevel, _nextSobtOverride);
                            if (_cabinManager.SessionFlightsCompleted == 0)
                            {
                                _groundOpsManager.Services.RemoveAll(x => x.Name == "Deboarding");
                            }
                            
                            SendToWeb(new { type = "groundOpsReady" });
                            SendToWeb(new { type = "groundOps", services = _groundOpsManager.Services, isDispatchSignedOff = true });
                        }
                        
                        // Let Dispatch know
                        SendToWeb(new { type = "cabinLog", level = "cyan", message = $"[DISPATCH] Final Load Sheet validated by Captain. Block Fuel confirmed at {blockFuel} kg." });
                        double bp = 0;
                        double.TryParse(blockFuel, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out bp);
                        SendToWeb(new { type = "fuelValidationSuccess", blockFuel = bp });

                        // Fermeture auto de la Modale détachable Fuel Sheet en s'assurant d'être sur le Dispatcher UI
                        Dispatcher.Invoke(() => {
                            try {
                                if (_fuelSheetWindow != null)
                                {
                                    _fuelSheetWindow.Close();
                                }
                            } catch (Exception) { /* Ignored */ }
                        });
                    }
                }
                else if (action == "prepareNextLeg")
                {
                    Dispatcher.Invoke(() => {
                        if (_phaseManager.CurrentPhase != FlightPhase.Turnaround && _phaseManager.CurrentPhase != FlightPhase.AtGate)
                        {
                            SendToWeb(new { type = "log", message = "[SYSTEM] Sequence Error: You can only prepare the next leg during Turnaround or At Gate." });
                            return;
                        }

                        bool tasksPending = _groundOpsManager.Services.Any(s => (s.Name == "Deboarding" || s.Name.StartsWith("Cargo")) && s.State != GroundServiceState.Completed && s.State != GroundServiceState.Skipped);
                        if (tasksPending)
                        {
                            SendToWeb(new { type = "log", message = "[SYSTEM] Sequence Error: Turnaround unloading (Deboarding & Cargo) must complete before preparing the next leg." });
                            return;
                        }

                        if (_rotationQueue.Count > 0)
                        {
                            LoadNextLeg();
                            // If the loaded leg is a real OFP (not a dummy), start Ground Ops
                            if (_currentResponse != null && _currentResponse.General?.FlightNumber != "DUMMY")
                            {
                                _groundOpsManager.StartOps();
                            }
                            
                            SendToWeb(new { type = "log", message = $"[SYSTEM] Dispatch: Rotation Leg {_cabinManager.SessionFlightsCompleted + 1} initialized." });
                        }
                        else
                        {
                            SendToWeb(new { type = "log", message = "[SYSTEM] No more legs in rotation." });
                        }
                    });
                }
                else if (action == "updateAvatar")
                {
                    var payloadStr = doc.RootElement.GetProperty("payload").GetString();
                    var saveDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor");
                    if (!System.IO.Directory.Exists(saveDir)) System.IO.Directory.CreateDirectory(saveDir);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(saveDir, "ProfileAvatar.b64"), payloadStr);
                }
                else if (action == "updateProfileField")
                {
                    if (_profileManager != null && _profileManager.CurrentProfile != null)
                    {
                        if (doc.RootElement.TryGetProperty("field", out var fieldProp) && 
                            doc.RootElement.TryGetProperty("value", out var valProp))
                        {
                            var field = fieldProp.GetString() ?? "";
                            var val = valProp.GetString() ?? "";
                            switch (field)
                            {
                                case "CallSign": _profileManager.CurrentProfile.CallSign = val; break;
                                case "AvatarPosition": _profileManager.CurrentProfile.AvatarPosition = val; break;
                                case "FullName": 
                                    var parts = val.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Length > 0) _profileManager.CurrentProfile.FirstName = parts[0];
                                    if (parts.Length > 1) _profileManager.CurrentProfile.LastName = parts[1];
                                    else _profileManager.CurrentProfile.LastName = "";
                                    break;
                                case "HomeBaseIcao": _profileManager.CurrentProfile.HomeBaseIcao = val.ToUpper(); break;
                                case "CountryCode": _profileManager.CurrentProfile.CountryCode = val.ToUpper(); break;
                            }
                            _profileManager.SaveProfile();
                        }
                    }
                }

                else if (action == "fetchLogbook")
                {
                    var history = FlightSupervisor.UI.Services.FlightLogger.GetLogbook();
                    SendToWeb(new { type = "logbookData", history });
                }
                else if (action == "acarsWeatherRequest")
                {
                    _ = RefreshLiveWeatherAsync();
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
                            _scoreManager.AddScore(success ? 150 : -500, success ? "Medical Emergency Handled Promptly" : "Medical Emergency Handled Too Late", ScoreCategory.PassengerExperience);
                            SendToWeb(new { type = "cabinLog", level = success ? "cyan" : "red", message = success ? "[PNC] We've secured a doctor on board! The passenger has been stabilized." : "[PNC] It took too long, the passenger's condition has severely worsened." });
                            _crisisManager.ResolveCrisis(success);
                        }
                        else if (crisisTypeStr == "UnrulyPassenger")
                        {
                            success = elapsed < 300;
                            _scoreManager.AddScore(success ? 100 : -300, success ? "Unruly Passenger Restrained" : "Unruly Passenger Ignored", ScoreCategory.PassengerExperience);
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

                            if (choice.ComfortImpact != 0) _scoreManager.AddScore(choice.ComfortImpact, $"Event: {evt.Title}", ScoreCategory.PassengerExperience);
                            if (choice.SafetyImpact != 0) _scoreManager.AddScore(choice.SafetyImpact, $"Event: {evt.Title}", ScoreCategory.Airmanship);
                        }
                    }

                    // Resume operations
                    _groundOpsManager.IsPaused = false;
                }
                else if (action == "startService" || action == "startDeboarding" || action == "startBoarding")
                {
                    var serviceName = doc.RootElement.TryGetProperty("service", out var svcProp) ? svcProp.GetString() : null;
                    if (action == "startDeboarding") serviceName = "Deboarding";
                    if (action == "startBoarding") serviceName = "Boarding";

                    if (!string.IsNullOrEmpty(serviceName))
                    {
                        bool engOn = (_phaseManager.Eng1N1 >= 5 || _phaseManager.Eng2N1 >= 5);
                        bool beaconOn = _phaseManager.IsBeaconLightOn;
                        bool doorOpen = _phaseManager.IsMainDoorOpen || _phaseManager.IsJetwayConnected;
                        bool canStart = true;
                        string failReason = "";

                        // Physical constraints (Doors, Engines) removed because standard MSFS door SimVars
                        // fail to read correctly on third-party aircraft like the Fenix A320, permanently blocking ops.

                        if (canStart)
                        {
                            if (serviceName.Equals("Deboarding", StringComparison.OrdinalIgnoreCase)) _cabinManager.StartDeboarding();
                            _groundOpsManager.StartManualService(serviceName);
                        }
                        else
                        {
                            SendToWeb(new { type = "log", message = $"[SYSTEM] Rejected {serviceName} start: Physical safety violation ({failReason})." });
                            
                            var s = _groundOpsManager.Services.FirstOrDefault(x => x.Name.Contains(serviceName, StringComparison.OrdinalIgnoreCase));
                            if (s != null) s.StatusMessage = $"Blocked ({failReason})";
                            SendToWeb(new { type = "groundOps", services = _groundOpsManager.Services });
                        }
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
                    _scoreManager.AddScore(-50, "Time Warp Convenience Used", ScoreCategory.FlightPhaseFlows);
                }
                else if (action == "sandboxToggle")
                {
                    var key = doc.RootElement.GetProperty("key").GetString();
                    var val = doc.RootElement.GetProperty("value").GetBoolean();
                    
                    if (key == "Beacon")
                    {
                        _phaseManager.IsBeaconLightOn = val;
                        if (!val && _phaseManager.CurrentPhase == FlightPhase.Arrived)
                        {
                             _phaseManager.ForcePhase(FlightPhase.Turnaround);
                        }
                    }
                    else if (key == "Eng1") _phaseManager.Eng1Combustion = val;
                    else if (key == "Eng2") _phaseManager.Eng2Combustion = val;
                    else if (key == "Seatbelts") _phaseManager.IsSeatbeltsOn = val;
                    else if (key == "ParkingBrake") 
                    {
                        if (val)
                        {
                            _phaseManager.ForcePhase(FlightPhase.Arrived);
                        }
                    }
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
                        else if (targetPhase == FlightPhase.Turnaround)
                        {
                            if (_rotationQueue.Count > 0)
                            {
                                if (_currentResponse != null && _currentResponse.Times != null && long.TryParse(_currentResponse.Times.SchedIn, out long schedInUnix))
                                {
                                    var newTime = DateTimeOffset.FromUnixTimeSeconds(schedInUnix).UtcDateTime;
                                    simAdvanceSec = (int)(newTime - _currentSimTime).TotalSeconds;
                                }

                                if (_cabinManager.SessionFlightsCompleted == 0)
                                    _cabinManager.SessionFlightsCompleted++; // Mark first flight done
                                
                                // Push into Turnaround and add missing ground tasks manually
                                _groundOpsManager.IsFuelSheetValidated = false;
                                _groundOpsManager.Services.Clear();
                                
                                int paxCount = _cabinManager.PassengerManifest?.Count ?? 0;
                                bool lcc = _groundOpsManager.IsLowCost;
                                int deboardDur = Math.Max(120, (int)(paxCount * (lcc ? 3.0 : 3.8))); 
                                int cargoDur = Math.Max(180, (int)(paxCount * 3.5));

                                _groundOpsManager.Services.Add(new GroundService { Name = "Deboarding", TotalDurationSec = deboardDur, StatusMessage = "Pending", State = GroundServiceState.NotStarted, ElapsedSec = 0, RequiresManualStart = true, IsAvailable = true });
                                _groundOpsManager.Services.Add(new GroundService { Name = "Cargo Unloading", TotalDurationSec = cargoDur, StatusMessage = "Pending", State = GroundServiceState.NotStarted, ElapsedSec = 0, RequiresManualStart = true, IsAvailable = true });
                                _groundOpsManager.StartOps();
                            }
                        }

                        if (simAdvanceSec > 0)
                        {
                            _currentSimTime = _currentSimTime.AddSeconds(simAdvanceSec);
                            if (_cabinManager.CurrentSimLocalTime != DateTime.MinValue)
                                _cabinManager.CurrentSimLocalTime = _cabinManager.CurrentSimLocalTime.AddSeconds(simAdvanceSec);
                            
                            // Let the CabinManager consume resources
                            _cabinManager.FastForward(simAdvanceSec, targetPhase);
                            
                            SendToWeb(new { type = "cabinLog", level = "orange", message = $"[DEBUG] Simulated Time Jump: +{simAdvanceSec/60}m." });
                        }

                        _phaseManager.SetSimulationState(targetPhase);
                        SendToWeb(new { type = "cabinLog", level = "cyan", message = $"[DEBUG] Force transitioned phase to: {targetPhase}" });
                    }
                }
                else if (action == "generateDebugDump")
                {
                    GenerateAgentDebugLog();
                }
                else if (action == "markBug")
                {
                    FlightSupervisor.UI.Services.DebugLogger.MarkBug();
                    GenerateAgentDebugLog();
                    SendToWeb(new { type = "cabinLog", level = "purple", message = $"[DEBUG] BUG MARKER INJECTED IN LOGS" });
                }
                else if (action == "debugToggleSimulation")
                {
                    _phaseManager.IsSimulationMode = doc.RootElement.GetProperty("enabled").GetBoolean();
                    if (_phaseManager.IsSimulationMode)
                    {
                        if (_currentResponse != null && _currentResponse.Times != null && long.TryParse(_currentResponse.Times.SchedOut, out long schedOutUnix))
                        {
                            _currentSimTime = DateTimeOffset.FromUnixTimeSeconds(schedOutUnix).UtcDateTime.AddMinutes(-40);
                            SendToWeb(new { type = "cabinLog", level = "purple", message = $"[SYSTEM] Mock clock aligned to SOBT -40m: {_currentSimTime:HH:mm}Z" });
                        }
                        else
                        {
                            _currentSimTime = DateTime.UtcNow;
                        }
                        
                        try {
                            if (_currentResponse?.Origin?.PosLat != null && _currentResponse?.Origin?.PosLong != null &&
                                double.TryParse(_currentResponse.Origin.PosLat, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                                double.TryParse(_currentResponse.Origin.PosLong, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lon))
                            {
                                string tzIana = GeoTimeZone.TimeZoneLookup.GetTimeZone(lat, lon).Result;
                                TimeZoneInfo tzi = TimeZoneConverter.TZConvert.GetTimeZoneInfo(tzIana);
                                _cabinManager.CurrentSimLocalTime = TimeZoneInfo.ConvertTimeFromUtc(_currentSimTime, tzi);
                            } else {
                                _cabinManager.CurrentSimLocalTime = _currentSimTime;
                            }
                        } catch {
                            _cabinManager.CurrentSimLocalTime = _currentSimTime;
                        }

                        // Set the aircraft to a safe parked state immediately so Ground Ops can begin
                        _phaseManager.SetSimulationState(FlightPhase.AtGate);
                    }
                    else
                    {
                        _currentSimTime = DateTime.MinValue;
                    }
                    SendToWeb(new { type = "cabinLog", level = "purple", message = $"[SYSTEM] SIMULATION MODE: {(_phaseManager.IsSimulationMode ? "ENABLED" : "DISABLED")}" });
                }
                else if (action == "debugTimeSkip")
                {
                    if (doc.RootElement.TryGetProperty("minutes", out var minProp))
                    {
                        int min = minProp.GetInt32();
                        int simAdvanceSec = min * 60;
                        _currentSimTime = _currentSimTime.AddSeconds(simAdvanceSec);
                        if (_cabinManager.CurrentSimLocalTime != DateTime.MinValue)
                            _cabinManager.CurrentSimLocalTime = _cabinManager.CurrentSimLocalTime.AddSeconds(simAdvanceSec);
                        
                        _cabinManager.FastForward(simAdvanceSec, _phaseManager.CurrentPhase);
                        SendToWeb(new { type = "cabinLog", level = "orange", message = $"[DEBUG] Simulated Time Jump: +{min}m." });
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
                else if (action == "requestSimbriefReload")
                {
                    string username = _profileManager.CurrentProfile.SimBriefUsername ?? "";
                    if (!string.IsNullOrEmpty(username))
                    {
                        var weatherSource = _profileManager.CurrentProfile.WeatherSource ?? "simbrief";
                        _ = FetchFlightPlan(username, false, null, weatherSource, false);
                        SendToWeb(new { type = "log", message = "[SYSTEM] Reloading SimBrief Data..." });
                    }
                    else
                    {
                        SendToWeb(new { type = "fetchStatus", status = "error", message = "Missing SimBrief Username in Profile." });
                    }
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
                    
                    if (doc.RootElement.TryGetProperty("options", out var opts))
                    {
                        if (opts.TryGetProperty("groundSpeed", out var gsProp))
                        {
                            if (Enum.TryParse<GroundOpsSpeed>(gsProp.GetString(), true, out var speed))
                                _groundOpsManager.SpeedSetting = speed;
                        }
                        if (opts.TryGetProperty("groundProb", out var gpProp))
                        {
                            if (int.TryParse(gpProp.GetString(), out var prob))
                                _groundOpsManager.EventProbabilityPercent = prob;
                        }
                        if (opts.TryGetProperty("firstFlightClean", out var ffcProp))
                        {
                            bool isClean = true;
                            if (ffcProp.ValueKind == System.Text.Json.JsonValueKind.True) isClean = true;
                            else if (ffcProp.ValueKind == System.Text.Json.JsonValueKind.False) isClean = false;
                            else if (ffcProp.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                if (bool.TryParse(ffcProp.GetString(), out bool bClean)) isClean = bClean;
                                else if (ffcProp.GetString() == "true") isClean = true;
                                else isClean = false;
                            }
                            
                            _cabinManager.FirstFlightClean = isClean;
                            if (isClean)
                            {
                                _cabinManager.SessionFlightsCompleted = 0;
                            }
                        }
                    }
                    
                    var units = new FlightSupervisor.UI.Models.UnitPreferences();
                    if (opts.ValueKind != System.Text.Json.JsonValueKind.Undefined && opts.TryGetProperty("units", out var unitsProp))
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
                else if (action == "submitShellRotation")
                {
                    Dispatcher.Invoke(() => {
                        try 
                        {
                            if (doc.RootElement.TryGetProperty("payload", out var payloadProp) && payloadProp.TryGetProperty("legs", out var legsProp))
                            {
                                string logPath = "sync_debug.txt";
                                System.IO.File.AppendAllText(logPath, $"\n[{DateTime.Now}] submitShellRotation called. Legs payload found.\n");

                                _rotationQueue.Clear();
                                foreach (var item in legsProp.EnumerateArray())
                                {
                                    try
                                    {
                                        var rotData = item.GetProperty("data").GetRawText();
                                        System.IO.File.AppendAllText(logPath, $"[DEBUG] Deserializing dummy leg... Length: {rotData.Length}\n");
                                        var response = System.Text.Json.JsonSerializer.Deserialize<FlightSupervisor.UI.Models.SimBrief.SimBriefResponse>(rotData, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                        if (response != null)
                                        {
                                            _rotationQueue.Add(response);
                                            System.IO.File.AppendAllText(logPath, $"[DEBUG] Dummy leg enqueued.\n");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        SendToWeb(new { type = "log", message = $"[IPC] Failed to deserialize shell leg: {ex.Message}" });
                                        System.IO.File.AppendAllText(logPath, $"[ERROR] Failed to deserialize dummy leg: {ex.Message}\n");
                                    }
                                }

                                if (_currentResponse == null && _rotationQueue.Count > 0)
                                {
                                    LoadNextLeg();
                                }

                                SendToWeb(new { type = "log", message = $"[SYSTEM] Shell Rotation validated. Awaiting Dispatch..." });
                                
                                // Reset and put the UI in Preflight phase manually
                                SendToWeb(new { type = "phaseChanged", phase = "Preflight" });
                                _phaseManager.Reset(false); // Make sure we are at AtGate or Preflight

                                SendToWeb(new { type = "shellRotationValidated" });
                                if (_currentResponse != null && _currentResponse.IsDummy)
                                {
                                    var orig = _currentResponse.Origin?.IcaoCode;
                                    var dest = _currentResponse.Destination?.IcaoCode;
                                    SendToWeb(new { type = "log", message = $"[SYSTEM] Shell Leg sequence created for {orig} -> {dest}. Please use Dashboard utilities to Fetch OFP when ready." });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SendToWeb(new { type = "log", message = $"[SYSTEM] Error processing shell rotation: {ex.Message}" });
                        }
                    });
                }
                else if (action == "openSimbriefForCurrentLeg")
                {
                    Dispatcher.Invoke(() => {
                        FlightSupervisor.UI.Models.SimBrief.SimBriefResponse targetLeg = null;
                        
                        if (_currentResponse != null && _currentResponse.IsDummy)
                        {
                            targetLeg = _currentResponse;
                        }
                        else if (_rotationQueue.Count > 0 && _rotationQueue[0].IsDummy)
                        {
                            targetLeg = _rotationQueue[0];
                        }

                        if (targetLeg != null)
                        {
                            var orig = targetLeg.Origin?.IcaoCode;
                            var dest = targetLeg.Destination?.IcaoCode;
                            var airline = targetLeg.General?.Airline;
                            var fltnum = targetLeg.General?.FlightNumber;
                            SendToWeb(new { type = "log", message = $"[SYSTEM] Triggering SimBrief OFP fetch for {orig} -> {dest}." });
                            SendToWeb(new { type = "openSimbriefForDummy", orig = orig, dest = dest, airline = airline, fltnum = fltnum });
                        }
                    });
                }
                else if (action == "startShellSession")
                {
                    Dispatcher.Invoke(() => {
                        SendToWeb(new { type = "phaseChanged", phase = "GroundOps" });

                        if (_currentResponse != null)
                        {
                            // Re-insert the fetched OFP back into the queue so LoadNextLeg recalculates the Airframe logic properly
                            _rotationQueue.Insert(0, _currentResponse);
                            LoadNextLeg();
                            
                            _groundOpsManager.StartOps();
                        }
                        
                        SendTelemetryToWeb();
                    });
                }
                else if (action == "finishDispatch")
                {
                    Dispatcher.Invoke(() => {
                        SendToWeb(new { type = "phaseChanged", phase = "GroundOps" });

                        if (_currentResponse == null && _rotationQueue.Count > 0)
                        {
                            LoadNextLeg();
                        }
                        
                        // Force telemetry render
                        SendTelemetryToWeb();
                    });
                }
                else if (action == "saveSettings")
                {
                    if (doc.RootElement.TryGetProperty("options", out var opts))
                    {
                        if (opts.TryGetProperty("groundSpeed", out var gsProp))
                        {
                            if (Enum.TryParse<GroundOpsSpeed>(gsProp.GetString(), true, out var speed))
                                _groundOpsManager.SpeedSetting = speed;
                        }
                        if (opts.TryGetProperty("groundProb", out var gpProp))
                        {
                            if (int.TryParse(gpProp.GetString(), out var prob))
                                _groundOpsManager.EventProbabilityPercent = prob;
                        }
                        if (opts.TryGetProperty("firstFlightClean", out var ffcProp))
                        {
                            bool isClean = true;
                            if (ffcProp.ValueKind == System.Text.Json.JsonValueKind.True) isClean = true;
                            else if (ffcProp.ValueKind == System.Text.Json.JsonValueKind.False) isClean = false;
                            else if (ffcProp.ValueKind == System.Text.Json.JsonValueKind.String && bool.TryParse(ffcProp.GetString(), out bool bC)) isClean = bC;
                            
                            _cabinManager.FirstFlightClean = isClean;
                            if (isClean && _cabinManager.SessionFlightsCompleted == 0)
                            {
                                _cabinManager.SessionFlightsCompleted = 0;
                            }
                        }
                        if (opts.TryGetProperty("gsxSync", out var gsxProp))
                        {
                            bool isSync = true;
                            if (gsxProp.ValueKind == System.Text.Json.JsonValueKind.True) isSync = true;
                            else if (gsxProp.ValueKind == System.Text.Json.JsonValueKind.False) isSync = false;
                            else if (gsxProp.ValueKind == System.Text.Json.JsonValueKind.String && bool.TryParse(gsxProp.GetString(), out bool bS)) isSync = bS;
                            _gsxAutoSyncEnabled = isSync;
                        }
                        if (opts.TryGetProperty("volumePa", out var paProp))
                        {
                            if (int.TryParse(paProp.GetString(), out var volPa))
                                _audioEngine.CaptainVolume = volPa;
                        }
                        if (opts.TryGetProperty("volumePnc", out var pncProp))
                        {
                            if (int.TryParse(pncProp.GetString(), out var volPnc))
                                _audioEngine.PncVolume = volPnc;
                        }
                    }
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
                        if (parentWindow == this) {
                            this.Hide();
                            if (_notifyIcon != null) _notifyIcon.Visible = true;
                        } else {
                            if (parentWindow != null) parentWindow.WindowState = WindowState.Minimized;
                        }
                    });
                }
                else if (action == "maximizeApp")
                {
                    Dispatcher.Invoke(() => {
                        if (parentWindow != null) {
                            if (parentWindow.WindowState == WindowState.Maximized)
                                parentWindow.WindowState = WindowState.Normal;
                            else
                                parentWindow.WindowState = WindowState.Maximized;
                        }
                    });
                }
                else if (action == "togglePinApp")
                {
                    Dispatcher.Invoke(() => {
                        if (parentWindow != null) {
                            parentWindow.Topmost = !parentWindow.Topmost;
                        }
                    });
                }
                else if (action == "closeApp")
                {
                    Dispatcher.Invoke(() => {
                        if (parentWindow == this) {
                            if (_notifyIcon != null) _notifyIcon.Dispose();
                            System.Windows.Application.Current.Shutdown();
                        } else {
                            parentWindow?.Close();
                        }
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
                else if (action == "startService" || action == "startDeboarding" || action == "startBoarding")
                {
                    var srvName = doc.RootElement.TryGetProperty("service", out var svcProp) ? svcProp.GetString() : null;
                    if (action == "startDeboarding") srvName = "Deboarding";
                    if (action == "startBoarding") srvName = "Boarding";

                    if (!string.IsNullOrEmpty(srvName))
                    {
                        bool engOn = (_phaseManager.Eng1N1 >= 5 || _phaseManager.Eng2N1 >= 5);
                        bool beaconOn = _phaseManager.IsBeaconLightOn;
                        bool doorOpen = _phaseManager.IsMainDoorOpen || _phaseManager.IsJetwayConnected;

                        bool canStart = true;
                        string failReason = "";

                        if (srvName.Equals("Deboarding", StringComparison.OrdinalIgnoreCase) || 
                            srvName.Equals("Boarding", StringComparison.OrdinalIgnoreCase) || 
                            srvName.Contains("Cargo", StringComparison.OrdinalIgnoreCase))
                        {
                            if (engOn) { canStart = false; failReason = "Engines Running"; }
                            else if (beaconOn) { canStart = false; failReason = "Beacon ON"; }
                            else if ((srvName.Equals("Deboarding", StringComparison.OrdinalIgnoreCase) || srvName.Equals("Boarding", StringComparison.OrdinalIgnoreCase)) && !doorOpen) 
                            { canStart = false; failReason = "Doors Closed"; }
                        }
                        else
                        {
                            if (engOn) { canStart = false; failReason = "Engines Running"; }
                            else if (beaconOn) { canStart = false; failReason = "Beacon ON"; }
                        }

                        if (canStart)
                        {
                            if (srvName == "Deboarding") _cabinManager.StartDeboarding();
                            if (srvName == "Boarding") _cabinManager.StartBoarding();
                            _groundOpsManager.StartManualService(srvName);
                        }
                        else
                        {
                            SendToWeb(new { type = "log", message = $"[SYSTEM] Rejected {srvName} start: Physical safety violation ({failReason})." });
                        }
                    }
                }
                else if (action == "updateGroundSpeed")
                {
                    var spd = doc.RootElement.GetProperty("value").GetString() ?? "Realistic";
                    _groundOpsManager?.SetGroundSpeedMultiplier(spd);
                }
                else if (action == "acarsWeatherRequest")
                {
                    Dispatcher.InvokeAsync(async () => {
                        await RefreshLiveWeatherAsync();
                    });
                }
                else if (action == "requestStartGroundOps")
                {
                    string expectedLatStr = "";
                    string expectedLonStr = "";
                    string expectedIcao = "";
                    if (doc.RootElement.TryGetProperty("expectedLat", out var expLatProp))
                        expectedLatStr = expLatProp.GetString() ?? "";
                    if (doc.RootElement.TryGetProperty("expectedLon", out var expLonProp))
                        expectedLonStr = expLonProp.GetString() ?? "";
                    if (doc.RootElement.TryGetProperty("expectedIcao", out var expIcaoProp))
                        expectedIcao = expIcaoProp.GetString() ?? "";

                    Dispatcher.Invoke(() => {
                        bool isMoteursEteints = !_phaseManager.Eng1Combustion && !_phaseManager.Eng2Combustion;
                        string failReason = "";
                        _isAtWrongAirport = !IsAircraftAtOrigin();

                        if (!_simConnectService.IsConnected) {
                            failReason = "Flight Simulator is not connected.";
                        } else if (!isMoteursEteints) {
                            failReason = "Both engines must be OFF to start Ground Ops.";
                        } else if (!_phaseManager.IsOnGround) {
                            failReason = "Aircraft must be ON THE GROUND to start Ground Ops.";
                        } else if (_isAtWrongAirport) {
                            string expectedIcao = _currentResponse?.Origin?.IcaoCode ?? "Unknown";
                            failReason = $"Wrong Airport! You are too far from {expectedIcao} to start Ground Operations.";
                        }

                        if (!_simConnectService.IsConnected || !isMoteursEteints || !_phaseManager.IsOnGround || _isAtWrongAirport)
                        {
                            if (string.IsNullOrEmpty(failReason)) failReason = "Action Denied by Gatekeeper system.";
                            SendToWeb(new { type = "gatekeeperFailed", reason = failReason });
                        }
                        else
                        {
                            SendToWeb(new { type = "gatekeeperPassed" });
                        }
                    });
                }
                else if (action == "syncRotationsAndStart")
                {
                    Dispatcher.Invoke(() => {
                        // Resync rotation queue with Javascript's Drag & Drop order
                        string logPath = "sync_debug.txt";
                        System.IO.File.AppendAllText(logPath, $"\n[{DateTime.Now}] syncRotationsAndStart called.\n");
                        
                        if (doc.RootElement.TryGetProperty("payloadStr", out var payloadStrProp))
                        {
                            string rawJson = payloadStrProp.GetString();
                            try 
                            {
                                var arr = System.Text.Json.JsonDocument.Parse(rawJson).RootElement;
                                System.IO.File.AppendAllText(logPath, $"[DEBUG] Parsing payload array of length {arr.GetArrayLength()}\n");
                                SendToWeb(new { type = "log", message = $"[DEBUG] syncRotationsAndStart: Parsing payload array of length {arr.GetArrayLength()}" });
                                _rotationQueue.Clear();
                                foreach (var item in arr.EnumerateArray())
                                {
                                try
                                {
                                    System.IO.File.AppendAllText(logPath, $"[DEBUG] Deserializing element of length {item.GetRawText().Length}...\n");
                                    var response = System.Text.Json.JsonSerializer.Deserialize<SimBriefResponse>(item.GetRawText(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                    if (response != null)
                                    {
                                        _rotationQueue.Add(response);
                                        System.IO.File.AppendAllText(logPath, $"[DEBUG] Enqueued flight {response.Origin?.IcaoCode} -> {response.Destination?.IcaoCode}.\n");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.IO.File.AppendAllText(logPath, $"[ERROR] Deserialize failed: {ex.Message}\n");
                                    SendToWeb(new { type = "log", message = $"[IPC] Failed to deserialize sorted leg: {ex.Message}" });
                                }
                            }
                            }
                            catch (Exception ex)
                            {
                                System.IO.File.AppendAllText(logPath, $"[ERROR] Failed to parse payloadStr JSON: {ex.Message}\n");
                            }
                        }
                        else
                        {
                            System.IO.File.AppendAllText(logPath, $"[ERROR] Payload was missing or not an array.\n");
                        }

                        System.IO.File.AppendAllText(logPath, $"[DEBUG] Queue size is now {_rotationQueue.Count}, _currentResponse is {(_currentResponse == null ? "NULL" : "SET")}\n");
                        SendToWeb(new { type = "log", message = $"[DEBUG] syncRotationsAndStart: Queue size is now {_rotationQueue.Count}, _currentResponse is {(_currentResponse == null ? "NULL" : "SET")}" });

                        // Prevent Ground Ops abort by setting the first leg into the state manager
                        if (_currentResponse == null && _rotationQueue.Count > 0)
                        {
                            System.IO.File.AppendAllText(logPath, $"[DEBUG] Calling LoadNextLeg()!\n");
                            SendToWeb(new { type = "log", message = $"[DEBUG] LoadNextLeg() is being called!" });
                            LoadNextLeg();
                        }
                        else if (_currentResponse != null)
                        {
                            System.IO.File.AppendAllText(logPath, $"[DEBUG] _currentResponse is already set! It has {(_currentResponse.Origin?.IcaoCode) ?? "NoOrigin"}. Not calling LoadNextLeg().\n");
                        }
                        else
                        {
                            System.IO.File.AppendAllText(logPath, $"[ERROR] _currentResponse is null, but _rotationQueue is empty!\n");
                        }
                        
                        _groundOpsManager.StartOps();
                        System.IO.File.AppendAllText(logPath, $"[DEBUG] StartOps() complete.\n");
                    });
                }
                else if (action == "setAlwaysOnTop")
                {
                    Dispatcher.Invoke(() => {
                        this.Topmost = doc.RootElement.GetProperty("value").GetBoolean();
                    });
                }
                else if (action == "requestTimeSkip")
                {
                    if (doc.RootElement.TryGetProperty("minutes", out var minProp))
                    {
                        int minutes = minProp.GetInt32();
                        
                        if (_phaseManager.CurrentPhase != FlightPhase.AtGate && _phaseManager.CurrentPhase != FlightPhase.Turnaround)
                        {
                            SendToWeb(new { type = "log", message = $"[SYSTEM] Action Denied. Time Skip is ONLY available during Ground Operations." });
                            minutes = 0;
                        }

                        DateTime? effectiveSobt = _groundOpsManager?.TargetSobt ?? _nextSobtOverride;

                        if (minutes > 0 && effectiveSobt != null)
                        {
                            double remainingMins = (effectiveSobt.Value - _currentSimTime).TotalMinutes;
                            if (remainingMins <= 5.0)
                            {
                                SendToWeb(new { type = "log", message = $"[SYSTEM] Action Denied. You are within the 5-minute pre-departure safety window." });
                                minutes = 0;
                            }
                            else if (minutes > remainingMins - 5.0)
                            {
                                int cappedMinutes = (int)Math.Floor(remainingMins - 5.0);
                                if (cappedMinutes > 0)
                                {
                                    SendToWeb(new { type = "log", message = $"[SYSTEM] Time Skip capped to {cappedMinutes} min to preserve the 5-minute safety window." });
                                    minutes = cappedMinutes;
                                }
                                else
                                {
                                    minutes = 0;
                                }
                            }
                        }

                        if (minutes > 0) 
                        {
                            ExecuteTimeSkip(minutes);

                            SendToWeb(new { type = "log", message = $"[CAPTAIN] Time advanced by {minutes} minutes." });
                            SendTelemetryToWeb();
                        }
                    }
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
                            string destIcao = _currentResponse?.Destination?.IcaoCode ?? "XXXX";
                            int.TryParse(_currentResponse?.Times?.EstTimeEnroute, out int timeSecs);
                            int timeMins = timeSecs / 60;
                            
                            bool badWeather = false;
                            string metar = _currentResponse?.Weather?.DestMetar?.ToUpper() ?? "";
                            if (metar.Contains(" TS") || metar.Contains(" RA") || metar.Contains(" SN") || metar.Contains(" FG")) badWeather = true;

                            int destTempC = 15;
                            var match = System.Text.RegularExpressions.Regex.Match(metar, @"\s([M]?\d{2})\/([M]?\d{2})\s");
                            if (match.Success) {
                                string tStr = match.Groups[1].Value;
                                if (tStr.StartsWith("M")) destTempC = -int.Parse(tStr.Substring(1));
                                else destTempC = int.Parse(tStr);
                            }

                            DateTime departureLocal = _cabinManager.CurrentSimLocalTime != DateTime.MinValue ? _cabinManager.CurrentSimLocalTime : DateTime.Now;
                            DateTime destLocal = departureLocal.AddMinutes(timeMins);

                            _cabinManager.AnnounceWelcome(destIcao, destName, timeMins, badWeather, destTempC, destLocal, departureLocal, metar);
                        }
                        else if (annType == "Approach")
                        {
                            string destName = _currentResponse?.Destination?.Name ?? _currentResponse?.Destination?.IcaoCode ?? "our destination";
                            string metar = _currentResponse?.Weather?.DestMetar?.ToUpper() ?? "";
                            
                            int destTempC = 15;
                            var match = System.Text.RegularExpressions.Regex.Match(metar, @"\s([M]?\d{2})\/([M]?\d{2})\s");
                            if (match.Success) {
                                string tStr = match.Groups[1].Value;
                                if (tStr.StartsWith("M")) destTempC = -int.Parse(tStr.Substring(1));
                                else destTempC = int.Parse(tStr);
                            }

                            // Calculate destination time for approach (basically current time)
                            DateTime destLocal = _cabinManager.CurrentSimLocalTime != DateTime.MinValue ? _cabinManager.CurrentSimLocalTime : DateTime.Now;

                            _cabinManager.AnnounceApproach(destName, metar, destTempC, destLocal);
                        }
                        else if (annType == "CruiseStatus")
                        {
                            string destName = _currentResponse?.Destination?.Name ?? _currentResponse?.Destination?.IcaoCode ?? "our destination";
                            string metar = _currentResponse?.Weather?.DestMetar?.ToUpper() ?? "";
                            string enrtMetar = "";
                            if (_currentResponse?.Weather?.EnrtMetar?.ValueKind == System.Text.Json.JsonValueKind.Array && _currentResponse.Weather.EnrtMetar.Value.GetArrayLength() > 0)
                            {
                                enrtMetar = _currentResponse.Weather.EnrtMetar.Value[0].GetString()?.ToUpper() ?? "";
                            }
                            else if (_currentResponse?.Weather?.EnrtMetar?.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                enrtMetar = _currentResponse.Weather.EnrtMetar.Value.GetString()?.ToUpper() ?? "";
                            }
                            
                            int destTempC = 15;
                            var match = System.Text.RegularExpressions.Regex.Match(metar, @"\s([M]?\d{2})\/([M]?\d{2})\s");
                            if (match.Success) {
                                string tStr = match.Groups[1].Value;
                                if (tStr.StartsWith("M")) destTempC = -int.Parse(tStr.Substring(1));
                                else destTempC = int.Parse(tStr);
                            }

                            string avgWind = _currentResponse?.General?.AvgWindComp ?? "";
                            int altitude = (int)_lastKnownAltitude;

                            string destIcao = _currentResponse?.Destination?.IcaoCode ?? "";
                            _cabinManager.AnnounceCruise(altitude, avgWind, destIcao, destName, metar, enrtMetar, destTempC, DateTime.Now);
                        }
                        else if (annType != null && (annType == "Delay" || annType.StartsWith("Delay_")))
                        {
                            string reason = "ATC";
                            if (annType.StartsWith("Delay_"))
                            {
                                reason = annType.Substring(6); // Gets "Pax", "Bags", "Cargo", "Weather", "Traffic", "Technical", "Catering"
                            }
                            else if (doc.RootElement.TryGetProperty("reason", out var reasonProperty))
                            {
                                reason = reasonProperty.GetString() ?? "ATC";
                            }
                            string destName = _currentResponse?.Destination?.Name ?? _currentResponse?.Destination?.IcaoCode ?? "our destination";
                            _cabinManager.AnnounceDelay(reason, destName);
                        }
                        else if (annType == "Descent" || annType == "ArrivalWeather")
                        {
                            string destIcao = _currentResponse?.Destination?.IcaoCode ?? "";
                            string destName = _currentResponse?.Destination?.Name ?? destIcao ?? "our destination";
                            string metar = _currentResponse?.Weather?.DestMetar?.ToUpper() ?? "";
                            
                            int destTempC = 15;
                            var match = System.Text.RegularExpressions.Regex.Match(metar, @"\s([M]?\d{2})\/([M]?\d{2})\s");
                            if (match.Success) {
                                string tStr = match.Groups[1].Value;
                                if (tStr.StartsWith("M")) destTempC = -int.Parse(tStr.Substring(1));
                                else destTempC = int.Parse(tStr);
                            }

                            _cabinManager.AnnounceDescent(destIcao, destName, metar, destTempC);
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
                else if (action == "requestTechRepair")
                {
                    if (_airframeManager?.CurrentAirframe != null && _groundOpsManager != null)
                    {
                        var af = _airframeManager.CurrentAirframe;
                        if (af.ActiveDefects.Count > 0)
                        {
                            var defect = af.ActiveDefects[0];
                            af.Events.Insert(0, new FlightSupervisor.UI.Models.AirframeLogEvent 
                            {
                                Timestamp = DateTime.UtcNow,
                                Type = "repair",
                                Location = _currentResponse?.Origin?.IcaoCode ?? "UNK",
                                Description = $"Line maintenance requested for: {defect}.",
                                Severity = "warn"
                            });
                            af.ActiveDefects.RemoveAt(0);
                            
                            if (af.MaintenanceGrade == "D" || af.MaintenanceGrade == "F")
                                af.MaintenanceGrade = "C";

                            _airframeManager.SaveAirframe(af);
                            
                            var newService = new FlightSupervisor.UI.Services.GroundService 
                            {
                                Name = "Tech Repair",
                                TotalDurationSec = 15 * 60,
                                IsOptional = false,
                                State = FlightSupervisor.UI.Services.GroundServiceState.InProgress,
                                StatusMessage = "Repairing fault..."
                            };
                            _groundOpsManager.Services.Add(newService);
                            
                            SendToWeb(new { type = "log", message = $"[TECH] Dispatched line maintenance to repair: {defect}. Turnaround delayed 15 minutes." });
                        }
                    }
                }
                else if (action == "removeCurrentLeg" || action == "cancelFlight")
                {
                    _phaseManager.Reset();
                    _scoreManager.Reset();
                    _cabinManager.Reset();
                    _currentResponse = null;
                    _groundOpsManager.AbortAllOperations();
                    _groundOpsManager.Services.Clear();
                    _aobt = null;
                    _aibt = null;
                    _isAtWrongAirport = false;
                    SendToWeb(new { type = "flightReset" });
                }
                else if (action == "removeAllLegs")
                {
                    _rotationQueue.Clear();
                    _phaseManager.Reset();
                    _scoreManager.Reset();
                    _cabinManager.FirstFlightClean = true;
                    _cabinManager.SessionFlightsCompleted = 0;
                    _cabinManager.Reset();
                    _currentResponse = null;
                    _groundOpsManager.AbortAllOperations();
                    _groundOpsManager.Services.Clear();
                    _aobt = null;
                    _aibt = null;
                    _isAtWrongAirport = false;
                    _sessionArchives.Clear();
                    SendToWeb(new { type = "flightReset" });
                    SendToWeb(new { type = "rotationCleared" }); 
                }
                else if (action == "deleteLegAtIndex")
                {
                    if (doc.RootElement.TryGetProperty("index", out var idxProp))
                    {
                        int idx = idxProp.GetInt32();
                        // index 0 in dashboard is _currentResponse, index 1 is _rotationQueue[0], etc.
                        if (idx > 0 && idx <= _rotationQueue.Count)
                        {
                            _rotationQueue.RemoveAt(idx - 1);
                            SendToWeb(new { type = "removeLegAtIndex", index = idx });
                        }
                    }
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
                        var briefingData = weatherService.GenerateBriefing(_currentResponse, _isAtWrongAirport);
                        SendToWeb(new { type = "briefingUpdate", briefing = briefingData });
                    }
                }
                else if (action == "acknowledgeFlightReport")
                {
                    Dispatcher.Invoke(() => {
                        if (_rotationQueue.Count > 0)
                        {
                            _phaseManager.ForcePhase(FlightPhase.Turnaround);
                            SendToWeb(new { type = "phaseChanged", phaseEnum = 1, phaseName = "TURNAROUND" });
                            // The web UI "Load Next Leg/Briefing" should be shown.
                        }
                        else
                        {
                            // No more legs, just show arrived
                        }
                    });
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
            string json = JsonSerializer.Serialize(data);
            
            if (MainWebView?.CoreWebView2 != null)
            {
                MainWebView.CoreWebView2.PostWebMessageAsJson(json);
            }
            
            if (_manifestWindow != null && _manifestWebView?.CoreWebView2 != null)
            {
                _manifestWebView.CoreWebView2.PostWebMessageAsJson(json);
            }
            if (_groundOpsWindow != null && _groundOpsWebView?.CoreWebView2 != null)
            {
                _groundOpsWebView.CoreWebView2.PostWebMessageAsJson(json);
            }
            if (_logsWindow != null && _logsWebView?.CoreWebView2 != null)
            {
                _logsWebView.CoreWebView2.PostWebMessageAsJson(json);
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
            _isAtWrongAirport = _currentResponse != null && !IsAircraftAtOrigin();
            
            // disabled to prevent UI spam when addon traffic/navdata pushes aircraft off the ideal origin coords
            bool uiMismatch = false;

            bool latParsed = false;
            bool lonParsed = false;
            double lat = 0, lon = 0;
            
            // STORY 38: Simplified Reference
            var originForDist = _currentResponse?.Origin;

            if (originForDist != null)
            {
                latParsed = double.TryParse(originForDist.PosLat, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out lat);
                lonParsed = double.TryParse(originForDist.PosLong, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out lon);
            }

            double calcDist = -1;
            if (latParsed && lonParsed && originForDist != null) {
                calcDist = Math.Round(CalculateHaversineDistanceNM(_phaseManager.Latitude, _phaseManager.Longitude, lat, lon), 2);
            }

            double destDist = -1;
            var destForDist = _currentResponse?.Destination;
            if (destForDist != null &&
                double.TryParse(destForDist.PosLat, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double dLat) &&
                double.TryParse(destForDist.PosLong, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double dLon))
            {
                destDist = Math.Round(CalculateHaversineDistanceNM(_phaseManager.Latitude, _phaseManager.Longitude, dLat, dLon), 2);
            }

            // STORY 38: DISCRETE AMBER LOG (Style SIS)
            if (uiMismatch && _lastNotifiedMismatchIcao != originForDist?.IcaoCode && calcDist > 0)
            {
                _lastNotifiedMismatchIcao = originForDist?.IcaoCode ?? "";
                SendToWeb(new { 
                    type = "cabinLog", 
                    level = "amber", 
                    message = $"[SYSTEM] Position Mismatch : Actuellement à {calcDist} NM de l'origine prévue ({_lastNotifiedMismatchIcao})." 
                });
            }
            else if (!uiMismatch)
            {
                _lastNotifiedMismatchIcao = ""; // Reset for next occurrence
            }

                // Bug fix: As long as there are boarded passengers from the previous leg, we show them on the UI.
                // We don't wait for "Deboarding" to be explicitly InProgress, otherwise they disappear instantly upon arrival when the new Leg is loaded.
                bool hasPreviousPassengersStillBoarded = _cabinManager.PreviousLegManifest.Any(p => p.IsBoarded);
                var activeManifest = hasPreviousPassengersStillBoarded ? _cabinManager.PreviousLegManifest : _cabinManager.PassengerManifest;

                SendToWeb(new 
                {
                    type = "telemetry",
                    phase = _phaseManager.GetLocalizedPhaseName(),
                    phaseEnum = _phaseManager.CurrentPhase.ToString(),
                    altitude = _phaseManager.IsSimulationMode ? _phaseManager.Altitude : _lastKnownAltitude,
                    groundSpeed = _phaseManager.IsSimulationMode ? _phaseManager.GroundSpeed : _lastKnownGroundSpeed,
                    radioHeight = _lastKnownRadioHeight,
                    sessionFlightsCompleted = _cabinManager.SessionFlightsCompleted,
                    isGearDown = _isGearDown,
                    seatbeltsOn = _cabinManager.IsSeatbeltsOn,
                    isCabinCallIncoming = _cabinManager.IsCabinCallIncoming,
                    isDelayed = _groundOpsManager.TargetSobt != null && (_aobt != null ? _aobt.Value > _groundOpsManager.TargetSobt.Value.AddMinutes(5) : _currentSimTime > _groundOpsManager.TargetSobt.Value.AddMinutes(5)),
                    isBoardingComplete = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Boarding")?.State == GroundServiceState.Completed,
                    isDeboardingAvailable = _groundOpsManager.Services.Any(s => s.Name == "Deboarding"),
                    isDeboardingCompleted = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Deboarding")?.State == GroundServiceState.Completed,
                    isFuelValidated = _groundOpsManager.IsFuelSheetValidated,
                    originDistanceNM = calcDist,
                    destDistanceNM = destDist,
                    isAtWrongAirport = uiMismatch,
                    isMaintenanceRequired = _airframeManager?.CurrentAirframe?.ActiveDefects?.Count > 0,
                plannedOriginIcao = originForDist?.IcaoCode ?? "",
                anxiety = Math.Round(_cabinManager.PassengerAnxiety, 1),
                comfort = Math.Round(_cabinManager.ComfortLevel, 1),
                satisfaction = Math.Round(_cabinManager.Satisfaction, 1),
                crewEsteem = Math.Round(_cabinManager.CrewEsteem / 10.0, 1),
                crewProactivity = Math.Round(_cabinManager.CrewProactivity, 1),
                crewEfficiency = Math.Round(_cabinManager.CrewEfficiency, 1),
                crewMorale = Math.Round(_cabinManager.CrewMorale, 1),
                serviceProgress = Math.Round(_cabinManager.InFlightServiceProgress, 1),
                securingProgress = Math.Round(_cabinManager.SecuringProgress, 1),
                isSecuringHalted = _cabinManager.IsSecuringHalted,
                isSecuringHurried = _cabinManager.IsSecuringHurried,
                satietyActive = _cabinManager.IsSatietyActive,
                cabinState = _cabinManager.State.ToString(),
                isServiceHalted = _cabinManager.IsServiceHalted,
                cabinReportCooldownElapsed = _cabinManager.SecondsSinceLastReport,
                isPlayingSafetyDemo = _cabinManager.IsPlayingSafetyDemo,
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
                virtualFuelPercentage = Math.Round(_cabinManager.VirtualFuelPercentage, 1),
                baggageCompletion = Math.Round(_cabinManager.BaggageCompletion, 1),
                isSilencePenaltyActive = _cabinManager.IsSilencePenaltyActive,
                crisisElapsed = _crisisManager.CrisisStartTimeSeconds > 0 ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _crisisManager.CrisisStartTimeSeconds : 0,
                passengers = activeManifest,
                turbulenceSeverity = (int)_phaseManager.TurbulenceSeverity,
                
                // Advanced Ground Ops Telemetry
                eng1N1 = Math.Round(_phaseManager.Eng1N1, 1),
                eng2N1 = Math.Round(_phaseManager.Eng2N1, 1),
                isMainDoorOpen = _phaseManager.IsMainDoorOpen,
                isJetwayConnected = _phaseManager.IsJetwayConnected,
                isBeaconOn = _phaseManager.IsBeaconLightOn,
                gsxBoardingState = _phaseManager.GsxBoardingState,
                gsxDeboardingState = _phaseManager.GsxDeboardingState,
                fob = _currentFobKg,
                aircraftState = _cabinManager.StateOfAircraft
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
            if (_rotationQueue.Count > 0)
            {
                var response = _rotationQueue[0];
                _rotationQueue.RemoveAt(0);
                _currentResponse = response;
                
                RefreshPassengerDemographics();
                
                SendToWeb(new { type = "popLeg", index = 0 });
            }
            
            if (_cabinManager.SessionFlightsCompleted == 0)
            {
                _sessionArchives.Clear();
            }

            _cabinManager.CurrentFlight = _currentResponse;

            // Upon preparing the next leg, we return the aircraft phase to AtGate for loading operations.
            // Turnaround phase is strictly meant for the intermediate UNLOADING step.
            _phaseManager.Reset(false); // Force AtGate
            
            _scoreManager.Reset();
            _cabinManager.Reset();
            _groundOpsResourceService.Reset();
            _eventEngine.Reset();
            _groundOpsManager.PrepareNextLeg(_currentFobKg, _cabinManager.CabinCleanliness, _cabinManager.CateringCompletion);
            
            // Clear previous leg's actual times
            _aobt = null;
            _aibt = null;
            
            // Build Airframe persistence from SimBrief data and current physical fuel
            _cabinManager.StateOfAircraft.AirframeId = _currentResponse?.Aircraft?.InternalId ?? "";
            _cabinManager.StateOfAircraft.Registration = _currentResponse?.Aircraft?.Reg ?? "";
            _cabinManager.StateOfAircraft.Model = _currentResponse?.Aircraft?.BaseType ?? _currentResponse?.Aircraft?.IcaoCode ?? "";
            
            if (_currentFobKg > 0)
            {
                _cabinManager.StateOfAircraft.InitialFobKg = _currentFobKg;
            }
            else
            {
                // Fallback to Fenix default spawn FOB (3000kg) to avoid completely nullizing the uplift process
                // This preserves a realistic fuel sheet interaction instead of perfectly matching SimBrief block.
                _cabinManager.StateOfAircraft.InitialFobKg = 3000;
                SendToWeb(new { type = "log", message = $"[SYSTEM/WARN] Isolation Fenix: Impossible de lire le fuel physique. Initial FOB forcé aux 3000 KG de base Fenix." });
            }

            RefreshPassengerDemographics();

            // _nextSobtOverride is now established securely at the start of the Turnaround Phase.
            // We preserved it across LoadNextLeg.
            if (_nextSobtOverride.HasValue)
            {
                SendToWeb(new { type = "log", message = $"[DISPATCH] Turnaround timer inherited from Leg 1 arrival: Target SOBT {_nextSobtOverride.Value:HH:mm}Z." });
            }

        // InitializeFromSimBrief is now deferred until validateFuel is received.

        _ = RefreshLiveWeatherAsync();

            SendToWeb(new { type = "groundOpsReady" });
            SendToWeb(new { type = "groundOps", services = _groundOpsManager.Services, isDispatchSignedOff = _groundOpsManager.IsFuelSheetValidated });

            if (!string.IsNullOrEmpty(_currentResponse?.General?.InitialAlt))
            {
                var digits = new string(_currentResponse.General.InitialAlt.Where(char.IsDigit).ToArray());
                if (double.TryParse(digits, out double alt))
                {
                    _phaseManager.TargetCruiseAltitude = alt < 1000 ? alt * 100 : alt;
                }
            }

            string acType = _currentResponse?.Aircraft?.BaseType ?? _currentResponse?.Aircraft?.IcaoCode ?? "";
            if (acType.StartsWith("A33") || acType.StartsWith("A34") || acType.StartsWith("A35") || acType.StartsWith("A38") || 
                acType.StartsWith("B74") || acType.StartsWith("B76") || acType.StartsWith("B77") || acType.StartsWith("B78") || acType.StartsWith("MD1"))
                _phaseManager.AircraftCategory = "Heavy";
            else if (acType.StartsWith("C1") || acType.StartsWith("SR") || acType.StartsWith("DA") || acType.StartsWith("PA") || acType.StartsWith("P28"))
                _phaseManager.AircraftCategory = "Light";
            else
                _phaseManager.AircraftCategory = "Medium";

            SendToWeb(new { type = "simulationState", msg = $"Leg Loaded: {_currentResponse?.Origin?.IcaoCode} to {_currentResponse?.Destination?.IcaoCode}. {(_rotationQueue.Count)} leg(s) remaining." });

            // Story 38: Trigger immediate UI update for Briefing
            var weatherService = new FlightSupervisor.UI.Services.WeatherBriefingService();
            var briefingData = weatherService.GenerateBriefing(_currentResponse, _isAtWrongAirport);
            SendToWeb(new { type = "briefingUpdate", briefing = briefingData });
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

                            var altnIcao = response.Alternates?.Count > 0 ? response.Alternates[0].IcaoCode : "";
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

                            var altnIcao = response.Alternates?.Count > 0 ? response.Alternates[0].IcaoCode : "";
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

                    // STORY 43 Validation: The new OFP MUST originate from the CURRENT airport!
                    if (_phaseManager.CurrentPhase == FlightPhase.Turnaround && _currentResponse != null) 
                    {
                        if (response.Origin?.IcaoCode?.ToUpper() != _currentResponse.Destination?.IcaoCode?.ToUpper())
                        {
                            SendToWeb(new { type = "log", message = $"[ERROR] Fetch Failed: SimBrief OFP ({response.Origin?.IcaoCode}->{response.Destination?.IcaoCode}) does not start at your current location ({_currentResponse.Destination?.IcaoCode}). Please generate the correct flight on SimBrief." });
                            Dispatcher.Invoke(() => {
                                if (_fuelSheetWindow != null) {
                                    // Make sure it doesn't leave the user hanging without a clue
                                    SendToWeb(new { type = "log", message = "[SYSTEM] Aborting OFP Injection to preserve simulation integrity." });
                                }
                            });
                            return;
                        }
                    }

                    // Step 1: Detect position in rotation (isDupe logic) before fixing time
                    bool isDupe = false;
                    int dupeIndex = -1;
                    bool replacesCurrent = false;

                    // Check if it replaces the ACTIVE leg
                    if (_currentResponse != null)
                    {
                        bool origDestMatch = _currentResponse.Origin?.IcaoCode == response.Origin?.IcaoCode &&
                                             _currentResponse.Destination?.IcaoCode == response.Destination?.IcaoCode;

                        bool isDummyBlocker = _currentResponse.IsDummy;

                        if (origDestMatch && (_currentResponse.IsDummy || _currentResponse.General?.FlightNumber == response.General?.FlightNumber))
                        {
                            replacesCurrent = true;
                            isDupe = true;
                        }
                        else if (isDummyBlocker)
                        {
                            // If we're stuck in a dummy (e.g. Turnaround empty shell), we forcefully replace it
                            // regardless of origin/destination mismatch, to allow the user to pivot easily.
                            replacesCurrent = true;
                            isDupe = true;
                            if (_rotationQueue.Count > 0) dupeIndex = 0;
                        }
                    }

                    // Check if it replaces a QUEUED leg
                    for (int i = 0; i < _rotationQueue.Count; i++)
                    {
                        var r = _rotationQueue[i];
                        
                        // STORY 43 - PIvot Support: If it's a dummy leg in Turnaround, we allow overwriting it even if destination changes!
                        bool isDummyTurnaroundPivot = r.IsDummy && _phaseManager.CurrentPhase == FlightPhase.Turnaround && r.Origin?.IcaoCode == response.Origin?.IcaoCode && i == 0;
                        
                        if ((r.Origin?.IcaoCode == response.Origin?.IcaoCode &&
                            r.Destination?.IcaoCode == response.Destination?.IcaoCode &&
                            (r.IsDummy || r.General?.FlightNumber == response.General?.FlightNumber)) || isDummyTurnaroundPivot)
                        {
                            dupeIndex = i;
                            isDupe = true;
                            break;
                        }
                    }

                    // Step 2: Time Synchronization Logic
                    if (response.Times != null)
                    {
                        // Identify the previous leg to chain from
                        FlightSupervisor.UI.Models.SimBrief.SimBriefResponse prevLeg = null;
                        if (dupeIndex > 0) prevLeg = _rotationQueue[dupeIndex - 1]; // Replaces a leg > 0
                        else if (dupeIndex == 0) prevLeg = _currentResponse; // Replaces leg 0, so chain to current
                        else if (!isDupe && _rotationQueue.Count > 0) prevLeg = _rotationQueue.Last(); // Appended, chain to last
                        else if (!isDupe && _rotationQueue.Count == 0) prevLeg = _currentResponse; // Appended as first item, chain to current if it exists

                        // If prevLeg has no times (e.g. it's a dummy/shell leg), we cannot chain from it. Treat as first.
                        if (prevLeg != null && prevLeg.Times == null)
                        {
                            prevLeg = null;
                        }

                        // --- NEW: FORCED NEXT SOBT INHERITANCE FOR MULTI-LEG ROTATIONS ---
                        if (replacesCurrent && _nextSobtOverride.HasValue)
                        {
                            if (long.TryParse(response.Times.SchedOut, out long originalOut) && long.TryParse(response.Times.SchedIn, out long originalIn))
                            {
                                long blockSecs = originalIn - originalOut;
                                if (blockSecs <= 0) blockSecs = 3600;

                                long newOut = ((DateTimeOffset)DateTime.SpecifyKind(_nextSobtOverride.Value, DateTimeKind.Utc)).ToUnixTimeSeconds();
                                long newIn = newOut + blockSecs;
                                
                                response.Times.SchedOut = newOut.ToString();
                                response.Times.SchedIn = newIn.ToString();
                                
                                Dispatcher.Invoke(() => {
                                    System.IO.File.AppendAllText("sync_debug.txt", $"\n[{DateTime.Now}] Leg 2+ Turnaround Sync: SimBrief response modified. SchedOut forced to {_nextSobtOverride.Value:HH:mm}Z based on previous Leg constraints.\n");
                                });
                            }
                        }
                        // If user specifically requested MSFS sync AND it's the very first valid leg (or overriding it)
                        else if (syncMsfsTime && prevLeg == null)
                        {
                            if (long.TryParse(response.Times.SchedOut, out long originalOut) && long.TryParse(response.Times.SchedIn, out long originalIn))
                            {
                                long blockSecs = originalIn - originalOut;
                                if (blockSecs <= 0) blockSecs = 3600;

                                long simNowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                if (_simConnectService != null && _simConnectService.IsConnected && _simConnectService.CurrentSimZuluTime.Year > 2000)
                                {
                                    simNowUnix = ((DateTimeOffset)_simConnectService.CurrentSimZuluTime).ToUnixTimeSeconds();
                                }

                                long newOut = simNowUnix + (30 * 60); 
                                long newIn = newOut + blockSecs;
                                
                                response.Times.SchedOut = newOut.ToString();
                                response.Times.SchedIn = newIn.ToString();
                            }
                        }
                        // Chaining time logic
                        else if (prevLeg?.Times != null)
                        {
                            if (long.TryParse(prevLeg.Times.SchedIn, out long lastIn) && 
                                long.TryParse(response.Times.SchedOut, out long currentOut) && 
                                long.TryParse(response.Times.SchedIn, out long currentIn))
                            {
                                long blockSecs = currentIn - currentOut;
                                if (blockSecs <= 0) blockSecs = 3600;

                                long turnaroundSecs = 45 * 60;
                                if (CurrentAirline != null) turnaroundSecs = _airlineDb.GetStandardTurnaroundTimeMinutes(CurrentAirline.Tier) * 60;
                                
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
                    }

                    // Step 3: Apply the replacement
                    if (replacesCurrent)
                    {
                        _currentResponse = response;
                        RefreshPassengerDemographics();
                        _groundOpsManager.InitializeFromSimBrief(response, _cabinManager.SessionFlightsCompleted == 0 && _cabinManager.FirstFlightClean, _currentFobKg, _cabinManager.CabinCleanliness, _cabinManager.CateringCompletion, _cabinManager.WaterLevel, _cabinManager.WasteLevel, _nextSobtOverride);
                        SendToWeb(new { type = "groundOpsReady" });
                        SendToWeb(new { type = "groundOps", services = _groundOpsManager.Services });
                        
                        // Force update of fuel sheet if it is already open on another screen.
                        if (_fuelSheetWindow != null)
                        {
                            Dispatcher.Invoke(() => {
                                OpenFuelSheetWindow();
                            });
                        }
                    }
                    if (dupeIndex >= 0)
                    {
                        _rotationQueue[dupeIndex] = response;
                    }

                    if (!isDupe)
                    {
                        _rotationQueue.Add(response);
                    }
                    
                    // Step 4: Cascade time approximations to Dummy legs in the queue
                    FlightSupervisor.UI.Models.SimBrief.SimBriefResponse currentCascade = _currentResponse;
                    foreach (var queued in _rotationQueue)
                    {
                        if (queued.IsDummy && currentCascade?.Times != null)
                        {
                            if (long.TryParse(currentCascade.Times.SchedIn, out long lastIn))
                            {
                                if (queued.Times == null) queued.Times = new FlightSupervisor.UI.Models.SimBrief.TimesInfo();
                                
                                long newOut = lastIn + (50 * 60); // 50 mins minimal turnaround
                                long newIn = newOut + (60 * 60); // 60 mins dummy block time
                                
                                queued.Times.SchedOut = newOut.ToString();
                                queued.Times.SchedIn = newIn.ToString();
                            }
                        }
                        currentCascade = queued; // advance the chain
                    }
                    
                    var weatherService = new WeatherBriefingService(units);
                    var briefingData = weatherService.GenerateBriefing(response, _isAtWrongAirport);

                    var passengerService = new FlightSupervisor.UI.Services.PassengerManifestService();
                    var manifestData = passengerService.GenerateManifest(response, _profileManager.CurrentProfile);

                    var aProfile = _airlineDb.GetProfileFor(response.General?.Airline ?? "");

                    SendToWeb(new { 
                        type = "flightData", 
                        data = response,
                        briefing = briefingData,
                        manifest = manifestData,
                        airlineProfile = aProfile
                    });

                    SendToWeb(new { type = "manifestUpdate", manifest = manifestData });
                    
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

            _msfsWatcherTimer = new System.Windows.Threading.DispatcherTimer();
            _msfsWatcherTimer.Interval = TimeSpan.FromSeconds(5);
            _msfsWatcherTimer.Tick += (s, ev) => {
                if (!_simConnectService.IsConnected)
                {
                    var procs = System.Diagnostics.Process.GetProcessesByName("FlightSimulator");
                    if (procs.Length > 0)
                    {
                        var hwHelper = new WindowInteropHelper(this);
                        _simConnectService.Connect(hwHelper.Handle);
                    }
                }
            };
            _msfsWatcherTimer.Start();
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
            FlightSupervisor.UI.Services.WindowSettingsManager.SaveWindowState(this, "MainWindow");
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

                var altnIcao = _currentResponse.Alternates?.Count > 0 ? _currentResponse.Alternates[0].IcaoCode : null;
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

                var altnIcao = _currentResponse.Alternates?.Count > 0 ? _currentResponse.Alternates[0].IcaoCode : null;
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
                var briefingData = weatherService.GenerateBriefing(_currentResponse, _isAtWrongAirport);
                SendToWeb(new { type = "briefingUpdate", briefing = briefingData });
            }
        }

        private double CalculateHaversineDistanceNM(double lat1, double lon1, double lat2, double lon2)
        {
            var r = 3440.065; // Radius of earth in Nautical Miles
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return r * c;
        }

        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        public void ExecuteTimeSkip(int minutes)
        {
            if (minutes > 0)
            {
                DateTime? currentEffectiveSobt = _groundOpsManager.TargetSobt ?? _nextSobtOverride;
                // Butoir de 5 minutes avant le départ (TargetSobt) - Applicable à TOUTES les jambes
                if (currentEffectiveSobt.HasValue)
                {
                    double timeToSobt = (currentEffectiveSobt.Value - _currentSimTime).TotalMinutes;
                    int maxSkip = (int)Math.Max(0, timeToSobt - 5);
                    
                    if (minutes > maxSkip)
                    {
                        minutes = maxSkip;
                        if (minutes <= 0)
                        {
                            SendToWeb(new { type = "log", message = $"[SYSTEM] Action Denied. Time Skip is unavailable within 5 minutes of departure. (Time to SOBT: {timeToSobt:F1} min)" });
                            return;
                        }
                        SendToWeb(new { type = "log", message = $"[SYSTEM] Time Skip limited to maintain 5 minutes departure buffer. (Time to SOBT: {timeToSobt:F1} min)" });
                    }
                }

                DateTime newSimTime = _currentSimTime.AddMinutes(minutes);

                if (_cabinManager != null)
                {
                    int simAdvanceSec = minutes * 60;
                    _cabinManager.CurrentSimLocalTime = _cabinManager.CurrentSimLocalTime.AddSeconds(simAdvanceSec);
                    _cabinManager.FastForward(simAdvanceSec, _phaseManager.CurrentPhase);
                }

                int loops = minutes * 60;
                for (int i = 0; i < loops; i++)
                {
                    _currentSimTime = _currentSimTime.AddSeconds(1);
                    _groundOpsManager?.Tick(_currentSimTime);
                    _groundOpsResourceService?.Tick(1000); // Pass exactly 1000ms
                }
                
                // Redundant safety set
                _currentSimTime = newSimTime;
                _ignoreSimTimeUntil = newSimTime.AddSeconds(-15);

                if (_simConnectService != null && _simConnectService.IsConnected && _currentSimTime.Year > 2000)
                {
                    _simConnectService.SendTimeWarpCommand(newSimTime);
                    SendToWeb(new { type = "log", message = $"[SYSTEM] Dispatched MSFS TimeWarp to {newSimTime:HH:mm}Z. Syncing..." });
                }
            }
        }

        public void ExecuteForcePhase(string phaseName)
        {
            if (Enum.TryParse<FlightPhase>(phaseName, true, out var phase))
            {
                _phaseManager.ForcePhase(phase);
                SendToWeb(new { type = "flightPhaseUpdate", phase = _phaseManager.GetLocalizedPhaseName() });
            }
        }

        private void GenerateAgentDebugLog()
        {
            try
            {
                var dump = new
                {
                    Timestamp = DateTime.UtcNow.ToString("O"),
                    Session = new {
                        CurrentPhase = _phaseManager.CurrentPhase.ToString(),
                        SessionFlightsCompleted = _cabinManager.SessionFlightsCompleted,
                        InternalClock = _currentSimTime.ToString("O")
                    },
                    Cabin = new {
                        PassengerCount = _cabinManager.PassengerManifest?.Count ?? 0,
                        Satisfaction = _cabinManager.Satisfaction,
                        Anxiety = _cabinManager.PassengerAnxiety,
                        SecuringProgress = _cabinManager.SecuringProgress,
                        IsFirstFlightClean = _cabinManager.FirstFlightClean,
                        WaterLevel = _cabinManager.WaterLevel,
                        WasteLevel = _cabinManager.WasteLevel
                    },
                    GroundOps = new {
                        IsStarted = _groundOpsManager.Services.Any(s => s.State == FlightSupervisor.UI.Services.GroundServiceState.InProgress),
                        IsFuelSheetValidated = _groundOpsManager.IsFuelSheetValidated,
                        TargetSobt = _groundOpsManager.TargetSobt?.ToString("O"),
                        Services = _groundOpsManager.Services.Select(s => new {
                            s.Name,
                            s.State,
                            s.ElapsedSec,
                            s.TotalDurationSec,
                            s.IsAvailable
                        }).ToList()
                    },
                    Telemetry = new {
                        FobKg = _currentFobKg
                    }
                };

                string json = System.Text.Json.JsonSerializer.Serialize(dump, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                string docsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\docs");
                if (System.IO.Directory.Exists(docsDir))
                {
                    string fpath = System.IO.Path.Combine(docsDir, "FlightSupervisor_Agent_Dump.json");
                    System.IO.File.WriteAllText(fpath, json);
                    SendToWeb(new { type = "log", message = $"[DEBUG] Agent Debug Dump written to: {fpath}" });
                }
                else
                {
                    string appData = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor");
                    if (!System.IO.Directory.Exists(appData)) System.IO.Directory.CreateDirectory(appData);
                    string fpath = System.IO.Path.Combine(appData, "FlightSupervisor_Agent_Dump.json");
                    System.IO.File.WriteAllText(fpath, json);
                    SendToWeb(new { type = "log", message = $"[DEBUG] Agent Debug Dump written to: {fpath}" });
                }
            }
            catch (Exception ex)
            {
                SendToWeb(new { type = "errorMessage", title = "DUMP FAILED", message = ex.Message });
            }
        }
    }
}


