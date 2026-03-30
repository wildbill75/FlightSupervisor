# Design Gameplay: Virtual Crew & Autonomy

## 1. Overview
Jusqu'à présent, *Flight Supervisor* plaçait le joueur dans un rôle "omnipotent" où il devait tout déclencher lui-même (annonces, services). Cette nouvelle mécanique opère un changement de paradigme fondamental : **Le commandant de bord ne fait pas le travail du PNC, il le supervise.** 

Le jeu va désormais simuler un **Équipage Virtuel (Virtual Crew)** doté d'une IA comportementale. Un bon équipage anticipe les problèmes, tandis qu'un mauvais équipage oblige le Commandant (le joueur) à faire du micro-management.

## 2. L'Équipage Virtuel (Crew Stats)

À chaque vol (ou rotation), un Chef de Cabine (Purser) et son équipage sont assignés à l'avion. Cet équipage possède des statistiques cachées ou visibles :
- **Proactivité (Proactivity Score) :** Détermine la capacité de l'équipage à prendre des initiatives sans ordre direct du cockpit.
- **Efficacité (Efficiency) :** Détermine la vitesse physique d'exécution des tâches (nettoyage, service repas).
- **Humeur / Tolérance (Morale) :** Baisse avec la fatigue (Multi-Leg) ou les passagers difficiles.

## 3. L'Autonomie des Annonces (L'IA du PNC)

Le système d'annonces cabines devient contextuel et partiellement autonome :
- **Cas d'un Équipage Proactif (Score > 80) :** Si l'avion accuse un retard au sol de 10 minutes, le Chef de Cabine prend l'initiative de faire l'annonce d'excuse ("Mesdames et messieurs, nous attendons...") de lui-même. Le joueur entend l'audio se déclencher dans son casque sans avoir touché à aucun bouton ! Le joueur ressent la satisfaction d'avoir une équipe compétente.
- **Cas d'un Équipage Passif ou Débutant (Score < 40) :** Le retard s'accumule. Le PNC ne fait rien. La Satisfaction des passagers chute lourdement. Le joueur s'en rend compte et doit intervenir en ouvrant l'Interphone.

## 4. Nouveau Paradigme de l'Interphone (Micro-Management)

Plutôt que des boutons où le pilote fait les annonces à la place des hôtesses, l'UI de communication devient un **Panneau d'Ordres**.

- **Donner un Ordre (Kick Ass) :** L'onglet Intercom proposera des boutons d'ordres directs au Chef de Cabine pour pallier leur manque d'initiative :
  - *"Ordonner l'annonce de retard"* (Fait grimper l'anxiété du PNC si utilisé trop souvent, l'équipage se sent fliqué).
  - *"Ordonner la préparation cabine"* (Si les PNC ont oublié de vérifier les ceintures à l'approche de la descente).
  - *"Demander Rapport Cabine"* (Requête d'information classique).
- **Conséquences :** Un équipage médiocre nécessitera une attention constante de la part du pilote (charge de travail augmentée dans Flight Simulator). Mais leur "crier dessus" via l'Interphone va baisser leur moral.

## 5. L'Effet Boule de Neige (L'Importance des Ground Ops)

Le Commandant décide, l'équipage subit. Si le joueur décide court-circuiter les opérations au sol pour gagner du temps, le score de Proactivité du PNC ne pourra pas le sauver :
- **Skipper le Catering :** Si le pilote part sans embarquer le catering pour rattraper un retard de 20 minutes, le PNC n'aura **aucune ressource** pour calmer les passagers en vol. La Satisfaction chutera inéluctablement, et aucun effort de l'équipage ne pourra compenser l'absence de nourriture/boissons.
- **Skipper le Nettoyage / Toilettes :** Un avion sale dégrade le *Confort* dès la minute zéro de l'embarquement.
- **La Spirale Négative :** Partir avec un avion "non optimal" génère une baisse constante de la Satisfaction. Le PNC, même proactif, sera vite débordé par les plaintes, ce qui heurtera son Moral, le rendant moins apte à gérer la suite du vol. C'est l'essence même de ce jeu de gestion : **assumer ses décisions de Commandant de Bord**.

## 6. Intégration dans le Flow du Jeu
- **Phase de Briefing :** Avant de démarrer l'avion, le manifeste des passagers affichera également un "Crew Roster" avec le nom du Purser et son "Grade/Rating". Le joueur saura immédiatement s'il va devoir micro-manager aujourd'hui.
- **Impact SuperScore :** Déléguer et avoir un bon équipage rapporte des points de "Crew Resource Management" (CRM). Devoir sans arrêt rappeler à l'ordre le PNC montre un Commandement fort, mais un mauvais CRM global.
