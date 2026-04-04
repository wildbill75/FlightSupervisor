# Design Gameplay - Profil Airine : Easyjet (EZY/EJU/EZS)

Ce document décrit en détail la "feuille de personnage" (profil opérationnel) pour la compagnie Easyjet. Ces paramètres seront lus par l'application pour modifier l'expérience de vol, les durées au sol, la politique carburant et l'ambiance cabine.

---

## 1. Identité & Classification
*   **Code ICAO / IATA** : EZY (UK) / EJU (Europe) / EZS (Switzerland) / U2
*   **Callsign** : EASY / ALPINE
*   **Tier de Compagnie** : LOW COST (Tier C/D)
*   **Flotte Typique** : A319 / A320 / A320neo / A321neo

## 2. Profil d'Équipage (PNC)
*   **Efficiency (Efficacité) : 95%** 
    *   *Des machines de guerre au sol.* Le nettoyage et l'embarquement sont optimisés à l'extrême.
*   **Empathy (Empathie) : 35%** 
    *   *Expéditifs.* Sourire commercial de base, mais focalisés sur les ventes à bord (Boutique/Bistro). Moins de temps pour faire "la nounou" avec les passagers anxieux.
*   **Proactivity (Proactivité) : 40%** 
    *   *Application stricte des SOPs.* Remontent les problèmes au cockpit plutôt que de prendre de gros risques d'initiative, pour se couvrir.

## 3. Ground Operations (Opérations au Sol)
*   **Target Turnaround Time (Objectif d'Escale)** : **25 à 30 minutes** maximum.
*   **Procédure d'Embarquement** : 
    *   Agricole / Double porte (Front & Rear via pax steps) = accélérateur d'embarquement natif si supporté par l'aéroport.
    *   Pas de Zone Priority complexe (juste Speedy Boarding puis le reste).
*   **Bagages cabine (Carry-Ons)** : 
    *   *Problème pénalisant* : Probabilité très élevée de la crise "Overhead Bins Full" juste avant le départ, demandant au PNC de mettre des bagages en soute à la dernière minute.
*   **Catering** : Pas de chargement de gros chariots repas chaud. Uniquement chargement de "Bistro" (snacks/boissons) une fois par jour. Temps de catering presque nul aux escales.
*   **Nettoyage** : Opéré par les PNC ("Cabin Clean (PNC)") entre les vols, d'où la fatigue accumulée en multi-leg.

## 4. Politique Flight Deck & Aircraft (Performance)
*   **Cost Index (CI)** : Généralement **CI 10 à 15** (Vol économique, vitesse réduite). Souvent CI 0 dans le simulateur ou CI 30 si gros retard.
*   **Fuel Policy (Politique Carburant)** : 
    *   *No Extra Fuel Tolérance.* Le fuel est calculé au plus juste (Contingency à 3% ou 5%).
    *   Culture de la "pince" : Ajouter de l'Extra Fuel sans bonne raison MTO est perçu négativement par le score "Company Satisfaction".
    *   *Tankering* fréquent (on fait le plein pour l'aller et le retour si le fuel est moins cher au départ).
*   **APU Policy** : 
    *   Extinction de l'APU obligatoire dès que l'avion est connecté au GPU (Ground Power Unit) ou au repoussage avec Single Engine Taxi.
    *   Utilisation restreinte de l'APU en vol ou au sol = Les PNC se plaignent plus vite de la température.

## 5. Passagers (Type de clientèle)
*   **Anxiété de Base** : Faible (Passagers habitués et vacanciers).
*   **Patience de Base** : Très Faible face aux retards.
*   **Attentes Confort / Pitch** : Inexistantes (Ils savent pourquoi ils ont payé). L'usure de la satisfaction client dépend presque uniquement de la ponctualité (On-Time Performance) et du comportement de l'avion (Soft Landings = applaudissements fréquents).
