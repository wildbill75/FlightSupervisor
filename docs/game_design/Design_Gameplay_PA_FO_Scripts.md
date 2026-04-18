# 👨‍✈️ First Officer Communications - Voice Action Scripts

Ce document contient l'architecture des "blocs" audio pour le **First Officer** (Copilote).

Tu as déjà de nombreuses voix de Captain pour les interactions (Arm doors, seats for takeoff, etc.). Nous allons donc utiliser la voix du First Officer pour les **déviations de sécurité de l'Airbus** (Checklist & Warnings), là où il interpelle le Commandant lorsqu'il constate une erreur selon la phase de vol.

*Toutes les pistes doivent être enregistrées au format `.mp3` sous un nouveau répertoire : `wwwroot/assets/sounds/EN_FD_FirstOfficer/Warnings/`.*

---

## 1. Flight Deck Cockpit Warnings
> _Diffusé automatiquement dans l'intercom du cockpit si le Commandant commet un oubli qui impacte le confort ou dépasse les contraintes de sécurité._

### 1a. Landing Gear (Train d'atterrissage)
*   `Warn_GearNotDown.mp3` 👉 "Captain, crossing 2000, gear is not down."
*   `Warn_GearExtended.mp3` 👉 "Captain, speed is increasing, gear is still down."

### 1b. Flaps (Volets)
*   `Warn_FlapsExtended.mp3` 👉 "Captain, speed is increasing, flaps are still extended."

### 1c. Seatbelts (Ceintures)
*   `Warn_SeatbeltTaxi.mp3` 👉 "Captain, aircraft is moving, seatbelts are off."
*   `Warn_Seatbelt10k.mp3` 👉 "We've reached cruise altitude, consider releasing the cabin."
*   `Warn_SeatbeltDesc.mp3` 👉 "Passing 10,000, seatbelts sign is off."

### 1d. Exterior Lights (Feux)
*   `Warn_LightsClimb.mp3` 👉 "Passing 10,000, landing lights are still on."
*   `Warn_LightsDesc.mp3` 👉 "Passing 10,000, landing lights are off."
