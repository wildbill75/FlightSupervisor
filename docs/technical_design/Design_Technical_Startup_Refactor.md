# Design Technical - Startup Refactor (Dégraissage du Mammouth)

## 1. Concept de Base
L'idée proposée par le joueur est de supprimer toutes les étapes intermédiaires (comme le setup initial du lieu de départ et du statut Pristine/Turnaround) au lancement de l'application. Cette approche vise à fluidifier drastiquement l'Expérience Utilisateur (UX). Flight Supervisor démarrera directement sur son interface principale (le Dashboard/Briefing), en état de veille (`Idle`), s'appuyant uniquement sur le contexte (SimConnect + SimBrief) pour s'auto-configurer.

## 2. Le Flux Utilisateur Simplifié (Nouveau Workflow)

1. **Lancement Immédiat** : L'app s'ouvre. Le Dashboard et la Sidebar sont affichés, mais l'avion est en état `Idle` et aucune métrique n'est verrouillée.
2. **Choix "Pristine vs Turnaround" déporté** : Ce choix devient une simple "Checkbox" ou option globale dans l'onglet `Settings` persistée en session (ex: *Default Session Start: Cold & Dark (Pristine)* ou *Turnaround*). 
3. **Importation du Plan de Vol (Briefing)** : Le joueur importe sa rotation via l'onglet Briefing. L'application récupère l'aéroport de départ prévu (`Origin ICAO`) ET ses coordonnées GPS exactes (que SimBrief inclut automatiquement dans son payload JSON).
4. **Validation Croisée Automatique (Le "Gatekeeper")** :
   - *Mode Connecté (MSFS)* : L'app lit les Lvars `PLANE LATITUDE` et `PLANE LONGITUDE` via SimConnect et mesure la distance avec l'aéroport SimBrief. Si l'avion n'est pas physiquement garé au bon aéroport, une bannière orange alerte le joueur de l'incohérence. S'il est au bon endroit et moteurs éteints, les Opérations au Sol se débloquent toutes seules.
   - *Mode Hors Ligne (Dev)* : Le système ignore la vérification spatiale et assume que l'avion est déjà stationné à l'Origin ICAO du plan de vol.

## 3. Avantages (Pros)

- **Friction Zéro ("Plug & Play")** : L'outil gagne une dimension "magique". Le joueur allume l'app, importe son vol, et le logiciel déduit tout le reste.
- **Nettoyage du Code Frontend** : Suppression de dizaines de lignes de code gérant le "Setup Modal" (HTML/JS), l'auto-complétion de saisies clavier manuelles, et la gestion des boutons de démarrage rigides.
- **Retrait de la logiques "Mid-Flight Resume" (Sécurisation)** : Suivant ta recommandation, on dégage le concept instable de "reprendre un vol en cours". L'application partira du principe abstrait qu'une session démarre au sol. Si l'application ou l'ordinateur plante en plein vol, on ne tentera plus de "rattraper le vol au vol", car c'est techniquement trop dangereux (désynchronisations massives du backend, timers erronés). À la place, seule la fonctionnalité **Pause** sera conservée pour pallier aux absences légitimes du joueur en cours de session.
- **Prévention d'Erreurs Humaines** : Puisque la source de vérité devient la géométrie MSFS couplée au plan SimBrief, on ne peut plus "se tromper" en renseignant manuellement LFSB au lieu de LFPG.

## 4. Points d'Attention (Challenges)

1. **La Logique Spatiale (Haversine Formula)** : SimConnect ne donne pas facilement l'ICAO actuel de l'avion sous forme de chaîne de caractères sans un plan de vol interne au FMC. La méthode la plus élégante sera de coder la formule de "Haversine" en C# pour vérifier que l'avion est dans un rayon de ~5 miles nautiques des coordonnées de l'aéroport SimBrief.
2. **Protection du Flux Actif** : Il faudra s'assurer que l'importation intempestive d'un nouveau plan de vol par erreur en pleine croisière n'écrase pas violemment le vol en cours si aucune précaution n'est prise.
3. **Surcharge Cognitive Transitoire** : Si l'utilisateur lance le jeu sans rien importer et que l'interface "Dashboard" vide est sa première vision, l'UI devra explicitement l'inviter à aller sur l'onglet Briefing (Ex: Un grand bouton central "Import SimBrief Rotation" au milieu du Dashboard vide).

## Conclusion
Ce refactoring est une excellente directive de Game Design. Il supprime le concept encombrant d'un "assistant d'initialisation" pour embrasser un fonctionnement ambiant et passif, en accord avec l'idée d'un "Flight Supervisor" silencieux, efficace et intelligent.
