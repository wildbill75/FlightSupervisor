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

## Note 21 - Annonce PA "Welcome" (Coded version missing Name)
- **Observation** : Dans la version implémentée dans `CabinManager.cs`, l'annonce du capitaine ne mentionne pas son prénom et son nom.
- **Texte exact actuellement codé** :
  > "Ladies and gentlemen, good {greeting} from the flightdeck this is your captain speaking, and in the name of {airlineName} I would like to welcome you all on board this {aircraftType} on our flight to {destName}. Today flight time will be approximately {flightTime} and we're expecting a {wxcConditions}. We're just finishing the last paper work and once completed we will start our pushback. We will get back to you with the latest weather information from our destination airport when we start the approach. Thank you very much for being our guests. Sit back, relax, and enjoy this flight with us."
- **Règle de gestion** :
  - Il faut rajouter "my name is [First Name] [Last Name]" après "this is your captain speaking", en récupérant les infos depuis le profil du joueur.
- **Statut** : À traiter.

## Note 22 - Disparition des boutons d'annonces PA
- **Observation** : Les boutons d'annonces PA ne disparaissent plus après avoir été cliqués (ils restent affichés), alors que cela fonctionnait correctement auparavant. Il s'agit d'une régression.
- **Règle de gestion** : Les boutons d'annonce (Welcome, Cruise, Descent, etc.) doivent tous être de type "One-Shot" et être désactivés (ou masqués) de manière permanente une fois joués.
- **Statut** : À traiter.

## Note 23 - Ajustement (Suite Note 12) : Durée de préparation Cabine (Takeoff)
- **Observation** : Malgré l'ajustement précédent, le temps de préparation de la cabine par les PNC reste un peu trop long (mesuré à 6 minutes durant ce test).
- **Règle de gestion** :
  - Abaisser le "scaling" de base de la durée de la préparation de 2 minutes, pour viser un temps nominal autour de **4 minutes**.
  - L'ajustement final de cette durée doit toujours rester soumis/pondéré par les compétences de l'équipage PNC (Morale / Efficiency).
- **Statut** : À traiter.

## Note 24 - Logique d'apparition/disparition du bouton "Annonce Turbulence"
- **Observation** : Le bouton permettant de faire l'annonce aux passagers concernant les turbulences s'affiche correctement lorsque le système détecte des turbulences, mais son masquage n'est pas optimal.
- **Règle de gestion** : 
  - Si le bouton apparaît (détection de turbulences), il doit rester affiché tant que les turbulences sont encore actives.
  - Dès que l'algorithme détecte que les turbulences se sont calmées, le bouton doit disparaître automatiquement de l'UI.
  - S'il y a une nouvelle zone de turbulences plus tard dans le vol, le bouton doit de nouveau réapparaître. Cela permet de rassurer les passagers à plusieurs reprises sur un même vol.
- **Statut** : À traiter.

## Note 25 - Faux Positif : Tail Strike au décollage
- **Observation** : Le système a détecté un "Tail Strike" (Pitch 11.8°) au décollage alors que la rotation s'est effectuée de manière normale.
- **Analyse Technique** :
  - Dans `WearAndTearManager.cs`, la détection vérifie simplemment `pitch > 11.5` et `_simOnGround == true`.
  - Problème 1 : Pendant la rotation, l'assise de l'avion s'incline (amortisseurs qui se détendent) mais MSFS considère parfois que l'avion est encore "Au Sol" pendant une fraction de seconde alors qu'il a déjà cabré à plus de 11.5°.
  - Problème 2 : Un pitch de 11.5° au sol est potentiellement trop restrictif pour déclencher un tail strike (sur le Fenix A320, les jambes de train permettent d'aller légèrement au-delà si étendues).
  - Problème 3 : L'événement s'enregistre sans délai/cooldown, menant potentiellement à des avertissements répétés.
- **Règle de gestion** :
  - Relever légèrement le seuil de tolérance (ex: `12.5°` ou `13.0°`).
  - Croiser avec une autre variable (ex: Vitesse verticale positive pour identifier qu'on est en plein décollage) ou ajouter un petit délai avant de valider le tail strike (debounce).
  - S'assurer qu'un seuil minimum de dureté de choc est respecté, ou empêcher les logs multiples.
- **Statut** : À traiter.

## Note 26 - Régression : Le catering ne se décrémente plus après le service
- **Observation** : Le nombre de rations de catering ne diminue pas après que le service a été effectué en vol.
- **Analyse Préliminaire** : Il s'agit d'une régression d'un comportement qui avait été corrigé. Il est fort probable que les récents changements sur la logique de consommation des ressources (consommation passive continue pour l'eau/les déchets introduite dans `CabinManager.cs`) aient accidentellement contourné ou désactivé le déclencheur de décrémentation "One-Shot" du catering.
- **Règle de gestion** : Lors du service en vol, la quantité de repas disponibles doit être déduite en fonction de l'avancement du service et des passagers servis.
- **Statut** : À traiter en priorité (Régression).

## Note 27 - Inexactitude du calcul du FPM à l'atterrissage
- **Observation** : Le log technique rapporte un impact à **-819 fpm (0.98G)** ("Severe Hard Landing"), ce qui est très largement exagéré pour un atterrissage qui s'est avéré seulement « modérément ferme » (-217 fpm relevé sur Volanta).
- **Analyse Technique** :
  - La valeur lue via SimConnect au moment précis où `simOnGround` passe à `true` attrape le pic (spike) de compression brutale des amortisseurs via la physique de MSFS. De plus, `_vsHistory.Min()` attrapait systématiquement ce pic négatif extreme.
- **Plan d'Action / Règle de gestion** :
  - La valeur est désormais lissée en utilisant `_vsHistory.Peek()` qui récupère la valeur du taux de chute environ 0.5s *avant* le point géométrique de contact, contournant totalement le calcul de compression des amortisseurs.
- **Statut** : **CORRIGÉ** (Implémenté en live).

## Note 28 - Ajustement (Suite Note 18) : Vitesse de débarquement (Deboarding)
- **Observation** : Pendant cette session, le débarquement de 151 passagers a pris environ 14 minutes.
- **Analyse Préliminaire** : Selon la **Note 18** précédente, nous avions ciblé un temps maximum de 10 à 11 minutes pour un avion plein (180 pax). Un temps de 14 minutes pour 151 passagers indique que la vitesse de débarquement par passager est encore trop lente. La baisse du ratio demandée à la note 18 n'a visiblement pas été suffisante ou pas prise en compte.
- **Règle de gestion** :
  - Vérifier impérativement la formule et baisser encore le "tick rate" ou le ration par passager lors du débarquement pour garantir le respect de l'objectif (180 pax = 11 mins maximum).
- **Statut** : **CORRIGÉ** (Durée de débarquement dynamiquement basée sur la formule: 3.0s/pax pour les LCC, 3.8s/pax pour les maj).

## Note 29 - BLOCKER : Désynchronisation de la Loadsheet / Index de Vol (Lié Note 20)
- **Observation** : Lors du Turnaround, la Loadsheet (Fuel/Dispatch) affiche toujours les données du vol précédent (Paris -> Toulouse) alors que le nouveau plan (LFBO -> LFPO) est formellement chargé et affiché sur le Dashboard. En conséquence, après le débarquement du vol précédent, les opérations au sol refusent d'avancer puisqu'elles semblent interroger l'ancien manifeste ou plan de vol.
- **Analyse Préliminaire** : Ce bug bloquant (issu des Note 19/20 et du handover précédent sur la gestion multi-leg) n'a manifestement pas été résolu correctement. Le passage à la `Leg N+1` (`ActiveLegIndex`, ou la récupération de la rotation suivante en C# ou JS) ne se propage pas à l'ensemble du système GroundOps.
- **Règle de gestion / Action** :
  - Priorité absolue au prochain développement : Il faut verrouiller la logique d'index de vol entre le C# (Backend) et l'UI (Frontend) et garantir que lorsqu'on entame le Turnaround, on passe définitivement à l'ID de la prochaine rotation. Les Ground Ops (Cargo, Boarding, FuelSheet) ne doivent pas pouvoir s'accrocher aux données du vol précédent.
- **Statut** : **CORRIGÉ** (La validation et le multi-leg tirent maintenant leurs infos de la variable UI `_currentResponse` de source unique).


## Note 30 - Chute brutale du stock de repas (Catering) en plein vol
- **Observation** : Durant le vol, les 155 repas embarqués pour 151 passagers sont tombés soudainement à zéro en plein service en vol, provoquant un message du PNC au Capitaine ("Captain, we have totally run out of meals...") et mécontentant les passagers.
- **Analyse Préliminaire** : Probablement lié aux récentes tentatives de correction de la Note 26. La boucle de consommation au moment du `In-flight service` retire trop de repas (ou par erreur pour tous les passagers à chaque 'tick').
- **Statut** : **CORRIGÉ** (Un `double drain` ambiant inopportun a été retiré, garantissant que les repas ne sont consommés QUE pendant le véritable `InFlightService`).

## Note 31 - Annonces CPT -> PA (PA Comms) manquantes
- **Observation** : Plusieurs annonces provenant du poste de pilotage (Captain vers PA) ne sont pas apparues au cours du vol, notamment l'annonce de croisière (Cruise).
- **Analyse Préliminaire** : Il semble y avoir une régression ou un problème de déclenchement dans la logique des communications (CabinManager / FlightPhaseManager). Les triggers basés sur les phases de vol (passage en Cruise, etc.) ne semblent pas avoir déclenché leurs événements PA respectifs.
- **Règle de gestion / Action** : Lors de la prochaine phase de correction, il faudra un audit complet (`check total`) du système de communications (messages Flight Deck -> Cabin).
- **Statut** : **CORRIGÉ** (Les hooks d'événements PAs 'CruiseStatus' et 'Descent' manquants ont été rajoutés dans le delegate `OnPhaseChanged` du `MainWindow.xaml.cs`).

## Note 32 - Les boutons du Command Board (PA) ne disparaissent pas
- **Observation** : Les messages PA (ex: Cruise, Approach) se jouent lorsqu'on clique dessus et donnent bien les points, mais le bouton UI (action du command board) ne disparaît pas ou ne se grise pas. Le joueur peut donc cliquer dessus "12 fois".
- **Règle de gestion** : Tous les boutons PA doivent avoir un comportement "One-Shot" et être supprimés (ou verrouillés/grisés) en permanence dès qu'ils ont été cliqués une fois. Et le système d'empilement (stack) audio doit être maintenu s'il y a déjà un son en cours.
- **Statut** : À traiter.

## Note 33 - Pénalité si on réclame "Seats for Takeoff" trop tôt
- **Observation** : Si le joueur clique sur "SEATS TAKEOFF" (demande aux PNC de s'asseoir) alors que la préparation cabine n'est pas complètement terminée (ex: roulage trop court), la progression s'interrompt et la cabine n'est virtuellement pas tout à fait prête.
- **Règle de gestion** : Si on interrompt la préparation PNC en forçant "Seats for Takeoff" prématurément, l'action doit être pénalisée (malus) car le balayage de sécurité cabine n'a pas pu être achevé dans les règles de l'art.
- **Statut** : À traiter.

## Note 34 - Bouton "Hurry" (Accélérateur de préparation cabine)
- **Observation** : Pour éviter l'interruption (Note 33) lors de roulages très courts, il manque un moyen d'ordonner aux PNC d'accélérer.
- **Règle de gestion** : 
  - Dès qu'un joueur a cliqué sur "Prep Takeoff", le bouton au lieu de juste se griser, se transforme en bouton rouge/orange **"HURRY"** (Dépéchez-vous).
  - Un clic sur "Hurry" doit accélérer drastiquement la jauge de "Preparing Cabin" sans provoquer de pénalité/malus.
- **Statut** : À traiter en tant que nouvelle mécanique ("Hurry" mechanic).

## Note 35 - Jauge "Securing Cabin" continue après l'atterrissage
- **Observation** : Le joueur a effectué un "Force Seats" pour l'atterrissage car la sécurisation n'était pas terminée. Mais après l'atterrissage (phase 'Landed'), la jauge de progression "Preparing Cabin" a continué de tourner en boucle.
- **Règle de gestion** : Le passage à la phase 'Landed' (ou le déclenchement de "SEATS_LANDING") doit impérativement interrompre et réinitialiser la jauge de préparation pour éviter qu'elle continue de tourner. Parallèlement, le malus "Force Seats" doit bien s'appliquer si 'SEATS_LANDING' est pressé prématurément.
- **Statut** : À traiter.

## Note 36 - Faux positif "Ground ops started while engines are running"
- **Observation** : Arrivé en porte, le joueur a reçu un malus/warning : *"Ground ops started while engines are running, this is extremely dangerous"*. Pourtant, il assure avoir éteint les moteurs (N1 < 5%) ET désactivé le Beacon AVANT de déclencher le déchargement (Cargo Unloading / Deboarding).
- **Règle de gestion** : Le déclencheur logique (GroundOpsManager ou WearAndTearManager) doit évaluer incorrectement le statut des moteurs (N1 > 5% ?) ou l'ordre des événements lors du tick. Il faut auditer la condition de sécurité du Ground Ops pour s'assurer que si RPM/N1 est bas et Beacon = OFF, le malus ne s'active en aucun cas.
- **Statut** : À traiter.

## Note 37 - Turnaround bloqué et mauvais Load Sheet sur l'étape suivante (Leg 2)
- **Observation** : En Turnaround de l'étape 1 vers 2, le joueur ne peut pas "démarrer quoi que ce soit". L'interface indique bien un Turnaround et la Leg 2 est présente, mais s'il ouvre le Load Sheet de Fuel, il voit les chiffres de la Leg 1. De plus, à la fin du déchargement (unloading), le simulateur reste bloqué sans passer automatiquement au portail d'embarquement (AtGate).
- **Règle de gestion** :
  1. Dès le déclenchement de la phase `Turnaround`, le système doit inspecter la file d'attente SimBrief et préparer virtuellement les données (climat, fuel) pour que la "prochaine" étape s'affiche immédiatement dans la fenêtre FIST Load Sheet sans briser les stats terminées de l'étape 1.
  2. Aussitôt le Unloading complet (Pax débarqués et soutes vidées), le programme doit ré-amorcer `LoadNextLeg()` tout seul pour basculer en phase d'embarquement `AtGate`. L'étape 2 "repart alors bien à zéro".
- **Statut** : À traiter.
