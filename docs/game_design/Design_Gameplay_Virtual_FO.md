# Design Gameplay - Le "Virtual First Officer" (FO)

Dans *Flight Supervisor*, le joueur incarne souvent le Commandant de Bord (Captain). Jusqu'à présent, le joueur interagit seul face au système (l'avion, le PNC, les passagers). L'introduction d'un **First Officer (FO)** virtuel, ou copilote, apporte une nouvelle strate de **gestion de la charge de travail (Workload Management)** et de **Crew Resource Management (CRM)**.

L'objectif est d'avoir un compagnon d'intelligence artificielle dans le cockpit qui ne pilote pas l'avion dans MSFS, mais qui assiste le joueur dans sa gestion des vols et du système *Flight Supervisor*.

---

## 1. L'Expérience du FO (Stats du Copilote)

Comme pour le PNC, la compagnie aérienne virtuelle va vous assigner un FO avec différents niveaux d'expérience, qui se ressentent directement dans l'aide qu'il vous apporte :

*   **Le "Rookie" (Junior FO, < 500 heures sur type) :**
    *   Très concentré sur ses tâches basiques, il ne prend aucune initiative.
    *   Il exécute vos ordres uniquement si vous les demandez dans l'interface.
    *   S'il y a une erreur ou une violation de sécurité en préparation, il n'ose rien dire (Cockpit Gradient trop fort).
*   **Le "Veteran" (Senior FO, > 3000 heures sur type) :**
    *   Un véritable atout. Il anticipe vos besoins.
    *   Vous pousse des informations de vol de manière autonome.
    *   Couvre vos erreurs avant qu'elles ne coûtent des points.

---

## 2. Emprise Gameplay & Mécaniques

Voici comment le FO peut concrètement faciliter la tâche du joueur et apporter du gameplay narratif :

### A. Le "Bouclier" de Sécurité (Safety Buffer)
Actuellement, si le joueur fait une erreur de pilotage *(ex: Phares d'atterrissage toujours allumés au-dessus de 10,000ft, survitesse sous 10,000ft, pente d'approche instable)*, le `FlightPhaseManager` déclenche une pénalité instantanée.
*   **Gameplay FO :** Le FO sert de tampon ("Buffer"). S'il est compétent, il va détecter l'oubli et vous alerter *avant* la pénalité. Par un TTS audio : *"Captain, landing lights are still on."* ou *"Check speed... we are approaching 250 knots."*
*   Le joueur gagne un délai de (par exemple) 15 à 30 secondes pour corriger son erreur sans perdre aucun point de SuperScore. Un FO novice (Rookie) ne remarquera rien, et la pénalité s'appliquera immédiatement.

### B. Gestion des Tâches Administratives (Workload Delegation)
Il y a beaucoup d'actions dans l'UI de *Flight Supervisor* (demander la météo, initialiser le vol suivant lors d'un turnaround).
*   **Automatisation Passive :** Un bon FO fera le travail pour vous. Plutôt que de cliquer sur "Get Arrival ATIS" dans l'application, lorsque vous passez en phase de descente, le Senior FO parle : *"J'ai récupéré l'ATIS de Toulouse. Piste 14R en service, QNH 1013, Temp 12. Je l'ai affiché sur votre EFB."* (Et les données s'affichent toutes seules dans l'UI).
*   **Turnaround (Multi-Leg) :** À l'arrivée à la porte, un bon FO enchaîne la préparation du vol suivant. Le "Briefing" du 2ème vol est instantanément préchargé dans l'UI, alors que sans lui, vous auriez dû cliquer manuellement pour séquencer le vol.

### C. Le Gestionnaire des "Comms" (Filtre PNC)
Actuellement, le Chef de Cabine (PNC) "spamme" parfois le cockpit s'il a une basse Proactivité (ex: problèmes de passagers).
*   **Délégation "Comms" :** Pendant les phases critiques (Décollage / Approche), le joueur est sous forte charge de travail. Le FO prend automatiquement le relais sur l'interphone. Si le PNC appelle pour un passager malade pendant une approche :
    *   *"Leave it to me Captain, focus on flying."*
    *   Le FO gère virtuellement l'appel du PNC pour vous, empêchant le système de vous le remonter.

---

## 3. Dynamique de CRM (Le Respect du Cockpit)

Le CRM (Crew Resource Management) est mesuré : le niveau de confiance que le copilote a envers vous.

*   **Construire la confiance :** Prendre soin de son équipage, respecter les check-lists, et voler doucement fait monter la confiance du FO. Quand la confiance est haute, il devient amical, fait des "callouts" rassurants et travaille mieux.
*   **Casser la confiance :** Si vous ignorez volontairement ses alertes (*"Check speed..."*), que vous faites des atterrissages brutaux, ou que vous forcez le départ avec un avion technique en panne, la jauge s'effondre.
*   **Conséquence d'un mauvais CRM :** Un FO défiant adoptera un ton très formel et froid (les audios changent), refusera de prendre des initiatives ("As you wish, Captain"), vous forçant à tout micro-manager. Pire, en fin de rotation, un FO avec 0% de confiance générera un "ASR" (Air Safety Report) invisible qui vous pénalisera massivement de retour à la base (Score Final de Pilotage).

---

## Résumé de l'Impact :
Le FO n'est pas juste un "Skin" vocal. C'est une **aide fonctionnelle dynamique** qui vient récompenser les joueurs voulant optimiser leur vol (et qui se méritera en gardant un pilotage propre).
