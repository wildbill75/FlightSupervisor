# ✈️ Cruise Announcements (Mid-Flight PA) - Voice Action Scripts

Ce document contient l'architecture des "blocs Lego" audio requis pour générer un discours de croisière (Cruise PA) dynamique de la part du commandant de bord, qui annonce l'altitude, les vents et la météo en cours de route.

*Toutes les pistes doivent être enregistrées au format `.mp3` sous le dossier `wwwroot/assets/sounds/EN_FD_Rowan/CRUISE_PA/`.*

---

## 1. Introduction (Greeting)
Dossier: `CRUISE_PA/Cruise_Intro/`

Le préfixe définit le moment de la journée (basé sur l'heure locale actuelle de l'avion). La phrase se termine juste avant le chiffre de l'altitude.
* `Morn_01.mp3`: "Good morning ladies and gentlemen, this is your Captain from the flight deck. We have just reached our cruising altitude of..."
* `Aftn_01.mp3`: "Good afternoon ladies and gentlemen, this is your Captain speaking. We're now established at our cruising altitude of..."
* `Eve_01.mp3`: "Good evening ladies and gentlemen, from the flight deck, this is the Captain. We have reached our cruising altitude of..."

## 2. Altitude (Cruising Altitude)
Dossier: `CRUISE_PA/Altitude/`

Le backend de Flight Supervisor prendra l'altitude actuelle (ou l'altitude initiale de SimBrief) et la fera correspondre au millier le plus proche (entre `30000` et `43000`).
* `Alt_30000.mp3` : "...thirty thousand feet."
* `Alt_31000.mp3` : "...thirty-one thousand feet."
* `Alt_32000.mp3` : "...thirty-two thousand feet."
* `Alt_33000.mp3` : "...thirty-three thousand feet."
* `Alt_34000.mp3` : "...thirty-four thousand feet."
* `Alt_35000.mp3` : "...thirty-five thousand feet."
* `Alt_36000.mp3` : "...thirty-six thousand feet."
* `Alt_37000.mp3` : "...thirty-seven thousand feet."
* `Alt_38000.mp3` : "...thirty-eight thousand feet."
* `Alt_39000.mp3` : "...thirty-nine thousand feet."
* `Alt_40000.mp3` : "...forty thousand feet."
* `Alt_41000.mp3` : "...forty-one thousand feet."
* `Alt_42000.mp3` : "...forty-two thousand feet."
* `Alt_43000.mp3` : "...forty-three thousand feet."

## 3. Winds (Vents)
Dossier: `CRUISE_PA/Winds/`

La composante moyenne des vents (Average Wind Component, `avg_wind_comp`) de SimBrief est parsée. "P" (Plus) indique un Tailwind (vent arrière), "M" (Minus) un Headwind (vent de face). Le script assemble la nature du vent et sa vitesse approximative.

**Nature du Vent (Prefix):**
* `Tailwind_01.mp3` : "We are currently benefiting from a nice tailwind of about..."
* `Tailwind_02.mp3` : "We've got a strong tailwind pushing us along at about..."
* `Headwind_01.mp3` : "However, we are currently pushing into a headwind of roughly..."
* `Headwind_02.mp3` : "We're experiencing some headwind up here at around..."
* `Calm_01.mp3` : "The winds aloft are relatively calm today." *(Fin de section vent si le vent < 20kts)*

**Vitesses de Vent (Suffix):**
(Seulement lu si la phrase se termine par "about...", arrondi à la vingtaine près)
* `Spd_20.mp3` : "...twenty knots."
* `Spd_40.mp3` : "...forty knots."
* `Spd_60.mp3` : "...sixty knots."
* `Spd_80.mp3` : "...eighty knots."
* `Spd_100.mp3` : "...one hundred knots."
* `Spd_120.mp3` : "...one hundred and twenty knots."
* `Spd_140.mp3` : "...over one hundred and forty knots."

## 4. En-Route Weather (Météo Trajet / Turbulences)
Dossier: `CRUISE_PA/EnRoute/`

Basé sur la lecture des METARs en route via SimBrief, ou basé de façon dynamique sur les conditions du moment (ex: orages sur la route).
* `Smooth_01.mp3` : "Looking ahead at the weather radar, the route is completely clear, so it should be a very smooth ride all the way."
* `Bumpy_01.mp3` : "Looking ahead, we are seeing a few weather systems along our route, so we might experience a few bumps."
* `Stormy_01.mp3` : "We do have some significant weather to navigate around coming up, so I will be keeping the seatbelt sign on."

## 5. Arrival Transition
Dossier: `CRUISE_PA/Arrival/`
* `ArrTrans_01.mp3`: "As for our destination in... "

**NOTE :** A partir de ce "As for our destination in...", le backend appellera à nouveau le même système **Destination Name** et **Weather Synthesis** que nous avons déjà implémentés pour le Briefing de départ, ce qui va réutiliser les mêmes blocs météorologiques (Température, Ciel, etc.).

## 6. Outro (Conclusion)
Dossier: `CRUISE_PA/Cruise_Outro/`
* `Outro_01.mp3` : "I will get back to you with an updated ETA before our descent. Until then, sit back, relax, and enjoy the rest of the flight."
* `Outro_02.mp3` : "We will talk to you again as we get closer to our destination. Thank you for flying with us, and have a great flight."
