# Matrice de Communication Cabine (PA & Intercom)

Ce document liste exhaustivement les flux de communication au sein du jeu pour clarifier "Qui parle à qui, comment, et quand". Il permet de séparer les actions du Commandant de bord (joueur) de l'autonomie des PNC.

## 0. Code Couleur (UI & Logs)

Pour que le joueur identifie instantanément la nature et la provenance d'un message dans la console des logs (ou sur les boutons), un **code couleur strict** doit être appliqué :

| Tag | Canal de Communication | Couleur UI (Tailwind) | Code Hex | Signification / Emploi |
| :--- | :--- | :--- | :--- | :--- |
| `[CPT PA]` | **Commandant (Cockpit) ➔ Passagers** | **Vert** (`text-emerald-500`) | `#10b981` | Le commandant prend le micro général. Ton rassurant et autoritaire ("Welcome Aboard"). |
| `[PNC PA]` | **PNC (Cabine) ➔ Passagers** | **Bleu** (`text-sky-400`) | `#38bdf8` | Le Chef de cabine s'adresse aux passagers ("Safety instructions", "Service"). |
| `[CPT INT]` | **Commandant (Cockpit) ➔ PNC** | **Orange** (`text-amber-500`) | `#f59e0b` | Ordre à l'interphone (Intercom) ou instruction procédurale ("Seats for Takeoff"). |
| `[PNC INT]` | **PNC (Cabine) ➔ Commandant** | **Cyan** (`text-cyan-400`) | `#22d3ee` | Communication privée de l'équipage au cockpit ("Cabin is secured", requêtes). |

---

## 1. Annonces au Public (PA - Passenger Announcements)

Il s'agit des communications diffusées dans la cabine via les haut-parleurs, audibles par les passagers.

| Émetteur | Code | Déclencheur | Description et Conditions | Impact Gameplay |
| :--- | :--- | :--- | :--- | :--- |
| **Commandant** | `[CPT PA]` | **Bouton Manuel** | **Welcome Aboard** : Diffusé idéalement au parking ou au roulage. Le commandant annonce les détails du vol. | Immersion, bonus potentiel de satisfaction si fait au bon moment. |
| **Commandant** | `[CPT PA]` | **Bouton Manuel** | **Turbulence Warning** : N'apparaît **uniquement** qu'en phase de croisière (Cruise) ET que la **jauge de turbulence** oscille (détection de turbulences). Ordonne aux pax de s'attacher. | Prévient une chute brutale de Confort/Sécurité en cas de secousses. |
| **PNC** | `[PNC PA]` | **Automatique** | **Safety Instructions** : Les PNC performent les consignes de sécurité automatiquement au début du roulage sans aucune intervention. | Requis pour la validation de la sécurité cabine avant le décollage. |
| **PNC** | `[PNC PA]` | **Automatique** | **Annonce Retard / Excuses** : Si le PNC est très proactif et le vol en retard, l'annonce d'excuse est lancée automatiquement. | Mitige la perte de Points de Satisfaction liée au retard. |

---

## 2. Communications Interphone (Intercom / Crew Only)

Il s'agit des communications privées (cockpit-cabine) qui ne sont pas entendues par les passagers.

| Émetteur | Code | Déclencheur | Description et Conditions | Impact Gameplay |
| :--- | :--- | :--- | :--- | :--- |
| **Commandant** | `[CPT INT]` | **Bouton Manuel** | **Prepare Cabin for Takeoff / Landing** : Le pilote ordonne à l'équipage de préparer la cabine juste avant les phases critiques. | Remplissage d'une jauge de sécurité et obtention de points (Score). Obligatoire. |
| **Commandant** | `[CPT INT]` | **Bouton Manuel** | **Seats for Takeoff / Landing** : Le pilote prévient les PNC qu'ils doivent s'attacher immédiatement. | Validation finale de la sécurité cabine (+ Points). |
| **PNC** | `[PNC INT]` | **Automatique** | **Status Reports** : Les PNC remontent l'état (température, plaintes, avancement). Aucun bouton n'est requis par le joueur. | Monitoring du PNC (Affiche dans le log les événements de vol). |
| **Commandant** | `[CPT INT]` | **Bouton Manuel** | **Request Cabin Report** : Permet de forcer un "Status Report" des PNC si le joueur veut savoir où en est le service. | Remontée instantanée de la Timeline d'Events. |

---

## Synthèse des Règles UI (Interface Web)
1. **Application stricte des couleurs** : Un bouton *Welcome Aboard* devra avoir un liseré vert fluo, tandis qu'un bouton d'ordre *Seats for Takeoff* aura un liseré orange (amber).
2. **Moins de clics, plus de gestion** : Le commandant n'utilise plus les boutons commerciaux (PNC PA). Tout ce qui concerne le service est géré par l'IA du PNC.
3. **Smart Focus** : Le bouton `Turbulence Warning` (PA) sera l'aspect le plus dynamique. Si le vol est calme, l'interface du PNC Report restera épurée. S'il y a des turbulences, le bouton apparaîtra instantanément à l'écran, poussant le joueur à réagir.
