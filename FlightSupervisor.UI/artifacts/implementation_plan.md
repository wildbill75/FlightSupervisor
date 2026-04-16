# Objectif : Refonte de la persistance technique (Historique, Usure, Fuel et Maintenance Ground Ops)

L'objectif global est étendu pour intégrer la Maintenance dans la boucle de gameplay (Turnaround) :
1. **Générateur d'Historique & Pannes (AirframeHistoryGenerator)** : Simule le passé de l'appareil (vols hors-ligne), génère de l'usure, des défauts (Soft/Hard products) selon la qualité de la compagnie.
3. **Moniteur d'Usure en Vol (Wear & Tear)** : Traque en temps réel les erreurs (Tail Strike, Hard Landing, Overpeed Flaps, Hot Brakes virtuels, Cooldown Moteur, Givre).
4. **Gameplay de Maintenance (Ground Ops & UI)** : Un onglet `TECH` est ajouté dans le Dashboard pour appeler la maintenance et réparer les éléments MEL/Inopérants. Les interventions rallongent le Turnaround et envoient des notifications.

## 1. Modifications du Modèle de Données (`AirframeData.cs`)
- **Ajout de propriétés d'usure** : `EngineWear`, `GearAndBrakeWear`, `FlapsWear`, `StructureWear`.
- **Ajout des listes de pannes** : `ActiveDefects` (pannes en cours nécessitant un Tech).

## 2. Générateur (Offline) & Carburant (`AirframeHistoryGenerator.cs`)
- **Simulation Hors-Ligne** : Lors du chargement d'un avion, si son dernier vol date de plus de 12 heures, on lui ajoute quelques vols fantômes (pour refléter l'utilisation réseau).
- **Pannes aléatoires** : Tirage au sort de défauts (Toilettes INOP, WiFi HS, Siège cassé, Défaut technique mineur) selon le niveau de la compagnie (Low-Cost vs Premium).

## 3. Moniteur d'Usure en Temps Réel (`WearAndTearManager.cs`)
- **Hard Landing** : `VerticalSpeed < -400` fpm.
- **Tail Strike** : `Pitch > 11.5` au sol.
- **Flaps Overspeed / Hot Brakes (virtuel)** : Pénalités d'usure calculées sans LVars tierces (basées sur la vitesse et les flaps).
- **Engine Cooldown** : Vérification des 3 minutes avant extinction totale.

## 4. Gameplay de Maintenance au sol (`GroundOpsResourceService` & UI)
- **Interface Dashboard** : Sous "PNC Comms", création d'une ligne "TECH Comms". Des boutons d'appels contextuels apparaissent s'il y a des `ActiveDefects` réparables au sol.
- **Impact Turnaround** : Chaque intervention tech appelée (ex: "Repair WiFi", "Fix Lavatory") coûte du temps de Ground Ops (+X minutes) et bloque le départ.
- **Notifications et Historique** : Les réparations tech notifient l'utilisateur ("Tech: Lavatory fixed") et s'inscrivent dans le tech log de l'avion (`AirframeLogEvent`).

## User Review Required

> [!IMPORTANT]
> **Plan approuvé ?** Si cette architecture te convient (notamment l'impact sur les Ground Ops et la gestion inédite de pannes aléatoires), on lance l'exécution ! 
> La prochaine étape sera la création du document `task.md` pour détailler les commits.
