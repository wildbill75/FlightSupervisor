# Design: Company Communications & Dispatch ACARS

## 1. Vision Globale
Dans le cadre de l'aspect "Gestion / Tycoon" et de la gestion dynamique des retards, le commandant de bord n'est pas omnipotent. Toute modification majeure du plan de vol qui a un impact financier (Fioul, Retard, Correspondances) doit être négociée avec le "Company Dispatch" (le centre de contrôle opérationnel de la compagnie de l'Airlines Profile).

L'idée est de créer une **Interface de Communication (Company COM / ACARS)** au sein de Flight Supervisor permettant au joueur d'interagir avec virtuellement sa compagnie.

## 2. L'Interface (Frontend)
### 2.1 Emplacement
Onglet dédié ou menu latéral rétractable nommé **"ACARS"** ou **"COMPANY COM"** dans le Dashboard de Flight Supervisor.
L'interface prendra l'apparence d'une messagerie textuelle stérile (façon terminal ACARS ou chat pro), avec un historique des messages horodatés (UTC).

### 2.2 Types de Messages
- **[UPLINK] (Reçus)** : Messages générés par la compagnie (ex: alerte météo, confirmation de slot, approbation/refus de requête).
- **[DOWNLINK] (Envoyés)** : Requêtes du pilote générées via un menu déroulant de requêtes pré-formatées (ex: `REQ COST INDEX INC`, `REQ TURNAROUND EXP`).

## 3. Les Requêtes du Pilote (Gameplay Loops)

### 3.1 Demande d'augmentation du Cost Index (Rattrapage de temps)
**Contexte :** Le pilote constate un retard global (Global Rotation Delay Estimator dans le rouge). Il souhaite accélérer en vol, ce qui nécessite d'augmenter le Cost Index (ex: passer de CI 12 à CI 45).
**Impact :** Voler plus vite coûte beaucoup plus cher en carburant. Si le pilote le fait *sans autorisation*, son score de gestion financière (Airmanship) s'effondre.
**Fonctionnement :**
1. Le joueur ouvre le Company COM et sélectionne `REQUEST REVISED COST INDEX (LATE SCHEDULE)`.
2. Le système calcule la réponse en fonction du **Profil de la Compagnie** :
   - **Compagnie Low-Cost (EasyJet, Ryanair)** : Politique de "Cost Saving" stricte. Le Dispatch refusera 80% du temps sauf si le retard dépasse un seuil dramatique (ex: > 90 mins où l'équipage risque de dépasser ses heures réglementaires de vol).
   - **Compagnie Legacy (Air France, Lufthansa)** : Politique axée sur le "Service Client" et les correspondances. Le Dispatch pardonnera le coût du fioul si cela sauve le planning des passagers. Approbation fréquente.
3. **Réponse ACARS Uplink** :
   - *Approuvé :* "DISPATCH TO CAPTAIN // REVISED CI APPROVED UP TO CI 50 // AUTHORIZED FUEL BURN MELTED. CATCH UP MAXIMUM TIME." (Le joueur peut alors taper CI 50 dans le Fenix sans pénalité).
   - *Refusé :* "DISPATCH TO CAPTAIN // CI REVISION DENIED // FUEL BUDGET RESTRICTED MAINTAIN PLANNED CI 12." (Si le joueur désobéit, forte pénalité).

### 3.2 Demande de Turnaround Express
**Contexte :** Rattraper du temps au sol.
**Fonctionnement :**
Le joueur demande un "Express Turnaround". S'il est approuvé, l'algorithme "Semi-Auto Ground Ops" (Ticket 39) bascule dans un mode d'urgence où les phases se chevauchent dangereusement (Embarquement pendant le Fueling, Catering limité, pas de nettoyage complet).
*Contrepartie :* Cela stresse considérablement les PNC, réduit le Confort Cabine (pas de nettoyage), mais fait gagner de précieuses minutes d'AOBT.

### 3.3 Demande d'Assistance / Diversion (Pannes, Passager Malade)
*Feature future :* En cas de passager malade, le joueur utilise l'ACARS pour demander aux opérations s'il doit se dérouter ou si la compagnie l'autorise financièrement à se poser sur l'aéroport de dégagement (qui a un coût de taxe aéroportuaire différent).

## 4. Architecture Technique (Backend C#)
- **Modèle de données :** Une classe `CompanyMessageCenter` qui stocke la liste temporelle des `CompanyMessage`.
- Un **gestionnaire d'évènements (Dispatcher Engine)** : Lorsqu'un Downlink est envoyé, le code C# intercepte la requête, consulte le `AirlineProfile` en cours (Economy/Legacy stats), évalue la demande (via un jet de probabilité RNG pondéré ou une logique stricte : Délai par rapport à X), et répond après un délai artificiel (ex: 2 à 5 minutes pour simuler le temps pris par l'agent au sol pour calculer/répondre).
- **Notification IPC :** Un son (carillon ACARS) est émis dans l'application locale, et une pastille rouge apparaît sur l'onglet COM.

## 5. Bilan
Cette mécanique accomplit 3 choses primordiales pour le jeu :
1. Elle donne du **poids concret** aux différences entre les compagnies aériennes (voler pour EasyJet ne sera pas jouer de la même manière que de voler pour Air France).
2. Elle **légitime** les actions du pilote. Au lieu de tricher dans le simulateur, le joueur doit jouer le jeu de l'administration aérienne.
3. Elle crée un sentiment **d'immersion et de responsabilité**. Le joueur n'est plus seul, il rend des comptes à une entité supérieure.
