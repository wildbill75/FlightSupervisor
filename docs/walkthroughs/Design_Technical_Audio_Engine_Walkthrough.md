# Walkthrough : Sound Engine (Audio Stringing)

Ce document valide l'implémentation réussie de la **Phase 7** du système de bord (Tickets 50 à 53), permettant de lire des séquences audio dynamiques sans modifier le code Frontend.

## 1. Ce qui a été accompli

- **Moteur Audio Javascript (`AudioQueue`)** 
  - Une nouvelle classe `AudioQueue` a été créée dans `app.js`.
  - Elle prend en entrée un tableau d'identifiants audios (ex: `["pa_welcome", "airline_name", "pa_welcome_end"]`).
  - Elle construit automatiquement le chemin `assets/sounds/[id].mp3` et les lit à la suite (en écoutant l'événement `ended`).
  - Tolérance aux fautes : si un MP3 est manquant (Erreur 404), le moteur l'ignore et passe instantanément au suivant (événement `error`), sans geler la boucle de jeu.
  
- **Intégration IPC (C# -> JS)**
  - Le système d'intercom et d'annonces PA envoie maintenant une propriété optionnelle `audioSequence` à travers les `cabinLog`.
  - Dès réception par `app.js`, la méthode `window.audioEngine.playSequence(payload.audioSequence)` est déclenchée.

- **Traduction des États C# (PNC et Pilote)**
  - **PNC (`CabinManager.cs:RequestCabinReport`)** : La météo de la cabine, le retard (`_currentDelayMinutes`), et l'étape d'embarquement dictent une liste de MP3. Par exemple, un retard génère `["pnc_delay_intro", "num_42", "pnc_minutes_frustration"]`.
  - **Pilote (`CabinManager.cs:AnnounceToCabin, AnnounceWelcome, AnnounceDescent`)** : Les fonctions d'annonce cabine injectent désormais les tags `pa_welcome`, `pa_turbulence`, `pa_descent`, `pa_delay_apology`.

## 2. Validation Technique
- La logique a été compilée avec succès (`Release`).
- L'interface d'annonces (boutons HTML) interagit correctement avec le backend via WebMessageReceived.
- L'architecture est désormais prête à accueillir l'ajout de vrais fichiers MP3/WAV par le Sound Designer, simplement en les glissant dans le dossier `wwwroot/assets/sounds/` avec les noms correspondants.

## 3. Prochaines Étapes Suggérées
1. **Intégration des MP3** : Ajouter physiquement les fichiers sons correspondants aux identifiants.
2. **Tester en vol** : Valider l'enchaînement temporel naturel lors d'un test MSFS.
