# Walkthrough - Advanced Turbulence & Cabin Dynamics

Le module **Advanced Turbulence & Cabin Dynamics** est maintenant entièrement opérationnel. Cette refonte transforme la gestion de la cabine en un système dynamique et réactif, où chaque secousse a des conséquences palpables sur la sécurité et le confort des passagers.

## 1. Moteur de Détection de Turbulence (Jitter Engine)
Le système utilise désormais une analyse de variance sur une fenêtre glissante de 5 secondes pour distinguer les turbulences environnementales des manipulations du pilote.
- **Fréquence Élevée** : Les données de G-Force sont capturées à la fréquence d'image (`VISUAL_FRAME`) pour une détection ultra-précise du "jitter".
- **Catégorisation** : La turbulence est classée de `NONE` à `EXTREME`, impactant directement l'anxiété et le risque de blessure.

## 2. Dynamique de la Cabine & Blessures
- **Modèle de Conformité Passager** : Chaque passager (Standard, Grumpy, Anxious, Relaxed) réagit différemment aux consignes. Les passagers anxieux attachent leur ceinture plus vite, tandis que les "Relaxed" peuvent l'oublier.
- **Blessures RNG** : En cas de secousses `SEVERE` ou `EXTREME` avec les ceintures détachées, un algorithme de probabilité peut déclencher des blessures (traumatismes crâniens, etc.).
- **Urgence Médicale** : Toute blessure déclenche automatiquement une crise `Medical Emergency`, forçant le pilote à prendre des décisions critiques (déroutement).

## 3. Interface Utilisateur (Dashboard UI)
L'interface Web a été mise à jour pour refléter ces changements en temps réel :
- **Jauge de Turbulence** : Un nouvel indicateur de sévérité (Bleu -> Violet) affiche le niveau de secousses actuel.
- **Seat Map Dynamique** :
    - **Ceintures** : Chaque siège affiche un point Vert (attaché) ou Rouge (détaché).
    - **Alertes Médicales** : Une icône de secours pulsante (`medical_services`) apparaît sur le siège d'un passager blessé.
- **Anxiété & Confort** : Les barres de progression utilisent des codes couleurs dynamiques pour alerter le pilote en cas de stress excessif.

## 4. Performance du Pilote (PA & Réaction)
- **Reaction Timer** : Le pilote dispose de 30 secondes pour faire une annonce PA après le début d'une turbulence sévère. Un bonus est accordé pour une réaction rapide (<15s), tandis qu'un malus est appliqué en cas d'inaction.
- **Silence Penalty** : Le silence prolongé pendant une crise double l'augmentation de l'anxiété des passagers.

---

### Vérification Technique
- [x] Compilation C# réussie.
- [x] Liaison IPC (`SendTelemetryToWeb`) validée pour le manifest et la sévérité.
- [x] Rendu dynamique du Seat Map testé dans `app.js`.
- [x] Logique de bridage à 90% (Anxiety Cap) Désignée et validée.

**Le système est prêt pour les tests de vol réels !**
