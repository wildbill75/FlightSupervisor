# 🎙️ Master Audio Scripts (Microsoft TTS)

> **Note :** Ce document annule et remplace les anciennes versions prévues pour l'enregistrement (ElevenLabs). Tous ces textes sont pensés pour être interprétés dynamiquement en C# via \`System.Speech\`.

---

## 📑 Sommaire
1. [👩‍✈️ Annonces du Flight Deck (Commandant de Bord)](#1-👩‍✈️-annonces-du-flight-deck-commandant-de-bord)
   - [Le Briefing de Bienvenue (PA Welcome)](#le-briefing-de-bienvenue-pa-welcome)
   - [L'ordre d'armement des portes](#lordre-darmement-des-portes)
   - [Les Commandes de Phase (Prep / Seats)](#les-commandes-de-phase-prep--seats)
   - [Les Avertissements (Turbulence / Delay)](#les-avertissements-turbulence--delay)
2. [👱‍♀️ Annonces de la Cabine (PNC / Chef de Cabine)](#2-👱‍♀️-annonces-de-la-cabine-pnc--chef-de-cabine)
   - [Le Briefing de Sécurité (Taxi Out - 3 Variantes)](#le-briefing-de-sécurité-taxi-out---3-variantes)
   - [Annonce de Début de Service](#annonce-de-début-de-service)
   - [Annonce de Descente (PNC)](#annonce-de-descente-pnc)
   - [Annonce d'Arrivée (Post-Landing)](#annonce-darrivée-post-landing)
3. [📞 Intercom (PNC vers Flight Deck)](#3-📞-intercom-pnc-vers-flight-deck)
   - [Rapports de base et Dynamiques (Anxiété/Confort)](#rapports-de-base-et-dynamiques)

---

## 1. 👩‍✈️ Annonces du Flight Deck (Commandant de Bord)
Ces annonces sont lues par une voix masculine forte (le joueur initie ces annonces en pressant les boutons du dashboard).

### Le Briefing de Bienvenue (PA Welcome)
C'est la pièce maîtresse du Commandant. Elle utilise la télémétrie du Briefing pour générer un texte ultra-immersif.
*Bouton : \`PA Welcome\` (Disponible en phase AtGate ou Boarding)*

**Gabarit C# :**
> "Ladies and gentlemen, this is your Captain speaking. First of all, welcome aboard our {Airline} flight. We'll be operating an {AircraftType} today for our trip to {Destination}. We are expecting a flight time of approximately {FlightDuration}. The weather en route looks {WeatherCondition}, and we should be arriving at {ArrivalTime} local time. For now, please settle in, and thank you for choosing to fly with us."

### L'ordre d'armement des portes
*Bouton : \`PA Arm Doors\` (Disponible juste avant le repoussage)*

**Gabarit C# :**
> "Cabin Crew, arm doors and cross check."

### Les Commandes de Phase (Prep / Seats)
Ces ordres formels servent à sécuriser la cabine avant les phases critiques.

**Prepare for Takeoff (Taxi Out) :**
> "Cabin Crew, prepare for takeoff."

**Seats for Takeoff (Juste avant d'entrer sur la piste) :**
> "Cabin Crew, please be seated for takeoff."

**Prepare for Landing (Passage sous les 10 000 pieds) :**
> "Cabin Crew, prepare for landing."

**Seats for Landing (Approche finale) :**
> "Cabin Crew, please be seated for landing."

### Les Avertissements (Turbulence / Delay)

**Turbulence Warning (Bouton `PA Turbulence`) :**
> "Ladies and gentlemen, the captain has turned on the fasten seatbelt sign due to some turbulence ahead. Please return to your seats immediately and ensure your seatbelts are securely fastened."

**Delay Apology (Bouton `PA Delay`) :**
*Conditions : Dès l'instant où l'embarquement est terminé (`IsBoarded == true`). N'est pas soumis à une phase de vol stricte (peut être utilisé AtGate).*
*UI : Le clic sur ce bouton devra ouvrir une sous-liste/menu permettant au joueur de choisir la raison du retard avant de lancer l'annonce.*
> "Ladies and gentlemen from the flight deck, I'd like to apologize for the delay. We are currently waiting for {DelayReason: ATC clearance / late connecting passengers / luggage loading / bad weather / technical checks} and expect to be moving in about {DelayMinutes} minutes. Thank you for your patience."

### L'Information Passagers Permanente (Cruise & Vol)
Un bouton permanent doit être disponible dès le décollage/croisière permettant au Commandant de s'adresser aux passagers librement selon la situation.
*Bouton : `PA Address Passengers` (Fixe, toujours présent).*
*UI : Comporte une liste déroulante d'options (comme pour les retards).*
**Option 1 - Statut de croisière standard :**
> "Ladies and gentlemen, this is your Captain speaking. We have reached our cruising altitude and the flight is progressing exactly on schedule/with a delay of {DelayMinutes} minutes/ahead of schedule of {DelayMinutes} minutes. We expect a smooth ride for the remainder of our journey. Sit back, relax, and enjoy the rest of the flight."

### L'Information Passagers Approach
> "Ladies and gentlemen, good morning/afternoon/evening, this is your Captain speaking. We have been cleared to land at {Destination} and we expect to be on the ground in approximately {ApproachTime} minutes on schedule/with a delay of {DelayMinutes} minutes/ahead of schedule of {DelayMinutes} minutes. The weather is {WeatherCondition}. Please ensure your seatbelts are securely fastened and your tray tables are stowed. Bye"

## 2. 👱‍♀️ Annonces de la Cabine (PNC / Chef de Cabine)
Ces annonces sont lues par une voix féminine douce. La plupart d'entre elles **se déclenchent automatiquement** selon la phase de vol.

### Le Briefing de Sécurité (Taxi Out - 3 Variantes)
**Déclenchement AUTO :** Dès le début de la phase \`TaxiOut\` (roulage), le PNC prend la parole de façon autonome (sans intervention du pilote). Comme demandé, voici 3 scripts sélectionnés aléatoirement pour varier les vols.

**Variante 1 (Standard & Complète) :**
> "Ladies and gentlemen, may we have your attention for the safety instructions. Please ensure your seatbelt is securely fastened, your seat back is upright, and your tray table is stowed. Smoking, including electronic cigarettes, is strictly prohibited on board. Emergency exits are located at the front, middle, and rear of the cabin. In the event of a sudden loss of cabin pressure, pull the oxygen mask towards you and place it over your nose and mouth before helping others. Thank you."

**Variante 2 (Courte & Directe) :**
> "Your attention please for a brief safety demonstration. Fasten your seatbelt by inserting the metal fitting into the buckle. Take a moment to locate your nearest emergency exit, keeping in mind it might be behind you. Smoking is not allowed at any time during this flight. All electronic devices must now be switched to airplane mode. We are currently preparing the cabin for departure."

**Variante 3 (Ton très strict - Période de pointe) :**
> "Ladies and gentlemen, Federal Aviation regulations require your compliance with all crew instructions and lighted signs. Please fasten your seatbelt and keep it fastened whenever the sign is illuminated. There are marked emergency exits along the cabin; identify your closest one now. Smoking and vaping are federal offenses in the lavatories and the cabin. Thank you for your full cooperation as we prepare for takeoff."

### Annonce de Début de Service
**Déclenchement AUTO :** En montée (passé 10 000 ft) ou au début de croisière, dès que le `CabinManager` valide le début du repas.
> "Ladies and gentlemen, we are pleased to inform you that our in-flight service is about to begin. We will be passing through the cabin shortly with complimentary beverages and snacks. Keep your seatbelts fastened even when the sign is off. Thank you."

### Annonce de Descente (PNC)
**Déclenchement AUTO :** Dès que l'avion commence sa descente physique (et que le Commandant a déclenché l'ordre "Prepare for Landing").
> "Ladies and gentlemen, we have begun our initial descent into {Destination}. Please return to your seats, fasten your seatbelts, and make sure your large electronic devices are stowed away."

### Annonce d'Arrivée (Post-Landing)
**Déclenchement AUTO :** Lorsque l'avion a atterri et passe en phase \`TaxiIn\` (sous 40 kts).
> "Welcome to {Destination}. The local time is {ArrivalTime}. For your safety and the safety of those around you, please remain seated with your seatbelt fastened until the captain has turned off the seatbelt sign at the gate. As you leave the aircraft, please check around your seat for any personal items. On behalf of the entire crew, thank you for flying with us today."

---

## 3. 📞 Intercom (PNC vers Flight Deck)
Ces phrases sont entendues en huis-clos dans le casque du pilote.

### Rapports Autonomes (Non Sollicités)
Certains messages sont déclenchés par le PNC de sa propre initiative (sans action du Commandant).

**Fin d'Embarquement (Boarding Complete) :**
**Déclenchement AUTO :** Dès que tous les passagers ont embarqué.
> "Boarding is complete Captain."

### Rapports Sollicités ("Request Cabin Report")
Ces phrases sont générées lorsque le Commandant presse le bouton "Request Cabin Report". Le système TTS va concaténer la phrase de "Statut" et la phrase "d'Humeur".

#### Rapports de base et Dynamiques

**Si en Embarquement :** 
> "We are still waiting for boarding to finish, Captain."

**Si Retard :**
> "The passengers are getting very frustrated and restless due to this long delay."

**Si Turbulences (Anciennes) :**
> "It's been quite bumpy, and the cabin is feeling very tense and anxious right now."

**Si Passagers Affamés (Service Skipped/Delayed) :**
> "People are very unhappy about not getting their meals yet. It's tough back here."

**Statut Standard (Tout va bien) :**
> "Cabin is clear and quiet, Captain. Everyone is relaxed."

**Si Ordre Inopportun (Ex: Demande de s'asseoir alors qu'ils préparent les chariots) :**
> "Captain, we haven't finished securing the galleys yet, we need a few more minutes!"
