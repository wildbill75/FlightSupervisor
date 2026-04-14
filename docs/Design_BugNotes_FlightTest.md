# Notes de Test en Vol - Session 2

## Note 10 - Annonces PA "Cruise", "Approach", "Descent"
- **Observation** : Le bouton "Cruise" persistait après utilisation.
- **Règle de gestion** : Les boutons PA de croisière, d'approche et de descente doivent être "One-Shot". Dès qu'ils sont cliqués, ils doivent disparaître ou se griser de manière permanente.
- **Statut** : Appliqué (lors de la session précédente).

## Note 11 - Séquençage des Commandes PNC (Takeoff Prep)
- **Observation** : Les boutons "PREP TAKEOFF" et "SEATS FOR TAKEOFF" ne doivent pas être affichés en même temps. 
- **Règle de gestion** :
  1. On affiche d'abord "PREP TAKEOFF".
  2. Une fois que la préparation est terminée, on affiche "SEATS FOR TAKEOFF".
  3. Chaque bouton doit disparaître immédiatement après avoir été activé (One-Shot).
- **Rationnel** : On ne peut pas demander aux PNC de s'asseoir avant qu'ils n'aient fini de sécuriser la cabine.
## Note 12 - Durée de préparation Cabine (Takeoff)
- **Observation** : Le temps de préparation de la cabine pour le décollage est jugé trop long (~5 minutes dans l'exemple cité).
- **Règle de gestion** :
  - Réduire la durée de base en la divisant par **1,5**.
  - Ajuster ensuite en fonction de la compétence des PNC (Crew Morale / Efficiency).
## Note 13 - Horodatage des logs (Zulu vs Système)
- **Observation** : Les messages dans la console (log) utilisent l'heure système Windows au lieu de l'heure du simulateur (Zulu).
- **Règle de gestion** : Synchroniser l'affichage des heures dans l'interface sur l'heure Zulu de MSFS pour plus de réalisme.
## Note 14 - Nouveau Script PA "Welcome"
- **Observation** : Le script de bienvenue actuel est un peu basique. Un nouveau script plus professionnel et dynamique est proposé.
- **Nouveau Script** :
  > "Ladies and gentlemen, good [Morning/Afternoon/Evening] from the flightdeck this is your captain speaking, my name is [First Name] [Last Name] and in the name of [company] I would like to welcome you all on board this [Airline] [Aircraft] on our flight to [Destination]. Today flight time will be approximately [EET] and we're expecting a [enroute weather conditions] We're just finishing the last paper work and once completed we will start our pushback. We will get back to you with the latest weather informations from our destination airport when we start the approach. Thank you very much for being ou guests. Seat back, relax, and enjoy this flight with us."
- **Variables dynamiques à mapper** :
  - `[Morning/Afternoon/Evening]` : Selon l'heure locale.
  - `[First Name] [Last Name]` : Extraire du profil joueur (Bertrand Le Quebec).
  - `[company]` / `[Airline]` : Extraire de la compagnie active.
  - `[Aircraft]` : Type d'avion actuel.
  - `[Destination]` : Nom de la destination.
  - `[EET]` : Temps de vol estimé (Estimated Enroute Time).
  - `[enroute weather conditions]` : Conditions météo (nice, bad, cloudy, etc.).
## Note 15 - Fenêtre temporelle PA "Welcome"
- **Observation** : L'annonce de bienvenue ne doit être jouable qu'à la porte (AtGate).
- **Règle de gestion** :
  - L'option "Welcome" doit disparaître dès que le "Pushback" commence.
  - Si le pilote ne l'a pas diffusée avant le départ, l'opportunité est perdue (ainsi que les points associés).
## Note 16 - Réalisme des tampons de temps & Turnaround (LCC vs Legacy)
- **Observation** : Le tampon entre l'heure d'atterrissage (ON) et l'heure d'arrivée bloc (IN) fourni par SimBrief est parfois très large (ex: 24 minutes pour un vol Paris-Toulouse).
- **Règle de gestion (Profils de Compagnies)** :
  - **LCC (Low-Cost Carriers)** : 
    - Horaires bloc-à-bloc plus serrés (réduction du tampon de **5 à 10 minutes**).
    - Temps de turnaround fixé à **25 minutes**.
  - **Legacy Carriers** : Conservation des tampons SimBrief standard (plus de "mou").
  - **Aléatoire (RNG)** : Ajouter une légère part d'aléatoire sur ces réductions pour plus de variété.
## Note 17 - Précision de la Consommation Carburant (Fuel Burn)
- **Observation** : Écart mesuré entre le Fenix (2980 kg) et Flight Supervisor (2790 kg), soit environ **200 kg** de différence.
- **Règle de gestion** : Réajuster l'algorithme de calcul ou la lecture des senseurs pour s'aligner plus précisément sur les données du Fenix A320.
## Note 18 - Vitesse de débarquement (Deboarding)
- **Observation** : Le débarquement est trop lent (14 min pour 110 pax).
- **Règle de gestion** :
  - Ajuster le ratio par passager pour atteindre ~8 minutes pour 110 pax.
  - Vitesse cible pour un avion plein (180 pax) : **10 à 11 minutes** maximum.
## Note 19 - Dysfonctionnement du "Time Skip" (Turnaround)
- **Observation** : Les boutons de saut de temps (+1m, +5m, +10m) affichés dans l'UI lors de la phase de Turnaround ne fonctionnent pas.
- **Règle de gestion** : S'assurer que ces boutons impactent correctement les timers des Ground Ops en cours pour permettre d'accélérer l'escale si souhaité.
## Note 20 - Séquençage Fuel Sheet (Leg 2) & Bouton Refresh
- **Observation** : Il est possible de signer et valider la Fuel Sheet de la Leg 2 alors que le débarquement et le déchargement de la Leg 1 sont encore en cours. Cela provoque une transition prématurée qui bloque le débarquement de la Leg 1.
- **Règle de gestion** :
  - **Verrouillage** : Interdire l'accès (ou la validation) de la Fuel Sheet de la jambe suivante tant que la première phase du turnaround (débarquement/déchargement) n'est pas terminée (100%).
  - **Bouton Refresh** : Ajouter un bouton "Refresh" sur la fenêtre de la Fuel Sheet pour permettre de re-télécharger le dernier plan SimBrief (utile en cas de correction du plan de vol sur le site de SimBrief).
- **Statut** : À traiter.
