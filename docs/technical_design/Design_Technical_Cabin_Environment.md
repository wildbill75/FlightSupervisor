# Design: Moteur d'Environnement Cabine (Confort, Anxiété et Satisfaction)

Ce document centralise les règles et le statut d'intégration des modificateurs psychologiques et physiques appliqués aux passagers (et par extension, au score de la compagnie) durant le vol via le service `CabinManager.cs`.

L'architecture s'articule autour de trois variables principales :
- **ComfortLevel (0-100)** : Le confort physique, matériel et thermique.
- **PassengerAnxiety (0-100)** : Le stress psychologique, l'impatience ou la peur (lié aux événements ou à la physique).
- **CabinSatisfaction (0-100)** : L'indicateur final d'appréciation global qui impacte le "Super Score".

L'approche est systémique (effet "boule de neige") : une frustration thermique engendre de l'anxiété, qui augmente la fréquence d'utilisation des toilettes, ce qui salit la cabine, ce qui diminue le confort, etc.

---

## 🟢 1. Déclencheurs Implémentés et Actifs (FAITS)

### A. Physiologie et Physique de Vol (Force G)
- **Turbulences Sévères (`gMax - gMin > 0.6`)** : 
  - Effet : Baisse **immédiate** du Confort et hausse de l'Anxiété.
  - Spécial : Si les consignes "Attachez vos ceintures" sont détachées = Pénalité sévère de satisfaction et coupure du InFlightService (Repas).
  - CRM : Une annonce PA "Turbulence" adoucit considérablement le pic d'Anxiété.
- **Roulis excessif (Bank Angle > 33°)** : 
  - Effet : Chute du confort et anxiété légère déclenchée immédiatement.
- **Taux de descente vertical extrême** : 
  - Effet : Le "mal d'oreilles" dégrade fortement et passivement le niveau de Confort.

### B. Gestion Temporelle (Retards et Délais) 
- **Retards au sol (Surcharge SOBT)** : 
  - Effet : Au-delà de 5 min (puis 15, puis +60min), l'anxiété et l'insatisfaction montent crescendo.
  - CRM : Les hôtesses interviennent selon leur proactivité ("Les gens s'impatientent / deviennent agressifs"). L'utilisation de `DelayApology` (Annonce) amortit la chute de Satisfaction.

### C. Services et Biologie (Turnaround/Cabine)
- **Rupture de Stock (Catering Shortage)** : 
  - Effet : Manquer de plateaux repas en plein vol déclenche une crise majeure (+30 Anxiété, -50 Satisfaction).
- **Températures Extrêmes (Thermal Engine)** : 
  - Effet : Moins de 18°C ou plus de 30°C déclenche la jauge `_thermalDissatisfactionGauge` avec alarme PNC et effondrement sévère du Confort.
- **Déchets et "Stress Bowels"** : 
  - Effet : Si `PassengerAnxiety` dépasse 60, le métabolisme des passagers accélère. La vitesse de remplissage des cuves de toilettes (`Waste Tanks`) est **mutipliée par 3**.
- **Salissures (Dirty Cabin)** : 
  - Effet : Turbulences + Boissons = "Spillages". L'accumulation de saletés lors de vols multi-legs draine passivement le Confort.

### D. Interventions du Cockpit (PA System)
- **Annonces (PA)** :
  - `CruiseStatus` : Hausse de satisfaction, chute d'anxiété.
  - `TurbulenceApology` : Baisse massive d'anxiété.
  - `DelayApology` : Ralentit l'hémorragie de la phase de retard.
- **Pénalité de Micromanagement** : Utiliser excessivement la PA au lieu de laisser les chefs de cabine s'en occuper baisse subtilement le moral PNC.

---

## 🔴 2. Déclencheurs Futurs (À FAIRE)

Voici la liste des éléments de game design prévus mais **non encore instanciés** dans le backend :

### A. Météorologie Visuelle ("La Peur par le Hublot")
- **État ciblé** : Lire les données du SIGMET (Noaa/ActiveSky) ou l'état exact des précipitations via SimConnect.
- **Effet Attendu** : Traverser des nuages très lourds (cumulonimbus), ou d'extrêmes précipitations de pluie/neige, génère de l'anxiété chez les passagers (peur de la foudre/mauvais temps), **même en l'absence de turbulences physiques**.
- **Statut** : La météo est lue au niveau Macro (Briefing), mais n'impacte pas en temps réel la boucle du `CabinManager`.

### B. Environnement Sonore (RPM Moteurs)
- **État ciblé** : Lire les N1/RPM via SimConnect.
- **Effet Attendu** : Un changement violent de régime moteur en pleine croisière, ou un maintien injustifié du TOGA (takeoff/go-around power) fait bondir l'Anxiété.
- **Statut** : Non branché.

### C. Évaluation de l'Atterrissage (Hard Landing)
- **État ciblé** : Analyse du FPM au Touchdown et de la G-Force d'impact final.
- **Effet Attendu** : 
  - < -300 FPM (Hard Landing) : Pénalité instantanée d'appréciation finale.
  - < -100 FPM (Kiss Landing) : Bonus massif de Satisfaction et potentiel applaudissement en cabine.
- **Statut** : Le `TouchdownFpm` est lu, mais n'est pas réinjecté dans une formule `ModifySatisfaction()` au sol.

---
*Ce document sert de feuille de route (roadmap) pour les itérations futures de simulation environnementale.*
