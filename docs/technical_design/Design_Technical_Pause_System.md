# [Technical Design - Pause System & Stabilisation Shield] 

Ce document décrit l'architecture technique nécessaire pour implémenter une fonctionnalité de Pause dans Flight Supervisor, qui se synchroniserait avec Microsoft Flight Simulator tout en offrant une période d'immunité (Shield) lors de la reprise pour éviter que les soubresauts du simulateur ne déclenchent de fausses pénalités.

## 1. Description du Flux Global
- L'utilisateur clique sur un bouton de Pause dans le Dashboard de Flight Supervisor.
- Le Frontend (WebView) envoie l'événement IPC `togglePause` au Backend C#.
- **Phase de Pause** : 
  - Le `SimConnectService` envoie l'événement natif `PAUSE_ON` (ou `PAUSE_TOGGLE`) à MSFS.
  - Le `FlightPhaseManager` et le `CabinManager` basculent dans un état `IsPaused = true`. Toutes les mises à jour des consos ou les checks de télémétrie sont court-circuités.
- L'utilisateur clique sur "Resume".
- **Phase de Reprise (Immunity Shield)** :
  - Envoi de l'événement `PAUSE_OFF` via SimConnect.
  - L'état local `IsPaused` passe à `false`.
  - Un compteur de grâce (ex: `ImmunityEndTime = DateTime.Now.AddSeconds(5)`) est enclenché.
  - Tant qu'on est sous le Shield, les variables telles que le **G-Force**, **Vertical Speed**, **Pitch** et **Bank** reçues par SimConnect sont ignorées pour éviter que le rebond physique capricieux de MSFS ne déclenche l'alarme `Severe G-Force` ou n'affecte l'anxiété des passagers.

## Décision Requise (User Review)

> [!IMPORTANT]
> **Origine absolue de la Pause :** 
> Voulez-vous que le bouton de Flight Supervisor soit le **seul maître** (il pause MSFS), ou souhaitez-vous que Flight Supervisor détecte également si vous appuyez sur la touche "Echap" (Echap/Echap ou bouton Pause dédié du Joystick) dans MSFS pour auto-pauser l'application de manière bidirectionnelle ? (L'écoute bidirectionnelle est faisable via les `SystemEvents` de SimConnect, mais l'inconvénient est que le menu MSFS peut souvent désynchroniser l'état).

## Modifications Proposées

### FlightSupervisor.UI\Services\SimConnectService.cs
- **[MODIFY]** `SimConnectService.cs` :
  - Ajouter l'énumérateur `EVENTS.PauseOn` et `EVENTS.PauseOff`.
  - Ajouter les mappings : `_simconnect.MapClientEventToSimEvent(EVENTS.PauseOn, "PAUSE_ON");`
  - Ajouter la méthode publique `public void SetPause(bool isPaused)` qui exécute `TransmitClientEvent` sur l'avion.

### FlightSupervisor.UI\Services\FlightPhaseManager.cs
- **[MODIFY]** `FlightPhaseManager.cs` :
  - Ajouter la propriété `public bool IsPaused { get; set; } = false;`
  - Ajouter la variable `private DateTime _immunityEndTime = DateTime.MinValue;`
  - Modifier `UpdateTelemetry` et `CalculateTurbulence` pour geler les données en entrée si `IsPaused` est True, **ou** si `DateTime.Now < _immunityEndTime`.
  - Créer la fonction `public void ResumeWithImmunity(int seconds = 5)` qui purgera les buffers de télémétrie (`_gForceHistory`) et mettra à jour l'heure de fin du Shield.

### FlightSupervisor.UI\Services\CabinManager.cs
- **[MODIFY]** `CabinManager.cs` :
  - Couper l'évolution thermique, l'anxiété, la consommation des ressources de service VIP, etc., dans la fonction `Tick()` si `_phaseManager.IsPaused` est vrai.

### FlightSupervisor.UI\MainWindow.xaml.cs
- **[MODIFY]** `MainWindow.xaml.cs` :
  - Dans la boucle IPC locale (`chrome.webview.addEventListener`), intercepter la commande `systemPause` (true/false / toggle).
  - Appeler `_simconnectService.SetPause(state)`.
  - Appeler `_phaseManager.IsPaused = state;`. Si state est `false`, appeler `_phaseManager.ResumeWithImmunity(5)`.

### FlightSupervisor.UI\wwwroot\app.js & index.html
- **[MODIFY]** `index.html` :
  - Ajouter un bouton dans le header de l'application (à côté de "Connected / Build") avec une icône de Pause. Mettre en surbrillance rouge pour signaler la pause actée.
- **[MODIFY]** `app.js` :
  - Lier le clic du bouton à `window.chrome.webview.postMessage({ action: "systemPause" });`

## Open Questions
- Où souhaitez-vous localiser ce bouton "Pause" exactement ? Dans le dashboard ou en global dans l'en-tête (Header) de la fenêtre, à côté du logo Météo pour que ce soit accessible même sur d'autres onglets ?

## Plan de Test (Verification Plan)
1. Tests unitaires : Vérification des logs `PAUSE_ON` transmis au simulateur par l'API IPC.
2. Vider les historiques : Les FIFO pour le `_gForceHistory` ou le `_vsHistory` devront être purgés au moment du retour en jeu.
3. Vérifier in-game avec MSFS : Simuler un grand coup dans le joystick pile au moment de la reprise pour certifier que le Shield de 5 secondes l'intègre bien sans vous pénaliser.
