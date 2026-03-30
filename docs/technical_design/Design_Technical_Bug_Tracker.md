# Flight Supervisor - Bug Tracker

Ce fichier sert à lister tous les bugs mineurs ou majeurs rencontrés lors des tests, pour les regrouper et les traiter lors d'une phase de correction dédiée ("Bug Squashing Sprint").

## 🐛 Bugs Ouverts

### [UI / Polling] Clignotement (Flicker) des fenêtres/panneaux lors du rafraîchissement
- **Date signalée :** 28 Mars 2026
- **Problème :** Toutes les interfaces et boutons liés au système de "polling" (rafraîchissement en boucle de la télémétrie) clignotent sévèrement lorsqu'on clique dessus ou qu'ils sont ouverts (ex: Panneau Ground Ops, requêtes PNC). Le re-render de l'interface écrase probablement l'état local du DOM à chaque tick de télémétrie.
- **Action Requise :** Implémenter un système de diffing (réconciliation DOM) ou isoler les états interactifs dans le frontend pour qu'ils ne soient pas réécrits si leur valeur n'a pas changé.
- **Statut :** Ouvert 🔴 *(Ajourné à la fin du test métier en cours)*

### [Game Design / Boarding] Remplissage quasi instantané des sièges du Manifest
- **Date signalée :** 28 Mars 2026
- **Problème :** Dès que l'embarquement (Boarding) commence, l'intégralité des passagers dans le Manifest se voit attribuer le statut de "Boarded" (apparemment en une ou deux secondes), au lieu de se remplir progressivement sur toute la durée du chrono d'embarquement.
- **Action Requise :** Lier la vitesse (rate) d'embarquement des individus au timer (ElapsedSec) du processus de Boarding. Le remplissage doit garder une part d'aléatoire, tout en suivant un cheminement logique (ex: porte avant). De plus, le tout dernier passager ne doit s'asseoir qu'à 99% du processus de Boarding.
- **Statut :** Ouvert 🔴 *(Seulement noté, à faire après la phase de test)*

---

## 🛠️ Bugs Résolus

### [UI / Logique] Boutons PNC inactifs durant le Taxi Out
- **Date signalée :** 28 Mars 2026
- **Problème :** Les boutons `PNC: PREPARE FOR TAKEOFF` et `PNC: FORCE SEATS (CAUTION)` ne déclenchaient aucune action.
- **Résolution :** L'interface Javascript envoyait des identifiants (`PREPARE_TAKEOFF`) qui divergeaient des valeurs attendues en C# (`PREP_TAKEOFF`). La logique a été unifiée et la validation pour cacher le bouton (tracking de commande) ajoutée.
- **Statut :** Corrigé 🟢

### [UI / Logique] Comportements erratiques du bouton "PA: TURBULENCE WARNING"
- **Date signalée :** 28 Mars 2026
- **Problème :** Duplication du bouton dans le panneau et affichage même lorsque l'avion est au sol.
- **Résolution :** Modification dans `app.js` pour éliminer le second bloc duplicatif. Imposition de contraintes strictes basées sur  la phase (`phase !== 'AtGate' && phase !== 'TaxiOut'`...).
- **Statut :** Corrigé 🟢

### [Game Design / Logique] Blocage strict du "PNC: START SERVICE" avec Ceintures ON
- **Date signalée :** 28 Mars 2026
- **Problème :** Le système empêchait de débuter le service `START_SERVICE` si les ceintures étaient activées.
- **Résolution :** Refactorisation de `CabinManager.HandleCommand()`. Le démarrage du service PNC dépend dorénavant du vecteur G-Force en temps réel pour ignorer la consigne Seatbelts si l'air est factuellement calme.
- **Statut :** Corrigé 🟢

### [Backend / Télémétrie] Jauge "Turbulence Level" figée sur NONE & Faux Positifs
- **Date signalée :** 28 Mars 2026
- **Problème :** Système de G-Force trop capricieux pour détecter le "Jitter" des micro-oscillations, causant des fausses alertes.
- **Résolution :** Calibration du moteur dans `FlightPhaseManager.CalculateTurbulence()`. Le "Light Turbulence" est haussé de 0.1G à 0.15G, et "Moderate" à 0.30G.
- **Statut :** Corrigé 🟢

### [Backend / Jitter Engine] Bouton PA "Turbulence" déclenché sur manoeuvre pilote
- **Date signalée :** 28 Mars 2026
- **Problème :** Les inputs de commandes violents (Pilotage manuel brusque) déclenchaient l'alerte météorologique sur la jauge de turbulence.
- **Résolution :** Ajout d'une trace d'évolution du `PitchRate` et `BankRate`. L'incrémentation très rapide du pitch ou bank est identifiée comme un input pilote (isPilotInput). Le bouton PA est donc occulté en cas de pilotage brusque.
- **Statut :** Corrigé 🟢

### [Game Design / Narratif] Réponse "Request Cabin Report" hors-contexte post-secousse
- **Date signalée :** 28 Mars 2026
- **Problème :** La requête cabine après une secousse renvoyait toujours des données stables.
- **Résolution :** Assainissement de la boucle contextuelle dans `CabinManager.RequestCabinReport()`. Une forte valeur `Anxiety` (> 50) retourne dorénavant des chaînes de tension (ex: passagers nerveux et tendus), modifiable vocalement.
- **Statut :** Corrigé 🟢

### [Game Design / Variables] Récupération de l'Anxiété et du Confort dans le temps
- **Date signalée :** 28 Mars 2026
- **Problème :** Stagnation après turbulence de l'anxiété et du confort passagers.
- **Résolution :** Intégration dans `CabinManager.Tick()` d'un mécanisme de "Cooldown" constant et progressif dès qu'aucune secousse n'est perçue. L'anxiété redescend doucement proportionnellement au temps de paix global (Decay).
- **Statut :** Corrigé 🟢


### Content from Design_Technical_BugList.md
# Liste des Bugs & Améliorations en Attente (Flight Supervisor)

Ce document recense les problèmes fonctionnels et régressions actuellement non traités (ou partiellement traités) dans le système de notation de *Flight Supervisor*, afin d'avoir une feuille de route claire pour les corriger.

## 1. Régressions & Manquements (Signalés lors du dernier vol)

### A. Limites de Vitesse des Volets (Flaps Overspeed)
- **Problème actuel :** Aucune détection de la survitesse des volets n'existe actuellement dans `FlightPhaseManager.cs`. C'est pour cette raison que l'avion engendre des alarmes (ECAM internes au Fenix), mais que le *SuperScore* de l'application n'est pas impacté.
- **Solution requise :** Il faut s'abonner aux variables de l'extension des volets (via le WasmLVarClient ou SimConnect classique) pour déduire la configuration actuelle (Flaps 1, 2, 3, FULL) et vérifier la marge `Indicated Airspeed` vs `VFE` (Vitesse Maximale Volets Sortis), puis déclencher `OnPenaltyTriggered` en cas de violation.

### B. Limites de Vitesse du Train d'Atterrissage (Gear Overspeed)
- **Problème actuel :** Contrairement aux volets, la détection `VLE > 260kts` **existe** déjà dans `FlightPhaseManager.cs` et déclenche bien le message *"Landing Gear deployed above maximum extended speed"*. Cependant, **ce message n'est pas mappé** spécifiquement dans `SuperScoreManager.cs` (il est donc absorbé par une pénalité par défaut mal calibrée ou noyé dans les Safety violations génériques sans mention de maintenance explicite).
- **Solution requise :** Ajouter explicitement cette chaîne dans `SuperScoreManager.cs` et attribuer une forte pénalité catégorisée **Maintenance** (ex: `-300` ou `-500` points d'opérations/maintenance) pour simuler les dégâts potentiels causés aux trappes du train.

## 2. Incohérences UI Cabine & Autonomie PNC (Signalées récemment)

### A. Affichage du Bouton "Seatbelts" sous 10 000 ft - [RÉSOLU]
- **Solution appliquée :** Le bouton 'PA: Turbulence Warning' n'est plus généré si l'altitude est inférieure à 10 000 ft, car l'ordre de rester assigné est obligatoire à ces altitudes de manière implicite.

### B. Suppression des Ordres Manuels PNC (Service Start) - [RÉSOLU]
- **Solution appliquée :** Suppression pure et simple du bouton `PNC: Start Service` en croisière/montée. L'action est pilotée par le PNC de de manière autogérée via le score de Proactivity.

### C. Remplissage SVG du Manifest (Boarding Sync) - [RÉSOLU]
- **Solution appliquée :** Filtrage de `p.IsBoarded !== false` au niveau du tableau de l'interface et correction de la condition de synchronisation C#. Le siège s'affiche visuellement vide dans le SVG jusqu'à ce que la phase d'embarquement `Boarding` ground operations s'active. La liste des passagers ne se popule plus aléatoirement hors Ground Ops.

## 3. Ajustements Récemment Appliqués
- **Phares de Roulage (Taxi Lights) - [RÉSOLU] :** Conformément à tes instructions, la surveillance stricte des Taxi Lights a été supprimée. Tu ne seras plus pénalisé si les Taxi Lights sont éteints pendant le roulage à plus de 5 kts, ce qui fluidifie l'utilisation (comme s'arrêter au parking).

## 4. Autres Éléments Sous Surveillance
- **Cycle complet de météo ACARS / Simulation de cabine :** Assurer que lors d'un vol long-courrier, les mises à jour météo NOAA / ActiveSky toutes les 15 minutes ne provoquent pas de bugs de rendering sur le frontend WebView2 (clignotements ou freeze).
- **Transitions de phases complexes :** Bien tester le reset des pénalités lors d'une **remise des gaz (Go-Around)**, pour ne pas accumuler des pénalités d'approche instable si la manœuvre est exécutée correctement.

---
*(Ce document pourra être mis à jour à chaque vol de validation pour suivre la complétion de ces tickets de façon ordonnée.)*
