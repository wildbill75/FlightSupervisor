# Design_Technical_App_Lifecycle

Ce document détaille les modalités techniques et le comportement de Flight Supervisor au moment du lancement et de la fermeture (App Lifecycle). Il met en lumière une nouvelle fonctionnalité d'automatisation (Auto-Fetch) et insiste sur l'importance du contrôle par l'utilisateur via les options.

---

## 1. Feasibility Report : Auto-Fetch du Plan de Vol (SimBrief)

**Réponse courte : OUI, c'est totalement et facilement faisable.**

Il est très courant, dans les utilitaires de simulation aérienne (comme vPilot ou Navigraph), de démarrer l'application avec les données déjà pré-chargées.

### Comment ça marche (Mécanique Télémétrique)
1. Au lancement matériel du fichier `.exe` (`MainWindow.xaml.cs` > `MainWindow_Loaded`), l'application lit son fichier de sauvegarde (`settings.json` ou `Properties.Settings.Default`).
2. Si le `SimBrief ID` ou le `SimBrief Username` est configuré **et** que l'option booléenne `AutoFetchOnStartup` est à `true` :
3. Le backend C# initie en arrière-plan la méthode `FetchLatestFlightPlanAsync()` de notre service `SimbriefService`.
4. Pendant ce temps, l'UI en HTML/JS finit de charger et affiche, par exemple, un petit loader ou un texte "Syncing Flight Plan...".
5. Dès que la donnée est reçue et parsée (OFP Data), le backend envoie son bridge habituel au frontend (`briefingUpdate` / `manifestUpdate`).
6. Le joueur arrive devant une interface **déjà remplie** et "Ready to Fly". S'il n'y a pas de plan de vol actif, l'UI repasse à son état neutre (bouton manuel).

## 2. Modalités de Lancement (Startup)

Pour s'assurer que l'application reste flexible et non-intrusive, la philosophie du "Lancement" doit suivre cette architecture.

- **Option : Démarrer à l'arrivée dans le Cockpit vs Lancement Manuel**
  *Par défaut*, le joueur lance l'exécutable lui-même. Cependant, nous avons (ou aurons) un paramètre pour auto-lancer l'application quand MSFS se lance (via `exe.xml`, méthode commune et transparente dans Flight Simulator).
- **Auto-Connect SimConnect**
  Le `SimConnectAdapter` doit tenter de se brancher *automatiquement* au lancement, sans exiger que le joueur clique systématiquement sur "LINK SIMULATOR". En cas d'échec (Simulateur fermé), un Fallback bascule sur le mode d'attente manuel avec le bouton visible.
- **Règle d'Or de l'UX (Le Rôle des Options)**
  Toujours se poser la question : *« Si cette automatisation foire ou frustre un utilisateur, peut-il l'éteindre ? »*
  - **Auto-Fetch Simbrief** : [ON/OFF]
  - **Auto-Connect MSFS** : [ON/OFF]
  - **GSX Sync Ground Services** : [ON/OFF] (Comme modélisé dans notre audit GSX).

## 3. Modalités de Fermeture (Shutdown)

Quitter le logiciel ne doit pas être une destruction aveugle du processus (`Kill`). 

### Hooking du "Close Event"
Dans WPF (`Window_Closing`), nous devons implémenter le mécanisme suivant avant de libérer la mémoire :

1. **Sauvegarde des Options Modifiées (`Save`) :** Si le joueur a changé sa métrique (`KG` -> `LBS`), modifié son design de PFD, ou allumé l'Auto-Fetch depuis l'interface Web, le bridge C# doit s'assurer que ces settings sont sérialisés et écrits sur le disque.
2. **Sauvegarde du Contexte de Vol (State persistence) :** Si jamais le logiciel a été fermé par erreur **en plein vol**, il est judicieux de sauvegarder le log actuel (`_issuedCommands`, `SuperScore` temp, `FlightPhase`). Si relancé 2 minutes plus tard, un *recovery* peut être proposé. *(Ceci est un Nice-to-Have, à isoler si trop complexe).*
3. **Fermeture propre de SimConnect :** Envoi d'un message `Dispose()` à MSFS pour libérer le "handle" et ne pas surcharger la mémoire IPC de Windows.
4. **Clean Exit :** `Application.Current.Shutdown()`.

## Bilan
Cette routine de cycle de vie (Auto-Fetch compris) offre une expérience utilisateur premium (« Click & Fly ») tout en restant robuste et totalement débrayable si le réseau (ou l'utilisateur) est capricieux.
