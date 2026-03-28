# Technical Design: Audio Stringing System

## 1. Overview
L'objectif est d'intégrer des annonces vocales dynamiques pour les échanges avec l'équipage (PNC) et les annonces du Commandant de bord (PA), sans compromettre la flexibilité narrative de *Flight Supervisor*. La technique retenue est l'**Audio Stringing** (ou Concaténation Audio), consistant à lire séquentiellement des fichiers sonores pré-générés par IA, assurant ainsi une flexibilité à 100% avec zéro frais d'API à l'usage.

## 2. Architecture des Dossiers (Audio Bank)
Tous les fichiers audios (.mp3 ou .wav) générés seront stockés dans un dossier local (ex: `wwwroot/audio/`).
- `/audio/pnc/` : Retours d'intercom, statut cabine.
- `/audio/pa/` : Annonces Commandant.
- `/audio/sfx/` : Sons divers (ding, chimes).
- `/audio/vars/` : Fichiers réutilisables (Nombres de 1 à 60, Hectopascals, Noms des aéroports pré-générés, Températures).

*(Exemple de génération pour éviter le syndrôme "Frankenstein" : Lors de la génération dans ElevenLabs, faire dire à l'IA "Alpha, quarante-deux, Bravo" et isoler le milieu pour garantir une intonation plate).*

## 3. Payload IPC (Backend -> Frontend)
Le C# ne se contente plus d'envoyer un texte statique, il accompagne le texte généré d'une séquence ordonnée d'identifiants audio à jouer côté UI.

**Nouveau DTO `PncMessagePayload` :**
```json
{ 
  "type": "pncMessage", 
  "importance": "info", 
  "content": { 
    "en": "We are heavily delayed by 42 minutes.", 
    "fr": "Nous avons un retard de 42 minutes." 
  },
  "audioSequence": ["pnc_delay_intro", "num_42", "pnc_minutes", "pnc_frustration"],
  "cooldown": 120
}
```

## 4. Logique de File d'attente (Frontend JavaScript)
La lecture d'une séquence doit être continue, robuste et ne pas se superposer. Une classe `AudioQueue` sera implémentée dans `app.js` pour gérer sa propre boucle de lecture en s'inscrivant sur l'évènement `onended` de l'API HTML5 `<audio>`.

---

## 5. Liste Explicite des Tickets d'Implémentation

- [ ] **TICKET 50 : Ajouter l'AudioQueue dans le Javascript (Frontend)**
  - Coder la fonction `playAudioSequence(array)` en JS qui accepte une liste de noms de fichiers.
  - Implémenter l'enchaînement automatique via l'event `onended`.
  - Intégrer la lecture à la réception des payloads `pncMessage` et `paMessage`.

- [ ] **TICKET 51 : Modifier le Payload IPC (Backend)**
  - Ajouter la propriété `string[] AudioSequence` à la classe de transmission des messages (si nécessaire, créer un nouveau DTO ou utiliser un dictionnaire existant).

- [ ] **TICKET 52 : Traduction Texte -> Audio dans `CabinManager` (Backend)**
  - Dans `RequestCabinReport()`, en fonction des mêmes branches conditionnelles que le texte, générer le `string[]` d'IDs audios associés.
  - *Ex: Si Priority 2 (Boarding) -> `["pnc_boarding_wait"]`.*
  - *Ex: Si Retard > 0 -> `{"pnc_delay_intro", $"num_{_currentDelayMinutes}", "pnc_minutes"}`.*

- [ ] **TICKET 53 : Implémenter l'interface des Annonces PA (Backend & Frontend)**
  - Ajouter l'UI pour déclencher "Welcome Aboard", "Turbulence Warning", "Descent Preparation".
  - Refléter le statut des annonces dans le système de Penalités et Bonus (SuperScore).

## 6. Flow Logique & Exemples de Concaténation

Voici deux exemples concrets illustrant comment assembler des segments audios génériques avec des variables dynamiques (villes, nombres) pour construire une phrase fluide.

### Exemple 1 : Annonce de bienvenue du Commandant (PA Welcome Aboard)

Le commandant formule son annonce d'accueil en incluant la compagnie et la destination complète.

**Phrase complète cible :**
> *"Bienvenue à bord de ce vol Air France, à destination de Toulouse Blagnac."*

**Découpage logique (Fichiers MP3/WAV) :**
1. `pa_welcome_intro` -> *"Bienvenue à bord de ce vol "*
2. `airline_air_france` -> *"Air France"*
3. `pa_bound_for` -> *", à destination de "*
4. `dest_toulouse_blagnac` -> *"Toulouse Blagnac."*

**Tableau `AudioSequence` transmis au Javascript :**
```json
[
  "pa_welcome_intro", 
  "airline_air_france", 
  "pa_bound_for", 
  "dest_toulouse_blagnac"
]
```

### Exemple 2 : Retour dynamique du PNC (Retard actif)

Le commandant interroge la cabine via l'intercom alors que le `_currentDelayMinutes` est de 42.

**Phrase complète cible :**
> *"Tout se passait bien, mais avec ce retard de 42 minutes, les passagers s'impatientent sérieusement."*

**Découpage logique (Fichiers MP3/WAV) :**
1. `pnc_delay_intro` -> *"Tout se passait bien, mais avec ce retard de "*
2. `num_42` -> *"42"* (généré avec une intonation plate)
3. `pnc_minutes_frustration` -> *" minutes, les passagers s'impatientent sérieusement."*

**Tableau `AudioSequence` transmis au Javascript :**
```json
[
  "pnc_delay_intro", 
  "num_42", 
  "pnc_minutes_frustration"
]
```
