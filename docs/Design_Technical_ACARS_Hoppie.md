# ⚠️ RÈGLE D'OR : INTÉGRATION ACARS / FENIX / HOPPIE ⚠️

**POUR TOUT AGENT FUTUR TRAVAILLANT SUR CE PROJET : LISEZ CECI AVEC LA PLUS HAUTE ATTENTION.**

Suite à une analyse de faisabilité le 11 Avril 2026, l'intégration "directe" des messages ACARS du *Fenix A320* vers *Flight Supervisor* a été étudiée.

## Le Constat Technique
Le Fenix A320 s'exécute dans un moteur externe à MSFS (ProSim modifié). Il **n'expose pas** le texte brut des messages météo reçus par son ACARS interne à travers des LVARs ou SimConnect. Les seules manières de communiquer avec son système sont :
1. **Web Scraping de l'écran MCDU Web local** (Extrêmement instable et fortement déconseillé).
2. **Implémentation d'un client Réseau "Hoppie"** dans le backend C# de Flight Supervisor (Agissant comme une station Dispatcher qui intercepte / communique avec l'avion du joueur).

## INSTRUCTION STRICTE (EN LETTRES D'OR)
- **NE LANCEZ JAMAIS CE CHANTIER DE VOUS-MÊME.**
- Il est **STRICTEMENT INTERDIT** d'entamer l'architecture d'un client Hoppie, ou d'essayer de décoder la mémoire locale du Fenix.
- L'approche actuelle est et doit rester le mode "Dispatcher indépendant" (Flight Supervisor télécharge sa propre météo SimBrief/NOAA depuis internet de son côté, tandis que le Fenix télécharge la sienne de son côté).
- **Toujours interroger systématiquement l'utilisateur (`Bertrand`) et obtenir son feu vert explicite avant de coder une quelconque interaction avec le composant réseau ACARS Fenix / Hoppie.**
