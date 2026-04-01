# Design Gameplay: Multi-Leg & Rotations

## 1. Overview
La fonctionnalité **Multi-Leg (Rotations)** est le cœur de l'expérience "Pilote de Ligne" de Flight Supervisor. Au lieu d'évaluer un simple vol isolé ("A vers B"), le logiciel évalue une **journée complète de travail** (ex: "A -> B -> C -> A").
Le défi ne réside plus uniquement dans le pilotage d'un vol, mais dans la gestion globale du temps, de la fatigue et de l'effet domino des retards sur une rotation entière.

## 2. Global Mechanics & Flow

### 🔄 La Période de "Turnaround" (L'Entre-Deux Vols)
- **Lecture du Flight Report :** À la fin du premier vol (frein de parc serré à la porte), le joueur accède à son compte-rendu de vol (Flight Report). Une fois la lecture du vol terminée, le joueur **revient sur l'application principale (Main Dashboard)**. Le logiciel ne s'arrête pas, et Flight Supervisor a pleinement conscience de l'état "usé" de la cabine.
- **La Phase de Deboarding (Débarquement) :** Le Commandant de Bord (Flight Deck) doit donner l'ordre explicite au PNC de commencer le débarquement. Le débarquement fonctionne exactement de la même manière que l'embarquement, mais à l'envers.
  - **Règles de Sécurité Strictes :** Pour que le débarquement se déroule correctement et légalement, il est impératif que les **consignes lumineuses de ceintures (Seatbelts) soient sur OFF** et que les **moteurs (Engines) soient arrêtés**. Le non-respect de ces règles entraînera de très lourdes pénalités de sécurité.
- **Continuité des Systèmes :** L'avion reste connecté. Une fois la cabine vidée, le nouveau cycle *Ground Ops* s'enchaîne avec le nouveau délai (SOBT) dicté par le prochain plan de vol (déjà chargé en mémoire lors du Briefing matinal).

### 🗂️ Pré-Planification de la Journée ("Up-Front Dispatch")
- Au démarrage exclusif du "Shift" (prise de service), l'utilisateur passe par la modale de **Dispatch Briefing**. 
- Contrairement à un système séquentiel, le joueur **génère ici l'ensemble des "Legs" de sa journée** (ex: Vol A -> B, Vol B -> C) via SimBrief, avant même de rejoindre son avion.
- **Règle métier (Max Cap) :** Par souci de réalisme temporel légal (Duty Time FTL) et de design de l'UI, le logiciel limite volontairement la journée de travail à **4 Rotations maximum** (4 "Legs").
- **Journey Overview :** Le menu de briefing se transforme en une feuille de route "Overview de la Journée", offrant au Commandant de bord une vision d'ensemble de l'itinéraire, ce qui permet d'anticiper les annonces passagers de fin de journée, la météo prédictive globale (météo sur plusieurs heures), et l'heure de fin de service.

### ⏱️ Gestion du Temps & Effet Domino (Domino Delay Effect)
- **Le problème du retard :** Si le Vol 1 ("Leg 1") arrive avec 25 minutes de retard, le *Scheduled Time of Departure* (STD) du Vol 2 est menacé. Le joueur va devoir prendre des décisions pour "rattraper" le temps perdu (accélérer le nettoyage, demander au dispatch d'augmenter le Cost Index pour voler plus vite, etc.).
- Chaque "Leg" possède son propre Timer d'embarquement, mais la journée entière possède une contrainte de temps globale.

## 3. Composants UI (Dashboard & Timetable)

### 📊 Flight Timetable (Master View)
Le widget de gauche (`Flight Timetable`) ne doit plus seulement afficher "LFPO to LFBO". Il doit comporter un **Onglet Rotation** (ou une liste déroulante) :
- **Leg 1 :** LFPO ✈️ LFBO | *Statut: Completed (✅)*
- **Leg 2 :** LFBO ✈️ LEMD | *Statut: Active (🟢)* | *STD: 14:30z*
- **Leg 3 :** LEMD ✈️ LFPO | *Statut: Pending (⏳)*

### 📝 Le Double Rapport (Leg Report vs Duty Report)
- **Fin de Leg :** Un "Mini-Report" popup apparaît brièvement au bloc pour résumer le vol qu'on vient de faire (Score + Temps + Incidents).
- **Fin de Rotation (End of Duty) :** Quand le joueur clique sur un bouton ou termine l'ultime étape prévue, le **Grand Rapport Final (Black Box)** est généré. Il agrège les scores de tous les Legs pour donner une note globale de la journée (ex: *Note Mondiale : A-*).

---

## 4. 💡 Propositions / "Trucs supplémentaires" pour pousser le réalisme

*Voici 4 concepts inédits dédiés exclusivement au format Multi-Leg pour maximiser l'immersion :*

### A. La Gestion de la Fatigue (Duty Time Limitation - FTL)
- Au début du premier vol, un chronomètre global invisible de "Temps de Service (Duty Time)" se lance.
- D'après la loi AESA, un équipage a souvent une limite de 12 à 14h de temps de service. Si le joueur enchaîne 6 jambes de vol et subit trop de retards (orages, trafic), il risque de **dépasser son temps légal**. S'il décolle alors que son Duty Time théorique dépasse la limite, pénalité colossale (violation légale massive).

### B. "Technical Carry-Over" (Rétention des Pannes / MEL)
- Si le joueur abîme un système ou grille l'APU lors du Vol 1 et qu'il n'est pas "mortel" (permettant un Dispatch MEL - Minimum Equipment List), **l'avion reste cassé pour le Vol 2**.
- Sur le Leg 2, le joueur devra donc gérer ses opérations au sol différemment (ex: obligation de commander un Ground Power Unit (GPU) et un Air Starter Unit (ASU) à chaque escale car l'APU est mort).

### C. Fatigue Cabine & PNC (PNC Morale Tracker)
- Refaire la cabine coûte de l'énergie aux PNC (surtout en compagnie Low-Cost où ils nettoient eux-mêmes).
- Plus on avance dans les "Legs" de la journée, plus le PNC risque d'être lent lors des opérations au sol, ou moins tolérant face aux passagers (anxiété de base qui augmente très légèrement sur le vol 4 de la journée).

### D. Catering Persistant (Double Load)
- Pour gagner du temps, certaines compagnies chargent la nourriture pour **deux vols (Aller-Retour)** sur la plateforme principale.
- Cela signifie que si le joueur programme "Paris -> Toulouse -> Paris" pour son avion, il n'y a de camion de Catering **qu'à Paris**. À Toulouse, le bouton Catering n'est pas requis. Le poids de l'avion prend en compte les chariots pleins du vol retour pesant pour le vol aller !

---

## 5. Persistance de l'État des Ground Ops (Le Défi du Turnaround)

Une escale (Turnaround) en multi-leg diffère grandement du dispatch initial (Aircraft Preparation). L'objectif majeur de cette phase est de jouer contre la montre : ne solliciter que les services terrestres strictement nécessaires pour "sauver" le STD (Scheduled Time of Departure).

Pour rendre cette gestion stratégique, **Flight Supervisor persiste l'état matériel de l'avion et de la cabine** entre chaque "Leg" :

### 🛢️ Refueling (Lecture Simulateur en temps réel)
- Contrairement au dispatch initial, l'escale conserve tout le carburant du vol précédent. Il est vital que le système **lise l'état immédiat du fuel à bord** (Fuel Weight) pour le donner au séquenceur.
- **Logique :** Flight Supervisor s'actualise via SimConnect lors de la transition. Si le plan SimBrief du Leg 2 exige 6,500 kg de fuel et qu'il reste 6,000 kg du Leg précédent (cas fréquent de "Tankering"), le **Refuel Service** ne charge dynamiquement que le delta manquant (500 kg). Le temps d'attente du camion-citerne s'en trouve massivement réduit.

### 🧹 État de la Cabine (Cabin Cleanliness)
- L'état de saleté de la cabine s'accumule d'un vol à l'autre en fonction de la durée du vol, de la jauge passager et des crises éventuelles.
- **Logique :** Le joueur peut choisir d'ignorer le service de nettoyage (Cleaning) s'il est très en retard. L'escale est virtuellement accélérée, mais le Leg suivant démarrera avec un niveau de "Comfort" capé ou en baisse continue due à l'état misérable de la cabine.
- **Règle additionnelle ("Sick Bags") :** Si le vol précédent a subi un stress énorme (fortes turbulences, anxiété > 80%), la cabine sera drastiquement plus sale (passagers malades, précipitation). Ignorer le nettoyage après un vol chaotique sera beaucoup plus lourd de conséquences qu'après un vol calme.

### 💧 Eau Potable & Toilettes (Water / Waste)
- Les réservoirs d'eau et les cuves de déchets ne se vident/remplissent pas magiquement à la coupure des moteurs. Ce sont des valeurs en pourcentage stockées dans le moteur Backend.
- **Logique :** Si le camion "Water/Waste" n'est pas appelé après un long vol parce que le joueur est pressé, le Leg suivant court le risque d'une annonce PNC catastrophique (ex: "Captain, water tanks are empty, restrooms are compromised"), causant un pic immédiat d'Anxiété et une chute de Satisfaction.
- **Règle additionnelle ("Stress Bowels") :** La consommation d'eau et le remplissage des cuves de déchets (Waste) sont proportionnels à l'Anxiété moyenne du vol. Des passagers stressés par un vol très mouvementé fréquenteront beaucoup plus les toilettes. Un vol retour après une telle épreuve sans réapprovisionnement risque le blocage total des WC !

### 🍱 Comptabilité du Catering (Meal Rations)
- Le Catering n'est plus un bouton binaire : c'est un "Stock" de rations.
- **Logique :** Une requête de Catering complète remplit les stocks à 100%. Si le vol Leg 1 n'a consommé que 40% (ex: vol de 45 minutes), l'avion atterrit avec 60% de rations.
- Le joueur peut faire l'impasse sur le Catering au Leg 2 pour vite refermer les soutes. Cependant, si le Leg 2 est long et épuise les rations restantes avant le service de repas (Meal Service), le statut "Meal Shortage" s'activera, effondrant le moral des PNC et la Satisfaction passager.

Toutes ces mécaniques forcent le joueur à gérer ses Ground Ops comme un puzzle logistique, où l'économie de minutes d'un service omis doit être pesée contre les pénalités de confort et de sécurité du vol à venir.

---

## 6. Workflow du Début de Service (Shift Start Configurator)

Pour initier la journée de travail, l'interface utilisateur isole la création du **Leg 1** de la préparation matérielle de la cabine via deux modales distinctes :

### Étape 1 : Le Choix de l'État Initial (Modal 1)
Le joueur ne choisit ici que l'état matériel de son avion au moment de la prise de fonction.
- **Pristine (Cold & Dark / First Flight) :** L'avion est totalement propre, les soutes à eau sont pleines, les poubelles sont vides, et le catering est chargé au maximum. Le joueur n'a pas besoin de faire de nettoyage ou de catering au premier départ. Ce statut ne signifie pas que l'avion est "froid" d'un point de vue moteur (le joueur peut très bien arriver dans un avion avec l'APU allumé par la maintenance), mais que **la cabine est vierge**.
- **Crew Handover (Turnaround) :** Le joueur prend le relais d'un équipage précédent. L'avion arrive avec une cabine **sale**, des cuves entamées, et potentiellement un retard. Cela force le joueur à débuter sa propre session par un processus complet de Ground Ops ("Turnaround") chronométré, avant même son tout premier vol !

### Étape 2 : L'Importation des Vols (Briefing Overview)
Une fois l'état matériel choisi, la modale de **Dispatch Briefing** s'ouvre pour importer les plans de vol (Leg 1 à Leg 4 max).
- Le compte SimBrief du joueur (précédemment renseigné dans l'onglet Settings) y est affiché. L'utilisateur clique sur "OPEN DISPATCH WINDOW" pour ouvrir la surcouche Navigraph.
- Dès validation du premier vol, le jeu propose une boucle interactive :
  - **[ + AJOUTER LEG SUIVANTE ]** -> Remet le compteur à "Leg 2", vide le popup, et te permet de générer ton vol retour/suivant (jusqu'à une limite stricte de 4 vols/jour).
  - **[ ✅ TERMINER LE BRIEFING ET ALLER À L'AVION ]** -> Quitte la vue Briefing Overview.
- Grâce aux données Up-front, la vue du "Flight Timetable" et du briefing peut désormais afficher une vue complète (Journey Overview) de toute la journée de travail, permettant d'adopter une stratégie de météo long-terme et d'anticiper l'heure de fin de service global.

---

## 7. La Phase "Turnaround" et le Timer Chronométré

Le cœur de la pression du Multi-Leg repose sur le chronomètre du **Turnaround**. 
- Après l'atterrissage et l'arrivée en porte du Leg 1 (Engine Shutdown, Seatbelts OFF, passagers débarqués), le vol passe officiellement dans la phase `Turnaround`.
- **Timer d'Efficacité :** Pendant cette phase, un compte à rebours se lance (Visible dans l'UI). Le temps alloué est dicté par le paramètre _Turnaround Efficiency_ du profil de la Compagnie Aérienne (ex: Ryanair = 25 minutes, Air France = 40 minutes).
- Le joueur doit faire vider les poubelles, nettoyer la cabine, re-caterer l'avion et embarquer les nouveaux passagers du Leg 2 **avant la fin de ce chronomètre**. Tout dépassement entamera le capital ponctualité de la rotation et causera du "Domino Delay", rendant les vols suivants de plus en plus difficiles à assurer à l'heure.
