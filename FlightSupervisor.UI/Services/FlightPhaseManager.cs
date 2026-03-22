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
        private int _taxiOverspeedSeconds = 0;
        private int _overspeedSeconds = 0;
        private double _highestAltitudeReached = 0;
        public double TargetCruiseAltitude { get; set; } = 10000;
        public double AccelerationAltitudeAgl { get; set; } = 1500; // Default NADP2 standard

        public void UpdateTelemetry(double groundSpeed, double indicatedAirspeed, double altitude, double radioHeight, bool isParkingBrakeSet, bool isGearDown)
        {
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

                // Gear deployed above 260 kts threshold
                if (isGearDown && indicatedAirspeed > 260.0 && !_hasTriggeredGearOverspeed)
                {
                    _hasTriggeredGearOverspeed = true;
                    OnPenaltyTriggered?.Invoke("Safety Violation: Landing Gear deployed above maximum extended speed (VLE > 260kts).");
                }

                // Gear forgotten on short final
                if (CurrentPhase == FlightPhase.Approach && radioHeight < 1000 && radioHeight > 50 && !isGearDown && groundSpeed > 50 && !_hasTriggeredGearLate)
                {
                    _hasTriggeredGearLate = true;
                    OnPenaltyTriggered?.Invoke("Safety: Unstable Approach (Gear Not Down below 1000ft AGL).");
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
                    if (groundSpeed >= 40.0) ChangePhase(FlightPhase.Takeoff);
                    break;
                
                case FlightPhase.Takeoff:
                    if (radioHeight >= 400) ChangePhase(FlightPhase.InitialClimb);
                    break;

                case FlightPhase.InitialClimb:
                    if (radioHeight >= AccelerationAltitudeAgl) ChangePhase(FlightPhase.Climb);
                    break;

                case FlightPhase.Climb:
                    // If within 500ft of target cruise altitude, we are cruising
                    if (altitude >= TargetCruiseAltitude - 500) ChangePhase(FlightPhase.Cruise);
                    break;

                case FlightPhase.Cruise:
                    // Detect descent if we drop 1500ft below the stabilized cruise altitude
                    if (_highestAltitudeReached > 10000 && altitude < (_highestAltitudeReached - 1500)) 
                    {
                        ChangePhase(FlightPhase.Descent);
                    }
                    else if (altitude < 10000 && altitude > 5000) // Fallback
                    {
                        ChangePhase(FlightPhase.Descent);
                    }
                    break;
                
                case FlightPhase.Descent:
                    if (radioHeight > 0 && radioHeight < 4000) ChangePhase(FlightPhase.Approach);
                    break;
                
                case FlightPhase.Approach:
                    // Touchdown transition based on Radio Altimeter
                    if (radioHeight > 0 && radioHeight <= 50 && groundSpeed < 170) ChangePhase(FlightPhase.Landing);
                    break;
                
                case FlightPhase.Landing:
                    if (groundSpeed < 40.0) ChangePhase(FlightPhase.TaxiIn);
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
                
                if (CurrentPhase == FlightPhase.Descent || CurrentPhase == FlightPhase.Approach)
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
    }
}
