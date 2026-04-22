# Design Gameplay: Virtual Crew & Autonomy

## 1. Overview
Jusqu'à présent, *Flight Supervisor* plaçait le joueur dans un rôle "omnipotent" où il devait tout déclencher lui-même (annonces, services). Cette nouvelle mécanique opère un changement de paradigme fondamental : **Le commandant de bord ne fait pas le travail du PNC, il le supervise.** 

Le jeu simule un **Équipage Virtuel (Virtual Crew)** doté d'une IA comportementale. Un bon équipage anticipe les problèmes, tandis qu'un mauvais équipage oblige le Commandant (le joueur) à faire du micro-management.

## 2. La Machine à État des PNC (Persistance)

Là où la cabine (les passagers) se réinitialise entièrement à chaque nouveau segment de vol (Leg), l'équipage (PNC) est une **machine à état persistante sur l'intégralité de la rotation**.
C'est cela qui détermine la difficulté : un mauvais vol va épuiser l'équipage. S'il n'est pas géré, la Leg 2 sera un cauchemar (lenteur de service, aucun automatisme).

En coulisses, le moteur conserve précieusement 3 statistiques historiques pour la rotation :
- **Proactivité (Proactivity)** : Capacité à devancer vos ordres.
- **Efficacité (Efficiency)** : Vitesse physique de service et de préparation (boarding, securing).
- **Moral (Morale)** : Tolérance au stress et patience.

**La "Note de l'équipage" (CrewEsteem)** : Affichée en très grand sur l'interface, calculée sur **10** avec une décimale (ex: `8.5`). Le backend la calcule secrètement sur 100 via la moyenne stricte des 3 valeurs ci-dessus.
*(Note technique : du 'bruit' est ajouté pour qu'un équipage n'atteigne jamais parfaitement 10.0 ou 0.0 - on observera un flottement autour de 9.5 ou 1.2).*

## 3. Triggers : L'Épuisement de l'Équipage

L'équipage subit l'évolution du vol et vos décisions. Voici les déclencheurs (triggers) qui érodent ou restaurent ces 3 piliers :

> [!WARNING]
> **A. Impact sur le Moral (Stress & Interactions Humaines)**
> - **Turbulences Sévères + Seatbelts OFF** : Si ça secoue fort et que l'équipage n'est pas attaché, c'est un risque énorme = baisse de moral.
> - **Plaintes successives des passagers** : Le pilote est responsable de l'avion, mais ce sont les PNC qui gèrent les plaintes. S'il fait trop froid (<18°C) ou trop chaud (>26°C), la plainte fait baisser le moral.
> - **Retards sans excuses** : Maintenir les passagers bloqués au sol pendant un très gros délai fait s'effondrer la satisfaction, mais aussi le Moral de l'équipe qui doit gérer l'émeute en porte.
> - **Événements Inattendus** : À l'avenir, toute situation anormale, panne ou passager indiscipliné aura un contrecoup direct sur le moral et la fatigue de l'équipe de cabine.

> [!WARNING]
> **B. Impact sur la Proactivité (Balance de Communication)**
> - **Micromanagement** : Le pire ennemi de la proactivité. Si le Commandant utilise les boutons manuels `[CPT PA]` pour faire les annonces (ou les appelle massivement en permanence), l'équipage se sent harcelé, flicqué, et attendra vos directives par agacement. Cela baisse l'Efficacité et la Proactivité.
> - **Abandon (Silence Radio)** : Si le commandant ne prend jamais la peine de demander un "Cabin Report" sur un long vol de croisière (plus de 1h de vol ou 3h d'inactivité complète), l'équipage se sent ignoré et délaissé. Le Moral chute doucement.
> - **Utilisation du bouton "Hurry"** : Bousculer le service ou le Turnaround détruit le sens de l'initiative propre de l'équipage.

> [!WARNING]
> **C. Impact sur l'Efficacité (La Fatigue Physique)**
> - **Enchaînement des Legs (Fatigue de Base)** : Au démarrage de chaque nouveau vol de la rotation (à l'exception de la leg 1), la fatigue du tronçon précédent grève l'efficacité. Plus la rotation est longue, plus l'équipe sera lente sur les Legs 3, 4, etc.
> - **Températures Chaudes** : Température cabine maintenue longtemps à > 26°C = Taux de chute de l'Efficacité (ils suent et s'épuisent).

### Triggers : La Récupération temporaire

> [!TIP]
> - Lors de la phase de **Turnaround** (L'escale au sol), lorsque les passagers de la leg précédente sont débarqués, si la gestion du temps le permet, l'équipe récupère un peu de souffle : **+10% de Moral restauré**. L'Efficacité, elle, ne remonte jamais.

## 4. Nouveau Paradigme de l'Interphone (Micro-Management)

Plutôt que des boutons où le pilote fait les annonces à la place des hôtesses, l'UI de communication devient un **Panneau d'Ordres**.

- **Donner un Ordre (Kick Ass) :** L'onglet Intercom proposera des boutons d'ordres directs au Chef de Cabine pour pallier leur manque d'initiative :
  - *"Ordonner l'annonce de retard"* 
  - *"Ordonner la préparation cabine"* (Si les PNC ont oublié de vérifier les ceintures à l'approche de la descente).
  - *"Demander Rapport Cabine"* (Requête d'information classique - attention à trouver le juste équilibre comme vu plus haut).
- **Conséquences :** Un équipage médiocre nécessitera une attention de la part du pilote. Mais mal doser le "CRM" aura un désavantage sur le SuperScore.

## 5. L'Effet Boule de Neige (L'Importance des Ground Ops)

Le Commandant décide, l'équipage subit. Si le joueur décide de raccourcir les opérations au sol pour gagner du temps :
- **Skipper le Catering :** Si le pilote part sans repas pour rattraper 20 minutes, le PNC n'aura aucune ressource en vol. C'est le PNC qui subira la colère des passagers et verra son Moral chuter.
- **La Spirale Négative :** Partir avec un avion "non optimal" génère une baisse constante de la Satisfaction. Le PNC, même proactif, sera vite débordé par les plaintes, heurtera son Moral, le rendant moins apte à gérer la suite du vol.

C'est l'essence même de ce jeu de gestion : **assumer ses décisions de Commandant de Bord** et piloter son équipage en bon leader humain.
