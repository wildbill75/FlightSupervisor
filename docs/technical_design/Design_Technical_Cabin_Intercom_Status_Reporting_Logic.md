# Technical Design: Cabin Intercom & Status Reporting Logic

## 1. Overview
The technical foundation of the Cabin Intercom system relies on a contextual string generator within the `CabinManager` service. It evaluates the current state of the cabin (Anxiety, Comfort, Service, Security) and selects a narrative fragment to transmit to the frontend.

## 2. Core Logic

### `CabinManager.GenerateReport()`
A new public method that returns a `LocalizedReport` object.
```csharp
public class LocalReport {
    public string English { get; set; }
    public string French { get; set; }
}
```

The logic follows a priority-based selection:
1. **Critical/Emergency**: If `isCrisisActive`, the report focuses on the crisis status.
2. **Boarding Phase**: If the cabin is currently boarding passengers, the report bypasses all mood metrics and simply states: "We are waiting for boarding to finish".
3. **Post-Turbulence**: If `Severe` turbulence occurred in the last 5 minutes, report on "Injuries & Cabin state".
4. **Transition Phase**: If in `TaxiOut` or `Descent`, report on `SecuringProgress`.
5. **Cruise (Normal)**: Selection based on `InFlightServiceProgress`.
6. **Atmosphere**: If `PassengerAnxiety > 50` or `ComfortLevel < 40`, append a note about the "Mood" (unless during boarding).

### Cooldown Management
- Property: `DateTime _lastReportRequest`.
- Logic: Reject requests if `(DateTime.Now - _lastReportRequest).TotalMinutes < 2`.
- Feedback: Send a specific `playSound` (intercom_busy) if rejected.

### Strategic Penalty (Temporal Hinderance)
When `SecuringProgress < 100` and a report is requested:
- Action: `SecuringRate` is halved for the next 10 seconds.
- Rationale: The PNC is distracted by the flight deck communication.

## 3. IPC Message Structure
Request (Frontend -> Backend):
```json
{ "type": "intercomQuery", "action": "cabin_report" }
```

Response (Backend -> Frontend):
```json
{ 
  "type": "pncMessage", 
  "importance": "info", 
  "content": { "en": "...", "fr": "..." },
  "cooldown": 120
}
```

## 4. UI Integration (`app.js`)
- **Button Element**: `#btn-cabin-report`.
- **Display**: Use the existing `renderPncMessage` function but with a specific "Intercom" CSS style (e.g., italicized text).

## 5. Next Steps
1. Create the string dictionary in `CabinManager`.
2. Add the `RequestReport` handler to `MainWindow.xaml.cs`.
3. Add the button to the Intercom panel in `index.html`.

## 6. Liste Explicite des Tickets d'Implémentation

- [x] **TICKET 41 : Backend - Logique de Génération (CabinManager)**
  - **Création du type de retour** : Définir une structure ou un tuple pour renvoyer le texte Anglais/Français.
  - **Logique de sélection** : 
    - Gérer l'état de crise (priorité 1).
    - Gérer l'état d'embarquement (priorité 2) : Si en cours d'embarquement -> "Nous attendons la fin de l'embarquement, Commandant."
    - Gérer les turbulences récentes (5 min).
    - Gérer les phases de préparation cabine (`SecuringProgress < 100`). **Pénalité** : Diviser par 2 le `_currentSecuringRate` pendant 10 secondes car le PNC est dérangé.
    - Gérer les humeurs (Anxiété > 50, Confort < 40, Retard SOBT passé) si hors priorités majeures.
    - Retour normal basé sur l'état de progression du service (`InFlightServiceProgress`).
  - **Cooldown** : Maintenir un registre `DateTime _lastReportRequest` pour refuser les demandes trop fréquentes (moins de 2 minutes d'intervalle).

- [x] **TICKET 42 : Backend - Routage IPC (MainWindow.xaml.cs)**
  - Capter l'action `"action": "cabin_report"` venant de l'interface WebView2.
  - Appeler `CabinManager.GenerateReport()`.
  - Renvoyer un payload JSON de type `"pncMessage"` avec la classe `importance: "info"`, le `content` (en/fr) et le `cooldown`.

- [x] **TICKET 43 : Frontend - UI & JS (app.js / index.html)**
  - Ajouter un bouton dans la section Intercom du HUD : `<button id="btn-cabin-report">Ask Cabin Status</button>`.
  - Mettre en place la logique Javascript pour empêcher le clic pendant la période de cooldown.
  - Afficher le texte renvoyé par le backend dans la console historique existante (via la méthode existante de rendu des messages d'équipage, avec un style "intercom / italic" si possible).
  
- [ ] **TICKET 44 : Ajout de la réponse Embarquement (C#)**
  - Intégrer la Priorité 2 (attente de l'embarquement) directement dans le code de `RequestCabinReport` (`CabinManager.cs`).
