# Design Gameplay: Black Box & Scoring

## 1. Overview
This document defines the Flight Telemetry engine, the SuperScore system (positive reinforcement), and the Final Flight Report (Black Box) generation flow.

## 2. Refonte du Système de Notation (5 Catégories)

Le système de notation de Flight Supervisor est organisé en cinq catégories distinctes pour refléter fidèlement le métier de commandant de bord. La note globale et le *Final Flight Report* décomposeront la performance dans ces catégories :

1. **FLIGHT PHASE FLOWS** : Exécution stricte et systématique des flows de la cabine/cockpit relatifs aux phases de vol.
2. **COMMUNICATIONS (PA+PNC+TECH+CO)** : La qualité et la ponctualité des annonces passagers, la gestion des hôtesses, le reporting à la maintenance et à la compagnie.
3. **AIRMANSHIP** : La qualité intrinsèque du pilotage manuel, l'anticipation, le respect des SOP et l'aisance technique.
4. **MAINTENANCE** : L'usure de l'appareil (limites structurelles, impact des atterrissages, température de freins).
5. **ABNORMAL OPERATIONS** : La prise de décision, la vitesse de réaction et l'adaptation lors de scénarios de crise (médical, pannes, MTO extrême).

### 2.1. Détail - Catégorie 1 : FLIGHT PHASE FLOWS

**Mécanique de validation :**
À la **fin de chaque phase** (le calcul se fait précisément au franchissement de la transition vers la phase suivante), le code fait un bilan direct de tous les paramètres du flow. À la fin du vol, chaque état est restitué visuellement dans le *Black Box Report* avec un indicateur (Rouge = Raté, Vert = Réussi).

*Voici les états exacts à enregistrer pour chaque phase :*

#### a) GROUND FLOW (AtGate / Turnaround)
*   **Parking brakes**: ON
*   **Thrust Lever**: IDLE
*   **Whipers (both)**: OFF
*   **Flaps**: ZERO
*   **GND Spoilers**: RETRACTED
*   **Engine mode selector**: NORM
*   **Engine Master (both)**: OFF
*   **Gear Lever**: DOWN
*   **Strobe light**: AUTO
*   **Landing lights**: OFF/RETRACTED
*   **Taxi lights**: OFF
*   **Rnw Turnoff**: OFF
*   **Rudder trim**: ZERO/RESET
*   **Seat belt**: ON si refuel terminé. OFF si refuel en cours.
*   **ANTI SKID**: ON

#### b) PUSHBACK FLOW
*   **BEACON LIGHT**: ON 
*   **Seat belt**: ON
*   **Thrust Lever**: IDLE
*   **Whipers (both)**: OFF
*   **Flaps**: ZERO
*   **GND Spoilers**: RETRACTED
*   **Parking brakes**: ON
*   **Taxi lights**: OFF
*   **Gear Lever**: DOWN
*   **Strobe light**: AUTO
*   **Landing lights**: OFF/RETRACTED
*   **Taxi lights**: OFF
*   **Rnw Turnoff**: OFF

#### c) TAXI OUT FLOW
*   **Seat belt**: ON
*   **Taxi lights**: ON
*   **Gear Lever**: DOWN
*   **Strobe light**: AUTO
*   **Landing lights**: OFF/RETRACTED
*   **Flaps**: 1, 2 or 3. NEVER 0 or FULL.
*   **Rnw Turnoff**: OFF
*   **GROUND SPEED**: INF à 30 knts (Tolérance d'infraction max de 5 secondes)

#### d) TAKE OFF / LINEUP FLOW
*   **Landing lights**: ON
*   **Strobe light**: ON
*   **Taxi lights**: TO
*   **Rnw Turnoff**: ON
*   **GND Spoilers**: ARMED
*   **AUTO BRAKES**: Max
*   **Seat belt**: ON

#### e) INITIAL CLIMB FLOW
*   **Gear Lever**: UP
*   **Taxi lights**: OFF
*   **Rnw Turnoff**: OFF
*   **Seat belt**: ON

#### f) CLIMB FLOW
*   **Gear Lever**: UP
*   **Taxi lights**: OFF
*   **Rnw Turnoff**: OFF
*   **Flaps**: ZERO
*   **GND SPOILERS**: RETRACTED
*   **BELOW 10000 feet**: IAS INFÉRIEUR à 250 knts (tolérance +/- 10 knts)
*   **ABOVE 10000 feet** (tolérance +/- 1000 feet) :
    *   **Seat belt**: OFF or ON (à discrétion)
    *   **Landing lights**: OFF/RETRACTED

#### g) CRUISE FLOW
*   **Gear Lever**: UP
*   **Taxi lights**: OFF
*   **Rnw Turnoff**: OFF
*   **Flaps**: ZERO
*   **GND SPOILERS**: RETRACTED
*   **Seat belt**: OFF or ON (à discrétion)
*   **Landing lights**: OFF/RETRACTED

#### h) DESCENT FLOW
*   **Gear Lever**: UP
*   **Taxi lights**: OFF
*   **Rnw Turnoff**: OFF
*   **BELOW 10000 feet**: IAS INFÉRIEUR à 250 knts (tolérance +/- 10 knts)
*   **BELOW 10000 feet** (tolérance +/- 1000 feet) :
    *   **Seat belt**: ON 
    *   **Landing lights**: ON

#### i) APPROACH FLOW
*   **Gear Lever**: DOWN
*   **Taxi lights**: TO
*   **Rnw Turnoff**: ON
*   **GND SPOILERS**: ARMED
*   **FLAPS**: 3 or Full

#### j) LANDING FLOW
*   **Gear Lever**: DOWN
*   **Taxi lights**: ON
*   **Rnw Turnoff**: OFF
*   **GND SPOILERS**: RETRACTED
*   **Strobe light**: AUTO
*   **Landing lights**: OFF/RETRACTED

#### k) TAXI IN FLOW
*   **Seat belt**: ON
*   **Taxi lights**: ON
*   **Gear Lever**: DOWN
*   **Strobe light**: AUTO
*   **Landing lights**: OFF/RETRACTED
*   **Flaps**: ZERO
*   **Rnw Turnoff**: OFF
*   **GROUND SPEED**: INF à 30 knts (Tolérance d'infraction max de 5 secondes)

#### l) ARRIVED FLOW
*   **BEACON LIGHT**: OFF
*   **Thrust Lever**: IDLE
*   **Whipers (both)**: OFF
*   **Flaps**: ZERO
*   **GND Spoilers**: RETRACTED
*   **Engine mode selector**: NORM
*   **Engine Master (both)**: OFF
*   **Gear Lever**: DOWN
*   **Strobe light**: AUTO
*   **Landing lights**: OFF/RETRACTED
*   **Taxi lights**: OFF
*   **Rnw Turnoff**: OFF
*   **Rudder trim**: ZERO/RESET
*   **Seat belt**: OFF

---

### 2.2. Détail - Catégorie 2 : COMMUNICATIONS (PA+PNC+TECH+CO)

**Mécanique de validation :**
Enregistre la complétion (Oui/Non) de chaque annonce attendue durant le vol. Toutes les annonces influencent la satisfaction ou l'anxiété, mais l'oubli des annonces de **SÉCURITÉ** (destinées aux PNC) génère des pénalités massives de points.

#### a) Annonces Sécurité & Équipage (PNC) - **CRITIQUE**
*   **Prepare Cabin for Takeoff** : L'équipage doit être prévenu *avant* l'alignement pour s'asseoir. Oublier = DANGER.
*   **Prepare Cabin for Landing** : Pareil, avant l'approche finale. Oublier = DANGER.
*   **Release Cabin (Décollage/Atterrissage)** : Autoriser l'équipage à se détacher une fois l'avion stabilisé en montée ou arrivé en porte.

#### b) Annonces Passagers (PA) - **SATISFACTION**
*   **Welcome PA** : Annonce de bienvenue. Influe fortement sur le confort global. Le skipper peut la sauter s'il ne veut pas parler, mais il perd le bonus de relation client.
*   **Approach PA (Météo/Arrivée)** : Annonce de préparation descente. Baisse l'anxiété.
*   **Abnormal / Delay PA** : Si un délai est détecté (holding, etc.), communiquer permet de dissiper l'impatience.

---

### 2.3. Détail - Catégorie 3 : AIRMANSHIP

**Mécanique de validation (Pondération en fin de phase) :**
L'Airmanship sanctionne ou récompense la qualité du **pilotage manuel** et la **précision technique** du commandant de bord. Contrairement à une simple perte de points instantanée, le code va comptabiliser la durée et la fréquence des infractions de pilotage. *Le calcul des pénalités se fait en fin de phase par pondération (ex: a-t-il freiné comme un bourrin 1 fois, ou 10 fois pendant le taxi ?).*

#### a) Ground Handling (Roulage)
*   **Taxi Overspeed / Virages Serrés** : Virages brusques au sol à haute vitesse.
*   **Freinages Brusques (Harsh Braking)** : Pilage excessif des freins en ligne droite.

#### b) Décollage (Takeoff)
*   **Pitch & Bank Violations** : Tirer sur le manche violemment (*"comme un connard"* au décollage).
*   **Centerline Tracking** : Précision du maintien sur l'axe de piste (Décollage et Atterrissage).

#### c) Approche & Atterrissage (Landing)
*   **Unstable Approach / Go-Around Mandated** : Si l'approche n'est pas stabilisée sous 1000ft (vitesse excessive, mauvais plan de descente, pente > 1000fpm), le copilote simulé (Copilot Logic) exigera une remise des gaz. Forcer l'atterrissage après une approche instable est une violation sévère de sécurité.
*   **Airmanship Reward (Pilotage Manuel)** : Maintien de l'approche sans Autopilot / Auto-Thrust. La déconnexion anticipée démontre la maîtrise du pilote.
*   **Airmanship vs Météo (La distinction vitale)** : Le système jugera de l'intelligence du commandant face aux conditions extrêmes :
    *   **Fort Vent de Travers (> 15kts)** : Les limites de l'Autoland sont généralement dépassées. Le pilote DOIT atterrir en manuel. Un atterrissage manuel réussi sous fort vent voit son bonus d'Airmanship **multiplié par 2 ou 3**.
    *   **Faible Visibilité / Brouillard (< 600m)** : Sous le seuil de décision standard (CAT I). Atterrir en manuel avec une visibilité nulle est une **violation sévère de sécurité** (Busted Minimums). L'équipage DOIT recourir à un **Autoland (CAT II/III)** et laisser l'Autopilot engagé jusqu'au sol. L'exécution correcte de l'Autoland dans ces conditions est récompensée.

#### d) Atterrissage (Landing)
*   **Softness (G-Force / FPM)** : Impact doux vs atterrissage dur.

#### e) Turbulence & Passenger Comfort (En Vol)
*   **Turbulence Management** : Étant difficilement perceptible physiquement devant l'écran, le système analysera la durée d'exposition aux turbulences (Light to Moderate). S'y exposer longuement sans allumer les Seatbelts entraînera l'intervention du copilote et une sanction sur la note de confort passager.
*   **Centerline deviation** : Précision sur l'axe.
*   **Crosswind Handling** : Récompense pour avoir atterri manuellement avec un vent de travers prononcé.

---

### 2.4. Détail - Catégorie 4 : MAINTENANCE

**Mécanique de validation :**
Enregistre toute usure ou dégât structurel anormal pour alerter l'équipe de maintenance. Ce système sanctionne les actions pouvant immobiliser l'avion.

#### a) Structure & Atterrissage
*   **Tail Strike** : Pitch excessif au décollage occasionnant un choc sur la queue de l'appareil. Une inspection devra être signalée !
*   **Hard Landing (G Loads)** : Atterrissage violent (> 600fpm ou charge de G monstrueuse). Nécessite l'inspection des trains d'atterrissage.

#### b) Limites Opérationnelles
*   **Flaps/Gear Overspeed** : Sortie et maintien des volets ou du train à une vitesse supérieure à la limite structurelle.
*   **Température des Freins (Brake Temp)** : Refroidissement insuffisant ou surchauffe des freins. Si la température critique est atteinte au sol, le commandant **doit** faire une annonce PA pour prévenir les passagers d'une attente technique sur le tarmac, voire prévoir un retour à la porte (Return to Gate). L'oubli de cette annonce aggravera le score.

---

### 2.5. Détail - Catégorie 5 : ABNORMAL OPERATIONS & COMPANY IMAGE

**Mécanique de validation :**
En cas de situation d'urgence ou d'anomalie de vol, le système audite la qualité de l'exécution procédurale du commandant. De plus, son pilotage global impacte la "Brand Image" (Image de Compagnie / Ressenti clientèle).

#### a) Phase de Go-Around (Remise des gaz)
*   *Note : Le RTO (Rejected Takeoff) est pour le moment ignoré du code de scoring, le jeu ne gérant pas encore de pannes moteurs.*
*   Le **Go-Around n'est pas qu'un déclencheur, c'est une Phase de Vol à part entière** : Dès que les gaz passent sur TOGA en phase d'approche, le gestionnaire de phase bascule sur `GO_AROUND`.
*   **Contrainte PNR (Point de Non Retour)** : Si les Reverses ont été enclenchés, le Go-Around est suicidaire (-1000 pts).
*   **Procédure dans la Phase** : Rentrée des volets d'un cran, Pitch Up, train d'atterrissage sur UP.
*   **Communication** : Obligation vitale d'annoncer la remise des gaz aux passagers une fois l'avion stabilisé, sous peine de panique générale en cabine.

#### b) Déroutement (Diversion & Alternate Decision Engine)
*   **L'Acte de Déroter** : Prendre la décision de se dérouter (vers un Alternate) est **toujours positif pour la sécurité**. L'ego du pilote est réprimé au profit du bon sens.
*   **Le Choix Stratégique (Sécurité vs Compagnie)** : Le système évaluera si l'Alternate choisi était le plus intelligent. 
    *   *Sécurité* : L'alternate a-t-il une meilleure météo (Visibilité, Vent) que la destination ?
    *   *Économie* : Est-il proche ? (Une distance trop grande coûte cher en fuel et taxes).
*   Se dérouter vers l'Alternate optimal (Météo claire + Proximité) offre un bonus d'Image de Marque massif pour le commandant. Se dérouter vers un aéroport engorgé, plus loin, et sous la tempête sera critiqué par la compagnie.

#### c) Ressenti Clientèle & Image de Marque
*   Sert de baromètre "Qualité" pour la compagnie. Le comportement global (atterrissages doux, interactions vocales régulières, respect des consignes) bonifie la perception de la compagnie. Inversement, une succession de vols brutaux sans communication dégradera fortement la note d'image de la compagnie aérienne associée à la rotation.

---

### 🧮 PROPOSITION DE POINTS (SuperScore Matrix V2)

Le barème de points doit être suffisamment permissif pour tolérer l'inconfort passager (points mineurs), mais **punitif** sur la sécurité. L'oubli de communication en cas d'anomalie est lourdement sanctionné car le "Panic Management" fait partie intégrante de l'Airmanship.

| Catégorie | Action / Événement | SuperScore Delta | Explication |
| :--- | :--- | :--- | :--- |
| **FLOWS (Cat 1)** | Flow de Phase accompli (ex: Climb, Descent à 100%) | **+50** | Flow parfait et propre. |
| **FLOWS (Cat 1)** | Oubli mineur (Lights, Wipers) | **-20** | Non respect des clous d'une SOP. |
| **FLOWS (Cat 1)** | Infraction de Vitesse Taxi / 250 knts | **-50** | Modéré. Tolérance de 5s appliquée. (+ speedbrakes pris en compte) |
| **FLOWS (Cat 1)** | Violation Sécurité Grave (Seatbelts relevées < 10000ft, Flaps Overspeed) | **-500** | Retrait massif, impact de sécurité direct. |
| **COMMS (Cat 2)** | *Prepare Cabin for Takeoff / Landing* OUBLIÉ | **-400** | Risque vital pour l'équipage cabine d'être debout. |
| **COMMS (Cat 2)** | Annonce PA Bienvenue / Descente FAIT | **+30** | Bonne relation client. |
| **COMMS (Cat 2)** | Oubli Annonce Passagers après RTO / Go-Around | **-200** | Laisse la cabine dans une panique absolue. |
| **AIRMANSHIP (Cat 3)** | Décollage violent (Pitch/Bank excessif) | **-200** | Manque de finesse et d'anticipation. |
| **AIRMANSHIP (Cat 3)** | Atterrissage dur (Hard Landing) | **-150** | Comfort des passagers ruiné. |
| **AIRMANSHIP (Cat 3)** | Pilotage Manuel Approche (Standard) | **+100** | Récompense du vrai "Airmanship". |
| **AIRMANSHIP (Cat 3)** | Atterrissage Manuel Vent de Travers Extrême | **+300** | Multiplicateur de maestria du commandant (Crosswind > 15kts). |
| **AIRMANSHIP (Cat 3)** | AUTOLAND Exécuté avec succès (Visibilité < 600m) | **+150** | Respect strict des procédures CAT III LVP. |
| **AIRMANSHIP (Cat 3)** | Violation des Minimas (Atterrissage manuel si Visibilité < 600m) | **-500** | Danger mortel de se poser à l'aveugle sans référence visuelle. |
| **MAINTENANCE (Cat 4)**| Tail Strike décelé | **-500** | Dommage structurel lourd, avion immobilisé (AOG). |
| **MAINTENANCE (Cat 4)**| Atterrissage Très Violent (Inspection requise) | **-300** | Inspection des trains obligatoires suite au G-load extrême. |
| **ABNORMAL (Cat 5)** | Go-Around bien exécuté au sein de sa Phase (Flaps + Comms) | **+200** | Récompense absolue de la prise de décision salvatrice et sûre. |
| **ABNORMAL (Cat 5)** | Tentative de Go-Around post-Reverses | **-1000** | Infraction monumentale aux règles de physique et procédures. |
| **ABNORMAL (Cat 5)** | Déroutement Optimal (Meilleure Météo / Proximité) | **+250** | Excellente gestion managériale et sécuritaire de crise. |

*Note sur la tolérance de vitesse (IAS under 10k)* : Le système prendra en considération le déploiement des Speedbrakes avant d'allouer la pénalité sévère, prouvant que le pilote essayait de réduire sa vitesse malgré le vent arrière.

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
