# Arbres de Communication (ASCII / Représentation Visuelle)

Ce document liste l'arborescence des actions et annonces existantes dans le code actuel. Servez-vous de ce squelette pour visualiser ce qui existe et m'indiquer où vous souhaitez "brancher" vos nouvelles arborescences (Drill-Downs) et quels textes remplacer.

```text
=========================================================
 1. PUBLIC ANNOUNCEMENTS (PA) : CAPTAIN ➡️ PASSAGERS
=========================================================

[PRE-FLIGHT]
 └── "Welcome / Briefing"
      ├── Condition : Joué manuellement ou auto après embarquement
      └── Texte : "Ladies and gentlemen, this is your Captain speaking. First of all, welcome aboard our {airlineName} flight. We'll be operating an {aircraftType} today for our trip to {destName}. We are expecting a flight time of approximately {flightTime}. The weather en route looks {wxcText}, and we should be arriving {arrTimeStr}. For now, please settle in, and thank you for choosing to fly with us."

[CRUISE]
 ├── "Top of Climb PA"
 │    ├── Condition : Altitude de croisière atteinte
 │    └── Texte : "Ladies and gentlemen, this is your Captain speaking. We have reached our cruising altitude and the flight is progressing {delayText}. We expect a smooth ride for the remainder of our journey. Sit back, relax, and enjoy the rest of the flight."
 │
 └── "Apology for Delay PA"
      ├── Condition : Bouton manuel d'excuses en vol
      └── Texte : "Ladies and gentlemen, this is the captain speaking. I'd like to extend another apology for our earlier delay. We are doing everything we can to make up some time in the air. Thank you for your continued patience."

[DESCENT / APPROACH]
 └── "Approach Briefing"
      ├── Condition : Clearance d'approche / Descente bien entamée
      └── Texte : "Ladies and gentlemen, {greeting}, this is your Captain speaking. We have been cleared to land at {destName} and we expect to be on the ground in approximately {approachTimeMinutes} minutes {delayText}. The weather is {wxcText}. Please ensure your seatbelts are securely fastened and your tray tables are stowed. Bye."


=========================================================
 2. INTERCOM CABINE : CAPTAIN ➡️ PNC (Cabin Crew)
=========================================================

[DEPARTURE]
 ├── "Doors"
 │    ├── Condition : Début du repoussage
 │    └── Texte : "Cabin Crew, arm doors and cross check."
 │
 ├── "Takeoff Prep"
 │    ├── Condition : Roulage
 │    └── Texte : "Cabin Crew, prepare for takeoff."
 │
 └── "Takeoff Imminent"
      ├── Condition : Entrée sur la piste
      └── Texte : "Cabin Crew, please be seated for takeoff."

[ARRIVAL]
 ├── "Top of Descent"
 │    ├── Condition : Descente amorcée
 │    └── Texte : "Cabin Crew, we are nearing top of descent."
 │
 ├── "Landing Prep"
 │    ├── Condition : En approche (env. 10,000ft)
 │    └── Texte : "Cabin Crew, prepare for landing."
 │
 └── "Landing Imminent"
      ├── Condition : Finale (env. 2,000ft)
      └── Texte : "Cabin Crew, please be seated for landing."


=========================================================
 3. ANNONCES CABINE : PNC (Purser) ➡️ PASSAGERS
=========================================================

[DEPARTURE]
 └── "Safety Demonstration"
      ├── Condition : Pushback
      └── Texte : "Your attention please for a brief safety demonstration. Fasten your seatbelt by inserting the metal fitting into the buckle..."

[CRUISE]
 ├── "Release Seatbelts"
 │    ├── Condition : Extinction du signal Seatbelt au-dessus de 10,000ft
 │    └── Texte : "Ladies and gentlemen, the captain has turned off the fasten seatbelt sign. You are now free to move about the cabin. However, we do recommend keeping your seatbelt fastened while seated, in case we experience any unexpected turbulence."
 │
 └── "Service Announcement"
      ├── Condition : Début du Meal Service (Croisière)
      └── Texte : "Ladies and gentlemen, we will shortly be passing through the cabin to offer complimentary drinks and snacks..."

[ARRIVAL]
 └── "Welcome to Destination"
      ├── Condition : Arrivée à la porte
      └── Texte : "Welcome to {destName}. The local time is {arrTime}. For your safety and the safety of those around you, please remain seated with your seatbelt fastened until the captain has turned off the seatbelt sign..."


=========================================================
 4. RAPPORTS INTERCOM : PNC ➡️ CAPTAIN
=========================================================
Note : Ces messages sont dynamiques et dépendent du moral.

[NORMAL STATUES]
 ├── "Boarding is complete Captain."
 ├── "Captain, cabin is secured for departure."
 └── "People are mostly comfortable and calm."

[STRESS / DELAYS]
 ├── "The passengers are getting very frustrated and restless due to this long delay."
 └── "The seatbelt sign has been on for a while, people are getting antsy."

[ENVIRONMENT]
 ├── "Captain, everyone is freezing back here!"
 └── "It's getting quite warm in the cabin."


=========================================================
 5. NOTIFICATIONS FO : FIRST OFFICER ➡️ CAPTAIN
=========================================================

[CHECKLISTS / LIGHTS]
 ├── "Passing 10,000, landing lights are still on." (Oubli en montée)
 └── "Passing 10,000, landing lights are off." (Rappel en descente)
```
