# Design Gameplay - Persistance Avancée et "Airframes Vivants"

> [!NOTE]
> **Vision Globale** : Un avion n'appartient pas au joueur. Le joueur n'est qu'un maillon de la chaîne d'une compagnie aérienne. Entre deux sessions de MSFS, l'avion (identifié par son Airframe de Simbrief) continue sa vie avec des équipages "virtuels". L'état dans lequel le joueur le récupère dépend de ce qui s'est passé pendant son absence, de l'âge de l'avion, et du niveau de maintenance de la compagnie.

## 1. Le Moteur d'Éclipse Temporelle (Time-Lapse Engine)

Lorsqu'un vol est initialisé via SimBrief, le système interroge la base de données locale des **Airframes**. 
Si l'avion existe déjà (le joueur l'a piloté dans le passé), le moteur calcule le *Delta T* (Temps écoulé dans la vraie vie depuis le dernier atterrissage enregistré).

**Génération de l'Activité Virtuelle :**
- **Cycle d'utilisation** : Selon le profil de la compagnie (`AirlineProfile`), on attribue un taux d'utilisation de l'avion. (ex: *Ryanair = 12h de vol/jour. Lufthansa = 9h/jour*).
- **Incrémentation des statistiques** : Le moteur ajoute automatiquement les *Heures de Vol (FH)* et les *Cycles de Vol (FC)* simulés au compteur global de l'Airframe.
- **L'Âge de l'avion** : Basé sur l'immatriculation, si l'âge réel ne peut être trouvé, on lui génère un âge selon l'année en cours et son nombre global d'heures de vols. 

## 2. Carnet de Vol & Tech Log (Le Registre Technique)

L'Airframe dispose désormais d'un **Tech Log** persistant. C'est le carnet de santé de l'avion.

- **L'historique du Joueur** : Si le joueur fait un *Hard Landing* à +600 fpm, ou coupe les moteurs de façon incorrecte, ceci est inscrit dans le Tech Log par la maintenance.
- **La simulation des Équipages Virtuels** : Pendant le *Time-Lapse*, le moteur simule des vols virtuels. Chaque vol virtuel a un % de chances de générer une "Panne" ou une "Remarque technique" mineure.
- **Le Bouclier de la Compagnie** : Ce pourcentage d'incidents virtuels est fortement mitigé par la réputation `SafetyRecord` et `MaintenanceRecord` de la compagnie aérienne :
  - **Compagnie Tier 1 (Ex: Air France, Emirates)** : L'avion a beaucoup volé pendant les 5 jours, mais la maintenance l'a chouchouté et a résolu toutes les remarques. L'avion revient propre.
  - **Compagnie Low-Cost ou Liste Noire (Tier 3/4)** : L'avion revient avec des *Deferred Defects* (Pannes reportées). La maintenance a mis du scotch (Speed Tape).

## 3. Impact direct sur le Gameplay (La phase Pré-vol)

## 3. Typologie des Pannes (Impact direct sur le Gameplay)

> [!IMPORTANT]
> Récupérer un avion "usé" n'est pas qu'un aspect cosmétique de l'interface, cela impacte les opérations et la note du joueur. Pour garantir une expérience immersive stricte, **aucune panne ne sera purement "cosmétique" (fake)**. Toutes les pannes consignées dans le Tech Log DOIVENT avoir un impact mécanique réel dans le simulateur ou un impact mesurable sur la note des passagers. Par ailleurs, si des réparations ou vérifications sont dictées par le *Tech Log*, elles se déroulent en temps réel et prolongent la durée des *Ground Ops*.

### 3.1. Pannes Cabine & Confort (Soft / Hard Product)
Ces pannes n'agissent pas sur la physique du simulateur (l'avion vole normalement) mais impactent durement les règles vitales du de *Flight Supervisor* et la note de *Comfort* des passagers :
*   **WiFi / IFE (In-Flight Entertainment) H.S.** : 
    *   *Conséquence* : L'ennui des passagers monte en flèche au fil des heures. L'anxiété s'installe.
    *   *Action Joueur* : Obligation pour le PNC de faire des rotations de service supplémentaires (Snacks/Boissons) avec un coût en ressources (`CateringRations`) pour pallier l'ennui.
*   **Machine à Café (Coffee Maker) H.S.** : 
    *   *Conséquence* : Impossible de servir des boissons chaudes. Lors du petit-déjeuner (petit-matin), le score de Catering s'effondre.
    *   *Action Joueur* : Faire des annonces d'excuse au micro pour adoucir le coup via les interphones.
*   **Toilettes Avant (FWD Lavatory) INOP** : 
    *   *Conséquence* : Double la vitesse de remplissage de la cuve globale (Waste) et provoque un embouteillage virtuel en cabine (bruit généré, perte de confort).
    *   *Action Joueur* : Commander un entretien spécifique (`Toilet Deep Clean`, rallonge le Cleaning de +15 min) aux Ground Ops pour la réparer.

### 3.2. Pannes Mécaniques Natives (Impact Avionique)
Ces pannes sont générées par le système (selon usure virtuelle) et **tolèrent toujours un départ (MEL - Minimum Equipment List)**. Il n'y aura pas de pannes "NO-GO" stricto sensu dans le générateur (laissant le joueur libre de voler), mais elles imposent de nouvelles procédures :
*   **APU INOP** : 
    *   *Conséquence* : L'avion doit démarrer ses moteurs sur l'équipement du sol. Impossible d'avoir de l'air conditionné à l'embarquement sans le tuyau de la climatisation externe.
    *   *Action Joueur* : Demander un chariot de démarrage pneumatique (ASU) et le Ground Power (GPU) lors des Ground Ops. Procédure CrossBleed Start obligatoire.
*   **Inverseur de Poussée Bloqué / H.S. (Thrust Reverser INOP)** : 
    *   *Conséquence* : Distance d'arrêt fortement allongée. L'alerte est donnée par le *Dispatch* lors du briefing.
    *   *Action Joueur* : Sélectionner l'Autobrake MAX à l'atterrissage et adapter ses V-Speeds.
*   **Capteur de Température des Freins (Brakes Temp Sensor fault)** : 
    *   *Conséquence* : Impossible de lire la température réelle des freins sur l'ECAM/EICAS.
    *   *Action Joueur* : Le joueur devra scrupuleusement laisser les freins refroidir "à la montre" au sol et commander obligatoirement un refroidissement par ventilateurs externes (si non dispos en option avion).

### 3.3. Pannes et Conséquences Causées par le Joueur
Les actions directes du pilote laissent des traces indélébiles dans l'Airframe et demandent "réparation" lors de la Turnaround, ce qui ajoute une couche de gestion du temps "pénalisante" (Temps réel ajouté aux Ground Ops).
*   **Hard Landing (Atterrissage Dur > 600 FPM ou Touchdown > 1.8G)** :
    *   *Conséquence* : Déclenche une inspection de sécurité obligatoire du train d'atterrissage par les ingénieurs locaux.
    *   *Action Joueur* : Rallonge automatique des opérations au sol avec l'apparition d'un Ground Service imprévu `"Maintenance Inspection"` (ex: +45 minutes). Ce sous-timer bloquera formellement tout Pushback.
*   **Ignorer la Règle du temps de Refroidissement Moteur (3 Minutes Engine Cooldown)** :
    *   *Règle* : Les moteurs doivent impérativement tourner au ralenti (Idle) pendant un minimum strict de **3 minutes continus** après l'atterrissage, le temps de se stabiliser thermiquement, avant qu'ils ne soient éteints au parking.
    *   *Conséquence* : Si le joueur coupe les moteurs prématurément, le métal chauffé s'abîme gravement (Thermal Shock). Le "Tech Log" enregistrera discrètement une dégradation de l'usure.
    *   *Action Joueur* : Sur les vols suivants de cet Airframe, notification de "Engine Oil Temp Fluctuation" par la maintenance, déclenchant des entretiens forcés et baissant la fiabilité générale perçue de l'appareil.
*   **Surchauffe des Freins au Parking (Freins > 300°C)** :
    *   *Conséquence* : Consigne un "Brake Wear" sévère dans l'Airframe.
    *   *Action Joueur* : Demande d'activer le service `"Brake Cooling"` (ventilateurs portatifs) depuis le menu externe, ce qui va geler (inhiber) le repoussage pour +15 minutes supplémentaires.

## 4. Scénarios Émergents (Random Encounters)

Au moment du chargement du rapport SimBrief, des "Dialogues Dispatch" spécifiques apparaîtront :
* "*Capitaine, le tail F-HBNJ sort juste d'une grosse visite A (A-Check). Il est comme neuf, mais attention au rodage.*"
* "*On vous a remis le G-EZWX. L'équipage d'hier a fait un touché très dur à Malaga. Les mécanos ont inspecté le train cette nuit, RAS, mais gardez-le à l'oeil.*" (Et cette annotation vient bel et bien du propre vol du joueur une semaine plus tôt !).

> [!TIP]
> **Stockage Technique** : Les Airframes seront stockés dans un petit dossier `Airframes/` de votre AppData, reprenant l'`InternalId` SimBrief. Ainsi, même un an plus tard, l'avion `361222_125` conservera son histoire.

## 5. Intégration UI : Le "Carnet de Vol" (Logbook Horizontal)

Pour éviter de surcharger et désorganiser le menu latéral gauche principal de l'application, la section de gestion des Airframes sera intégrée de façon stylisée.
*   **Format d'affichage** : L'interface prendra la forme d'un Carnet de bord (Horizontal Notebook). Elle sera conçue idéalement comme une fenêtre ou fenêtre modale déplaçable (draggable) et redimensionnable (resizable) s'ouvrant depuis une icône dédiée dans le panneau gauche.
*   **Navigation "Jour par Jour"** : L'utilisateur pourra "tourner les pages" horizontalement pour remonter dans l'historique temporel de chaque avion.
*   **Données affichées par Page (Aircraft Identity)** :
    *   Immatriculation (Registration), Type d'appareil, et Compagnie opérante.
    *   *Mise en service & Construction* : L'interface prévoira des sous-titres renseignant l'année de construction de l'appareil et son entrée en service. *(Note Technique : ces données peuvent potentiellement être récupérées par l'interrogation d'API web tierces à partir du Registration Code, de façon asynchrone)*.
    *   Liste chronologique et détaillée des incidents/Tech Log de la vie récente de l'appareil.

## 6. Rapport de Rotation Globale (Flight Report)

Puisque le gameplay favorise le chaînage des vols, le système de résumé final (scoring) doit s'adapter.
*   **Le Flight Report de Rotation** : Au lieu de se limiter à des rapports individuels par Leg, le joueur aura accès à un **Rapport de Rotation Global** englobant la totalité des étapes (Legs) accomplies. Le joueur aura l'opportunité de nommer sa rotation (ex : *Rotation Estivale WizzAir 4-Legs*).
*   **Section Flotte utilisée** : Ce rapport inclura un tableau récapitulatif listant tous les appareils qui ont été sollicités par l'équipage pour accomplir la rotation, avec la part de temps de vol et les incidents relevés sur chaque appareil singulier.
*   **Persistance Profil du Joueur** : L'ensemble de ces informations (Quels avions ont été pilotés ? Pendant combien d'heures réelles ?) remontera globalement dans les statistiques pérennes du profil du joueur, afin d'offrir une rétrospective agréable et gratifiante.
