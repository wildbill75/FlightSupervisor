# Flight Supervisor - Assistant de Vol Avancé pour MSFS

**Flight Supervisor** est un compagnon de vol intelligent pour Microsoft Flight Simulator, conçu pour évaluer la qualité du pilotage à travers une approche positive (le **SuperScore**) et assurer le confort des passagers en temps réel.

## 🌟 Vision du Projet

Le projet repose sur trois piliers fondamentaux :
1.  **Philosophie Positive (SuperScore)** : Récompenser les bonnes décisions (Kiss Landing, gestion des turbulences) plutôt que de simplement punir les erreurs.
2.  **Immersion Totale** : Une interface intégrée (Toolbar Panel) compatible VR/2D et une "Boîte Noire" qui traque chaque paramètre critique.
3.  **Intelligence Opérationnelle** : Analyse automatique des METAR/TAF, intégration SimBrief et gestion dynamique des phases de vol.

---

## 🚀 Fonctionnalités Actuelles & Prévues

### ✈️ Gestion du Vol
- **Flight Phase Manager** : Détection automatique des phases (Taxi, Take-off, Climb, Cruise, etc.).
- **SimConnect Engine** : Monitoring haute fréquence (20Hz) de la télémétrie (G-Force, Vertical Speed, Pitch/Bank).
- **Anti-Slew/Pause Protection** : Filtrage des données lors de l'utilisation du mode transposition ou de la pause.

### 📋 Préparation & Météo
- **SimBrief API** : Importation directe des plans de vol opérationnels (OFP).
- **Smart Weather Briefing** : Synthèse textuelle intelligente des conditions météo (METAR/TAF) sous forme de "Briefing Commandant".
- **Calculateur de Limites** : (En cours) Base de données des limites "Go/No-Go" selon l'appareil.

### 🎭 SuperScore & Passagers
- **Pilier Pilotage** : Évaluation technique (respect des limites structurelles, handling).
- **Pilier Commercial** : Gestion du confort passager (température cabine, signal Seatbelts en turbulence).
- **Bonus Dynamiques** : Multiplicateurs de score basés sur la difficulté météo (Crosswind, visibilité).

### 🖥️ Interface Utilisateur
- **WPF Preparation UI** : Application de bureau pour le paramétrage et le briefing pré-vol.
- **In-Game Toolbar Panel** : Interface HTML/JS déportée affichant en direct :
    - Phase de vol actuelle.
    - Horaires ZULU et Timetable (Prévu vs Réel).
    - Feedbacks dynamiques (Notifications de bonus/pénalités).

---

## 🏗️ Architecture Technique

- **Backend** : C# .NET 8.0 (WPF).
- **Communication Sim** : SimConnect (DLL native).
- **In-Game Overlay** : Bloom Toolbar HTML/JS/CSS.
- **Bridge** : Serveur WebSocket/HTTP local pour la communication UI <-> Panel.
- **Aircraft Compatibility Layer** : Providers spécifiques pour gérer les L-Vars (Fenix A320, PMDG, iniBuilds).

---

## 📍 Road Map

- [x] Intégration API SimBrief.
- [x] Moteur de Briefing Météo intelligent.
- [x] Machine d'états des phases de vol.
- [x] Architecture de base du Panel Toolbar.
- [ ] Packaging Community Folder pour MSFS.
- [ ] Moteur de Score final (SuperScore Interpretation).
- [ ] Adaptateur spécifique Fenix A320 (Seatbelts/Engines L-Vars).
- [ ] Enregistrement "Boîte Noire" (SQLite logging).

---

## 🛠️ Installation

1. Clonez le dépôt dans votre dossier de développement.
2. Compilez la solution `FlightSupervisor.slnx` via Visual Studio.
3. (À venir) Copiez le dossier `flight-supervisor-panel` dans votre dossier `Community`.
4. Lancez le simulateur, puis l'application UI.

---

**Développé par wildbill75 - 2026**