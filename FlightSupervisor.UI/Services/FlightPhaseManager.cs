using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightSupervisor.UI.Services
{
    public enum FlightPhase
    {
        AtGate,
        Turnaround,
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

    public enum TurbulenceSeverityLevel
    {
        None,
        Light,
        Moderate,
        Severe,
        Extreme
    }

    public class FlightPhaseManager
    {
        public FlightPhase CurrentPhase { get; private set; } = FlightPhase.AtGate;
        
        public bool IsPaused { get; set; } = false;
        private DateTime _immunityEndTime = DateTime.MinValue;

        public void ResumeWithImmunity(int seconds = 5)
        {
            IsPaused = false;
            _immunityEndTime = DateTime.Now.AddSeconds(seconds);
            _gForceHistory.Clear();
            _vsHistory.Clear();
        }
        
        public string GetLocalizedPhaseName()
        {
            switch (CurrentPhase)
            {
                case FlightPhase.AtGate: return LocalizationService.Translate("At Gate", "À la porte");
                case FlightPhase.Turnaround: return LocalizationService.Translate("Turnaround", "Escale");
                case FlightPhase.Pushback: return LocalizationService.Translate("Pushback", "Repoussage");
                case FlightPhase.TaxiOut: return LocalizationService.Translate("Taxi Out", "Roulage (Départ)");
                case FlightPhase.Takeoff: return LocalizationService.Translate("Takeoff", "Décollage");
                case FlightPhase.InitialClimb: return LocalizationService.Translate("Initial Climb", "Montée Initiale");
                case FlightPhase.Climb: return LocalizationService.Translate("Climb", "Montée");
                case FlightPhase.Cruise: return LocalizationService.Translate("Cruise", "Croisière");
                case FlightPhase.Descent: return LocalizationService.Translate("Descent", "Descente");
                case FlightPhase.Approach: return LocalizationService.Translate("Approach", "Approche");
                case FlightPhase.Landing: return LocalizationService.Translate("Landing", "Atterrissage");
                case FlightPhase.TaxiIn: return LocalizationService.Translate("Taxi In", "Roulage (Arrivée)");
                case FlightPhase.Arrived: return LocalizationService.Translate("Arrived", "Arrivé");
                default: return CurrentPhase.ToString();
            }
        }
        
        public event Action<FlightPhase>? OnPhaseChanged;
        public event Action<string>? OnPenaltyTriggered;
        public event Action<string>? OnFoMessage;
        
        private bool _hasTriggeredOverspeedPenalty = false;
        private bool _hasTriggeredTaxiPenalty = false;
        private bool _hasTriggeredGearOverspeed = false;
        private bool _hasTriggeredGearLate = false;
        private bool _hasTriggeredGearUpBonus = false;
        private bool _hasTriggered10kClimbBonus = false;
        private bool _hasTriggeredAbnormalGear = false;
        private bool _hasTriggeredEngineFailure = false;
        
        private bool _hasFoWarnedGearUp = false;
        private bool _hasFoWarnedGearDown = false;
        private bool _hasFoWarnedSeatbeltTaxi = false;
        private bool _hasFoWarnedSeatbelt10k = false;
        private bool _hasFoWarnedSeatbeltDesc = false;
        private bool _hasFoWarnedLights10kClimb = false;
        private bool _hasFoWarnedLights10kDesc = false;
        private DateTime _lastTightTurnPenalty = DateTime.MinValue;
        private double? _lastHeading = null;
        public double Heading { get; private set; }
        public double WindDirection { get; private set; } = 0.0;
        public double WindVelocity { get; private set; } = 0.0;
        public double Latitude { get; private set; } = 0.0;
        public double Longitude { get; private set; } = 0.0;
        public bool Eng1Combustion { get; private set; } = true;
        public bool Eng2Combustion { get; private set; } = true;
        private int _taxiOverspeedSeconds = 0;
        private int _overspeedSeconds = 0;
        public bool IsGoAroundActive { get; private set; } = false;
        public bool IsSevereTurbulenceActive { get; private set; } = false;
        public bool HasEngineFailure => _hasTriggeredEngineFailure;
        private DateTime? _goAroundStartTime = null;
        private double _highestAltitudeReached = 0;
        private bool _isAutopilotActive = false;
        private bool _isAutothrustActive = false;
        public int ManualFlyingSecondsApproach { get; private set; } = 0;
        private DateTime? _manualFlyingStart = null;
        public bool IsLandingLightOn { get; set; } = false;
        public bool IsTaxiLightOn { get; set; } = false;
        public bool IsStrobeLightOn { get; set; } = false;
        public bool IsOnGround { get; set; } = true;
        public double VerticalSpeed { get; set; } = 0.0;
        private double _gForce = 1.0;
        public double GForce 
        { 
            get => _gForce;
            set 
            {
                if (IsPaused || DateTime.Now < _immunityEndTime) return;
                _gForce = value;
                CalculateTurbulence(value);
            }
        }
        public double GroundSpeed { get; private set; } = 0.0;
        private bool _hasLanded = false;
        public double TouchdownFpm { get; private set; } = 0.0;
        public double TouchdownGForce { get; private set; } = 1.0;

        public TurbulenceSeverityLevel TurbulenceSeverity { get; private set; } = TurbulenceSeverityLevel.None;
        private System.Collections.Generic.Queue<double> _gForceHistory = new System.Collections.Generic.Queue<double>();
        private const int JitterWindowSize = 300; // ~5 seconds at Visual Frame rate (60Hz)

        // Fenix specific states
        public int FenixNoseLight { get; set; } = 0; // 0=OFF, 1=TAXI, 2=TO
        public bool IsRunwayTurnoffLightOn { get; set; } = false;
        public int FenixStrobeLight { get; set; } = 0; // 0=OFF, 1=AUTO, 2=ON
        public bool IsSeatbeltsOn { get; set; } = true;
        public bool FenixApuMaster { get; set; } = false;
        public bool FenixApuStart { get; set; } = false;
        public bool FenixApuBleed { get; set; } = false;
        public bool FenixPack1 { get; set; } = false;
        public bool FenixPack2 { get; set; } = false;
        
        public float FenixCabinTempCockpit { get; set; } = 0f;
        public float FenixCabinTempFwd { get; set; } = 0f;
        public float FenixCabinTempAft { get; set; } = 0f;
        private DateTime _lastApuPenalty = DateTime.MinValue;
        private DateTime _lastLightPenalty = DateTime.MinValue;
        private DateTime _lastBankPenalty = DateTime.MinValue;
        private DateTime _lastPitchPenalty = DateTime.MinValue;
        private DateTime _lastGForcePenalty = DateTime.MinValue;
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
        private double? _lastPitch = null;
        private double? _lastBank = null;
        public double PitchRate { get; private set; } = 0.0;
        public double BankRate { get; private set; } = 0.0;

        public void UpdateHeading(double heading)
        {
            Heading = heading;
        }

        public void UpdateWind(double direction, double velocity)
        {
            WindDirection = direction;
            WindVelocity = velocity;
        }

        public void UpdatePosition(double lat, double lon)
        {
            Latitude = lat;
            Longitude = lon;
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

        public void UpdateAutopilot(bool isActive)
        {
            _isAutopilotActive = isActive;
        }

        public void UpdateAutothrust(bool isActive)
        {
            _isAutothrustActive = isActive;
        }

        public void UpdateTelemetry(double groundSpeed, double indicatedAirspeed, double altitude, double radioHeight, bool isParkingBrakeSet, bool isGearDown, double throttle, double pitch, double bank)
        {
            if (IsPaused || DateTime.Now < _immunityEndTime) return;

            GroundSpeed = groundSpeed;

            // Calculate Turn Rate (degrees per second)
            double turnRate = 0;
            if (_lastHeading.HasValue)
            {
                double rawDiff = Math.Abs(Heading - _lastHeading.Value);
                turnRate = rawDiff > 180 ? 360 - rawDiff : rawDiff;
            }
            _lastHeading = Heading;
            
            
            if (radioHeight <= 100 && radioHeight >= 2.0 && !IsOnGround)
            {
                _vsHistory.Enqueue(VerticalSpeed);
                if (_vsHistory.Count > 5) _vsHistory.Dequeue();
            }

            // Calculate Deceleration and Rates
            double decelerationKnotsPerSec = 0;
            if (_lastGroundSpeed.HasValue && _lastGroundSpeedTime.HasValue)
            {
                double dt = (DateTime.Now - _lastGroundSpeedTime.Value).TotalSeconds;
                if (dt >= 0.05)
                {
                    decelerationKnotsPerSec = (_lastGroundSpeed.Value - groundSpeed) / dt;
                    if (_lastPitch.HasValue && _lastBank.HasValue)
                    {
                        PitchRate = Math.Abs(pitch - _lastPitch.Value) / dt;
                        BankRate = Math.Abs(bank - _lastBank.Value) / dt;
                    }
                }
            }
            _lastGroundSpeed = groundSpeed;
            _lastGroundSpeedTime = DateTime.Now;
            _lastPitch = pitch;
            _lastBank = bank;

            // Track highest cruise altitude to detect Descent accurately
            if (altitude > _highestAltitudeReached && 
                (CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb || CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Cruise))
            {
                _highestAltitudeReached = altitude;
            }

            // Logic for Manual Flying Approach Tracker
            if (CurrentPhase == FlightPhase.Approach || CurrentPhase == FlightPhase.Landing)
            {
                if (!_isAutopilotActive && !_isAutothrustActive)
                {
                    if (_manualFlyingStart == null) _manualFlyingStart = DateTime.Now;
                    else ManualFlyingSecondsApproach = (int)(DateTime.Now - _manualFlyingStart.Value).TotalSeconds;
                }
                else
                {
                    _manualFlyingStart = null;
                    ManualFlyingSecondsApproach = 0;
                }
            }

            // --- VIRTUAL FO CALLOUTS (MVP) ---
            if (!isGearDown && CurrentPhase == FlightPhase.Approach && radioHeight < 2000 && !_hasFoWarnedGearDown)
            {
                _hasFoWarnedGearDown = true;
                OnFoMessage?.Invoke("Captain, crossing 2000, gear is not down.");
            }
            if (isGearDown && (CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb || CurrentPhase == FlightPhase.Climb) && indicatedAirspeed > 200.0 && !_hasFoWarnedGearUp)
            {
                _hasFoWarnedGearUp = true;
                OnFoMessage?.Invoke("Captain, speed is increasing, gear is still down.");
            }
            if (CurrentPhase == FlightPhase.TaxiOut && groundSpeed > 5.0 && !IsSeatbeltsOn && !_hasFoWarnedSeatbeltTaxi)
            {
                _hasFoWarnedSeatbeltTaxi = true;
                OnFoMessage?.Invoke("Captain, aircraft is moving, seatbelts are off.");
            }
            if ((CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Cruise) && altitude > 10500 && IsSeatbeltsOn && !_hasFoWarnedSeatbelt10k)
            {
                _hasFoWarnedSeatbelt10k = true;
                OnFoMessage?.Invoke("Passing 10,000, consider releasing the cabin.");
            }
            if ((CurrentPhase == FlightPhase.Descent || CurrentPhase == FlightPhase.Approach) && altitude < 9500 && !IsSeatbeltsOn && !_hasFoWarnedSeatbeltDesc)
            {
                _hasFoWarnedSeatbeltDesc = true;
                OnFoMessage?.Invoke("Passing 10,000, seatbelts sign is off.");
            }
            if ((CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Cruise) && altitude > 10500 && (IsLandingLightOn || FenixNoseLight == 2) && !_hasFoWarnedLights10kClimb)
            {
                _hasFoWarnedLights10kClimb = true;
                OnFoMessage?.Invoke("Passing 10,000, landing lights are still on.");
            }
            if ((CurrentPhase == FlightPhase.Descent || CurrentPhase == FlightPhase.Approach) && altitude < 9500 && !IsLandingLightOn && FenixNoseLight == 0 && !_hasFoWarnedLights10kDesc)
            {
                _hasFoWarnedLights10kDesc = true;
                OnFoMessage?.Invoke("Passing 10,000, landing lights are off.");
            }
            // ----------------------------------

            // Global Airborne Speed Limit (250kts under 10,000ft)
            if (CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb || CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Cruise || 
                CurrentPhase == FlightPhase.Descent || CurrentPhase == FlightPhase.Approach)
            {
                // Global Airborne Speed Limit (250kts under 10,000ft) with 260kt tolerance
                if (altitude < 9500 && indicatedAirspeed > 260.0) 
                {
                    _overspeedSeconds++;
                    if (_overspeedSeconds >= 10 && !_hasTriggeredOverspeedPenalty)
                    {
                        _hasTriggeredOverspeedPenalty = true;
                        OnPenaltyTriggered?.Invoke(LocalizationService.Translate(
                            $"Overspeed: Aircraft exceeded 250 knots IAS below 10,000 ft! (IAS: {indicatedAirspeed:F0} kts)",
                            $"Survitesse: Avion > 250 kts IAS sous 10,000 ft! (IAS: {indicatedAirspeed:F0} kts)"
                        ));
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
                    OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Gear Retraction Bonus: Clean gear up after takeoff (+50)", "Bonus Train Rentré: Rentrée propre après décollage (+50)"));
                }

                // Gear deployed above 260 kts threshold
                if (isGearDown && indicatedAirspeed > 260.0 && !_hasTriggeredGearOverspeed)
                {
                    _hasTriggeredGearOverspeed = true;
                    OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Safety Violation: Landing Gear deployed above maximum extended speed (VLE > 260kts).", "Violation Sécurité: Train atterrissage déployé au-dessus de la vitesse limite (VLE > 260kts)."));
                }

                // Abnormal Gear Deployment
                if (isGearDown && (CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Cruise || CurrentPhase == FlightPhase.Descent) && !_hasTriggeredAbnormalGear)
                {
                    _hasTriggeredAbnormalGear = true;
                    OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Safety Violation: Abnormal Gear Deployment (Climb/Cruise/Descent)", "Violation Sécurité: Déploiement anormal du train d'atterrissage"));
                }

                // Gear forgotten on short final
                if (CurrentPhase == FlightPhase.Approach && radioHeight < 1000 && radioHeight > 50 && !isGearDown && groundSpeed > 50 && !_hasTriggeredGearLate)
                {
                    _hasTriggeredGearLate = true;
                    OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Safety: Unstable Approach (Gear Not Down below 1000ft AGL).", "Sécurité: Approche Instable (Train non sorti sous 1000ft AGL)."));
                }

                // Pitch & Bank Limits (Airborne)
                if (Math.Abs(bank) > 67.0 && (DateTime.Now - _lastBankPenalty).TotalSeconds > 10)
                {
                    _lastBankPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Safety Violation: Structural Bank Limit Exceeded ({Math.Abs(bank):F0}°)", $"Violation Sécurité: Limite Structurelle Roulis Dépassée ({Math.Abs(bank):F0}°)"));
                }
                else if (Math.Abs(bank) > 35.0 && (DateTime.Now - _lastBankPenalty).TotalSeconds > 10)
                {
                    _lastBankPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Safety Violation: Excessive Bank Angle ({Math.Abs(bank):F0}°)", $"Violation Sécurité: Angle d'inclinaison excessif ({Math.Abs(bank):F0}°)"));
                }
                

                // Pitch Limits: > 30 up or < -15 down (Structural Limits)
                if ((pitch > 30.0 || pitch < -15.0) && (DateTime.Now - _lastPitchPenalty).TotalSeconds > 10)
                {
                    _lastPitchPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Safety Violation: Structural Pitch Limit Exceeded ({pitch:F0}°)", $"Violation Sécurité: Limite Structurelle Assiette Dépassée ({pitch:F0}°)"));
                }
                else 
                {
                    double maxPitchUp = (CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb || CurrentPhase == FlightPhase.Climb) ? 20.0 : 15.0;
                    if ((pitch > maxPitchUp || pitch < -10.0) && (DateTime.Now - _lastPitchPenalty).TotalSeconds > 10)
                    {
                        _lastPitchPenalty = DateTime.Now;
                        OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Safety Violation: Excessive Pitch Angle ({pitch:F0}°)", $"Violation Sécurité: Assiette excessive ({pitch:F0}°)"));
                    }
                }

                    // Severe turbulence flag is now driven by the categorizer
                    IsSevereTurbulenceActive = TurbulenceSeverity >= TurbulenceSeverityLevel.Severe;
                    if (Math.Abs(GForce - 1.0) < 0.1)
                    {
                        IsSevereTurbulenceActive = false;
                    }

                    if ((DateTime.Now - _lastGForcePenalty).TotalSeconds > 10)
                    {
                        if (GForce > 2.0 || GForce < 0.0)
                        {
                            _lastGForcePenalty = DateTime.Now;
                            OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Safety Violation: Structural G-Force Limit Exceeded ({GForce:F2}G)", $"Violation Sécurité: Limite Structurelle Force G dépassée ({GForce:F2}G)"));
                        }
                        else if (GForce > 1.6 || GForce < 0.4)
                        {
                            _lastGForcePenalty = DateTime.Now;
                            OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Safety Violation: Severe G-Force ({GForce:F2}G)", $"Violation Sécurité: Force G Sévère ({GForce:F2}G)"));
                        }
                    }
                }

                // Landing Lights Rule
                if (altitude < 9500 && radioHeight > 50 && 
                   (CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Descent || CurrentPhase == FlightPhase.Approach || CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb))
                {
                    if (!IsLandingLightOn && (DateTime.Now - _lastLightPenalty).TotalMinutes > 0.5)
                    {
                        _lastLightPenalty = DateTime.Now;
                        OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Safety Violation: Landing Lights OFF below 10,000ft", "Violation Sécurité: Phares d'atterrissage ETEINTS sous 10,000ft"));
                    }
                }
                else if (altitude >= 10500 && (IsLandingLightOn || FenixNoseLight == 2) && (DateTime.Now - _lastLightPenalty).TotalMinutes > 0.5)
                {
                    _lastLightPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Safety Violation: Landing Lights ON above 10,000ft", "Violation Sécurité: Phares d'atterrissage ALLUMES au-dessus de 10,000ft"));
                }

            // APU Left ON during Cruise Penalty - DISABLED FOR NOW (STORY 48)
            /*
            if (CurrentPhase == FlightPhase.Cruise && FenixApuMaster && (DateTime.Now - _lastApuPenalty).TotalMinutes > 15)
            {
                _lastApuPenalty = DateTime.Now;
                OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Efficiency Violation: APU left running during Cruise!", "Violation Efficacité: APU oublié en Croisière!"));
            }
            */

            // Unpressurized Takeoff Penalty
            if ((CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb) && 
                !FenixPack1 && !FenixPack2 && !FenixApuBleed)
            {
                if ((DateTime.Now - _lastApuPenalty).TotalMinutes > 0.5)
                {
                    _lastApuPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke(LocalizationService.Translate(
                        "Safety Violation: Unpressurized Takeoff! (Packs & APU Bleed OFF)", 
                        "Violation Sécurité: Décollage non pressurisé ! (Packs & APU Bleed OFF)"
                    ));
                }
            }

            // Ground lighting rules (Strobe & Landing Lights OFF)
            // Removed TaxiOut & Pushback so the user can turn them on at engine start or holding point
            if (CurrentPhase == FlightPhase.AtGate || 
                CurrentPhase == FlightPhase.Turnaround || 
                CurrentPhase == FlightPhase.Arrived ||
                (CurrentPhase == FlightPhase.TaxiIn && _taxiInStartTime.HasValue && (DateTime.Now - _taxiInStartTime.Value).TotalSeconds > 120))
            {
                if (((IsStrobeLightOn && FenixStrobeLight != 1) || IsLandingLightOn) && (DateTime.Now - _lastLightPenalty).TotalMinutes > 0.5)
                {
                    _lastLightPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Safety Violation: Strobes or Landing Lights ON during ground ops", "Violation Sécurité: Strobes ou phares atterrissage ALLUMÉS au sol"));
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
                        OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Taxi Overspeed: Aircraft exceeded 30kts on the ground for 10s!", "Excès vitesse Roulage: Avion > 30kts au sol pendant 10s!"));
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
                    OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Comfort Violation: Tight turn at high speed (> 15kts)", "Violation Confort: Virage serré à vitesse élevée (> 15kts)"));
                }

                // Harsh Braking Penalty (Taxi)
                if (decelerationKnotsPerSec > 8.0 && groundSpeed > 2.0 && (DateTime.Now - _lastBrakingPenalty).TotalMinutes > 1)
                {
                    _lastBrakingPenalty = DateTime.Now;
                    OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Comfort Violation: Harsh braking ({decelerationKnotsPerSec:F1} kts/sec)", $"Violation Confort: Freinage brusque ({decelerationKnotsPerSec:F1} kts/sec)"));
                }


            }

            // Engine Failure Detection (between Takeoff and Landing)
            if ((CurrentPhase == FlightPhase.Takeoff || CurrentPhase == FlightPhase.InitialClimb || 
                 CurrentPhase == FlightPhase.Climb || CurrentPhase == FlightPhase.Cruise || 
                 CurrentPhase == FlightPhase.Descent || CurrentPhase == FlightPhase.Approach) && 
                 (!Eng1Combustion || !Eng2Combustion) && !_hasTriggeredEngineFailure)
            {
                _hasTriggeredEngineFailure = true;
                OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Critical Safety Violation: In-Flight Engine Failure", "Violation Sécurité Critique: Panne Moteur en vol"));
            }

            // Intelligent Rule-Based State Machine
            switch (CurrentPhase)
            {
                case FlightPhase.AtGate:
                case FlightPhase.Turnaround:
                    if (groundSpeed >= 0.5 && !isParkingBrakeSet) ChangePhase(FlightPhase.Pushback);
                    break;
                
                case FlightPhase.Pushback:
                    if (groundSpeed >= 8.0) ChangePhase(FlightPhase.TaxiOut);
                    break;

                case FlightPhase.TaxiOut:
                    if ((throttle >= 60.0 && groundSpeed >= 40.0) || groundSpeed > 60.0)
                    {
                        if (IsStrobeLightOn && (IsLandingLightOn || FenixNoseLight == 2) && (IsTaxiLightOn || FenixNoseLight >= 1))
                        {
                            OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Line-up Configuration Bonus: Strobes/Landing/Taxi ON", "Bonus Alignement: Phares Strobes/Landing/Taxi ALLUMÉS"));
                        }
                        else
                        {
                            OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Safety Violation: Poor Line-up Configuration (Missing Lights)", "Violation Sécurité: Configuration d'alignement incorrecte (Phares manquants)"));
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
                    // 10,000ft Climb Flow Bonus
                    if (altitude >= 10000 && altitude <= 12000 && !_hasTriggered10kClimbBonus)
                    {
                        if (!IsLandingLightOn && FenixNoseLight == 0 && !IsSeatbeltsOn)
                        {
                            _hasTriggered10kClimbBonus = true;
                            OnPenaltyTriggered?.Invoke(LocalizationService.Translate("10,000ft Climb Flow Complete (+50)", "Procédure de montée 10,000ft Complète (+50)"));
                        }
                    }

                    // If within 500ft of target cruise altitude and leveling off (VS < 500 fpm)
                    if (altitude >= TargetCruiseAltitude - 500 && Math.Abs(VerticalSpeed) < 500) 
                    {
                        ChangePhase(FlightPhase.Cruise);
                        TargetCruiseAltitude = altitude; // Dynamically adjust if we leveled off higher/lower
                    }
                    // Critical Bug Fix: Override descent if target cruise altitude was never reached (SimBrief vs actual FL)
                    else if (VerticalSpeed < -500 && altitude < _highestAltitudeReached - 3000)
                    {
                        ChangePhase(FlightPhase.Descent);
                    }
                    else if (altitude < 10000 && altitude > 5000 && TargetCruiseAltitude > 10000 && VerticalSpeed < -500) // Fallback
                    {
                        ChangePhase(FlightPhase.Descent);
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

                    if (IsOnGround)
                    {
                        IsGoAroundActive = false;
                        _goAroundStartTime = null;
                        if (_vsHistory.Count > 0)
                        {
                            // On prend la moyenne des dernières frames avant le contact sol (radioHeight > 2.0)
                            TouchdownFpm = _vsHistory.Average(); 
                        }
                        else
                        {
                            TouchdownFpm = VerticalSpeed;
                        }
                        
                        ChangePhase(FlightPhase.Landing);
                    }
                    else
                    {
                        // Detect Go-Around: Positive VS during approach
                        if (VerticalSpeed > 700 && radioHeight < 2000)
                        {
                            if (_goAroundStartTime == null) _goAroundStartTime = DateTime.Now;
                            else if ((DateTime.Now - _goAroundStartTime.Value).TotalSeconds > 3)
                            {
                                IsGoAroundActive = true;
                            }
                        }
                    }
                    break;
                case FlightPhase.Landing:
                    if (IsOnGround && !_hasLanded)
                    {
                        _hasLanded = true;
                        TouchdownGForce = GForce;
                        string landingQualityEn = "Normal Landing";
                        string landingQualityFr = "Atterrissage Normal";
                        if (TouchdownFpm > -150) { landingQualityEn = "Butter Landing"; landingQualityFr = "Kiss Landing"; }
                        else if (TouchdownFpm < -600) { landingQualityEn = "Severe Hard Landing"; landingQualityFr = "Atterrissage Très Dur"; }
                        else if (TouchdownFpm < -450) { landingQualityEn = "Hard Landing"; landingQualityFr = "Atterrissage Dur"; }
                        
                        OnPenaltyTriggered?.Invoke(LocalizationService.Translate(
                            $"{landingQualityEn}: Touchdown at {TouchdownFpm:F0} fpm ({TouchdownGForce:F2}G)",
                            $"{landingQualityFr}: Posé à {TouchdownFpm:F0} fpm ({TouchdownGForce:F2}G)"
                        ));

                        // Touchdown Zone Time Evaluation
                        if (_timeAt50Ft.HasValue)
                        {
                            double flareSeconds = (DateTime.Now - _timeAt50Ft.Value).TotalSeconds;
                            double minFlare = 4.0;
                            double maxFlare = 7.0;
                            
                            if (AircraftCategory == "Heavy") { minFlare = 5.0; maxFlare = 9.0; }
                            else if (AircraftCategory == "Light") { minFlare = 3.0; maxFlare = 6.0; }

                            if (flareSeconds < minFlare) {
                                OnPenaltyTriggered?.Invoke(LocalizationService.Translate(
                                    $"Short Flare (-100): Touchdown too early {flareSeconds:F1}s (Ideal: {minFlare}-{maxFlare}s)",
                                    $"Arrondi Court (-100): Posé trop tôt {flareSeconds:F1}s (Idéal: {minFlare}-{maxFlare}s)"
                                ));
                            } 
                            else if (flareSeconds > maxFlare) {
                                OnPenaltyTriggered?.Invoke(LocalizationService.Translate(
                                    $"Long Flare (-100): Touchdown too late {flareSeconds:F1}s (Ideal: {minFlare}-{maxFlare}s)",
                                    $"Arrondi Long (-100): Posé trop tard {flareSeconds:F1}s (Idéal: {minFlare}-{maxFlare}s)"
                                ));
                            }
                            else {
                                OnPenaltyTriggered?.Invoke(LocalizationService.Translate(
                                    $"Perfect Flare (+50): {flareSeconds:F1}s exactly in the ideal zone!",
                                    $"Arrondi Parfait (+50): {flareSeconds:F1}s dans la zone idéale !"
                                ));
                            }
                        }

                        // Centerline logic
                        double dev = 0.0;
                        string devSource = "";
                        if (HasLocalizer)
                        {
                            dev = Math.Abs(NavLocalizerError); // in degrees
                            devSource = "ILS Localizer";
                            if (dev > 1.0) OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Centerline Deviation (-100): {dev:F2}° off-center ({devSource}) !", $"Déviation Axe (-100): {dev:F2}° d'écart ({devSource}) !"));
                            else OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Perfect Centerline (+50): {dev:F2}° exactly on center ({devSource})", $"Axe Parfait (+50): {dev:F2}° sur l'axe ({devSource})"));
                        }
                        else 
                        {
                            dev = Math.Abs(GpsCrossTrackError); // in meters
                            devSource = "GPS Track";
                            if (dev > 25.0) OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Centerline Deviation (-100): {dev:F0}m off-center ({devSource}) !", $"Déviation Axe (-100): {dev:F0}m d'écart ({devSource}) !"));
                            else OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Perfect Centerline (+50): {dev:F0}m exactly on center ({devSource})", $"Axe Parfait (+50): {dev:F0}m sur l'axe ({devSource})"));
                        }
                        
                        // Override Crosswind Bonus if dev is bad
                        bool goodCenterline = (HasLocalizer && dev <= 1.0) || (!HasLocalizer && dev <= 25.0);

                        // Crosswind Bonus Calculation
                        double angleRad = (WindDirection - Heading) * Math.PI / 180.0;
                        double crosswind = WindVelocity * Math.Abs(Math.Sin(angleRad));
                        double headwind = WindVelocity * Math.Cos(angleRad);

                        if (goodCenterline)
                        {
                            if (crosswind > 25.0)
                            {
                                OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Extreme Crosswind Landing (+150): {crosswind:F0} kts crosswind neutralized!", $"Atterrissage Vent de Travers Extrême (+150): {crosswind:F0} kts maîtrisés !"));
                            }
                            else if (crosswind > 20.0)
                            {
                                OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Great Crosswind Landing (+100): {crosswind:F0} kts crosswind neutralized!", $"Atterrissage Vent de Travers Fort (+100): {crosswind:F0} kts maîtrisés !"));
                            }
                            else if (crosswind > 15.0)
                            {
                                OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Nice Crosswind Landing (+50): {crosswind:F0} kts crosswind neutralized!", $"Bon Atterrissage Vent de Travers (+50): {crosswind:F0} kts maîtrisés !"));
                            }
                            else if (headwind >= 20.0)
                            {
                                OnPenaltyTriggered?.Invoke(LocalizationService.Translate($"Strong Headwind Landing (+50): {headwind:F0} kts headwind neutralized!", $"Fort Vent de Face (+50): {headwind:F0} kts maîtrisés !"));
                            }
                        }
                        else if (crosswind > 15.0 || headwind >= 20.0)
                        {
                            OnPenaltyTriggered?.Invoke(LocalizationService.Translate("Crosswind/Headwind Bonus Cancelled: Centerline not maintained.", "Bonus Vent Annulé : Axe non maintenu."));
                        }

                        // Manual Flying Airmanship Bonus
                        if (ManualFlyingSecondsApproach >= 120)
                        {
                            OnPenaltyTriggered?.Invoke(LocalizationService.Translate(
                                $"True Airmanship (+200): Flown manually for {ManualFlyingSecondsApproach}s until touchdown!", 
                                $"Pilotage Manuel Magistral (+200): Volé en manuel pendant {ManualFlyingSecondsApproach}s avant l'atterrissage !"));
                        }
                        else if (ManualFlyingSecondsApproach >= 60)
                        {
                            OnPenaltyTriggered?.Invoke(LocalizationService.Translate(
                                $"Good Airmanship (+100): Flown manually for {ManualFlyingSecondsApproach}s until touchdown!", 
                                $"Bon Pilotage Manuel (+100): Volé en manuel pendant {ManualFlyingSecondsApproach}s avant l'atterrissage !"));
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

        public void ForcePhase(FlightPhase newPhase)
        {
            ChangePhase(newPhase);
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
                
                if (CurrentPhase == FlightPhase.AtGate || CurrentPhase == FlightPhase.Turnaround || CurrentPhase == FlightPhase.Pushback)
                {
                    _hasLanded = false;
                    _hasTriggeredTaxiPenalty = false;
                    _hasTriggeredGearLate = false;
                    _hasTriggeredGearOverspeed = false;
                    _hasTriggeredGearUpBonus = false;
                    _hasTriggeredAbnormalGear = false;
                    _hasTriggeredEngineFailure = false;
                    
                    _hasFoWarnedGearUp = false;
                    _hasFoWarnedGearDown = false;
                    _hasFoWarnedSeatbeltTaxi = false;
                    _hasFoWarnedSeatbelt10k = false;
                    _hasFoWarnedSeatbeltDesc = false;
                    _hasFoWarnedLights10kClimb = false;
                    _hasFoWarnedLights10kDesc = false;
                }
                else if (CurrentPhase == FlightPhase.Descent || CurrentPhase == FlightPhase.Approach)
                {
                    _hasTriggeredOverspeedPenalty = false;
                }

                if (CurrentPhase == FlightPhase.AtGate || CurrentPhase == FlightPhase.Turnaround)
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

        private void CalculateTurbulence(double gf)
        {
            if (IsOnGround)
            {
                _gForceHistory.Clear();
                TurbulenceSeverity = TurbulenceSeverityLevel.None;
                return;
            }

            _gForceHistory.Enqueue(gf);
            if (_gForceHistory.Count > JitterWindowSize) _gForceHistory.Dequeue();
            if (_gForceHistory.Count < 5) return;

            double maxDev = 0;
            int crossings = 0;
            double? prev = null;

            foreach (var val in _gForceHistory)
            {
                double dev = Math.Abs(val - 1.0);
                if (dev > maxDev) maxDev = dev;
                
                if (prev.HasValue)
                {
                    if ((prev.Value < 1.0 && val >= 1.0) || (prev.Value > 1.0 && val <= 1.0))
                        crossings++;
                }
                prev = val;
            }

            // High frequency jitter detection: crossing the 1.0G line multiple times.
            // If Autopilot is ON, any deviation is environmental.
            // If Autopilot is OFF, we require jitter to classify as "Weather Turbulence" vs "Pilot Input".
            bool isJitter = crossings >= 2; 
            bool isPilotInput = PitchRate > 5.0 || BankRate > 10.0; // Rapid manual control changes

            if (!isPilotInput && (_isAutopilotActive || isJitter))
            {
                if (maxDev < 0.15) TurbulenceSeverity = TurbulenceSeverityLevel.None;
                else if (maxDev < 0.30) TurbulenceSeverity = TurbulenceSeverityLevel.Light;
                else if (maxDev < 0.45) TurbulenceSeverity = TurbulenceSeverityLevel.Moderate;
                else if (maxDev < 0.7) TurbulenceSeverity = TurbulenceSeverityLevel.Severe;
                else TurbulenceSeverity = TurbulenceSeverityLevel.Extreme;
            }
            else
            {
                TurbulenceSeverity = TurbulenceSeverityLevel.None;
            }
        }

        public void Reset(bool isTurnaround = false)
        {
            ChangePhase(isTurnaround ? FlightPhase.Turnaround : FlightPhase.AtGate);
            _highestAltitudeReached = 0;
            TouchdownFpm = 0.0;
            TouchdownGForce = 1.0;
            _hasLanded = false;
            IsOnGround = true;
            _timeAt50Ft = null;
            _vsHistory.Clear();
            _gForceHistory.Clear();
            TurbulenceSeverity = TurbulenceSeverityLevel.None;
        }
    }
}
