# Design Technical: Flight Phase State Machine

## Overview
The `FlightPhaseManager` is the core state machine of the application. It transitions the flight status based on real-time telemetry from SimConnect (Radio Height, Ground Speed, Engine Combustion, etc.).

## 🚉 Flight Phases
1. **AtGate**: Initial state. Engines off or parking brake set at origin.
2. **Pushback**: Parking brake released, ground speed detected, or pushback L:Var active.
3. **TaxiOut**: Moving on ground towards the runway.
4. **Takeoff**: High throttle and ground speed above 80 kts.
5. **InitialClimb**: Airborne, positive rate, altitude < 1500ft AGL.
6. **Climb**: Passing 1500ft AGL towards cruise altitude.
7. **Cruise**: Altitude stable (+/- 500ft) near Target Cruise Altitude.
8. **Descent**: Sustained negative vertical speed from cruise altitude.
9. **Approach**: Passing 4000ft AGL or captured localizer.
10. **Landing**: Below 50ft AGL until touchdown.
11. **TaxiIn**: Vacated runway, moving on ground.
12. **Arrived**: Parking brake set at destination or engines cut.

## 🛠️ Logic Design
- **Transition Constraints**: Prevents illegal jumps (e.g., AtGate direct to Cruise).
- **Hysteresis**: Filters noise in vertical speed and altitude to avoid "phase flickering".
- **Radio Height Priority**: Uses Radio Height over Alt MSL for landing/takeoff detection to handle high-altitude airports (e.g., La Paz).
