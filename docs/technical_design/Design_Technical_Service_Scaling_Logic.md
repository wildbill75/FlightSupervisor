# Design Technical: In-Flight Service Scaling Logic

## Overview
Implements a time-sensitive service simulation that adapts its progress rate based on the SimBrief planned flight time, with manual interruption capability.

## ⚙️ Logic & State
- **`ServiceHalted`**: A boolean flag in `CabinManager.cs`.
- **`UpdateService()`**: 
  - Increments progress ONLY if `!ServiceHalted` and `AltitudeMSL > 10000`.
  - Pause-on-Toggle: Hitting the `INTERRUPT_SERVICE` command toggles the flag.
- **Automatic Interruption**:
  - `Severe Turbulence`: Automatically sets `ServiceHalted = true` and triggers a message to the Captain.

## 📡 UI Sync
- **Dynamic Button**: 
  - `START_SERVICE` -> becomes `INTERRUPT_SERVICE` while running.
  - `INTERRUPT_SERVICE` -> becomes `RESUME_SERVICE` while halted.
- **Gauge State**: 
  - `Blue`: Service running.
  - `Orange`: Service halted.
  - `Green`: Service complete (100%).
