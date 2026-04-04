# Design Technique : Audio Engine (PNC & Passagers)

## Architecture Globale
Le système audio de Flight Supervisor permet de simuler de manière ultra-réaliste les annonces cabines (PNC) et les sons ambiants via l'API Web Audio du navigateur (WebView2).

### 1. Le Générateur d'Annonces (Backend C#)
Le système repose sur `CabinManager.cs` qui intercepte les événements de vol (Top of Descent, Boarding Complete, Urgences) et déclenche l'événement `OnCrewMessage`.
- La méthode `FormatAudioSequence` est chargée de convertir une liste d'IDs (ex: `pnc_report_preboard`) en chemins de fichiers concrets.
- **Règle de nommage :** Le backend génère des chemins sous la forme `airlines/{airline_id}/pnc/en/{voice_id}/{nom_du_fichier}.mp3`.
- **Note Importante :** Le C# ajoute **déjà** l'extension `.mp3` ou `.wav` à la fin de la chaîne envoyée au Frontend.

### 2. Le Moteur de Lecture (Frontend JS - `AudioQueue`)
Défini dans `app.js`, la classe `AudioQueue` (instanciée sous `window.audioEngine`) gère la file d'attente (queue) des audios à lire, garantissant qu'aucun son ne se chevauche.

#### Effets Appliqués (Web Audio API)
Pour procurer une immersion "Système d'Interphone" (PA System), la sortie audio navigue à travers plusieurs noeuds (Nodes) :
1. **Source :** Balise `<audio>` HTML5 standard.
2. **Filtre Bandpass (1200 Hz) :** Coupe les basses profondes et les aigus extrêmes pour imiter la plage de fréquences limitée d'un combiné interphone.
3. **Filtre Highshelf (3500 Hz, -12dB) :** Assourdit volontairement le son pour imiter les haut-parleurs bon marché des cabines d'avion.
4. **Distorsion (Waveshaper) :** Ajoute une saturation et de légers grésillements (crackle) typiques lors des prises de parole.
5. **Destination :** Sortie audio par défaut de Windows.

---

## Registre des Bugs Connus et Résolus

### Bug #1 : Le son du PNC ne joue pas (Erreur 404 Silencieuse)
**Symptôme :** Lorsqu'une annonce est déclenchée (ex: "Cabin checks are complete..."), l'icône indique que le son est joué, mais la cabine reste muette.
**Cause (Double Extension) :**
- `CabinManager.cs` (C#) a envoyé : `.../pnc_report_preboard.mp3`
- `AudioQueue.playNext()` (JS, ligne ~90) l'a concaténé en y ajoutant en dur un `.mp3` : `this.audioElement.src = "assets/sounds/" + filename + ".mp3";`
- Le navigateur a donc tenté de chercher le fichier `.../pnc_report_preboard.mp3.mp3` qui n'existait pas sur le disque, déclenchant le silencieux `console.warn("[AudioEngine] Fichier introuvable...")`.

**Résolution Attendue :** 
Modifier `app.js` pour retirer le ` + ".mp3"` manuel de la concaténation, laissant ainsi le backend C# dicter l'extension exacte (idéal pour supporter d'éventuels `.wav` plus tard).
