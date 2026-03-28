# Design Technical: Chronometry (AOBT/AIBT)

## Overview
Ensures precise tracking of flight timing based on simulator Zulu time, allowing for accurate punctuality scoring against the SimBrief operational schedule.

## ⏱️ Key Metrics
- **SOBT (Scheduled Off-Block Time)**: Fetched from SimBrief.
- **AOBT (Actual Off-Block Time)**: Locked the moment the parking brake is released in the `Pushback` phase.
- **SIBT (Scheduled In-Block Time)**: Fetched from SimBrief.
- **AIBT (Actual In-Block Time)**: Locked when the parking brake is set at the destination gate in the `Arrived` phase.

## 📈 Delay Calculation
- Delay is purely based on `AIBT - SIBT`. 
- **Grace Period**: 5 minutes of "Schedule Buffer" allowed before Comfort/Operations penalties start ticking.
- **Progression**: After 15 minutes of delay, passenger anxiety begins to rise exponentially if no "Delay Announcement" is made via the Intercom.

## 🔗 Sync Logic
- Uses `ZULU TIME` variable from SimConnect to avoid issues with local system time or simulator pauses.
