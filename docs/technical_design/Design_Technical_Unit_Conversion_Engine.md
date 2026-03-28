# Design Technical: Unit Conversion Engine

## Overview
Ensures all aeronautical data can be displayed in the pilot's preferred unit system, handling the complexity of international aviation standards (US Imperial vs. Metric/International).

## ⚙️ Supported Conversions
- **Weight**: KG ↔ LBS (Factor: 2.20462)
- **Speed**: Knots ↔ KM/H (Factor: 1.852)
- **Temperature**: Celsius ↔ Fahrenheit (`(C * 9/5) + 32`)
- **Pressure**: hPa (QNH) ↔ inHg (Altimeter) (Factor: 0.02953)
- **Altitude**: Feet ↔ Meters (Factor: 0.3048)

## 🏗️ Technical Integration
- **`UnitPreferences`**: A central model injected into services (like `WeatherBriefingService`).
- **Dynamic Formatting**: Methods like `FormatWeight(double kg)` automatically check the preference and return a localized string with the correct suffix.
- **No Double-Rounding**: All internal calculations are performed in a "Base Unit" (Metric/SI) and are only converted at the presentation layer (UI/Briefing) to avoid precision drift.
