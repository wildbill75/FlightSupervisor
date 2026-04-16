# Refonte du Système de Dispatch (Rotations et SimBrief)

Ce document décrit le nouveau workflow "Étape par Étape" pour la gestion des rotations dans Flight Supervisor, conçu spécifiquement pour maintenir la compatibilité avec les clients ATC (BeyondATC, SayIntentions) et Vatsim.

## 1. Création de la Rotation (Dashboard)
* **Suppression du téléchargement massif :** Le joueur ne récupère plus les plans de vols SimBrief à l'avance sur le Dashboard.
* **Création de "Coquilles Vides" (Shell Legs) :** Le joueur indique simplement le programme de la journée en insérant des étapes génériques. 
* **Figeage du Planning (Important) :** Lors de cette création, il faut spécifier les aéroports (Ex: LFPG -> LFBO) et idéalement les **horaires prévus (STD/STA)**. Ces coquilles vides figent le planning officiel de la journée.
* **Validation :** Le système enregistre cette rotation et démarre la session. Aucune donnée SimBrief n'est encore téléchargée.

### Lien de Pré-remplissage SimBrief (Navigateur Externe)
Afin de minimiser le travail du joueur et de garder l'interface pure, Flight Supervisor ouvrira un **navigateur web externe** (et non une iframe interne) pointant vers SimBrief. 
L'URL générée **pré-remplira automatiquement** :
- L'Origine et la Destination (ex: `orig=LFPG&dest=LFBO`).
- L'avion (Airframe) en récupérant **le Type** (ex: `type=A320`) ET la **Registration (Immatriculation)** (ex: `reg=F-GZNA`) utilisés lors de la *Leg précédente* (ou figurant dans la Coquille Vide originelle).
Le joueur n'aura qu'à cliquer sur "Generate" sur la page web, pour tout unifier.

## 2. Début de la Rotation (Leg 1 - At Gate)
* Au lancement, l'avion est "At Gate", mais les opérations (Boarding, Fuel) sont bloquées.
* Le système en attente demande au joueur de **générer son premier plan de vol réel** sur le site SimBrief.
* Le joueur clique sur un bouton manuel **[ PLAN LEG 1 ]** dans l'UI.
* Le système télécharge les données de la Leg 1, déverrouille la Fuel Sheet, génère le manifeste passagers, et autorise le début du Ground Ops.

## 3. Phase de Turnaround (Arrivée)
C'est ici qu'intervient le plus gros changement : le passage à l'étape suivante n'est plus automatisé.

### Ajout de la tâche "Plan Next Leg"
* Pendant le Turnaround, les actions existantes continuent (Deboarding, Cargo Unloading).
* **Nouveau Bouton :** Une 3ème action apparaît dans l'UI du Turnaround : **[ PLAN NEXT LEG ]**.
* Le joueur doit cliquer de lui-même sur cette action une fois qu'il a généré la Leg 2 sur le site internet de SimBrief.
* C'est cette action qui va télécharger le fichier JSON de la Leg 2 pour remplacer la coquille vide.

### Bouton de Transition (Proceed to Next Leg)
* Lorsque les 3 conditions sont remplies (Deboarding complet, Cargo vidé, ET Next Leg téléchargée), un bouton de validation absolu apparaît : **[ PROCEED TO NEXT LEG ]**.
* En cliquant dessus, le flux bascule brutalement : la phase de vol redevient "At Gate", et le Fuel, le manifeste et le Boarding de la Leg 2 peuvent commencer.

## 4. Anti-Triche et Sanctuarisation du Temps (Sim Time)
* Une fois le planning figé lors de la création initiale (Coquille Vide), les heures des étapes sont fixes par rapport au temps du simulateur. 
* Si le joueur régénère son plan SimBrief avec des heures erronées ou modifiées, **Flight Supervisor ignorera les horaires du fichier JSON SimBrief** et continuera de forcer la rotation en se basant sur l'heure UTC en direct du simulateur. Le joueur ne pourra donc pas tricher pour récupérer artificiellement du temps de vol ou d'escale moyen en jouant avec le dispatching.

## 5. Persistance Réaliste (Usure et Consommables)
Lors du clic sur "Proceed to Next Leg", tous les scores et la progression du vol sont réinitialisés (le vol 1 est fini), **SAUF l'état physique de l'avion** qui persiste pour les passagers suivants :
1. Période de Nettoyage de la cabine (`CabinCleanliness`).
2. Rations du Catering (Nourriture restante).
3. Niveau d'Eau Potable.
4. Remplissage des Réservoirs de Déchets (Waste Tanks).
5. Usure physique de la cellule (Airframe Wear/Defects).
6. Le carburant restant (mesuré et conservé).
