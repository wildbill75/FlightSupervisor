# Bilan des Communications Inter-Équipage (État Actuel)

Ce document rassemble et catégorise l'ensemble des dialogues et annonces audios (`SpeakAsCaptain`, `SpeakAsPurser`, `SpeakAsFO`) actuellement "codés en dur" ou dynamiques dans l'application Flight Supervisor (essentiellement dans `CabinManager.cs` et `FlightPhaseManager.cs`). 

Conformément à la requête, cette refonte vise à uniformiser le tout, à préparer le terrain pour vos futurs **Arbres de Dialogue**, et à consolider les compétences PNC.

---

## 1. First Officer (FO) ➡️ Captain
Généré via `_audioEngine?.SpeakAsFO(msg)`.
*   **Lights On at 10k (Climb)** : `"Passing 10,000, landing lights are still on."`
*   **Lights Off at 10k (Descent)** : `"Passing 10,000, landing lights are off."`

## 2. Captain ➡️ PNC (Cabin Crew)
Généré via `SpeakAsCaptain(string)` adressé à l'équipage.
*   **Top of Descent** : `"Cabin Crew, we are nearing top of descent."`
*   **Prepare for Landing** : `"Cabin Crew, prepare for landing."`
*   **Seated for Landing (Imminent)** : `"Cabin Crew, please be seated for landing."`
*   **Prepare for Takeoff** : `"Cabin Crew, prepare for takeoff."`
*   **Seated for Takeoff** : `"Cabin Crew, please be seated for takeoff."`
*   **Arm Doors** : `"Cabin Crew, arm doors and cross check."`

## 3. Captain ➡️ Public Announcements (Pax)
Généré via `SpeakAsCaptain(string)` via bouton PA ou automatique.
*   **Welcome / Briefing (Pre-flight)** : `"Ladies and gentlemen, this is your Captain speaking. First of all, welcome aboard our {airlineName} flight. We'll be operating an {aircraftType} today for our trip to {destName}. We are expecting a flight time of approximately {flightTime}. The weather en route looks {wxcText}, and we should be arriving {arrTimeStr}..."`
*   **Cruise (Top of Climb PA)** : `"Ladies and gentlemen, this is your Captain speaking. We have reached our cruising altitude and the flight is progressing {delayText}. We expect a smooth ride for the remainder of our journey..."`
*   **Descent / Approach PA** : `"Ladies and gentlemen, {greeting}, this is your Captain speaking. We have been cleared to land at {destName} and we expect to be on the ground in approximately {approachTimeMinutes} minutes {delayText}. The weather is {wxcText}..."`
*   **Apology for Delay PA** : `"Ladies and gentlemen, this is the captain speaking. I'd like to extend another apology for our earlier delay. We are doing everything we can to make up some time in the air..."`
*   *(Note: Il existe diverses annonces de turbulence et boutons manuels dans `CabinManager.cs` lignes 1526-1596 qui se calent sur le même modèle).*

## 4. PNC (Purser) ➡️ Captain (Intercom)
Généré via `SpeakAsPurser(string)` pour rapporter l'état de la cabine au pilote.
*   **Boarding Complete** : `"Boarding is complete Captain."`
*   **Cabin Report (Manual Request)** : Divers textes dynamiques décrivant l'humeur des passagers, générés dans `GetCabinReportSpeechEn()`. Exemples issus du code :
    *   *“Captain, cabin is secured for departure.”*
    *   *“The passengers are getting very frustrated and restless due to this long delay.”*
    *   *“Captain, everyone is freezing back here!”*
    *   *“It's getting quite warm in the cabin.”*
    *   *“People are mostly comfortable and calm.”*
    *   *“The seatbelt sign has been on for a while, people are getting antsy.”*

## 5. PNC (Purser) ➡️ Public Announcements (Pax)
Généré via `SpeakAsPurser(string)` adressé aux passagers.
*   **Safety Demo / Pushback** : `"Your attention please for a brief safety demonstration. Fasten your seatbelt by inserting the metal fitting into the buckle..."`
*   **Release Seatbelts (Cruise)** : `"Ladies and gentlemen, the captain has turned off the fasten seatbelt sign. You are now free to move about the cabin..."`
*   **Meal/Beverage Service** : `"Ladies and gentlemen, we will shortly be passing through the cabin to offer complimentary drinks and snacks..."`
*   **Arrival PA** : `"Welcome to {destName}. The local time is {arrTime}. For your safety and the safety of those around you, please remain seated with your seatbelt fastened until the captain has turned off the seatbelt sign..."`

---

## 6. Refonte des Compétences PNC (À Implémenter)

**Situation Actuelle :** 
Dans le fichier de design (`Design_Gameplay_Airline_Profiles.md`) et dans le code (`CrewProfile.cs`), nous utilisions encore des traces de compétences multiples (Efficacité, Empathie, Proactivité). La classe `CrewProfile` a commencé la transition avec une variable unique : `Efficiency`.

**Axe D'Amélioration (Objectif) :**
- Regrouper formellement les mécaniques sous **UNE seule compétence** clé (Nommée `Efficiency` ou `Crew Capability`).
- Cette compétence unique dictera la durée des actions (nettoyage, sécurité cabine, distribution repas) ET agira comme multiplicateur émotionnel (capacité du PNC à limiter la baisse de moral lors d'un long retard).
- Associer strictement cette valeur au Tier de la `Company_Profile` choisie (Elite = 90-95, Low Cost = 65-75, etc.).

---

## 7. Refonte UI/UX : Système de Déclenchement par Arborescence (Drill-Down)

Le système actuel de menus déroulants (drop-downs) pour le déclenchement manuels des requêtes et PAs sera supprimé. Nous introduisons un design ergonomique, propre et contextuel :

- **Boutons Uniques Conditionnels** : Seuls les boutons *pertinents* à l'instant T s'affichent. Si une annonce est hors contexte ou a déjà été jouée (ex: *Welcome PA*, *Safety Demo*), le bouton disparait.
- **Affichage Épuré** : L'UI affichera un maximum de 3 à 4 tuiles carrées côte à côte simultanément.
- **Catégories d'Adressage** : Les actions seront regroupées logiquement (`FlightDeck to PA` pour les annonces publiques, `FlightDeck to PNC` pour l'intercom cabine).
- **Logique de Forage (Drill-Down Trees)** pour les annonces complexes :
  - *Scénario Retard* : Si le système détecte un retard SOBT (> 5 mins), un bouton unique `[ Delay ]` apparait.
  - *Niveau 1* : Un clic sur `[ Delay ]` masque les autres boutons et affiche les causes globales (`ATC`, `Weather`, `Technical`, `Ground`).
  - *Niveau 2* : Un clic sur `Ground` affiche la cause spécifique (ex: `Cargo Loading`).
  - *Déclenchement* : Au clic sur `Cargo Loading`, l'annonce spécifique est déclenchée.
  - *Retour* : L'UI revient automatiquement au menu principal et le bouton de retard est masqué ou mis en cooldown.
- **Suivi des Phases** : Cette approche garantit que l'écran PNC/Comms ne soit jamais pollué par 25 options inutiles pendant la croisière.
