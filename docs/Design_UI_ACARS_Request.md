# Design_UI_ACARS_Request

## Contexte
Suite au bilan de session sur la refonte de l'interface Briefing Météo, il a été demandé d'implémenter une interface de requête ACARS manuelle fonctionnelle (style MCDU) permettant au joueur de "requêter" lui-même l'actualisation via un bouton dédié.

## Objectif du Design
Créer une UI de style MCDU/Cockpit qui renforce l'immersion "True Airmanship".
- Abandonner le simple bouton qui tourne en boucle.
- Afficher un Modal dédié lors de l'appel à la fonction de rafraîchissement météo.
- Maintenir une cohérence fonctionnelle avec le backend `acarsWeatherRequest`.

## Structure de l'Interface (Modal)
L'UI se présente sous forme de popup superposée (Z-index 60) avec un fond sombre et un encart central rappelant un ordinateur de bord (MCDU) :

1.  **Header** : `AOC WX REQ 1/1`
2.  **Corps de message (Variables dynamiques)** :
    *   `< ORIGIN` : (Aéroport de départ du Leg actif)
    *   `< DESTIN` : (Aéroport d'arrivée du Leg actif)
    *   `< ALTN` : (Aéroport de dégagement du Leg actif)
3.  **Actions et Status** :
    *   Le statut affiche `READY` puis `SENDING...`, puis `UPLINK IN PROGRESS`, et enfin `PRINTING NEW WX...`
    *   Boutons d'interactions typés FMC (`< CLOSE` et `SEND REQ *`)
4.  **Scratchpad** : Simule visuellement l'envoi et la réception des paquets réseau AOC.

## Logique Javascript (Frontend)
- **Ouverture (`window.requestAcarsUpdate`)** : Ouvre le modal, réinitialise l'état visuel de la requête, et injecte les aéroports du Leg en cours en lisant `window.allRotations[window.activeLegIndex]`.
- **Envoi (`window.sendAcarsReq`)** :
    - Fait disparaître le bouton d'envoi pour bloquer les requêtes multiples.
    - Démarre une séquence de `setTimeout` simulant un délai réaliste d'uplink radio/satellite (2 secondes + 3 secondes).
    - Envoie ensuite la vraie requête IPC au backend C# via : `window.chrome.webview.postMessage({ action: 'acarsWeatherRequest' });`
    - Termine la séquence avec l'apparition du bouton `< EXIT`.

## Lien Backend (C#)
Le message renvoyé au backend déclenche la méthode asynchrone `RefreshLiveWeatherAsync()` dans `MainWindow.xaml.cs`, qui met à jour l'objet `BriefingData` et renvoie la nouvelle instance typée via l'IPC au frontend pour être repassée dans `parseBriefing()`. Le composant badge de météo s'assure déjà de colorer en orange/rouge les conditions via `getSeverityStyle()`.
