# Design UI - Gestion Réactive de la Fenêtre et du Contenu

Ce document vise à répondre à la problématique de l'adaptation du contenu et de la fenêtre lors du passage d'un onglet à un autre, afin d'éviter d'avoir à redimensionner à la main ou de subir un scroll encombrant sur toute la fenêtre.

## Les trois pistes principales

Pour répondre à ta question *(Est-ce la fenêtre qui s'agrandit, ou le contenu qui s'adapte ?)*, voici les possibilités techniques et ergonomiques qui s'offrent à nous.

### Piste 1 : Redimensionnement C# dynamique de la Fenêtre (Content-Driven)
**Concept :** Le contenu dicte sa taille à la fenêtre de l'application (WPF).
- À chaque changement d'onglet, l'interface calcule exactement la hauteur et la largeur dont elle a besoin pour afficher l'intégralité du contenu sans aucun scroll (`document.body.scrollHeight/Width`).
- Elle envoie cette dimension au backend C# pour qu'il étende ou réduise instantanément la taille de la fenêtre Windows sur le bureau du joueur.

**Avantages :** 
- Le joueur a toujours 100% des informations sous les yeux sans aucune barre de défilement.

**Inconvénients (Majeurs) :** 
- La fenêtre Windows va bondir et changer de taille physique à chaque clic sur le menu gauche. C'est visuellement très perturbant (effet "pop-up").
- Si la liste des passagers est très longue (ex: 200 pax), la hauteur demandée va dépasser la définition de l'écran du joueur (1080p, 1440p), coupant la fenêtre au-dessus ou sous la barre des tâches.

---

### Piste 2 : Le Contenu s'adapte à la Fenêtre Fixe (Window-Driven Flexbox)
**Concept :** La fenêtre Windows reste fixe, robuste et ancrée (ex: définie à 1850x1020, ou libre d'être redimensionnée par le joueur). Le contenu web est conçu, via CSS, pour ne **jamais** déborder de la hauteur de cette fenêtre (`height: 100vh`, `overflow: hidden` global).
- L'interface se contracte intelligemment via Flexbox.
- Seules les **zones de contenu spécifiques** (ex: la liste météo, l'historique de log, la liste des passagers) se voient attribuer une barre de défilement locale interne élégante (`overflow-y: auto`), tandis que la navigation, les en-têtes et les boutons d'action vitaux, ainsi que les jauges globales, restent constamment épinglés et visibles.

**Avantages :** 
- C'est le standard ergonomique de 100% des applications logicielles sérieuses (EFB, MCDU, Discord, Navigraph).
- Extrêmement stable, l'illusion du "Tableau de Bord métier" est totale. L'application ne "danse" pas sur l'écran.

**Inconvénients :** 
- Nécessite de conserver le scroll, mais uniquement à l'intérieur de composants précis (listes de données), et non sur la fenêtre entière.

---

### Piste 3 : Scaling Adaptatif (Piste 2 alternative - "Zoom Out")
**Concept :** Variante hybride. La fenêtre ne bouge pas, mais le contenu, au lieu d'afficher une barre de défilement, dézoome (le conteneur rétrécit virtuellement) pour forcer tous les éléments à rentrer sur 1 seul écran.

**Avantages :** 
- Strictement aucun scroll (ni global, ni local). Tout rentre au chausse-pied.

**Inconvénients (Majeurs) :** 
- Hétérogénéité du design : des menus avec peu de choses auraient une taille "normale", mais la page Manifest avec beaucoup d'éléments verrait sa police devenir minuscule, au point que les boutons soient illisibles ou difficiles à cliquer sur l'écran.

---

## 🎯 Recommandation & Implémentation

La méthode numéro 1 (la fenêtre bouge toute seule) est techniquement faisable (bridge IPC C#) mais dégradera drastiquement l'expérience utilisateur et l'esthétique statique que nous essayons de construire depuis le début.

**Ma recommandation penche de manière tranchée vers la Piste 2 (Window-Driven Flexbox - Contenu adaptatif et contenu localement déroulant).**
Il suffirait, pour implémenter ce design à merveille, de rigidifier notre conteneur `app.js`  et `index.html` pour bloquer le overflow du `<body>`, contraindre le flex-layout vertical sur `100vh`, et ajouter les balises de scroll stylisées sur nos panneaux de logs et feed temps réel.

**=> DÉCISION VALIDÉE : Lors de la session de test du 29 Mars, l'Option 2 a été validée officiellement comme norme de design pour l'application.**

---

## 🛠 Bugs & Retours UI - (À corriger après les tests en vol)

Pendant la phase de test, plusieurs comportements non désirés ont été repérés concernant les menus d'annonces PA (Flight Deck PA) nécessitant une correction à venir (NE PAS CODER LORS DU VOL EN COURS) :

1. **Doublon d'Apology (Menu vs Bouton Dynamique)**
   - **Symptôme :** Il y a "deux fois" le menu d'apology (une option dans le menu déroulant fixe, et un bouton dynamique en dessous). Le bouton dynamique généré disparaît d'une manière visuellement étrange (probablement après le repoussage). 
   - **Action Requise :** Il y a une interface de trop. Il faudra choisir laquelle conserver (Bouton dynamique `makeDelayBtn` vs l'option `DelayApology` dans le menu déroulant statique) et supprimer le doublon pour éviter les confusions d'interface au sol.

2. **Turbulence Apology au sol (Flight Deck PA)**
   - **Symptôme :** Dans le menu déroulant statique des "Flight Deck PA", l'option `Turbulence Apology` est visuellement atteignable/sélectionnable alors que l'avion est encore au sol.
   - **Action Requise :** L'option `Turbulence Apology` doit être inaccessible (grisée ou supprimée) tant que l'appareil est au sol (`FlightPhase` n'est pas "en l'air"). On ne peut proposer qu'un `Delay Apology` au sol.
