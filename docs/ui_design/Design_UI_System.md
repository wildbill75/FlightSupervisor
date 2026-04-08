# Charte Graphique & UI System - Flight Supervisor (V1.1.0)

Ce document formalise les règles de design et de styling tirées de l'interface principale (Dashboard "True Airmanship") de Flight Supervisor. Il sert de référence unique (Single Source of Truth) pour tout le développement futur du projet.

## 1. Philosophie Générale
L'approche visuelle du dashboard est celle d'un **système avionique moderne** et d'un outil de télémétrie "Cyber-Aviation".
- **Lisibilité avant tout :** Les données en surbrillance guident l'œil. L'interface reste très aérée pour ne jamais agresser la vision, même de nuit.
- **Minimalisme et "Glassmorphism" sombre :** Les modales et les blocs flottent sur un fond quasi-noir, délimités par des bordures extrêmement subtiles.
- **Signification par la couleur :** Les couleurs vives ne sont pas utilisées pour la décoration, mais comme indicateurs de statut strict.

---

## 2. Palette de Couleurs (Couleurs Tailwind)

### A. Fonds (Backgrounds)
Les fonds utilisent des tons très froids et sombres, pour maximiser le contraste sans éblouir l'utilisateur.

| Utilisation | Code Hexadécimal | Équivalence Tailwind | Description |
| :--- | :--- | :--- | :--- |
| **Fond Global (Body)** | `#141414` | N/A (Custom) | Noir profond pur/légèrement gris mat. Absolument pas de `#000000`. |
| **Fonds des Sous-Panneaux** | `#1C1F26` | N/A (Custom) | Un gris-bleu très sombre, utilisé pour les cartes ("Cabin Experience", "Turnaround"). |
| **Highlight Rapide (Hover/Active)** | `#1e293b` | `bg-slate-800` | Pour les boutons et éléments listés au survol. |
| **Bordures et Séparateurs**| `rgba(255,255,255,0.05)` | `border-white/5` | Séparation ultra discrète. Utilisé systématiquement sur tous les `<hr>` et contours de panneaux. |

### B. Typographie & Contrastes Globaux
| Utilisation | Code Hexadécimal | Équivalence Tailwind | Description |
| :--- | :--- | :--- | :--- |
| **Valeurs Majeures** | `#ffffff` | `text-white` | Utilisé pour la télémétrie brute, les chiffres massifs (ex: Score 1000). |
| **Titres & Labels (Inactifs)** | `#7b7b7b` / `#b6b6b6` | `text-[#7b7b7b]` / `text-[#b6b6b6]` | Utilisé pour les en-têtes (ex: `TURNAROUND`, `COMFORT`), les légendes et le texte standard. |

### C. La Palette Sémantique (Sémantique des Indicateurs)
**Règle d'or absolue : AUCUNE couleur vive n'est utilisée pour de la décoration.**
Les couleurs s'utilisent *strictement* pour marquer des états sémantiques (retards, alertes, succès, intégrité). De plus, les teintes doivent être **douces/pastels** (les séries `300` ou `400` de Tailwind) et surtout pas "criardes" ou fluo.

| État | Couleur Tailwind | Code Hex | Emploi Technique |
| :--- | :--- | :--- | :--- |
| 🔵 **Primaire / Focus** | `text-sky-300` / `400` | `#7dd3fc` | Noms des aéroports (LFPO), boutons actifs (Menu, Pin), données de statut neutre mais actives. |
| 🟢 **Succès / Pristine** | `text-emerald-300` / `400` | `#6ee7b7` | Horaires dans les temps (`ON TIME`), cabine impeccable (100% propre), réserves OK. |
| 🟠 **Avertissement / Moyen** | `text-amber-300` / `400` | `#fcd34d` | Retard mineur, scores moyens (50-79), avertissement météo. |
| 🔴 **Critique / Erreur** | `text-red-400` / `500` | `#f87171` | Dégradation de score, retard critique, bannières d'urgence (Crisis Banner). |

---

## 3. Typographie Spécifique

**Polices utilisées :**
1. **Manrope :** Typographie stylisée "Headline", utilisée spécifiquement pour les bannières majeures, le logo (`TRUE AIRMANSHIP`), ou les modes d'urgence.
2. **Inter :** Police système pour toutes les sections de données, les étiquettes et les UI.

**Règle des Labels / Titres de Section (`<h2>`, `<h3>`...) :**
- Toujours en majuscules : `uppercase`
- Extrêmement espacés (Tracking important) : `tracking-widest` ou `tracking-[0.4em]`
- Petite taille de police relative : `text-xs` ou `text-sm`
- Opacité réduite pour ne pas voler la vedette à la donnée : `text-[#7b7b7b]`
- **Exemple :** `CABIN EXPERIENCE`, `PAX MANIFEST`

**Règle des Valeurs de Données :**
- Grandes, lisibles et épaisses : `text-lg` à `text-2xl`.
- Pour les chiffres précis (timers, indicateurs de carburant), souvent en `font-bold` ou parfois mis en évidence via des `shadow-inner` ou glows subtils.

---

## 4. Composants et Styles UI

### A. Les Panneaux (Cards)
Les "widgets" principaux (comme Turnover, Cabin Experience) partagent une fondation commune stricte :
- **Background :** `bg-[#1C1F26]`
- **Angles (Border Radius) :** Très arrondis, souvent `rounded-xl` ou `rounded-2xl`
- **Bordures :** `border border-white/5`
- **Ombres :** `shadow-xl` pour marquer le détachement avec l'arrière-plan le plus sombre.
- **Padding intérieur (Aération) :** Le design "Flight Supervisor" _n'est jamais compact_. L'espace vide fait partie du design. Utilisation massive de `p-6` ou `p-8`.

### B. Boutons et Pilules (Pills)
- **Minimalisme des Encadrements :** Éviter à tout prix de surcharger les nouveaux éléments en les encadrant de bordures épaisses ou en abusant d'effets de luminosité (`shadow`/`glow`). Les éléments doivent reposer naturellement sur le fond en utilisant simplement la couleur de leur texte.
- **Styles de pilule :** Utilisées pour résumer une information textuelle (ex: Event). Fond très translucide avec bordure minime (ex: `text-sky-300 bg-sky-600/10 border border-sky-500/20`) et un arrondi extrême `rounded-full`.
- **Interactions & Mouse Over unifiés :** Les comportements au survol (`hover:`) doivent être strictement unifiés dans toute l'application. La norme est d'utiliser une variation d'opacité ou un voile blanc ultra-léger (`hover:bg-white/5`, `hover:text-white` avec `transition-colors duration-300`). Il ne faut **jamais** faire apparaître un fond de couleur vive au survol d'un élément neutre.

### C. Fenêtres Autonomes (Windows)
Pour le Manifest, Ground Ops et Logs :
- Titre très discret avec barre de poigne activée pour le système d'exploitation Windows (`-webkit-app-region: drag`).
- Contrôles minimalistes en haut à droite avec les boutons Minimiser, Épingler (`push_pin`) et Fermer (`X`), fonctionnant via IPC `WebView2`. La punaise active prend la couleur `text-sky-400`.

### D. Progress Bars & Slides
- Le conteneur "Background" (vide) est stylisé pour se fondre : `bg-slate-600` (`#64748b`).
- La jauge intérieure utilise strictement les couleurs Sémantiques définies ci-dessus (Vert pour confort, Rouge/Orange pour anxiété ou poubelles).

---

## 5. Synthèse "Checklist" pour le Développement Frontend
Lors de la création d'un nouveau bloc UI, posez-vous ces questions :
- [ ] Le fond du conteneur est-il bien `#1C1F26` avec un `border-white/5` ?
- [ ] Le titre du sous-menu est-il bien en `uppercase` + `tracking-widest` + gris clair (par ex. `#7b7b7b`) ?
- [ ] Les valeurs importantes (numériques ou les scores) sont-elles visuellement détachées du texte brut (blanches ou de couleur sémantique et épaisses) ?
- [ ] L'espace (padding) est-il suffisant pour conserver l'aspect d'un tableau de bord de luxe (au moins `gap-6` ou `p-6`) ?
- [ ] Ai-je utilisé les bonnes teintes Sémantiques (Sky Blue = Information/Focus, Emerald = OK/Prêt, Red = Erreur) ?
