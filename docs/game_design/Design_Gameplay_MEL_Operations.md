# Design: Minimum Equipment List (MEL) Operations & Gameplay

## 1. Concept & Vision
Ce module introduit la gestion des pannes tolérées via la **MEL (Minimum Equipment List)** au sein de *Flight Supervisor*. 
L'objectif est d'ajouter une asymétrie de gameplay majeure basée sur le modèle économique de la compagnie virtuelle choisie par le joueur : **Low-Cost Carrier (LCC)** ou **Legacy (Historique)**. Le joueur (Commandant) devra prendre des décisions cruciales de "Dispatch" (accepter l'avion avec une panne) qui auront un effet domino sur la fluidité de la rotation.

## 2. Le Socle Réglementaire (Les Règles Invariables)
Peu importe le modèle économique, TOUTES les compagnies sont soumises à des règles de sécurité identiques pour reporter une panne. L'avion ne peut partir que si l'élément figure dans la MEL approuvée par les autorités.

Les réparations sont classifiées en 4 catégories de délais stricts (excluant le jour de la découverte) :
*   **Catégorie A :** Temps spécifique dicté par la propre remarque de la MEL (Ex: à réparer sous *2 cycles de vol*, ou avant *la fin du jour civil*).
*   **Catégorie B :** À réparer dans les **3 jours calendaires**.
*   **Catégorie C :** À réparer dans les **10 jours calendaires**.
*   **Catégorie D :** À réparer dans les **120 jours calendaires**.

Dans la quasi-totalité des cas, le report (Deferral) s'accompagne d'instructions spécifiques :
*   **Opérationnelles (O) :** Restriction altimétrique, vitesse réduite, augmentation de consommation carburant de X %.
*   **Maintenance (M) :** Nécessité pour les pilotes ou mécanos d'isoler physiquement un circuit (breaker) ou d'appliquer une procédure au sol à chaque rotation (ex: Brancher l'ASU externe tous les jours).

## 3. Le Choc des Modèles : Low-Cost vs Legacy (Gameplay Dynamics)

C'est ici que l'asymétrie de *Flight Supervisor* prend son sens !

### 🟠 Profil Low-Cost (LCC) - "Le cauchemar opérationnel"
* **L'ADN :** Turnarounds express (25-35 minutes) et utilisation maximale (6+ vols par jour). Flotte unique.
* **Vulnérabilité:** Réparer en Outstation (aéroport éloigné de la base) coûte trop de temps et est souvent impossible faute de pièces ou de techniciens agréés sur place. Un AOG (Aircraft On Ground) en outstation détruit la marge bénéficiaire de la journée de rotation.
* **Le Choix Évident (Mais mortel) :** En tant que joueur LCC, vous serez poussé à quasiment toujours choisir l'option **"Defer to MEL"** (Reporter) pour sauver la ponctualité. 
* **L'effet Papillon :** La décision vous mordra ! Si vous partez avec un APU INOP, chaque turnaround successif nécessitera d'attendre un groupe de démarrage à air (ASU) externe, rallongeant les temps de blocage d'embarquement, supprimant le conditionnement d'air (malus Confort Pax massif), et provoquant des retards en chaîne qui frustreront votre "Anxiety meter". 

### 🔵 Profil Legacy - "La résilience via les infrastructures"
* **L'ADN :** Connexions par hub, réseau étendu, focus sur la protection de la marque et service Premium.
* **Le Confort avant le chrono :** Les temps de rotation sont moins serrés (1h+). La compagnie dispose d'alliances et de contrats de maintenance massifs sur d'autres grands aéroports, ainsi que de multiples avions de réserve à sa base (Hub).
* **Le Choix :** Le joueur Legacy aura un système de pénalité de classement beaucoup moins sévère s'il choisit **"Call Tech"** (Appeler le mécanicien), même si cela inflige 45 minutes de délai (Delay Code : *Technical*). 
* **Gestion d'image :** Accepter de voler avec une "toilette INOP" en Legacy sur un vol de 4h causera une chute vertigineuse de l'évaluation du service passager. Le jeu Legacy vous forcera à réparer ce qui impacte le passager avant de décoller. Le joueur LCC, lui, s'en foutra royalement tant que l'avion tourne.

## 4. Intégration Mécanique dans Flight Supervisor

### Événement Interstitiel "Tech Log Entry"
* Après l'atterrissage ou l'arrivée à la porte, un événement peut popper : "Maintenance Note: ENG 1 Bleed Valve stuck closed."
* Le joueur dispose de 2 boutons majeurs :
   1. **[ DEFER ITEM TO MEL ]** : Autorise le départ. Ajoute la contrainte aux Ground Ops (Ex: +10% de Total Duration sur le Refoulement). Active un compte à rebours caché (Ex: "Expires in 3 legs").
   2. **[ REQUEST REPAIR (TECH) ]** : Bloque l'avion. Ajoute un Service au sol obligatoire "Line Maintenance" qui va durer entre 25 et 90 minutes. Plombe l'heure locale, mais garantit de repartir avec un avion "Clean". 

### Exemples d'Avaries et Impacts "Flight Sup" :
* **APU INOP (Cat C) :** 
  - *Conséquence:* Ground Power Unit obligatoirement connecté très vite. L'Air Conditionné est inactif -> Si TempExt > 25°C, confort en chute libre en cabine ; blocage potentiel d'embarquement prioritaire.
* **1 Toilet (LAV) INOP :**
  - *Conséquence:* Le paramètre *Cleanliness* décline deux fois plus vite en vol. Les Pax Timeout s'accélèrent car la file d'attente crée des conflits. En Low-Cost, pas grave. En Legacy, impact sévère sur le score final.
* **Auto-Thrust INOP (Cat B) :**
  - *Conséquence:* Facteur de fatigue du pilote (vous) augmenté. Pénalité de "Handling" dans le score de fin de vol si vous êtes mal compensé sans AP.
* **Une porte (Door Assist) inopérante :**
  - *Conséquence:* Les durées de de-boarding (+15%) et boarding (+15%) augmentent, forçant le joueur LCC à choisir entre être en retard ou ne pas nettoyer l'avion pour rattraper le temps ("Skip Cleaning").

### "AOG - MEL EXPIRED" (La Ligne Rouge)
Si un joueur continue de repousser la réparation au-delà du nombre de _Cycles_ autorisés par la Catégorie (surtout pour une Cat A avec "Expires in 2 legs"), après son atterrissage, l'avion passera au statut **GROUNDED**.
Le Hub de Ground Ops refusera toute action "Start Boarding" tant qu'une opération de maintenance exceptionnelle lourde (ex: 4 Heures) n'aura pas été subie, ruinant littéralement la Session de jeu. Le joueur est donc incité à planifier ses retours au "Hub" ou se débrouiller pour réparer juste à temps. 
