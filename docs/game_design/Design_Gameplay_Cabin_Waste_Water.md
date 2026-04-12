# Design Note : Gestion Avancée des Déchets, de l'Eau et de la Propreté

Ce document détaille la logique "sous le capot" régissant la vitesse de dégradation de la propreté (Cleanliness), l'accumulation des déchets (Waste) et la consommation d'eau potable, sans exposer ces mécaniques complexes directement dans l'interface, afin que cela reste organique et naturel.

## 1. Moteurs de base

Le système par défaut s'appuie sur la durée du vol et le nombre de passagers embarqués.
- **Consommation d'eau** : diminue linéairement proportionnellement au nombre total de passagers.
- **Déchets (Waste)** : augmente lors de la phase de "Meal Service", et légèrement en continu durant la croisière.
- **Propreté (Cleanliness)** : diminue très lentement en croisière. Se dégrade fortement lors des secousses (turbulences) pendant les repas.

## 2. Nouveaux Multplicateurs Organiques (Granularité)

Afin d'apporter de la vie et des comportements réalistes, nous introduisons des modificateurs psychologiques et géographiques.

### A. Influence Psychologique (Stress et Tolérance)
Les passagers `Grumpy` et `Anxious` (déjà générés par l'IA d'embarquement) impactent directement le service.
- **Anxiété / Stress** : Un passager anxieux (ou rendu anxieux par des turbulences sévères) consomme plus d'eau potable (demandes à l'équipage, allers-retours aux toilettes fréquents). 
    - *Règle technique* : Si le "Stress" moyen de la cabine dépasse 50%, la consommation d'eau globale est multipliée par **1.3x**.
- **Comportement Râleur / Impatient (`Grumpy`)** : Ont tendance à moins respecter les consignes et à jeter des choses hors des poubelles.
    - *Règle technique* : Chaque tranche de 10% de pax `Grumpy` à bord accélère la dégradation naturelle de la propreté (`CabinCleanliness`) de **1.1x**.

### B. Influence de la Destination (Typologie du Vol)
Le type de destination (via `AirportArchetype`) dicte l'ambiance générale du vol.

1. **Vols "Loisirs / Été" (Holiday / Summer Destinations)**
   - *Exemples* : Ibiza, Palma de Majorque, Rhodes, Antalya, Canaries.
   - *Profil passagers* : Souvent des familles, des groupes de jeunes partant faire la fête, ambiance détendue mais beaucoup plus désordonnée. Boivent plus, grignotent plus, font plus d'aller-retours en cabine.
   - *Règle technique* : Si `DestArchetype == Holiday`, la génération de déchets (`WasteLevel`) lors du service repas augmente de **+40%** et la Propreté baisse **+20%** plus vite.

2. **Vols "Business / Affaires" (Business Destinations)**
   - *Exemples* : Francfort, Londres City, Bruxelles, Genève.
   - *Profil passagers* : Habituellement calmes, sur leur ordinateur ou dorment. Très peu de déplacements inutiles, gardent leur espace propre.
   - *Règle technique* : Si `DestArchetype == Business`, la génération de déchets est normale (1.0x), et la dégradation de la propreté est **ralentie (0.8x)**.

---
**Note d'Implémentation :**
Ces modificateurs s'appliqueront directement dans `CabinManager.cs` lors du calcul `Tick()` périodique, sans avoir besoin d'afficher un indicateur de "Racisme" ou de profilage. Le multiplicateur sera silencieux et se verra uniquement de façon systémique à l'arrivée : un avion revenant d'Ibiza nécessitera un nettoyage au sol plus long qu'un avion revenant de Francfort.
