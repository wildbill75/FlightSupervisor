# Design Technical Handover - Next Session

Ce document sert de prise de notes en temps réel durant la session de tests en vol (Phase 4). Il liste les bugs rencontrés, les comportements inattendus et les régressions à prioriser pour la prochaine session de développement.

## Bugs Remontés (Test en Vol en cours)

- **Bug 22 (PAX Manifest)** : Il manque la colonne "Nationalité" dans le tableau d'affichage du manifeste passager.
- **Bug 23 (Cleanliness bloquée)** : La valeur de l'état de propreté (*Cleanliness*) reste bloquée (ici à 68%) malgré l'exécution du nettoyage par les PNC. 
  - *Design intent* : Il faudra lier le pourcentage récupéré à l'efficacité/compétence des PNC.
  - *Règle* : La propreté ne doit jamais pouvoir atteindre 100% car l'erreur est humaine. *(Rejoint les Bugs 7 & 9).*
- **Bug 24 (UI Modales)** : Enlever la mention "TRUE AIRMANSHIP" du titre de toutes les modales afin de permettre la lecture complète du nom de la fenêtre.
- **Bug 25 (Seatbelt Logging)** : Les passages du Seat Belt Switch sur ON/OFF n'ont pas été monitorés ni tracés dans les logs d'événements du vol.
- **Bug 26 (Ground Ops UI)** : Le texte de statut "GROUND OPERATIONS COMPLETED" (en vert) affiché au-dessus  de la barre de progression globale du Turnaround ne disparaît pas automatiquement une fois terminé ; il devrait être masqué.
- **~~Bug 27 (Event Logger & SuperScore Déconnectés)~~** : *[HOTFIXÉ en session]* Les événements (simples et complexes) sont de nouveau monitorés et liés au SuperScore (avec impact coloriel selon Delta, ou gris neutre pour les Ops pures).
- **Bug 28 (Délai de Réaction FO & Grace Period)** : Actuellement, le FO signale les oublis (ex: landing gear sorti au-dessus vitesse, phares au-dessus/en-dessous 10k ft, passage des 10k avec Seat Belts oubliés/allumées), mais le malus tombe instantanément. Il faut scripter un **délai de grâce (environ 5 à 10 secondes)** après le callout du FO avant d'infliger la pénalité ou la perte de points au SuperScore.
- **Bug 29 (Bonus 1000ft AGL Manquant)** : Le franchissement de la gate d'approche finale (1000ft AGL) pénalise correctement le joueur s'il n'est pas configuré (ex: train rentré), mais ne lui offre **aucun bonus** s'il est parfaitement stabilisé (Stable Approach Flow / Flaps Full, Gear Down). Il faut rajouter ce point de gratification.
- **Bug 30 (Location Mismatch & Non-Transition Leg 2)** : À l'arrivée (fin de Leg 1), l'application passe bien en phase `Turnaround` et incrémente en `Leg 2`, mais les données de vol globales ne se mettent pas à jour. Erreur fatale : un modal "Location Mismatch" apparaît estimant que l'avion devrait être au départ de LFPO (Origine Leg 1) alors qu'il vient de se poser à LFBO. L'UI From/To reste plantée sur LFPO -> LFBO au lieu de basculer.
- **Bug 31 (Post-Flight Debriefing incomplet)** : L'écran de debrief affiche bien le SuperScore Global (1029), mais toutes les sous-catégories (Safety, Comfort, Maintenance, Operations) sont à `0`. De plus, le "Block Time" affiche `0h 0m` et il y a une erreur d'encodage sur le caractère de la flèche (`LFPO âž" LFBO`).
- **Bug 32 (Spam PNC Cleanliness)** : En mode Turnaround, si la propreté (Cleanliness) de la cabine est trop basse, le système génère un spam en boucle : le PNC envoie le même message "passengers complaining about the disgusting state..." plusieurs fois à la suite dans les logs UI sans délai de cooldown respecté.
- **Bug 33 (Statut Cabine défectueux en Turnaround)** : Alors que l'avion est au sol, les portes ouvertes et que le débarquement est en cours (ex: 10/174 passagers restants sur le tracker graphique), le statut PNC principal affiche paradoxalement un macaron vert "Cabin Ready & Seated".

## Nouvelles Features Demandées
- **Feature 1 (Télémétrie Phase)** : Ajouter une fonctionnalité de chronométrage invisible pour chaque phase de vol afin d'obtenir un tracking très précis des temps (block, vol, taxi, etc.) à la seconde près.
- **Feature 2 (LVARs & Sys Tracking)** : Connecter et exposer dans l'Event Log les LVARs/SimVars suivantes qui ne sont pas encore traquées :
  - **Pilotes Automatiques** : Différencier AP1 et AP2.
  - **Speedbrakes** : État logique complet (Armed, 1/2, Full, Retracted) au lieu du simple pourcentage.
  - **Météo / Radar (WXR)** : PWS (Predictive Windshear) ON/OFF et Système (SYS 1/OFF/2).
  - **Flight Director (FD)** : Bouton FD ON/OFF.
- **Feature 3 (Hardcore / Realistic Mode)** : Ajouter un mode UI minimaliste pour une immersion totale. Ce mode masquera toute la "télémétrie magique" (SuperScore, Températures précises au dixième, jauges de confort et anxiété) et l'Event Log live. Le Dashboard n'affichera que :
  - **Infos de base** : Origin/Dest, Phase de vol actuelle, Heure et Timings des legs.
  - **Comms** : Les messages reçus des PNC et les boutons pour leur envoyer des directives.
  - *Note : Le vol est tout de même monitoré et noté en arrière-plan, mais tout n'est révélé qu'à la clôture du vol (Débriefing).*
- **Feature 5 (Ground Ops Turnaround Workflow & Intégration GSX Pro)** : Refondre la logique du Turnaround pour la lier étroitement à l'état physique de l'avion (Lvars) et potentiellement à l'outil GSX :
  - **Déclencheur Maître (Beacon Light & Engine Rules)** : 
    - Le passage en phase `Turnaround` est déclenché par **Beacon Light = OFF**. C'est le signal universel autorisant le ground crew à approcher l'appareil.
    - **CRITICAL SAFETY VIOLATION** : Si le Beacon est mis sur OFF alors que les moteurs tournent encore (N1 > 5%, ils doivent avoir eu le temps de ralentir - spool down), le joueur reçoit une pénalité de sécurité colossale (-500 ou -1000 pts) car le personnel au sol serait mis en danger mortel.
  - **Libération Cabine vs Débarquement** : 
    - Le passage des **Seat Belts = OFF** "libère" les passagers (ils se lèvent). S'ils restent parqués et attachés alors que le Beacon est OFF, le PNC appelle le cockpit ("Captain, we are at the gate, shall we release the passengers?"), et leur Satisfaction chute rapidement en flèche (Immobilisme post-vol).
  - **Condition d'ouverture système (Détecteurs Portes / Fenix & GSX Integration)** :
    - Élargir le `WasmLVarClient` pour traquer les portes via la télémétrie native de MSFS/Fenix (`A:INTERACTIVE POINT OPEN`, `L:S_OH_EXT_LT_BEACON`) et se servir de l'application comme centre de commande :
    - **Fallback Natif (Fenix A320 Base)** : Le bouton deboarding nécessite au minimum `Porte 1L = Ouverte` + présence Jetway MSFS ou Escaliers (via Lvars Fenix EFB).
    - **GSX Pro (Opt-in)** : Si détecté, GSX Pro expose des LVars (ex: `L:FSDT_GSX_BOARDING_STATE`) qui peuvent être lues ET déclenchées depuis C#. Un clic sur Deboarding pourrait alors déclencher le script GSX automatiquement.
    - **Le bouton Deboarding** ne se dégrise que si : `Seat Belts = OFF` + `Porte 1L = Ouverte` + (`Jetway = Connected` OU `Stairs = Connected`).
    - **Le bouton Cargo Unload** ne se dégrise que si : `Beacon = OFF` + `Portes Cargo = Ouvertes`.
  - **Séquençage strict & Application universelle (Leg 1 incluse)** : 
    - Le "Ground Ops" de la Leg 1 (Pre-Flight, avion Cold & Dark) répondra **exactement** aux mêmes exigences ! La montée des passagers (Boarding) ou le chargement du catering sera grisé si vous n'avez pas ouvert les bonnes portes et branché les bons équipements au sol.
    - Pour les rotations multi-legs (Turnarounds), les services complets (Refuel, Catering, Boarding) restent en plus grisés tant que le Deboarding de la Leg précédente n'est pas complètement terminé.
  - **Feedback Visuel/Audio** : Ajouter des indices UI (tooltips bloquants) et des interactions CRM vocales fluides pour guider le pilote sur ce qui bloque la rotation.

---
## Historique des Bugs Existants à Vérifier (Pour mémoire)
- **Bug 8 (Comportement Sièges)** : Réaction des ceintures lors de l'embarquement (éviter que tous obéissent aveuglément).
- **Bug 6 (Thermique au Démarrage)** : Confort initial. Le jeu démarre arbitrairement sur 22°C (au lieu de l'ambiante MSFS) à l'instant où le code démarre.
- **Bug 7 & 9 (Anxiété & Produit)** : Recalibrer les formules de l'Audit Produit (propreté plafonnée, calculs de Catering cassés) et algorithmes d'Anxiété globale.
