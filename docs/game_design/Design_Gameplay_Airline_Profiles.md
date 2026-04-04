# Design Gameplay - Profils des Compagnies & Personnages PNC

La compagnie aérienne (définie par l'ICAO via SimBrief) ne se contente pas de dicter les attentes des passagers. Elle conditionne avant tout l'environnement de travail de l'équipage (PNC - Personnel Navigant Commercial).

Les conditions de travail (salaires, cadences de vol, politique managériale) générées par la typologie de la compagnie (Tier) vont engendrer un profil de PNC bien spécifique. Chaque équipage généré aura des statistiques propres qui auront un **impact tangible et mécanique sur le vol**. 

Il n'y aura aucune statistique "vide" : chaque valeur se ressentira dans la boucle de gameplay (le `Tick()`).

---

## 1. Les 3 Compétences Fondamentales du PNC

Pour éviter les doublons (comme entre Proactivité et Efficacité) et clarifier leur impact, voici les trois piliers qui définissent un équipage :

### A. Efficiency (Efficacité / Vitesse d'exécution)
*Agilité physique et rapidité de l'équipage à exécuter les tâches standardisées.*
*   **Impact Gameplay Principal :** Les chronomètres et l'avancement des phases.
*   **Traductions en jeu :**
    *   **Vitesse d'embarquement / débarquement :** Un équipage très efficace fluidifie les couloirs (ex: baisse le temps total d'embarquement de 15%).
    *   **Phase de préparation (Cabin Secure) :** Répond au signal "Prepare for Takeoff / Landing" très rapidement (ex: rapport "Cabin Ready" dans les 90 secondes au lieu de 3 minutes).
    *   **Service (Catering) :** Le service de repas s'achève plus tôt, laissant du temps libre pour le repos.
*   *Lien Compagnie :* Chez les Low-Costs (EasyJet, Ryanair), l'Efficacité est **maximale** (les PNC sont drillés au turnaround de 25 minutes et sont redoutables d'efficacité physique).

### B. Empathy (Empathie / Sens du Service Client)
*Ce qui remplace le "Moral" statique : c'est la gentillesse humaine, le sourire, le contact humain.*
*   **Impact Gameplay Principal :** Bouclier naturel contre l'Anxiété et l'Inconfort des passagers.
*   **Traductions en jeu :**
    *   **Désamorçage des conflits :** Face à un retard (Delay > 15m), l'équipage empathique circule en cabine avec de l'eau. Les passagers râlent, mais le PNC fait tampon = **Annule 50% de la perte de Satisfaction liée au retard.**
    *   **Turbulences :** Pendant de grosses secousses, si le commandant allume les Seatbelts, un équipage empathique rassure = **Le gain d'Anxiété des passagers est divisé par deux.**
    *   **Rapport radio :** Un Chief Purser empathique commence toujours ses appels au Cockpit de façon cordiale et rassurante, même dans la tourmente.
*   *Lien Compagnie :* Chez les Elite (SIA, Emirates), l'Empathie est **très élevée**. Chez les Low-Costs, l'Empathie est **très faible** (épuisement professionnel, pas le temps de discuter).

### C. Proactivity (Initiative & Autonomie)
*La capacité de prendre des décisions sans attendre l'ordre du Cockpit.*
*   **Impact Gameplay Principal :** La fréquence des notifications / L'aide passive au joueur.
*   **Traductions en jeu :**
    *   **Micro-Crises Autonomes :** Un conflit pour un bagage survient dans l'allée. Avec une **haute** Proactivité, le PNC le règle seul dans l'ombre et le joueur reçoit juste un *"[SYS] Equipage proactif : incident bagage réglé, embarquement optimisé (+5 pts)"*. Avec une **basse** Proactivité, le PNC appelle le cockpit : *"Capitaine, deux mecs se battent pour un coffre et l'embarquement est bloqué, que fait-on ?"* -> Le joueur doit gérer le délai.
    *   **Anticipation Météo :** Un PNC proactif repère que la température cabine atteint 25°C et vous prévient immédiatement *avant* que l'Anxiété des passagers ne monte.
*   *Lien Compagnie :* Les compagnies Legacy (Standard / Elite) ont des PNC très proactifs et bien formés pour laisser le commandant se concentrer sur le pilotage. Les compagnies "Struggling" n'osent prendre aucune initiative.

---

## 2. Génération Basée sur le "Tier" (Matrice d'attribution)

Au lancement d'un vol, les statistiques de l'équipage sont tirées "aux dés", mais lourdement pondérées par la compagnie renseignée dans le plan de vol :

| Tier de Compagnie | Efficiency (Temps) | Empathy (Résistance) | Proactivity (Autonomie) | Exemple de Résultat en Vol |
| :--- | :--- | :--- | :--- | :--- |
| **ELITE** (10/10) <br>*SIA, Emirates, Qatar* | **Moyenne (60-80%)** <br>*On prend le temps de bien faire.* | **Excellente (90-100%)** <br>*Petits soins absolus.* | **Excellente (80-100%)** <br>*Aucun appel inutile au Flight Deck.* | Un vol où le joueur est un roi. Les PNC s'occupent de presque tout de manière invisible et maintiennent le confort à 100% presque magiquement, mais l'embarquement prend son temps normal. |
| **STANDARD** (Legacys) <br>*Air France, Delta, LH*| **Bonne (70-85%)** <br>*Équilibre.* | **Bonne (60-85%)** <br>*Professionnel.* | **Modérée (50-70%)** <br>*Suit rigoureusement les SOP.* | L'expérience équilibrée parfaite. Ils préviennent le Capitaine s'il y a un souci, répondent vite aux appels, maintiennent la cabine calme. |
| **LOW COST** <br>*Ryanair, EasyJet, Wizz*| **Surhumaine (90-100%)** <br>*Turnaround en 25 mins chrono.* | **Très Basse (20-40%)** <br>*Le PNC est épuisé, expéditif.* | **Basse (30-50%)** <br>*Pas payés pour inventer des solutions.* | Le sol (Ground Ops) est expédié en une vitesse record. Maiiis à la moindre crise en vol, les passagers paniquent et le PNC ne fait rien pour aider, appelant le Cockpit dès qu'il y a un imprévu. |
| **STRUGGLING/DANGER** <br>*PIA, Tunisair* | **Basse (20-40%)** <br>*Mauvaise orga.* | **Critique (10-30%)** <br>*Hostile.* | **Nulle (10-20%)** <br>*Passive.* | Retards importants à l'embarquement, PNC très lents à securing the cabin. Le joueur doit porter l'équipage sur ses épaules pour limiter les dégâts. |

---

## 3. Le "Crew Morale" devient une barre de vie

Plutôt que d'être une statistique fixe générée au départ, le **"Moral de l'Équipage"** (Crew Morale) agirait comme un amplificateur ou un frein dynamique au cours de votre journée de vol (surtout lors de vols **Multi-Legs**).

*   **Siège confortable :** L'Empathie du PNC est de 80.
*   **Mais si c'est la 3e leg de la journée** et que l'avion est resté au sol sous 35°C sans APU (donc sans clim), le **Moral tombe à 10%**.
*   **Résultat :** Quand le Moral est très bas, la stat "Empathie" de 80 d'origine est **divisée par deux**. Le PNC élite commence à devenir agressif avec les passagers, et de fausses "alertes" (erreurs humaines, appels cockpit pour râler) apparaissent.

### Comment le Joueur (Capitaine) influe sur le Moral :
*   **Bonus au Moral :** Poser l'avion en douceur (Soft Landing) = "Great landing Captain, the crew appreciates it!". ; Mettre l'avion à température (21°C) parfaite pendant le turnaround ; Gérer les retards de manière proactive (annonces).
*   **Malus au Moral :** Déclenchement brutal des Seatbelts pendant un service chaud ; Roulage frénétique ; Atterrissage très dur ; Refus de dérouter quand il le faudrait.

---

## 4. Paramètres Techniques & Politiques Compagnie (SOPs)

Outre le profil du PNC, la feuille de personnage d'une compagnie inclura des données techniques (via un fichier JSON ou base de données) qui dicteront les attentes du **Flight Supervisor** :

*   **Turnaround Target Time** : (Ex: 25-30 mins pour LCC, 60 mins pour Legacy).
*   **Fuel Policy** : Tolérance sur l'Embarquement de Carburant Supplémentaire (Extra Fuel).
    *   *Sévère* : Ajouter de l'Extra Fuel donne un malus de "Company Satisfaction" (Low Cost).
    *   *Libérale* : Tolérance jusqu'à +1500kg sans pénalité morale (Legacys).
*   **Cost Index Typique** : L'indication du `CI` pour préparer le vol (ex: CI 15 pour LCC, CI 35 pour Legacy court-courrier).
*   **APU Policy** : Exigence forte (ou non) de couper l'APU au profit du Ground Power (pour réduire les coûts de kérosène).
*   **Baggage & Boarding Policy** : 
    *   Le risque de l'événement "Overhead Bins Full" augmente à 80% chez les compagnies avec option bagage payante (ex: Ryanair/EasyJet).
    *   Vitesse d'embarquement boostée si politique de "Dual Boarding" (escaliers Avant et Arrière) autorisée et disponible.

*(Voir `Design_Gameplay_Airline_Easyjet.md` pour un exemple complet de feuille de personnage d'une compagnie.)*
