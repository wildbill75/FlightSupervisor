using System;
using FlightSupervisor.UI.Models;

namespace FlightSupervisor.UI.Services
{
    public class WearAndTearManager
    {
        private readonly SimConnectService _simConnectService;
        private readonly AirframeManager _airframeManager;

        private bool _wasAirborne = false;
        private DateTime? _touchdownTime = null;
        private DateTime? _airborneStartTime = null;
        private bool _enginesRunning = false;
        private double _verticalSpeed = 0;
        private System.Collections.Generic.Queue<double> _vsHistory = new System.Collections.Generic.Queue<double>();
        private double _flapsHandleIndex = 0;
        private bool _simOnGround = true;

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
                // We ensure the aircraft has been airborne for at least 30 seconds to prevent takeoff bounces from registering as landings.
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
            }
        }

        private void HandleAirspeed(double airspeed)
        {
            var af = _airframeManager.CurrentAirframe;
            if (af == null) return;

            // Flaps Overspeed calculation
            // Assuming FlapsIndex > 0 means deployed. Thresholds depend on aircraft.
            // For generic A320, say Flaps 1 limit is ~230 kts. If flaps deployed and > 230 kts -> wear.
            if (_flapsHandleIndex > 0 && airspeed > 230)
            {
                af.FlapsWear += 0.01; // Continuous wear while overspeeding
            }

            // Virtual Hot Brakes
            if (_simOnGround && airspeed > 80 && _wasAirborne == false)
            {
                // If rolling fast on runway and braking (implied by decelerating rapidly)
                // A more complex heuristic would track deceleration.
                af.GearAndBrakeWear += 0.005;
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

            // Simple icing heuristic: flying in < 10 degrees generates very slight engine/structure wear if anti-ice isn't modeled well,
            // but ideally we'd check if specific LVars for AntiIce are active.
            // We'll leave the framework here for when WasmLVarClient exposes AntiIce state.
            if (temp < 10.0)
            {
                // e.g. af.EngineWear += 0.001;
            }
        }
    }
}
