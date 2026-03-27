# Technical Design: Restructuration totale en WebView2

## 1. Synthèse du Concept
La **Restructuration totale en WebView2** est une refonte architecturale majeure visant à séparer la logique métier agressive (le backend) de la présentation visuelle (le frontend). Au lieu de compter sur les contrôles WPF natifs, difficiles à styliser modernement et sujets aux pertes de performance lors d'un rafraîchissement intensif, l'interface entière repose désormais sur un moteur de rendu web embarqué (Microsoft Edge WebView2).

Cette approche hybride permet de combiner deux mondes avec leurs forces respectives : 
- **C# / .NET** gère parfaitement la communication bas niveau très gourmande avec Microsoft Flight Simulator (SimConnect), les algorithmes complexes, le multi-threading et l'accès au système de fichiers.
- **HTML / CSS (Tailwind) / Javascript** gère la fluidité des animations, la haute densité des informations, le responsive design et l'esthétique "Glassmorphism" premium requise pour une immersion totale.

## 2. Architecture et Mécaniques Actives

### A. Le Pont de Communication (The Bridge)
La pièce maîtresse de cette restructuration est l'échange de données asynchrone bidirectionnel en JSON, qui garantit que l'UI ne gèlera jamais le jeu, et que le jeu ne gèlera jamais l'UI.
- **C# vers Javascript (`CoreWebView2.PostWebMessageAsJson`) :** Le backend pousse des "Charges Utiles" (Payloads) catégorisées (`phaseUpdate`, `scoreUpdate`, `flightData`, `groundOpsProgress`). Le frontend Javascript agit comme un simple récepteur passif qui met à jour le DOM de manière chirurgicale.
- **Javascript vers C# (`window.chrome.webview.postMessage`) :** Les interactions utilisateur (cliquer sur "Fetch", demander l'accord d'un PNC) renvoient des appels légers interceptés par un `try...catch` asynchrone (`WebMessageReceived`) côté WPF.

### B. Scalabilité et Performance
- Décharger le pipeline UI du Thread WPF principal permet au `FlightPhaseManager` et au `SuperScoreManager` de tourner à 10-20 Hz (rafraîchissements par seconde) sans perturber l'expérience utilisateur. 
- L'utilisation de TailwindCSS permet de designer des tableaux de bord extrêmement denses très rapidement par classe utilitaire, impossible à faire avec du XAML standard.

### C. Gestion d'Erreurs
Puisque le pont JSON est invisible, il requiert une rigueur absolue :
- **Global Error Hooks :** Mise en place d'un `window.onerror` côté JS pour capturer silencieusement (ou alerter en Debug) toute propriété indéfinie (`TypeError`) lors du parsing de payloads profonds (ex: SimBrief API).
- Côté C#, les sérialisations JSON sont blindées avec les paramètres locaux et l'utilisation rigoureuse des balises `[JsonPropertyName]`.

---

## 3. Liste des Tickets (Structure du Design)

Bien que cette restructuration soit en grande partie le socle actuel du projet, voici la structure théorique des tickets de cette architecture :

- [ ] **TICKET 1 : Intégration du composant WebView2**
  - Ajout du package NuGet `Microsoft.Web.WebView2`.
  - Remplacement de l'arborescence UI WPF de `MainWindow.xaml` par un tag `<wv2:WebView2>` asynchrone unique.
  - Configuration du `EnsureCoreWebView2Async()` de l'environnement virtuel.

- [ ] **TICKET 2 : Création de l'arborescence Frontend (`wwwroot`)**
  - Création du dossier local embarqué, de l'`index.html` de base et des liaisons de fichiers statiques (CSS/JS/Fonts/Icons).
  - Injection du CDN TailwindCSS (ou bundle compilé) pour activer les utilitaires de style de l'application.

- [ ] **TICKET 3 : Le Routeur de Messages (Message Router)**
  - Implémentation du routeur `switch(payload.type)` global dans `app.js` pour dispatcher l'update DOM.
  - Mappage de la fonction `webView_WebMessageReceived` en C# pour interpréter le JSON entrant depuis les boutons du HTML.

- [ ] **TICKET 4 : Migration de la Télémétrie en temps réel**
  - Adaptation de la boucle principale `Telemetry_Tick` vers l'envoi exclusif d'un objet anonyme JSON `{ altitude, speed, vs, phase ... }`.
  - Rafraîchissement ciblé des éléments HTML (`document.getElementById().innerText`) pour limiter le reflow/repaint du navigateur.

- [ ] **TICKET 5 : Sécurisation Globale (Fool-proofing)**
  - Traitement natif des attributs JSON (gestion du Casse/CamelCase entre dotnet et JS).
  - Gestion experte du chargement (`DOMContentLoaded` vs démarrage du SimConnect) pour éviter d'envoyer des événements à une WebView blanche.
  - Ajout d'une balise d'alerte `window.onerror` JS pour éviter le syndrome de "l'application qui ne répond plus".
