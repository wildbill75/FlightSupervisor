# Bilan d'État d'Avancement du Projet (State of the Project)
**Date :** 03 Mai 2026
**Application :** Flight Supervisor (True Airmanship)

Ce document dresse le portrait complet de l'application à ce jour, en comparant ce qui est codé et opérationnel vis-à-vis des différents Game Designs initiaux.

---

## PARTIE 1 : Ce qui a été accompli et stabilisé (Ready for Release)

### 1. Architecture Core & UI
- **WebView2 & TailwindCSS** : Le socle de l'application est robuste. L'interface graphique est fluide, moderne (glassmorphism), responsive et communique parfaitement avec le backend C# via un bridge IPC.
- **Système de Profil & Wall of Fame** : La persistance des données du pilote (`Profile.json`) est implémentée. Le tracking des statistiques (Heures de vol/Block Time, Vitesse verticale moyenne, Score moyen) et le système d'accomplissements (Badges de carrière, honneur et honte) sont opérationnels, y compris la remise à zéro (Wipe Career).

### 2. Multi-Leg Rotations & SimBrief
- **Intégration SimBrief** : Importation automatique du dernier plan de vol (OFP), extraction des poids, du nombre de passagers, et de la route.
- **Gestion des Rotations** : Possibilité d'enchaîner plusieurs vols (Legs) avec un vrai processus de Turnaround entre les deux étapes. Le Dashboard met à jour intelligemment la "Current Leg" et la "Global Rotation" sans perdre l'historique.

### 3. Flight Telemetry & SuperScore (SOPs)
- **Flight Phase Manager** : L'état de l'avion est suivi avec précision du sol au sol (Taxi-Out, Takeoff, Cruise, Descent, Approach, Landing, Taxi-In).
- **Analyse de l'atterrissage** : Suivi avancé du Touchdown FPM (taux de chute), de l'impact G (mesuré sur 1 seconde post-touchdown), et du flare time pour déterminer la "Touchdown Zone" exacte.
- **Continuous Monitoring** : Les infractions aux SOPs (vitesse de 250kts sous 10 000 pieds, feux d'atterrissage, risques de Tail Strike) sont calculées en temps réel avec des fenêtres de tolérance réalistes (pas de faux positifs lors des turbulences ou du passage des 10 000 pieds).
- **Logbook** : L'interface affiche clairement les événements, les pénalités associées (ex: -15 pts) et le nouveau SuperScore total dynamiquement.

### 4. Opérations au Sol & Intégration GSX Pro
- **Ground Ops "Passive Listener"** : Intégration transparente et non intrusive de GSX Pro. L'application lit les L-Vars de MSFS pour détecter la connexion des camions de carburant, de catering, et la progression de l'embarquement.
- **Roleplay & Workflow** : Des portes logiques strictes (Gating) sont en place. Par exemple, l'embarquement (Boarding) nécessite d'abord la validation de la feuille de chargement (Loadsheet) par le commandant.

### 5. Communication & Expérience Cabine
- **Command Board (Intercom)** : Interactions possibles avec le chef de cabine (PNC) et appels passagers (PA).
- **Drill-down Menus** : L'interface permet des annonces spécifiques (ex: Excuse pour Retard dû à la météo, à l'ATC, technique, etc.).
- **Ambiance Sonore** : Lecture de fichiers audio locaux (ding dong, annonces génériques) en fonction des interactions UI.

### 6. Intégration ACARS et Télémétrie Météo
- **Requêtes ACARS** : Le service C# télécharge la météo (NOAA/ActiveSky) en temps réel. Le système de requête ACARS permet de récupérer le METAR/TAF en cours de vol pour les aéroports de départ, d'arrivée et de dégagement, avec affichage dynamique dans l'UI.

---

## PARTIE 2 : Ce qu'il reste à faire (Backlog & Roadmap)

### 1. Économie des Passagers & Dégradation du Confort
- **État actuel** : Les systèmes de satisfaction (faim, soif, propreté de la cabine) sont gérés. La télémétrie de la température cabine est **déjà implémentée** (`CabinManager.cs` intègre une inertie thermique, une jauge de dissipation d'inconfort `_thermalDissatisfactionGauge`, et des alertes audio/UI si la température passe sous 19°C ou au-dessus de 25°C). Un cas d'urgence (`CrisisManager.cs`) se déclenche même si la température devient insoutenable.
- **À faire** : Compléter si nécessaire d'autres éléments mineurs de confort passager (ex: gestion fine de l'éclairage cabine en vol de nuit, impact du divertissement IFE).

### 2. TechLog, MEL & Abnormal Procedures
- **État actuel** : Le framework de scoring est prêt à recevoir des pénalités massives pour non-respect des règles de sécurité.
- **À faire** :
  - **MEL (Minimum Equipment List)** : Générer des vols où un équipement est défectueux (ex: APU Inop), forçant le joueur à adapter son démarrage (Start cart) sous peine de pénalité de "Safety Infraction".
  - **Abnormal Procedures** : Gérer la reconnaissance et le tracking de l'exécution des check-lists anormales (ex: Engine Failure, Depressurization).
  - **Crisis Generator** : Déclencher des urgences médicales ou des passagers indisciplinés en vol de croisière, nécessitant peut-être un déroutement (Diversion) comptabilisé dans le Wall of Fame.

### 3. Virtual FO (Copilote Autonome)
- **État actuel** : Le design définit la possibilité de déléguer des tâches (`Design_Gameplay_Virtual_FO.md`).
- **À faire** : Coder l'intelligence artificielle du copilote. Utiliser SimConnect pour que le programme puisse écrire (et pas seulement lire) les L-Vars afin d'allumer physiquement les phares, rentrer le train, et régler la radio à la demande du joueur.

### 4. In-Game Panel MSFS (Mode VR & Moniteur Unique)
- **État actuel** : L'application tourne dans une fenêtre externe WPF (Windows Presentation Foundation).
- **À faire** : Emballer le code UI (HTML/JS) dans un widget MSFS Toolbar utilisant les WebSockets pour communiquer avec le backend C#, permettant aux utilisateurs VR de garder Flight Supervisor sous les yeux pendant tout le vol.

---
**Conclusion**
La fondation de *True Airmanship* est non seulement solide, mais ses mécaniques centrales de vol de routine (Préparation, Roulage, Vol, Score, Turnaround, Persistance) sont abouties et prêtes à l'emploi. Les prochaines phases de développement relèvent de la complexification (Urgences, Abnormal Procedures, Pannes) et de l'immersion ultra-poussée (Confort des passagers à la carte).
