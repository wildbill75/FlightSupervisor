# Design Document: Fuel Planning and Cockpit Briefing

## Vue d'Ensemble
L'objectif de cette fonctionnalité est d'immerger le joueur dès la préparation du vol en simulant une procédure de **Fuel Planning** réaliste. Au lieu de se contenter de charger une valeur aveugle depuis le manifeste SimBrief, le Flight Supervisor permettra au commandant (joueur) de réévaluer, ajuster et valider son **Block Fuel** depuis l'interface avant de consolider les paramètres du vol.

## Fonctionnement Actuel de l'Ingestion (Phase 1 - Terminée)
Actuellement, lorsque l'utilisateur initie une requête "Fetch SimBrief" (que ce soit pour le premier vol de la rotation _Pristine_ ou lors d'une succession Multi-Leg), l'application exécute ceci :
1. Extraction silencieuse de la donnée XML (e.g., `flight_plan.fuel.plan_ramp`).
2. Réponse sérialisée envoyée du backend (C#) vers le frontend (JS) sous l'objet `manifestData`.
3. Le planificateur ne modifie pas encore cette donnée, et les services (comme le `CabinManager` ou `GroundOpsManager` si implémentés) peuvent y faire référence passivement.

## Objectif (Phase 2 - Formulaire de Fuel Planning)
La refonte à venir intégrera une sous-catégorie ou un widget de "Fuel Planning" directement sur l'écran **Briefing** (le Dashboard affiché avant le repoussage).

### Workflow Utilisateur Attendu
1. **Extraction par Défaut** : L'écran de Briefing pré-remplit le champ **Block Fuel** avec la valeur extraite de SimBrief. Une analyse rapide s'affiche (ex: *Trip + Alternate + Final Reserve + Contingency*).
2. **Interactive Tweak** : Le joueur peut éditer l'estimation (soit en ajoutant manuellement +500kg d'Extra Fuel lié aux conditions MTO, soit via un champ modifiable).
3. **Consolidation** : Une fois la révision passée, le joueur clique sur un bouton "Validate Fuel" (ou "Confirm Block Fuel").
   - Ce bouton gèlera la valeur et actera l'objectif pour le camion de Refueling.
   - Les appels intercom (si existants) avec l'agent de piste pourront utiliser cette donnée consolidée (`"Captain, here is the fuel slip, we loaded XXXX kg..."`).

### Contraintes Architecturales
- La validation du carburant sera *indispensable* pour **autoriser le chargement physique** du Fuel lors de la Ground Op `Refueling`. Tant que le "Fuel Load Sheet" n'est pas validé par le commandant, le camion de refueling peut arriver à l'avion et se connecter (Logistique / Approche), mais le pompage restera en attente (`Waiting for Fuel Sheet`).
- Si le vol est chargé en mode _Fast_, l'application auto-validera le Request Fuel basé strictement sur le plan Simbrief pour accélérer le "chemin critique".

### Interface Recommandée (Draft UI)
- Le widget de Fuel devra adopter le style EFB (Electronic Flight Bag). 
- Un cadran sobre affichant :
  - `TRIP`: 5231 kg
  - `RES`: 1200 kg
  - `ALTN`: 900 kg
  - `EXTRA`: [ Champ Texte Libre / Boutons +100kg ]
  - `TOTAL BLOCK`: **XXXX kg**

Cette implémentation permet d'augmenter significativement le lore "Pilote d'Airliner" sans alourdir drastiquement la télémétrie en cours de vol.
