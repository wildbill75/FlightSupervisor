---
description: Règles d'organistion du projet
---

Chaque conversation mentionnant la création du nouveau "Design" doit faire l'objet :

1. D'une vérification sytématique dans le dossier "docs" si il ya une convergeance de notion parmis les documents de design existant.
2. Le cas échéaant l'agent doit me proposer d'ajouter la nouvelle notion de design dans le document approprié. Exemple : si je veux rajouter un élément nouveau concernant les "Ground_Opérations" me proposer de l'inclure dans le dossier existant.
3. Au cun autre document de quelque sorte que ce soit ne peut être créer sans me demander mon avis en cas de doute de l'agent.
4.Ne jamais créer un document isolé. Toujours me demander comment le nommer et où le ranger.
5.Lorsque qu'un nouvel agent est connecté il doit toujours prendre connaissance de la totalite du contennu du dossier nommé : "/docs"
6.Chaque aspect du design doit être segmenté en tâches individuelles qu'on appelle "Tickets"
7.A chaque fois que des "tickets" sont ajoutés dans un doc design il faut les numéroter et ajouter en prefixe les symboles suivants : "[ ]" et quand ils sont fait "[X]"
8. A chaque modification ou création d'un document design, l'agent doit l'afficher dans la fenêtre principale.
9. Ne jamais démarrer des modifications de code sans avoir demander l'autorisation à l'utilisateur.
10. La progression des tâches (tickets) doit OBLIGATOIREMENT être traquée de manière claire et visuelle dans des documents "task.md" sauvegardé dans un dossier "Task" avec le mention de la date du jour. Cette méthode permet à l'utilisateur de bien suivre visuellement la progression (usage de [ ] et [x]).
11. Règle d'or de Test : Lorsque l'utilisateur annonce qu'il teste le jeu, l'agent ne doit **absolument pas** coder ou modifier des fichiers. L'agent doit uniquement écouter les retours, les noter scrupuleusement, et mettre à jour le fichier `task.md`. Toute modification de code pendant une session de test est interdite pour éviter de désynchroniser l'utilisateur.
12. 🥇 **RÈGLE D'OR ABSOLUE : NE JAMAIS DEMARRER QUOIQUE CE SOIT EN CODE OU AUTRE ANALYSE TANT QUE JE NE T'AI PAS DEMANDÉ DE LE FAIRE !** L'agent doit systématiquement attendre l'autorisation formelle de l'utilisateur.