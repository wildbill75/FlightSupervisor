---
description: Règles de Gestion des Versions (Git Workflow)
---
# Workflow de Sauvegarde et Versioning (Git)

Suite aux problèmes rencontrés (perte temporaire du pont WASM et de la détection lors de refactorings), ce workflow dicte les règles strictes de sauvegarde que chaque agent doit respecter pour protéger le code de Flight Supervisor.

## 1. Fréquence des Sauvegardes (Commits)
**Règle d'or :** Ne jamais entamer un refactoring majeur ou la refonte d'un système sans avoir d'abord sauvegardé la version stable actuelle.
- **Micro-commits :** Après avoir corrigé un bug spécifique ou implémenté une nouvelle fonctionnalité mineure testée et approuvée par le Commandant (utilisateur), l'agent DOIT créer un commit Git clair (ex: `fix: résolution du séquençage GroundOps`).
- **Pré-refactoring :** Avant de toucher à un composant central (`SimConnectService`, `CabinManager`, etc.) ou d'effectuer une restructuration lourde, proposer un commit de "backup".

## 2. Restauration en cas de Crise
Si l'utilisateur signale qu'un système "qui marchait très bien hier" ne marche plus :
1. Ne pas essayer de deviner ou de réécrire le système à l'aveugle.
2. Utiliser `git log` et `git diff` pour isoler la modification destructive.
3. Restaurer le fichier ou le bloc de code avec `git checkout <commit> -- fichier` pour revenir au point de stabilité certifié.

## 3. Nommage des Commits (Conventionnelle)
- `feat:` pour une nouvelle "Story" ou fonctionnalité.
- `fix:` pour la correction d'un bug.
- `docs:` pour la mise à jour des documents `Design_*.md`.
- `refactor:` pour la refonte de la base de code sans ajout de feature.

> [!IMPORTANT]
> Ne laissez pas le travail non commité s'accumuler sur plusieurs sessions. L'utilisateur se repose sur ce filet de sécurité.
