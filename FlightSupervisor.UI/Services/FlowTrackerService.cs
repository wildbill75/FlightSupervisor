using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FlightSupervisor.UI.Services
{
    public class FlowTrackerService
    {
        private readonly SimConnectService _simConnect;
        private readonly FlightPhaseManager _phaseManager;
        private readonly SuperScoreManager _scoreManager;

        // --- Continuous Trackers ---
        private bool _isTaxiSpeedViolationActive = false;
        private DateTime _taxiSpeedViolationStartTime;
        public int TotalTaxiSpeedViolations { get; private set; }
        public double AccumulatedTaxiSpeedViolationSeconds { get; private set; }

        private bool _isFlapsOverspeedViolationActive = false;
        public int TotalFlapsOverspeedViolations { get; private set; }
        public double AccumulatedFlapsOverspeedViolationSeconds { get; private set; }

        private bool _isTurbulenceViolationActive = false;
        public int TotalTurbulenceViolations { get; private set; }
        public double AccumulatedTurbulenceViolationSeconds { get; private set; }

        // --- Critical Event Flags ---
        public bool HasTailStrikeInPhase { get; private set; }
        public bool HasHardLandingInPhase { get; private set; }
        public bool HasBrakeTempCriticalInPhase { get; private set; }
        
        // --- Dependencies State ---
        private double _currentIas = 0;
        private double _currentGs = 0;
        private double _currentPitch = 0;
        private double _currentGForce = 1.0;
        private double _currentVs = 0;
        private double _currentBank = 0;
        private double _currentRadioHeight = 0;
        private DateTime _lastComfortPenaltyTime = DateTime.MinValue;
        private DateTime _lastAirmanshipPenaltyTime = DateTime.MinValue;
        private DateTime? _prolongedBankStart = null;
        private DateTime? _prolongedPitchStart = null;
        private bool _isOnGround = true;
        private double _throttleLever1 = 0;
        private double _throttleLever2 = 0;
        
        // --- Flow State Cache ---
        public bool IsParkingBrakeOn { get; private set; }
        public bool AreWipersOff { get; private set; } = true;
        public double FlapsIndex { get; private set; }
        public bool AreSpoilersArmed { get; private set; }
        private double _rawSpoilerPos = 0;
        public bool IsSpoilerDeployed => _rawSpoilerPos >= 5.0;
        public bool AreSpoilersRetracted => !IsSpoilerDeployed && !AreSpoilersArmed;
        
        public string SpoilerStateText 
        {
            get 
            {
                if (IsSpoilerDeployed) return "DEPLOYED";
                if (AreSpoilersArmed) return "ARMED";
                return "RETRACTED";
            }
        }
        public int EngineMode { get; private set; } = 1; // 1 = NORM
        public bool IsEngineMaster1On { get; private set; }
        public bool IsEngineMaster2On { get; private set; }
        public bool IsGearDown { get; private set; } = true;
        public int StrobeLightState { get; private set; } = 0;
        public bool AreLandingLightsOn { get; private set; } = false;
        public bool IsBeaconLightOn { get; private set; } = false;
        public int TaxiLightState { get; private set; } = 0;
        public bool IsRnwTurnoffOn { get; private set; }
        public bool AreSeatbeltsOn { get; private set; }
        public bool IsThrustIdle => Math.Abs(_throttleLever1) < 5.0 && Math.Abs(_throttleLever2) < 5.0;
        public bool IsRefueling { get; private set; }

        // --- Live Telemetry Exposed Properties ---
        public double Airspeed => _currentIas;
        public double GroundSpeed => _currentGs;
        public double Pitch => _currentPitch;
        public double Bank => _currentBank;
        public double VerticalSpeed => _currentVs;
        public double RadioHeight => _currentRadioHeight;
        public double GForce => _currentGForce;
        public double Throttle1 => _throttleLever1;
        public double Throttle2 => _throttleLever2;
        private bool _ticket43Violated = false;

        public event Action<string> OnCriticalEventDetected;
        public event Action OnUnstableApproachDetected; // Trigger FO GoAround

        public FlowTrackerService(SimConnectService simConnect, FlightPhaseManager phaseManager, SuperScoreManager scoreManager)
        {
            _simConnect = simConnect;
            _phaseManager = phaseManager;
            _scoreManager = scoreManager;

            HookTelemetry();
            _phaseManager.OnPhaseEnding += PhaseManager_OnPhaseEnding;
        }

        private void HookTelemetry()
        {
            _simConnect.OnAirspeedReceived += ias => { 
                _currentIas = ias; 
                EvaluateContinuousRules(); 
            };
            _simConnect.OnGroundSpeedReceived += gs => { 
                _currentGs = gs; 
                EvaluateContinuousRules(); 
            };
            _simConnect.OnPitchReceived += pitch => { 
                _currentPitch = pitch; 
                EvaluateCriticalEvents(); 
            };
            _simConnect.OnGForceReceived += gforce => { 
                _currentGForce = gforce; 
                EvaluateCriticalEvents(); 
            };
            _simConnect.OnVerticalSpeedReceived += vs => { 
                _currentVs = vs; 
            };
            _simConnect.OnBankReceived += bank => {
                _currentBank = bank;
            };
            _simConnect.OnRadioHeightReceived += rh => {
                _currentRadioHeight = rh;
            };
            _simConnect.OnSimOnGroundReceived += gnd => { 
                _isOnGround = gnd; 
            };
            // Throttles for Go-Around checking
            _simConnect.OnThrottleReceived += throt => {
                _throttleLever1 = throt; // SimConnect gives 0-100 or -100 to 100
                _throttleLever2 = throt;
                CheckGoAroundCondition();
            };

            // Hook Flow States
            _simConnect.OnParkingBrakeReceived += b => IsParkingBrakeOn = b;
            _simConnect.OnWipersStateReceived += (l, r) => AreWipersOff = (!l && !r);
            _simConnect.OnFlapsReceived += f => FlapsIndex = f;
            _simConnect.OnSpoilersArmedReceived += s => AreSpoilersArmed = s;
            _simConnect.OnSpoilersReceived += s => _rawSpoilerPos = s;
            _simConnect.OnEngineSwitchesChanged += (mode, m1, m2) => { EngineMode = mode; IsEngineMaster1On = m1; IsEngineMaster2On = m2; };
            _simConnect.OnGearDownReceived += g => IsGearDown = g;
            _simConnect.OnFenixStrobeStateChanged += s => StrobeLightState = s;
            _simConnect.OnLightBeaconReceived += l => IsBeaconLightOn = l;
            _simConnect.OnLightLandingReceived += l => AreLandingLightsOn = l;
            _simConnect.OnNoseLightChanged += n => TaxiLightState = n;
            _simConnect.OnRunwayTurnoffChanged += r => IsRnwTurnoffOn = r;
            _simConnect.OnCabinSeatbeltsChanged += s => { 
                AreSeatbeltsOn = s;
                CheckRefuelingSeatbeltViolation();
            };
            _simConnect.OnGsxRefuelingStateReceived += r => { 
                IsRefueling = (r == 5); // Assuming 5 is active refueling for GSX
                CheckRefuelingSeatbeltViolation();
            };
        }

        private void PhaseManager_OnPhaseEnding(FlightPhase endingPhase)
        {
            // The ScoreFlowEvaluator will grab the data, so we don't reset immediately.
            // Reset must be called explicitly by the evaluator AFTER it takes a snapshot.
        }

        public void ResetPhaseTrackers()
        {
            TotalTaxiSpeedViolations = 0;
            AccumulatedTaxiSpeedViolationSeconds = 0;
            _isTaxiSpeedViolationActive = false;

            TotalFlapsOverspeedViolations = 0;
            AccumulatedFlapsOverspeedViolationSeconds = 0;
            _isFlapsOverspeedViolationActive = false;

            TotalTurbulenceViolations = 0;
            AccumulatedTurbulenceViolationSeconds = 0;
            _isTurbulenceViolationActive = false;

            HasTailStrikeInPhase = false;
            HasHardLandingInPhase = false;
            HasBrakeTempCriticalInPhase = false;
        }

        private void EvaluateContinuousRules()
        {
            var phase = _phaseManager.CurrentPhase;

            // 1. Taxi Speed Check (> 30 kts on ground)
            if (_isOnGround && (phase == FlightPhase.TaxiOut || phase == FlightPhase.TaxiIn))
            {
                if (_currentGs > 30.0)
                {
                    if (!_isTaxiSpeedViolationActive)
                    {
                        _isTaxiSpeedViolationActive = true;
                        _taxiSpeedViolationStartTime = DateTime.UtcNow;
                        TotalTaxiSpeedViolations++;
                    }
                }
                else
                {
                    if (_isTaxiSpeedViolationActive)
                    {
                        _isTaxiSpeedViolationActive = false;
                        AccumulatedTaxiSpeedViolationSeconds += (DateTime.UtcNow - _taxiSpeedViolationStartTime).TotalSeconds;
                    }
                }
            }

            // 2. Unstable Approach Check
            if (phase == FlightPhase.Approach && !_isOnGround)
            {
                // Basic Example logic: High VS close to ground + excessive speed
                if (_currentIas > 180 && _currentVs < -1500)
                {
                    OnUnstableApproachDetected?.Invoke();
                }
            }

            // 3. Comfort vs Airmanship Logic
            if (!_isOnGround)
            {
                // Structural / Operational Limits (Airmanship)
                if (Math.Abs(_currentBank) > 45.0)
                {
                    if ((DateTime.UtcNow - _lastAirmanshipPenaltyTime).TotalSeconds > 10)
                    {
                        _scoreManager.AddScore(-200, $"Safety Violation: Structural Bank Limit Exceeded ({_currentBank:F0}°)", ScoreCategory.Airmanship);
                        _lastAirmanshipPenaltyTime = DateTime.UtcNow;
                    }
                }
                
                if (_currentPitch > 25.0 || _currentPitch < -15.0)
                {
                    if ((DateTime.UtcNow - _lastAirmanshipPenaltyTime).TotalSeconds > 10)
                    {
                        _scoreManager.AddScore(-200, $"Safety Violation: Structural Pitch Limit Exceeded ({_currentPitch:F0}°)", ScoreCategory.Airmanship);
                        _lastAirmanshipPenaltyTime = DateTime.UtcNow;
                    }
                }

                if (_currentIas > 260 && _currentRadioHeight < 10000 && phase != FlightPhase.Cruise)
                {
                    if ((DateTime.UtcNow - _lastAirmanshipPenaltyTime).TotalSeconds > 10)
                    {
                        _scoreManager.AddScore(-150, "Overspeed: Aircraft exceeded 250kts below 10,000ft", ScoreCategory.Airmanship);
                        _lastAirmanshipPenaltyTime = DateTime.UtcNow;
                    }
                }

                // Passenger Comfort Limits
                double bankRate = _phaseManager.BankRate;
                double pitchRate = _phaseManager.PitchRate;

                // Sudden movements
                if (bankRate > 15.0 || pitchRate > 10.0)
                {
                    if ((DateTime.UtcNow - _lastComfortPenaltyTime).TotalSeconds > 10)
                    {
                        _scoreManager.AddScore(-10, "Comfort Violation: Abrupt maneuvers", ScoreCategory.PassengerExperience);
                        _lastComfortPenaltyTime = DateTime.UtcNow;
                    }
                }

                // Prolonged uncomfortable bank (> 35 deg)
                if (Math.Abs(_currentBank) > 35.0 && Math.Abs(_currentBank) <= 45.0)
                {
                    if (_prolongedBankStart == null) _prolongedBankStart = DateTime.UtcNow;
                    else if ((DateTime.UtcNow - _prolongedBankStart.Value).TotalSeconds > 5)
                    {
                        if ((DateTime.UtcNow - _lastComfortPenaltyTime).TotalSeconds > 10)
                        {
                            _scoreManager.AddScore(-20, "Comfort Violation: Prolonged steep bank angle", ScoreCategory.PassengerExperience);
                            _lastComfortPenaltyTime = DateTime.UtcNow;
                        }
                    }
                }
                else
                {
                    _prolongedBankStart = null;
                }

                // High Vertical Speed near ground
                if (_currentVs < -2500 && _currentRadioHeight < 5000)
                {
                    if ((DateTime.UtcNow - _lastComfortPenaltyTime).TotalSeconds > 10)
                    {
                        _scoreManager.AddScore(-15, "Comfort Violation: High sink rate near ground", ScoreCategory.PassengerExperience);
                        _lastComfortPenaltyTime = DateTime.UtcNow;
                    }
                }
            }
        }

        private void EvaluateCriticalEvents()
        {
            var phase = _phaseManager.CurrentPhase;

            // Tail Strike Check (On ground, Takeoff/Landing, Pitch > 11)
            if (_isOnGround && (phase == FlightPhase.Takeoff || phase == FlightPhase.Landing))
            {
                if (_currentPitch > 11.0)
                {
                    if (!HasTailStrikeInPhase)
                    {
                        HasTailStrikeInPhase = true;
                        OnCriticalEventDetected?.Invoke("TAIL STRIKE");
                    }
                }
            }

            // Hard Landing Check (Landing, High G-Force)
            if (_isOnGround && phase == FlightPhase.Landing)
            {
                if (_currentGForce > 2.5) // Example threshold for Hard Landing
                {
                    if (!HasHardLandingInPhase)
                    {
                        HasHardLandingInPhase = true;
                        OnCriticalEventDetected?.Invoke("HARD LANDING");
                    }
                }
            }
        }

        private void CheckGoAroundCondition()
        {
            var phase = _phaseManager.CurrentPhase;
            if (phase == FlightPhase.Approach)
            {
                // Throttles to TOGA or MAX
                if (_throttleLever1 > 90 || _throttleLever2 > 90)
                {
                    // A proper Phase transition should be made here if we want to model Go Around.
                    // For now, tracked in FlightPhaseManager.IsGoAroundActive.
                }
            }
        }

        private void CheckRefuelingSeatbeltViolation()
        {
            if (IsRefueling && AreSeatbeltsOn && !_ticket43Violated)
            {
                _ticket43Violated = true;
                _scoreManager.AddScore(-100, "Seatbelts ON during Refueling (Fire Hazard)", ScoreCategory.FlightPhaseFlows);
            }
            if (!IsRefueling)
            {
                _ticket43Violated = false; // Reset for next turnaround
            }
        }
    }
}
