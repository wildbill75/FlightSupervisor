# Design Gameplay : Options et Paramètres Audio (PNC)

## 1. Vue d'ensemble
L'onglet **Options / Settings** de l'application doit intégrer une nouvelle rubrique dédiée au paramétrage du son, et plus particulièrement aux annonces de l'équipage de cabine (PNC / Cabin Crew). Ces options permettront au joueur de personnaliser l'identité vocale de son équipage, ainsi que le niveau de confort linguistique et de réalisme des annonces.

## 2. Rubrique requise : "Choix Vocaux PNC"

Afin de prévoir l'ajout d'autres voix off à l'avenir, les options doivent inclure un sélecteur de voix pour le Chef(fe) de Cabine.

- [ ] **TICKET 36 : Sélecteur de Voix PNC**
  - **Type de composant :** Sélecteur déroulant (Dropdown) ou Boutons sélecteurs (Radio style cartes).
  - **Voix disponibles (actuellement) :** 
    - `Darine` (*Voix par défaut, Chef de cabine féminine*)
  - **Spécifications :** Le choix de la voix modifie le chemin/préfixe utilisé par le `CabinManager` pour faire appel aux fichiers MP3. (ex: `./audio/Cabin/Darine/Welcome.mp3`).

## 3. Rubrique requise : "Réalisme Linguistique des Annonces"

Le système audio reproduit la dynamique linguistique des compagnies réelles en fonction de leur nationalité (Ex: Air France). Cependant, cela peut gêner un joueur qui ne comprend pas la langue maternelle de la compagnie simulée lorsqu'il joue en anglais. 

- [ ] **TICKET 37 : Option de simplification de la langue (Realistic vs All-English)**
  - Ajouter un paramètre de type "Bascule" (*Toggle*) ou boutons radio pour choisir comment sont diffusées les annonces.
  - **Mode Actif (Réaliste / Native)** : 
    - *Réseau Cabine (PA)* : Le PNC s'adresse aux passagers en **Double Annonce** (Langue Maternelle de la Compagnie ➝ Puis en Anglais).
    - *Intercom Copit (À l'adresse du CDB)* : Le PNC ne s'adresse au Commandant de Bord que dans **sa langue maternelle** (ex: En Français pour Air France, en Danois pour SAS).
  - **Mode Inactif (Simplifié / All-English)** : 
    - *Réseau Cabine (PA)* : Le PNC s'adresse aux passagers en **Anglais uniquement** (Une seule annonce).
    - *Intercom (À l'adresse du CDB)* : Le PNC s'adresse au Commandant de Bord en **Anglais uniquement**, ignorant la nationalité de la compagnie.

## 4. Intégration dans le Panel des Options
Ces nouvelles rubriques doivent s'intégrer harmonieusement dans l'onglet `Options`. Elles doivent conserver l'esthétique "Glassmorphism" actuelle, par exemple sous forme de deux *Cards* grisées au même titre que les autres paramètres de télémétrie.
