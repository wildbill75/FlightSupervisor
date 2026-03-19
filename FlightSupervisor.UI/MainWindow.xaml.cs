using System;
using System.Windows;
using System.Windows.Interop;
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
        private DateTime _currentSimTime = DateTime.MinValue;
        private DateTime? _aobt = null; // Actual Off-Block Time
        private DateTime? _aibt = null; // Actual In-Block Time
        
        private PanelServerService? _panelServer;

        public MainWindow()
        {
            InitializeComponent();
            _simBriefService = new SimBriefService();
            
            var saveFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor_User.txt");
            if (System.IO.File.Exists(saveFilePath))
                SimBriefUsernameInput.Text = System.IO.File.ReadAllText(saveFilePath);

            SimBriefUsernameInput.Focus();

            _phaseManager = new FlightPhaseManager();
            _phaseManager.OnPhaseChanged += phase => {
                Dispatcher.Invoke(() => {
                    PhaseStatusText.Text = $"Phase: {phase}";
                    
                    if (phase == FlightPhase.Pushback || phase == FlightPhase.TaxiOut)
                    {
                        if (_aobt == null && _currentSimTime != DateTime.MinValue) 
                        {
                            _aobt = _currentSimTime;
                            UpdateFlightPlanDisplay();
                        }
                    }
                    else if (phase == FlightPhase.Arrived)
                    {
                        if (_aibt == null && _currentSimTime != DateTime.MinValue) 
                        {
                            _aibt = _currentSimTime;
                            UpdateFlightPlanDisplay();
                        }
                    }
                });
            };
            _phaseManager.OnPenaltyTriggered += msg => {
                Dispatcher.Invoke(() => MessageBox.Show(msg, "SuperScore Penalty!", MessageBoxButton.OK, MessageBoxImage.Warning));
            };

            // Start Local Web Server for In-Game Panel Bridge
            _panelServer = new PanelServerService(new SimBriefService(), new WeatherBriefingService(), _phaseManager);
            try { _panelServer.StartServer(); } catch { /* Ignore if port in use */ }

            _simConnectService = new SimConnectService();
            _simConnectService.OnConnectionStateChanged += state => {
                Dispatcher.Invoke(() => SimConnectStatusText.Text = state);
            };
            _simConnectService.OnAltitudeReceived += alt => {
                _lastKnownAltitude = alt;
                Dispatcher.Invoke(() => SimConnectStatusText.Text = $"MSFS Connected | Alt: {alt:F0} ft | GS: {_lastKnownGroundSpeed:F0} kts | PKG BRK: {(_isParkingBrakeSet ? "ON" : "OFF")}");
                _phaseManager.UpdateTelemetry(_lastKnownGroundSpeed, _lastKnownAirspeed, alt, _lastKnownRadioHeight, _isParkingBrakeSet);
            };
            _simConnectService.OnRadioHeightReceived += rh => {
                _lastKnownRadioHeight = rh;
            };
            _simConnectService.OnGroundSpeedReceived += gs => {
                _lastKnownGroundSpeed = gs;
            };
            _simConnectService.OnAirspeedReceived += ias => {
                _lastKnownAirspeed = ias;
            };
            _simConnectService.OnParkingBrakeReceived += pb => {
                _isParkingBrakeSet = pb;
            };
            _simConnectService.OnSimTimeReceived += time => {
                _currentSimTime = time;
                Dispatcher.Invoke(() => SimTimeText.Text = time.ToString("HH:mm") + "z");
            };

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            // In a real scenario, this hooks the window procedure to MSFS messages
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
            if (_simConnectService != null)
                _simConnectService.Disconnect();
                
            _panelServer?.StopServer();
        }

        private void OpenTester_Click(object sender, RoutedEventArgs e)
        {
            var tester = new BriefingTesterWindow();
            tester.Show();
        }

        private async void FetchPlanButton_Click(object sender, RoutedEventArgs e)
        {
            var username = SimBriefUsernameInput.Text.Trim();
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Please enter a SimBrief Username.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SaveUsernameCheckbox.IsChecked == true)
            {
                var saveFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlightSupervisor_User.txt");
                System.IO.File.WriteAllText(saveFilePath, username);
            }

            FetchPlanButton.IsEnabled = false;
            StatusText.Text = "Fetching flight plan...";
            FlightPlanResultText.Text = "";

            try
            {
                var response = await _simBriefService.FetchFlightPlanAsync(username);

                if (response != null && response.Fetch?.Status == "Success")
                {
                    _currentResponse = response;
                    StatusText.Text = "Flight plan loaded successfully!";
                    UpdateFlightPlanDisplay();
                }
                else
                {
                    StatusText.Text = "Error fetching flight plan.";
                    FlightPlanResultText.Text = "Could not parse or fetch flight plan. Please verify the Username and ensure 'Generate Flight' was clicked on SimBrief.";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "An error occurred.";
                FlightPlanResultText.Text = ex.Message;
            }
            finally
            {
                FetchPlanButton.IsEnabled = true;
            }
        }

        private void UpdateFlightPlanDisplay()
        {
            if (_currentResponse == null) return;
            var response = _currentResponse;

            var gen = response.General;
            var orig = response.Origin;
            var dest = response.Destination;
            var wgt = response.Weights;
            var acft = response.Aircraft;
            var prm = response.Params;
            
            double.TryParse(response.Times?.EstTimeEnroute ?? "0", out double enrouteSecs);
            var formattedTime = TimeSpan.FromSeconds(enrouteSecs).ToString("hh\\:mm");

            long.TryParse(response.Times?.SchedOut ?? "0", out long schedOutUnix);
            long.TryParse(response.Times?.SchedIn ?? "0", out long schedInUnix);
            string sobtStr = schedOutUnix > 0 ? DateTimeOffset.FromUnixTimeSeconds(schedOutUnix).UtcDateTime.ToString("HH:mm") + "z" : "--:--z";
            string sibtStr = schedInUnix > 0 ? DateTimeOffset.FromUnixTimeSeconds(schedInUnix).UtcDateTime.ToString("HH:mm") + "z" : "--:--z";
            
            string aobtStr = _aobt.HasValue ? _aobt.Value.ToString("HH:mm") + "z" : "--:--z";
            string aibtStr = _aibt.HasValue ? _aibt.Value.ToString("HH:mm") + "z" : "--:--z";

            string airlineName = GetAirlineName(gen?.Airline ?? "");
            
            var weatherService = new WeatherBriefingService();
            var briefingText = weatherService.GenerateBriefing(response);
            
            string blockFuel = response.Fuel?.PlanRamp ?? wgt?.EstBlock ?? wgt?.BlockFuel ?? "N/A";

            string flightLevel = gen?.InitialAlt;
            if (string.IsNullOrWhiteSpace(flightLevel) && !string.IsNullOrWhiteSpace(gen?.StepClimbString))
            {
                var scParts = gen.StepClimbString.Split('/');
                flightLevel = scParts.Length > 1 ? scParts[1] : scParts[0];
            }
            if (!string.IsNullOrWhiteSpace(flightLevel))
            {
                if (flightLevel.EndsWith("00") && flightLevel.Length >= 4)
                    flightLevel = flightLevel.Substring(0, flightLevel.Length - 2);
                flightLevel = flightLevel.TrimStart('0');
                
                if (double.TryParse(flightLevel, out double fl))
                {
                    _phaseManager.TargetCruiseAltitude = fl * 100;
                }
            }

            FlightPlanResultText.Text = 
                $"Airline: {airlineName} ({gen?.Airline})\n" +
                $"Flight Number: {gen?.FlightNumber}\n" +
                $"Aircraft: {acft?.Name} ({acft?.BaseType})\n" +
                $"Cruise Altitude: FL {flightLevel}\n" +
                $"Step Climbs: {gen?.StepClimbString}\n" +
                $"---------------------------\n" +
                $"Origin: {orig?.IcaoCode} ({orig?.Name})\n" +
                $"Destination: {dest?.IcaoCode} ({dest?.Name})\n" +
                $"Route: {gen?.Route}\n" +
                $"---------------------------\n" +
                $"TIMETABLE (UTC) :\n" +
                $"Scheduled Off-Block (SOBT): {sobtStr}  |  Actual (AOBT): {aobtStr}\n" +
                $"Scheduled In-Block  (SIBT): {sibtStr}  |  Actual (AIBT): {aibtStr}\n" +
                $"Est. Time Enroute: {formattedTime}\n" +
                $"---------------------------\n" +
                $"Passengers: {wgt?.PaxCount}\n" +
                $"ZFW: {wgt?.EstZfw} {prm?.Units}\n" +
                $"Block Fuel: {blockFuel} {prm?.Units}\n" +
                $"---------------------------\n" +
                $"COMMANDER BRIEFING:\n{briefingText}\n" +
                $"---------------------------\n" +
                $"Origin METAR: {response.Weather?.OrigMetar}\n" +
                $"Origin TAF: {response.Weather?.OrigTaf}\n\n" +
                $"Destination METAR: {response.Weather?.DestMetar}\n" +
                $"Destination TAF: {response.Weather?.DestTaf}\n\n" +
                $"--- Alternates & Enroute Weather ---\n" +
                ExtractJsonStrings(response.Weather?.AltnMetar) + "\n" +
                ExtractJsonStrings(response.Weather?.EnrtMetar) + "\n" +
                ExtractJsonStrings(response.Weather?.AltnTaf) + "\n" +
                ExtractJsonStrings(response.Weather?.EnrtTaf);
        }

        private string ExtractJsonStrings(System.Text.Json.JsonElement? element)
        {
            if (!element.HasValue) return "";
            var e = element.Value;
            if (e.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var item in e.EnumerateArray())
                {
                    if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                        sb.AppendLine(item.GetString());
                }
                return sb.ToString().TrimEnd();
            }
            if (e.ValueKind == System.Text.Json.JsonValueKind.String)
                return e.GetString() ?? "";
            return "";
        }

        private string GetAirlineName(string icao)
        {
            if (string.IsNullOrWhiteSpace(icao)) return "Unknown";
            var upper = icao.ToUpperInvariant();
            return upper switch
            {
                "FBU" => "French bee",
                "AFR" => "Air France",
                "RYR" => "Ryanair",
                "EZY" => "easyJet",
                "BAW" => "British Airways",
                "DLH" => "Lufthansa",
                "UAE" => "Emirates",
                "QFA" => "Qantas",
                "AAL" => "American Airlines",
                "DAL" => "Delta Air Lines",
                "UAL" => "United Airlines",
                "SWA" => "Southwest Airlines",
                _ => icao // Fallback to ICAO if unknown
            };
        }

    }
}