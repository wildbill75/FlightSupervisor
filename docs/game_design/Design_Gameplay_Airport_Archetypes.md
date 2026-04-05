# Design Gameplay - Archétypes et Tiers d'Aéroport

Ce document définit la logique de classification des aéroports dans Flight Supervisor. Chaque aéroport dans le jeu appartient à une catégorie globale ("Tier") qui influence mécaniquement le flux de l'escale (Turnaround), la logistique au sol, et l'apparition d'événements stochastiques.

---

## Philosophie de Design
Un vol entre `LFPG` (Paris CDG, gigantesque) et un vol vers `EGGW` (Londres Luton, aéroport LCC de taille moyenne) ne doit pas avoir le même rythme. Sur un Hub massif, le volume d'équipement au sol est abondant, mais les distances et les bouchons de circulation (Taxi) sont grands. À l'inverse, sur un petit aéroport, le peu de véhicules partagés crée potentiellement de forts temps d'attente (En Route).

---

## 1. La Matrice des Tiers

| Tier | Envergure | Distance Taxi moy. | Moyens G-Ops | Profil du Traffic | Exemples |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **S (Mega Hub)** | Intercontinental | Très longue (15-25m) | Abondants et Dédiés | Congestion constante, files d'attente | LFPG, EGLL, KJFK, OMDB |
| **A (Grand)** | Hub National | Longue (10-15m) | Dédiés ou Mutualisés | Haute densité aux heures de pointe | LFPO, EDDM, LEBL |
| **B (Moyen)** | Régional / LCC | Moyenne (5-10m) | Mutualisés (risques) | Vagues sporadiques, souvent efficace | LFMN, EGGW, LFML |
| **F (Petit / Low)** | Secondaire | Courte (2-5m) | Très faibles (1 seul) | Très calme, mais haut risque de panne | LFBO (fret), petits IFR |

---

## 2. Impacts Mécaniques (Gameplay)

### A. Véhicules de Ground Operations (Temps d'Approche / "En Route")
Lorsque l'équipage demande un service au sol (ex: Camion de restauration, citerne de carburant), il y a un délai de trajet physique.

*   **Tier S / A (Très rapides) :** Les gros aéroports assignent souvent un set de véhicules par terminal ou par porte. Le trajet est réduit au minimum car le véhicule est déjà garé juste à côté (ex: **1 à 3 minutes** d'En Route).
*   **Tier B (Moyens) :** Véhicules dispatchés par secteur. Temps de trajet : **3 à 6 minutes**.
*   **Tier F (Petits) :** Un seul camion pour tout l'aéroport. S'il est occupé ailleurs, on attend. Temps de trajet : **8 à 15 minutes** (voir génération d'événement "Rupture de ressource").

*(Note : Peu importe le temps En Route, le temps d'action **InProgress** (le temps pour faire le plein ou vider l'avion) reste toujours conditionné à l'efficacité du PNC et à la taille de l'avion).*

### B. Evénements Aléatoires (Probabilités)
Le Tier altère les tables de tirages au sort du générateur de crises (Crisis Generator) :
*   **Tier S / A :** +40% de probabilité de grève perlée / Congestion de l'espace aérien (Slot restriction CTOT).
*   **Tier F :** +50% de probabilité de panne matérielle au sol (Grues bagagistes en panne, passerelle bloquée, plus de kérosène de rechange).

### C. Gestion des Slots (CTOT - Calculated Take-Off Time)
Sur les aéroports S et A, la ponctualité de bloc (Block Off) doit être chirurgicale. Si le délai accumulé sur le Turnaround dépasse 15 minutes, l'équipage court le risque de perdre son "Slot" de décollage. (Voir `Design_Gameplay_Slot_Logic`).

## 3. Logique d'Attribution dans le Système
La détermination du Tier se fera par recoupement si possible :
1. Selon le **nombre de pistes** ou les **données cartographiques** remontées du Sim.
2. S'il n'y a pas d'API pour la catégorisation, la logique *fallback* pourrait se baser sur le temps de croisière SimBrief lié à l'infrastructure.
3. Pour la MVP, on peut fonctionner sur un ratio de "chance" si l'information n'est pas fiable, ou via une Database locale associant les 100 gros aéroports mondiaux, le reste devenant Tier B ou F.
