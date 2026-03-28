# Design Technical: Passenger Manifest Engine

## Overview
The `PassengerManifestService` is responsible for Procedural Content Generation (PCG) of the flight's occupants.

## 🎲 Randomization & Dictionaries
- **Name Engine**: Uses nationality-specific arrays of first and last names (FR, UK, US, ES, DE, IT).
- **Seat Generation**:
  - Skips Row 13 for realism.
  - Automatically selects columns (A-F for narrowbody, A-K for widebody) based on `maxPax` from SimBrief.
  - Uses a Fisher-Yates shuffle to scatter passengers randomly.

## 📐 Manifest Data Model
- **`CrewMember`**: { Name, Role }.
- **`Passenger`**: { Name, Seat, Nationality, Age, IsFastened, AnxietyLevel }.

## 🌍 Region Logic
Mapping ICAO prefixes (e.g., `LF` -> `FR`, `EG` -> `UK`) to determine the localized naming pool for both crew and passengers.
- **Fallback**: Defaults to an "International" mix if the region is unrecognized.
