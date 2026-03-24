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
        private double? _lastLogThrottle = null;
        private double? _lastLogSpoilers = null;
        private bool? _lastLogLightBeacon = null;
        private bool? _lastLogLightStrobe = null;
        private bool? _lastLogLightNav = null;
        private bool? _lastLogLightLanding = null;
        private bool? _lastLogLightTaxi = null;
        
        private GroundOpsManager _groundOpsManager;
        private System.Windows.Threading.DispatcherTimer _uiTimer;
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private PanelServerService? _panelServer;
        private SuperScoreManager _scoreManager;

        public MainWindow()
        {
            InitializeComponent();
            _simBriefService = new SimBriefService();

            _groundOpsManager = new GroundOpsManager();
            _groundOpsManager.OnOpsCompleted += () => SendToWeb(new { type = "groundOpsComplete" });
            _groundOpsManager.OnOpsLog += msg => SendToWeb(new { type = "log", message = msg });

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
                _groundOpsManager.Tick();
                if (_groundOpsManager.IsAnyOperationInProgress() && (_phaseManager.GroundSpeed > 1.0 || !_phaseManager.IsOnGround))
                {
                    _groundOpsManager.AbortAllOperations();
                    _scoreManager.CancelFlight("Flight Cancelled: Unauthorized movement during Ground Operations!");
                    SendToWeb(new { type = "flightCancelled" });
                }
                SendTelemetryToWeb();
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
                    else if (phase == FlightPhase.Arrived)
                    {
                        if (_aibt == null && _currentSimTime != DateTime.MinValue) 
                            _aibt = _currentSimTime;

                        if (_currentResponse?.Times?.SchedIn != null && long.TryParse(_currentResponse.Times.SchedIn, out long sibtUnix))
                        {
                            long aibtUnix = ((DateTimeOffset)_currentSimTime).ToUnixTimeSeconds();
                            long rawDelaySec = aibtUnix - sibtUnix;
                            
                            int groundOpsDelaySec = _groundOpsManager.Services.Sum(s => s.DelayAddedSec);
                            long effectiveDelaySec = rawDelaySec - groundOpsDelaySec;

                            if (effectiveDelaySec > 300) // 5 minutes late blaming the pilot
                            {
                                int penalty = effectiveDelaySec > 900 ? -100 : -50;
                                string timeStr = $"{(rawDelaySec / 60)} min";
                                string groundOpsPardon = groundOpsDelaySec > 0 ? $" (Amnistie Sol: -{groundOpsDelaySec / 60}m)" : "";
                                _scoreManager.AddScore(penalty, $"Retard à l'arrivée: {timeStr}{groundOpsPardon}");
                            }
                            else if (effectiveDelaySec <= 300 && rawDelaySec > 300)
                            {
                                _scoreManager.AddScore(50, $"Amnistie Retard : {rawDelaySec / 60}m justifiés par les Ops Sol !");
                            }
                            else if (rawDelaySec <= 300)
                            {
                                _scoreManager.AddScore(100, $"Ponctualité : Arrivée à l'heure !");
                            }

                            // Generate Flight Report
                            long schedOut = 0;
                            if (_currentResponse?.Times?.SchedOut != null) long.TryParse(_currentResponse.Times.SchedOut, out schedOut);
                            
                            var report = new
                            {
                                Score = _scoreManager.CurrentScore,
                                Dep = _currentResponse?.Origin?.IcaoCode ?? "",
                                Arr = _currentResponse?.Destination?.IcaoCode ?? "",
                                FlightNo = _currentResponse?.General?.FlightNumber ?? "",
                                Airline = _currentResponse?.General?.Airline ?? "",
                                BlockTime = _aobt.HasValue && _aibt.HasValue ? (int)(_aibt.Value - _aobt.Value).TotalMinutes : 0,
                                SchedBlockTime = schedOut > 0 ? (sibtUnix - schedOut) / 60 : 0,
                                TouchdownFpm = _phaseManager.TouchdownFpm,
                                TouchdownGForce = _phaseManager.TouchdownGForce,
                                Zfw = _currentResponse?.Weights?.EstZfw ?? "",
                                Tow = _currentResponse?.Weights?.EstTow ?? "",
                                BlockFuel = _currentResponse?.Fuel?.PlanRamp ?? "",
                                DelaySec = effectiveDelaySec,
                                RawDelaySec = rawDelaySec
                            };
                            SendToWeb(new { type = "flightReport", report });
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

            _panelServer = new PanelServerService(new SimBriefService(), new WeatherBriefingService(), _phaseManager);

            try { _panelServer.StartServer(); } catch { }

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
            MainWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            MainWebView.CoreWebView2.Navigate("http://app.local/index.html");

            var saveFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor_User.txt");
            var username = System.IO.File.Exists(saveFilePath) ? System.IO.File.ReadAllText(saveFilePath) : "";
            
            MainWebView.CoreWebView2.NavigationCompleted += (s, e) => 
            {
                if (!string.IsNullOrEmpty(username))
                    SendToWeb(new { type = "savedUsername", username = username });
                
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
                }
                else if (action == "startGroundOps")
                {
                    Dispatcher.Invoke(() => {
                        _groundOpsManager.StartOps();
                    });
                }
                else if (action == "cancelFlight")
                {
                    _phaseManager.Reset();
                    _scoreManager.Reset();
                    _currentResponse = null;
                    _groundOpsManager.AbortAllOperations();
                    _groundOpsManager.Services.Clear();
                    _aobt = null;
                    _aibt = null;
                    SendToWeb(new { type = "flightReset" });
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

        private void SendTelemetryToWeb()
        {
            SendToWeb(new 
            {
                type = "telemetry",
                phase = _phaseManager.CurrentPhase.ToString(),
                altitude = _lastKnownAltitude,
                groundSpeed = _lastKnownGroundSpeed,
                radioHeight = _lastKnownRadioHeight,
                isGearDown = _isGearDown
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
                    _currentResponse = response;
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
                    
                    var weatherService = new WeatherBriefingService(units);
                    var briefingText = weatherService.GenerateBriefing(response);

                    // Convert complex response to a simpler format for JS to avoid serialization deep nesting loops if any
                    SendToWeb(new { 
                        type = "flightData", 
                        data = response,
                        briefing = briefingText
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
    }
}