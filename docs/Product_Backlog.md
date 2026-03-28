# Flight Supervisor - Product Backlog (Macro)

> **Légende :**
> - **Story (Macro)** : Un grand ensemble de fonctionnalités métier.
> - **Sprint (Micro)** : Un document de tickets techniques précis situé dans `docs/sprints/`.
> - **Status** : `[x]` Fini, `[/]` En cours, `[ ]` À faire.

---

## 🚀 Version 1.0 (MVP) - En cours

### Story 1 : Core Architecture & Telemetry Bridge [/]
*Objectif : Établir une connexion SimConnect/WASM robuste.*
- **Détails :** [Sprint_Core_Architecture.md](sprints/Sprint_Core_Architecture.md)
- **Technical Design :** [Design_Technical_Extraction_WASM_LVars_Fenix.md](technical_design/Design_Technical_Extraction_WASM_LVars_Fenix.md)

### Story 2 : Flight Phase State Machine [x]
*Objectif : Transition de phases fiable par Radio Height et G-Force.*
- **Technical Design :** [Design_Technical_Flight_Phase_Machine.md](technical_design/Design_Technical_Flight_Phase_Machine.md)

### Story 3 : Ground Operations & Anti-Cheat [/]
*Objectif : Simulation des services au sol et synchronisation temporelle.*
- **Détails :** [Sprint_Ground_Operations.md](sprints/Sprint_Ground_Operations.md)
- **Gameplay Design :** [Design_Gameplay_Ground_Operations.md](game_design/Design_Gameplay_Ground_Operations.md)
- **Technical Design :** [Design_Technical_Ground_Operations.md](technical_design/Design_Technical_Ground_Operations.md)

### Story 4 : SuperScore & Black Box [/]
*Objectif : Analyse de performance (FPM, G-Force, SOP).*
- **Détails :** [Sprint_BlackBox_Scoring.md](sprints/Sprint_BlackBox_Scoring.md)
- **Technical Design :** [Design_Technical_SuperScore_System.md](technical_design/Design_Technical_SuperScore_System.md)

### Story 5 : Chronometry & Final Flight Report [x]
*Objectif : Figeage AOBT/AIBT et rapport visuel modernisé.*
- **Technical Design :** [Design_Technical_Chronometry_AOBT_AIBT.md](technical_design/Design_Technical_Chronometry_AOBT_AIBT.md)

### Story 6 : UI/UX Reorganization [/]
*Objectif : Sidebar navigation et ergonomie globale.*
- **Technical Design :** [Design_Technical_Restructuration_WebView2.md](technical_design/Design_Technical_Restructuration_WebView2.md)
- **UI Design :** [Design_UI_System.md](ui_design/Design_UI_System.md)

### Story 7 : Localization (i18n) [/]
*Objectif : Support FR/EN pour UI et messages Backend.*
- **Technical Design :** [Design_Technical_Localization_System.md](technical_design/Design_Technical_Localization_System.md)

### Story 8 : Third-Party Weather Integration [/]
*Objectif : Support SimBrief, Active Sky et MSFS Live.*
- **Technical Design :** [Design_Technical_Weather_Multisource.md](technical_design/Design_Technical_Weather_Multisource.md)

### Story 9 : GSX Advanced Integration [ ]
*Objectif : Auto-Sync avec GSX Pro via L:Vars.*

### Story 10 : Measuring Units Architecture [x]
*Objectif : Conversion automatique KGS/LBS, C/F, HPA/INHG.*
- **Technical Design :** [Design_Technical_Unit_Conversion_Engine.md](technical_design/Design_Technical_Unit_Conversion_Engine.md)

### Story 11 : Contextual Tooltips & Onboarding [ ]
*Objectif : Tutoriels et aides à la navigation.*

### Story 13 : Multi-Leg Planning [ ]
*Objectif : Enchaînement des vols sans retour menu ("Fetch Next Leg").*

### Story 14 : Passenger & Crew Manifest [x]
*Objectif : Génération de passagers par nationalité et Seat Map 2D.*
- **Gameplay Design :** [Design_Gameplay_Passenger_Manifest.md](game_design/Design_Gameplay_Passenger_Manifest.md)
- **Technical Design :** [Design_Technical_Passenger_Manifest.md](technical_design/Design_Technical_Passenger_Manifest.md)

### Story 15 : Advanced Dispatch & Met Briefing [x]
*Objectif : Analyse TAF, NOTAMs et anticipation de piste.*
- **Gameplay Design :** [Design_Gameplay_Advanced_Dispatch_Briefing.md](game_design/Design_Gameplay_Advanced_Dispatch_Briefing.md)

### Story 16 : In-Flight Judgment & Airmanship [ ]
*Objectif : Récompense pour déviation SIGMET et vol manuel.*

### Story 17 : Passenger Satisfaction & Cabin Management [/]
*Objectif : Turbulence Jitter, PA Button, et simulation de ceintures.*
- **Détails :** [Sprint_Advanced_Turbulence_Cabin_Dynamics.md](sprints/Sprint_Advanced_Turbulence_Cabin_Dynamics.md)
- **Gameplay Design :** [Design_Gameplay_Advanced_Turbulence_Cabin_Dynamics.md](game_design/Design_Gameplay_Advanced_Turbulence_Cabin_Dynamics.md), [Design_Gameplay_Captain_Announcements.md](game_design/Design_Gameplay_Captain_Announcements.md)
- **Technical Design :** [Design_Technical_Captain_Announcements_Logic.md](technical_design/Design_Technical_Captain_Announcements_Logic.md)

### Story 17.5 : PNC Communication & Cabin Prep [x]
*Objectif : Jauge de préparation cabine dynamique et gestion du temps PNC pendant le taxi.*
- **Gameplay Design :** [Design_Gameplay_PNC_Communication_Cabin_Prep.md](game_design/Design_Gameplay_PNC_Communication_Cabin_Prep.md)
- **Technical Design :** [Design_Technical_Cabin_Preparation_Logic.md](technical_design/Design_Technical_Cabin_Preparation_Logic.md)

### Story 17.6 : In-Flight Service Scaling [/]
*Objectif : Adaptation de la vitesse du service au temps de vol et respect de la limite des 10 000 pieds.*
- **Gameplay Design :** [Design_Gameplay_InFlight_Service_Scaling.md](game_design/Design_Gameplay_InFlight_Service_Scaling.md)
- **Technical Design :** [Design_Technical_Service_Scaling_Logic.md](technical_design/Design_Technical_Service_Scaling_Logic.md)

### Story 18 : Crisis Generator [/]
*Objectif : Moteur de probabilité d'urgences critiques.*
- **Détails :** [Sprint_Crisis_Generator.md](sprints/Sprint_Crisis_Generator.md)
- **Gameplay Design :** [Design_Gameplay_Crisis_Generator.md](game_design/Design_Gameplay_Crisis_Generator.md)
- **Technical Design :** [Design_Technical_Crisis_Generator.md](technical_design/Design_Technical_Crisis_Generator.md)

### Story 19 : Airline Policies & Risk Management [ ]*Objectif : Profils Legacy vs Low-Cost (Pondération du score).*

### Story 20 : In-Game Toolbar Panel [x]
*Objectif : Toolbar MSFS pour VR/2D.*
- **Gameplay Design :** [Design_Gameplay_InGame_Panel_MSFS.md](game_design/Design_Gameplay_InGame_Panel_MSFS.md)
- **Technical Design :** [Design_Technical_MSFS_Toolbar_Bridge.md](technical_design/Design_Technical_MSFS_Toolbar_Bridge.md)

---

## 🎯 Stories Post-Release (21 - 35)

- **S21 : Fuel Economy** [ ]
  - **Gameplay Design :** [Design_Gameplay_Fuel_Economy.md](game_design/Design_Gameplay_Fuel_Economy.md)
- **S22 : Flight Logger History** [/]
- **S23 : Manual Flying Bonus** [ ]
  - **Gameplay Design :** [Design_Gameplay_Manual_Flying.md](game_design/Design_Gameplay_Manual_Flying.md)
- **S33 : Pilot Profile & Achievements** [/]
  - **Gameplay Design :** [Design_Gameplay_Achievements_Badges.md](game_design/Design_Gameplay_Achievements_Badges.md)
  - **Technical Design :** [Design_Technical_Pilot_Profile_Persistence.md](technical_design/Design_Technical_Pilot_Profile_Persistence.md)
- **S35 : Cockpit Scrambler** [ ]
