using System;
using System.Windows;
using System.Windows.Interop;
using System.Text.Json;
using System.Runtime.InteropServices;
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
        private bool _isGearDown = true;
        private DateTime _currentSimTime = DateTime.MinValue;
        private DateTime? _aobt = null;
        private DateTime? _aibt = null;
        
        private GroundOpsManager _groundOpsManager;
        private System.Windows.Threading.DispatcherTimer _uiTimer;
        private PanelServerService? _panelServer;

        public MainWindow()
        {
            InitializeComponent();
            _simBriefService = new SimBriefService();

            _groundOpsManager = new GroundOpsManager();
            _groundOpsManager.OnOpsCompleted += () => SendToWeb(new { type = "groundOpsComplete" });

            _uiTimer = new System.Windows.Threading.DispatcherTimer();
            _uiTimer.Interval = TimeSpan.FromSeconds(1);
            _uiTimer.Tick += (s, e) => { 
                _groundOpsManager.Tick();
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
                    }
                    SendToWeb(new { type = "phaseUpdate", phase = phase.ToString() });
                });
            };
            _phaseManager.OnPenaltyTriggered += msg => {
                Dispatcher.Invoke(() => SendToWeb(new { type = "penalty", message = msg }));
            };

            _panelServer = new PanelServerService(new SimBriefService(), new WeatherBriefingService(), _phaseManager);
            try { _panelServer.StartServer(); } catch { }

            _simConnectService = new SimConnectService();
            _simConnectService.OnConnectionStateChanged += state => {
                Dispatcher.Invoke(() => SendToWeb(new { type = "simConnectStatus", status = state }));
            };
            _simConnectService.OnAltitudeReceived += alt => {
                _lastKnownAltitude = alt;
                _phaseManager.UpdateTelemetry(_lastKnownGroundSpeed, _lastKnownAirspeed, alt, _lastKnownRadioHeight, _isParkingBrakeSet, _isGearDown);
            };
            _simConnectService.OnGearDownReceived += gd => { _isGearDown = gd; };
            _simConnectService.OnRadioHeightReceived += rh => { _lastKnownRadioHeight = rh; };
            _simConnectService.OnGroundSpeedReceived += gs => { _lastKnownGroundSpeed = gs; };
            _simConnectService.OnAirspeedReceived += ias => { _lastKnownAirspeed = ias; };
            _simConnectService.OnParkingBrakeReceived += pb => { _isParkingBrakeSet = pb; };
            _simConnectService.OnSimTimeReceived += time => { 
                _currentSimTime = time;
                Dispatcher.Invoke(() => SendToWeb(new { type = "simTime", time = time.ToString("HH:mm") + "z" }));
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
            if (System.IO.File.Exists(saveFilePath))
            {
                var username = System.IO.File.ReadAllText(saveFilePath);
                MainWebView.CoreWebView2.NavigationCompleted += (s, e) => 
                {
                     SendToWeb(new { type = "savedUsername", username = username });
                };
            }
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
                    var username = doc.RootElement.GetProperty("username").GetString();
                    var remember = doc.RootElement.GetProperty("remember").GetBoolean();
                    await FetchFlightPlan(username, remember);
                }
                else if (action == "skipService")
                {
                    var srvName = doc.RootElement.GetProperty("service").GetString();
                    _groundOpsManager.SkipService(srvName);
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

        private async System.Threading.Tasks.Task FetchFlightPlan(string username, bool remember)
        {
            if (string.IsNullOrEmpty(username)) return;

            if (remember)
            {
                var saveFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor_User.txt");
                System.IO.File.WriteAllText(saveFilePath, username);
            }

            SendToWeb(new { type = "fetchStatus", status = "loading", message = "Fetching flight plan..." });

            try
            {
                var response = await _simBriefService.FetchFlightPlanAsync(username);

                if (response != null && response.Fetch?.Status == "Success")
                {
                    _currentResponse = response;
                    _groundOpsManager.InitializeFromSimBrief(response);
                    _groundOpsManager.StartOps();
                    
                    var weatherService = new WeatherBriefingService();
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
            
            _simConnectService.Connect(helper.Handle);
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