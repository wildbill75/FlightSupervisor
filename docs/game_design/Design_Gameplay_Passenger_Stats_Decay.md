# Design Gameplay: Passenger Stats Decay (Caps & Floors)

## 1. Objectif du Système (Le concept des Caps Permanents)
L'objectif est de rendre la gestion des passagers plus réaliste en introduisant le concept de résilience et de seuil critique. Lorsqu'un événement négatif se produit (turbulences, retards, vols désagréables), le confort et la satisfaction des passagers chutent, et leur anxiété grimpe. Bien que les statistiques puissent "guérir" avec le temps en vol ou grâce à un bon service (repas, annonces), elles ne reviendront **jamais à leur niveau initial**. 

Chaque passager se verra doté de "limites" strictes pour la rotation :
- **MaxSatisfactionCap** (Initial = 100) : Le plafond maximal de satisfaction.
- **MaxComfortCap** (Initial = 100) : Le plafond maximal de confort.
- **MinAnxietyFloor** (Initial = 0) : Le seuil de stress "résiduel" sous lequel l'anxiété ne pourra plus jamais descendre pour la durée de ce vol.

## 2. Règle Principale de "Guérison" (Recovery System)
Au cours du vol (notamment en phase de Croisière si l'avion ne bouge pas) :
- La **Satisfaction** remonte lentement jusqu'à atteindre son propre `MaxSatisfactionCap`.
- Le **Confort** remonte lentement jusqu'au `MaxComfortCap`.
- L'**Anxiété** baisse progressivement jusqu'à buter d'elle-même sur le `MinAnxietyFloor`.

> Un événement négatif ne se contente pas de faire chuter la variable actuelle, il **entaille de façon permanente le plafond de récupération**.

*Exemple* : Un passager s'installe. Satisfaction = 80, son `MaxSatisfactionCap` = 100. Un retard de 5 minutes survient :
- Sa stat chute de 80 à 70 (-10 en choc immédiat). 
- Son seuil plafond `MaxSatisfactionCap` tombe de 100 à 78.
- Lors de la croisière, sans aucune autre erreur du pilote, sa satisfaction montera lentement de 70 pour finalement buter contre le mur des 78, et s'y bloquera pour le reste du vol. Si d'autres délais s'enchaînent, le plafond diminue davantage, créant un *cercle vicieux*.

## 3. Échelonnement des Pénalités de Retard
La perte de points due aux retards s'accumule de manière mathématique et proportionnelle au temps d'attente.

| Milieu de Retard (Trigger) | Choc immédiat (Actuel) | Entaille Permanente (Plafond/Plancher) | Comportement UI / Passagers |
|---|---|---|---|
| **Dépassement 5 minutes** | -10 Satisfaction<br>+5 Anxiété | **-2 MaxSatisfaction**<br>+2 MinAnxiety | Les passagers soupirent. Frustration mineure. L'UI peut flasher mais sans alerte rouge. |
| **Dépassement 15 minutes** | -20 Satisfaction<br>+15 Anxiété | **-5 MaxSatisfaction**<br>+5 MinAnxiety | Forte grogne cabine. Agacement net. Un bouton 'Appel PNC' est justifié. |
| **Dépassement 30 minutes** | -35 Satisfaction<br>+30 Anxiété | **-15 MaxSatisfaction**<br>+15 MinAnxiety | Inquiétude sévère, potentiels cas de rage, mutinerie. |

*(Passé 30 minutes, les pénalités liées à l'horloge seule sont capées pour éviter d'atteindre 0 instantanément en cas d'imprévu très long. C'est ensuite le rôle des actions PNC et des annonces de gérer les dégâts).*

## 4. Autres Événements et Effet Vicieux
Ce même système de Plancher / Plafond s'appliquera par la suite à :
- **Pilotage violent (Hard Pitch/Roll)** : Augmente drastiquement le `MinAnxietyFloor` sans retour possible. Les passagers auront peur jusqu'à l'atterrissage.
- **Go-Around** : Fait bondir le `MinAnxietyFloor` (+20).
- **Turbulences Sévères prolongées** : Abaisse le `MaxComfortCap`.

## 5. Modifications Backend à prévoir (Architecture)
1. Injecter `MaxSatisfaction`, `MaxComfort`, et `MinAnxiety` dans `PassengerState` (`CabinManager.cs`).
2. Créer une méthode centrale `ApplyEventPenalty(amount, capDamage, statType)` pour appliquer le choc et raboter le plafond simultanément.
3. Repenser le bloc Update (qui est appelé chaque seconde) pour s'assurer que la guérison (Drift) se fait toujours vers la nouvelle limite (ex: `Math.Min(MaxSatisfactionCap, currentSat)`) au lieu que ça remonte vers 100%.
