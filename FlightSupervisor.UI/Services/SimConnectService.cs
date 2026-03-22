using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.FlightSimulator.SimConnect;

namespace FlightSupervisor.UI.Services
{
    public class SimConnectService
    {
        private SimConnect? _simconnect = null;
        private const int WM_USER_SIMCONNECT = 0x0402;
        private IntPtr _windowHandle;

        public event Action<string>? OnConnectionStateChanged;
        public event Action<double>? OnAltitudeReceived;
        public event Action<double>? OnGroundSpeedReceived;
        public event Action<double>? OnAirspeedReceived;
        public event Action<double>? OnRadioHeightReceived;
        public event Action<DateTime>? OnSimTimeReceived;
        public event Action<bool>? OnParkingBrakeReceived;
        public event Action<bool>? OnGearDownReceived;

        enum DEFINITIONS { PlaneData }
        enum REQUESTS { PlaneDataReq }

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
        }

        public SimConnectService() { }

        public void Connect(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            try
            {
                OnConnectionStateChanged?.Invoke("Connecting natively...");
                _simconnect = new SimConnect("Flight Supervisor", windowHandle, WM_USER_SIMCONNECT, null, 0);

                _simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnRecvOpen);
                _simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnRecvQuit);
                _simconnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnRecvException);
                _simconnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(SimConnect_OnRecvSimobjectData);

                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "INDICATED ALTITUDE", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "GROUND VELOCITY", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "AIRSPEED INDICATED", "Knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "ZULU TIME", "Seconds", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "BRAKE PARKING INDICATOR", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "RADIO HEIGHT", "Feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                _simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "GEAR HANDLE POSITION", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                _simconnect.RegisterDataDefineStruct<PlaneDataStruct>(DEFINITIONS.PlaneData);
                
                _simconnect.RequestDataOnSimObject(REQUESTS.PlaneDataReq, DEFINITIONS.PlaneData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
            }
            catch (COMException ex)
            {
                OnConnectionStateChanged?.Invoke($"MSFS not found ({ex.ErrorCode})");
            }
            catch (Exception ex)
            {
                OnConnectionStateChanged?.Invoke($"SimConnect Err: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            if (_simconnect != null)
            {
                _simconnect.Dispose();
                _simconnect = null;
                OnConnectionStateChanged?.Invoke("Disconnected.");
            }
        }

        public void ReceiveMessage()
        {
            if (_simconnect != null)
            {
                try { _simconnect.ReceiveMessage(); }
                catch (COMException) { Disconnect(); }
            }
        }

        private void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            string appName = new string(data.szApplicationName).TrimEnd('\0');
            OnConnectionStateChanged?.Invoke("Linked to " + appName);
        }

        private void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Disconnect();
        }

        private void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            Debug.WriteLine("SimConnect Exception: " + data.dwException);
        }

        private void SimConnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwRequestID == (uint)REQUESTS.PlaneDataReq)
            {
                var planeData = (PlaneDataStruct)data.dwData[0];
                OnAltitudeReceived?.Invoke(planeData.Altitude);
                OnGroundSpeedReceived?.Invoke(planeData.GroundSpeed);
                OnAirspeedReceived?.Invoke(planeData.IndicatedAirspeed);
                OnRadioHeightReceived?.Invoke(planeData.RadioHeight);
                OnParkingBrakeReceived?.Invoke(planeData.ParkingBrakeIndicator > 0.5);
                OnGearDownReceived?.Invoke(planeData.GearHandle > 0.5);

                // Build Sim Time
                try
                {
                    var timeSpan = TimeSpan.FromSeconds(planeData.ZuluTime);
                    var simTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, DateTimeKind.Utc);
                    OnSimTimeReceived?.Invoke(simTime);
                }
                catch { }
            }
        }
    }
}
