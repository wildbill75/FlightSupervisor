# Design_Technical_CabinReport_Gauges

## 1. Contexte & Objectif
Actuellement, lorsque l'équipage de conduite demande un `Cabin Report` (via le bouton d'intercom PNC), le système a tendance à renvoyer une réponse vocale générique ou TTS s'il n'est pas dans une phase de vol hardcodée. L'objectif de ce design est de **dynamiser le statut rapporté par le PNC** ("Cabin Report") en le liant directement à l'état réel de la cabine via les statistiques système : **Comfort**, **Anxiety** et **Satisfaction**.

---

## 2. Inventaire des Fichiers Audios Existants (PNC Report)
D'après l'analyse du système de base (ex: `assets/sounds/airlines/air_france/pnc/en/female_1/`), nous disposons déjà d'une structure narrative très riche :

### A. Fichiers Liés aux Phases de Vol (Priorité 1)
Ces fichiers doivent s'enclencher *uniquement* si l'action est en cours :
- `pnc_report_preboard.mp3` (En attente d'embarquement)
- `pnc_report_boarding.mp3` (Embarquement en cours)
- `pnc_report_taxi_out.mp3` (Roulage vers la piste)
- `pnc_report_service_start.mp3` (Début du service repas)
- `pnc_report_service_mid.mp3` (Milieu de service repas)
- `pnc_report_service_end.mp3` (Fin du service repas)
- `pnc_report_descent.mp3` (Cabine en descente / Approche)

### B. Fichiers Liés aux Jauges / Événements (Priorité 2)
Ces fichiers prennent le relais si aucune action de phase n'est en cours (Vol de Croisière "Idle") :
- `pnc_report_idle.mp3` : **Jauges au vert** (Tout va bien en cabine, clair et calme).
- `pnc_report_comfort_low.mp3` : **Confort critique** (Plaintes sur la température, les sièges).
- `pnc_report_anxiety_gen.mp3` : **Anxiété critique** globale.
- `pnc_report_anxiety_turb.mp3` : **Anxiété liée aux Turbulences**.
- `pnc_report_anxiety_food.mp3` : **Insatisfaction** liée au manque de catering/nourriture.
- `pnc_report_anxiety_delayed.mp3` : **Anxiété liée au retard** sur le SOBT/Schedules.
- `pnc_report_injured.mp3` : **Urgence médicale** (Priorité Absolue).

---

## 3. Logique de Déclenchement & Seuils (Scaling)

Lors d'un clic sur `CABIN REPORT`, le `CabinManager` évaluera les conditions dans l'ordre de priorité suivant. *Si une condition est remplie, on joue l'audio associé et on termine la requête.*

> [!IMPORTANT]
> Les Seuils de déclenchements "Négatifs" doivent être validés. Nous proposons les plages suivantes pour éviter de sur-stresser le joueur au moindre petit défaut.

### Priorité 1 : Urgences Médicales ou Techniques
- Si `InjuredPassengers > 0` ➔ Jouer `pnc_report_injured.mp3`

### Priorité 2 : Dynamique des Phases Actives
- Si Phase = `Boarding` ➔ `pnc_report_boarding.mp3`
- Si Phase = `TaxiOut` ➔ `pnc_report_taxi_out.mp3`
- Si Service Repas (Meal Service) = En cours ➔ 
   - < 30% complété = `pnc_report_service_start.mp3`
   - > 30% et < 80% = `pnc_report_service_mid.mp3`
   - > 80% = `pnc_report_service_end.mp3`
- Si Phase = `Descent/Approach` ➔ `pnc_report_descent.mp3`

### Priorité 3 : Lecture des Jauges en Croisière (Idle State)
Si l'avion est en croisière (Cruise) et qu'aucun service n'est actif, le système évalue la pire jauge. On établit le score du "pire" critère :
1. **Anxiété extrême** (`Anxiety > 60%`) :
   - Sous-critère : S'il y a eu de nombreuses turbulences récemment ➔ `pnc_report_anxiety_turb.mp3`
   - Sous-critère : Si le vol est sévèrement en retard ➔ `pnc_report_anxiety_delayed.mp3`
   - Sinon ➔ `pnc_report_anxiety_gen.mp3`
2. **Confort critique** (`Comfort < 40%`) :
   - *Température hors limite* ou *Seatbelts ON depuis > 45 minutes* ➔ `pnc_report_comfort_low.mp3`
3. **Satisfaction / Catering critique** (`Satisfaction < 45%` causé par manque de repas) :
   - ➔ `pnc_report_anxiety_food.mp3`
4. **Si toutes les jauges sont dans les clous** (Condition Normale) :
   - ➔ `pnc_report_idle.mp3` ("Cabin is clear and quiet, Captain.")

---

## 4. Audios Manquants à Générer (Identifiés)

La liste de base est étonnamment complète, cependant il manque quelques nuances pour parfaire le réalisme :
1. `pnc_report_securing_cabin.mp3` : L'équipage ne signale pas explicitement qu'il *vérifie/sécurise la cabine* avant le décollage ou avant l'atterrissage. (C'est ce qui avait choqué le joueur lors d'une demande de Cabin Report pendant le Securing).
2. `pnc_report_boarding_completed.mp3` : Savoir que l'embarquement est *terminé* (souvent remplacé par le "Cabin is secured" qui est ambigu).

## 5. Résumé de l'Action à coder (Lors du prochain Sprint)
- Dans le `CabinManager.cs` (`HandleIntercomCall`), injecter ce système de cascades (IF/ELSE : Priorité 1, 2, 3) analysant les taux (`CabinState.Comfort`, `CabinState.Anxiety`).
- Au lieu de forger un prompt TTS, renvoyer l'alias exact de l'audio (`pnc_report_anxiety_gen`, etc.) vers `AudioEngineService`.
