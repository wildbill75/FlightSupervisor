# [AMBRE] Design Gameplay: Captain Announcements & Passenger Reassurance

## 1. Overview
Captain announcements (PA) are the primary tool for managing passenger anxiety, especially during critical flight phases or crises.

## 2. Mechanics

### 🎤 Scheduled Announcements
1. **Welcome & Taxi**: EET and light weather briefing.
2. **Descent**: Destination temp and landing weather.

### 🚨 Crisis Announcements
1. **Go-Around**: Crucial for preventing a panic spike.
2. **Diversion**: Maintaining trust through transparency.

### ⚠️ The "Silence" Penalty
If a major event occurs (Severe Turbulence, Go-Around, Crisis) and the Captain remains silent, the **Anxiety Spike is doubled**.

---

## 3. Liste des Tickets (Structure du Design)

- [x] **TICKET 11 : Welcome PA Design**
  - Logic to pull EET and TAF summary (Bumpy/Smooth) into the PA message.
  - Trigger availability after pushback.

- [x] **TICKET 12 : Descent PA Design**
  - Trigger availability at TOC (Top of Descent). 
  - Dynamic string with Ground Temperature and Wind status.

- [x] **TICKET 13 : Crisis PA Logic**
  - Add logic for Go-Around and Diversion triggers in `FlightPhaseManager.cs`.

- [x] **TICKET 14 : Oral Briefing Integration**
  - (Linked to S15/S18) Feed the specific METAR/TAF highlights into the PA strings.

- [x] **TICKET 15 : Silence Penalty System**
  - Design of the 2-minute timer during crisis.
  - Doubling of anxiety spikes if silence exceeds 120s.

---

## 4. Spécifications de Design Technique

### [BACKEND] CabinManager.cs / FlightPhaseManager.cs
- **Welcome PA Trigger** : Disponibilité après le repoussage (GroundSpeed > 0 & Phase = TaxiOut).
- **Descent PA Trigger** : Disponibilité lors de l'amorce de la descente (V/S < -500 fpm au-dessus de 10k ft).
- **Turbulence PA Trigger** : Disponibilité en cas de détection de turbulences sévères ou si les consignes sont allumées manuellement.
- **Gestionnaire de Silence** : Chronomètre de 2 minutes (`_silenceTimer`) déclenché lors d'une crise (Turbulence Sévère, Go-Around, Panne).
- **Pénalité de Silence** : Si `_silenceTimer > 120s`, doubler le taux d'Anxiété `_anxietyRate`.

### [FRONTEND] app.js / Intercom
- **Boutons PA Dynamiques** : Affichage contextuel des boutons `Welcome`, `Descent` et `Crisis` selon la phase de vol.
- **Détails de Briefing** : Intégration des variables `EET`, `DestTemp`, et `ArrivalWeather` dans les messages affichés à l'écran.

REFONTE TOTALE DE LA LOGIQUE DES ANNONCES EN MVP

1. ACTEURS
  -CAPITAINE (Joueur)
  -PNC (PNc)
  -PASSAGERS (PNJ)

  1.1
    CAPITAINE
      -Peut parler aux passagers via l'intercom
      -Peut parler aux PNC via l'intercom
Le mode d'action est toujours déclenchable par un bouton sur l'intercom

  1.2
    Phases de vol et type de messages

    AUX PASSAGERS AVEC BOUTON

    AtGate, Pusback et TaxiOut : Welcome PA (avec EET et météo de départ ainsi que type d'avion, destination (Ville+nom de l'aéroport) Meteo brève (vent, visibilité, nuages, temp de l'arrivée. Informations méteo du vol avec précisions sur les turbulences et les vents en altitude. 
    
    Note : D'autres types messages seront définit ici par la suite

    AU PNC AVEC BOUTON
    -Rapport de cabine (toputes les phases de vol)
    -Prepare cabine for take off : phase taxiout
    -Prepare cabine for landing : toujours à partir de 10 000 pieds. Pas de restriction de phase à part Landing, Taxi In et arrived évidement
    -Seats for take off : Phase taxi out uniquement
    -Seats for landing : Phase approach uniquement

    PNC

    Jamis de bouton pour les PNC. Ils parlent automatiquement en fonction de la phase de vol et des consignes données par le capitaine. 
    
  Ladies and gentlemen, good [Morning/Afternoon/Evening] from the flightdeck this is your captain speaking, my name is [First Name] [Last Name] and in the name of [company] I would like to welcome you all on board this [Airline] [Aircraft] on our flight to [Destination]. Today flight time will be approximately [EET] and we're expecting a [enroute weather conditions] We're just finishing the last paper work and once completed we will start our pushback. We will get back to you with the latest weather informations from our destination airport when we start the approach. Thank you very much for being ou guests. Seat back, relax, and enjoy this flight with us.