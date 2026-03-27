using System;
using System.Diagnostics;
using System.Threading;

namespace FlightSupervisor.UI.Services
{
    public enum CrisisType
    {
        None,
        MedicalEmergency,
        UnrulyPassenger,
        Depressurization
    }

    public enum CrisisFrequency
    {
        Off = 0,
        Realistic = 1,  // ~1% chance per minute
        Frequent = 2,   // ~5% chance per minute
        Chaos = 3       // ~20% chance per minute
    }

    public class CrisisManager
    {
        private readonly FlightPhaseManager _phaseManager;
        private readonly SimConnectService _simConnect;
        
        // State
        public CrisisType ActiveCrisis { get; private set; } = CrisisType.None;
        public CrisisFrequency Frequency { get; set; } = CrisisFrequency.Realistic;
        public double CrisisStartTimeSeconds { get; private set; } = 0;
        
        // Random Engine
        private readonly Random _random = new Random();
        private DateTime _lastTick = DateTime.Now;

        // Events
        public event Action<CrisisType>? OnCrisisTriggered;
        public event Action<CrisisType, bool, bool>? OnCrisisResolved; // True/False success, Manual/Timeout
        public event Action<string, string>? OnCabinMessage; // Level (color), Message

        private double _currentAltitude = 0;

        public CrisisManager(FlightPhaseManager phaseManager, SimConnectService simConnect)
        {
            _phaseManager = phaseManager;
            _simConnect = simConnect;
            
            _simConnect.OnAltitudeReceived += alt => _currentAltitude = alt;

            // Hook onto a 1-second tick or we can provide our own timer
            // For now, let's use a background timer that ticks every 10 seconds to limit probability checks
            var timer = new System.Threading.Timer(Tick, null, 10000, 10000);
        }

        private void Tick(object? state)
        {
            if (ActiveCrisis != CrisisType.None)
            {
                // We are actively inside a crisis, monitor constraints
                MonitorActiveCrisis();
                return;
            }

            // Probability Check to spawn a NEW crisis
            if (Frequency == CrisisFrequency.Off) return;

            // Only spawn crises while airborne (Climb, Cruise, Descent)
            if (_phaseManager.CurrentPhase != FlightPhase.Climb &&
                _phaseManager.CurrentPhase != FlightPhase.Cruise &&
                _phaseManager.CurrentPhase != FlightPhase.Descent)
            {
                return;
            }

            // Base probability per 10-second tick
            double probThreshold = 0.0;
            switch (Frequency)
            {
                case CrisisFrequency.Realistic: probThreshold = 0.001; break; // 0.1% every 10s (~0.6% per min = rarely happens in short flights)
                case CrisisFrequency.Frequent: probThreshold = 0.02; break;   // 2% every 10s (~12% per min)
                case CrisisFrequency.Chaos: probThreshold = 0.15; break;      // 15% every 10s (Almost guaranteed)
            }

            // TODO: Context modifiers (Weather, Turbulence, Delays) can multiply probThreshold here.

            // Explicit "Mauvaise Manip" Checks (Overrides standard probability)
            if (_currentAltitude > 10000 && !_phaseManager.FenixPack1 && !_phaseManager.FenixPack2)
            {
                TriggerSpecificCrisis(CrisisType.Depressurization);
                return;
            }
            
            // Extreme Cabin Temperature Settings 
            // The Fenix A320 rotaries might be scaled 0.0-1.0 or 0.0-100.0. The math below covers both extremes.
            bool isExtreme(float val) => (val >= 0f && val < 0.05f) || (val > 0.95f && val <= 1.0f) || (val > 1.0f && val < 5.0f) || (val > 95.0f);

            bool fwdExtreme = isExtreme(_phaseManager.FenixCabinTempFwd);
            bool aftExtreme = isExtreme(_phaseManager.FenixCabinTempAft);

            if (fwdExtreme || aftExtreme)
            {
                // Unbearable temperatures will trigger passenger crises fairly quickly
                if (_random.NextDouble() < 0.20) // 20% chance every 10s to spawn if kept extreme
                {
                    string zone = fwdExtreme && aftExtreme ? "Whole Cabin" : (fwdExtreme ? "FWD Cabin" : "AFT Cabin");
                    OnCabinMessage?.Invoke("orange", $"[PNC] The temperature in the {zone} is unbearable! Passengers are getting aggressive.");
                    
                    TriggerSpecificCrisis(_random.Next(2) == 0 ? CrisisType.MedicalEmergency : CrisisType.UnrulyPassenger);
                    return;
                }
            }

            if (_random.NextDouble() < probThreshold)
            {
                TriggerRandomCrisis();
            }
        }

        private void TriggerSpecificCrisis(CrisisType type)
        {
            if (ActiveCrisis != CrisisType.None) return;
            
            ActiveCrisis = type;
            CrisisStartTimeSeconds = GetUnixTime();

            Debug.WriteLine($"[CRISIS GENERATOR] Spawning Event explicitly from Pilot Action: {type}");
            OnCrisisTriggered?.Invoke(type);
        }

        private void TriggerRandomCrisis()
        {
            // Pick a random crisis
            var list = new System.Collections.Generic.List<CrisisType> 
            { 
                CrisisType.MedicalEmergency, 
                CrisisType.UnrulyPassenger 
            };
            
            if (_currentAltitude > 20000)
            {
                list.Add(CrisisType.Depressurization);
            }
            
            CrisisType type = list[_random.Next(list.Count)];
            
            ActiveCrisis = type;
            CrisisStartTimeSeconds = GetUnixTime();

            Debug.WriteLine($"[CRISIS GENERATOR] Spawning Event: {type}");
            OnCrisisTriggered?.Invoke(type);
        }

        private void MonitorActiveCrisis()
        {
            double elapsed = GetUnixTime() - CrisisStartTimeSeconds;

            if (ActiveCrisis == CrisisType.Depressurization)
            {
                // Descend below 10,000 ft to resolve the emergency
                if (_currentAltitude < 10000)
                {
                    ResolveCrisis(true, true);
                    return;
                }
                
                // If it takes more than 5 minutes (300s) to descend, fail
                if (elapsed > 300)
                {
                    ResolveCrisis(false, false);
                }
                return;
            }

            // If a crisis goes unresolved for too long, it impacts passenger comfort significantly.
            // That specific logic might belong in SuperScoreManager or here. We'll handle resolution triggers soon.
            if (elapsed > 600) // 10 minutes max
            {
                // Force fail (auto-timeout)
                ResolveCrisis(false, false);
            }
        }

        public void ResolveCrisis(bool success, bool isManual = true)
        {
            if (ActiveCrisis == CrisisType.None) return;

            Debug.WriteLine($"[CRISIS GENERATOR] Resolving Event: {ActiveCrisis} | Success: {success} | Manual: {isManual}");
            var resolvedType = ActiveCrisis;
            ActiveCrisis = CrisisType.None;
            CrisisStartTimeSeconds = 0;

            OnCrisisResolved?.Invoke(resolvedType, success, isManual);
        }

        private double GetUnixTime()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
