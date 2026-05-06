# Design Technical VoiceStudio Macros : Dynamic Phrase Builder

L'objectif de cette implémentation est d'éliminer l'effet "robotique" des annonces séquentielles (Audio Stringing) en générant des phrases complètes et fluides via ElevenLabs. Pour ce faire, nous allons transformer le système de Macro de Voice Studio pour supporter des **Variables Dynamiques** dans une même phrase.

## User Review Required

> [!IMPORTANT]  
> **Explosion Combinatoire :** Si vous croisez 20 compagnies aériennes et 50 aéroports de destination, le système va générer **1000 fichiers audios** en une seule macro (ce qui va consommer pas mal de crédits API ElevenLabs). Êtes-vous d'accord pour que le système prévienne ou demande confirmation avant de lancer des macros qui génèrent plus de X fichiers (ex: 50) ?

> [!NOTE]  
> **Nommage des Fichiers :** Pour que le C# (`CabinManager.cs`) puisse les retrouver facilement sans avoir à faire des listes manuelles complexes, il faut une convention claire.
> Par exemple, si vous tapez une macro pour un "Welcome", comment nommer la centaine de fichiers produits ?
> **Suggestion :** L'interface vous demandera un "Préfixe" (ex: `pnc_welcome_`). Le fichier généré sera alors `pnc_welcome_AFR_LFPG.wav`.

## Proposed Changes

### Voice Studio Frontend (`app.js` & `index.html`)

1. **Nouveau mode dans la Modale Macro : "Phrase Dynamique (Smart Phrase)"**
   - Remplacer le simple sélecteur de "Packs" par un champ de texte avancé.
   - L'utilisateur pourra taper une phrase avec des variables entre accolades, par exemple : 
     `Bonjour et bienvenue sur ce vol {airline} à destination de {dest}.`
   - Le système détectera automatiquement la présence de balises connues (ex: `{airline}`, `{dest}`, `{dep}`).

2. **Génération des Permutations (JavaScript)**
   - Avant d'envoyer au serveur, le JS calculera toutes les combinaisons possibles en se basant sur les listes (Aéroports, Airlines) définies dans la zone de droite (Custom Data).
   - Il affichera un résumé intelligent : *"Cette macro va générer 150 fichiers. Appuyez sur Lancer pour continuer."*

3. **Nommage Dynamique**
   - Ajout d'un champ "Préfixe du fichier" (ex: `pnc_welcome`).
   - Le JS générera les requêtes vers l'API Python avec le nom de fichier final, ex: `pnc_welcome_AFR_LFPG.wav`.

### Voice Studio Backend (`app.py`)

- Le backend Python n'a quasiment pas besoin de changer ! Il recevra simplement des requêtes classiques avec le texte final déjà substitué (ex: *"Bonjour et bienvenue sur ce vol Air France à destination de Toulouse"*) et le nom de fichier cible généré par le frontend.
- C'est l'approche la plus sûre et la plus évolutive.

### L'Impact sur le C# (`CabinManager.cs`)

- Si l'on passe sur un système où Voice Studio génère des phrases complètes, il faudra ensuite que je modifie `CabinManager.cs` pour qu'il ne tente plus de recoller les morceaux (`pa_welcome_intro` + `airline` + `dest`) mais qu'il cherche directement le fichier complet (`pnc_welcome_AFR_LFPG.wav`), et s'il ne le trouve pas, utiliser une version raccourcie générique (`pnc_welcome_AFR.wav`).
