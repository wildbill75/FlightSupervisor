# [AMBRE] Design Technical: Captain Announcements Logic

## Overview
Implements a context-aware PA system that pulls data from SimBrief, Live Weather, and the Flight Phase Machine.

## ⚙️ Logic & Triggers
- **`Welcome_PA_Trigger`**: Enabled after Pushback completion. Pulls `DestName`, `SimBrief_EET`, and `TafSummary`.
- **`Descent_PA_Trigger`**: Enabled when `FlightPhase` transitions to `Descent`. Pulls `Live_Metar_Dest_Temp` and `WindFactor`.
- **`Crisis_PA_Trigger`**: 
  - `IsGoAroundActive`: Boolean flag set by `FlightPhaseManager` upon sudden pitch/power increase at low altitude.
  - `IsDiversionActive`: Set if the destination in the log differs from the initial flight plan.

## 📡 Content Generation
- **Airport Names**: Use the full city/airport name from `airports.json`, never the 4-letter ICAO code for the oral PA.
- **Weather Strings**: Logic identifies "Bumpy", "Windy", or "Clear" based on TAF/METAR parsing.

## 🏆 Anxiety Logic
- **`ReassuranceBonus`**: A temporary multiplier (e.g., 0.5x) applied to anxiety spikes for 10 minutes following an informative PA.
- **`SilenceMultiplier`**: If a crisis is active and `PA_Crisis_Timer > 120s`, the `AnxietyIncrement` in `CabinManager.cs` is multiplied by 2.0.
