# Design Gameplay : Générateur de Crise (Crisis Generator)

## 1. Synthèse du Concept
Le **Générateur de Crise** est un moteur d'événements aléatoires ou conditionnels conçu pour briser la monotonie des longs vols (particulièrement en croisière) et tester la réactivité du commandant de bord (le joueur). 

La gestion de ces crises a un impact massif sur le **SuperScore**, l'**Anxiété** et le **Confort** de la cabine, offrant des bonus de leadership si la situation est bien gérée, ou des pénalités sévères si elle est ignorée.

## 2. Mécaniques

### A. Le Moteur de Déclenchement (Trigger Engine)
Les crises ne sont pas purement aléatoires, elles s'adaptent au contexte du vol :
- **Probabilité de base** : Réglable dans les paramètres (Off, Réaliste, Fréquent, Chaos).
- **Déclencheurs contextuels** : 
    - *Altitude et Systèmes* : Voler au-dessus de 10 000 ft sans Packs (pressurisation) déclenche une Dépressurisation.
    - *Température Cabine* : Maintenir une température extrême (> 26°C ou < 18°C) augmente les chances de malaise ou d'agressivité passager.
    - *Phases de vol* : Les crises ne surviennent qu'en montée, croisière ou descente.

### B. Typologie des Crises
1. **Urgence Médicale (Medical Emergency)** : Un passager fait un malaise.
    - *Résolution* : Annonce PA "Assistance Médicale" pour trouver un médecin à bord.
    - *Impact* : Échec entraîne une pénalité de -500 pts de Safety.
2. **Passager Indiscipliné (Unruly Passenger)** : Un passager perturbe la cabine.
    - *Résolution* : Commande Intercom "Maîtriser le Passager" (Restrain).
    - *Impact* : Échec entraîne une pénalité de -200 pts et une chute de confort.
3. **Dépressurisation Cabine** : Perte de pression atmosphérique.
    - *Résolution* : Descente d'urgence immédiate sous 10 000 ft en moins de 5 minutes.
    - *Impact* : Succès +250 pts / Échec -1000 pts.

### C. Interface et Alertes
- **Bannière de Crise** : Une alerte rouge clignotante apparaît avec le type d'urgence.
- **Minuteur (Timer)** : Un compte à bour s'affiche (ex: descente d'urgence ou délai de réaction).
- **Feedback Audio** : Triple carillon d'alarme (`chime_emergency.wav`) au déclenchement.

## 3. Liste des Tickets (État d'Avancement)

- [x] **TICKET 1 : Architecture du Core CrisisEngine**
    - [x] Création de `CrisisManager.cs`.
    - [x] Boucle de décision `Tick()` (toutes les 10s).
- [x] **TICKET 2 : Interface UI & Alarme Sonore**
    - [x] `CrisisBanner` dynamique dans `app.js`.
    - [x] Intégration sonore du carillon d'urgence.
- [x] **TICKET 3 : Scénario "Urgence Médicale"**
    - [x] Déclenchement et résolution via bouton PA dédié.
- [x] **TICKET 4 : Scénario "Passager Indiscipliné"**
    - [x] Résolution via Intercom et impact sur le confort.
- [x] **TICKET 5 : Couplage Télémétrie "Dépressurisation"**
    - [x] Surveillance de l'altitude réelle via SimConnect.
    - [x] Minuteur de 5 minutes pour la descente.
- [x] **TICKET 6 : Paramètres de Fréquence**
    - [x] Slider de probabilité fonctionnel dans les Settings.
- [x] **TICKET 23 : Résolution via Boutons Action**
    - [x] Injection dynamique des boutons de résolution selon la crise active.

---

## 4. Spécifications Techniques

### [BACKEND] CrisisManager.cs
Évalue les probabilités et surveille les conditions de succès (ex: altitude < 10k ft). Gère le timer de défaillance automatique après 10 minutes pour les crises non résolues.

### [FRONTEND] app.js
Affiche la barre d'alerte et propose les actions correctives via le menu Intercom/PA.
