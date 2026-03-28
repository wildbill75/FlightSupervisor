# Design Technical: Cabin Prep & Seating Logic

## Overview
Extends the `CabinManager.cs` to handle dual-phase preparation (Taxi/Descent) and conditional command enabling.

## ⚙️ Logic & State
- **`CabinSecuringProgress`**: Tracked independently for each phase.
- **`PrepareCommand_Enabled`**: 
  - TaxiOut: Always enabled.
  - Descent: Enabled when `FlightPhase == Descent`.
- **`SeatingCommand_Enabled`**: 
  - Enabled when `CabinSecuringProgress == 100.0`.
  - **Penalty Check**: If `SeatingCommand` is triggered while `< 100`, apply `_forcedSeatingPenalty` and skip remaining progress.

## 📡 UI Sync
- **Gauges**: 
  - `Amber / Blinking`: Prep in progress (< 100%).
  - `Green`: Prep complete.
  - `Grey`: Seating complete & Crew ready.
- **Web Message**: PNC audio confirmation triggered upon 100% and command reception.

## 🛠️ Triggers
- **Takeoff Penalty**: Triggered if `G-Force > 1.05` or `GroundSpeed > 40` (acceleration) while `State != TakeoffSecured`.
- **Landing Penalty**: Triggered if `AltitudeMSL < 500` AGL while `State != LandingSecured`.
