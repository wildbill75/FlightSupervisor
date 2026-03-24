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
        public double WindDirection { get; private set; } = 0.0;
        public double WindVelocity { get; private set; } = 0.0;
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
        public string AircraftCategory { get; set; } = "Medium";
        public double NavLocalizerError { get; set; } = 0.0;
        public double GpsCrossTrackError { get; set; } = 0.0;
        public bool HasLocalizer { get; set; } = false;
        private DateTime? _timeAt50Ft = null;
        private double? _lastGroundSpeed = null;
        private DateTime? _lastGroundSpeedTime = null;
        private DateTime _lastVsPenalty = DateTime.MinValue;
        private DateTime _lastBrakingPenalty = DateTime.MinValue;
        private DateTime? _taxiInStartTime = null;
        private System.Collections.Generic.Queue<double> _vsHistory = new System.Collections.Generic.Queue<double>();

        public void UpdateHeading(double heading)
        {
            Heading = heading;
        }

        public void UpdateWind(double direction, double velocity)
        {
            WindDirection = direction;
            WindVelocity = velocity;
        }

        public void UpdateNavigation(double locErr, double gpsErr, bool hasLoc)
        {
            NavLocalizerError = locErr;
            GpsCrossTrackError = gpsErr;
            HasLocalizer = hasLoc;
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
            
            
            if (radioHeight <= 100)
            {
                _vsHistory.Enqueue(VerticalSpeed);
                if (_vsHistory.Count > 5) _vsHistory.Dequeue();
            }

            // Calculate Deceleration
            double decelerationKnotsPerSec = 0;
            if (_lastGroundSpeed.HasValue && _lastGroundSpeedTime.HasValue)
            {
                double dt = (DateTime.Now - _lastGroundSpeedTime.Value).TotalSeconds;
                if (dt >= 0.05)
                {
                    decelerationKnotsPerSec = (_lastGroundSpeed.Value - groundSpeed) / dt;
                }
            }
            _lastGroundSpeed = groundSpeed;
            _lastGroundSpeedTime = DateTime.Now;
            // Track highest cruise altitude to detect Descent accurately
            if (altitude > _highestAltitudeReached && 
                (CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb || CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Cruise))
            {
                _highestAltitudeReached = altitude;
            }

            // Global Airborne Speed Limit (250kts under 10,000ft)
            if (CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb || CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Cruise || 
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
                else if (Math.Abs(bank) > 28.0 && (DateTime.Now - _lastBankPenalty).TotalSeconds > 10)
                {
                    _lastBankPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke($"Comfort Violation: Steep Bank Angle ({Math.Abs(bank):F0}°) causing passenger anxiety");
                }
                
                if (Math.Abs(VerticalSpeed) > 2800 && radioHeight > 1500 && (DateTime.Now - _lastVsPenalty).TotalSeconds > 10)
                {
                    _lastVsPenalty = DateTime.Now;
                    string vsDir = VerticalSpeed > 0 ? "Climb" : "Descent";
                    OnPenaltyTriggered?.Invoke($"Comfort Violation: High Vertical Speed ({Math.Abs(VerticalSpeed):F0} fpm {vsDir}) causing ear pressure");
                }
                
                // Pitch Limits: > 15 up (except takeoff/climb where 20 is allowed for Airbus SRS) or < -10 down
                double maxPitchUp = (CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb || CurrentPhase == FlightPhase.Climb) ? 20.0 : 15.0;
                // In MSFS, nose up pitch is often negative or positive depending on plane, checking standard representation
                if ((pitch > maxPitchUp || pitch < -10.0) && (DateTime.Now - _lastPitchPenalty).TotalSeconds > 10)
                {
                    _lastPitchPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke($"Safety Violation: Excessive Pitch Angle ({pitch:F0}°)");
                }
                else if ((pitch > maxPitchUp - 3.0 || pitch < -7.0) && radioHeight > 500 && (DateTime.Now - _lastPitchPenalty).TotalSeconds > 10)
                {
                    _lastPitchPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke($"Comfort Violation: Uncomfortable Pitch Angle ({pitch:F0}°) felt in cabin");
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
            // Removed TaxiOut & Pushback so the user can turn them on at engine start or holding point
            if (CurrentPhase == FlightPhase.AtGate || 
                CurrentPhase == FlightPhase.Arrived ||
                CurrentPhase == FlightPhase.Arrived ||
                (CurrentPhase == FlightPhase.TaxiIn && _taxiInStartTime.HasValue && (DateTime.Now - _taxiInStartTime.Value).TotalSeconds > 120))
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
                    // 10 seconds tolerance for taxi bursts
                    if (_taxiOverspeedSeconds >= 10 && !_hasTriggeredTaxiPenalty)
                    {
                        _hasTriggeredTaxiPenalty = true;
                        OnPenaltyTriggered?.Invoke("Taxi Overspeed: Aircraft exceeded 30kts on the ground for 10s!");
                    }
                }
                else if (_taxiOverspeedSeconds > 0)
                {
                    _taxiOverspeedSeconds--;
                }
                
                // Tight Turn Penalty (Taxi)
                if (groundSpeed > 15.0 && turnRate >= 8.0 && (DateTime.Now - _lastTightTurnPenalty).TotalMinutes > 1)
                {
                    _lastTightTurnPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke("Comfort Violation: Tight turn at high speed (> 15kts)");
                }

                // Harsh Braking Penalty (Taxi)
                if (decelerationKnotsPerSec > 8.0 && groundSpeed > 2.0 && (DateTime.Now - _lastBrakingPenalty).TotalMinutes > 1)
                {
                    _lastBrakingPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke($"Comfort Violation: Harsh braking ({decelerationKnotsPerSec:F1} kts/sec)");
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
                    if ((throttle >= 60.0 && groundSpeed >= 40.0) || groundSpeed > 60.0)
                    {
                        if (IsStrobeLightOn && IsLandingLightOn && IsTaxiLightOn)
                        {
                            OnPenaltyTriggered?.Invoke("Line-up Configuration Bonus: Strobes/Landing/Taxi ON");
                        }
                        else
                        {
                            OnPenaltyTriggered?.Invoke("Safety Violation: Poor Line-up Configuration (Missing Lights)");
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
                    // Step Climb Detection (Left cruise altitude to climb higher)
                    // We need to see a positive vertical speed and an altitude clearly above our previous cruise altitude
                    if (VerticalSpeed > 500 && altitude > TargetCruiseAltitude + 1000)
                    {
                        TargetCruiseAltitude = altitude + 2000; // Bump target up temporarily so we can re-evaluate leveling off
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
                    else if (altitude < 10000 && altitude > 5000 && TargetCruiseAltitude > 10000 && VerticalSpeed < -500) // Fallback
                    {
                        ChangePhase(FlightPhase.Descent);
                    }
                    break;
                
                case FlightPhase.Descent:
                    if (radioHeight > 0 && radioHeight < 4000) ChangePhase(FlightPhase.Approach);
                    break;
                
                case FlightPhase.Approach:
                    if (radioHeight > 0 && radioHeight <= 50.0 && _timeAt50Ft == null)
                    {
                        _timeAt50Ft = DateTime.Now;
                    }
                    if (radioHeight <= 50 && !IsOnGround)
                    {
                        if (_vsHistory.Count > 0)
                        {
                            // On prend la valeur la plus représentative avant le spike du gear compression
                            // MSFS a tendance à donner un spike positif ou fortement négatif à l'instant du contact.
                            TouchdownFpm = _vsHistory.Average(); 
                        }
                        else
                        {
                            TouchdownFpm = VerticalSpeed;
                        }
                        
                        if (groundSpeed < 170) ChangePhase(FlightPhase.Landing);
                    }
                    break;
                case FlightPhase.Landing:
                    if (IsOnGround && !_hasLanded)
                    {
                        _hasLanded = true;
                        TouchdownGForce = GForce;
                        string landingQuality = "Normal Landing";
                        if (TouchdownFpm > -150) landingQuality = "Butter Landing";
                        else if (TouchdownFpm < -600) landingQuality = "Severe Hard Landing";
                        else if (TouchdownFpm < -450) landingQuality = "Hard Landing";
                        
                        OnPenaltyTriggered?.Invoke($"{landingQuality}: Touchdown at {TouchdownFpm:F0} fpm ({TouchdownGForce:F2}G)");

                        // Touchdown Zone Time Evaluation
                        if (_timeAt50Ft.HasValue)
                        {
                            double flareSeconds = (DateTime.Now - _timeAt50Ft.Value).TotalSeconds;
                            double minFlare = 4.0;
                            double maxFlare = 7.0;
                            
                            if (AircraftCategory == "Heavy") { minFlare = 5.0; maxFlare = 9.0; }
                            else if (AircraftCategory == "Light") { minFlare = 3.0; maxFlare = 6.0; }

                            if (flareSeconds < minFlare) {
                                OnPenaltyTriggered?.Invoke($"Short Landing (-100): Touchdown trop tôt {flareSeconds:F1}s (Idéal: {minFlare}-{maxFlare}s)");
                            } 
                            else if (flareSeconds > maxFlare) {
                                OnPenaltyTriggered?.Invoke($"Float Landing (-100): Touchdown trop tard {flareSeconds:F1}s (Idéal: {minFlare}-{maxFlare}s)");
                            }
                            else {
                                OnPenaltyTriggered?.Invoke($"Perfect Touchdown Zone (+50): {flareSeconds:F1}s d'arrondi dans la zone idéale !");
                            }
                        }

                        // Centerline logic
                        double dev = 0.0;
                        string devSource = "";
                        if (HasLocalizer)
                        {
                            dev = Math.Abs(NavLocalizerError); // in degrees
                            devSource = "ILS Localizer";
                            if (dev > 1.0) OnPenaltyTriggered?.Invoke($"Centerline Deviation (-100): {dev:F2}° off-center ({devSource}) !");
                            else OnPenaltyTriggered?.Invoke($"Perfect Centerline (+50): {dev:F2}° sur l'axe ({devSource})");
                        }
                        else 
                        {
                            dev = Math.Abs(GpsCrossTrackError); // in meters
                            devSource = "GPS Track";
                            if (dev > 25.0) OnPenaltyTriggered?.Invoke($"Centerline Deviation (-100): {dev:F0}m off-center ({devSource}) !");
                            else OnPenaltyTriggered?.Invoke($"Perfect Centerline (+50): {dev:F0}m sur l'axe ({devSource})");
                        }
                        
                        // Override Crosswind Bonus if dev is bad
                        bool goodCenterline = (HasLocalizer && dev <= 1.0) || (!HasLocalizer && dev <= 25.0);

                        // Crosswind Bonus Calculation
                        double angleRad = (WindDirection - Heading) * Math.PI / 180.0;
                        double crosswind = WindVelocity * Math.Abs(Math.Sin(angleRad));

                        if (goodCenterline)
                        {
                            if (crosswind > 25.0)
                            {
                                OnPenaltyTriggered?.Invoke($"Extreme Crosswind Landing (+150): {crosswind:F0} kts crosswind neutralized!");
                            }
                            else if (crosswind > 20.0)
                            {
                                OnPenaltyTriggered?.Invoke($"Great Crosswind Landing (+100): {crosswind:F0} kts crosswind neutralized!");
                            }
                            else if (crosswind > 15.0)
                            {
                                OnPenaltyTriggered?.Invoke($"Nice Crosswind Landing (+50): {crosswind:F0} kts crosswind neutralized!");
                            }
                        }
                        else if (crosswind > 15.0)
                        {
                            OnPenaltyTriggered?.Invoke($"Crosswind Bonus Cancelled: Centerline not maintained.");
                        }
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
                
                if (CurrentPhase == FlightPhase.TaxiIn)
                {
                    _taxiInStartTime = DateTime.Now;
                }
                
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
                    _timeAt50Ft = null;
                    _taxiInStartTime = null;
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
            _timeAt50Ft = null;
            _vsHistory.Clear();
        }
    }
}
