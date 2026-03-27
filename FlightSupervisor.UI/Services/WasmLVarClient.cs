using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.FlightSimulator.SimConnect;

namespace FlightSupervisor.UI.Services
{
    public class WasmLVarClient
    {
        private SimConnect _simConnect;
        private bool _isRegistered = false;

        public event Action<FenixLVarPayload>? OnFenixLVarsReceived;

        private enum DATA_DEFINITIONS
        {
            CommandString = 1000,
            ResponseString = 1001,
            FenixLVarsArray = 1002
        }

        private enum REQUESTS
        {
            ResponseReq = 2000,
            LVarsReq = 2001
        }

        private enum CLIENT_DATA_ID
        {
            MF_Command = 3000,
            MF_Response = 3001,
            FS_Command = 3002,
            FS_LVars = 3003
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct StringPayload
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string Data;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FenixLVarPayload
        {
            public float Seatbelts;       // Offset 0
            public float NoSmoking;       // Offset 4
            public float ApuMaster;       // Offset 8
            public float ApuStart;        // Offset 12
            public float ApuBleed;        // Offset 16
            public float NoseLight;       // Offset 20
            public float TurnoffLight;    // Offset 24
            public float StrobeLight;     // Offset 28
            public float BeaconLight;     // Offset 32
            public float NavLight;        // Offset 36
            public float LandingLightL;   // Offset 40
            public float LandingLightR;   // Offset 44
            public float WingAntiIce;     // Offset 48
            public float Eng1AntiIce;     // Offset 52
            public float Eng2AntiIce;     // Offset 56
            public float EngMode;         // Offset 60
            public float EngMaster1;      // Offset 64
            public float EngMaster2;      // Offset 68
            public float ParkingBrake;    // Offset 72
            public float CabinTempCockpit;// Offset 76
            public float CabinTempFwd;    // Offset 80
            public float CabinTempAft;    // Offset 84
            public float Eng1Bleed;       // Offset 88
            public float Eng2Bleed;       // Offset 92
            public float Pack1;           // Offset 96
            public float Pack2;           // Offset 100
        }

        public WasmLVarClient(SimConnect simConnect)
        {
            _simConnect = simConnect;
        }

        public void Initialize()
        {
            try
            {
                _isRegistered = false;

                // Map default MobiFlight Channels
                _simConnect.MapClientDataNameToID("MobiFlight.Command", CLIENT_DATA_ID.MF_Command);
                _simConnect.MapClientDataNameToID("MobiFlight.Response", CLIENT_DATA_ID.MF_Response);

                // Define string structs for command/response
                _simConnect.AddToClientDataDefinition(DATA_DEFINITIONS.CommandString, 0, 256, 0, 0);
                _simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, StringPayload>(DATA_DEFINITIONS.CommandString);
                
                _simConnect.AddToClientDataDefinition(DATA_DEFINITIONS.ResponseString, 0, 256, 0, 0);
                _simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, StringPayload>(DATA_DEFINITIONS.ResponseString);

                // Subscribe to Response channel
                _simConnect.RequestClientData(CLIENT_DATA_ID.MF_Response,
                    REQUESTS.ResponseReq,
                    DATA_DEFINITIONS.ResponseString,
                    SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                    SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                    0, 0, 0);

                // Send dummy command to wake up (as recommended by MobiFlight docs)
                SendCommand(CLIENT_DATA_ID.MF_Command, "MF.Ping");

                // Send registration command
                SendCommand(CLIENT_DATA_ID.MF_Command, "MF.Clients.Add.FlightSupervisor");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WASM Init Error: " + ex.Message);
            }
        }

        private void SendCommand(CLIENT_DATA_ID channel, string commandStr)
        {
            var payload = new StringPayload { Data = commandStr };
            _simConnect.SetClientData(channel, DATA_DEFINITIONS.CommandString,
                SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, payload);
        }

        public void ProcessClientData(SIMCONNECT_RECV_CLIENT_DATA data)
        {
            if (data.dwRequestID == (uint)REQUESTS.ResponseReq)
            {
                var payload = (StringPayload)data.dwData[0];
                string response = payload.Data.TrimEnd('\0');

                if (response.Contains("MF.Clients.Add.FlightSupervisor.Finished") && !_isRegistered)
                {
                    _isRegistered = true;
                    Debug.WriteLine("MobiFlight WASM Registration Completed!");
                    SetupCustomChannels();
                }
            }
            else if (data.dwRequestID == (uint)REQUESTS.LVarsReq)
            {
                var fenixPayload = (FenixLVarPayload)data.dwData[0];
                OnFenixLVarsReceived?.Invoke(fenixPayload);
            }
        }

        private void SetupCustomChannels()
        {
            try
            {
                _simConnect.MapClientDataNameToID("FlightSupervisor.Command", CLIENT_DATA_ID.FS_Command);
                _simConnect.MapClientDataNameToID("FlightSupervisor.LVars", CLIENT_DATA_ID.FS_LVars);

                _simConnect.AddToClientDataDefinition(DATA_DEFINITIONS.FenixLVarsArray, 0, (uint)Marshal.SizeOf<FenixLVarPayload>(), 0, 0);
                _simConnect.RegisterStruct<SIMCONNECT_RECV_CLIENT_DATA, FenixLVarPayload>(DATA_DEFINITIONS.FenixLVarsArray);

                _simConnect.RequestClientData(CLIENT_DATA_ID.FS_LVars,
                    REQUESTS.LVarsReq,
                    DATA_DEFINITIONS.FenixLVarsArray,
                    SIMCONNECT_CLIENT_DATA_PERIOD.ON_SET,
                    SIMCONNECT_CLIENT_DATA_REQUEST_FLAG.CHANGED,
                    0, 0, 0);

                // Subscribe to Fenix Variables
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Clear");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_SIGNS,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_SIGNS_SMOKING,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_ELEC_APU_MASTER,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_ELEC_APU_START,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_PNEUMATIC_APU_BLEED,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_EXT_LT_NOSE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_EXT_LT_RWY_TURNOFF,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_EXT_LT_STROBE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_EXT_LT_BEACON,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_EXT_LT_NAV_LOGO,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_EXT_LT_LANDING_L,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_EXT_LT_LANDING_R,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_PNEUMATIC_WING_ANTI_ICE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_PNEUMATIC_ENG1_ANTI_ICE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_PNEUMATIC_ENG2_ANTI_ICE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_ENG_MODE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_ENG_MASTER_1,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_ENG_MASTER_2,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_MIP_PARKING_BRAKE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:A_OH_PNEUMATIC_CKPT_TEMP,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:A_OH_PNEUMATIC_FWD_TEMP,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:A_OH_PNEUMATIC_AFT_TEMP,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_PNEUMATIC_ENG1_BLEED,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_PNEUMATIC_ENG2_BLEED,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_PNEUMATIC_PACK_1,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.SimVars.Add.(L:S_OH_PNEUMATIC_PACK_2,Number)");

                // Map variables to our client area in the exact struct order
                SendCommand(CLIENT_DATA_ID.FS_Command, $"MF.SimVars.SetTarget.FlightSupervisor"); // Tell WASM which client area we are building
                
                // Now Add definitions to map to our Struct Offset sequentially!
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_SIGNS,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_SIGNS_SMOKING,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_ELEC_APU_MASTER,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_ELEC_APU_START,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_PNEUMATIC_APU_BLEED,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_EXT_LT_NOSE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_EXT_LT_RWY_TURNOFF,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_EXT_LT_STROBE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_EXT_LT_BEACON,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_EXT_LT_NAV_LOGO,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_EXT_LT_LANDING_L,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_EXT_LT_LANDING_R,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_PNEUMATIC_WING_ANTI_ICE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_PNEUMATIC_ENG1_ANTI_ICE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_PNEUMATIC_ENG2_ANTI_ICE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_ENG_MODE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_ENG_MASTER_1,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_ENG_MASTER_2,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_MIP_PARKING_BRAKE,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:A_OH_PNEUMATIC_CKPT_TEMP,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:A_OH_PNEUMATIC_FWD_TEMP,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:A_OH_PNEUMATIC_AFT_TEMP,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_PNEUMATIC_ENG1_BLEED,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_PNEUMATIC_ENG2_BLEED,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_PNEUMATIC_PACK_1,Number)");
                SendCommand(CLIENT_DATA_ID.FS_Command, "MF.Clients.Add.LVar.(L:S_OH_PNEUMATIC_PACK_2,Number)");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WASM Custom Channels Setup Error: " + ex.Message);
            }
        }
    }
}
