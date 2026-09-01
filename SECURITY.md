# Sécurité de Murim

## Objectif

Murim doit être construit selon le principe de défense en profondeur. Aucune application ne peut garantir qu'elle sera impossible à attaquer ou qu'un fichier téléchargé sera toujours sain, mais le projet applique plusieurs protections pour réduire fortement les risques.

## Règles de développement

- Aucun mot de passe, token, clé API ou clé privée ne doit être stocké dans le dépôt.
- Les entrées utilisateur et les sauvegardes doivent être traitées comme non fiables.
- Les fichiers externes ne doivent jamais être exécutés par le moteur du jeu.
- Les ressources du jeu sont chargées selon une liste de types autorisés.
- Les gros catalogues et assets sont chargés à la demande afin de limiter les risques de déni de service local par consommation excessive de mémoire.
- Les builds Windows natifs utilisent les protections compilateur et linker disponibles (stack protection, CFG, ASLR et DEP/NX).
- Les dépendances doivent être maintenues et vérifiées avant publication.

## Distribution

Les exécutables distribués doivent être produits par le workflow de build et accompagnés d'un contrôle d'intégrité (SHA-256). Une signature Authenticode avec un certificat de signature de code sera ajoutée lorsque la distribution publique de l'application sera prête.

## Signalement

Pour signaler une vulnérabilité, ne publiez pas de secret ou d'exploit fonctionnel dans une issue publique. Utilisez un canal privé de signalement lorsque celui-ci sera configuré pour le projet.
