---
description: Comment compiler et relancer l'application Flight Supervisor
---

Lorsque tu dois compiler le projet C# (FlightSupervisor.UI) après avoir fait des modifications, suis toujours ces étapes :

1. Arrête d'abord l'instance actuellement en cours d'exécution.
// turbo-all
2. Exécute la commande PowerShell : `Stop-Process -Name FlightSupervisor.UI -Force -ErrorAction SilentlyContinue`
3. Exécute la commande de build : `dotnet build d:\FlightSupervisor\FlightSupervisor.UI\FlightSupervisor.UI.csproj -c Release`
4. Vérifie que la compilation `Build succeeded` apparaît avant de continuer le développement.
