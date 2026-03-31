# Design_Technical_Handover_NextSession

## Ce qui a été accompli (Multi-Leg & Debug Tools)

### 1. Persistance Multi-Leg (Turnaround)
- **`ShiftStateManager.cs`** : Création d'un système de sérialisation léger écrivant l'état de l'équipage et des consommables dans `ShiftState.json` à chaque fin de vol (`FlightPhase.Arrived`).
- **Variables Persistantes** : `SessionFlightsCompleted`, propreté de la cabine (`CabinCleanliness`), Ration de Repas (`CateringRations`), niveaux d'eau et déchets (`WaterLevel`, `WasteLevel`) ainsi que le Moral et la Réputation.
- **Interface Utilisateur ("Continue Shift")** : À l'initialisation de l'application, l'événement IPC `uiReady` lit le JSON. Si une sauvegarde existe, une pop-up modale demande au joueur de "Continuer son shift" ou "Sign-off" (Réinitialiser).

### 2. Outils de Débogage Hors Ligne (Developer Phase Sim)
- **UI Dédiée** : Un panneau rouge "Dev Tools" a été inséré dans l'onglet `Settings` du frontend (`index.html`).
- **Sauts Temporels Artificiels** : Des boutons permettent de forcer le passage aux phases clés du vol sans être lié à MSFS.
- **Accélération Logique (`FastForward`)** : Dans `CabinManager.cs`, la fonction `FastForward(deltaSeconds, phase)` draine instantanément et de façon mathématiquement proportionnelle les consommables (eau, propreté, toilettes) en simulant des sauts dans le temps (ex: +30 minutes de croisière, durée du blocktime SimBrief, etc.). Les repas sont déduits au passage de la croisière.
- **Stabilité** : Compilation vérifiée avec succès. Application 100% testable hors ligne.

---

## Prochaines Étapes pour le prochain Agent (Turnaround Re-Stocking)

1. **Intégration Ground Ops <-> Cabin Manager** :
   - Maintenant que l'avion "s'abime" et se "vide" d'un leg à l'autre, les services au sol (Catering, Cleaning, Lavatory, Water) doivent restaurer ces variables lorsque démarrés via l'UI Ground Ops.
   - Par exemple : Compléter le service de "Cleaning" doit remonter `CabinCleanliness` à 100.0. "Catering" doit assigner `CateringRations` au maximum prévu par SimBrief, etc.
   
2. **Tuning Financier & Pénalités** :
   - Régler la vitesse de consommation des ressources en vol (`baseDrainRate`). Si la saleté de la cabine descend sous les 40%, déclencher des plaintes passagers et réduire le taux global de Satisfaction.
   - Ajouter potentiellement des alarmes visuelles dans l'interface UI (badge orange/rouge) si l'eau ou la nourriture est critique.

3. **Validation & Test de Flow Complète** :
   - Réaliser un vol multi-leg via l'interface de "Debug Tools" : "Boarding" -> "Takeoff" -> "Cruise" -> "Arrived".
   - Observer les jauges au bas, engager les Ground Services pour nettoyer, et enchaîner manuellement un 2ème vol pour confirmer que le JSON persist globalise bien la suite des événements.

> [!NOTE] 
> Rappel de Règle de Conception : Ne pas utiliser le terme "Implementation" dans les documentations formelles. Privilégier le terme "Design_..." comme vu dans les dossiers actuels. Appliquez assidûment la Nomenclature Globale en rigueur !
