# Handover - Session Fenix A320 Telemetry & Active Phase Stabilization

## Contexte
Cette session a été principalement dédiée à la stabilisation de la télémétrie du Fenix A320 et à la correction des bugs de l'évaluation des phases de vol (Active Phase) dans True Airmanship. L'utilisateur rencontrait des problèmes où les variables par défaut de MSFS (ex: le levier de train d'atterrissage, la position des spoilers) clignotaient ou affichaient "FAIL" alors que l'action physique du joueur était correcte.

## Ce qui a été accompli
1. **Stabilisation du Levier de Train d'Atterrissage (Gear Lever)** : 
   - Désactivation de la lecture de la variable générique de MSFS au profit exclusif de la L-Var `S_MIP_GEAR` lorsque le client WASM Fenix est détecté dans `SimConnectService.cs`.
2. **Normalisation des Spoilers** : 
   - Mise à jour de la détection de la plage du levier des Speedbrakes (Retracted = 66%, Armed = 100%, Deployed = <5%) pour le Fenix A320, garantissant une synchronisation exacte entre le matériel du joueur et l'UI.
3. **Refonte de la Logique Active Phase (`ScoreFlowEvaluator.cs`)** :
   - Mise à jour des méthodes `AddRule` pour utiliser le 6ème paramètre (le string `ActualState` réel), empêchant l'apparition de faux-positifs "FAIL" pendant les transitions.
   - Alignement parfait sur les SOP A320 réelles (ex: spoilers armés nécessaires pour l'Approche, tolérance pour les phares d'atterrissage et feux à éclats, spoilers rentrés pour la phase Taxi In).
4. **Nouveau Panneau "Live Telemetry"** :
   - Ajout d'un panneau d'affichage HUD de données de vol (Vitesse, Altitude, V/S, Pitch, Bank, G-Force, Flaps, Gear) dans la fenêtre de "Flight Logs".
   - Modification de `FlowTrackerService.cs` (ajout des accesseurs) et `MainWindow.xaml.cs` (transmission payload `telemetryUpdate` au Tick UI).

## Statut Technique Actuel
- La branche Git `NEW_UI` a été commitée ("feat: ajout de la télémétrie en direct et finalisation intégration Fenix A320") et poussée (`git push origin NEW_UI`).
- L'application compile sans erreurs.

## Prochaines Étapes pour le prochain Agent
- L'utilisateur entame une session de vol complet pour tester la télémétrie en direct et la transition de chaque phase de vol.
- S'il signale un bug sur la transition "Approach -> Landing", vérifiez les conditions dans `ScoreFlowEvaluator.cs` à la méthode `ValidateLanding()`.
- S'il y a un décalage (lag) sur le nouveau panneau "Live Telemetry", envisagez de découpler la fréquence d'envoi du payload JSON `telemetryUpdate` du `_uiTimer` principal (qui tourne à 1 sec).

## Consignes Globales Actives
- Le terme "Implementation" est proscrit pour nommer les documents. Utiliser toujours `Design_` pour toutes les documentations markdown créées.
