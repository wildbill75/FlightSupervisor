# Design Document: Générateur de Crise (Crisis Generator)

## 1. Synthèse du Concept
Le **Générateur de Crise** est un moteur d'événements aléatoires ou conditionnels conçu pour briser la monotonie des longs vols (particulièrement en croisière) et tester la réactivité du commandant de bord (le joueur). 

Plutôt que de subir un vol parfait, le joueur peut être confronté à des urgences médicales, des incidents passagers, des aléas météorologiques extrêmes ou des pannes simulées. La gestion de ces crises aura un impact massif sur le **SuperScore**, l'**Anxiété** et le **Confort** de la cabine, offrant d'énormes bonus de leadership si la situation est bien gérée, ou des pénalités fatales si elle est ignorée.

## 2. Idées et Mécaniques Actives

### A. Le Moteur de Déclenchement (Trigger Engine)
Les crises ne sont pas purement aléatoires, elles doivent avoir du sens :
- **Probabilité de base :** % de chance par heure de vol (paramétrable dans les réglages "Probabilité de Crise").
- **Déclencheurs contextuels :** 
  - *Turbulences sévères prolongées* -> Augmente les risques d'urgence médicale ou de passager indiscipliné.
  - *Retard important (AOBT)* -> Augmente la probabilité de tensions cabine.

### B. Typologie des Crises
1. **Urgence Médicale (Medical Incident) :** Un passager fait un malaise.
   - *Action requise :* Appel PNC (Intercom) -> Annonce PA ("Doctor on board") -> Décision de diversion (si grave) ou traitement à bord.
   - *Impact :* L'anxiété cabine grimpe lentement. La gérer rapidement rassure tout le monde.
2. **Passager Indiscipliné (Unruly Passenger Escalation) :** Une évolution de l'événement actuel.
   - *Action requise :* Refus de servir de l'alcool (Catering), Annonce PA autoritaire, avertissement de la police locale à l'arrivée.
3. **Dépressurisation Cabine / Technique (Cabin Depressurization) :** 
   - *Action requise :* Check télémétrie MSFS (chute d'altitude rapide exigée < 10,000ft), Annonce masque à oxygène.
4. **Alerte Météo Critique (Severe Weather / ATC Hold) :**
   - *Action requise :* Attacher les ceintures immédiatement, arrêter le service Catering, rassurer par PA.

### C. UI / UX Visuelle
- **Alerte Critique :** Une bannière rouge clignotante apparaît sur le Dashboard principal de Flight Supervisor (accompagnée d'un son "Chime" d'urgence, type triple ding).
- **Timer de Réaction :** Une jauge de temps s'écoule. Plus le joueur met de temps à réagir (via l'Intercom ou le pilotage), plus l'anxiété de la cabine devient incontrôlable.

---

## 3. Liste des Tickets (Structure du Design)

Voici le découpage technique pour intégrer ce design étape par étape dans l'application :

- [ ] **TICKET 1 : Architecture du Core CrisisEngine**
  - Création du service `CrisisManager.cs` en C#.
  - Implémentation du système de probabilités et d'évaluation conditionnelle dans la boucle `Tick()` du vol (actif uniquement après le décollage).
  
- [ ] **TICKET 2 : UI Crisis Banner & Alarme Sonore**
  - Ajout d'une zone d'alerte UI dans `app.js` et `index.html` (qui écrase temporairement le Meta Bar ou le Dashboard Header).
  - Lecture d'un fichier audio (Emergency Chime) via l'UI lors de la réception d'un event `crisisTriggered`.

- [ ] **TICKET 3 : Implémentation "Urgence Médicale"**
  - Ajout du scénario Medical Emergency dans le `CrisisManager`.
  - Intégration des boutons de résolution spécifiques (Nouveau bouton PA "Medical Assistance", bouton Intercom "Report to Captain").
  - Calcul du SuperScore : +150 points si géré en moins de 3 minutes.

- [ ] **TICKET 4 : Implémentation "Passager Indiscipliné Évolué"**
  - Migration de l'ancien événement vers le nouveau système de crise.
  - Pénalité dynamique appliquée à la barre de confort (tout le monde est dérangé) jusqu'à ce que le joueur règle le problème via l'Intercom.

- [ ] **TICKET 5 : Couplage Télémétrie "Dépressurisation" (Optionnel/Plus Tard)**
  - Lecture de l'altitude MSFS en temps réel dès le déclenchement de la crise.
  - Validation de la crise uniquement si le joueur atteint 10,000ft en moins de X minutes (Emergency Descent).

- [ ] **TICKET 6 : Paramètres Utilisateur**
  - Ajout d'un slider "Fréquence des Crises" (Off, Réaliste, Fréquent, Chaos) dans l'onglet Settings.
  - Sauvegarde du paramètre et retransmission au C# lors du `FetchFlightPlan`.
