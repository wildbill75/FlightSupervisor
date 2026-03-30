# Design Gameplay : Confort Thermique en Cabine (Temperature Comfort)

## 1. Description de l'Objectif
La gestion de la température de la cabine doit devenir un acteur fondamental de la métrique globale de **Confort** et de **Satisfaction**. À l'heure actuelle, la logique de détection des températures extrêmes n'est pas assez nuancée et ne prend pas en compte le fonctionnement réel des systèmes de l'avion (notamment sur le Fenix A320), ni l'impact progressif du temps d'exposition sur le bien-être des passagers.

Ce design redéfinit le "Ressenti Passager" (Plage de Confort), ajoute une notion d'accumulation ("Jauge Virtuelle de Mécontentement") basée sur la durée d'exposition, et orchestre des interactions autonomes du Chef de Cabine vers le cockpit.

---

## 2. Physique du Simulateur (Cas d'étude Fenix A320)

D'après les relevés in-game :
- **Chute Thermique (Cold) :** Même avec la climatisation coupée par 14°C extérieur (OAT), la cabine a du mal à chuter sous les 18°C de par l'inertie thermique de l'avion. En forçant la climatisation (Pack Flow HIGH, sélecteurs sur FROID), on se stabilise à **18°C**.
- **Chauffe (Hot) :** En forçant la climatisation au chaud maximum (Pack Flow HIGH, sélecteurs sur CHAUD), on se stabilise à **30°C**.
- **Limites d'exposition extrêmes :** La plage de températures logiques de tolérance se situe donc entre 10-12°C (au minimum) et ~96-97°C (si l'avion séjourne au soleil en zone désertique sans clim). 

**Conclusion :** Les malus doivent se déclencher dans une bande métrique bien plus resserrée, allant approximativement de 18°C à 30°C.

---

## 3. La Plage de Ressenti et d'Évaluation

### A. Le Moment d'Évaluation (Conditions de Départ)
- L'algorithme se base en grande partie sur la température extérieure et sur la situation de l'avion, car les passagers entrant dans l'appareil sont "conditionnés" par la météo extérieure. L'évaluation de la température en cabine n'a de sens que sur la durée et une fois la dynamique du vol établie.

### B. Mappage des Températures
- **Zone Neutre / Confort Idéal :** `[ 21,0 °C - 24,0 °C ]`
  - Aucun malus. Les passagers se sentent bien.
- **Désagrément Froid / Gradatif :** `] 18,0 °C - 21,0 °C [`
  - Plage froide gérable. Le mécontentement s'accumule doucement.
- **Désagrément Chaud / Gradatif :** `] 24,0 °C - 30,0 °C ]`
  - Plage chaude gérable. Le mécontentement s'accumule doucement.
- **Extrême Froid :** `<= 18,0 °C`
  - Les passagers gèlent. Action urgente requise.
- **Extrême Chaud :** `> 30,0 °C`
  - La cabine devient un four. Action urgente requise.

---

## 4. Mécanique Temporelle & Conséquences

### Jauge Virtuelle d'Inconfort Thermique (Time-based Exponential Drain)
Le désagrément thermique ne frappe pas immédiatement, il s'accumule **exponentiellement** avec le temps d'exposition :
1. Plus la température est basse (ou haute) et hors de la zone `21-24°C`, plus la vitesse de remplissage de la jauge est importante.
2. Si la situation "extrême" (`<18°C` ou `>30°C`) s'éternise, l'impact sur les statistiques de **Confort** et de **Satisfaction** globales de l'avion sera drastique et exponentiel ("ça doit baisser de manière exponentielle / très violente").
3. **Drainage (Recouvrement) :** Si le pilote ajuste la température qui retourne dans la plage `21-24°C`, la tension redescend doucement.

---

## 5. Retours PNC (Communication au Cockpit)

Pour guider le joueur (qui est confiné dans le cockpit virtuel) sur l'état thermique de sa cabine, le PNC intervient. 

### 1) Push Alert ! (Message PNC Automatique)
- Si l'inconfort monte trop haut ou dure trop longtemps, le PNC déclenchera *de lui-même* l'interphone.
- **Audio/Message PNC :** 
  - *Froid :* "Captain, a few passengers are complaining about the cold. Please consider turning up the AC."
  - *Chaud :* "Captain, it's getting really hot back here, passengers are complaining. Can you adjust the temperature?"
- **Pas d'acquittement requis :** Cette alerte est purement informative. Le pilote doit régler le problème lui-même sur l'Overhead Panel.
- **Confirmation de Résolution :** Si le PNC a émis une alerte, et qu'ensuite le pilote corrige le tir (la température revient à la normale pendant un 'cooldown' suffisant), le PNC rappelle : "Captain, the temperature is much better now, thank you."

### 2) Intercom Cabin Report (Rapport sur Demande)
- Lors d'une demande manuelle de statut via l'UI (`[Request Cabin Report]`), le texte/l'audio généré doit souligner l'état de la température si elle n'est pas bonne ("We have some complaints about the heat/cold...").

---

## 6. Liste des Tâches d'Implémentation (Task Segmentation)

*En attente du GO de l'utilisateur pour lancer les modifications dans le code backend.*

- [ ] **Tâche 1 : Ajout de la métrique `ThermalFrustration`**
  - Dans `CabinManager.cs`, intégrer une variable continue mesurant l'accumulation de frustration (ex. `_thermalAgitationGauge` de 0 à 100).
- [ ] **Tâche 2 : Logique de Tick Météo (La Gradation et l'Exponentielle)**
  - Implémenter la logique dans `CabinManager.Tick()`:
  - Ignorer au besoin lors des portes grandes ouvertes sans APU/PCA (à définir si le boarding conditionnel est exclus).
  - Calculer un multiplicateur agressif pour `<18°C` et `>30°C`.
  - Appliquer des malus directs sur `CabinComfort` et `CabinSatisfaction` en cas de forte agitation.
- [ ] **Tâche 3 : Alarmes PNC (Alerting System)**
  - Construire un événement `OnThermalComplaintTriggered` et `OnThermalResolved`.
  - Intégrer la logique pour lancer les fichiers audio correspondants (Froid / Chaud / Résolu).
- [ ] **Tâche 4 : Rapport de statut (Cabin Status)**
  - Modifier le générateur de compte-rendu PNC pour y injecter dynamiquement le sentiment des passagers lié à la chaleur/au froid si celui-ci s'écarte de la zone neutre.
- [ ] **Tâche 5 : Télémétrie et Feedback UI**
  - Envoyer éventuellement une data au frontend ou un log dans la boîte d'événements "PNC" lorsque la température est critique, pour assister la compréhension du joueur.
