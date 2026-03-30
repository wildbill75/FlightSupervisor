# Design_Technical_GSX_Integration

> **Note de Conformité (Design vs Implementation) :**
> Conformément à la règle globale du projet, le terme "Implementation" (comme suggéré verbalement par *GSX Implementation*) a été proscrit et remplacé par **Design_Technical_GSX_Integration** afin de respecter la nomenclature stricte des documents de conception.

---

## 1. Objectif de l'Audit

Ce document vise à évaluer la faisabilité technique de synchroniser **Flight Supervisor** avec l'add-on **GSX Pro** (FSDreamTeam) dans Microsoft Flight Simulator, en se focalisant initialement sur la phase d'avitaillement (**Refueling**).

L'objectif principal est de conserver **Flight Supervisor comme MAÎTRE absolu** de la logique temporelle, du score et de l'opération, tout en permettant à GSX de gérer l'aspect purement cosmétique (animations 3D du camion citerne) afin que l'Immersion (GSX) et l'Exigence (Flight Supervisor) cohabitent sans conflit.

## 2. Philosophie d'Intégration (Flight Supervisor = Master)

- **Déclenchement Passif :** L'utilisateur interagit avec le menu de GSX dans le simulateur ou via son EFB (Fenix) pour demander le Refueling. Flight Supervisor "écoute" simplement ce déclenchement.
- **Désynchronisation Temporelle Assumée :** 
  - GSX possède ses propres timings (souvent accélérés ou dépendants des animations des camions).
  - Flight Supervisor possède **son propre rythme** (basé sur la quantité réelle de carburant demandée via SimBrief ou nos paramètres, imposant par exemple un temps réaliste de 12 à 15 minutes).
  - **L'Application ignore la fin d'animation de GSX.** Même si le camion GSX s'en va après 3 minutes, le processus de refueling dans Flight Supervisor continuera jusqu'à la fin de SON propre minuteur. Le joueur ne pourra pas demander l'embarquement (sous peine de pénalité de sécurité) tant que le chronomètre Flight Supervisor n'est pas terminé, indépendamment de ce que montre l'animation 3D.
- **Animations Reléguées à GSX :** Flight Supervisor n'essaiera pas de manipuler les camions ou les bonshommes GSX (ce qui est effectivement un "casse-tête" technique instable). Nous laissons GSX faire sa vie visuelle.

## 3. Moyens Techniques (L-Vars GSX)

GSX expose plusieurs variables locales (L-Vars) accessibles via SimConnect, qui trahissent l'état de ses services en temps réel.
Pour le Refueling, la variable clé est :
`L:FSDT_GSX_REFUELING_STATE`

Les valeurs standard de cette variable d'état sont généralement (à revérifier empiriquement via la console MSFS) :
- `0` : Inactif (Off)
- `1` : Demandé (Requested)
- `2` : En approche (Vehicle arriving)
- `3` : Connecté, prêt (Connected)
- `4` : Refueling en cours (Fueling active)
- `5` : Camion repart (Vehicle leaving)
- `6` : Terminé (Completed)

Il est possible de demander à notre `TelemetryService` (ou un futur `GsxListenerService`) de s'abonner à cette variable.
*Important:* Flight Supervisor devra s'assurer que notre backend WPF ajoute correctement l'enregistrement de cette variable LVAR au RequestDataOnSimObject via la définition de structure typique (comme pour les variables Fenix).

## 4. Plan de Design (Plan d'attaque)

Pour implémenter de façon sécure cette écoute, voici la séquence d'action (le "Plan d'attaque") :

### Phase 0 : Paramétrage (Settings UI)
1. **Option Débrayable :** Ajouter un switch dans les options du logiciel (Settings > Integration) intitulé "Sync GSX Ground Services" (ou similaire).
2. **Fallback :** Si cette option est désactivée (ou en cas d'instabilité future de GSX), Flight Supervisor ignorera la lecture des variables GSX et le pilote devra déclencher ses services manuellement via notre interface, comme c'est le cas actuellement. Il est impératif de toujours pouvoir désactiver une interférence externe.

### Phase 1 : Capter le Signal (Le "Trigger")
1. **Ajout de la L-Var au moteur Télémétrie :** Ajouter `L:FSDT_GSX_REFUELING_STATE` ou `L:FSDT_GSX_REFUELING_VEHICLE_STATE` à la liste des données SimConnect écoutées par `TelemetryService`.
2. **Filtrage des Faux-Positifs :** Observer le comportement de GSX "en direct" pour s'assurer qu'un state > 0 confirme que le camion a bien été appelé, afin de déclencher l'action UI côté notre app.

### Phase 2 : Injection dans la Logique Flight Supervisor
1. **Écoute de Transition :** Si `FSDT_GSX_REFUELING_STATE` passe de `0` à `1` (ou `2`), Flight Supervisor intercepte cet événement.
2. **Déclenchement Automatisé :** La fonction C# simule alors l'action d'un clic sur le bouton UI `START REFUELING` de Flight Supervisor.
3. **Le chronomètre interne prend le relais :** Notre serveur Ground Ops lance le compteur de refueling à **NOTRE RIYTHME**. Lors de ce déroulement, nous ignorons toute mise à jour de complétion venant de GSX pour ne pas écourter notre chronomètre.

### Phase 3 : Extensibilité Future (Catering, Boarding)
Cette architecture sera facilement reproductible pour les autres services sans se mordre la queue, car nous n'utilisons GSX que comme "déclencheur initial" :
- `L:FSDT_GSX_BOARDING_STATE` (déclenchera notre Boarding passager et l'arrivée du Crew).
- `L:FSDT_GSX_CATERING_STATE` (déclenchera notre Timer de Catering exclusif).

### Bilan de Faisabilité (Audit)
**Statut : HAUTEMENT FAISABLE.**
Se contenter « d'écouter » une variable (mode Read-Only) pour lancer un chronomètre interne abstrait n'est absolument pas un casse-tête ni une folie. C'est l'approche la plus saine et la plus stable techniquement. Les risques de bugs critiques ou de plantages du simulateur (souvent causés par ceux qui tentent d'écrire ou de forcer GSX via SimConnect) sont ici gommés par cette approche 100% passive de lecture de state.
