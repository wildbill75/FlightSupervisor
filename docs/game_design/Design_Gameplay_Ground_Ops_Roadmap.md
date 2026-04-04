# Roadmap: Dynamisation des Opérations au Sol (Scaling Metrics)

Cette roadmap résume l'analyse approfondie de l'implémentation actuelle de `GroundOpsManager.cs` par rapport aux spécifications de design, et dresse la liste des fonctionnalités à implémenter pour rendre les durées de chaque service 100% dynamiques et liées à SimBrief/Télémétrie.

## Analyse de l'existant vs Nouveaux Besoins

### 1. Variables Globales & Outils Déjà Implémentés `[FAIT]` / `[À FAIRE]`
- **Tier rating aéroportuaire (Ticket 35) `[FAIT]`** : La logique gérant le niveau (S à F) d'un aéroport est active. Un multiplicateur `tierTimeMultiplier` est d'ores et déjà appliqué sur la durée totale (`applyTime`) de TOUS les services pour simuler l'efficacité ou la lenteur de certaines escales.
- **Variation Aléatoire `[FAIT]`** : Chaque opération a déjà une variation aléatoire de +/- 15% pour casser la monotonie d'un chrono purement statique.
- **Phasage d'Approche Logistique (Logs & Délais) `[À FAIRE]`** : Séparer "le temps mis pour arriver" du "temps passé à l'avion". Le déclenchement d'un service (ex: Refuel) passera par plusieurs sous-étapes visibles sur la pilule de l'UI : *Service ordered* -> *Truck on its way* -> *Arrived / Connecting* -> *Opérationnelle*. Ce délai logistique dépendra de la taille de l'aéroport.

### 2. Le Refueling (Plein de carburant) `[PARTIELLEMENT FAIT]`
- **Actuel** : Le temps est correctement scalé ! La formule déduit le `FuelNeededKg` en soustrayant le `CurrentFob` (fuel à bord) au `PlanRamp` (exigé par Simbrief). La vitesse est cadencée à 50kg/seconde, avec un minimum forfaitaire de 10 minutes (ou durée fixe si mode low-cost accéléré).
- **À Ajouter** : 
  - **Contrainte Majeure** : Le service de Refueling **ne peut pas démarrer** tant que la `Fuel Load Sheet` n'a pas été remplie et formellement validée par le pilote (ce design est en cours de création via `Design_Gameplay_Fuel_Planning.md`). Le camion arrivera, mais l'opération sera en pause (`Waiting for Fuel Sheet`).
  - Moduler le débit (50kg/sec) en fonction du type d'infrastructure (pompes sous-terraines haute pression vs classiques camions citernes) basé sur le Tier de l'Aéroport.

### 3. Boarding & Deboarding (Passagers) `[À FAIRE]`
- **Actuel** : La durée d'embarquement utilise des bases fixes (15 minutes en LCC, 20 minutes en Legacy), seulement modulées par le `boardingEfficiencyRatio` (l'efficacité de l'équipage PNC). Le débarquement est une durée fixe bloquée (10 ou 15 mins).
- **À Ajouter (Basé sur les données réelles Airbus A320)** :
  - **Lier impérativement la durée de base au nombre exact de passagers (Pax Count)** du manifeste SimBrief. 
  - *Data Réelle* : L'embarquement d'un A320 se fait au rythme d'environ **12 passagers par minute** (soit environ 5 secondes par passager) par porte. Le débarquement est plus rapide, environ **20 passagers par minute** (3 secondes par passager). Un A320 plein prend donc au minimum ~13.5 min à embarquer et ~8 min à débarquer dans un flux parfait.
  - Garder le modificateur d'efficacité de l'équipage actuel en surcouche.
  - Ajouter potentiellement un poids de Réputation Compagnie (qui influence la discipline globale des passagers à s'asseoir vite).

### 4. Cargo & Valises `[À FAIRE]`
- **Actuel** : Le temps s'appuie uniquement sur le nombre de passagers (`Pax * 6 secondes`), avec 10 minutes minimum.
- **À Ajouter** :
  - Il manque l'inclusion du Poids du Fret (`Cargo Weight`) issu de SimBrief.
  - La formule de calcul doit faire la somme `(Pax Count * factor) + (CargoWeight * factor) / BaggageHandlersSpeed`.

### 5. Cleaning / Ménage de la Cabine `[À FAIRE]`
- **Actuel** : Basé sur une constante arbitraire (5 minutes si fait par les PNC en `LowCost`, ou 15 minutes en `Legacy`). Ce n'est pas lié à l'état de l'avion.
- **À Ajouter** : 
  - La durée doit être proportionnelle au niveau de saleté : on utilise la variable d'arrivée du vol précédent `CabinCleanliness`.
  - Exemple de calcul : Passer de 30% propreté à 100% prendra considérablement plus de temps que passer de 85% à 100%.

### 6. Catering (Nourriture) `[À FAIRE]`
- **Actuel** : Forfait fixe arbitraire (5 minutes en `LowCost`, 15 minutes en complet).
- **À Ajouter (Basé sur les données réelles Airbus A320)** :
  - *Data Réelle* : L'installation / retrait du matériel logistique (positionnement du camion et ouverture) prend de façon incompressible environ **10 minutes**. Ensuite, l'échange d'un chariot complet (trolley full-size) prend environ **1 minute** par chariot. 
  - *Impact du remplissage (A320)* : Même si l'avion n'est pas plein, la logistique de base prend du temps. La différence entre un vol à moitié plein et plein ne varie donc que de quelques minutes (le temps de charger quelques trolleys supplémentaires). De nombreuses low-cost coupent parfois même le catering court-courrier pour gagner ces précieuses 10 minutes incompressibles.
  - Formule proposée : `10 minutes (base) + (1 minute * Nombre de Trolleys)`. Calculer un "Delta de Rations" : combien de chariots ou de plateaux moyens doivent être rechargés par rapport à ceux consommés.

### 7. Water / Waste (Eaux & Toilettes) `[À FAIRE]`
- **Actuel** : Temps bloqué à 450 secondes fixes (~7,5 minutes).
- **À Ajouter** :
  - Le temps doit dépendre du delta pour remplir la `PotableWaterTank` et pour vider le `WasteTank`. Plus les réservoirs ont été sollicités, plus le temps de pompage sera long.

## Mise à jour des documents (Design) 
Le document `Design_Gameplay_Ground_Operations.md` a été mis à jour dans le `TICKET 34` avec toutes ces nouvelles règles de Game Design pour acter officiellement la direction à prendre.

## User Review Required
> [!IMPORTANT]
> Es-tu d'accord avec ce plan et ce constat exhaustif de la situation ? Souhaites-tu ajuster quelques unes des métriques du "À FAIRE" avant que l'équipe de dev ne commence à l'implémenter ?
