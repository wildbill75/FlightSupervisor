# Design Technical: SuperScore Scoring System

## Overview
The `SuperScoreManager` evaluates pilot performance across four distinct pillars. It listens to events from the `FlightPhaseManager` and `SimConnectService`.

## 🏆 The 4 Pillars
1. **Safety**: Adherence to SOPs (Lights, Gear, Flaps), structural limits (Pitch/Bank/G), and unstable approaches.
2. **Comfort**: Passenger wellbeing based on vertical speed, bank angle, and hard landings.
3. **Maintenance**: Wear and tear (Hard landings, Engine cooldown times, Tail strikes, Flaps overspeed).
4. **Operations**: Dispatch efficiency (Fuel accuracy, Punctuality vs SOBT/SIBT).

## 🧮 Scoring Logic
- **Baseline**: Starts at 1000 points.
- **Dynamic Penalties**: Instant deductions (e.g., -50 for forgotten Landing Lights).
- **Objective Contracts**: Voluntary "Company Objectives" accepted at the start of the flight (e.g., "Max -200 FPM Landing"). Failing a contract results in a massive penalty (-500), success grants a bonus (+200).
- **Airmanship Bonus**: Rewarding manual flying in the final 4000ft of approach.

## 📡 Events & Payloads
- **`OnScoreChanged`**: Fired on every delta to update the UI Glassmorphism gauges.
- **`EndFlight`**: Aggregates all events into a JSON array for the Final Flight Report accordion.
