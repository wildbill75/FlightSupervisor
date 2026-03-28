# Design Gameplay: PNC Communication & Cabin Preparation [AMBRE]

> [!WARNING]
> **STATUT : AMBRE (Design Partiellement Validé - Nouveaux Tickets en attente)**

## 1. Overview
This feature deepens the interaction between the Captain and the Cabin Crew (PNC) during Taxi and Descent. It transforms simple commands into strategic constraints, and turns the Intercom into a vital tool for assessing passenger morale.

## 2. Mechanics

### 📋 The Cabin Preparation Gauge
The Captain must command the PNC to secure the cabin twice per flight.
1. **Taxi Out**: Prepare for Takeoff.
2. **Descent**: Prepare for Landing (starts at the beginning of descent).

- **Visual Gauge**: A progress bar (Amber -> Green) indicating readiness level.
- **Dynamic Duration**: 1.5 to 5 minutes based on passenger load, PNC energy, and random incidents (e.g., bin issues).

### 💺 "Seats for Takeoff / Landing" Command
Once the cabin is prepared (100%), the Captain must give the final "Seating" command.
- **Hard Constraint**: These buttons are disabled until the cabin preparation gauge reaches 100%.
- **Forced Override**: If the Captain forces the "Seats" command before 100%, a **Major SuperScore Penalty** is applied.

### ⚠️ Operational Consequences & Service
- **Unready Takeoff/Landing**: Acceleration for takeoff or crossing 500ft AGL without a secured cabin results in a **Dishonorable** safety penalty.
- **Aggressive Maneuvers**: Sharp turns or high taxi speeds interrupt the PNC's work.
- **Règle des 10 000 ft** : Le service ne progresse qu'au-dessus de cette altitude.

### 📞 Dynamic Intercom Feedback (Anxiety & Comfort)
When the Captain calls the PNC via the Intercom to ask for a status update, the response **must no longer be a static "Everything is fine".**
The generated response must dynamically reflect the **current state of Passenger Anxiety and Comfort**, with the following strict exceptions:
- **Boarding Priority**: If the Intercom is used while boarding is still in progress (or before it has completed), the PNC must simply reply: *"We are currently waiting for boarding to finish, Captain."* The general cabin mood isn't reported until the doors are closed.
- If a delay caused anxiety to spike (e.g., SOBT passed), the PNC must mention the passengers' impatience.
- If severe turbulence has decreased comfort, the PNC must report that passengers are feeling sick or nervous.

---

## 3. Liste des Tickets (Structure du Design)

- [x] **TICKET 7 : Backend Prep Logic**
- [x] **TICKET 8 : Service Scaling Logic**
- [x] **TICKET 9 : UI Prep & Service Gauge**
- [x] **TICKET 10 : PNC Intercom Feedbacks** (Cabin ready confirmations)
- [x] **TICKET 11 : Safety Enforcement**

- [ ] **TICKET 12 : Intercom Dynamic Feedback (Anxiety/Comfort)**
  - Lier la réponse de l'Intercom ("How are things in the back?") aux valeurs actuelles de `PassengerAnxiety` et `ComfortLevel`.
  - Créer des conditions textuelles spécifiques pour les retards (Delay > SOBT), les turbulences, et le catering avorté.

---

## 4. Spécifications de Design Technique

### [BACKEND] CabinManager.cs
- **TICKET 12** : Modifier la méthode d'appel Intercom (ex: `HandleIntercomRequest()`) pour évaluer `PassengerAnxiety`.
  - Si en cours d'embarquement -> "Nous attendons la fin de l'embarquement, Commandant."
  - Si `Delay > 0` et `Anxiety > 30` -> "Les passagers s'énervent à cause du retard."
  - Si `Turbulence` récente -> "Ça secoue pas mal, ils sont tendus."
  - Sinon -> "Tout se passe bien à l'arrière."

### [FRONTEND] app.js / index.html
- Le Dashboard doit simplement afficher la string générée par `CabinManager` dans la console des messages d'équipage. Rien à coder côté JS, le C# s'occupe de la logique conditionnelle.
