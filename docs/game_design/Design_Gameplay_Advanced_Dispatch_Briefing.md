# Design Gameplay: Advanced Dispatch & Met Briefing

## Overview
The Advanced Dispatch system transforms raw SimBrief data and live METAR/TAF reports into a conversational, pilot-style briefing. It helps the crew anticipate threats like runway changes, icing, or severe turbulence.

## 🎙️ The Oral Briefing
The `WeatherBriefingService` generates a narrative that covers:
- **Flight Plan Expectations**: Planned cruise altitude vs Tropopause height, and planned step climbs.
- **Station Reports**: Detailed analysis of Departure, Destination, and Alternate airports.
- **Enroute Hazards**: Winds aloft (Head/Tail wind components), ETOPS considerations, and turbulence forecasts from the SimBrief Navlog.

## ⚡ Dynamic Threat Analysis
- **Runway Wind Shift**: The system compares the planned arrival runway with the TAF forecast at the Estimated Time of Arrival (ETA). If a significant tailwind is forecast, it warns the pilot of a "Probable Runway Change".
- **NOTAM Alerts**: Scans the SBP plan for keywords like "CLSD", "U/S", or "OUT OF SERVICE" to identify critical infrastructure issues (Runway/ILS/Taxiway closures).
- **ETA-based TAF Parsing**: Instead of just reading the raw TAF, it identifies the specific `FM` (From), `BECMG` (Becoming), or `TEMPO` (Temporary) block that will be active at the moment of touchdown.

## 🌍 Alternate Viability
Automatically fetches and analyzes the weather for the primary alternate airport mentioned in the SimBrief flight plan, ensuring the pilot has a clear "Plan B" before engine start.
