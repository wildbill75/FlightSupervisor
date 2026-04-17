# Design: UI Tech Log & MEL (Minimum Equipment List)

## 1. Objectif du Design
Fournir une interface esthétique, tendue et hautement professionnelle simulant le carnet technique (Tech Log) d'un avion avec une expérience asymétrique selon le choix du modèle de compagnie (Low-Cost vs Legacy). Le design doit refléter le stress opérationnel en **Low Cost** et le souci du service premium en **Legacy**.

## 2. Mockup Visuel Initial
Le mockup conceptuel généré met en avant les badges d'états dynamiques, un mode sombre immersif et des options de décision claires.

![UI Concept: Tech Log & MEL](file:///C:/Users/Bertrand/.gemini/antigravity/brain/029e267d-6531-4791-990d-b590cd102d76/mel_tech_log_ui_1776441631044.png)

## 3. Asymétrie UI : Low-Cost vs Legacy

L'interface du "Tech Log Event" sera la même, mais les éléments de prise de décision s'afficheront différemment pour renforcer l'immersion psychologique :

### 🔸 Mode Low-Cost Carrier (LCC)
- **Palette de couleurs :** Des tons industriels stricts, avec le bouton **[ DEFER ITEM TO MEL ]** mis en évidence (ex: orange vibrant, couleur principale) car c'est la décision vitale pour ne pas détruire les marges.
- **Micro-Copy Agressive :** Les avertissements se concentrent sur la ponctualité. 
  - *Ex: "Requesting Technical Repair will induce a 65 min delay. Consecutive legs will be critically impacted."*
- **Bouton [ REQUEST REPAIR ] :** Plus discret (bouton fantôme avec bordure fine), car faire appel à la maintenance en Outstation est la pire décision économique.

### 🔹 Mode Legacy
- **Palette de couleurs :** Ambiance soignée (bleu profond/navy), axée sur la qualité et la résilience. 
- **Micro-Copy Axée Passager :** Les avertissements informent sur le risque de chute du score de la marque.
  - *Ex: "Aircraft defect impacts Premium Pax Comfort. Flying without addressing this issue will incur severe brand penalties."*
- **Bouton [ REQUEST REPAIR ] :** Bouton principal ou équilibré. Le système Hub permet une réparation fiable. Le prix en temps est assumé par la compagnie.

## 4. Composants UI (TailwindCSS)

### 4.1. Le Pop-up / Panneau Tech Log
*   **Conteneur :** Un effet `backdrop-blur-md bg-slate-900/90` très moderne, recouvrant l'écran ou glissant depuis le bas (style tiroir).
*   **Header :** 
    *   Titre : `TECHNICAL DEFECT LOGGED`
    *   Icône clignotante : ⚠️ (Warning)
    *   ATA Chapter visuel. ex: `ATA 21 - AIR CONDITIONING / PRESSURIZATION`

### 4.2. Les Badges de Statut (Pills)
*   `CAT A` / `CAT B` / `CAT C` / `CAT D`
*   `NO DISPATCH` (AOG - Aircraft On Ground) -> Rouge écarlate `bg-red-600 animate-pulse`
*   `MEL APPLIED` -> Ambre `bg-amber-500`

### 4.3. Les Conséquences Détaillées
Une zone d'impact en forme de tableau de bord décrivant les effets sur la cabine :
- 💺 **Comfort:** -25%
- ⏱️ **Ground Ops:** +15 mins Boarding
- ⛽ **Fuel Penalty:** +3% Trip Fuel

## 5. Interactions (Animation et Transitions)
1.  **L'Apparition (Trigger) :** Lors du block-in ou en vol, une notification rouge "TECH LOG MSG" apparaît en haut à droite avec un léger son d'imprimante ACARS.
2.  **L'Expansion :** Au clic, le panneau se déploie. L'écran entier s'assombrit pour forcer la décision (`z-index: 50`).
3.  **La Décision :**
    - Si `DEFER TO MEL` cliqué : Un tampon numérique rouge ou orange s'imprime : **"DISPATCHED WITH MEL"**, le volet se referme et l'élément passe dans l'onglet "MEL / DEFERRALS" actif du hub.
    - Si `REQUEST REPAIR` cliqué : Un timer s'enclenche avec l'icône technicien `🛠️ Tech Onboard`. Le turnaround de l'avion est freezé jusqu'à réparation complète.
