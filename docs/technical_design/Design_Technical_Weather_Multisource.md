# Design Technical: Multisource Weather System

## Overview
Ensures that the application has access to high-fidelity weather data by aggregating multiple sources, providing fallbacks when primary APIs are unavailable or when specific simulator add-ons (like Active Sky) are detected.

## 🛰️ Weather Sources
1. **SimBrief API**: Initial source for METAR/TAF during the briefing phase.
2. **NOAA (AviationWeather.gov)**: Real-time source for global METAR/TAF updates every 15-30 minutes.
3. **Active Sky (ASFS/ASP3D)**: Direct integration with the user's weather engine via a local XML API (`http://localhost:19285`). Active Sky data takes priority over NOAA if the service is reachable.

## ⚙️ Logic & Parsing
- **Regex Parsing**: Extensive use of Regular Expressions to extract Wind, Visibility, Temp/Dew, QNH, and Cloud layers from raw METAR/TAF strings.
- **Unit Normalization**: Automatically converts hPa to inHg or Celsius to Fahrenheit based on the user's `UnitPreferences`.
- **Hysteresis & Refresh**: Refreshes "Live Weather" every 15 minutes to avoid excessive API calls while keeping the briefing updated during long-haul flights.

## 📂 Station Objects
Data is structured into `BriefingStation` objects, which include:
- `RawMetar` / `RawTaf`
- `Commentary` (The narrative analysis)
- `RunwayAdvice`
- `Notams`
- `IsolatedVars` (QNH, Temp, Wind, etc. for badge display)
