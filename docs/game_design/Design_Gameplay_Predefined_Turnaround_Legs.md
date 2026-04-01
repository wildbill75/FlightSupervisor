# Design Gameplay: Predefined Turnaround Legs (Rotations)

## Concept
Introduire un mode de "Shift" pré-généré pour Flight Supervisor. Actuellement, l'utilisateur doit créer sa propre rotation ("Custom") via SimBrief en rentrant manuellement ses destinations. L'objectif de ce nouveau système est d'offrir des journées de travail (Rotations) "clés en main" basées sur la réalité, idéales pour les joueurs souffrant du syndrome de la page blanche ou ceux désirant une immersion immédiate.

## Pré-requis Identifiés
Pour qu'une rotation prédéfinie soit cohérente, le système doit filtrer les propositions selon 3 critères majeurs :
1. **La Compagnie Aérienne (Airline)** : Les routes d'Air France, d'easyJet ou de Ryanair sont fondamentalement différentes.
2. **L'Appareil (Airframe)** : Actuellement limité au Fenix A320 (A320-200), le système de filtrage doit prévoir l'arrivée d'autres modules (A320neo, B738, etc.).
3. **L'Aéroport de Départ (Origin)** : La rotation doit logiquement commencer là où le joueur est stationné.

## Les Approches de Sélection (Flow)
Le document propose deux manières d'identifier l'aéroport de départ :

### 1. Auto-détection (Live MSFS via FSUIPC/SimConnect)
- **Principe** : L'application attend que le joueur lance le simulateur, apparaisse dans le cockpit au parking (Cold & Dark), puis lit les coordonnées GPS pour en déduire le code OACI de départ.
- **Avantage** : Immersion totale et fluidité absolue.
- **Inconvénient** : Nécessite que le simulateur soit déjà chargé avant de configurer le "Duty", ce qui inverse la logique habituelle de préparation de vol où le dispatch se fait généralement avant le chargement 3D.

### 2. Sélection Manuelle
- **Principe** : L'utilisateur indique simplement son aéroport de départ souhaité dans l'interface, et l'application lui suggère une liste de rotations associées.
- **Avantage** : Permet de préparer le vol et lire le briefing pendant que le simulateur charge en arrière-plan.

## Typologie des Rotations
Il est envisagé de catégoriser les rotations pour offrir une dimension "jeu/challenge" supplémentaire :
- **Rotations "Classiques"** : Les aller-retours typiques (ex: EGGW - EHAM - EGGW).
- **Rotations "Challenging / Quirky"** : Des journées de travail plus intenses incluant des aéroports compliqués (ex: EGGW - LOWI - EGGW) ou des contraintes horaires serrées.

## Phase 1 : Le Minimum Viable Product (MVP) - Tests & Validation
Afin d'implémenter et tester la fonctionnalité rapidement et sereinement, le développement de la "Phase 1" se concentrera **exclusivement sur un seul environnement de test** :
- **Compagnie** : easyJet (EZY)
- **Appareil** : Fenix A320
- **Hub de Départ** : Londres Luton (EGGW)

Ce choix permet de coder le sélecteur, la base de données JSON et l'injection des parties temporelles sans s'éparpiller. Une fois validé, il suffira de "remplir" le fichier JSON avec d'autres hubs (LFPO, EDDF, etc.) et d'autres compagnies.

## Base de Données (Implémentation Technique)
La logique reposera sur un fichier `wwwroot/data/predefined_rotations.json` contenant la liste des shifts.
Exemple de structure pour le MVP easyJet depuis Luton :
```json
{
  "EZY": {
    "EGGW": [
      {
        "id": "ezy_eggw_classic_1",
        "title": "The Dutch Hop",
        "type": "Classic",
        "aircraft": "A320",
        "difficulty": 1,
        "legs": ["EGGW", "EHAM", "EGGW"]
      },
      {
        "id": "ezy_eggw_holiday_1",
        "title": "Palma Escapade",
        "type": "Classic",
        "aircraft": "A320",
        "difficulty": 2,
        "legs": ["EGGW", "LEPA", "EGGW"]
      },
      {
        "id": "ezy_eggw_chal_1",
        "title": "Alpine Challenge",
        "type": "Challenging",
        "aircraft": "A320",
        "difficulty": 5,
        "legs": ["EGGW", "LOWI", "EGGW"]
      }
    ]
  }
}
```
