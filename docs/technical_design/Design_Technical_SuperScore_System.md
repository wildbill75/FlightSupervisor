# Design Technical: SuperScore Scoring System

## Overview
The `SuperScoreManager` evaluates pilot performance and generates a detailed flight report across **five distinct categories**. It listens to events from the `FlightPhaseManager` and `SimConnectService`, and conducts rigorous flow checks at the end of each flight phase.

## 🏆 The 5 Categories
1. **FLIGHT PHASE FLOWS**: Mandatory checks linked to specific flight phases (e.g. Ground, Taxi, Climb, Cruise). At the end of each phase, a detailed report is generated (Red: failed, Green: Success) summarizing all tracked parameters for that phase.
2. **COMMUNICATIONS (PA+PNC+TECH+CO)**: Proper execution of passenger announcements, crew coordination, technical reporting, and company messaging.
3. **AIRMANSHIP**: Smoothness of manual flying, go-around decisions, proper trajectory management, crosswind handling, and overall good practices.
4. **MAINTENANCE**: Wear and tear, mechanical limits (Hard landings, brake temp, flaps/gear overspeed, structural limits).
5. **ABNORMAL OPERATIONS**: Crisis reaction, system failures, and proper handling of emergencies.

## 🧮 Scoring Logic
- **Baseline**: Starts at 1000 points.
- **Dynamic Penalties**: Instant deductions (e.g., -50 for forgotten Landing Lights).
- **Objective Contracts**: Voluntary "Company Objectives" accepted at the start of the flight (e.g., "Max -200 FPM Landing"). Failing a contract results in a massive penalty (-500), success grants a bonus (+200).
- **Airmanship Bonus**: Rewarding manual flying in the final 4000ft of approach.

## 📡 Events & Payloads
- **`OnScoreChanged`**: Fired on every delta to update the UI Glassmorphism gauges.
- **`EndFlight`**: Aggregates all events into a JSON array for the Final Flight Report accordion.
