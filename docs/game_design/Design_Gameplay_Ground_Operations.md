# [AMBRE] Design Gameplay: Ground Operations

## 1. Overview
Ground Operations simulate the critical turnaround phase between flights. The pilot must actively manage ground services (Catering, Cleaning, Boarding, Refueling) within the allocated Scheduled Off Block Time (SOBT).

## 2. Mechanics

### ⏳ Time Management (SOBT)
- A countdown timer dictates the allowed turnaround time.
- Any delay past SOBT negatively impacts pilot SuperScore and passenger anxiety/comfort.

### 🚚 Turnaround Services Restrictions
Ground operations must follow a logical and realistic order. The following strict rules apply:
- **Catering vs Boarding**: Mutual exclusivity. Catering cannot commence if passengers are boarding, and passengers cannot board if Catering is actively loading.
- **Cleaning vs Boarding**: Cleaning cannot be started if passengers have already boarded or have started boarding.
- **Low-Cost Airline Operations**: Low-cost carriers do not contract external cleaning services. Instead of calling a cleaning truck, the Cabin Crew (PNC) is responsible for preparing the cabin. The standard Cleaning service button is disabled and replaced with an indication that the PNC is managing it.

### 👥 Cabin Experience & Boarding Constraint
- **Cabin Experience Suspension**: The calculation and measurement of passenger **Comfort** and **Anxiety** must be strictly suspended while boarding is in progress. The "Cabin Experience" simulation only begins once boarding is completely finished and the cabin doors are verified.

---

## 3. Liste des Tickets (Structure du Design)

- [X] **TICKET 31 : Incompatibilité Catering / Embarquement**
  - Griser/bloquer l'Embarquement si le Catering est en cours (Attente Catering).
  - Le service de Catering est purement et simplement annulé (Skipped) si l'embarquement a débuté ou est terminé, car on ne livre pas les plateaux avec des pax à bord.

- [X] **TICKET 32 : Nettoyage (Cleaning) Interdit avec Passagers**
  - Le service de Nettoyage externe ou PNC est bloqué dès que l'embarquement a débuté (ou si le flux de passagers est > 0).

- [X] **TICKET 33 : Nettoyage Low-Cost (PNC)**
  - Si le profil de la compagnie retenue est typé `LowCost` (ex: EasyJet, Ryanair), modifier l'interface du conteneur de nettoyage.
  - Au lieu de pouvoir appeler la compagnie externe, le bouton est grisé et affiche "PNC Cabin Turnaround" pour simuler que l'équipage gère lui-même le ménage entre les vols.

- [ ] **TICKET 36 : Syncro Dynamique du Manifest ("Rebel Pax")**
  - Le Manifest doit réagir dynamiquement à la consigne *Fasten Seatbelts* même lorsque l'embarquement est terminé et que l'avion est à la porte.
  - Si le commandant éteint la consigne, les passagers doivent se détacher (passer en visuel "Unfastened"). L'état on/off est conservé en direct.
  - **Polissage Visuel (Progression) :** Lorsqu'on allume la consigne, l'attachement des passagers ne doit pas être instantané pour tout le monde. Créer un délai (cascade effect) pour simuler le temps qu'il faut aux passagers pour s'asseoir et boucler leur ceinture.
  - *Détail :* Conserver l'aspect réaliste des "fortes têtes" qui décident de ne pas s'attacher.

- [ ] **TICKET 37 : Compteurs du Manifest (Live Count)**
  - Dans la légende du Manifest, afficher le nombre en direct de passagers pour chaque état. (Ex: "Fasten: 140", "Unfasten: 20", "Empty: 16", "Injured: 0").

- [ ] **TICKET 38 : Missing Passenger Event (No-Show)**
  - L'événement (imprévu) de passager introuvable à la porte ne doit se déclencher **que pendant la phase d'embarquement (Boarding)**. S'assurer qu'il ne se déclenche jamais après.
  - L'issue dépend du choix du pilote :
    - **Choix A (Attendre)** : Allonge le timer d'embarquement. À l'issue du temps supplémentaire, les passagers sont retrouvés (générer un feedback "Passager retrouvé").
    - **Choix B (Fermer les portes sans eux)** : Le ou les passagers manquants sont **définitivement soustraits** du total de passagers prévu par SimBrief.
    - *UI Manifest* : Si le Choix B est pris, le Manifest doit mettre en évidence le nom et le siège du passager, avec une mention claire "N'a pas embarqué".

---

## 4. Spécifications de Design Technique

### [BACKEND] GroundOperationsManager.cs (ou classe similaire)
- **TICKET 31 & 32** : Implémenter des checks de validation stricts dans les méthodes `CanStartCatering()`, `CanStartBoarding()`, et `CanStartCleaning()`. (Si `IsBoarding == true`, `return false;`).
- **TICKET 33** : Lire `AirlineProfile` pour vérifier le type. Exposer une variable `IsLowCostCleaning` via la télémétrie pour en informer l'interface Web.

### [FRONTEND] app.js / Dashboard
- Gérer dynamiquement la classe CSS (`opacity-50 cursor-not-allowed`) sur les boutons conflictuels (Catering, Boarding, Cleaning) via la boucle de rafraîchissement.
- Varier le texte du label "Cleaning Company" vers "Cabin Crew Chores" en fonction du flag Low Cost de la compagnie.

---

## 5. Nouvelles Spécifications : Réalisme et Notes Aéroportuaires (WIP)

- [ ] **TICKET 34 : Variables Dynamiques & Durées d'Opérations (Scaling)**
  - Les temps au sol doivent s'adapter dynamiquement aux variables de la rotation, de l'état de l'avion, et des métriques SimBrief.
  - **Refueling** : Doit être calculé à partir de la quantité de fuel manquante (`(PlanRamp - CurrentFob) / Débit`). *Note: Déjà partiellement implémenté.*
  - **Boarding & Deboarding** : La durée doit être un calcul du nombre total de passagers (`Simbrief Pax Count` x `Temps moyen par PAX`). Ce temps est ensuite pondéré à la hausse ou à la baisse en fonction du score de réputation / d'efficacité de l'équipage PNC (`CrewEfficiency`).
  - **Cargo / Bagages** : Le temps est calculé en fonction du nombre de passagers (bagages soute) ET de la masse du fret (Cargo Weight) extraits de SimBrief.
  - **Cleaning / Ménage** : La durée doit être proportionnelle à l'état de dégradation au moment du "turnaround". Une cabine très sale (`CabinCleanliness < 30%`) nécessitera beaucoup plus de temps qu'une cabine propre.
  - **Catering** : La durée de chargement doit dépendre du nombre exact de plateaux et rations à raitailler (delta depuis la fin du vol précédent).
  - **Water / Waste** : Temps variable selon les niveaux des cuves d'eaux usées à vider et d'eau potable à remplir.

- [X] **TICKET 35 : "Feuilles de Personnage" d'Aéroports (ICAO Tier List)**
  - Afin de simuler l'efficacité et l'organisation variable des différentes escales, chaque aéroport (selon son code ICAO) possèdera un rating ou "Tier" de S à F. Ce rating affecte le multiplicateur global de temps (T) pour les services au sol (Catering, Fuel, Luggage, etc.) et la probabilité (P) de générer un retard/incident au sol (effet domino).
  - **Tier S** (Ultra-efficace / Super-Hub LCC) : Temps réduit de 15% grâce à des infrastructures hyper-optimisées. Risque d'incident très faible. (ex: `EGSS` Stansted, `EIDW` Dublin, `KATL` Atlanta).
  - **Tier A** (Excellent Hub / Aéroports Standards) : Temps normal (0% de pénalité). Risque d'incident standard. (ex: `LFBO` Toulouse, `EDDM` Munich). S'applique par défaut aux aéroports non-référencés.
  - **Tier B** (Méga Hubs / Fort Trafic) : Pas de pénalité de temps de base (leurs immenses moyens logistiques compensent leur taille). **Cependant**, la probabilité d'événements aléatoires (traffic jam sur le tarmac, équipe envoyée au mauvais terminal) est augmentée de 20%. (ex: `LFPG` CDG, `EGLL` Heathrow, `EDDF` Frankfurt).
  - **Tier C/F** (Congestion chronique / Sous-effectifs) : Pénalité de temps de +30%. Fort risque d'incident et de délai permanent. (ex: `EHAM` Amsterdam l'été, `KEWR` Newark, `LIRF` Rome).
  - *Design* : Afficher subtilement ce rating dans le Header de l'UI et afficher une Modale/Panel descriptif en haut de la page Ground Operations avec lettre en couleur et description du profil d'infrastructure.

- [ ] **TICKET 39 : Séquençage Semi-Auto des Opérations (Piloté par la Politique Compagnie)**
  - Développer un mode "Semi-Auto" pour les Ground Ops permettant de lancer des *groupes* d'opérations logiques par phases, plutôt que de cliquer sur chaque service manuellement, respectant ainsi les politiques IATA/IGOM et de la compagnie.
  - **Concept UI** : Un bouton principal intelligent (Primary Action) qui évolue : "START PREPARATION PHASE" -> "START BOARDING PHASE" -> "START FINALIZATION".
  - **Logique des Groupes / Politique Compagnie** :
    - *Phase 1 (Preparation)* : Lance systématiquement le Cargo/Baggage, le Cleaning et le Catering. Si la politique de la compagnie **interdit** l'avitaillement pendant l'embarquement (Legacy Carriers), cette phase lance également le **Fueling**.
    - *Phase 2 (Boarding)* : Se déverrouille une fois Cleaning et Catering terminés (conflits d'allées obligent). Lance l'Embarquement. Si la politique de la compagnie **autorise/force** l'avitaillement pendant l'embarquement (Low-Cost), le **Fueling** est lancé en parallèle ici.
  - **Gameplay & Pénalités (Risk Management)** :
    - Si le Fueling tourne en même temps que le Boarding (parce que c'est la politique Low-Cost), le joueur **doit** avoir fait l'annonce "Refueling in progress" (No Smoking / Ceintures détachées) ou le jeu infligera une forte augmentation d'Anxiété et potentiellement une pénalité de "Safety Violation".
