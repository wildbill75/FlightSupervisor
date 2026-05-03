using System;
using FlightSupervisor.UI.Models;

namespace FlightSupervisor.UI.Services
{
    public class WearAndTearManager
    {
        private readonly SimConnectService _simConnectService;
        private readonly AirframeManager _airframeManager;

        private bool _wasAirborne = false;
        private bool _simOnGround = true;
        private DateTime? _touchdownTime = null;
        private DateTime? _airborneStartTime = null;
        private bool _enginesRunning = false;
        private double _verticalSpeed = 0;
        private System.Collections.Generic.Queue<double> _vsHistory = new System.Collections.Generic.Queue<double>();
        private double _flapsHandleIndex = 0;
        // Airmanship Events
        public event Action<double>? OnHardLandingDetected;
        public event Action<double>? OnTailStrikeDetected;
        public event Action<double>? OnEngineCooldownBreached;
        public event Action? OnFlapsOverspeedDetected;
        public event Action? OnHotBrakesTakeoff;

        // Virtual Brake Temperature (Degrees Celsius)
        public double VirtualBrakeTemp { get; private set; } = 15.0; // Ambient default
        private DateTime _lastBrakeTempUpdate = DateTime.Now;
        private double _currentAirspeed = 0.0;
        private bool _isFlapsOverspeeding = false;

        public WearAndTearManager(SimConnectService simConnectService, AirframeManager airframeManager)
        {
            _simConnectService = simConnectService;
            _airframeManager = airframeManager;

            _simConnectService.OnSimOnGroundReceived += HandleSimOnGround;
            _simConnectService.OnPitchReceived += HandlePitch;
            _simConnectService.OnAirspeedReceived += HandleAirspeed;
            _simConnectService.OnEngineCombustionReceived += HandleEngineCombustion;
            _simConnectService.OnAmbientTemperatureReceived += HandleAmbientTemperature;
            _simConnectService.OnVerticalSpeedReceived += HandleVerticalSpeed;
            _simConnectService.OnFlapsReceived += f => _flapsHandleIndex = f;

            // Timer for continuous virtual calculations
            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += UpdateVirtualSystems;
            timer.Start();
        }

        private void UpdateVirtualSystems(object sender, System.Timers.ElapsedEventArgs e)
        {
            double elapsedSeconds = (DateTime.Now - _lastBrakeTempUpdate).TotalSeconds;
            _lastBrakeTempUpdate = DateTime.Now;

            // Update Virtual Brake Temp
            if (_simOnGround)
            {
                if (_currentAirspeed > 60 && !_wasAirborne)
                {
                    // High-speed rollout / hard braking after landing or RTO
                    VirtualBrakeTemp += 8.0 * elapsedSeconds; // Fast heat up
                }
                else if (_currentAirspeed > 5)
                {
                    // Normal taxiing
                    VirtualBrakeTemp += 0.5 * elapsedSeconds; // Slow heat up
                }
                else
                {
                    // Stopped or parked
                    VirtualBrakeTemp -= 0.5 * elapsedSeconds; // Slow cooling
                }
            }
            else
            {
                // Airborne (gear up, cold air)
                VirtualBrakeTemp -= 5.0 * elapsedSeconds; // Fast cooling
            }

            // Clamp temperature to ambient minimum (approx 15C)
            if (VirtualBrakeTemp < 15.0) VirtualBrakeTemp = 15.0;

            // Hot Brakes Takeoff Detection
            if (!_simOnGround && _wasAirborne == false && VirtualBrakeTemp > 300.0)
            {
                // Just took off with hot brakes
                OnHotBrakesTakeoff?.Invoke();
                // Prevent spamming
                VirtualBrakeTemp = 290.0; 
            }
        }

        private void HandleVerticalSpeed(double vs)
        {
            _verticalSpeed = vs;
            if (!_simOnGround)
            {
                _vsHistory.Enqueue(vs);
                if (_vsHistory.Count > 5) _vsHistory.Dequeue();
            }
        }

        private void HandleSimOnGround(bool onGround)
        {
            _simOnGround = onGround;
            if (onGround && _wasAirborne)
            {
                _wasAirborne = false;
                _touchdownTime = DateTime.Now;

                var af = _airframeManager.CurrentAirframe;
                if (af == null) return;

                // Analyze Hard Landing
                bool wasAirborneLongEnough = _airborneStartTime.HasValue && (DateTime.Now - _airborneStartTime.Value).TotalSeconds > 30;

                if (wasAirborneLongEnough)
                {
                    double touchdownVs = _vsHistory.Count > 0 ? _vsHistory.Peek() : _verticalSpeed;
                    
                    if (touchdownVs < -400)
                    {
                        double severity = Math.Abs(touchdownVs + 400) / 100.0;
                        af.GearAndBrakeWear += severity * 2.5;
                        af.Events.Add(new AirframeLogEvent
                        {
                            Timestamp = DateTime.Now,
                            Type = "hard_landing",
                            Severity = "error",
                            Description = $"Hard landing detected: {touchdownVs:F0} fpm"
                        });
                        _airframeManager.SaveAirframe(af);

                        OnHardLandingDetected?.Invoke(touchdownVs);
                    }
                }
            }
            else if (!onGround)
            {
                if (!_wasAirborne)
                {
                    _airborneStartTime = DateTime.Now;
                }
                _wasAirborne = true;
                _touchdownTime = null;
            }
        }

        private DateTime? _lastTailStrikeTime = null;

        private void HandlePitch(double pitch)
        {
            var af = _airframeManager.CurrentAirframe;
            if (af == null || !_simOnGround) return;

            // Detect tail strike (pitch too high on ground during takeoff/landing)
            if (pitch > 13.5)
            {
                if (_lastTailStrikeTime.HasValue && (DateTime.Now - _lastTailStrikeTime.Value).TotalSeconds < 10)
                {
                    return; // Debounce for 10 seconds
                }
                
                _lastTailStrikeTime = DateTime.Now;

                af.StructureWear += 10.0;
                af.Events.Add(new AirframeLogEvent
                {
                    Timestamp = DateTime.Now,
                    Type = "tail_strike",
                    Severity = "error",
                    Description = $"Tail strike detected! Pitch reached {pitch:F1} degrees on ground."
                });
                _airframeManager.SaveAirframe(af);

                OnTailStrikeDetected?.Invoke(pitch);
            }
        }

        private void HandleAirspeed(double airspeed)
        {
            _currentAirspeed = airspeed;
            var af = _airframeManager.CurrentAirframe;
            if (af == null) return;

            // Flaps Overspeed calculation
            if (_flapsHandleIndex > 0 && airspeed > 230)
            {
                af.FlapsWear += 0.01; // Continuous wear while overspeeding
                if (!_isFlapsOverspeeding)
                {
                    _isFlapsOverspeeding = true;
                    OnFlapsOverspeedDetected?.Invoke();
                }
            }
            else
            {
                _isFlapsOverspeeding = false;
            }
        }

        private void HandleEngineCombustion(bool eng1, bool eng2)
        {
            bool anyEngine = eng1 || eng2;
            
            if (_enginesRunning && !anyEngine)
            {
                _enginesRunning = false;

                var af = _airframeManager.CurrentAirframe;
                if (af != null && _touchdownTime.HasValue)
                {
                    var cooldownDuration = (DateTime.Now - _touchdownTime.Value).TotalMinutes;
                    if (cooldownDuration < 3.0)
                    {
                        af.EngineWear += (3.0 - cooldownDuration) * 2.0;
                        af.Events.Add(new AirframeLogEvent
                        {
                            Timestamp = DateTime.Now,
                            Type = "engine_cooldown_breach",
                            Severity = "warn",
                            Description = $"Engines shut down too early ({cooldownDuration:F1} mins post-landing). Minimum 3 mins required."
                        });
                        _airframeManager.SaveAirframe(af);

                        OnEngineCooldownBreached?.Invoke(cooldownDuration);
                    }
                }
            }
            else if (anyEngine)
            {
                _enginesRunning = true;
            }
        }

        private void HandleAmbientTemperature(double temp)
        {
            var af = _airframeManager.CurrentAirframe;
            if (af == null || _simOnGround) return;

            // Simple icing heuristic
            if (temp < 10.0)
            {
                // e.g. af.EngineWear += 0.001;
            }
        }
    }
}
