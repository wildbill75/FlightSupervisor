# Design Gameplay: Achievements & Pilot Badges

## Overview
The Achievement system rewards pilots for precision, safety, and dedication while also marking "Dishonorable" moments to encourage better flying habits.

## 🎖️ Achievement Tiers
- **Rookie**: Basic milestones (First flight, Butter landing, On-time arrival).
- **Line Captain**: Consistency and skill (50 flights, 10 min manual flying, zero-infraction streaks).
- **Check Airman**: Mastery of the aircraft (High-crosswind landings, precise touchdown zones, long-haul endurance).
- **Dishonorable**: Failures (Spine crusher landings, skipping safety procedures, landing without lights).

## 🛠️ Evaluation & Unlocks
Badges are evaluated at the conclusion of every flight log. 
- **Criteria**: Matches flight telemetry (FPM, G-Force, manual time) against badge definitions.
- **Persistence**: Unlocked badges are saved to the `Profile.json` file and are displayed in the "Pilot Profile" tab of the UI.
