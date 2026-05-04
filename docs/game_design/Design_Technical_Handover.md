# Handover pour la prochaine session

**Date :** 04 Mai 2026
**Application :** Flight Supervisor (True Airmanship)

## Ce qui a été accompli lors des dernières sessions
1. **Wear Visualization (Airframe)** :
   - Remplacement des pannes MEL "Hard" par des pannes liées au train d'atterrissage, volets et structure (freins, usure pneu, etc.) dans `AirframeHistoryGenerator.cs`.
   - Logique de calcul dynamique du `MaintenanceGrade` basée sur l'usure la plus élevée implémentée dans `AirframeManager.cs`.
   - L'interface `airframe_window.html` affiche un panneau "Systems Health" avec 4 barres de progression liées aux données JSON envoyées par le backend, avec un changement de couleur dynamique (Vert, Orange, Rouge) selon les seuils d'usure.
2. **Refonte de l'interface Weather Briefing** :
   - Refonte totale du backend (`BriefingData.cs` & `WeatherBriefingService.cs`) avec des objets fortement typés, isolant les variables météorologiques clés (QNH, Temp/Dew, Wind, Visibility, CloudBase) tout en conservant les commentaires "style pilote".
   - Refonte du frontend avec rendu HTML dynamique via TailwindCSS, avec rapports METAR/TAF dans un bloc de style terminal et des badges pour les variables clés.
   - La boucle ACARS de `MainWindow.xaml.cs` a été ajustée pour rafraîchir l'interface Web après l'actualisation des données météo.
3. **Logique RTO & Autobrake MAX** :
   - Possibilité de récupérer manuellement un décollage rejeté (RTO) : si la manette est replacée sur Takeoff Thrust (>60%) et la vitesse est de plus de 40kts, la phase repasse en `Takeoff` (évitant de forcer le `TaxiIn`).
   - Mappage de la variable locale Fenix `L:S_MIP_AUTOBRAKE_MAX` via le client WASM/LVar.
   - Ajout d'une pénalité automatique (`ScoreFlowEvaluator`) si le joueur tente de décoller sans l'Autobrake sur MAX.
4. **Correction de Crash (NullReferenceException)** :
   - Le crash silencieux au démarrage a été résolu. Il était causé par un ordre d'instanciation incorrect où le `ScoreFlowEvaluator` tentait de s'abonner aux événements du `WearAndTearManager` avant sa création. L'application compile et se lance désormais correctement.

## Prochaines Étapes pour le prochain agent
1. **Tests en vol et Validation UI** :
   - Vérifier que l'UI de Briefing Météo se met correctement à jour (sans clignoter et sans casser la mise en page CSS) lors des rafraîchissements périodiques ACARS (~toutes les 15 minutes).
   - Valider que les alertes visuelles (surbrillance rouge/orange sur les badges météorologiques) fonctionnent bien lorsque les minimums sont atteints (ex: vent fort, visibilité très réduite).
   - Valider le comportement du RTO en conditions réelles dans le simulateur, et vérifier le déclenchement du malus de l'Autobrake.
2. **Fonctionnalités à Développer (Optionnelles, selon les priorités du joueur)** :
   - **ACARS Manuel** : Implémenter une UI de demande ACARS manuelle fonctionnelle (style MCDU) permettant au joueur de "requêter" lui-même l'actualisation via un bouton dédié dans le panneau de vol.
   - **TechLog & Minimum Equipment List (MEL)** : Intégrer les pénalités d'Airmanship liées à la MEL (générer des vols où un équipement est défectueux, forçant des procédures de démarrage spécifiques).
   - **Procédures Anormales** : Gérer la reconnaissance de l'exécution des check-lists anormales (ex: panne moteur, dépressurisation) via la télémétrie.

## Règles Importantes (Rappel)
- **Règle de Nomenclature Globale (Design)** : Tous les documents relatifs au design doivent être nommés avec le préfixe `Design_` suivi du style (ex: `Design_Gameplay_`, `Design_Technical_`). Ne jamais laisser de documents éparpillés avec des noms génériques.
- **Bannissement du terme "Implementation"** : Ne plus jamais utiliser le terme "Implementation" ou "Implémentation" dans les discussions ou la documentation pour parler d'un concept. Ce terme doit être systématiquement remplacé par "Design".
