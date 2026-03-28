# Design Plan: Advanced Turbulence & Cabin Dynamics

## 1. Goal Description
Implement an advanced turbulence detection system (Light/Moderate/Severe/Extreme) coupled with a reaction-time-based PA button. Revamp the passenger demographics to introduce soft caps on anxiety/comfort. Introduce a per-passenger seatbelt simulation where individual compliance affects injury risk and comfort during rough flying, visually reflected in the Manifest UI.

## 2. État des Lieux (Audit Post-Annonces)
- [x] **Détection Sévère (SimConnect)** : Implémentée dans `FlightPhaseManager` via G-Force delta > 0.4.
- [x] **Bouton PA Dynamique** : Opérationnel dans l'Intercom, clignote en cas de silence.
- [x] **Démographies de Base** : Les profils Standard, Râleur, Anxieux, Détendu existent et influencent les multiplicateurs.
- [x] **Moteur de Jitter** : Implémenté via variance glissante 5s et détection zero-crossing.
- [x] **Conformité Individuelle** : Intégrée au `CabinManager` et visualisée sur le Seat Map.
- [x] **Moteur de Blessures** : Système de probabilités actif en cas de secousses `Severe+`.
- [x] **Caps de Sécurité** : Brider à 90% via `IncreaseAnxiety` pour les cas non-critiques.

---

## 3. Liste des Tickets (Design Plan)

- [x] **TICKET 21 : Moteur de Jitter Avancé**
  - Catégorisation (Light/Moderate/Severe/Extreme) basée sur la variance de G-Force sur 5 secondes. [DÉSIGNÉ]

- [x] **TICKET 22 : Tracker de Réaction & Bonus PA**
  - Chronomètre de réaction lancé dès détection Sévère+. [DÉSIGNÉ]

- [x] **TICKET 23 : Modèle de Conformité Passager**
  - Attribution d'un état de ceinture individuel à chaque passager. [DÉSIGNÉ]

- [x] **TICKET 24 : Engine de Blessures & Urgence Médicale**
  - Calcul du risque d'impact pour les passagers non attachés en cas de `Severe+`. [DÉSIGNÉ]

- [x] **TICKET 25 : Visualisation Manifest (Seat Map)**
  - Ajout des icônes de ceintures (Vert/Rouge) sur le plan de cabine dans l'interface Manifest. [DÉSIGNÉ]

- [x] **TICKET 26 : Caps de Sécurité (90/10)**
  - Design du "Safety Buffer" : L'anxiété plafonne à 90% pour les turbulences. [DÉSIGNÉ]

---

## 4. Proposed Changes

### Backend C# (Core Logic)

#### [MODIFY] `FlightSupervisor.UI\Services\CabinManager.cs`
- **Turbulence Scale Model (Weather vs Pilot)**: 
  - To differentiate environmental turbulence from a pilot aggressively maneuvering the aircraft, the detection will rely on **high-frequency G-Force jitter** (rapid oscillations crossing 1.0G multiple times per second) rather than simple Min/Max G limits. 
  - Sustained high/low G-forces (e.g., pulling 1.5G smoothly over 3 seconds) will be classified as 'Bad Flying' (Pitch/Bank limit violations) and will *not* trigger the PA button.
  - If the Autopilot is engaged (`_isAutopilotActive == true`), any significant G-Force fluctuation is automatically guaranteed to be environmental.
  - Categorize this true weather turbulence into `Light`, `Moderate`, `Severe`, and `Extreme` based on the amplitude of the high-frequency jitter.
- **Reaction Timer (`_turbulenceReactionStartTime`)**: When `Severe` or `Extreme` turbulence is detected and seatbelts are OFF, start a timer. 
- **PA Command Update (`PA_Turbulence`)**: 
  - Stop the reaction timer.
  - Apply severe SuperScore penalties and Anxiety spikes if the reaction time was too slow (>15s). Grant a bonus if reaction was fast (<5s).
  - Fire an event to `SimConnectService` to forcefully toggle the aircraft's Seatbelt switch to ON.
  - **Dynamic Visibility:** The UI button must disappear automatically as soon as the turbulence dissipates.
- **Unannounced Seatbelt Logic**: If the captain flips the physical Seatbelt switch to ON *without* first making a PA announcement, trigger a slight Anxiety increase (passengers are confused/worried about the lack of communication).
- **Demographics & Flying Capping**:
  - Modify `IncreaseAnxiety` and `DecreaseComfort`. Pure turbulence or flying mistakes (Pitch/Bank) will now be hard-capped at 90% Anxiety and 10% Comfort, respectively. Only major crises (Depressurization, Engine Failure) can push it to 100% / 0%.

#### [MODIFY] `FlightSupervisor.UI\Services\PassengerManager.cs` (or create logic within)
- **Per-Passenger Seatbelt Tracking & Compliance Model**: 
  - Create a `List<PassengerState>` containing `IsSeatbeltFastened` and `DemographicProfile` (Relaxed, Anxious, etc.).
  - **Sign ON / Critical Phases**: During Takeoff/Landing, or when the Seatbelt sign is ON, compliance is extremely high (only 1 or 2 rebellious passengers ignore it).
  - **Sign OFF (Above 10k ft)**: Behavior relies strictly on demographics. Anxious passengers remain seated with belts on, while Relaxed passengers untether.
  - Expose this array to the frontend via the `EndBoarding` payload or a new `ManifestUpdate` event.
- **Injury / RNG Consequence Engine**: 
  - During bad flying (Pitch/Bank violations) or severe turbulence, isolated unfastened passengers are subjected to an RNG dice roll.
  - Fastened passengers always bypass this risk.
  - Unfastened passengers take randomized damage: Minor cuts/bruises (severe comfort loss) to Serious Injuries if the forces are prolonged or extremely violent. Serious injuries trigger a Medical Emergency Crisis automatically.

#### [MODIFY] `FlightSupervisor.UI\Services\SimConnectService.cs`
- **Aircraft Writing**: Add a method `SetSeatbelts(bool state)`. For Asobo/Standard aircraft, transmit `K:CABIN_SEATBELTS_ALERT_SWITCH_TOGGLE`. For the Fenix, attempt to write to `L:S_OH_CALLS_SEATBELTS` via the WASM client (requires researching MobiFlight WASM write Design later).

#### [MODIFY] `FlightSupervisor.UI\MainWindow.xaml.cs`
- **Bridge Audio/UI Payload**: Pass the new `TurbulenceSeverity` down to the `telemetry` JSON so the UI knows when to show the blinking PA button.
- Catch the `PA_Turbulence` WebMessage to play the new audio file securely.

---

### Frontend HTML/JS/CSS (Interface)

#### [MODIFY] `FlightSupervisor.UI\wwwroot\app.js`
- **Dynamic PA Button**: 
  - Modify `updateIntercomButtons`. The "PA: Turbulence" button ONLY appears if `payload.turbulenceSeverity === 'Severe' || 'Extreme'` and seatbelts are currently OFF. 
  - Disappears immediately when `turbulenceSeverity === 'None'`.
  - Hook it up with CSS classes (`animate-pulse bg-red-600`) to catch the captain's eye immediately.
- **Audio Hook**: Play the designated turbulence chime sound when the button appears or is clicked.
- **Manifest Updates**: In `renderManifest()` (or similar logic handling the seat map), parse the new passenger array. Add a tiny green/red seatbelt icon `seatbelt_icon` to each seat block reflecting their individual choice.
- **Localizations**: Update the JSON dictionary for the new severity announcements.

## Verification Plan

### Automated/Manual Testing
1. **Turbulence Trigger:** Fly into a heavy thunderstorm. Verify the PA: Turbulence button appears out of nowhere, blinks, and starts a clock. Verify it disappears completely once clear of the clouds.
2. **Pilot-Induced Jitter Prevention:** Manually yank the yoke violently up and down. Verify the PA: Turbulence button *does not* appear, but you suffer basic Pitch/G-Force SuperScore penalties.
3. **Unannounced Seatbelt:** Flip the physical switch without touching the PA button. Verify anxiety slowly ticks up with a log indicating "Passengers worried about sudden seatbelt sign".
4. **Per-Passenger Seatbelts & Demographics:** Open the Manifest tab. During cruise (Sign OFF), observe roughly 60% of people (mostly the Relaxed ones) removing their belts. 
5. **Injury RNG:** With belts OFF in cruise, purposely perform a violent Pitch maneuver. Verify the SuperScore logs RNG injuries heavily skewed towards unfastened passengers.
6. **Capping Check:** Deliberately fly terribly (constant 45-degree banks). Ensure Anxiety stops rising at 90% and Comfort stops dropping at 10%.

---

## 5. Spécifications de Design Technique

### [BACKEND] CabinManager.cs
- **Moteur de Jitter** : Implémenter le calcul de variance G-Force pour classer la turbulence (4 niveaux).
- **Tracker de Réaction** : Système de chronomètre ('_turbulenceReactionStartTime') pour évaluer le temps de réaction du Commandant.
- **Moteur de Blessure RNG** : Simulation individuelle des blessures pour les passagers non attachés lors de secousses sévères.
- **Suivi des Ceintures** : 'List<PassengerState>' pour suivre la conformité individuelle.

### [FRONTEND] app.js / Manifest UI
- **Bouton PA Dynamique** : Affichage contextuel du bouton 'PA: Turbulence' avec animation de clignotement.
- **Visualisation Manifest** : Icônes de ceintures sur le plan de cabine (Seat Map) mises à jour en tempo réel.
- **Audio** : Intégration du carillon d'alerte lors de la détection de turbulences sévères.
