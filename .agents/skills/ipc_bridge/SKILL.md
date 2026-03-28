---
name: Architecture IPC (Inter-Process Communication) C# / JS
description: Règles et conventions sur la communication entre le backend C# et le frontend WebView2 (Javascript).
---

# Règles de Communication C# -> Javascript
1. **Frontend (app.js)** : Le point d'entrée unique de toutes les données se trouve dans le listener `chrome.webview.addEventListener('message', ...)` de `d:\FlightSupervisor\FlightSupervisor.UI\wwwroot\app.js`.
2. **Backend (C#)** : Pour envoyer des données à l'interface, le backend doit obligatoirement utiliser la méthode `SendToWeb(new { type = "nom_de_levenement", ... })` présente dans `MainWindow.xaml.cs`.
3. **Payloads** : L'objet envoyé via `SendToWeb` est sérialisé en JSON automatiquement. Le frontend trie ensuite l'action à effectuer grâce à un `switch(payload.type)`. 

A lire impérativement avant de créer de nouvelles jauges ou de nouveaux formulaires dans l'interface, pour éviter de casser la boucle d'événements existante.
