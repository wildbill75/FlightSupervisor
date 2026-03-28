# [AMBRE] Design Gameplay: Passenger Manifest & Seat Map

## 1. Overview
The Passenger Manifest creates an immersive environment by generating a unique list of passengers and crew members for every flight, complete with names, nationalities, and ages. It also dynamically reflects the state of the cabin (Boarding, Seatbelts) in real-time.

## 2. Mechanics

### 👥 Crew Generation
- **Flight Crew**: Always includes a Commander and a First Officer.
- **Cabin Crew**: Scaled based on aircraft type and passenger count.
- **Naming**: Regional names are generated based on the departure airport or the airline's home.

### 💺 Passenger Simulation
- **Nationalities**: 45% Origin, 45% Destination, 10% International.
- **Boarding Progression**: Passengers don't appear instantly. During the "Boarding" phase, they board the aircraft in real-time, progressively filling the virtual seats.
- **Seat Map UI**: An interactive 2D grid allowing dragging (pan) and zooming.

### 🚥 Seatbelt Compliance
- Instead of a tiny dot, the **entire seat background** reflects the passenger's choice:
  - **Blue/Emerald**: Seatbelt Fastened.
  - **Red**: Seatbelt Unfastened.
  - **Dark Gray**: Passenger not yet boarded.

---

## 3. Liste des Tickets (Structure du Design)

- [x] **TICKET 21 : Pan / Zoom de la Carte SVG**
  - Rendre les variables `scale`, `currentX` et `currentY` globales (`window.manifestPanZoom`) pour empêcher la réinitialisation brutale lors du refresh télémétrique (100ms).

- [x] **TICKET 22 : Tooltip des Passagers**
  - Fixer le CSS pour éviter le clignotement (`pointer-events: none`).
  - Assurer un `z-index` massif (ex: 50) pour que l'étiquette (Nom, Âge, Siège) passe *par-dessus* tous les autres sièges et icones.

- [x] **TICKET 23 : Refonte UI des Ceintures**
  - Supprimer le point de couleur (dot).
  - Modifier le background du `div.seat` en fonction de `IsSeatbeltFastened`. Bleu (`bg-sky-500`) si attaché, Rouge (`bg-red-500`) si détaché.

- [x] **TICKET 24 : Simulation d'Embarquement (Boarding)**
  - Ajouter un flag `IsBoarded` côté C# (`PassengerState`).
  - Dans la boucle `Tick()` de `CabinManager`, populer de 1 à 3 passagers toutes les 2 secondes si la phase est `AtGate` et `HasBoardingStarted`. 
  - Si l'embarquement est fermé, passer tous les passagers restants à `IsBoarded = true`.
  - Côté SVG, laisser le siège en gris si le passager n'a pas encore embarqué.

- [ ] **TICKET 25 : Purser Voice Identity Linking (Audio stringing)**
  - Lors de la génération aléatoire de l'équipage, attribuer un `VoiceProfile` (ex: `female_1`, `male_1`) au Chef de Cabine (Purser).
  - Ce profil sera utilisé dynamiquement par l'Audio Engine C# pour pointer vers le sous-dossier vocal correspondant (ex: `airlines/air_france/pnc/en/female_1/`) lors du déclenchement des annonces passagers.

---

## 4. Spécifications de Design Technique

### [BACKEND] CabinManager.cs
- **Objet `PassengerState`** : Ajoute la propriété `bool IsBoarded`.
- **Logique d'Embarquement** : Utiliser un timer `_lastBoardingTick` pour injecter aléatoirement des passagers `IsBoarded = true` depuis la collection `PassengerManifest.Where(p => !p.IsBoarded)`.

### [FRONTEND] app.js
- **Variable de Session** : Maintenir l'état de la caméra dans `window.manifestPanZoom` à la racine pour survivre à la ré-écriture `innerHTML` de `renderManifest`.
- **Générateur HTML** :
  - Condition : `if (p && p.IsBoarded !== false)` pour afficher le passager sous forme de siège de couleur. Sinon, rendre un `<div class="seat"></div>` vide.
  - Classes dynamiques : attribuer `.seat.fastened` ou `.seat.unfastened` selon la télémétrie.
