# Design Plan: Crisis Generator

## Problem Description
To break the monotony of long cruise phases and to test the player's responsiveness, the application needs a dynamic event generation engine known as the **Crisis Generator**. This engine will simulate medical emergencies, unruly passengers, and other critical alerts (e.g. depressurization) based on contextual triggers and probabilities.

## Proposed Changes

### Core Architecture
#### [NEW] [CrisisManager.cs](file:///d:/FlightSupervisor/FlightSupervisor.UI/Services/CrisisManager.cs)
- Introduce a new service responsible for hosting the crisis lifecycle.
- **Tick Engine:** Evaluate probabilities every minute while the aircraft is airborne and above 10,000 ft. Contextual modifiers (e.g. severe turbulence, massive delays) will exponentially increase these odds.
- **Crisis State:** Maintain an active enum (e.g., `None`, `MedicalEmergency`, `UnrulyPassenger`, `Depressurization`).
- **Resolution Tracking:** Track time-to-respond. Fast responses grant SuperScore bonuses (+150 pts), while ignored crises drain passenger comfort and trigger exponential penalties.
- Expose unified C# events to notify the UI: `OnCrisisTriggered`, `OnCrisisResolved`, `OnCrisisCritical`.

### Frontend / UI Layer
#### [MODIFY] [app.js](file:///d:/FlightSupervisor/FlightSupervisor.UI/wwwroot/js/app.js)
- Attach socket listeners / C# bridge calls for `crisisTriggered`.
- Display a flashing red Critical Alert Banner pushing down the main interface.
- Play a triple-chime emergency audio sound (`<audio>` tag Design).
- Display a dynamic countdown/progress bar representing passenger anxiety.

#### [MODIFY] [index.html](file:///d:/FlightSupervisor/FlightSupervisor.UI/wwwroot/index.html)
- Add the hidden DOM elements for the Crisis Alert Banner and the audio tags.

### Scenarios & Integration
#### [MODIFY] [SimConnectService.cs](file:///d:/FlightSupervisor/FlightSupervisor.UI/Services/SimConnectService.cs)
- Monitor telemetry for sudden altitude drops to trigger/validate the `Depressurization` crisis.

#### [MODIFY] [SuperScoreManager.cs](file:///d:/FlightSupervisor/FlightSupervisor.UI/Services/SuperScoreManager.cs)
- Subscribe to `CrisisManager` events to calculate the massive bonus / penalty payouts upon resolution or failure.

#### [MODIFY] [Settings UI / Backend]
- Add a "Crisis Frequency" setting (Off, Realistic, Frequent, Chaos) in both the HTML modal and the backend `AppSettings.cs`.

## Verification Plan
### Automated / UI Tests
- Use a temporary debug button in the UI (or a `/crisis` developer shortcut in the backend) to instantly force a crisis to trigger without waiting hours.
### Manual Verification
- Verify the alarm triggers both the banner and the audio file.
- Confirm resolution buttons on Intercom and PA panel correctly clear the banner.
- Validate the SuperScore increment in the Flight Log.
