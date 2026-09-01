# Murim — Application

Cette arborescence prépare la migration de Murim vers une application PC.

## Architecture cible

- `client/` : interface et rendu.
- `engine/` : simulation et règles du monde.
- `data/` : données du Jianghu.
- `assets/` : ressources visuelles chargées à la demande.
- `tools/` : génération et tests.

## Phase 1

Le premier objectif est un noyau d'application stable : lancement, nouvelle vie, sauvegarde/chargement, temps, personnage, PNJ et simulation minimale.

Les gros catalogues et portraits ne sont pas chargés au démarrage. Ils seront branchés progressivement et testés individuellement.

## Migration

La version web reste disponible pendant la transition. Les données de jeu doivent rester séparées de l'interface afin de pouvoir être utilisées par plusieurs clients.
