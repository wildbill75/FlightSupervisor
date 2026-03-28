using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.FlightSimulator.SimConnect;

namespace FlightSupervisor.UI.Services
{
    public class SimConnectService
    {
        private SimConnect? _simconnect = null;
        private const int WM_USER_SIMCONNECT = 0x0402;
        private IntPtr _windowHandle;
        private bool _isNativelyConnected = false;
        private WasmLVarClient? _wasmClient = null;
        public bool IsWasmOverriding { get; set; } = true;

        public event Action<string>? OnConnectionStateChanged;
        public event Action<double>? OnAltitudeReceived;
        public event Action<double>? OnGroundSpeedReceived;
        public event Action<double>? OnAirspeedReceived;
        public event Action<double>? OnRadioHeightReceived;
        public event Action<DateTime>? OnSimTimeReceived;
        public event Action<bool>? OnParkingBrakeReceived;
        public event Action<bool>? OnGearDownReceived;
        public event Action<double>? OnFlapsReceived;
        public event Action<bool>? OnAutopilotReceived;
        public event Action<bool>? OnAutothrustReceived;
        public event Action<double>? OnThrottleReceived;
        public event Action<double>? OnSpoilersReceived;
        public event Action<bool>? OnLightBeaconReceived;
        public event Action<bool>? OnLightStrobeReceived;
        public event Action<bool>? OnLightNavReceived;
        public event Action<bool>? OnLightTaxiReceived;
        public event Action<bool>? OnLightLandingReceived;
        public event Action<double>? OnPitchReceived;
        public event Action<double>? OnBankReceived;
        public event Action<bool>? OnSimOnGroundReceived;
        public event Action<double>? OnVerticalSpeedReceived;
        public event Action<double>? OnGForceReceived;
        public event Action<bool, bool>? OnEngineCombustionReceived;
        public event Action<double>? OnHeadingReceived;
        public event Action<double, double>? OnWindReceived;
        public event Action<double, double, bool>? OnNavigationReceived;

        public event Action<bool>? OnCabinSeatbeltsChanged;
        public event Action<bool>? OnNoSmokingChanged;
        public event Action<bool, bool, bool>? OnApuStateChanged;
        public event Action<bool, bool>? OnEngineBleedsChanged;
        public event Action<bool, bool>? OnPacksChanged;
        public event Action<bool, bool, bool>? OnAntiIceChanged;
        public event Action<int, bool, bool>? OnEngineSwitchesChanged;
        public event Action<float, float, float>? OnCabinTemperatureTargetsChanged;
        public event Action<int>? OnNoseLightChanged;
        public event Action<bool>? OnRunwayTurnoffChanged;

        enum DEFINITIONS { PlaneData, GForceData }
        enum REQUESTS { PlaneDataReq, GForceReq }
        enum EVENTS { SetZuluHours, SetZuluMinutes }
        enum GROUP { Group0 }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct PlaneDataStruct
        {
            public double Altitude;
            public double GroundSpeed;
            public double IndicatedAirspeed;
            public double ZuluTime;
            public double ParkingBrakeIndicator;
            public double RadioHeight;
            public double GearHandle;
            public double FlapsHandleIndex;
            public double AutopilotMaster;
            public double AutothrustMaster;
            public double ThrottleLever;
            public double SpoilersHandle;
            public double LightBeacon;
            public double LightStrobe;
            public double LightNav;
            public double LightTaxi;
            public double LightLanding;
            public double Pitch;
            public double Bank;
            public double SimOnGround;
            public double VerticalSpeed;
            public double GForce;
            public double Eng1Combustion;
            public double Eng2Combustion;
            public double Heading;
            public double WindDirection;
            public double WindVelocity;
            public double NavLocalizerError;
            public double GpsCrossTrackError;
            public double HasLocalizer;
            public double CabinSeatbelts;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct GForceDataStruct
        {
            public double GForce;
        }

        public SimConnectService() { }

        public void Connect(IntPtr windowHandle)
        {
            if (_simconnect != null) return;
            _windowHandle = windowHandle;
            try
            {
                OnConnectionStateChanged?.Invoke("Connecting natively...");
                _simconnect = new SimConnect("Flight Supervisor", windowHandle, WM_USER_SIMCONNECT, null, 0);

                _simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnRecvOpen);
                _simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnRecvQuit);
                _simconnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnRecvException);
                _simconnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(SimConnect_OnRecvSimobjectData);
                _simconnect.OnRecvClientData += new SimConnect.RecvClientDataEventHandler(SimConnect_OnRecvClientData);

                _wasmClient = new WasmLVarClient(_simconnect);
                _wasmClient.OnFenixLVarsReceived += WasmClient_OnFenixLVarsReceived;
                _wasmClient.Initialize();

                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "INDICATED ALTITUDE", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "GROUND VELOCITY", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "AIRSPEED INDICATED", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "ZULU TIME", "Seconds", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "BRAKE PARKING INDICATOR", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "RADIO HEIGHT", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "GEAR HANDLE POSITION", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "FLAPS HANDLE INDEX", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "AUTOPILOT MASTER", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "AUTOPILOT THROTTLE ARM", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "GENERAL ENG THROTTLE LEVER POSITION:1", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "SPOILERS HANDLE POSITION", "Percent Over 100", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "LIGHT BEACON", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "LIGHT STROBE", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "LIGHT NAV", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "LIGHT TAXI", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "LIGHT LANDING", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "PLANE PITCH DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "PLANE BANK DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "SIM ON GROUND", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "VERTICAL SPEED", "Feet per minute", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "G FORCE", "GForce", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "GENERAL ENG COMBUSTION:1", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "GENERAL ENG COMBUSTION:2", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "PLANE HEADING DEGREES TRUE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "AMBIENT WIND DIRECTION", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "AMBIENT WIND VELOCITY", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "NAV LOCALIZER ERROR:1", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "GPS WP CROSS TRK", "Meters", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "NAV HAS LOCALIZER:1", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "CABIN SEATBELTS ALERT SWITCH", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                _simconnect.AddToDataDefinition(DEFINITIONS.GForceData, "G FORCE", "GForce", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                _simconnect.MapClientEventToSimEvent(EVENTS.SetZuluHours, "ZULU_HOURS_SET");
                _simconnect.MapClientEventToSimEvent(EVENTS.SetZuluMinutes, "ZULU_MINUTES_SET");

                _simconnect.RegisterDataDefineStruct<PlaneDataStruct>(DEFINITIONS.PlaneData);
                _simconnect.RegisterDataDefineStruct<GForceDataStruct>(DEFINITIONS.GForceData);

                _simconnect.RequestDataOnSimObject(REQUESTS.PlaneDataReq, DEFINITIONS.PlaneData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
                _simconnect.RequestDataOnSimObject(REQUESTS.GForceReq, DEFINITIONS.GForceData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.VISUAL_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
            }
            catch (COMException ex) { OnConnectionStateChanged?.Invoke($"MSFS not found ({ex.ErrorCode})"); }
            catch (Exception ex) { OnConnectionStateChanged?.Invoke($"Connection error: {ex.Message}"); }
        }

        public void Disconnect()
        {
            if (_simconnect != null)
            {
                if (_wasmClient != null) { _wasmClient.OnFenixLVarsReceived -= WasmClient_OnFenixLVarsReceived; _wasmClient = null; }
                _simconnect.Dispose(); _simconnect = null;
                _isNativelyConnected = false; OnConnectionStateChanged?.Invoke("Disconnected.");
            }
        }

        public void SendTimeWarpCommand(DateTime targetZulu)
        {
            if (_simconnect != null)
            {
                _simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.SetZuluHours, (uint)targetZulu.Hour, GROUP.Group0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                _simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, EVENTS.SetZuluMinutes, (uint)targetZulu.Minute, GROUP.Group0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
        }

        public void ReceiveMessage()
        {
            if (_simconnect != null) { try { _simconnect.ReceiveMessage(); } catch (COMException) { Disconnect(); } }
        }

        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            _isNativelyConnected = true;
            string appName = new string(data.szApplicationName).TrimEnd('\0');
            OnConnectionStateChanged?.Invoke("Linked to " + appName);
        }

        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data) => Disconnect();
        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data) => Debug.WriteLine("SimConnect Exception: " + data.dwException);

        private void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwRequestID == (uint)REQUESTS.PlaneDataReq)
            {
                if (!_isNativelyConnected) { _isNativelyConnected = true; OnConnectionStateChanged?.Invoke("Linked to MSFS (Telemetry)"); }
                var planeData = (PlaneDataStruct)data.dwData[0];
                OnAltitudeReceived?.Invoke(planeData.Altitude);
                OnGroundSpeedReceived?.Invoke(planeData.GroundSpeed);
                OnAirspeedReceived?.Invoke(planeData.IndicatedAirspeed);
                OnRadioHeightReceived?.Invoke(planeData.RadioHeight);
                
                var timeSpan = TimeSpan.FromSeconds(planeData.ZuluTime);
                try {
                    OnSimTimeReceived?.Invoke(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, DateTimeKind.Utc));
                } catch { }

                OnGearDownReceived?.Invoke(planeData.GearHandle > 0.5);
                OnFlapsReceived?.Invoke(planeData.FlapsHandleIndex);
                OnAutopilotReceived?.Invoke(planeData.AutopilotMaster > 0.5);
                OnAutothrustReceived?.Invoke(planeData.AutothrustMaster > 0.5);
                OnThrottleReceived?.Invoke(planeData.ThrottleLever);
                OnSpoilersReceived?.Invoke(planeData.SpoilersHandle);
                OnPitchReceived?.Invoke(-planeData.Pitch);
                OnBankReceived?.Invoke(-planeData.Bank);
                OnSimOnGroundReceived?.Invoke(planeData.SimOnGround != 0);
                OnVerticalSpeedReceived?.Invoke(planeData.VerticalSpeed);
                OnEngineCombustionReceived?.Invoke(planeData.Eng1Combustion > 0.5, planeData.Eng2Combustion > 0.5);
                OnHeadingReceived?.Invoke(planeData.Heading);
                OnWindReceived?.Invoke(planeData.WindDirection, planeData.WindVelocity);
                OnNavigationReceived?.Invoke(planeData.NavLocalizerError, planeData.GpsCrossTrackError, planeData.HasLocalizer > 0.5);

                if (!IsWasmOverriding)
                {
                    OnCabinSeatbeltsChanged?.Invoke(planeData.CabinSeatbelts > 0.5);
                    OnParkingBrakeReceived?.Invoke(planeData.ParkingBrakeIndicator > 0.5);
                    OnLightBeaconReceived?.Invoke(planeData.LightBeacon > 0.5);
                    OnLightStrobeReceived?.Invoke(planeData.LightStrobe > 0.5);
                    OnLightNavReceived?.Invoke(planeData.LightNav > 0.5);
                    OnLightTaxiReceived?.Invoke(planeData.LightTaxi > 0.5);
                    OnLightLandingReceived?.Invoke(planeData.LightLanding > 0.5);
                }
            }
            else if (data.dwRequestID == (uint)REQUESTS.GForceReq)
            {
                var gData = (GForceDataStruct)data.dwData[0];
                OnGForceReceived?.Invoke(gData.GForce);
            }
        }

        private void SimConnect_OnRecvClientData(SimConnect sender, SIMCONNECT_RECV_CLIENT_DATA data)
        {
            _wasmClient?.ProcessClientData(data);
        }

        private void WasmClient_OnFenixLVarsReceived(WasmLVarClient.FenixLVarPayload data)
        {
            if (IsWasmOverriding)
            {
                OnCabinSeatbeltsChanged?.Invoke(data.Seatbelts > 0.5);
                OnNoSmokingChanged?.Invoke(data.NoSmoking > 0.5);
                OnParkingBrakeReceived?.Invoke(data.ParkingBrake > 0.5);
                OnApuStateChanged?.Invoke(data.ApuMaster > 0.5, data.ApuStart > 0.5, data.ApuBleed > 0.5);
                OnPacksChanged?.Invoke(data.Pack1 > 0.5, data.Pack2 > 0.5);
                OnAntiIceChanged?.Invoke(data.WingAntiIce > 0.5, data.Eng1AntiIce > 0.5, data.Eng2AntiIce > 0.5);
                OnEngineBleedsChanged?.Invoke(data.Eng1Bleed > 0.5, data.Eng2Bleed > 0.5);
                OnEngineSwitchesChanged?.Invoke((int)data.EngMode, data.EngMaster1 > 0.5, data.EngMaster2 > 0.5);
                OnCabinTemperatureTargetsChanged?.Invoke(data.CabinTempCockpit, data.CabinTempFwd, data.CabinTempAft);
                OnNoseLightChanged?.Invoke((int)data.NoseLight);
                OnRunwayTurnoffChanged?.Invoke(data.TurnoffLight > 0.5);
                OnLightBeaconReceived?.Invoke(data.BeaconLight > 0.5);
                OnLightStrobeReceived?.Invoke(data.StrobeLight > 0.5);
                OnLightNavReceived?.Invoke(data.NavLight > 0.5);
                OnLightTaxiReceived?.Invoke(data.NoseLight == 1); // 1 = Taxi, 2 = TO
                OnLightLandingReceived?.Invoke(data.LandingLightL > 0.5 || data.LandingLightR > 0.5);
            }
        }
    }
}
