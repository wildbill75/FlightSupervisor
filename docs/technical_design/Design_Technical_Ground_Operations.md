# Design Technical: Ground Operations System

## Overview
The `GroundOpsManager` handles the lifecycle of ground services. It uses a state-driven approach (`GroundServiceState`) and synchronizes with MSFS Zulu time.

## 🏗️ Data Structure: `GroundService`
- **`TotalDurationSec`**: Base time calculated from SimBrief Pax/Fuel data.
- **`StartOffsetMinutes`**: T-Minus value relative to `TargetSobt`.
- **`RequiresManualStart`**: Boolean flag (used for Refueling).
- **`ProgressPercent`**: Scaled value used by the UI Glassmorphism gauges.

## ⏱️ Logic & Synchronization
- **Zulu Sync**: The `Tick(DateTime? currentZulu)` method ensures that even if the simulator is paused or time-accelerated, the ground services progress according to the simulation's "internal" clock.
- **Warp Logic**: `ForceCompleteAllServices()` sets all active service states to `Completed` and sets their `ElapsedSec` to the target duration.
- [X] **TICKET 31 : Incompatibilité Catering / Embarquement**
  - Implémenté via des checks d'exclusivité dans `Tick()`. Si Embarquement `InProgress` ou `Delayed`, l'Embarquement attend ("Waiting for Catering"). Si l'état de l'embarquement est entamé ou terminé (`> NotStarted` & `!= Skipped`), le Catering est ignoré (`Skipped`) avec le message "Skipped (Pax on board)".
- [X] **TICKET 32 : Nettoyage (Cleaning) Interdit avec Passagers**
  - Évalué dans `Tick()`. Identique au Catering : si l'embarquement est entamé, le Cleaning est ignoré (`Skipped`).
- [X] **TICKET 33 : Nettoyage Low-Cost (PNC)**
  - Lecture automatique du `SimBriefResponse` ICAO/IATA (RYR, EZY, WZZ...). Le service "Cleaning" est remplacé à la volée par "PNC Chores" et assigné au PURSER pour le logging virtuel.

## 🎭 Virtual Actors (Logs)
The system maps services to specific actors to provide a narrative log in the UI:
- **GATE AGENT**: Boarding updates.
- **PURSER**: Catering/Cleaning status.
- **RAMP AGENT**: Fuel, Cargo, and Technical services.
