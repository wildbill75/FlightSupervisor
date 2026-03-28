# Design Gameplay : Interphone Cabine & Rapports d'État

## 1. Description de l'Objectif
La fonctionnalité "Interphone Cabine & Rapports d'État" complète la boucle de communication entre le Commandant et l'équipage de cabine (PNC). Elle introduit un système de "Requête" proactive pour les rapports d'état et un mécanisme de "Mise à l'échelle du service" (Service Scaling) pour gérer les aléas du vol (turbulences, descente).

## 2. Mécaniques

### 📞 La Requête Interscope (RAPPORT CABINE)
Un nouveau bouton, **"DEMANDER RAPPORT CABINE"**, est disponible dans le panneau PNC.
- **Disponibilité** : Toujours disponible en vol.
- **Refroidissement (Cooldown)** : 2 minutes avec un compte à rebours dynamique sur le bouton.
- **Impact Stratégique** : Demander un rapport pendant les phases de préparation ralentit la progression (`SecuringProgress`) de 50% pendant 15 secondes (distraction de l'équipage).
- **Retour Audio** : `intercom_ding` en cas de succès, `intercom_busy` pendant le temps de refroidissement.

### 💬 Narrations Contextuelles
Le PNC génère un message synthétisant plusieurs points de données :
- **État du Service** : Indique si le service est en cours ou terminé.
- **État de la Cabine** : Rapporte les niveaux d'anxiété et de confort des passagers.
- **Post-Turbulences** : Confirme la sécurité de la cabine après des événements météorologiques sévères.
- **Logique Stratégique** : La narration s'adapte si une "Pénalité Stratégique" est active.

### 🍱 Mise à l'échelle & Interruption du Service (Story 17.6)
Le Commandant peut désormais gérer directement le flux du service en vol.
- **Bascule Manuelle** : Le bouton "PAUSE/REPRISE SERVICE" permet au pilote d'arrêter l'équipage pour des raisons de sécurité.
- **Arrêt Automatique** : Le service s'arrête immédiatement si :
    - Des turbulences `Sévères` sont détectées (delta G-Force > 0.6).
    - L'avion est en dessous de 10 000 ft (sauf en Croisière).
    - Une descente rapide est détectée (> 1000 fpm en dessous de 20 000 ft).
- **Alerte Visuelle** : La barre de progression du catering devient **Rouge** et **Pulsante** lorsqu'elle est arrêtée.

## 3. Liste des Tickets (Plan de Design)

- [x] **TICKET 31 : Logique de Requête Interphone**
    - [x] Implémenter `RequestCabinReport()` dans `CabinManager.cs`.
    - [x] Génération de narrations contextuelles.
- [x] **TICKET 32 : Expansion de l'Interface UI**
    - [x] Bouton "RAPPORT CABINE" avec cooldown dans `app.js`.
- [x] **TICKET 33 : Pénalités Stratégiques**
    - [x] Pénalité de `SecuringRate` pour distraction de l'équipage.
- [x] **TICKET 34 : Variations Audio & Chimes**
    - [x] Retour audio Ding/Busy.
- [x] **TICKET 35 : Mise à l'échelle du Service (Story 17.6)**
    - [x] Bascule manuelle de pause/reprise.
    - [x] Logique d'arrêt automatique (turbulences/descente).
    - [x] Retour visuel "Arrêté" sur la jauge de service.

---

## 4. Spécifications de Design Technique

### [BACKEND] CabinManager.cs
- `IsServiceHalted` : Indicateur pour suspendre la progression.
- `ToggleServiceInterruption()` : Commande pour basculer l'état et notifier.
- `RequestCabinReport()` : Évaluer les variables et retourner un résumé localisé.

### [FRONTEND] app.js
- `updateIntercomButtons()` : Injection dynamique de boutons d'action contextuels.
- Gestionnaire de `telemetry` : Met à jour le style de la barre de progression selon `isServiceHalted`.

## 5. Plan de Vérification
1. **Arrêt Manuel** : Mettre le service en pause en croisière. Vérifier que la jauge devient rouge et que la progression s'arrête.
2. **Arrêt Auto** : Descendre sous 10k ft. Vérifier la suspension automatique et le message de l'équipage.
3. **Alertes Météo** : Confirmer que des turbulences sévères déclenchent l'arrêt immédiat du service.
4. **Boucle Narrative** : Demander un rapport. Vérifier qu'il mentionne l'anxiété/le confort avec précision.
