# Design: Multi-Leg Auto-Consumption Engine

Ce document définit le "Moteur d'Escale (Turnaround Engine)" qui va gérer la dégradation de la cabine et la consommation des ressources au fil des étapes (legs). L'objectif est de créer une simulation suffisamment réaliste pour que la décision d'omettre un service au sol (pour gagner du temps) ait de vraies considérations stratégiques.

## 1. Initialisation : L'option `First Flight Always Serviced`
- **Mécanique** : Si l'option est cochée, le Vol 1 (Leg 1) force toutes les jauges au statut parfait : `CabinCleanliness = 100%`, `WaterLevel = 100%`, `WasteLevel = 0%`.
- Par la suite, ces variables deviennent **persistantes** pour le reste de la session de jeu (Leg 2, Leg 3, etc.). Si une rotation est terminée ou l'application redémarrée, l'état se réinitialise.

## 2. Le Modèle de Consommation "Intelligent" (Tick Loop)

Je propose d'intégrer dans le `CabinManager.Tick()` le calcul suivant (basé sur le temps réel écoulé `deltaTimeSeconds` ou le "Time Warp") :

### A. Le Catering (Les Repas)
*Plutôt qu'un % abstrait, on utiliserait un nombre entier de Rations.*
- **Chargement** : Un camion Catering "Full" charge le nombre de repas équivalent à `Nombre de Pax Prévus + 10% (pour l'équipage)`.
- **Consommation** : Pendant la phase de vol `InFlightService`, chaque passager a de base **90% de chances de consommer un repas**. S'il est endormi ou perturbé, il ne mange pas.
- **Résultat** : Un avion atterrira toujours avec un petit "solde" de catering positif (les restes). Pour un vol très court, le reliquat peut permettre de faire le vol retour en faisant l'impasse sur le Catering !

### B. Niveaux des Fluides (Water & Waste)
*L'eau potable baisse, tandis que les eaux usées montent.*
- **Base Rate (Consommation de croisière)** : Remplissage/Vidange constant par passager (ex: 0.5% par heure par Pax).
- **Le Multiplicateur Psychologique (Anxiété)** : 
  - Si l'anxiété moyenne (`PassengerAnxiety`) dépasse **60%**, la cabine déclenche le statut *stress bowels*. La fréquentation des toilettes bondit artificiellement et le WasteLevel se remplit **2x plus vite**.
  - Si le niveau Waste atteint **100%**, les toilettes "condamnées" font crasher dramatiquement le taux de Confort et de Satisfaction de toute la cabine.

### C. Propreté de la Cabine (Cabin Cleanliness)
*Décroît de 100% vers 0%.*
- **Base Rate** : Dégâts organiques dus au temps passé à bord (ex: perte de 15% pour un vol normal d'1h30).
- **Modificateurs Dynamiques** :
  - **Crises & Turbulences Lourdes** : Chute instantanée de -10% de propreté immédiate (boissons renversées, passagers malades avec les "sick bags").
  - **Repas & Débarquement** : Lors du débarquement final, la propreté chute brutalement de -5% car les passagers laissent leurs emballages en partant.
- **Résultat** : Si la cabine atterrit sur le Leg 3 à 40% de propreté et que le nettoyage (Cleaning Service) est sauté, les passagers du Leg 4 entreront dans un avion déjà sale, causant un malus sévère de `-20% Confort Initial`.

---

## 3. Intégration dans le Cycle "Turnaround"

### L'Interaction avec le GroundOpsManager
Lorsque Flight Supervisor est "au bloc" entre deux vols (Turnaround Phase) :
- Appeler le service **Cleaning** remet `CabinCleanliness` à 100%. Le temps du service est proportionnel au degré de saleté (nettoyer une cabine à 90% prend 3 minutes ; à 30%, ça prend 15 minutes !).
- Appeler le **Water/Waste Truck** draine les déchets (0%) et remplit l'eau (100%).
- Appeler **Catering** ajoute le delta manquant pour remonter au cap attendu pour le Leg suivant.

## Questions Ouvertes pour Validation
- Le calcul en nombre entier "Rations Catering" te convient-il mieux qu'une simple jauge globale en %, pour plus de lisibilité en cabine ?
- Dois-je afficher au joueur le niveau (% d'eau, % de toilettes) directement dans l'UI du "Cabine Status" ou garder cela semi-invisible ?
