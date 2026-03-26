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

### Scoring Mechanism & The 4 Pillars
You begin every flight with a **Base SuperScore** of 1000 points, acting as your "Captain's License Trust". 
*   Ending near **1000** implies a standard, safe flight.
*   Ending **> 1100** rewards exceptional airmanship (e.g., perfect centerline, kiss landings).
*   Ending **< 800** flags severe SOP violations or potential incidents.

Your score fluctuates dynamically during the flight, silently distributed across **4 Core Pillars** that are evaluated in your final debriefing:

1. **Safety (Airmanship / Sécurité)** 🦺 
   Focuses on preventing fatal danger and respecting ATC.
   *Examples:* Speeding (> 250kts) below 10,000ft, unstable approaches, perfect runway centerline tracking (+50), aggressive pitch attitudes, flying without required lights.

2. **Comfort (Confort Passager)** 🍸 
   Focuses on passenger well-being and anxiety reduction.
   *Examples:* Harsh braking during taxi, tight high-speed turns, excessive G-forces, high vertical speeds causing ear pressure, smooth "Butter Landings" (+150).

3. **Maintenance (Intégrité Avion)** 🔧 
   Focuses on preventing structural damage to the airframe.
   *Examples:* Severe Hard Landings (> 600 fpm) damaging the gear, extending flaps/gear above placard speeds, tailstrikes, or mismanaging engine failures.

4. **Operations (Company Rules)** ⏱️ 
   Focuses on fuel efficiency and logistical punctuality.
   *Examples:* Leaving the APU running during cruise (wasting fuel), excessive block delays, or pushing back with pending ground services (forcing compensation claims).

## 5. Arrival & Debriefing

1. Upon landing and arriving at the destination gate, the Simulator will detect block-in. 
2. A **Flight Report** modal will generate automatically.
3. It will detail your:
    *   Total Block Time and Punctuality.
    *   Detailed landing metrics including Touchdown Zone FPM (Feet Per Minute) and G-Force.
    *   Final SuperScore breakdown into sub-categories.
4. Flights below a certain score tolerance may be considered "Failed" by company standards.
