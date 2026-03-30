# Design Gameplay: Black Box & Scoring

## 1. Overview
This document defines the Flight Telemetry engine, the SuperScore system (positive reinforcement), and the Final Flight Report (Black Box) generation flow.

## 2. Mechanics

### 📈 SuperScore (Positive Scoring)
- **Positive Actions**: Smooth landing, on-time arrival, efficient fuel management, passenger comfort maintenance.
- **Penalties**: Safety violations, extreme maneuvers, ground ops bypass.

### 📼 Black Box (Flight Report)
- Detailed log of all events from Pushback to Arrived.
- Narrative debrief from the Chief Pilot based on performance metrics.

---

## 3. Liste des Tickets

- [ ] **TICKET 40 : Advanced Flight Telemetry**
  - Design of Pitch/Bank violation monitoring.
  - Detection of Gear/Flaps overspeed.
  - G-Force and FPM monitoring at touchdown.

- [ ] **TICKET 41 : Maintenance Log (Tail Strike & Brake Temp)**
  - Detect Tail Strike (> 11° pitch on ground).
  - Calculate Brake Temperature and potential fire risks.

- [ ] **TICKET 42 : Chief Pilot Narrative Debrief**
  - Parser engine to generate human-readable feedback in the final report.

- [ ] **TICKET 43 : Safety Violation - Seatbelts during Refueling**
  - **Règle stricte :** La consigne *Fasten Seatbelts* DOIT être sur OFF pendant l'avitaillement (Refueling) pour permettre une évacuation rapide en cas d'incendie.
  - Si le pilote passe la consigne sur ON pendant que le Refueling est actif, déclencher un malus sévère (-50 à -100 points) au SuperScore.
  - Feedback à afficher : "Vous avez allumé la consigne de sécurité pendant l'avitaillement".

---

## 4. Spécifications de Design Technique

### [BACKEND] SuperScoreManager.cs / FlightLogger.cs
- **Infrastructure Télémétrie** : Monitoring constant des LVARs et SimVars durant toutes les phases.
- **Moteur de Pénalité** : Application en temps réel des malus/bonus selon les seuils (ex: +/- 500ft marge sur 10k ft).
- **Go-Around Logic** : Validation de la procédure stable à 1000ft AGL.
- **Persistence** : Archivage local des rapports au format JSON.

### [FRONTEND] app.js / Flight Report Modal
- **Narrative Debrief** : Analyseur Javascript pour transformer les métriques en texte naturel.
- **History Page** : Visualisation des vols passés stockés localement.
