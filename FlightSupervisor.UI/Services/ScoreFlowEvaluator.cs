using System;
using System.Collections.Generic;

namespace FlightSupervisor.UI.Services
{
    public class ScoreFlowEvaluator
    {
        private readonly FlowTrackerService _flowTracker;
        private readonly FlightPhaseManager _phaseManager;
        private readonly SuperScoreManager _scoreManager;
        private readonly CabinManager _cabinManager;

        public ScoreFlowEvaluator(FlowTrackerService flowTracker, FlightPhaseManager phaseManager, SuperScoreManager scoreManager, CabinManager cabinManager)
        {
            _flowTracker = flowTracker;
            _phaseManager = phaseManager;
            _scoreManager = scoreManager;
            _cabinManager = cabinManager;
            // Re-subscribe here or let MainWindow inject? We hook up event.
            _phaseManager.OnPhaseEnding += PhaseManager_OnPhaseEnding;
            _phaseManager.OnGoAroundFinished += PhaseManager_OnGoAroundFinished;
        }

        private void PhaseManager_OnGoAroundFinished()
        {
            if (!_cabinManager.HasAnnouncedGoAroundPA)
            {
                _scoreManager.AddScore(-200, "Missed PA: Failed to brief passengers during Go-Around", ScoreCategory.Communication);
            }
        }

        private void PhaseManager_OnPhaseEnding(FlightPhase endingPhase)
        {
            // Evaluate generic phase flows (Category 1)
            EvaluateFlightPhaseFlow(endingPhase);

            // Evaluate continuous violations first
            EvaluateContinuousViolations(endingPhase);
            
            // Evaluate communications
            EvaluateCommunications(endingPhase);
            
            // Check Critical Errors
            if (_flowTracker.HasTailStrikeInPhase)
            {
                _scoreManager.AddScore(-5000, $"TAIL STRIKE DURING {endingPhase.ToString().ToUpper()}", ScoreCategory.Maintenance);
            }

            if (_flowTracker.HasHardLandingInPhase)
            {
                _scoreManager.AddScore(-2000, $"HARD LANDING DETECTED (>2.5G)", ScoreCategory.Maintenance);
            }

            // Once evaluated, reset the trackers for the new phase.
            _flowTracker.ResetPhaseTrackers();
        }

        private void EvaluateContinuousViolations(FlightPhase phase)
        {
            // Taxi Speed (AirmanShip)
            if (_flowTracker.TotalTaxiSpeedViolations > 0)
            {
                // Penalty formulation: 50 points per violation + 2 points per second overspeeding
                int penalty = -(_flowTracker.TotalTaxiSpeedViolations * 50 + (int)(_flowTracker.AccumulatedTaxiSpeedViolationSeconds * 2));
                _scoreManager.AddScore(penalty, $"High speed taxi (>{_flowTracker.TotalTaxiSpeedViolations} instances)", ScoreCategory.Airmanship);
            }

            // Flaps Overspeed
            if (_flowTracker.TotalFlapsOverspeedViolations > 0)
            {
                int penalty = -(_flowTracker.TotalFlapsOverspeedViolations * 200 + (int)(_flowTracker.AccumulatedFlapsOverspeedViolationSeconds * 10));
                _scoreManager.AddScore(penalty, $"Flaps Overspeed (>{_flowTracker.TotalFlapsOverspeedViolations} instances)", ScoreCategory.Airmanship);
            }
        }

        private void EvaluateFlightPhaseFlow(FlightPhase phase)
        {
            List<string> errors = new List<string>();
            bool safetyViolation = false;

            // Helper checks
            bool strobeAuto = _flowTracker.StrobeLightState == 0 || _flowTracker.StrobeLightState == 1; // 0=OFF, 1=AUTO

            switch (phase)
            {
                case FlightPhase.AtGate:
                case FlightPhase.Turnaround:
                    if (!_flowTracker.IsThrustIdle) errors.Add("Thrust Lever not IDLE");
                    if (!_flowTracker.AreWipersOff) errors.Add("Wipers left ON");
                    if (_flowTracker.FlapsIndex > 0) errors.Add("Flaps not ZERO");
                    if (!_flowTracker.AreSpoilersRetracted) errors.Add("GND Spoilers not RETRACTED");
                    if (_flowTracker.EngineMode != 2) errors.Add("Engine Mode not NORM");
                    if (_flowTracker.IsEngineMaster1On || _flowTracker.IsEngineMaster2On) errors.Add("Engines not OFF");
                    if (!_flowTracker.IsGearDown) errors.Add("Gear Lever not DOWN");
                    if (!strobeAuto) errors.Add("Strobe Light not AUTO");
                    if (_flowTracker.AreLandingLightsOn) errors.Add("Landing Lights left ON");
                    if (_flowTracker.TaxiLightState != 0) errors.Add("Taxi Lights left ON");
                    if (_flowTracker.IsRnwTurnoffOn) errors.Add("Rnw Turnoff left ON");
                    break;

                case FlightPhase.Pushback:
                    if (_flowTracker.StrobeLightState == 2) errors.Add("Strobe Light left ON");
                    if (_flowTracker.TaxiLightState != 0) errors.Add("Taxi Lights left ON");
                    if (!_flowTracker.AreSeatbeltsOn) { errors.Add("Seatbelts OFF during pushback"); safetyViolation = true; }
                    break;

                case FlightPhase.TaxiOut:
                    if (!_flowTracker.AreSeatbeltsOn) { errors.Add("Seatbelts OFF during Taxi"); safetyViolation = true; }
                    if (_flowTracker.TaxiLightState == 0) errors.Add("Taxi Lights OFF during Taxi");
                    if (!strobeAuto && _flowTracker.StrobeLightState != 1) errors.Add("Strobe should be AUTO");
                    if (_flowTracker.AreLandingLightsOn) errors.Add("Landing lights ON during taxi");
                    if (_flowTracker.FlapsIndex == 0) errors.Add("Flaps ZERO during taxi (invalid TO config)");
                    break;

                case FlightPhase.Takeoff:
                    if (!_flowTracker.AreLandingLightsOn) errors.Add("Landing lights OFF during Takeoff");
                    if (_flowTracker.StrobeLightState != 2) errors.Add("Strobe lights OFF during Takeoff (Must be ON)");
                    if (_flowTracker.TaxiLightState != 2) errors.Add("Taxi Lights not T.O."); // T.O is 2
                    if (!_flowTracker.AreSpoilersArmed) errors.Add("Spoilers not ARMED for Takeoff");
                    if (!_flowTracker.AreSeatbeltsOn) { errors.Add("Seatbelts OFF during Takeoff!"); safetyViolation = true; }
                    break;

                case FlightPhase.InitialClimb:
                case FlightPhase.Climb:
                    if (_flowTracker.IsGearDown) errors.Add("Gear left DOWN");
                    if (_flowTracker.TaxiLightState != 0) errors.Add("Taxi Lights left ON");
                    if (_flowTracker.IsRnwTurnoffOn) errors.Add("Rnw Turnoff left ON");
                    if (_flowTracker.FlapsIndex > 0) errors.Add("Flaps left EXTENDED");
                    if (!_flowTracker.AreSpoilersRetracted) errors.Add("Spoilers left EXTENDED");
                    break;

                case FlightPhase.Cruise:
                    if (_flowTracker.IsGearDown) errors.Add("Gear left DOWN in Cruise");
                    if (_flowTracker.TaxiLightState != 0) errors.Add("Taxi Lights left ON in Cruise");
                    if (_flowTracker.FlapsIndex > 0) errors.Add("Flaps left EXTENDED in Cruise");
                    if (_flowTracker.AreLandingLightsOn) errors.Add("Landing lights left ON in Cruise");
                    break;

                case FlightPhase.Descent:
                    if (!_flowTracker.AreSeatbeltsOn) errors.Add("Seatbelts OFF entering Descent");
                    // Landing lights checked externally by FlightPhaseManager rule for < 10,000ft
                    break;

                case FlightPhase.Approach:
                    if (!_flowTracker.IsGearDown) errors.Add("Gear UP during Approach");
                    if (_flowTracker.TaxiLightState != 2) errors.Add("Taxi Lights not T.O.");
                    if (!_flowTracker.IsRnwTurnoffOn) errors.Add("Rnw Turnoff OFF during Approach");
                    if (!_flowTracker.AreSpoilersArmed) errors.Add("Spoilers not ARMED for Approach");
                    if (_flowTracker.FlapsIndex == 0) errors.Add("Flaps ZERO during Approach");
                    break;

                case FlightPhase.Landing:
                    if (!_flowTracker.IsGearDown) errors.Add("Gear UP during Landing");
                    if (_flowTracker.TaxiLightState == 0) errors.Add("Taxi Lights OFF during Landing");
                    break;

                case FlightPhase.TaxiIn:
                    if (!_flowTracker.AreSeatbeltsOn) errors.Add("Seatbelts OFF during Taxi In");
                    if (_flowTracker.TaxiLightState == 0) errors.Add("Taxi Lights OFF during Taxi In");
                    if (!strobeAuto && _flowTracker.StrobeLightState != 1) errors.Add("Strobe should be AUTO");
                    if (_flowTracker.AreLandingLightsOn) errors.Add("Landing lights ON during taxi");
                    if (_flowTracker.FlapsIndex > 0) errors.Add("Flaps not retracted during taxi in");
                    break;

                case FlightPhase.Arrived:
                    if (_flowTracker.StrobeLightState == 2) errors.Add("Beacon/Strobe Light left ON at gate");
                    if (!_flowTracker.IsThrustIdle) errors.Add("Thrust Lever not IDLE");
                    if (!_flowTracker.AreWipersOff) errors.Add("Wipers left ON");
                    if (_flowTracker.FlapsIndex > 0) errors.Add("Flaps left EXTENDED");
                    if (_flowTracker.EngineMode != 2) errors.Add("Engine Mode not NORM");
                    if (_flowTracker.IsEngineMaster1On || _flowTracker.IsEngineMaster2On) errors.Add("Engines left ON");
                    if (!strobeAuto) errors.Add("Strobe Light not AUTO");
                    if (_flowTracker.AreLandingLightsOn) errors.Add("Landing Lights left ON");
                    if (_flowTracker.TaxiLightState != 0) errors.Add("Taxi Lights left ON");
                    break;
            }

            if (errors.Count > 0)
            {
                foreach (var err in errors)
                {
                    int pen = safetyViolation ? -200 : -20;
                    _scoreManager.AddScore(pen, $"SOP Violation ({phase}): {err}", ScoreCategory.FlightPhaseFlows);
                }
            }
            else
            {
                // Give a perfect flow bonus for specific critical phases
                if (phase != FlightPhase.Turnaround && phase != FlightPhase.AtGate && phase != FlightPhase.Arrived && phase != FlightPhase.Cruise)
                {
                    _scoreManager.AddScore(50, $"Perfect Flow: {phase}", ScoreCategory.FlightPhaseFlows);
                }
            }
        }

        private void EvaluateCommunications(FlightPhase endingPhase)
        {
            // --- COMMUNICATION AUDITS (Category 2) ---
            
            if (endingPhase == FlightPhase.TaxiOut)
            {
                // Has the crew prepared the cabin for takeoff? Checked when TaxiOut ends (meaning Takeoff begins)
                if (!_cabinManager.HasPlayedPrepareTakeoffPA)
                {
                    _scoreManager.AddScore(-400, "Safety Violation: Takeoff without explicit Cabin Preparation PA!", ScoreCategory.Communication);
                }

                // Did the captain welcome the passengers at the gate or taxi? Checked when TaxiOut ends
                if (_cabinManager.HasPlayedWelcomePA)
                {
                    _scoreManager.AddScore(30, "Welcome PA completed", ScoreCategory.Communication);
                }
            }
            
            if (endingPhase == FlightPhase.Descent)
            {
                // Did the captain announce descent while in Cruise/Descent? Checked when Descent ends (meaning Approach begins)
                if (_cabinManager.HasPlayedDescentPA)
                {
                    _scoreManager.AddScore(30, "Top of Descent PA completed", ScoreCategory.Communication);
                }
            }

            if (endingPhase == FlightPhase.Approach) 
            {
                // Has the crew prepared the cabin for landing? Checked when Approach ends (meaning Landing begins)
                if (!_cabinManager.HasPlayedPrepareLandingPA)
                {
                    _scoreManager.AddScore(-400, "Safety Violation: Landing approach without explicit Cabin Preparation PA!", ScoreCategory.Communication);
                }
            }

        }
    }
}
