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
        
        private double _lastKnownGroundSpeed = 0;
        private double _lastKnownAirspeed = 0;
        private double _lastKnownAltitude = 0;
        private double _lastKnownRadioHeight = 0;
        private bool _isParkingBrakeSet = false;
        private double _lastKnownThrottle = 0;
        private double _lastKnownPitch = 0;
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
        
        private GroundOpsManager _groundOpsManager;
        private System.Windows.Threading.DispatcherTimer _uiTimer;
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private PanelServerService? _panelServer;
        private SuperScoreManager _scoreManager;
        private CabinManager _cabinManager;
        private AirlineProfileManager _airlineDb;
        private FlightSupervisor.UI.Services.GroundEventEngine _eventEngine;
        public AirlineProfile? CurrentAirline { get; private set; }
        private ProfileManager _profileManager;

        public MainWindow()
        {
            InitializeComponent();
            _simBriefService = new SimBriefService();
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

            _cabinManager = new CabinManager();
            _cabinManager.OnCrewMessage += (level, msg) => SendToWeb(new { type = "cabinLog", level = level, message = msg });
            _cabinManager.OnPenaltyTriggered += (points, reason) => {
                if (_scoreManager != null) _scoreManager.AddScore(points, reason, ScoreCategory.Comfort);
            };
            _cabinManager.OnPncStatusChanged += (status, state) => {
                Dispatcher.Invoke(() => SendToWeb(new { type = "pncStatus", status = status, state = state.ToString() }));
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

            // Start Dashboard update loop
            _uiTimer = new System.Windows.Threading.DispatcherTimer();
            _uiTimer.Interval = TimeSpan.FromSeconds(1);
            _uiTimer.Tick += (s, e) => { 
                _groundOpsManager.Tick(_currentSimTime);

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
                _cabinManager.Tick(_phaseManager.GForce, _phaseManager.Heading, isBoardingComplete, _currentSimTime, sobtDate, _phaseManager.CurrentPhase);
                
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

                            if (effectiveDelaySec > 300) // 5 minutes late blaming the pilot
                            {
                                int penalty = effectiveDelaySec > 900 ? -100 : -50;
                                string timeStr = $"{(rawDelaySec / 60)} min";
                                string groundOpsPardon = groundOpsDelaySec > 0 ? $" (Amnistie Sol: -{groundOpsDelaySec / 60}m)" : "";
                                _scoreManager.AddScore(penalty, $"Retard à l'arrivée: {timeStr}{groundOpsPardon}", ScoreCategory.Operations);
                            }
                            else if (effectiveDelaySec <= 300 && rawDelaySec > 300)
                            {
                                _scoreManager.AddScore(50, $"Amnistie Retard : {rawDelaySec / 60}m justifiés par les Ops Sol !", ScoreCategory.Operations);
                            }
                            else if (rawDelaySec <= 300)
                            {
                                _scoreManager.AddScore(100, $"Ponctualité : Arrivée à l'heure !", ScoreCategory.Operations);
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
                        var objectiveResults = _scoreManager.EvaluateObjectives(CurrentAirline, effectiveDelaySec, (int)Math.Round(_cabinManager.ComfortLevel), _phaseManager.TouchdownFpm, cateringPerformed);

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
                                objectiveResults.All(o => o.Passed));

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
                                Objectives = objectiveResults.Cast<object>().ToList(),
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
                                Objectives = objectiveResults.Cast<object>().ToList(),
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
            _scoreManager = new SuperScoreManager(_phaseManager, _simConnectService);
            _scoreManager.OnScoreChanged += (score, delta, reason) => {
                Dispatcher.Invoke(() => SendToWeb(new { type = "scoreUpdate", score = score, delta = delta, msg = reason }));
            };

            _simConnectService.OnConnectionStateChanged += state => {
                Dispatcher.Invoke(() => SendToWeb(new { type = "simConnectStatus", status = state }));
            };
            _simConnectService.OnAltitudeReceived += alt => {
                _lastKnownAltitude = alt;
                _phaseManager.UpdateTelemetry(_lastKnownGroundSpeed, _lastKnownAirspeed, alt, _lastKnownRadioHeight, _isParkingBrakeSet, _isGearDown, _lastKnownThrottle, _lastKnownPitch, _lastKnownBank);
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

            // Fenix Custom Hooks
            _simConnectService.OnFenixSeatbeltsReceived += sb => { 
                _cabinManager.UpdateSeatbelts(sb, _phaseManager.CurrentPhase); 
                if (_lastLogSeatbelts != null && _lastLogSeatbelts != sb) SendToWeb(new { type = "log", message = sb ? "Fasten Seatbelts ON" : "Fasten Seatbelts OFF" });
                _lastLogSeatbelts = sb;
            };
            _simConnectService.OnFenixApuReceived += (mst, start, bleed) => {
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
            
            int? _lastLogFenixNose = null;
            _simConnectService.OnFenixNoseLightReceived += nl => { 
                _phaseManager.FenixNoseLight = nl;
                if (_lastLogFenixNose != null && _lastLogFenixNose != nl)
                {
                    string state = nl == 0 ? "OFF" : (nl == 1 ? "TAXI" : "TAKE-OFF");
                    SendToWeb(new { type = "log", message = $"Nose Light {state} (Fenix)" });
                }
                _lastLogFenixNose = nl;
            };
            _simConnectService.OnFenixRunwayTurnoffReceived += rwy => { _phaseManager.IsRunwayTurnoffLightOn = rwy; };

            _simConnectService.OnSimTimeReceived += time => { 
                _currentSimTime = time;
                Dispatcher.Invoke(() => SendToWeb(new { type = "simTime", time = time.ToString("HH:mm") + "z", rawUnix = ((DateTimeOffset)time).ToUnixTimeSeconds() }));
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
            MainWebView.CoreWebView2.Navigate("http://app.local/index.html");

            var saveFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor_User.txt");
            var username = System.IO.File.Exists(saveFilePath) ? System.IO.File.ReadAllText(saveFilePath) : "";
            
            MainWebView.CoreWebView2.NavigationCompleted += (s, e) => 
            {
                if (!string.IsNullOrEmpty(username))
                    SendToWeb(new { type = "savedUsername", username = username });
                
                // Initialize the Pilot Profile in the UI
                if (_profileManager != null && _profileManager.CurrentProfile != null)
                {
                    SendToWeb(new { type = "InitProfile", payload = _profileManager.CurrentProfile });
                }

                // Lancer SimConnect SEULEMENT quand la page JS est prête à intercepter les messages !
                var helper = new WindowInteropHelper(this);
                _simConnectService.Connect(helper.Handle);
            };
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
                else if (action == "acceptContract")
                {
                    _scoreManager.IsContractAccepted = true;
                    SendToWeb(new { type = "log", message = "[Tycoon] Airline Objective Contract Accepted." });
                }
                else if (action == "fetchLogbook")
                {
                    var history = FlightSupervisor.UI.Services.FlightLogger.GetLogbook();
                    SendToWeb(new { type = "logbookData", history });
                }
                else if (action == "resolveGroundEvent")
                {
                    var eventId = doc.RootElement.GetProperty("eventId").GetString();
                    var choiceId = doc.RootElement.GetProperty("choiceId").GetString();
                    
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
                else if (action == "fetch") 
                {
                    var username = doc.RootElement.GetProperty("username").GetString() ?? "";
                    var remember = doc.RootElement.GetProperty("remember").GetBoolean();
                    
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
                    
                    await FetchFlightPlan(username, remember, units);
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
                    var srvName = doc.RootElement.GetProperty("service").GetString();
                    _groundOpsManager.SkipService(srvName);
                    
                    var srv = _groundOpsManager.Services.FirstOrDefault(s => s.Name == srvName);
                    if (srv != null)
                    {
                        if (srvName == "Catering") _cabinManager.CateringCompletion = srv.ProgressPercent;
                        if (srvName == "Cargo") _cabinManager.BaggageCompletion = srv.ProgressPercent;
                    }
                }
                else if (action == "startService")
                {
                    var srvName = doc.RootElement.GetProperty("service").GetString();
                    if (!string.IsNullOrEmpty(srvName))
                    {
                        _groundOpsManager.StartManualService(srvName);
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
                else if (action == "announceCabin")
                {
                    var annType = doc.RootElement.GetProperty("annType").GetString();
                    if (annType != null) _cabinManager.AnnounceToCabin(annType);
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
                else if (action == "changeLanguage")
                {
                    var lang = doc.RootElement.GetProperty("language").GetString() ?? "en";
                    FlightSupervisor.UI.Services.LocalizationService.CurrentLanguage = lang;
                    
                    if (_currentResponse != null)
                    {
                        var weatherService = new FlightSupervisor.UI.Services.WeatherBriefingService(new FlightSupervisor.UI.Models.UnitPreferences());
                        var briefingText = weatherService.GenerateBriefing(_currentResponse);
                        SendToWeb(new { type = "briefingUpdate", briefing = briefingText });
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
                altitude = _lastKnownAltitude,
                groundSpeed = _lastKnownGroundSpeed,
                radioHeight = _lastKnownRadioHeight,
                isGearDown = _isGearDown,
                anxiety = Math.Round(_cabinManager.PassengerAnxiety, 1),
                comfort = Math.Round(_cabinManager.ComfortLevel, 1),
                airline = CurrentAirline
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

        private async System.Threading.Tasks.Task FetchFlightPlan(string username, bool remember, FlightSupervisor.UI.Models.UnitPreferences? units = null)
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
                    _phaseManager.Reset();
                    _scoreManager.Reset();
                    _cabinManager.Reset();
                    _eventEngine.Reset();
                    _currentResponse = response;
                    CurrentAirline = _airlineDb.GetProfileFor(response.General?.Airline ?? "");
                    _cabinManager.InitializeFlightDemographics(CurrentAirline);
                    _groundOpsManager.InitializeFromSimBrief(response);
                    SendToWeb(new { type = "groundOpsReady" });

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
                    
                    var weatherService = new WeatherBriefingService(units);
                    var briefingText = weatherService.GenerateBriefing(response);

                    var passengerService = new FlightSupervisor.UI.Services.PassengerManifestService();
                    var manifestData = passengerService.GenerateManifest(response);

                    // Convert complex response to a simpler format for JS to avoid serialization deep nesting loops if any
                    SendToWeb(new { 
                        type = "flightData", 
                        data = response,
                        briefing = briefingText,
                        manifest = manifestData
                    });
                    
                    SendToWeb(new { type = "fetchStatus", status = "success", message = "Flight plan loaded !" });
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
    }
}