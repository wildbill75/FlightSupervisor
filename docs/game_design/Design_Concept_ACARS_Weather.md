# Design Concept : ACARS Weather Request

## 1. Fonctionnement dans un vrai Airbus (et Fenix A320)
Dans la réalité, l'**ACARS** (Aircraft Communications Addressing and Reporting System) est un système de liaison de données numériques entre l'avion et le sol (via VHF ou satellite). 
Lorsque le pilote souhaite obtenir la météo :
- Il utilise le **MCDU** (Multipurpose Control and Display Unit).
- Il navigue dans le menu **AOC** (Aeronautical Operational Control) > **WX REQUEST**.
- Il saisit l'OACI de l'aéroport (ex: LFPG) et sélectionne le type de données (METAR, TAF, ATIS).
- La requête est envoyée ("SEND"). Quelques minutes plus tard, la compagnie (via le prestataire de données) renvoie le bulletin météo sous forme de message texte qui s'imprime dans le cockpit ou s'affiche sur le MCDU.

## 2. Implémentation dans Flight Supervisor
**Flight Supervisor** joue le rôle d'une tablette EFB (Electronic Flight Bag) avancée ou d'une interface de supervision au sol (Dispatch). Le but n'est pas de *remplacer* le MCDU du Fenix, mais de simuler la réception de ces mêmes données côté "Superviseur".

### Le bouton "REQ ACARS WX" (à implémenter)
Nous allons ajouter un bouton de requête manuelle dans le panneau de vol de l'interface `index.html`. 
Lorsqu'il sera cliqué, Flight Supervisor simulera une requête ACARS pour rafraîchir les données météo du vol en cours.

### La source de la Météo (Le problème des 27°C)
Pourquoi avez-vous eu 27°C sans lancer ActiveSky ? Voici comment Flight Supervisor décide d'où provient la météo (dans le code C# `RefreshLiveWeatherAsync`) :

1. **ActiveSky (Si configuré dans vos paramètres)** :
   Flight Supervisor tente de se connecter à l'API locale d'ActiveSky (`http://localhost:19285/ActiveSky/API/GetWeatherInfo`). 
   - **Si ActiveSky tourne**, il récupère la météo injectée dans le simulateur (très précise).
   - **Si ActiveSky ne tourne pas** (ou API injoignable), la requête échoue silencieusement.

2. **Le Fallback (SimBrief)** :
   Si Flight Supervisor n'arrive pas à joindre ActiveSky (parce qu'il n'est pas lancé), il conserve les données générées **au moment où vous avez fait votre plan de vol SimBrief**. Les 27°C que vous avez vus étaient donc probablement la météo statique et prévue par SimBrief au moment de la génération du dispatch, et non la météo "live".

3. **L'alternative NOAA (Live Weather)** :
   Si vous utilisez la météo en direct de MSFS (sans ActiveSky), il faut configurer la source météo de Flight Supervisor sur **NOAA**. Dans ce cas, l'ACARS ira chercher les vrais METAR/TAF sur les serveurs mondiaux (ce qui correspond à 99% à la météo Live de MSFS).

---

> [!TIP]
> **En résumé :**
> - Le système que l'on va implémenter est bien le même principe que le `WX REQUEST` du Fenix, mais accessible depuis votre dashboard Flight Supervisor.
> - Si vous utilisez ActiveSky, **il doit être lancé avant le vol**, sinon Flight Supervisor utilisera la météo "figée" du plan de vol SimBrief.
> - Le bouton `REQ ACARS WX` forcera une nouvelle interrogation de la source configurée (ActiveSky, NOAA, ou SimBrief).

**Est-ce que cette explication clarifie le comportement pour vous ? Si c'est le cas, je peux procéder à l'ajout du bouton ACARS dans l'interface et démarrer l'intégration du Logbook/Global Report !**
