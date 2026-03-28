# Walkthrough - Raffinement de la Gestion Cabine & Service en Vol

Nous avons terminé le Design complet du module **PNC Communication & Cabin Prep**. Ces fonctionnalités font passer l'atmosphère cabine de simples statistiques statiques à une simulation dynamique et sensible aux opérations.

## Changements Clés

### 📋 Système de Préparation Cabine (TICKET 7)
Les commandes "Prepare for Takeoff" et "Prepare for Landing" déclenchent désormais une **Progression de Mise en Sécurité** (0-100%).
- **Conditions d'Arrêt** : La progression s'arrête si la vitesse au sol est > 25 kts ou si les G-Force sortent de la plage 0.8G–1.2G.
- **Sièges Forcés** : Si le Commandant force la commande "Seats" avant 100%, une **pénalité de -300 points** est appliquée.

### 🏔️ Service en Vol (Règle des 10 000 pieds) (TICKET 8)
- **Limite d'Altitude** : Le service ne progresse qu'au-dessus de 10 000 pieds MSL.
- **Arrêt en Descente** : Suspension automatique si la vitesse verticale est < -500 fpm ou si l'altitude descend sous 10 000 pieds.
- **Calibration EET** : La vitesse du service s'adapte dynamiquement selon le temps de vol estimé (SimBrief EET).

### 🔔 Feedbacks Audio & Intercom (TICKET 10)
- **Chime de Prêt** : Un signal sonore (Chime) est déclenché automatiquement lorsque la cabine atteint 100% de mise en sécurité.
- **Logs PNC** : Des messages de confirmation au format "[PNC] Cabin is now secure..." s'affichent dans le flux de messages.

### ⚠️ Application de la Sécurité (TICKET 11)
- **Surveillance Active** : Le système vérifie l'état de la cabine lors de la phase de décollage et lors du passage sous les 500ft RA en approche.
- **Pénalités Sévères** : Toute opération critique effectuée avec une cabine non sécurisée entraîne une pénalité de **-300 points SuperScore**.

## Preuve de Vérification

### Logique Backend (`CabinManager.cs`)
La boucle `Tick` a été enrichie pour gérer les collisions entre les phases de vol et l'état de préparation.

```csharp
bool cabinCheckRequired = (phase == FlightPhase.Takeoff) || (phase == FlightPhase.FinalApproach && altitude < 500);
if (cabinCheckRequired && !isCabinReady) {
    OnPenaltyTriggered?.Invoke(-300, "DISHONORABLE: Unsecure Cabin for Ops");
}
```

### Infrastructure Audio (`index.html` & `app.js`)
L'infrastructure de lecture audio a été connectée à la télémétrie C#.

```javascript
else if (payload.type === 'playSound') {
    const audio = document.getElementById('chimeEmergency');
    audio.play();
}
```

## Prochaines Étapes
1. **Design de la Génération de Crises** : Extension du système pour inclure des événements plus rares (problème pressurisation, passager malade grave).
2. **Annonces Cdt Contextuelles** : Lier les annonces du capitaine aux événements de vol en cours.
