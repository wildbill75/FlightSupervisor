# Audio Scripts: PNC to Passengers (English)

Ce document liste l'intégralité des annonces standards d'un PNC (Personnel Navigant Commercial) s'adressant aux passagers. 

## Configuration ElevenLabs Recommandée
- **Modèle :** Multilingual v2
- **ID Voix / Acteur (`female_1`) :** `Darine - Narrator` (Voix forcée avec accent français pour immersion maximale).
- **Astuce de découpage :** Copie-colle la ligne "Texte ElevenLabs" directement, les tirets `—` forceront des coupures nettes idéales pour l'outil `audio_slicer_wav.py`.

---

## 1. Boarding Complete & Welcome (Portes fermées)
**Contexte :** Fin de l'embarquement, annonce de bienvenue.
**Template Python :** `welcome_pa`

**Texte ElevenLabs :**
> Ladies and gentlemen, welcome aboard this — Air France — flight, bound for — Toulouse Blagnac. — Please ensure your hand luggage is safely stowed in the overhead bins or under the seat in front of you. — We ask that you fasten your seatbelt, and secure your tray table.

**Découpage attendu :**
1. `pa_welcome_intro` ("Ladies and gentlemen, welcome aboard this")
2. `airline_air_france` ("Air France")
3. `pa_bound_for` ("flight, bound for")
4. `dest_toulouse_blagnac` ("Toulouse Blagnac.")
5. `pa_welcome_luggage` ("Please ensure your hand luggage is safely stowed...")
6. `pa_welcome_seatbelts` ("We ask that you fasten your seatbelt...")

---

## 2. Safety Demonstration (Démonstration de sécurité)
**Contexte :** Roulage vers la piste.
**Variable :** AUCUNE (Annonce statique)
**Format attendu :** 1 seul fichier audio complet.
**Nom de fichier final :** `pa_safety_demo.mp3` (ou `.wav`)

**Texte ElevenLabs :**
> Ladies and gentlemen, may we have your attention please for the safety instructions. There are emergency exits located at the front, middle, and rear of the cabin. Please take a moment to locate your nearest exit, bearing in mind it may be behind you. In the event of a sudden loss of cabin pressure, oxygen masks will drop automatically. Please secure your own mask before assisting others.

---

## 3. Seatbelt Sign Off & Service (Croisière)
**Contexte :** Le commandant éteint le signal Fasten Seatbelts.
**Variable :** AUCUNE (Annonce statique)
**Format attendu :** 1 seul fichier audio complet.
**Nom de fichier final :** `pa_service_start.mp3` (ou `.wav`)

**Texte ElevenLabs :**
> The captain has turned off the fasten seatbelt sign. You are now free to move around the cabin. However, for your safety, we recommend keeping your seatbelt fastened while seated. In a few moments, we will begin our in-flight service.

---

## 4. Turbulence Warning (Turbulences)
**Contexte :** Le commandant rallume le signal Seatbelts en vol.
**Variable :** AUCUNE (Annonce statique)
**Format attendu :** 1 seul fichier audio complet.
**Nom de fichier final :** `pa_turbulence_warning.mp3` (ou `.wav`)

**Texte ElevenLabs :**
> Ladies and gentlemen, the captain has turned on the fasten seatbelt sign due to turbulence. Please return to your seats immediately and securely fasten your seatbelts. 

---

## 5. Top of Descent (Début de descente)
**Contexte :** L'avion entame son approche finale.
**Template Python :** `descent_pa`

**Texte ElevenLabs :**
> Ladies and gentlemen, we will shortly be beginning our descent into — Toulouse Blagnac. — Please return to your seats, fasten your seatbelts, and ensure your seat back and tray tables are in their full upright position.

**Découpage attendu :**
1. `pa_descent_intro` ("...beginning our descent into")
2. `dest_toulouse_blagnac` ("Toulouse Blagnac.")
3. `pa_descent_secure` ("Please return to your seats...")

---

## 6. Arrival & Post-Landing (Arrivée à la porte)
**Contexte :** Roulage vers la porte d'embarquement après atterrissage.
**Template Python :** `arrival_pa`

**Texte ElevenLabs :**
> Ladies and gentlemen, welcome to — Toulouse Blagnac. — The local time is — eight — a m. — For your safety, please remain seated with your seatbelt fastened until the aircraft has come to a complete stop at the gate and the seatbelt sign has been switched off. 

*(Note : Pour l'heure, générer "eight a m" permet de vérifier le rythme de l'heure. Plus tard, tu génèreras les heures séparément).*

**Découpage attendu :**
1. `pa_arrival_welcome` ("Ladies and gentlemen, welcome to")
2. `dest_toulouse_blagnac` ("Toulouse Blagnac.")
3. `pa_arrival_time_is` ("The local time is")
4. `time_8` ("eight")
5. `time_am` ("a m.")
6. `pa_arrival_remain_seated` ("For your safety, please remain seated...")

---

## Fichiers Additifs à Générer Plus Tard (Variables Pures)
Pour donner de la rejouabilité algorithmique, tu devras générer ces "banques de mots" en utilisant le découpeur :

**Banque des Heures (1 à 12 ou 1 à 24) :**
> One. — Two. — Three. — Four. — Five. — Six. — Seven. — Eight. — Nine. — Ten. — Eleven. — Twelve.
*(Templates : `time_1`, `time_2`, etc.)*

**Banque des Minutes (00, 05, 10... jusqu'à 55) :**
> O'clock. — Oh five. — Ten. — Fifteen. — Twenty. — Twenty-five. — Thirty. — Thirty-five. — Forty. — Forty-five. — Fifty. — Fifty-five.

**Banque des Périodes :**
> A M. — P M.

---

## 7. Delay Apology (Annonce Passagers)
**Contexte :** Le vol subit un retard au sol et le commandant demande une annonce pour rassurer les passagers.
**Variable :** AUCUNE
**Format attendu :** 1 seul fichier complet.
**Nom de fichier final :** `pa_delay_apology.mp3` (ou `.wav`)

**Texte ElevenLabs :**
> Ladies and gentlemen, apologies for the delay, we will be departing shortly.

---

# Audio Scripts: PNC to Cockpit (Intercom)

Ces annonces sont diffusées uniquement dans le poste de pilotage lorsque tu appelles la cabine. Puisque le backend `CabinManager.cs` concatène dynamiquement les phrases d'état et les phrases d'anxiété, tu dois générer les phrases suivantes séparément avec la même voix (`female_1`).

## 1. Rapports d'état de base (Base Status)
Générer la phrase suivante en continu, séparée par des tirets pour le découpeur, ou une par une.

**Texte ElevenLabs :**
> Cabin checks are complete. We are ready when you are to begin boarding. — We are waiting for boarding to finish, Captain. — Cabin is clear and quiet, Captain. — We're almost finished with the bins and oversized luggage, nearly ready. — Galley is being secured, and we're starting the final cabin check. — We are still tending to injured passengers. The mood is very somber. — We've just started preparing the service carts. — The meal service is in full swing. Everyone seems satisfied. — Service is complete, and the cabin is resting.

**Découpage attendu :**
1. `pnc_report_preboard` ("Cabin checks are complete. We are ready when you are to begin boarding.")
2. `pnc_report_boarding` ("We are waiting for boarding to finish, Captain.")
3. `pnc_report_idle` ("Cabin is clear and quiet, Captain.")
4. `pnc_report_taxi_out` ("We're almost finished with the bins and oversized luggage, nearly ready.")
5. `pnc_report_descent` ("Galley is being secured, and we're starting the final cabin check.")
6. `pnc_report_injured` ("We are still tending to injured passengers. The mood is very somber.")
7. `pnc_report_service_start` ("We've just started preparing the service carts.")
8. `pnc_report_service_mid` ("The meal service is in full swing. Everyone seems satisfied.")
9. `pnc_report_service_end` ("Service is complete, and the cabin is resting.")

---

## 2. Rapports Dynamiques d'Anxiété / Confort
Si la cabine n'est pas silencieuse ou est perturbée, le code ajoute dynamiquement l'une de ces phrases à la suite du rapport de base.

**Texte ElevenLabs :**
> The passengers are getting very frustrated and restless due to this long delay. — It's quite bumpy, and the cabin is feeling very tense and anxious. — People are very unhappy about not getting their meals. It's tough back here. — Note that some passengers are quite anxious about the flight. — Passengers are complaining about the general comfort level.

**Découpage attendu :**
1. `pnc_report_anxiety_delay` ("The passengers are getting very frustrated and restless due to this long delay.")
2. `pnc_report_anxiety_turb` ("It's quite bumpy, and the cabin is feeling very tense and anxious.")
3. `pnc_report_anxiety_food` ("People are very unhappy about not getting their meals. It's tough back here.")
4. `pnc_report_anxiety_gen` ("Note that some passengers are quite anxious about the flight.")
5. `pnc_report_comfort_low` ("Passengers are complaining about the general comfort level.")
