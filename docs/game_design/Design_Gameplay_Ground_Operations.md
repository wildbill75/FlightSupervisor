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

- [X] **TICKET 34 : Durées d'Opérations Réalistes (A320/B737)**
  - Les temps au sol doivent s'inspirer de la réalité. Pour un *Narrowbody* (A320/B737) de ~150-180 places, les temps moyens de Turnaround sont de 25-35 minutes minimum (Low-Cost) et 45-60 min (Standard Hub).
  - Redéfinir la durée de base de chaque service :
    - *Deboarding* : ~10-15 minutes (flux constant à 20 pax/min)
    - *Cleaning* : ~10-15 minutes (Standard) ou ~5 minutes (PNC Low-Cost)
    - *Catering* : ~15 minutes (Complet) ou ~5 minutes (Snacks Low-Cost)
    - *Refueling* : ~10-15 minutes
    - *Boarding* : ~15-20 minutes
  - *À définir* : Appliquer un ratio "Temps de Jeu" (ex: divisé par 2) pour l'utilisateur, ou forcer les temps réels complets pour la simulation hardcore.

- [X] **TICKET 35 : "Feuilles de Personnage" d'Aéroports (ICAO Tier List)**
  - Afin de simuler l'efficacité et l'organisation variable des différentes escales, chaque aéroport (selon son code ICAO) possèdera un rating ou "Tier" de S à F. Ce rating affecte le multiplicateur global de temps (T) pour les services au sol (Catering, Fuel, Luggage, etc.) et la probabilité (P) de générer un retard/incident au sol (effet domino).
  - **Tier S** (Ultra Efficient / Super-Hub LCC) : Temps réduit de 15% (ex: `EGSS` Stansted, `EIDW` Dublin, `KATL` Atlanta).
  - **Tier A** (Excellent Hub / Standard) : Temps normal, 0% de pénalité (ex: `LFBO` Toulouse, `EDDM` Munich). S'applique aux aéroports non-référencés.
  - **Tier B** (Congestionné / Major Hub) : Pénalité de temps de +15%. Risque de retard accru. (ex: `LFPG` CDG, `EGLL` Heathrow, `EDDF` Frankfurt).
  - **Tier C/F** (Grèves/Sous-effectifs/Délai systématique) : Pénalité de temps de +30%. Fort risque d'incident catering/cleaning introuvable. (ex: `EHAM` Amsterdam l'été, `KEWR` Newark, `LIRF` Rome, etc.).
  - *Design* : Afficher subtilement ce rating dans le Header de l'UI et afficher une Modale/Panel descriptif en haut de la page Ground Operations avec lettre en couleur et description du profil d'infrastructure.
