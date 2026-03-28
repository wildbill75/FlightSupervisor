# Task: Compilation Warnings Cleanup

## Contexte
Lors de la compilation du projet, 54 warnings ont été remontés par le compilateur C#. 

## Analyse des Warnings
- **~50 Warnings "Nullable" C# 8.0+** : La majorité de ces alertes (CS8618, CS8600, CS8625, etc.) signalent des variables qui peuvent théoriquement devenir nulles mais n'ont pas le suffixe `?`. Le projet a la balise `<Nullable>enable</Nullable>`, ce qui pousse le compilateur à râler si toutes les strings d'un modèle ne sont pas explicitées comme non-nulles ou initialisées.
- **4 Warnings Fonctionnels (Vrais Défauts)** : 
  - `CS0414` : Dans `CabinManager.cs`, la variable `_hasRewardedTurbulenceReaction` est assignée mais jamais lue ultérieurement.
  - `CS4014` : Dans `MainWindow.xaml.cs` (approx ligne 885), un appel asynchrone n'est pas "awaited", ce qui peut causer une exécution flottante sans gestion d'exception de la Task.
  - `CS8622` : Les deux gestionnaires d'événements `MainWindow_Closed` et `CoreWebView2_WebMessageReceived` ont des signatures où la nullabilité de "sender" ne correspond pas parfaitement à l'EventHandler natif (`object? sender` vs `object sender`).

## Actions à Prendre (Backlog)
- [ ] **Définir la politique de Nullabilité** : Faut-il simplement ajouter `<Nullable>disable</Nullable>` dans le fichier `FlightSupervisor.UI.csproj` pour masquer les dizaines de faux-positifs, ou bien corriger méticuleusement les 50 variables pour calmer le validateur tout en rendant le code plus sûr ?
- [ ] **Supprimer** le champ inutilisé `_hasRewardedTurbulenceReaction` dans `CabinManager.cs`.
- [ ] **Ajouter** un opérateur `await` devant l'appel asynchrone dans la méthode concernée de `MainWindow.xaml.cs`.
- [ ] **Ajuster** la signature des deux handlers d'événements WebView2/Window dans `MainWindow.xaml.cs` pour ajouter le `?` après `object` (`object? sender`).
