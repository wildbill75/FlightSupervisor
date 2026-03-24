using System;

namespace FlightSupervisor.UI.Services
{
    public enum FlightPhase
    {
        AtGate,
        Pushback,
        TaxiOut,
        Takeoff,
        InitialClimb,
        Climb,
        Cruise,
        Descent,
        Approach,
        Landing,
        TaxiIn,
        Arrived
    }

    public class FlightPhaseManager
    {
        public FlightPhase CurrentPhase { get; private set; } = FlightPhase.AtGate;
        public event Action<FlightPhase>? OnPhaseChanged;
        public event Action<string>? OnPenaltyTriggered;
        
        private bool _hasTriggeredOverspeedPenalty = false;
        private bool _hasTriggeredTaxiPenalty = false;
        private bool _hasTriggeredGearOverspeed = false;
        private bool _hasTriggeredGearLate = false;
        private bool _hasTriggeredGearUpBonus = false;
        private bool _hasTriggeredAbnormalGear = false;
        private bool _hasTriggeredEngineFailure = false;
        private DateTime _lastTightTurnPenalty = DateTime.MinValue;
        private double? _lastHeading = null;
        public double Heading { get; private set; }
        public bool Eng1Combustion { get; private set; } = true;
        public bool Eng2Combustion { get; private set; } = true;
        private int _taxiOverspeedSeconds = 0;
        private int _overspeedSeconds = 0;
        private double _highestAltitudeReached = 0;
        public bool IsLandingLightOn { get; set; } = false;
        public bool IsTaxiLightOn { get; set; } = false;
        public bool IsStrobeLightOn { get; set; } = false;
        public bool IsOnGround { get; set; } = true;
        public double VerticalSpeed { get; set; } = 0.0;
        public double GForce { get; set; } = 1.0;
        public double GroundSpeed { get; private set; } = 0.0;
        private bool _hasLanded = false;
        public double TouchdownFpm { get; private set; } = 0.0;
        public double TouchdownGForce { get; private set; } = 1.0;
        private DateTime _lastLightPenalty = DateTime.MinValue;
        private DateTime _lastBankPenalty = DateTime.MinValue;
        private DateTime _lastPitchPenalty = DateTime.MinValue;
        public double TargetCruiseAltitude { get; set; } = 10000;
        public double AccelerationAltitudeAgl { get; set; } = 1500; // Default NADP2 standard

        public void UpdateHeading(double heading)
        {
            Heading = heading;
        }

        public void UpdateEngineCombustion(bool eng1, bool eng2)
        {
            Eng1Combustion = eng1;
            Eng2Combustion = eng2;
        }

        public void UpdateTelemetry(double groundSpeed, double indicatedAirspeed, double altitude, double radioHeight, bool isParkingBrakeSet, bool isGearDown, double throttle, double pitch, double bank)
        {
            GroundSpeed = groundSpeed;

            // Calculate Turn Rate (degrees per second)
            double turnRate = 0;
            if (_lastHeading.HasValue)
            {
                double rawDiff = Math.Abs(Heading - _lastHeading.Value);
                turnRate = rawDiff > 180 ? 360 - rawDiff : rawDiff;
            }
            _lastHeading = Heading;
            // Track highest cruise altitude to detect Descent accurately
            if (altitude > _highestAltitudeReached && 
                (CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb || CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Cruise))
            {
                _highestAltitudeReached = altitude;
            }

            // Global Airborne Speed Limit (250kts under 10,000ft)
            if (CurrentPhase == FlightPhase.InitialClimb || CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Cruise || 
                CurrentPhase == FlightPhase.Descent || CurrentPhase == FlightPhase.Approach)
            {
                // Global Airborne Speed Limit (250kts under 10,000ft) with 260kt tolerance
                if (altitude < 10000 && indicatedAirspeed > 260.0) 
                {
                    _overspeedSeconds++;
                    if (_overspeedSeconds >= 10 && !_hasTriggeredOverspeedPenalty)
                    {
                        _hasTriggeredOverspeedPenalty = true;
                        OnPenaltyTriggered?.Invoke($"Overspeed: Aircraft exceeded 250 knots IAS below 10,000 ft! (IAS: {indicatedAirspeed:F0} kts)");
                    }
                }
                else if (indicatedAirspeed <= 250.0)
                {
                    _overspeedSeconds = 0;
                }

                // Gear Up Bonus
                if ((CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb) && !isGearDown && radioHeight > 100 && !_hasTriggeredGearUpBonus)
                {
                    _hasTriggeredGearUpBonus = true;
                    OnPenaltyTriggered?.Invoke("Gear Retraction Bonus: Clean gear up after takeoff (+50)");
                }

                // Gear deployed above 260 kts threshold
                if (isGearDown && indicatedAirspeed > 260.0 && !_hasTriggeredGearOverspeed)
                {
                    _hasTriggeredGearOverspeed = true;
                    OnPenaltyTriggered?.Invoke("Safety Violation: Landing Gear deployed above maximum extended speed (VLE > 260kts).");
                }

                // Abnormal Gear Deployment
                if (isGearDown && (CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Cruise || CurrentPhase == FlightPhase.Descent) && !_hasTriggeredAbnormalGear)
                {
                    _hasTriggeredAbnormalGear = true;
                    OnPenaltyTriggered?.Invoke("Safety Violation: Abnormal Gear Deployment (Climb/Cruise/Descent)");
                }

                // Gear forgotten on short final
                if (CurrentPhase == FlightPhase.Approach && radioHeight < 1000 && radioHeight > 50 && !isGearDown && groundSpeed > 50 && !_hasTriggeredGearLate)
                {
                    _hasTriggeredGearLate = true;
                    OnPenaltyTriggered?.Invoke("Safety: Unstable Approach (Gear Not Down below 1000ft AGL).");
                }

                // Pitch & Bank Limits (Airborne)
                if (Math.Abs(bank) > 35.0 && (DateTime.Now - _lastBankPenalty).TotalSeconds > 10)
                {
                    _lastBankPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke($"Safety Violation: Excessive Bank Angle ({Math.Abs(bank):F0}°)");
                }
                
                // Pitch Limits: > 15 up (except takeoff/climb where 20 is allowed for Airbus SRS) or < -10 down
                double maxPitchUp = (CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb || CurrentPhase == FlightPhase.Climb) ? 20.0 : 15.0;
                // In MSFS, nose up pitch is often negative or positive depending on plane, checking standard representation
                if ((pitch > maxPitchUp || pitch < -10.0) && (DateTime.Now - _lastPitchPenalty).TotalSeconds > 10)
                {
                    _lastPitchPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke($"Safety Violation: Excessive Pitch Angle ({pitch:F0}°)");
                }

                // Landing Lights Rule
                if (altitude < 9500 && radioHeight > 50 && 
                   (CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Descent || CurrentPhase == FlightPhase.Approach || CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb))
                {
                    if (!IsLandingLightOn && (DateTime.Now - _lastLightPenalty).TotalMinutes > 5)
                    {
                        _lastLightPenalty = DateTime.Now;
                        OnPenaltyTriggered?.Invoke("Safety Violation: Landing Lights OFF below 10,000ft");
                    }
                }
                else if (altitude >= 10500 && IsLandingLightOn && (DateTime.Now - _lastLightPenalty).TotalMinutes > 5)
                {
                    _lastLightPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke("Safety Violation: Landing Lights ON above 10,000ft");
                }
            }

            // Ground lighting rules (Strobe & Landing Lights OFF)
            if (CurrentPhase == FlightPhase.AtGate || CurrentPhase == FlightPhase.Pushback || 
                CurrentPhase == FlightPhase.TaxiOut || CurrentPhase == FlightPhase.TaxiIn || CurrentPhase == FlightPhase.Arrived)
            {
                if ((IsStrobeLightOn || IsLandingLightOn) && (DateTime.Now - _lastLightPenalty).TotalMinutes > 5)
                {
                    _lastLightPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke("Safety Violation: Strobes or Landing Lights ON during ground ops");
                }
            }

            // Global Taxi Ground Speed Limit (30kts)
            if (CurrentPhase == FlightPhase.TaxiOut || CurrentPhase == FlightPhase.TaxiIn)
            {
                if (groundSpeed > 30.0)
                {
                    _taxiOverspeedSeconds++;
                    // 15 seconds tolerance for taxi bursts
                    if (_taxiOverspeedSeconds >= 15 && !_hasTriggeredTaxiPenalty)
                    {
                        _hasTriggeredTaxiPenalty = true;
                        OnPenaltyTriggered?.Invoke("Taxi Overspeed: Aircraft exceeded 30kts on the ground for 15s!");
                    }
                }
                else
                {
                    _taxiOverspeedSeconds = 0;
                }
                
                // Tight Turn Penalty (Taxi)
                if (groundSpeed > 15.0 && turnRate >= 8.0 && (DateTime.Now - _lastTightTurnPenalty).TotalMinutes > 1)
                {
                    _lastTightTurnPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke("Comfort Violation: Tight turn at high speed (> 15kts)");
                }

                // Taxi Lights Rule
                if (groundSpeed > 5.0 && !IsTaxiLightOn && (DateTime.Now - _lastLightPenalty).TotalMinutes > 5)
                {
                    _lastLightPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke("Safety Violation: Taxiing without Taxi Lights ON");
                }
            }

            // Engine Failure Detection (between Takeoff and Landing)
            if ((CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb || 
                 CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Cruise || 
                 CurrentPhase == FlightPhase.Descent || CurrentPhase == FlightPhase.Approach) && 
                 (!Eng1Combustion || !Eng2Combustion) && !_hasTriggeredEngineFailure)
            {
                _hasTriggeredEngineFailure = true;
                OnPenaltyTriggered?.Invoke("Critical Safety Violation: In-Flight Engine Failure");
            }

            // Intelligent Rule-Based State Machine
            switch (CurrentPhase)
            {
                case FlightPhase.AtGate:
                    if (groundSpeed >= 0.5 && !isParkingBrakeSet) ChangePhase(FlightPhase.Pushback);
                    break;
                
                case FlightPhase.Pushback:
                    if (groundSpeed >= 8.0) ChangePhase(FlightPhase.TaxiOut);
                    break;

                case FlightPhase.TaxiOut:
                    if (groundSpeed >= 40.0 || throttle >= 60.0)
                    {
                        if (IsStrobeLightOn && IsLandingLightOn && IsTaxiLightOn)
                        {
                            OnPenaltyTriggered?.Invoke("Line-up Configuration Bonus: Strobes/Landing/Taxi ON");
                        }
                        ChangePhase(FlightPhase.Takeoff);
                    }
                    break;
                
                case FlightPhase.Takeoff:
                    if (radioHeight >= 400) ChangePhase(FlightPhase.InitialClimb);
                    break;

                case FlightPhase.InitialClimb:
                    if (radioHeight >= AccelerationAltitudeAgl) ChangePhase(FlightPhase.Climb);
                    break;

                case FlightPhase.Climb:
                    // If within 500ft of target cruise altitude and leveling off (VS < 500 fpm)
                    if (altitude >= TargetCruiseAltitude - 500 && Math.Abs(VerticalSpeed) < 500) 
                    {
                        ChangePhase(FlightPhase.Cruise);
                        TargetCruiseAltitude = altitude; // Dynamically adjust if we leveled off higher/lower
                    }
                    break;

                case FlightPhase.Cruise:
                    // If target cruise altitude was updated mid-flight and we are still climbing towards it
                    if (altitude < TargetCruiseAltitude - 1000)
                    {
                        ChangePhase(FlightPhase.Climb);
                        break;
                    }
                    // Secured Descent Trigger (Step Climbs & TCAS tolerance)
                    // The aircraft must drop 3000ft below its highest cruise altitude to confirm a real descent.
                    // This prevents TCAS RAs or minor step descents from triggering the approach flow.
                    if (VerticalSpeed < -500 && altitude < _highestAltitudeReached - 3000)
                    {
                        ChangePhase(FlightPhase.Descent);
                    }
                    else if (altitude < 10000 && altitude > 5000 && TargetCruiseAltitude > 10000) // Fallback
                    {
                        ChangePhase(FlightPhase.Descent);
                    }
                    break;
                
                case FlightPhase.Descent:
                    if (radioHeight > 0 && radioHeight < 4000) ChangePhase(FlightPhase.Approach);
                    break;
                
                case FlightPhase.Approach:
                case FlightPhase.Landing:
                    if (radioHeight > 0 && radioHeight <= 50 && !IsOnGround)
                    {
                        TouchdownFpm = VerticalSpeed; // Store last known VS before ground
                        if (CurrentPhase != FlightPhase.Landing && groundSpeed < 170) ChangePhase(FlightPhase.Landing);
                    }
                    
                    if (IsOnGround && !_hasLanded)
                    {
                        _hasLanded = true;
                        TouchdownGForce = GForce;
                        string landingQuality = "Normal Landing";
                        if (TouchdownFpm > -150) landingQuality = "Butter Landing";
                        else if (TouchdownFpm < -600) landingQuality = "Severe Hard Landing";
                        else if (TouchdownFpm < -450) landingQuality = "Hard Landing";
                        
                        OnPenaltyTriggered?.Invoke($"{landingQuality}: Touchdown at {TouchdownFpm:F0} fpm ({TouchdownGForce:F2}G)");
                    }
                    
                    if (_hasLanded && groundSpeed < 35.0)
                    {
                        ChangePhase(FlightPhase.TaxiIn);
                    }
                    break;
                
                case FlightPhase.TaxiIn:
                    if (groundSpeed < 0.5 && isParkingBrakeSet) ChangePhase(FlightPhase.Arrived);
                    break;
            }
        }

        private void ChangePhase(FlightPhase newPhase)
        {
            if (CurrentPhase != newPhase)
            {
                CurrentPhase = newPhase;
                
                if (CurrentPhase == FlightPhase.AtGate || CurrentPhase == FlightPhase.Pushback)
                {
                    _hasLanded = false;
                    _hasTriggeredTaxiPenalty = false;
                    _hasTriggeredGearLate = false;
                    _hasTriggeredGearOverspeed = false;
                    _hasTriggeredGearUpBonus = false;
                    _hasTriggeredAbnormalGear = false;
                    _hasTriggeredEngineFailure = false;
                }
                else if (CurrentPhase == FlightPhase.Descent || CurrentPhase == FlightPhase.Approach)
                {
                    _hasTriggeredOverspeedPenalty = false;
                }

                if (CurrentPhase == FlightPhase.AtGate)
                {
                    _hasTriggeredOverspeedPenalty = false;
                    _hasTriggeredTaxiPenalty = false;
                    _taxiOverspeedSeconds = 0;
                    _overspeedSeconds = 0;
                    _highestAltitudeReached = 0;
                }
                OnPhaseChanged?.Invoke(CurrentPhase);
            }
        }

        public void Reset()
        {
            ChangePhase(FlightPhase.AtGate);
            _highestAltitudeReached = 0;
            TouchdownFpm = 0.0;
            TouchdownGForce = 1.0;
            _hasLanded = false;
            IsOnGround = true;
        }
    }
}
