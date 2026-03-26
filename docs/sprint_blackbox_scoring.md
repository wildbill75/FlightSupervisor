# SPRINT SCOPE: Black Box & Scoring (Pushback -> Arrived)

Ce document temporaire rassemble l'ensemble des fonctionnalités relatives à la télémétrie en vol, au système de notation (SuperScore) et à la Boîte Noire (Flight Report) jusqu'à l'arrivée de l'avion en porte. L'objectif est de clôturer et boucler de bout-en-bout le flow opérationnel d'un vol.

## 🟢 Développé mais en attente de validation / test
L'infrastructure fondamentale a été codée mais nécessite des vols de qualification complets pour confirmer que ce flux de données opère parfaitement d'un bout à l'autre :
- [ ] **Story 1 :** LVARs Fenix A320 (Seatbelts et Lumières complexes) - Les variables natifs manquent, à valider en vol si notre système WASM de secours tiendra.
- [ ] **Story 2 :** Transitions de phases réelles (TaxiOut -> Takeoff -> InitialClimb -> Climb -> Cruise -> Descent -> Approach -> Landing -> TaxiIn -> Arrived). Vérifier qu'aucune phase ne s'active prématurément ou en retard.
- [ ] **Story 4 :** Pénalités dynamiques (FPM atterrissage, G-Force sévérité, Pitch & Bank limits, Gear Retraction, Engine Failure en vol, Landing Lights 10k ft). Déjà codées. 

## 🟡 Tâches "To Do" : Tolérances & Confort en vol
Ces tickets vont parfaire le jugement et rendre le pilotage naturel sans punition injustée.
- [ ] **Story 4 :** Dérogation APU en Croisière (Ne pas punir l'APU si panne génératrice en vol).
- [ ] **Story 4 :** Tolérance 10 000ft (Seatbelts/Lights/Speed) : Marge de +/- 500 ft autour du niveau de transition pour que le Chief Pilot ne flashe pas instantanément au pied de la barrière symbolique.
- [ ] **Story 5 :** Ajournement des pénalités de retard : Cacher l'anxiété ou le malus temporel jusqu'à la phase Arrived afin que le joueur puisse tenter un rattrapage de Fuel ou de Raccourci avant l'heure d'arrivée.
- [ ] **Story 17 : Interface PNC Cabin Intercom (MVP) & Logique "Cabin Secured" :** 
  - [ ] UI Grise (Dropdown & Button Send) sur le Dashboard.
  - [ ] Minuterie de réponse des PNC limitant l'autorisation de Takeoff / Landing. Pénalité grave associée si le pilote n'attend pas ses hôtesses.

## 🔴 Tâches "To Do" : Télémétrie d'Arrivée (Flight Report)
Tickets techniques finaux permettant d'épaissir les statistiques de vol et la "Note du Chef Pilote".
- [ ] **Story 4 - Tail Strike (Maintenance)** : Détecter un cabré excessif (> 11°) alors que le train principal touche le sol (au décollage ou à l'atterrissage).
- [ ] **Story 4 - Flaps Overspeed (Maintenance)** : Pénaliser un maintien des volets au-delà de 260 kts.
- [ ] **Story 4 - Engine Cooldown (Maintenance)** : Imposer un chronomètre de 3 minutes entre l'atterrissage et la coupure réacteurs.
- [ ] **Story 4 - Brake Temp (Maintenance)** : Récupération de la chauffe des freins (température disque chaude ou Peak) au moment du blocage final et pénaliser si un feu s'annonce.
- [ ] **Story 4 - Go-Around / Unstable Approach** : Analyser l'altitude 1000 AGL ; si instable et atterrissage forcé = Pénalité lourde. Si Go-Around (Remise des gaz) = Validation procédure et annulation des fautes mineures antérieures.
- [ ] **Story 25 - Chief Pilot Narrative Debrief** : Ajouter un parser (moteur d'analyse Javascript) sur la modale "Flight Report". Le système lira le JSON des métriques et crachera un ou deux paragraphes textuels synthétisant la note (Ex: "Atterrissage brutal et mauvaise gestion des PNC, mais vol à l'heure : 60/100").
- [ ] **Story 22 - Page Flight History (Optionnel mais recommandé)** : Permettre d'enregistrer le Final Report au format JSON local dans `Documents` avec un bouton depuis la modale finale, pour relire ses vols passés dans l'appli.
