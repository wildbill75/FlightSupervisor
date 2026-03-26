# Flight Supervisor - Agile Tasks Workspace

> **Légende d'avancement :**
> - `[ ]` : À faire
> - `[?] À tester (via simulateur)`
> - `[!] Testé mais défectueux (Bug)`
> - `[x]` : Développé, testé et validé !

> **HANDOFF POUR L'AGENT SUIVANT :**
> - **Mission accomplie :** L'UI est figée, le design carré/uppercase est en place, les espacements de grille réparés. Le rapport de vol (Flight Report) complet a été implémenté en Glassmorphism. L'architecture de "SuperScore" à 4 piliers est définie dans le manuel. L'Artefact a été restauré et fusionné sur Git.
> - **Documentation :** Voir `docs/UI_Design_System.md` pour les règles graphiques, `docs/User_Manual.md` pour le manuel de fond, et CE FICHIER (`docs/agile_story_task.md`) pour la feuille de route technique.
> - **Prochaine priorité recommandée :** L'Amnistie du Pilote Automatique (Story 4), la dérogation APU (Story 4), et l'intégration ACARS (Story 6).

---

## 🎯 SCOPE & ROADMAP DU PROJET

### 🚀 VERSION 1.0 (MVP)
*La version actuelle en cours de développement.*
- **Features** : Toutes les Stories de 1 à 33 listées ci-dessous.
- **Flotte MSFS 2020 / 2024 Supportée** : 
  - ✅ Tous les avions de base Asobo (Narrow bodies / Piston / Turboprop). *Hors Wide Bodies.*
  - ✅ FlyByWire A32NX Family.
  - ✅ Fenix A320 Family (A319, A320, A321).
- **Objectif** : Une boucle de gameplay Short/Medium Haul parfaitement stable avec une interface UI léchée et une intégration SimConnect solide.

### 🌌 VERSION 2.0 (POST-RELEASE)
*Les concepts ambitieux repoussés pour une architecture ultérieure.*
- **Compatibilité Long Courrier & Wide Bodies** :
  - Support natif des règles ETOPS, fatigue d'équipage prolongée, et Alternate complexes.
- **Extension Flotte Payware MSFS** :
  - iniBuilds A300, A310, A350.
  - Aerosoft A330 (et autres).
  - FSLabs A321 NEO (si dispo).
  - PMDG Family (737 Narrow, 777/747 Wide bodies).
- **Extension Plateforme** :
  - Portage natif pour **X-Plane 12** (via un nouveau pont de télémétrie type XPlaneConnect / UDP ou plugin C++ dédié).
- **Custom Ground Ops Animations (Remplacement GSX)** :
  - Remplacement total de *GSX Pro* par le développement "In-House" de nos propres modèles 3D, véhicules d'escales, et animations MSFS (Attention: Demande de lourdes compétences SDK MSFS et 3D Modélisation `glTF` !).
- **Extension Avions Toliss (X-Plane)** :
  - Toliss A340-600 et famille A320 Neo.

---

## Story 1 : Core Architecture & Telemetry Bridge
- [x] Restructuration totale en WebView2
- [x] Intégration dans le PlaneDataStruct `SimConnectService.cs`
- [x] Ajout des variables Seatbelts, APU Master/Start/Bleed, EXT_LT_NOSE, RWY_TURNOFF
- [x] Câblage des pénalités (APU Cruise, Turbulence sans ceinture, Line-up Nose Light) dans les Managers
- [ ] Recherche L:Vars Fenix A320 (Seatbelts) via WASM
- [ ] **Bugfix** : Trouver la bonne LVAR car Seatbelts Fenix reste inerte.
- [ ] **Validation Live** sous MSFS 2024 / Fenix A320

## Story 2 : Flight State Machine & Resilience
- [x] Fiabilisation par Radio Height à l'atterrissage
- [x] Sécurisation TCAS & Step Climbs (limite d'altitude pour la descente)
- [ ] Tester rigoureusement les transitions de phases réelles (TaxiOut -> Takeoff)
- [x] **Bugfix** : Correction de la phase "Cruise" qui se déclenche prématurément.
- [x] **Bugfix** : Rétrogradation "Cruise -> Climb" corrigée.

## Story 3 : Ground Operations & Anti-Cheat
- [x] Déclenchement manuel via le bouton "Start Ground Operations"
- [x] Sécurité Anti-Cheat (Annulation sur mouvement)
- [x] Sanction Anti-Cheat (Flight Cancelled & figeage)
- [x] Formatage du temps en MM:SS dans les barres de progression
- [x] Refonte Visuelle Tab Ground Ops (Drop-down Accordéons, icones narratives)
- [x] Méta-Barre Ground Ops intégrée au Dashboard
- [x] Refonte des durées de chargement (Paramétrage algorithmique et durée optionnelle)
- [x] Génération aléatoire d'évènements impactant le retard
- [ ] **Tolérance Freins de Parc (Wheel Chocks) :** Ne pas déclencher l'Anti-Cheat si le frein de parc est relâché pendant l'escale TANT QUE l'avion reste strictement immobile.
- [ ] **Séquençage Logique :** Empêcher le Boarding tant que le Cleaning et le Catering ne sont pas terminés.
- [ ] **Maintenance Inspection (Turnaround) :** Générer dynamiquement une étape de "Maintenance" supplémentaire dans les Opérations au Sol si l'atterrissage précédent a été trop rude.
- [ ] **Time-Scale Sync (SOBT vs MSFS) :** Avertir le joueur si le temps restant avant SOBT est insuffisant pour les Ground Ops.

## Story 4 : SuperScore & Flight Safety Envelope
- [x] Système complet d'analyse d'atterrissage (FPM, G-Force)
- [x] Pénalités de phares (Taxi, Landing lights)
- [x] Bonus Line-up : Vérification Strobes/Landing et Taxi (Tolérance T.O Airbus)
- [x] **Malus Éclairage au Sol (Runway Vacation)** : Pénaliser l'activation des *Strobes* ou *Landing Lights* au sol (Avec tolérance de 120s après TaxiIn).
- [x] **Ajustement Pitch Airbus** : Adoucir la limite de *Pitch Angle* à 20° pour la montée.
- [x] **Malus Landing Lights (Croisière)** : Pénaliser l'oubli au-dessus de 10 000 ft.
- [x] **Bonus Gear Up** : Récompenser le pilote (+50) pour une rentrée propre.
- [x] **Malus Sortie de Train Anormale** : Pénaliser sévèrement (-200) la sortie hors approche.
- [x] **Malus Coupure Moteur en Vol** : Pénaliser massivement (-500) la perte de combustion en vol.
- [x] Évaluation atterrissage : Précision du point de toucher (Touchdown Zone) et Axe de piste (Centerline).
- [x] **Amnistie Phase Finale** : Lock de l'historique de V/S sous 2ft AGL pour supprimer les faux-positifs "Impact rude" de compression MSFS.
- [ ] **Dérogation APU en Croisière** : Désactiver le malus (-50 Safety) de l'APU en croisière pour ne pas pénaliser les procédures d'urgences (panne génératrice).
- [x] **Amnistie Pilote Automatique (AP1/AP2)** : Désactiver les malus de pilotage direct (danger de pitch, bank, G) lorsque le Pilote Automatique est engagé.
- [ ] **Tolérance 10 000ft (Seatbelts/Lights/Speed)** : Ajouter une marge de tolérance de +/- 500 ft autour de l'altitude de transition pour ne pas punir les actions décalées de quelques secondes.
- [ ] **Télémétrie Freins (Brake Temp)** : Récupérer et évaluer la chauffe des freins après le dégagement de la piste (Maintenance).
- [ ] **Tail Strike (Maintenance)** : Détecter un cabré excessif (> 11°) alors que le train principal touche le sol (Takeoff/Landing).
- [ ] **Flaps Overspeed (Maintenance)** : Pénaliser le maintien des volets à une vitesse structurellement dangereuse (> 260 kts).
- [ ] **Engine Cooldown (Maintenance)** : Assurer un délai minimum (ex: 3 minutes) entre l'atterrissage et la coupure des réacteurs.
- [ ] **Contamination Hivernale (Maintenance)** : Pénaliser la rentrée des volets au sol après un atterrissage s'il fait très froid (OAT < 3°C).
- [ ] **Go-Around / Unstable Approach** : Pénaliser lourdement un atterrissage forcé depuis une approche instable à 1000ft, mais récompenser le Go-Around.

## Story 5 : Chronometry & Final Flight Report
- [x] Rapatriement de la Timetable sur le Dashboard
- [x] Figeage des temps AOBT/AIBT au pushback et à la porte
- [x] Intégration de la Charte Visuelle de Ponctualité (Gradient Bleu/Vert/Orange/Rouge)
- [x] Séparation du SuperScore en 4 Piliers : Safety, Comfort, Maintenance, Operations.
- [x] Générer le "Flight Report" visuel complet de fin de vol (Modale modernisée Glassmorphism).
- [x] Découplage du rapport : Garantir le prompt du Flight Report à l'arrivée même si SimBrief SchedIn est manquant.
- [ ] Ajournement des pénalités de retard (Amnistie jusqu'à la phase Arrived)

## Story 6 : UI/UX Reorganization
- [x] Relocaliser "Fetch Plan" et "Start Ops" dans la Sidebar gauche
- [x] Réinitialisation complète de l'interface (Ground Ops) lors d'un nouveau Fetch
- [x] Remplacer le bouton "Fetch Plan" par "Cancel Flight" après chargement.
- [x] Dashboard Overview : Ajouter l'en-tête (Origine ✈️ Destination), N° Vol, et Compagnie
- [?] Dashboard Overview : Afficher le layout ICAO/IATA (ex: AFR/AF606) et la Variante Custom de l'Avion (Fetching SimBrief)
- [x] Ajouter un bouton "Save Settings" dans l'onglet Options avec un visuel de confirmation.
- [x] Ajouter une icône circulaire "Reset" (🔄) pour restaurer les paramètres par défaut des Ground Ops
- [x] Mode "Always on Top"
- [x] Bouton Maximize (Plein Écran)
- [x] Option "Ground Ops Duration" dans les Settings
- [x] Slider de probabilité des évènements aléatoires (Ground Ops)
- [x] Ajouter les détails textuels des opérations bloquantes dans la Meta-Bar du Dashboard
- [x] Refonte des 4 blocs du Dashboard (Flight Details, Routing, Payload, Timetable)
- [ ] **Système ACARS Intégré (Météo & Dispatch)** : Ajouter un module "ACARS Messages" sur le Dashboard. Permettre de fetch (ou push auto en descente) le METAR/TAF. Générer une notification sonore/visuelle façon "Incoming Message".

## Story 7 : Localization (i18n) Support
- [x] Frontend (`locales.js`) for static UI (buttons, menus, labels).
- [x] Add a language selection option in the Settings menu (FR/EN).
- [ ] Backend (`LocalizationService.cs`) for generated messages (logs, penalties, briefings).
- [ ] Ensure language switch syncs Backend instantly (e.g., regenerate Weather Briefing).

## Story 8 : Third-Party Weather Integration
- [x] Options UI : Ajouter le choix de la source météo (SimBrief vs Active Sky vs MSFS)
- [ ] Backend : Détecter et lire Active Sky (`current_wx_snapshot.txt` ou API HTTP locale)
- [ ] Logique : Remplacer les données SimBrief par la source tierce dans le Tactical Briefing

## Story 9 : GSX Advanced Integration (Hybrid Master/Slave Architecture)
- [x] Options UI : Ajouter un toggle "Enable GSX Auto-Sync"
- [ ] Pré-requis Utilisateur : Avertir l'utilisateur de désactiver toutes les options GSX auto dans les tablettes de ses avions.
- [ ] Backend : Étude de faisabilité lecture et écriture des LVARs GSX (`L:FSDT_GSX_BOARDING_STATE`, `L:FSDT_GSX_MENU_SELECT`) via SimConnect.

## Story 10 : Measuring Units Architecture
- [x] Backend : Créer un service de conversion (KGS/LBS, C/F, FT/M, HPA/INHG, KTS/KMH)
- [x] Backend (SimBrief) : Convertir les poids bruts (`estzfw`, `esttow`, `pax_count`, etc.) selon l'option choisie
- [x] Backend (Weather) : Parser le METAR et convertir Température et Pression de QNH
- [x] Frontend : Mettre à jour dynamiquement l'affichage du Briefing.
- [x] **Bugfix** : Correction du formatage de temps (12H/24H) à travers l'UI pour unifier l'expérience.

## Story 11 : Contextual Tooltips & Onboarding
- [ ] Créer un système d'infobulles contextuelles pour expliquer les acronymes complexes.
- [ ] Développer un mini-tutoriel d'onboarding "premier lancement".

## Story 12 : Documentation Centrale
- [x] Rédaction initiale du document de référence détaillant toutes les features (User Manual).
- [x] Inscription définitive des 4 piliers du SuperScore au coeur du manuel.
- [x] Migration de l'Agile Story Task (Backlog technique) en dur dans le dépôt Git (ce fichier).
- [ ] Gérer l'itération et la mise à jour continue du manuel au fil du développement.

## Story 13 : Le Briefing & Planning de Rotation (Multi-Leg)
- [ ] **Mise à jour visuelle du Briefing :** Rajouter une section "Day Roster" affichant le vol actuel comme "Leg 1".
- [ ] **Logique SimBrief in-game :** Coder la méthode "Fetch Next Leg" qui va télécharger le plan généré sur l'EFB pendant l'escale.
- [ ] **Mise à jour Météo :** Régénérer le briefing météo tactique (METAR/TAF) sur la nouvelle destination au moment du "Fetch Next Leg".

## Story 14 : Passenger & Crew Manifest (Onglet Cabine)
- [x] **Moteur PNC (Backend C#) :** Génération des PNC (1 PNC par 50 sièges).
- [x] **Moteur Passagers (Backend C#) :** Générateur de nationalités basé sur l'aéroport.
- [x] **Logique Seat Map (Backend C#) :** Assigner virtuellement un siège.
- [x] **UI Nouvelle Page (Frontend) :** Créer un nouvel onglet "Cabin / Manifest".
- [x] **UI Grille Visuelle (Frontend) :** Coder le composant graphique (Seat Map 2D interactif).

## Story 15 : Advanced Dispatch & Meteorological Briefing
- [x] **Analyseur Temporel TAF :** Parser les TAFs (`FM`, `BECMG`, `TEMPO`) et croiser avec l'ETA.
- [x] **Runway Anticipation Predictor :** Affiche la piste prévue et détecte les vents arrière potentiels.
- [x] **Parseur de NOTAMs :** Extraire et résumer les NOTAMs de départ/arrivée.
- [x] **Parseur de SIGMETs :** Détecter la météo convective et le givrage en route.
- [x] **Refonte Visuelle du Briefing :** Grille CSS et design de tickets pour Alternate / Routing.
- [x] **L'Alternate Airport :** Extraire l'Alternate de SimBrief et l'afficher formellement.
- [x] **Route Optimization :** Analyser le profil de Tropopause et exposer les Step Climbs prévus.

## Story 16 : In-Flight Judgment & Airmanship
- [ ] **SuperScore Météo :** Pénaliser la traversée "aveugle" d'un volume SIGMET actif.
- [ ] **Récompense de Déviation Tactique :** Détecter et récompenser la déviation manuelle hors route (`GPS WP CROSS TRK` élevé) pour contourner le mauvais temps.

## Story 17 : Passenger Satisfaction & Cabin Management
- [x] **Anxiété Cabine (Turbulences) :** Créer la jauge "Passenger Anxiety" basée sur G-Force et assiettes.
- [x] **Anxiété Cabine (Retard SOBT) :** Retard au départ = angoisse progressive.
- [x] **Interface "Captain Announcements" (MVP) :** Générer un bouton dynamique ("Announce Delay", "Announce Turbulence") sur le Dashboard qui calme l'anxiété.
- [ ] **Cabin Intercom (MVP) :** Remplacer le bouton par un module Intercom (Menu déroulant des ordres : "Prepare for Takeoff", "Seats for Landing", etc. + Bouton Send).
- [ ] **Logique Cabin Secured (Backend) :** Intégrer un délai de réponse des PNC suite à un ordre avant d'autoriser certaines phases (ex: Takeoff) sous peine de pénalité de sécurité grave.
- [ ] **Intégration LVAR Fenix (Advanced) :** Binder le bouton physique `CAB` du Radio Management Panel (RMP) du Fenix A320 pour déclencher l'Intercom directement depuis le cockpit virtuel.
- [x] **Conséquences Abort / Lost Baggage :** Répercussions des Opérations au Sol abrégées (Plaintes Catering incomplet, valises manquantes).
- [x] **Intégration UI Stitch :** Refonte générale des jauges vers la charte Neon/Glassmorphism.

## Story 18 : Impondérables & Diversions (Déroutements)
- [ ] **Générateur de Crise :** Créer un moteur de probabilité générant une urgence médicale.
- [ ] **State Machine "Diversion" :** Ajouter un état de vol `Diverting` pour clore le plan de vol initial sur un aéroport d'atterrissage impromptu valide.

## Story 19 : Airline Policies & Risk Management
- [ ] **Base de données Compagnies (Profils) :** Profils de tolérance (Low-Cost vs Legacy/Premium).
- [ ] **Pondération SuperScore (Delay vs Comfort) :** Adapter dynamiquement le poids des malus selon le profil de la compagnie.
- [ ] **Le Dilemme "Missing Pax" (Boarding) :** Si un passager manque, Interactive decision ("Attendre" vs "Laisser").

## Story 20 : Virtual Reality & MSFS In-Game Toolbar
- [x] **Développement du Panel In-Game :** Packager un add-on MSFS natif (HTML/JS)
- [x] **Communication Localhost (WebSocket) :** Établir le lien bidirectionnel en temps réel.

## Story 21 : Fuel Economy & Ecological Operations
- [ ] **Télémétrie Fuel (Backend C#) :** Logger le fuel prévu vs le final actuel.
- [ ] **SuperScore Company Ops :** Récompenser un vol où la consommation est inférieure ou égale au plan, et analyser la brûlure d'Extra Fuel.

## Story 22 : Flight Logger & History
- [ ] **Bouton Save Report :** Ajouter un bouton "Save & Close" sur la modale du Final Flight Report pour sauvegarder la session en JSON local.
- [ ] **Page Flight History :** Créer un onglet UI affichant l'historique complet.

## Story 23 : True Airmanship (Manual Flying Bonus)
- [x] **Détection A/P & A/THR (Approche) :** Démarrer un chrono au passage des 4000ft (début phase Approach) jusqu'au Touchdown.
- [x] **Calcul Temps Manuel :** Comptabiliser le temps passé totalement en manuel.
- [x] **SuperScore Airmanship :** Récompenser le pilotage manuel avec un gros bonus (+100, +200).

## Story 24 : Moteur de Profils Aérodynamiques (Aircraft Profiles)
- [ ] **Extraction de la Catégorie :** Utiliser la variable `AircraftCategory` (Light, Medium, Heavy) de SimBrief pour le profil de base.
- [ ] **Base Config JSON :** Créer un fichier de configuration des limites aérodynamiques et structurelles par appareil (Max Bank, G-Force Limite, Max Pitch).
- [ ] **SuperScore Ajustable :** Le FlightPhaseManager doit lire le profil d'avion injecté au lieu d'utiliser les variables "en dur" (Part 25) du code C# pour juger les violations de confort.

## Story 24 : Immersive Mode (Hidden Scoring)
- [ ] **Option UI :** Ajouter une case à cocher "Immersive Mode".
- [ ] **Masquage Frontend :** Masquer les compteurs de points et popups pendant le vol pour ne pas break l'immersion, ne les découvrant qu'au Debrief Final.

## Story 25 : Chief Pilot Narrative Debrief (Flight Report)
- [?] **Moteur de Synthèse (JS) :** Parseur des pénalités du Report Final.
- [?] **Génération de Texte :** Construire dynamiquement 2/3 paragraphes narratifs du Head of Operations selon la réussite du vol.
- [?] **Intégration Modale :** Ajouter ce bloc commenté au bas de la modale Flight Report.

## Story 26 : Debrief Breakdown Accordion
- [x] **History Logs Backend :** Sauvegarder chaque variation de score (`amount`, `reason`, `category`) dans une liste.
- [x] **Injection Payload :** Envoyer cet Array dans le WebMessage `EndFlight`.
- [?] **UI Accordéon :** Créer une zone dépliante dans la Modale Debrief listant toutes les pénalités et bonus obtenus ligne par ligne de manière élégante (Glassmorphism).

## Story 27 : Dynamic Cabin Comfort Feedback (Intercom)
- [ ] **Jauges Dynamiques :** Câbler les jauges "Anxieté" (commence à 0% vert -> rouge) et "Confort" (commence à 100% bleu -> rouge) aux événements physiques (V/S, Bank, G-Force).
- [ ] **Régénération :** Permettre aux jauges de s'améliorer progressivement avec le temps si les paramètres de vol sont cléments.
- [ ] **Ordre PNC "Cabin Status" :** Ajouter "Comment ça se passe en cabine ?" dans le Dropdown Intercom du Dashboard.
- [ ] **Narrative Response :** Générer une réponse contextuelle des PNC (ex: "Horrible, une valise est tombée" vs "Tout va bien on sert les cafés") en fonction de l'historique récent des pénalités ou du niveau de confort actuel, qui mettra également à jour les jauges visuelles.

## Story 28 : Dynamic Passenger Demographics & Airline Reputation
- [x] **Base de données Compagnies** : Injecter un ranking empirique (Premium, Standard, LowCost).
- [x] **Générateur Démographique** : Créer aléatoirement un profil de passagers (Grumpy, Relaxed, Anxious).
- [x] **Modificateurs Dynamiques** : Altérer les jauges Comfort/Anxiety selon la compagnie et le profil.
- [x] **Pénalité de Retard (Sans Comms)** : Incrémenter l'anxiété et le confort à la porte (AtGate) si le SOBT est dépassé et qu'aucune annonce de retard n'a été faite.

## Story 29 : Realistic Ground Ops & Time Warp
- [?] **Automated Scheduling (T-Minus)** : Lier le Catering, Cleaning, et Boarding à l'horloge interne par rapport au SOBT (SimBrief), selon la catégorie de l'avion (Ex: Boarding à T-40 min pour A320).
- [?] **Déclencheurs Manuels (Pilot-in-Command)** : L'approvisionnement en fioul ("Request Refueling") nécessitera une activation via UI.
- [?] **Bouton Time Warp ("Skip to Departure")** : Ajouter un bouton UI qui complète instantanément les Ground Ops en attente.
- [?] **Synchronisation ZULU MSFS** : Câbler le bouton Time Warp pour envoyer les évènements SimConnect ZULU_HOURS_SET / ZULU_MINUTES_SET et aligner le temps MSFS sur le SOBT.
- [?] **Pénalité de Convenience (Trade-off)** : Appliquer un malus fixe au SuperScore si le Time Warp est utilisé.

## Story 30 : Difficulty Tiers & Immersive Options (Global Architecture)
- [ ] **Refonte de l'onglet Settings** : Ajouter un sélecteur de difficulté globale : Easy / Normal / Hardcore.
- [ ] **Tier 'Easy'** : Désactive les malus de confort passager, autorise le Time Warp sans pénalité.
- [ ] **Tier 'Normal' (Défaut)** : Expérience équilibrée. Le Time Warp applique un léger malus.
- [ ] **Tier 'Hardcore'** : Tolérance zéro (Catering complet exigé, SOBT strict, pénalités X2, Time Warp lourdement sanctionné).
- [ ] **Câblage SuperScoreManager** : Lier la variable globale CurrentDifficulty pour pondérer ou désactiver les pénalités calculées.

## Story 31 : Dynamic Ground Ops Virtual Actors (Live Logging)
- [ ] **Définition des Acteurs Virtuels** :
  - **Chef de Cabine (Purser)** : Gère le Service Traiteur (Catering), le Nettoyage (Cleaning), et l'accueil Embarquement.
  - **Chef d'Escale (Gate Agent)** : Gère le flux Embarquement depuis le terminal et les passagers manquants.
  - **Agent de Piste (Ramp Agent)** : Gère l'Avitaillement (Fuel), le Chargement des Soutes (Baggage/Cargo), et le Service d'Eau/Toilettes.
- [ ] **Remplacement des Textes Statiques** : Mettre à jour l'UI des 6 modules Ground Ops pour afficher un flux de logs dynamiques narratifs au lieu d'une phrase générique.
- [ ] **Générateur de Dialogues (Backend ou JS)** : Créer des phrases interactives (ex: "Ramp Agent : Tuyau connecté, transfert en cours (1500 kg / 4500 kg)...").

## Story 32 : Airline Tycoon & Captain Decisions Engine (Sprint 2)
- [ ] **Modèle de Réputation (Reputation Engine)** : Remplacer le simple Enum (Premium/Standard/LowCost) par une structure riche :
  - *Hard Product* (Confort des sièges, espace). Impacte la régénération de base du ComfortLevel.
  - *Soft Product* (Nourriture, service, PNC). Impacte la tolérance des passagers aux aléas.
  - *Safety & Maintenance Record* (Accidentologie passée). Voler sur une compagnie "douteuse" démarre le vol avec une anxiété de base élevée pour tous les passagers (surtout les anxieux).
  - *Punctuality Score* : La tolérance de la compagnie face aux retards.
- [ ] **Airline Identity Card (Dashboard UI)** : Créer un bloc visuel sur le Dashboard affichant la Note Globale sur 100 de la compagnie et 3 "Directives" sous forme de bullet points (Ex: "La ponctualité prime sur le confort client").
- [ ] **Moteur d'Évènements Tolérance (Decision Tree)** : Générer des Impondérables Ground Ops interactifs :
  - Passagers manquants -> [Attendre / Débarquer les valises].
  - Camion Catering en panne -> [Attendre la réparation / Partir sans Catering].
  - Passager ivre/agressif -> [Refuser l'accès (Retard) / Laisser faire (Risque Sécurité)].
  - **⚠️ [TODO 1.0 RELEASE]** : Ré-équilibrer la probabilité d'apparition des évènements (actuellement boostée pour les tests) pour la version finale.
- [ ] **Scoring Pondéré** : Le SuperScoreManager doit évaluer la décision finale à l'aune des Directives de la Compagnie de la carte d'identité.
- [x] **Système d'Objectifs (Airline Objectives)** : Ajouter des contrats chiffrés et évaluables (ex: `MaxDelay=15`, `MinComfort=80`, `MaxTouchdownFpm=-200`) dans `Airlines.json`. Les intégrer au `SuperScoreManager.cs` pour une validation stricte (Pass/Fail) à la fin du vol, avec affichage d'une toute nouvelle section "Airline Objectives" [✅/❌] dans le composant UI du Flight Report final.

## Story 33 : Pilot Profile & Gamification (Statistics & Achievements)
- [!] **Modèle de Données (C#)** : Persistance locale capricieuse (Le JS ne récupère pas toujours le JSON au lancement). Bug enregistré, on passe à autre chose pour le moment.
- [x] **Tab Profile UI (Frontend)** : Dessiner un tout nouvel onglet affichant visuellement le passeport du pilote, ses heures de vol et ses badges façon vitrine.
- [ ] **Statistiques Globales (Logbook)** : Cumul des `Total Flights`, `Block Time`, `Distance Flown`, `Passengers`, `Cargo Hauled`, et `Fuel Burned`.
- [ ] **Métriques de Performance (Leaderboard Data)** : Sauvegarder `Average SuperScore`, `Highest Score`, `Punctuality %`, `Smoothest Touchdown`, `Hardest Impact`, et le % de vol manuel.
- [ ] **Achievement Engine (Backend)** : À la clôture du vol, parser le vol par rapport à une liste de badges bloqués pour déclencher des unlocks if conditions met.
- [ ] **Badges Tier 1 (Rookie)** : `First Entry`, `Butter the Bread` (> -150fpm), `Swiss Watch` (0 retard), `By the Book` (Ops parfaites).
- [ ] **Badges Tier 2 (Line Captain)** : `Frequent Flyer` (50 vols), `The Hand of God` (10min vol manuel), `Company Man` (SuperScore > 1000), `Safe and Sound` (10 vols sans pénalité de sécurité), `Go-Around, Flaps 3`.
- [ ] **Badges Tier 3 (Check Airman)** : `Flawless Execution` (0 pénalités + 100% objs), `Through the Storm` (Atterrissage par xwind violent / MSFS sans se faire engueuler par les PAX), `Feather Touch` (Touchdown entre -10 et -50 fpm), `Iron Bladder` (10h Block Time en continu), `Airmanship Master` (SuperScore > 1200).
- [ ] **Secret Tier (Casseroles)** : `Spine Crusher` (Touch < -600 fpm), `Coffee Machine Broken` (Partir sans Catering), `Pitch Black` (Atterrir sans Landing Lights la nuit).

### Bugs Fixés
- [?] **Traductions Ground Ops** : Remplacement des constantes Javascript en dur par le dictionnaire `locales.js` pour supporter dynamiquement le FR et l'EN.
- [?] **Feedback Immersif Modal** : Le log de feedback (ex: "Décision Cdt reçue") après avoir pris une décision depuis un Pop-up s'affichait dans l'onglet des pénalités caché. Le flux C# a été routé vers `type = "cabinLog"` pour apparaître directement dans le live feed des opérations au sol.
