using System;
using System.Collections.Generic;

namespace FlightSupervisor.UI.Services
{
    public class FlowRule
    {
        public string ItemName { get; set; }
        public string Expected { get; set; }
        public string Actual { get; set; }
        public bool IsPass { get; set; }
        public string FailMessage { get; set; }
        public bool IsSafety { get; set; }
    }

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

        public List<FlowRule> GetRulesForPhase(FlightPhase phase)
        {
            var rules = new List<FlowRule>();
            bool strobeAuto = _flowTracker.StrobeLightState == 0 || _flowTracker.StrobeLightState == 1; // 0=OFF, 1=AUTO

            void AddRule(string itemName, bool condition, string expectedStr, string failMessage, bool isSafety = false, string actualStr = null)
            {
                rules.Add(new FlowRule
                {
                    ItemName = itemName,
                    Expected = expectedStr,
                    Actual = actualStr,
                    IsPass = condition,
                    FailMessage = failMessage,
                    IsSafety = isSafety
                });
            }

            switch (phase)
            {
                case FlightPhase.AtGate:
                case FlightPhase.Turnaround:
                    AddRule("Parking brakes", _flowTracker.IsParkingBrakeOn, "ON", "Parking brakes not ON");
                    AddRule("Thrust Lever", _flowTracker.IsThrustIdle, "IDLE", "Thrust Lever not IDLE");
                    AddRule("Whipers (both)", _flowTracker.AreWipersOff, "OFF", "Wipers left ON");
                    AddRule("Flaps", _flowTracker.FlapsIndex == 0, "ZERO", "Flaps not ZERO");
                    AddRule("GND Spoilers", _flowTracker.AreSpoilersRetracted, "RETRACTED", "GND Spoilers not RETRACTED", false, _flowTracker.SpoilerStateText);
                    AddRule("Engine mode", _flowTracker.EngineMode == 1, "NORM", "Engine Mode not NORM");
                    AddRule("Engine Master", !_flowTracker.IsEngineMaster1On && !_flowTracker.IsEngineMaster2On, "OFF", "Engines not OFF", false, _flowTracker.IsEngineMaster1On || _flowTracker.IsEngineMaster2On ? "ON" : "OFF");
                    AddRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear Lever not DOWN", false, _flowTracker.IsGearDown ? "DOWN" : "UP");
                    AddRule("Strobe light", strobeAuto, "AUTO", "Strobe Light not AUTO");
                    AddRule("Landing lights", !_flowTracker.AreLandingLightsOn, "OFF/RETRACTED", "Landing Lights left ON");
                    AddRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON");
                    AddRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    if (_flowTracker.IsRefueling)
                        AddRule("Seat belt", !_flowTracker.AreSeatbeltsOn, "OFF", "Seatbelts ON during Refueling!", true);
                    else
                        AddRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF at Gate");
                    break;

                case FlightPhase.Pushback:
                    AddRule("Beacon light", _flowTracker.IsBeaconLightOn, "ON", "Beacon Light OFF during Pushback", true);
                    AddRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF during pushback", true);
                    AddRule("Thrust Lever", _flowTracker.IsThrustIdle, "IDLE", "Thrust Lever not IDLE");
                    AddRule("Whipers (both)", _flowTracker.AreWipersOff, "OFF", "Wipers left ON");
                    AddRule("Flaps", _flowTracker.FlapsIndex == 0, "ZERO", "Flaps not ZERO");
                    AddRule("GND Spoilers", _flowTracker.AreSpoilersRetracted, "RETRACTED", "GND Spoilers not RETRACTED", false, _flowTracker.SpoilerStateText);
                    AddRule("Parking brakes", _flowTracker.IsParkingBrakeOn, "ON", "Parking brakes not ON", false, _flowTracker.IsParkingBrakeOn ? "ON" : "OFF");
                    AddRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear Lever not DOWN", false, _flowTracker.IsGearDown ? "DOWN" : "UP");
                    AddRule("Strobe light", strobeAuto, "AUTO", "Strobe Light not AUTO");
                    AddRule("Landing lights", !_flowTracker.AreLandingLightsOn, "OFF/RETRACTED", "Landing Lights left ON");
                    AddRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON");
                    AddRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    break;

                case FlightPhase.TaxiOut:
                    AddRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF during Taxi", true);
                    AddRule("Taxi lights", _flowTracker.TaxiLightState != 0, "ON", "Taxi Lights OFF during Taxi");
                    AddRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear Lever not DOWN", false, _flowTracker.IsGearDown ? "DOWN" : "UP");
                    AddRule("Strobe light", strobeAuto, "AUTO", "Strobe should be AUTO");
                    AddRule("Landing lights", !_flowTracker.AreLandingLightsOn, "OFF/RETRACTED", "Landing lights ON during taxi");
                    AddRule("Flaps", _flowTracker.FlapsIndex > 0 && _flowTracker.FlapsIndex < 4, "1, 2 or 3", "Flaps ZERO or FULL during taxi (invalid TO config)");
                    AddRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    break;

                case FlightPhase.Takeoff:
                    AddRule("Landing lights", _flowTracker.AreLandingLightsOn, "ON", "Landing lights OFF during Takeoff");
                    AddRule("Strobe light", _flowTracker.StrobeLightState == 2, "ON", "Strobe lights OFF during Takeoff (Must be ON)");
                    AddRule("Taxi lights", _flowTracker.TaxiLightState == 2, "TO", "Taxi Lights not T.O.");
                    AddRule("Rnw Turnoff", _flowTracker.IsRnwTurnoffOn, "ON", "Rnw Turnoff OFF during Takeoff");
                    AddRule("GND Spoilers", _flowTracker.AreSpoilersArmed, "ARMED", "Spoilers not ARMED for Takeoff", false, _flowTracker.SpoilerStateText);
                    AddRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF during Takeoff!", true);
                    break;

                case FlightPhase.InitialClimb:
                    AddRule("Gear Lever", !_flowTracker.IsGearDown, "UP", "Gear left DOWN", false, _flowTracker.IsGearDown ? "DOWN" : "UP");
                    AddRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON");
                    AddRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    AddRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF during Initial Climb", true);
                    break;

                case FlightPhase.Climb:
                    AddRule("Gear Lever", !_flowTracker.IsGearDown, "UP", "Gear left DOWN", false, _flowTracker.IsGearDown ? "DOWN" : "UP");
                    AddRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON");
                    AddRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    AddRule("Flaps", _flowTracker.FlapsIndex == 0, "ZERO", "Flaps left EXTENDED");
                    AddRule("GND Spoilers", _flowTracker.AreSpoilersRetracted, "RETRACTED", "Spoilers left EXTENDED", false, _flowTracker.SpoilerStateText);
                    break;

                case FlightPhase.Cruise:
                    AddRule("Gear Lever", !_flowTracker.IsGearDown, "UP", "Gear left DOWN in Cruise", false, _flowTracker.IsGearDown ? "DOWN" : "UP");
                    AddRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON in Cruise");
                    AddRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    AddRule("Flaps", _flowTracker.FlapsIndex == 0, "ZERO", "Flaps left EXTENDED in Cruise");
                    AddRule("GND Spoilers", _flowTracker.AreSpoilersRetracted, "RETRACTED", "Spoilers left EXTENDED in Cruise", false, _flowTracker.SpoilerStateText);
                    AddRule("Landing lights", !_flowTracker.AreLandingLightsOn, "OFF/RETRACTED", "Landing lights left ON in Cruise");
                    break;

                case FlightPhase.Descent:
                    AddRule("Gear Lever", !_flowTracker.IsGearDown, "UP", "Gear left DOWN", false, _flowTracker.IsGearDown ? "DOWN" : "UP");
                    AddRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON");
                    AddRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    AddRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF entering Descent");
                    AddRule("Landing lights", _flowTracker.AreLandingLightsOn, "ON", "Landing lights OFF entering Approach");
                    break;

                case FlightPhase.Approach:
                    AddRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear UP during Approach", false, _flowTracker.IsGearDown ? "DOWN" : "UP");
                    AddRule("Taxi lights", _flowTracker.TaxiLightState == 2, "TO", "Taxi Lights not T.O.");
                    AddRule("Rnw Turnoff", _flowTracker.IsRnwTurnoffOn, "ON", "Rnw Turnoff OFF during Approach");
                    AddRule("Landing lights", _flowTracker.AreLandingLightsOn, "ON", "Landing lights OFF during Approach");
                    AddRule("GND Spoilers", _flowTracker.AreSpoilersArmed, "ARMED", "Spoilers not ARMED for Approach", false, _flowTracker.SpoilerStateText);
                    AddRule("Flaps", _flowTracker.FlapsIndex >= 3, "3 or Full", "Flaps not 3/Full during Approach");
                    break;

                case FlightPhase.Landing:
                    AddRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear UP during Landing", false, _flowTracker.IsGearDown ? "DOWN" : "UP");
                    AddRule("Taxi lights", _flowTracker.TaxiLightState != 0, "ON", "Taxi Lights OFF during Landing");
                    AddRule("Rnw Turnoff", _flowTracker.IsRnwTurnoffOn, "ON", "Rnw Turnoff OFF during Landing");
                    AddRule("GND Spoilers", !_flowTracker.AreSpoilersRetracted, "DEPLOYED/ARMED", "GND Spoilers left RETRACTED", false, _flowTracker.SpoilerStateText);
                    AddRule("Strobe light", _flowTracker.StrobeLightState == 2, "ON", "Strobe OFF during Landing");
                    AddRule("Landing lights", _flowTracker.AreLandingLightsOn, "ON", "Landing lights OFF during Landing");
                    break;

                case FlightPhase.TaxiIn:
                    AddRule("Seat belt", _flowTracker.AreSeatbeltsOn, "ON", "Seatbelts OFF during Taxi In", true);
                    AddRule("Taxi lights", _flowTracker.TaxiLightState != 0, "ON", "Taxi Lights OFF during Taxi In");
                    AddRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear UP during Taxi In", false, _flowTracker.IsGearDown ? "DOWN" : "UP");
                    AddRule("Strobe light", strobeAuto, "AUTO", "Strobe should be AUTO");
                    AddRule("Landing lights", !_flowTracker.AreLandingLightsOn, "OFF/RETRACTED", "Landing lights ON during taxi");
                    AddRule("GND Spoilers", _flowTracker.AreSpoilersRetracted, "RETRACTED", "Spoilers left ARMED/EXT", false, _flowTracker.SpoilerStateText);
                    AddRule("Flaps", _flowTracker.FlapsIndex == 0, "ZERO", "Flaps not retracted during taxi in");
                    AddRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff ON during Taxi In");
                    break;

                case FlightPhase.Arrived:
                    AddRule("Beacon light", !_flowTracker.IsBeaconLightOn, "OFF", "Beacon Light left ON at gate");
                    AddRule("Thrust Lever", _flowTracker.IsThrustIdle, "IDLE", "Thrust Lever not IDLE");
                    AddRule("Whipers (both)", _flowTracker.AreWipersOff, "OFF", "Wipers left ON");
                    AddRule("Flaps", _flowTracker.FlapsIndex == 0, "ZERO", "Flaps left EXTENDED");
                    AddRule("GND Spoilers", _flowTracker.AreSpoilersRetracted, "RETRACTED", "Spoilers not RETRACTED", false, _flowTracker.SpoilerStateText);
                    AddRule("Engine mode", _flowTracker.EngineMode == 1, "NORM", "Engine Mode not NORM");
                    AddRule("Engine Master", !_flowTracker.IsEngineMaster1On && !_flowTracker.IsEngineMaster2On, "OFF", "Engines left ON", false, _flowTracker.IsEngineMaster1On || _flowTracker.IsEngineMaster2On ? "ON" : "OFF");
                    AddRule("Gear Lever", _flowTracker.IsGearDown, "DOWN", "Gear Lever not DOWN", false, _flowTracker.IsGearDown ? "DOWN" : "UP");
                    AddRule("Strobe light", strobeAuto, "AUTO", "Strobe Light not AUTO");
                    AddRule("Landing lights", !_flowTracker.AreLandingLightsOn, "OFF/RETRACTED", "Landing Lights left ON");
                    AddRule("Taxi lights", _flowTracker.TaxiLightState == 0, "OFF", "Taxi Lights left ON");
                    AddRule("Rnw Turnoff", !_flowTracker.IsRnwTurnoffOn, "OFF", "Rnw Turnoff left ON");
                    AddRule("Seat belt", !_flowTracker.AreSeatbeltsOn, "OFF", "Seatbelts left ON");
                    break;
            }
            return rules;
        }

        private void EvaluateFlightPhaseFlow(FlightPhase phase)
        {
            var rules = GetRulesForPhase(phase);
            List<string> errors = new List<string>();
            bool safetyViolation = false;

            System.Text.StringBuilder logBuilder = new System.Text.StringBuilder();
            logBuilder.AppendLine($"\n[SCORE_FLOW] === Phase Ended: {phase.ToString().ToUpper()} ===");

            foreach (var rule in rules)
            {
                if (rule.IsPass)
                {
                    logBuilder.AppendLine($"[SCORE_FLOW] - {rule.ItemName}: {rule.Expected} -> PASS");
                }
                else
                {
                    logBuilder.AppendLine($"[SCORE_FLOW] - {rule.ItemName}: {rule.Expected} -> FAIL");
                    errors.Add(rule.FailMessage);
                    if (rule.IsSafety) safetyViolation = true;
                }
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
