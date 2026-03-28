# Walkthrough: Dynamic Cabin Intercom Reporting

L'objectif de cette implémentation technique était de rendre l'Intercom PNC interactif, réactif et utile de manière stratégique au déroulé du vol.

## Ce qui a été accompli (Tickets 41, 42, 43)

### 1. Intelligence du Backend (`CabinManager.cs`)
La logique de génération des rapports (via `RequestCabinReport`) a été intégralement densifiée pour refléter la situation *réelle* de l'humeur des passagers.
- **Détection des retards (SOBT)** : Le système mémorise désormais l'accumulation précise du temps de retard pendant la phase *AtGate*.
- **Pénalités dynamiques** : Si le Commandant demande un rapport alors que la cabine est en cours de sécurisation, le processus de préparation de la cabine (`_currentSecuringRate`) est lourdement pénalisé (réduit de moitié) pendant 10 secondes car les PNCs ont été dérangées.
- **Réponses Nucléées** :
  - En cas de crise majeure (médicale / indiscipline), l'équipe PNC ignore les jérémiades des passagers pour se concentrer sur l'urgence.
  - En l'absence de crise, la PNC prend en compte un passif récent (Turbulences de moins de 15 minutes, retard de plus de 15 minutes, loupé du Catering, etc.) pour exprimer explicitement l'anxiété du vol (ex: *"The passengers are getting very frustrated and restless due to this long delay"*).

### 2. Le Routage IPC (`MainWindow.xaml.cs`)
- La machinerie d'écoute de l'évènement `"action": "intercomQuery"` venant de l'Interface UI est exploitée pour déclencher la demande de statut.
- Le C# émet conditionnellement le son `intercom_ding` ou `intercom_busy` via IPC (WebSocket local) tout en transmettant le minuteur écoulé depuis le dernier appel (`CabinManager.SecondsSinceLastReport`).

### 3. Interface UI (`app.js` & `index.html`)
- Le bouton HTML a été conditionné dynamiquement dans `app.js` au sein du `dynamicIntercomContainer`.
- Une protection anti-spam visuelle et technique est activée : un **Cooldown imposé de 2 minutes** verrouille purement et simplement le bouton pour empêcher le joueur de spammer l'équipe PNC. Pendant ce temps de recharge, le bouton devient translucide et affiche le délai restant avant la prochaine tentative.

## Validation & Prochaines Étapes
- ✔️ Compilation propre (*0 erreur*).
- 🚀 **Prêt pour le vol test** : Il conviendra de surveiller le comportement de l'intercom lors de la prise de retard (SOBT) en jeu, ou suite à une zone de fortes turbulences.
