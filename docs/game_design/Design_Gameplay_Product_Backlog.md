# Flight Supervisor - Product Backlog (Macro)

> **Légende :**
> - **Story (Macro)** : Un grand ensemble de fonctionnalités métier.
> - **Sprint (Micro)** : Un document de tickets techniques précis situé dans `docs/sprints/`.
> - **Status** : `[x]` Fini, `[/]` En cours, `[ ]` À faire.

---

## 🚀 Version 1.0 (MVP) - En cours

### Story 1 : Core Architecture & Telemetry Bridge [/]
*Objectif : Établir une connexion SimConnect/WASM robuste.*
- **Détails :** [Sprint_Core_Architecture.md](sprints/Sprint_Core_Architecture.md)
- **Technical Design :** [Design_Technical_Extraction_WASM_LVars_Fenix.md](technical_design/Design_Technical_Extraction_WASM_LVars_Fenix.md)

### Story 2 : Flight Phase State Machine [x]
*Objectif : Transition de phases fiable par Radio Height et G-Force.*
- **Technical Design :** [Design_Technical_Flight_Phase_Machine.md](technical_design/Design_Technical_Flight_Phase_Machine.md)

### Story 3 : Ground Operations & Anti-Cheat [/]
*Objectif : Simulation des services au sol et synchronisation temporelle.*
- **Détails :** [Sprint_Ground_Operations.md](sprints/Sprint_Ground_Operations.md)
- **Gameplay Design :** [Design_Gameplay_Ground_Operations.md](game_design/Design_Gameplay_Ground_Operations.md)
- **Technical Design :** [Design_Technical_Ground_Operations.md](technical_design/Design_Technical_Ground_Operations.md)

### Story 4 : SuperScore & Black Box [/]
*Objectif : Analyse de performance (FPM, G-Force, SOP).*
- **Détails :** [Sprint_BlackBox_Scoring.md](sprints/Sprint_BlackBox_Scoring.md)
- **Technical Design :** [Design_Technical_SuperScore_System.md](technical_design/Design_Technical_SuperScore_System.md)

### Story 5 : Chronometry & Final Flight Report [x]
*Objectif : Figeage AOBT/AIBT et rapport visuel modernisé.*
- **Technical Design :** [Design_Technical_Chronometry_AOBT_AIBT.md](technical_design/Design_Technical_Chronometry_AOBT_AIBT.md)

### Story 6 : UI/UX Reorganization [/]
*Objectif : Sidebar navigation et ergonomie globale.*
- **Technical Design :** [Design_Technical_Restructuration_WebView2.md](technical_design/Design_Technical_Restructuration_WebView2.md)
- **UI Design :** [Design_UI_System.md](ui_design/Design_UI_System.md)

### Story 7 : Localization (i18n) [/]
*Objectif : Support FR/EN pour UI et messages Backend.*
- **Technical Design :** [Design_Technical_Localization_System.md](technical_design/Design_Technical_Localization_System.md)

### Story 8 : Third-Party Weather Integration [/]
*Objectif : Support SimBrief, Active Sky et MSFS Live.*
- **Technical Design :** [Design_Technical_Weather_Multisource.md](technical_design/Design_Technical_Weather_Multisource.md)

### Story 9 : GSX Advanced Integration [ ]
*Objectif : Auto-Sync avec GSX Pro via L:Vars.*

### Story 10 : Measuring Units Architecture [x]
*Objectif : Conversion automatique KGS/LBS, C/F, HPA/INHG.*
- **Technical Design :** [Design_Technical_Unit_Conversion_Engine.md](technical_design/Design_Technical_Unit_Conversion_Engine.md)

### Story 11 : Contextual Tooltips & Onboarding [ ]
*Objectif : Tutoriels et aides à la navigation.*

### Story 13 : Multi-Leg Planning [ ]
*Objectif : Enchaînement des vols sans retour menu ("Fetch Next Leg").*

### Story 14 : Passenger & Crew Manifest [x]
*Objectif : Génération de passagers par nationalité et Seat Map 2D.*
- **Gameplay Design :** [Design_Gameplay_Passenger_Manifest.md](game_design/Design_Gameplay_Passenger_Manifest.md)
- **Technical Design :** [Design_Technical_Passenger_Manifest.md](technical_design/Design_Technical_Passenger_Manifest.md)

### Story 15 : Advanced Dispatch & Met Briefing [x]
*Objectif : Analyse TAF, NOTAMs et anticipation de piste.*
- **Gameplay Design :** [Design_Gameplay_Advanced_Dispatch_Briefing.md](game_design/Design_Gameplay_Advanced_Dispatch_Briefing.md)

### Story 16 : In-Flight Judgment & Airmanship [ ]
*Objectif : Récompense pour déviation SIGMET et vol manuel.*

### Story 17 : Passenger Satisfaction & Cabin Management [/]
*Objectif : Turbulence Jitter, PA Button, et simulation de ceintures.*
- **Détails :** [Sprint_Advanced_Turbulence_Cabin_Dynamics.md](sprints/Sprint_Advanced_Turbulence_Cabin_Dynamics.md)
- **Gameplay Design :** [Design_Gameplay_Advanced_Turbulence_Cabin_Dynamics.md](game_design/Design_Gameplay_Advanced_Turbulence_Cabin_Dynamics.md), [Design_Gameplay_Captain_Announcements.md](game_design/Design_Gameplay_Captain_Announcements.md)
- **Technical Design :** [Design_Technical_Captain_Announcements_Logic.md](technical_design/Design_Technical_Captain_Announcements_Logic.md)

### Story 17.5 : PNC Communication & Cabin Prep [x]
*Objectif : Jauge de préparation cabine dynamique et gestion du temps PNC pendant le taxi.*
- **Gameplay Design :** [Design_Gameplay_PNC_Communication_Cabin_Prep.md](game_design/Design_Gameplay_PNC_Communication_Cabin_Prep.md)
- **Technical Design :** [Design_Technical_Cabin_Preparation_Logic.md](technical_design/Design_Technical_Cabin_Preparation_Logic.md)

### Story 17.6 : In-Flight Service Scaling [/]
*Objectif : Adaptation de la vitesse du service au temps de vol et respect de la limite des 10 000 pieds.*
- **Gameplay Design :** [Design_Gameplay_InFlight_Service_Scaling.md](game_design/Design_Gameplay_InFlight_Service_Scaling.md)
- **Technical Design :** [Design_Technical_Service_Scaling_Logic.md](technical_design/Design_Technical_Service_Scaling_Logic.md)

### Story 18 : Crisis Generator [/]
*Objectif : Moteur de probabilité d'urgences critiques.*
- **Détails :** [Sprint_Crisis_Generator.md](sprints/Sprint_Crisis_Generator.md)
- **Gameplay Design :** [Design_Gameplay_Crisis_Generator.md](game_design/Design_Gameplay_Crisis_Generator.md)
- **Technical Design :** [Design_Technical_Crisis_Generator.md](technical_design/Design_Technical_Crisis_Generator.md)

### Story 19 : Airline Policies & Risk Management [ ]*Objectif : Profils Legacy vs Low-Cost (Pondération du score).*

### Story 20 : In-Game Toolbar Panel [x]
*Objectif : Toolbar MSFS pour VR/2D.*
- **Gameplay Design :** [Design_Gameplay_InGame_Panel_MSFS.md](game_design/Design_Gameplay_InGame_Panel_MSFS.md)
- **Technical Design :** [Design_Technical_MSFS_Toolbar_Bridge.md](technical_design/Design_Technical_MSFS_Toolbar_Bridge.md)

---

## 🎯 Stories Post-Release (21 - 35)

- **S21 : Fuel Economy** [ ]
  - **Gameplay Design :** [Design_Gameplay_Fuel_Economy.md](game_design/Design_Gameplay_Fuel_Economy.md)
- **S22 : Flight Logger History** [/]
- **S23 : Manual Flying Bonus** [ ]
  - **Gameplay Design :** [Design_Gameplay_Manual_Flying.md](game_design/Design_Gameplay_Manual_Flying.md)
- **S33 : Pilot Profile & Achievements** [/]
  - **Gameplay Design :** [Design_Gameplay_Achievements_Badges.md](game_design/Design_Gameplay_Achievements_Badges.md)
  - **Technical Design :** [Design_Technical_Pilot_Profile_Persistence.md](technical_design/Design_Technical_Pilot_Profile_Persistence.md)
- **S35 : Cockpit Scrambler** [ ]


## --- Imported from Status/Handovers ---

### Content from Design_Report_Status_PNC_Pax_Ops.md
# 📊 Design Report: Bilan d'Avancement (PNC, Passagers, Ground Ops)

**Date d'édition :** 29 Mars 2026
**Objet :** Synthèse transversale des spécifications Design (Gameplay & Technique) et du statut d'implémentation des gros "morceaux" de l'application.

---

## 1. 💺 PASSAGERS & VIE À BORD (Story 14 & 17)

**Ce qui est FAIT (Implémenté et Testé) :**
- **Génération Démographique :** La liste des passagers est générée dynamiquement avec noms, pays (45% origin/dest), et profils de poids (Standard/IMC).
- **Seat Map Interactive (100%) :** Le rendu SVG en 2D avec options de zoom/pan (`app.js`) fonctionne. Les couleurs des sièges représentent l'occupation (gris/occupé) et le statut de la ceinture (bleu/rouge).
- **IA Comportementale (Ceintures) :** Intégration dans `CabinManager.cs` des probabilités de respect des consignes selon 3 profils : "Peureux", "Fortes têtes", et "Standard". Maintien d'un équilibre statistique de 2/3 détachés en croisière.
- **Micro-variations d'Anxiété :** Le système détecte les turbulences réelles (Jitter Moteur) et possède un système de *Cooldown* (l'anxiété redescend lentement après une secousse). Un cap (Anxiety Cap) limite cependant la baisse à un pourcentage minimum.

**Ce qui RESTE À FAIRE (Backlog & Bugs) :**
- 🔴 **Desync Visuel Boarding :** (Identifié) L'animation visuelle de remplissage progressif des sièges ne se synchronise pas encore parfaitement avec la vitesse accélérée (x2, x4) du "Timer" Ground Ops. 

---

## 2. 👩‍✈️ CREW & PNC (Story 17.5 & 18)

**Ce qui est FAIT (Implémenté et Testé) :**
- **Logique de Préparation Cabine :** Le Commandant dispose des boutons Intercom adéquats (Prepare For Takeoff/Landing) et d'un "Force Seats" si la cabine n'est pas prête à temps (avec les pénalités `SuperScore` d'inconfort affiliées). Les "Bugs" d'apparition des boutons par phase (taxi/approche) sont **corrigés**.
- **Reporting Intercom Dynamique :** Lors d'un "Request Cabin Report", le moteur Backend lit désormais les jauges pour informer le commandant verbalement (ex: *"Les passagers s'énervent à cause du retard"* ou *"Nous attendons la fin de l'embarquement "* au lieu d'une phrase bateau).

**Ce qui RESTE À FAIRE (Backlog & Prochains Chantiers) :**
- 🔴 **Autonomie "Virtual Crew" :** Le document de design indique que le PNC devrait pouvoir prendre de petites décisions seul s'il a un bon score de `Proactivity` (ex: annoncer le retard lui-même sans attendre que le Commandant presse le bouton). Actuellement, c'est encore très guidé (Boutons manuels).
- 🔴 **Vitesse du Service Repas (Scaling) :** Créer la logique de temps mathématique pour les PNC qui servent les boissons dans l'allée (Service Scaling) pour un vol de 45 minutes par rapport à un vol de 4h.

---

## 3. ⛽ OPÉRATIONS AU SOL & GROUND OPS (Story 3)

**Ce qui est FAIT (Implémenté et Testé) :**
- **Architecture Logique & Timers :** Détermination automatique des "Tiers" d'aéroport (LowCost/Legacy) et calcul du temps du Fueling.
- **Workflow & Séquençage Actif :** Le joueur *peut* outrepasser ou forcer l'embarquement. 
- **Time Warp & First Flight :** La case à cocher « First Flight » ignore logiquement les nettoyages et le carburant initial dans la nouvelle barre de progression. Le clignotement UI massif (Flicker) et le bug de pourcentage à 30% d'entrée de jeu sont **corrigés**.

**Ce qui RESTE À FAIRE (Backlog & GSX) :**
- 🔴 **Interaction Télémétrique Extérieure (Fenix / GSX L:Vars) :** Empêcher totalement le mouvement des passerelles ou du Jetway. GSX Pro intégration reste encore dans la colonne **"À analyser"** (Story 9), notamment pour sniffer s'il y a un camion carburant autour de l'avion ou non via la lecture de données WASM (qui est documentée mais pas encore codée au niveau *Observer*).

---

## 4. 🎤 CHANTIER AUDIO : MOTEUR TTS MICROSOFT (PLAN D'ACTION MVP)

Suite à la décision d'utiliser le moteur vocal natif de Microsoft (TTS) au lieu d'implémenter un moteur lourd de lecture de fichiers MP3/WAV, tout le design audio précédent a été remplacé par ce nouveau plan d'action :

### Phase 1 : Binding des Voix (C# `System.Speech`)
- Assigner une **voix masculine** (ex: Microsoft David ou équivalent) au **Commandant de bord**.
- Assigner une **voix féminine** (ex: Microsoft Zira ou Hortense) au **PNC / Chef de Cabine**.
- Implémenter une gestion de file d'attente basique (*Audio Queue*) pour empêcher les voix de se chevaucher si deux événements se produisent en même temps.

### Phase 2 : Réécriture Intégrale des Scripts (Le Scripting)
- **Réécrire absolument tous les textes d'annonces et d'intercom**, spécifiquement pour qu'ils "sonnent bien" avec un synthétiseur vocal (utilisation stratégique des virgules pour le rythme, suppression des abréviations difficiles à prononcer).
- Création d'un dictionnaire dynamique mixant le texte statique et les variables de vol (Nom de l'aéroport, température d'arrivée, minutes précises de retard "T-Minus", etc.).

---

## Conclusion & Focus Immédiat
L'interface de la cabine est prête à prendre vie de façon sonore. 
**Ton prochain chantier immédiat est donc la Phase 2 du Chantier Audio :** La réécriture conjointe et totale des textes que diront le Commandant de Bord et le/la PNC. Mettre l'Autonomie de côté pour l'instant et se concentrer sur l'établissement des dialogues finaux du MVP.

### Content from Design_Technical_Changelog_Mar29.md
# Résumé des Modifications (Session du Matin - 29 Mars)

Voici la liste exacte et exhaustive de tout ce qui a été modifié, corrigé ou ajouté dans le code ce matin.



## 4. Compilation et Propreté du Code
- Le projet a été nettoyé et recompilé (`FlightSupervisor.UI.dll` généré avec succès en 1.8s) sans produire d'erreurs suite aux modifications.

## 5. Bugs Identifiés & Points à Corriger (En attente)
- [ ] **Desync Animation Boarding / Vitesse Ops :** L'animation et la vitesse de remplissage progressif des passagers sur le plan de cabine (onglet "Manifest") ne correspondent pas à la progression réelle de la jauge "Boarding" de l'onglet "Ground Ops". Il faudra s'assurer que le rythme visuel d'embarquement soit synchronisé avec le réglage de vitesse choisi par l'utilisateur (Realistic/Normal, Short ou Instant) pour que le visuel reste cohérent avec l'état réel des opérations au sol.
- [ ] **Affichage prématuré du PA Apology :** L'annonce "PA Apology for Delay" ne doit apparaître dans les options de communication radio (PNC Comms) *que lorsque l'embarquement est totalement terminé*.
- [x] **Doublon & Refonte des Compteurs (Cabin & Manifest) :** L'information des ceintures (Fasten/Unfasten) apparaissait en double. Suppression de l'affichage au-dessus de la liste. Consolidation des compteurs : ceintures attachées/détachées, pax blessés et sièges vides dans l'en-tête (en haut à droite, au-dessus du plan de la cabine) de l'onglet Manifest.
- [x] **Incrémentation Build:** Numéro de version poussé à 1.0.1.0 dans le fichier projet et compilation réussie (0 erreurs/warnings introduits).

Pendant la phase d'embarquement visuel dans la page manifest simuler le fait que certain passagers attachent leurs ceintures alors que la consigne n'est pas encore donnée.Il faut se baser sur la variable de profil des passagers et priviligier les "peureux" et lancer un dé pour que d'autres en fasse de même.Même mécanique pour les fortes têtes qui refusent d'attacher leurs ceintures. Là aussi ça peut varier régulièrement pendant le vol alors que la consigne est allumée. Sinon pour la phase de cruise il faut que ça soit aussi randomisé mais de façon plus légère et avoir en tête que les 2 tiers des passagers n'ont pas leur ceinture.Et enfin mettre en place un système de on/off des ceintures pour simuler la vie à bord
### Content from Design_Technical_Handover_Mar28.md
# Bilan du Vol de Test & Handover (Mise à jour)

Ce document centralise toutes les corrections, refontes de logique et nouvelles features évoquées suite au dernier vol de test. Il servira de point d'entrée au prochain agent IA pour reprendre le travail là où il s'est arrêté, sachant qu'**aucune ligne de code n'a encore été écrite** pour ces modifications.

## 1. La Matrice de Communication (Intercom vs PA)
Un nouveau document a été créé : `Design_Gameplay_Announcements_Matrix.md`.
Il clarifie exactement qui parle, avec quel code couleur et dans quel canal :
- **[CPT PA] (Vert)** : Le Commandant parle aux passagers (Boutons manuels interactifs : *Welcome*, *Turbulence Warning*).
- **[PNC PA] (Bleu)** : Le Chef de cabine s'adresse aux passagers (Totalement Automatique : *Safety Demo*, *Excuses de retard*).
- **[CPT INT] (Orange)** : Ordres du commandant au PNC via l'Interphone (Boutons manuels interactifs : *Seats for Takeoff*, *Prepare Cabin*).
- **[PNC INT] (Cyan)** : Remontées d'information du PNC vers le poste ("Cabin is Secured", "Hot/Cold"). Automatique sans boutons.

**Action requise pour le prochain agent :** Mettre à jour `app.js` et le C# pour appliquer strictement ces codes couleurs (Tailwind) et réviser les labels des boutons.

## 2. Refonte du Bouton "Turbulence Warning"
- **Bug constaté :** Le bouton disparaissait au-dessus de 10 000 pieds quand les ceintures étaient enlevées.
- **Nouvelle Logique :**
  - N'apparaît **jamais** ni au sol, ni en dessous de 10 000 pieds.
  - Au-dessus de 10 000 fr (Phase Cruise), **si et seulement si** la télémétrie décèle l'activation d'une jauge/détection de turbulences.
  - Le pilote doit alors allumer les fameux *Seatbelt signs*. À ce moment, le bouton de PA Turbulence (`CPT PA` Vert) devient visible pour lui permettre de faire l'annonce aux passagers de regagner leurs sièges. S'il éteint les Seatbelts plus tard, le bouton disparaît à nouveau.

## 3. La Trinité Cabine Bloquée à Zéro 
- **Bug constaté :** Les jauges de confort et d'anxiété restent à 0% alors que la température fluctue, car le système s'articule autour d'une valeur moyenne globale qui a été buggée à zéro, effaçant le côté dynamique des passagers.
- **Action requise pour le prochain agent :** 
  - Refonte de la propriété de calcul dans `CabinManager.cs`. `ComfortLevel`, `PassengerAnxiety`, et `Satisfaction` doivent devenir de vraies moyennes calculées dynamiquement à partir des états individuels (`PassengerState`) des passagers embarqués.
  - Implémenter une boucle de rafraîchissement qui applique de micro-variations (-0.1, +0.2) en fonction des profils psychologiques (`Grumpy`, `Relaxed`) et des conditions ambiantes (Chaud, Froid) pour que les jauges aient l'air "vivantes" en permanence. Éviter absolulment qu'elles puissent se bloquer purement à 0% ou 100% de manière stérile.

## 4. Pénalité Sécurité au Refueling
- **Bug constaté :** Une pénalité est appliquée si le *Seatbelt sign* est allumé pendant le ravitaillement, même quand l'avion est vide !
- **Action requise pour le prochain agent :** Modifier `FlightPhaseManager` ou `GroundOpsManager` pour que cette sécurité ne s'applique et ne déclenche une perte de points **que** si `HasBoardingStarted` est vrai, ou si le manifeste contient déjà des passagers à bord.

## 5. Détections de Télémétrie Manquantes (Freins & Volets)
- **Action requise pour le prochain agent :** 
  - Lire l'accélération sur l'axe longitudinal (`VelocityBodyZ`) pour pénaliser les manœuvres de freinage brutales lors du roulage ou de l'atterrissage.
  - Coder la limite "VFE" : Si les volets (`FlapsAngle > 0`) sont sortis à une vitesse supérieure à ~230 nœuds, déclencher une violation.

## 6. Time Warp
- **Décision (Mise en Stand-by) :** Le joueur a explicitement demandé de **laisser le Time Warp de côté** pour le moment. On ne touche pas à cette logique. On gèle la fonctionnalité ou on accepte la remise à zéro parfaite du chronomètre (annulation de l'avance/retard) le temps d'une itération future.

---

> L'agent suivant doit commencer par appliquer le **point 1 et 2** (Nettoyage de l'UI et redéfinition sémantique des boutons de comms) avant de s'attaquer au réacteur C# de la Trinité.

### Content from Task_Cleaning_Warnings.md
# Task: Compilation Warnings Cleanup

## Contexte
Lors de la compilation du projet, 54 warnings ont été remontés par le compilateur C#. 

## Analyse des Warnings
- **~50 Warnings "Nullable" C# 8.0+** : La majorité de ces alertes (CS8618, CS8600, CS8625, etc.) signalent des variables qui peuvent théoriquement devenir nulles mais n'ont pas le suffixe `?`. Le projet a la balise `<Nullable>enable</Nullable>`, ce qui pousse le compilateur à râler si toutes les strings d'un modèle ne sont pas explicitées comme non-nulles ou initialisées.
- **4 Warnings Fonctionnels (Vrais Défauts)** : 
  - `CS0414` : Dans `CabinManager.cs`, la variable `_hasRewardedTurbulenceReaction` est assignée mais jamais lue ultérieurement.
  - `CS4014` : Dans `MainWindow.xaml.cs` (approx ligne 885), un appel asynchrone n'est pas "awaited", ce qui peut causer une exécution flottante sans gestion d'exception de la Task.
  - `CS8622` : Les deux gestionnaires d'événements `MainWindow_Closed` et `CoreWebView2_WebMessageReceived` ont des signatures où la nullabilité de "sender" ne correspond pas parfaitement à l'EventHandler natif (`object? sender` vs `object sender`).

## Actions Prises (Mises à jour OK)
- [x] **Définir la politique de Nullabilité** : Choix pris de conserver `<Nullable>enable</Nullable>` mais d'utiliser `<NoWarn>` dans `FlightSupervisor.UI.csproj` pour masquer proprement les 50 faux-positifs liés aux variables sans briser le comportement `#nullable` du C# 8.
- [x] **Supprimer** le champ inutilisé `_hasRewardedTurbulenceReaction` dans `CabinManager.cs`.
- [x] **Ajouter** un opérateur `await` devant l'appel asynchrone `Dispatcher.InvokeAsync` dans `MainWindow.xaml.cs`.
- [x] **Ajuster** la signature des deux handlers d'événements WebView2/Window dans `MainWindow.xaml.cs` pour les définir en tant que `object? sender`.
