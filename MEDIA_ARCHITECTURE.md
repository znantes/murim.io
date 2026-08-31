# Murim — architecture des médias IA

## Objectif

Le jeu réserve au minimum **10 000 identités visuelles de PNJ**. Une identité est persistante : son visage, son âge, ses particularités et son appartenance peuvent évoluer sans changer d'identité.

## Portraits

Les portraits de production sont prévus en WebP, avec chargement différé (`loading="lazy"`) et une résolution adaptée à l'interface. Aucun SVG n'est utilisé comme portrait.

Structure recommandée :

```text
portraits/
  pnj/
    p000001/
      age_0.webp
      age_10.webp
      age_20.webp
      age_30.webp
      age_40.webp
      age_50.webp
      age_60.webp
      age_70.webp
    p000002/
      ...
```

Le moteur connaît 16 tranches d'âge (0, 1, 3, 6, 10, 13, 16, 20, 25, 30, 40, 50, 60, 70, 80, 90). Les assets manquants ne génèrent pas d'icône d'image cassée : l'interface indique simplement que le portrait IA n'est pas encore disponible.

## Identité visuelle

Chaque PNJ peut conserver :

- traits du visage et morphologie ;
- sexe et âge ;
- cheveux et yeux ;
- cicatrices, marques et blessures ;
- vêtements selon profession/faction/saison ;
- expressions ;
- état physique ;
- particularités visuelles ;
- liens familiaux.

Les familles, sectes, clans démoniaques et catégories de non-combattants doivent avoir leurs propres directions artistiques. Deux factions différentes ne partagent donc pas automatiquement le même visage ou la même identité visuelle.

## Chargement et poids

Pour éviter de transformer GitHub Pages en téléchargement de plusieurs dizaines de Go :

1. charger seulement les portraits visibles ;
2. utiliser WebP compressé ;
3. ne pas charger toutes les tranches d'âge d'un PNJ en même temps ;
4. garder les variantes haute résolution hors du premier chargement ;
5. pouvoir servir les médias depuis un stockage/CDN séparé du code du jeu.

Ordre de grandeur indicatif pour 10 000 portraits :

- 150 Ko/image ≈ 1,5 Go ;
- 300 Ko/image ≈ 3 Go ;
- 500 Ko/image ≈ 5 Go ;
- 1 Mo/image ≈ 10 Go.

Ces chiffres sont des budgets de conception, pas la taille actuelle du dépôt.

## Autres médias prévus

La même architecture peut accueillir des illustrations IA pour :

- lieux, routes et régions ;
- villages et villes avec variantes jour/nuit/météo/saison ;
- familles et sectes ;
- clans démoniaques ;
- techniques et manuels ;
- armes et artefacts ;
- commerces et ateliers ;
- maladies et états physiques ;
- donjons, ruines et héritages ;
- événements historiques et catastrophes ;
- montures et animaux.

Le moteur ne doit jamais confondre une illustration décorative avec le portrait d'identité d'un PNJ.
