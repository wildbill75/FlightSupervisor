# Technical Design: Extraction des L:Vars spécifiques (WASM / Fenix A320)

## 1. Synthèse du Concept
Ce document technique cadre exclusivement la solution pour résoudre la problématique d'extraction des variables locales (L:Vars) inaccessibles nativement depuis l'extérieur de Microsoft Flight Simulator (C#). 

Des avions "Study-Level" comme le Fenix A320 ou le PMDG contournent les variables standard (ex: `CABIN SEATBELTS ALERT SWITCH`) pour coder leur propre logique sous forme de **L:Vars** (ex: `L:S_OH_SEATBELTS`). Pour que la télémetrie de Flight Supervisor puisse "voir" l'état réel de ces interrupteurs et appliquer les pénalités correctement, l'architecture C# classique via SimConnect est insuffisante. Il faut obligatoirement injecter un pont d'exécution dans la mémoire même du simulateur via la technologie WebAssembly (WASM).

## 2. Architecture et Mécaniques Actives

La lecture externe des L:Vars nécessite une architecture scindée en deux : un agent interne (WASM) et un agent externe (C#).

### A. L'Agent Interne (Module WASM en C++)
Un module WASM est programmé en C++ et compilé avec le SDK MSFS. Il est ensuite poussé dans le dossier `Community` sous forme de package (ex: `flightsupervisor-wasm-bridge`).
- **Évaluation :** À chaque frame de simulation, le module utilise la fonction de bas niveau `execute_calculator_code` pour lire l'état en direct de `(L:S_OH_SEATBELTS, Bool)`.
- **Exposition :** Le module écrit cette valeur binaire dans une zone mémoire partagée et nommée, appelée **Client Data Area (CDA)** (exemple: `FSUPERVISOR_LVAR_AREA`).

### B. L'Agent Externe (Pont SimConnect en C#)
Côté Flight Supervisor (`SimConnectService.cs`) :
- Au lieu de s'abonner uniquement au flux de télémétrie classique (`RequestDataOnSimObject`), l'application cartographie la mémoire externe via la fonction `MapClientDataNameToID("FSUPERVISOR_LVAR_AREA")`.
- Elle s'abonne ensuite via `RequestClientData` pour recevoir des notifications "OnRecvClientData" uniquement lorsque la zone mémoire est altérée par l'agent interne WASM.

### C. L'Écraseur de Données (Data Override Logic)
Pour garantir une compatibilité universelle, l'application Flight Supervisor fonctionne en mode "Hybrid" :
- Par défaut, la structure `PlaneDataStruct` se remplit avec les SimVars officiels.
- Un Middleware C# lit en temps réel l'ICAO de l'avion cible (ex: `FNX320`). Si la condition est vraie, il écrase silencieusement la propriété native `Seatbelts` avec celle extraite de l'événement asynchrone `OnRecvClientData`. Les `Managers` évaluant les pénalités n'y voient que du feu.

---

## 3. Liste des Tickets (Structure du Design)

Ce scope ultra-focus requiert des tickets techniques pointus couvrant à la fois du C++ embarqué et du C# applicatif.

- [ ] **TICKET 1 : R&D et Choix d'Architecture WASM**
  - **Décision :** Créer un C++ WASM "maison" Flight Supervisor ou, alternativement, exiger l'installation du **WASM Module gratuit de MobiFlight** (un pont communautaire robuste) et implémenter son API `MobiFlight.LVars` en C#. La deuxième option économise 90% du temps de développement R&D.

*(Si l'option WASM Maison est choisie pour l'indépendance) :*
- [ ] **TICKET 2 : Création de la jauge WASM (C++)**
  - Configuration du projet Visual Studio pour la compilation WASM avec le SDK MSFS 2020.
  - Design du `SimConnect_Open` côté Gauge et création de la zone mémoire partagée (`Client Data Area`).

- [ ] **TICKET 3 : Évaluation Périodique C++**
  - Déclaration d'un array de L:Vars d'intérêt (Seatbelts, APU Master, APU Bleed propres au Fenix).
  - Programmation de la boucle d'interrogation et écriture du struct sérialisé vers la Client Data Area.

- [ ] **TICKET 4 : Écoute du Client Data Area (C#)**
  - Design des appels `MapClientDataNameToID` et de l'event handler `OnRecvClientData` dans le `SimConnectService.cs` principal.
  - Vérification de l'intégrité de la struct binaire reçue (marshalling de bytes vers une struct C# locale).

- [ ] **TICKET 5 : Le Data Source Overrider (C#)**
  - Ajout d'une condition d'identification "FNX320 / A32NX" à la reconnexion du vol.
  - Écrasement logique des propriétés standard avant la soumission du payload `phaseUpdate` vers l'application JS.
