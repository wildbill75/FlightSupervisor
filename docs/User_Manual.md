# Flight Supervisor - User Manual

## Overview
Flight Supervisor is an advanced companion application for Microsoft Flight Simulator. It links directly to the simulator to actively monitor, score, and guide pilots through realistic airline flight operations. From briefing to ground handling and arrival scoring, it transforms passive flights into a structured career experience.

## 1. Getting Started

### Launching the Application
1. Start Microsoft Flight Simulator.
2. Launch the Flight Supervisor desktop application.
3. The app connects automatically to the simulator via SimConnect. If the connection fails or is lost, you can click "**LINK SIMULATOR**" in the sidebar menu.

### Initial Configuration
Before requesting your first flight plan, open the **Settings** menu to set up your preferences:
*   **SimBrief Integration:** Enter your SimBrief username to enable one-click flight plan fetching.
*   **Aviation Units:** Select your preferred units for Weight (Lbs / Kg), Temperature (C / F), Speed (Knots / Mach / Kph), and Pressure (InHg / HPa). All app data will instantly translate to these choices.
*   **Regional & Locale:** Adjust time display logic.

## 2. Pre-Flight Preparation

### Fetching a Flight Plan
1. Ensure you have generated a dispatch on SimBrief using your configured username.
2. On the Flight Supervisor dashboard, click the "**FETCH PLAN**" button in the bottom right corner.
3. The app will securely download your dispatch, parse the route, weights, pax count, and METAR/TAF weather data.

### Flight Briefing
1. The **Flight Briefing** modal will automatically compile a smart textual briefing.
2. Review the conditions at your Departure and Destination.
3. Pay attention to highlighted warnings (e.g., strong crosswinds, short runways). The briefing will also suggest when you should heavily depend on your Alternate airport.

## 3. Ground Operations

1. Once the flight plan is loaded and you are cold & dark at the gate, click "**START OPS**".
2. Open the **Ground Ops** tab on the left menu.
3. You will see a Live Terminal showing ground services arriving (Refuel, Catering, Boarding, Cargo, Cleaning). 
4. **Delays:** Random operational delays may occur. The terminal will log these communications.
5. You must wait for Ground Operations to complete before pushing back. Pushing back early or clicking the "**ABORT**" modal may negatively impact your safety and ops scores.

## 4. In-Flight Monitoring

### The Dashboard
*   The **Header Bar** continuously displays the current Zulu Time and the automatically detected **Flight Phase** (e.g., STANDBY, BOARDING, TAXI OUT, CLIMB, CRUISE, DESCENT).
*   **Live Telemetry:** The main dashboard updates your altitude, ground speed, and heading dynamically.
*   **Cabin Anxiety & Logs:** The Cabin Manifest tab shows your passengers. Aggressive maneuvers and excessive G-forces will trigger passenger anxiety, reducing your Comfort Score.

### Scoring
You begin every flight with a **SuperScore** of 1000.
Safety, Comfort, Maintenance, and Operations sub-scores are tracked actively.
*   **Comfort:** Avoid steep bank angles and G-force spikes.
*   **Safety/Ops:** Respect speed limits below 10,000ft, do not over-speed your flaps/gear, and manage your ground ops properly.

## 5. Arrival & Debriefing

1. Upon landing and arriving at the destination gate, the Simulator will detect block-in. 
2. A **Flight Report** modal will generate automatically.
3. It will detail your:
    *   Total Block Time and Punctuality.
    *   Detailed landing metrics including Touchdown Zone FPM (Feet Per Minute) and G-Force.
    *   Final SuperScore breakdown into sub-categories.
4. Flights below a certain score tolerance may be considered "Failed" by company standards.
