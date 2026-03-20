# FlightSupervisor

**FlightSupervisor** est un assistant de vol avancé pour Microsoft Flight Simulator (MSFS). Il fournit une interface de suivi en temps réel via une application WPF dédiée et un panneau Bloom (Toolbar) intégré directement dans le simulateur.

## 🚀 Fonctionnalités Principales

- **Connexion SimConnect Native** : Récupération des données de télémétrie en temps réel directement depuis MSFS.
- **Flight Phase Manager** : Détection automatique des phases de vol (Taxi, Takeoff, Climb, Cruise, Descent, Approach, Landing, Parked).
- **Intégration SimBrief** : Importation de vos plans de vol SimBrief pour un suivi précis.
- **Briefing Météo** : Analyse en temps réel des METAR et TAF pour vos aéroports de départ et d'arrivée.
- **In-Game Toolbar Panel** : Un panneau HTML/JS léger accessible directement en vol (idéal pour la VR ou le 2D).
- **Système de Scoring (SuperScore)** : (En développement) Évalue votre pilotage et le respect des procédures opérationnelles.

## 🛠️ Architecture du Projet

Le projet est divisé en deux composants majeurs :

### 1. `FlightSupervisor.UI` (WPF / C#)
L'application principale qui gère la logique métier, la communication SimConnect et l'affichage des données détaillées.
- **Services** : `SimConnectService`, `FlightPhaseManager`, `SimBriefService`, `WeatherBriefingService`.
- **Serveur Panel** : Gère la synchronisation des données avec le panneau en jeu via `PanelServerService`.

### 2. `FlightSupervisor.Panel` (HTML / CSS / JS)
Le panneau "Toolbar" intégré à MSFS.
- Interface moderne et réactive.
- Communication bidirectionnelle avec l'application UI.

## 📥 Installation & Utilisation

1. Lancez Microsoft Flight Simulator.
2. Lancez `FlightSupervisor.UI`.
3. Assurez-vous que la connexion SimConnect est établie (voyant vert).
4. Importez votre vol SimBrief si nécessaire.
5. Ouvrez le panneau Flight Supervisor depuis la barre d'outils de MSFS.

## 📝 Licence

Copyright © 2026 - wildbill75