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

---

## 4. Spécifications de Design Technique

### [BACKEND] GroundOperationsManager.cs (ou classe similaire)
- **TICKET 31 & 32** : Implémenter des checks de validation stricts dans les méthodes `CanStartCatering()`, `CanStartBoarding()`, et `CanStartCleaning()`. (Si `IsBoarding == true`, `return false;`).
- **TICKET 33** : Lire `AirlineProfile` pour vérifier le type. Exposer une variable `IsLowCostCleaning` via la télémétrie pour en informer l'interface Web.

### [FRONTEND] app.js / Dashboard
- Gérer dynamiquement la classe CSS (`opacity-50 cursor-not-allowed`) sur les boutons conflictuels (Catering, Boarding, Cleaning) via la boucle de rafraîchissement.
- Varier le texte du label "Cleaning Company" vers "Cabin Crew Chores" en fonction du flag Low Cost de la compagnie.
