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

            System.Text.StringBuilder logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine($"\n[SCORE_FLOW] === Phase Ended: {phase.ToString().ToUpper()} ===");

            void CheckRule(string itemName, bool condition, string expectedStr, string failMessage, bool isSafety = false)
            {
                if (condition)
                {
                    logBuilder.AppendLine($"[SCORE_FLOW] - {itemName}: {expectedStr} -> PASS");
                }
                else
                {
                    logBuilder.AppendLine($"[SCORE_FLOW] - {itemName}: {expectedStr} -> FAIL");
                    errors.Add(failMessage);
                    if (isSafety) safetyViolation = true;
                }
            }

            switch (phase)
            {
                case FlightPhase.AtGate:
                case FlightPhase.Turnaround:
                    CheckRule("Parking brakes", _flowTracker.IsParkingBrakeOn, "ON", "Parking brakes not ON");
                    CheckRule("Thrust Lever", _flowTracker.IsThrustIdle, "IDLE", "Thrust Lever not IDLE");
                    CheckRule("Whipers (both)", _flowTracker.AreWipersOff, "OFF", "Wipers left ON");
                    CheckRule("Flaps", _flowTracker.FlapsIndex == 0, "ZERO", "Flaps not ZERO");
                    CheckRule("GND Spoilers", _flowTracker.AreSpoilersRetracted, "RETRACTED", "GND Spoilers not RETRACTED");
                    CheckRule("Engine mode", _flowTracker.EngineMode == 1, "NORM", "Engine Mode not NORM");
                    CheckRule("Engine Master", !_flowTracker.IsEngineMaster1On && !_flowTracker.IsEngineMaster2On, "OFF", "Engines not OFF");
                    CheckRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear Lever not DOWN");
                    CheckRule("Strobe light", strobeAuto, "AUTO", "Strobe Light not AUTO");
                    CheckRule("Landing lights", !_flowTracker.AreLandingLightsOn, "OFF/RETRACTED", "Landing Lights left ON");
                    CheckRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON");
                    CheckRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    if (_flowTracker.IsRefueling)
                        CheckRule("Seat belt", !_flowTracker.AreSeatbeltsOn, "OFF (Refueling)", "Seatbelts ON during Refueling!", true);
                    else
                        CheckRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF at Gate");
                    break;

                case FlightPhase.Pushback:
                    CheckRule("Beacon light", _flowTracker.IsBeaconLightOn, "ON", "Beacon Light OFF during Pushback", true);
                    CheckRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF during pushback", true);
                    CheckRule("Thrust Lever", _flowTracker.IsThrustIdle, "IDLE", "Thrust Lever not IDLE");
                    CheckRule("Whipers (both)", _flowTracker.AreWipersOff, "OFF", "Wipers left ON");
                    CheckRule("Flaps", _flowTracker.FlapsIndex == 0, "ZERO", "Flaps not ZERO");
                    CheckRule("GND Spoilers", _flowTracker.AreSpoilersRetracted, "RETRACTED", "GND Spoilers not RETRACTED");
                    CheckRule("Parking brakes", _flowTracker.IsParkingBrakeOn, "ON", "Parking brakes not ON");
                    CheckRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear Lever not DOWN");
                    CheckRule("Strobe light", strobeAuto, "AUTO", "Strobe Light not AUTO");
                    CheckRule("Landing lights", !_flowTracker.AreLandingLightsOn, "OFF/RETRACTED", "Landing Lights left ON");
                    CheckRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON");
                    CheckRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    break;

                case FlightPhase.TaxiOut:
                    CheckRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF during Taxi", true);
                    CheckRule("Taxi lights", _flowTracker.TaxiLightState != 0, "ON", "Taxi Lights OFF during Taxi");
                    CheckRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear Lever not DOWN");
                    CheckRule("Strobe light", strobeAuto, "AUTO", "Strobe should be AUTO");
                    CheckRule("Landing lights", !_flowTracker.AreLandingLightsOn, "OFF/RETRACTED", "Landing lights ON during taxi");
                    CheckRule("Flaps", _flowTracker.FlapsIndex > 0 && _flowTracker.FlapsIndex < 4, "1, 2 or 3", "Flaps ZERO or FULL during taxi (invalid TO config)");
                    CheckRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    break;

                case FlightPhase.Takeoff:
                    CheckRule("Landing lights", _flowTracker.AreLandingLightsOn, "ON", "Landing lights OFF during Takeoff");
                    CheckRule("Strobe light", _flowTracker.StrobeLightState == 2, "ON", "Strobe lights OFF during Takeoff (Must be ON)");
                    CheckRule("Taxi lights", _flowTracker.TaxiLightState == 2, "TO", "Taxi Lights not T.O.");
                    CheckRule("Rnw Turnoff", _flowTracker.IsRnwTurnoffOn, "ON", "Rnw Turnoff OFF during Takeoff");
                    CheckRule("GND Spoilers", _flowTracker.AreSpoilersArmed, "ARMED", "Spoilers not ARMED for Takeoff");
                    CheckRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF during Takeoff!", true);
                    break;

                case FlightPhase.InitialClimb:
                    CheckRule("Gear Lever", !_flowTracker.IsGearDown, "UP", "Gear left DOWN");
                    CheckRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON");
                    CheckRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    CheckRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF during Initial Climb", true);
                    break;

                case FlightPhase.Climb:
                    CheckRule("Gear Lever", !_flowTracker.IsGearDown, "UP", "Gear left DOWN");
                    CheckRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON");
                    CheckRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    CheckRule("Flaps", _flowTracker.FlapsIndex == 0, "ZERO", "Flaps left EXTENDED");
                    CheckRule("GND Spoilers", _flowTracker.AreSpoilersRetracted, "RETRACTED", "Spoilers left EXTENDED");
                    break;

                case FlightPhase.Cruise:
                    CheckRule("Gear Lever", !_flowTracker.IsGearDown, "UP", "Gear left DOWN in Cruise");
                    CheckRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON in Cruise");
                    CheckRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    CheckRule("Flaps", _flowTracker.FlapsIndex == 0, "ZERO", "Flaps left EXTENDED in Cruise");
                    CheckRule("GND Spoilers", _flowTracker.AreSpoilersRetracted, "RETRACTED", "Spoilers left EXTENDED in Cruise");
                    CheckRule("Landing lights", !_flowTracker.AreLandingLightsOn, "OFF/RETRACTED", "Landing lights left ON in Cruise");
                    break;

                case FlightPhase.Descent:
                    CheckRule("Gear Lever", !_flowTracker.IsGearDown, "UP", "Gear left DOWN");
                    CheckRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON");
                    CheckRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    CheckRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF entering Descent");
                    CheckRule("Landing lights", _flowTracker.AreLandingLightsOn, "ON", "Landing lights OFF entering Approach");
                    break;

                case FlightPhase.Approach:
                    CheckRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear UP during Approach");
                    CheckRule("Taxi lights", _flowTracker.TaxiLightState == 2, "TO", "Taxi Lights not T.O.");
                    CheckRule("Rnw Turnoff", _flowTracker.IsRnwTurnoffOn, "ON", "Rnw Turnoff OFF during Approach");
                    CheckRule("GND Spoilers", _flowTracker.AreSpoilersArmed, "ARMED", "Spoilers not ARMED for Approach");
                    CheckRule("Flaps", _flowTracker.FlapsIndex >= 3, "3 or Full", "Flaps not 3/Full during Approach");
                    break;

                case FlightPhase.Landing:
                    CheckRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear UP during Landing");
                    CheckRule("Taxi lights", _flowTracker.TaxiLightState != 0, "ON", "Taxi Lights OFF during Landing");
                    CheckRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff ON during Landing");
                    CheckRule("GND Spoilers", _flowTracker.AreSpoilersRetracted, "RETRACTED", "GND Spoilers left ARMED/EXT");
                    CheckRule("Strobe light", strobeAuto, "AUTO", "Strobe should be AUTO");
                    CheckRule("Landing lights", !_flowTracker.AreLandingLightsOn, "OFF/RETRACTED", "Landing lights ON during Landing");
                    break;

                case FlightPhase.TaxiIn:
                    CheckRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF during Taxi In", true);
                    CheckRule("Taxi lights", _flowTracker.TaxiLightState != 0, "ON", "Taxi Lights OFF during Taxi In");
                    CheckRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear UP during Taxi In");
                    CheckRule("Strobe light", strobeAuto, "AUTO", "Strobe should be AUTO");
                    CheckRule("Landing lights", !_flowTracker.AreLandingLightsOn, "OFF/RETRACTED", "Landing lights ON during taxi");
                    CheckRule("Flaps", _flowTracker.FlapsIndex == 0, "ZERO", "Flaps not retracted during taxi in");
                    CheckRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff ON during Taxi In");
                    break;

                case FlightPhase.Arrived:
                    CheckRule("Beacon light", !_flowTracker.IsBeaconLightOn, "OFF", "Beacon Light left ON at gate");
                    CheckRule("Thrust Lever", _flowTracker.IsThrustIdle, "IDLE", "Thrust Lever not IDLE");
                    CheckRule("Whipers (both)", _flowTracker.AreWipersOff, "OFF", "Wipers left ON");
                    CheckRule("Flaps", _flowTracker.FlapsIndex == 0, "ZERO", "Flaps left EXTENDED");
                    CheckRule("GND Spoilers", _flowTracker.AreSpoilersRetracted, "RETRACTED", "Spoilers not RETRACTED");
                    CheckRule("Engine mode", _flowTracker.EngineMode == 1, "NORM", "Engine Mode not NORM");
                    CheckRule("Engine Master", !_flowTracker.IsEngineMaster1On && !_flowTracker.IsEngineMaster2On, "OFF", "Engines left ON");
                    CheckRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear Lever not DOWN");
                    CheckRule("Strobe light", strobeAuto, "AUTO", "Strobe Light not AUTO");
                    CheckRule("Landing lights", !_flowTracker.AreLandingLightsOn, "OFF/RETRACTED", "Landing Lights left ON");
                    CheckRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON");
                    CheckRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    CheckRule("Seat belt", !_flowTracker.AreSeatbeltsOn, "OFF", "Seatbelts left ON");
                    break;
            }

            logBuilder.AppendLine($"[SCORE_FLOW] -> Total Violations: {errors.Count}");
            logBuilder.AppendLine("[SCORE_FLOW] =======================================\n");
            
            try
            {
                System.IO.File.AppendAllText("sync_debug.txt", logBuilder.ToString());
                System.Diagnostics.Debug.WriteLine(logBuilder.ToString());
            }
            catch { }

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
