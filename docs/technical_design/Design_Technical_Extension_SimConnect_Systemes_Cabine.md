# Technical Design: Extension Télémetrie Avancée (Systèmes & Cabine)

## 1. Synthèse du Concept
Ce document technique détaille l'extension de la capture de données (Telemetry) depuis Microsoft Flight Simulator vers l'application Flight Supervisor. L'objectif est d'aller au-delà des simples variables de vol (vitesse, altitude) pour capturer l'état des systèmes de l'avion (APU, éclairage extérieur spécifique, consigne des ceintures). 

L'intégration de ces variables dans les algorithmes de `SuperScoreManager` permet de sanctionner rigoureusement le non-respect des Procédures Opérationnelles Standards (SOPs), augmentant considérablement l'immersion et la rigueur exigée du joueur. De plus, ce design englobe la problématique technique complexe de la lecture des variables locales (L:Vars) propres aux avions "Study-Level" (comme le Fenix A320).

## 2. Architecture et Mécaniques Actives

### A. Extension du `PlaneDataStruct` (SimConnect)
La structure C# `PlaneDataStruct` doit être enrichie avec de nouvelles variables issues du SDK MSFS :
- **Systèmes APU :** `APU BLEED`, `APU GENERATOR ACTIVE`, `APU SWITCH`.
- **Éclairage :** Séparation des lumières au lieu de l'état binaire "Taxi/Landing", ex: `LIGHT TAXI`, `LIGHT NAV`, et si possible `EXT_LT_NOSE` / `RWY_TURNOFF` (selon les index d'éclairage).
- **Cabine :** `CABIN SEATBELTS ALERT SWITCH` (binaire/entier).

### B. Câblage des Pénalités (SOP Enforcement)
Ces variables sont réinjectées via le `FlightPhaseManager` dans le `SuperScoreManager` pour évaluer la situation contextuelle :
1. **APU Cruise Penalty :** 
   - *Condition :* `Phase == Cruise` ET (`APU_Bleed == 1` OU `APU_Master == 1`).
   - *Mécanique :* L'APU n'a rien à faire allumé en croisière (sauf panne moteur). Pénalité immédiate de -50 points pour "Gaspillage Carburant / Usure APU".
2. **Turbulence sans Ceinture (Safety Violation) :**
   - *Condition :* `Phase == Cruise / Climb / Descent` ET `GForce > 1.25` (ou `< 0.75`) ET `Seatbelts == 0`.
   - *Mécanique :* Pénalité sévère de -100 points pour "Mise en danger des passagers", augmentant drastiquement l'anxiété cabine.
3. **Lumières de Roulage et Alignement (Line-up Lights) :**
   - *Condition :* `Phase == TakeoffRoll` ET `Nose_Light == 0`.
   - *Mécanique :* Entrer sur la piste active sans les phares d'alignement/décollage entraîne une pénalité de "Safety Violation - Runway Incursion Risk".

### C. Recherche L:Vars et Interopérabilité WASM (Fenix A320)
Les add-ons "Study-Level" (Fenix, PMDG, FlyByWire) court-circuitent les SimVars natifs (le bouton Seatbelts de MSFS ne bouge pas quand on clique sur celui du Fenix).
- **Stratégie :** Utiliser un module WASM (ex: MobiFlight WASM ou le Client Data Area natif de MSFS) pour exposer les L:Vars (ex: `L:S_OH_SEATBELTS` ou `L:S_OH_APU_MASTER`).
- **Pontage :** Mettre en place un dictionnaire d'équivalence dans `SimConnectService.cs` qui, si le code ICAO de l'avion est `A320` (Fenix), lit la L:Var au lieu de la SimVar standard.

---

## 3. Liste des Tickets (Structure du Design)

- [ ] **TICKET 1 : Extension du Mapping SimConnect (`PlaneDataStruct`)**
  - Ajout des champs (bool/int) pour `Seatbelts`, `APU_Master`, `APU_Bleed`, `Light_Nose`, `Light_Turnoff`.
  - Enregistrement des Data Definitions dans `SimConnectService.cs`.

- [ ] **TICKET 2 : Design Pénalités APU**
  - Ajout de la règle de vérification dans `SuperScoreManager.EvaluatePhaseRules()`.
  - Condition : Déclenchement d'une pénalité unique par vol si APU allumé au-dessus de 10 000ft (hors urgence).

- [ ] **TICKET 3 : Design Constantes de Turbulences & Ceintures**
  - Couplage des variations de G-Force avec l'état `Seatbelts`.
  - Création de la pénalité "Safety Violation: Unsecured Cabin in Turbulence".

- [ ] **TICKET 4 : Logique Éclairage Sol (Nose & Turnoff)**
  - Ajout de la vérification "Nose Light On" obligatoire lors du passage en `TakeoffRoll`.
  - Vérification de l'extinction des Runway Turnoff lights en croisière.

- [ ] **TICKET 5 : R&D et Design WASM pour L:Vars (Fenix)**
  - Étude de l'API WASM de MSFS ou installation du module MobiFlight WASM en pré-requis.
  - Test de lecture de la variable L:Var `S_OH_SEATBELTS` via SimConnect Client Data.
  - Déploiement d'une structure conditionnelle dans `UpdateTelemetry` pour écraser les valeurs standards par les L:Vars si l'avion détecté est "FNX320".
