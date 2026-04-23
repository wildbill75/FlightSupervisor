# Bilan de Développement - Build Cabin Comms & Thermal (Session Actuelle)

Ce document récapitule l'ensemble des modifications techniques et de design implémentées lors de la dernière session de développement. 
En cas de bug ou d'échec lors de la passe de test en vol, indiquez simplement ce document ou les éléments ci-dessous pour reprendre le contexte exact.

## 1. Transition de Vol (Turnaround & Next Leg)
*Fichier impacté : `MainWindow.xaml.cs`*

- **Suppression du pop-up automatique `FuelSheetWindow`** : La fenêtre de carburant ne s'ouvre plus de force à la fin du débarquement. L'interface passe silencieusement en état "PENDING LEG 2 INITIALIZATION" pour laisser le joueur importer tranquillement son prochain plan de vol Simbrief.
- **Correction du Bug "Dummy Leg" (Faux Vol Actif)** : Lors de la transition vers un nouveau vol, le système crée une étape de vol temporaire ("Dummy"). J'ai corrigé l'oubli de la balise `IsDummy = true`. Ainsi, lors du rafraîchissement Simbrief, le système écrase bien ce vol factice et met correctement à jour le nombre de passagers attendus au lieu de rester bloqué sur l'ancien vol.

## 2. Refonte du Système d'Appels PNC (Intercom Queue)
*Fichier impacté : `CabinManager.cs`*

- **Centralisation de la Queue** : L'ancienne méthode manuelle (`EvaluateCabinCallTrigger` et `AnswerPendingCall`) a été dépréciée. **Tous** les appels cabine (Température, Retard Passagers, etc.) passent désormais obligatoirement par la méthode `TriggerIncomingCabinCall`.
- **Mécanique de Timeout (60s)** : Ajout d'un compte à rebours de 60 secondes pour chaque appel entrant. Si le joueur ignore l'appel clignotant (`FROM PNC`) pendant plus de 60 secondes, l'appel est supprimé de la file et une pénalité automatique de **-20 Crew Esteem** est appliquée pour mauvaise gestion du CRM.
- **Cooldown inter-appels (5s)** : Implémentation d'une période de "silence" obligatoire de 5 secondes après qu'un appel ait été traité ou ignoré. Durant ce laps de temps, l'UI `FROM PNC` reste inactive. Cela évite l'enchaînement brutal de multiples alarmes et laisse au joueur le temps de respirer.
- **Stabilité de la file d'attente (`_pendingCabinCalls`)** : Le système de queue a été renforcé (via `ConcurrentQueue`) pour s'assurer que la réinitialisation du `TriggerTime` d'un élément ne provoque pas la perte d'autres événements en attente.

## 3. Correction de la Thermodynamique Cabine (Inertie)
*Fichiers impactés : `CabinManager.cs`, `MainWindow.xaml.cs`*

- **Diagnostic Fenix LVARs** : Confirmation que le comportement de températures extrêmes lors de la déconnexion de l'APU / Bleed n'est pas un bug des LVARs du Fenix, mais le comportement normal du fallback du code vers la température extérieure MSFS (`CurrentAmbientTemperature`) lorsque la clim (AC) est considérée comme inactive.
- **Réduction de l'Inertie Thermique** : La dérive de température physique de la carlingue (lorsque les packs sont coupés) était beaucoup trop violente (environ 12°C par minute).
  - L' `inertiaRate` a été modifié de `0.2` à `0.02`.
  - **Résultat** : La température dérive désormais à un rythme réaliste d'environ **1.2°C par minute**. Si le joueur oublie l'APU pendant l'embarquement, il dispose désormais d'une marge de manœuvre de 10 à 15 minutes avant que la cabine n'atteigne des seuils critiques (déclenchant les appels PNC et les malus).

## Prochaines Étapes Prévues (Après validation du test en vol)
1. Intégration des fichiers audios PNC en cours de génération (ex: `EN_PNC_Beth` Arrival PA).
2. Démarrage du développement de la fonctionnalité **ACARS Weather** (`Design_Concept_ACARS_Weather.md`), incluant le bouton `REQ ACARS WX` sur le Dashboard et le rafraîchissement des données METAR/TAF.

