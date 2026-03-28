# Design Technical: Pilot Profile Persistence

## Overview
Handles the storage of the pilot's career progress, including flight logs summary, total hours, and unlocked achievements.

## 💾 Data Model: `PilotProfile`
- **Career Stats**: `TotalFlights`, `TotalHours`, `AverageSuperScore`, `SafetyInfractions`.
- **Achievements**: `List<string>` of unlocked Badge IDs.
- **Persistence**: Local storage in `Profile.json` using `System.Text.Json`.

## 🔄 Load/Save Lifecycle
- **`LoadProfile()`**: Triggered on application startup. Includes a fail-safe mechanism that creates a default profile if the file is missing or corrupted.
- **`SaveProfile()`**: Triggered at the end of every flight or when an achievement is unlocked.
- **Error Handling**: Writes a `ProfileLoadError.txt` in the base directory if a catastrophic deserialization error occurs.
