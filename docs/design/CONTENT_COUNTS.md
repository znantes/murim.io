# Inventaire de contenu

## Catalogues déterministes

| Catalogue | Quantité | Construction |
|---|---:|---|
| Techniques | **10 000** | 20 préfixes × 25 motifs × 20 finales |
| Monstres / formes évolutives | **5 000** | 50 familles × 20 mutations × 5 stades |
| Pilules fictives | **1 260** | 15 buts × 7 raretés × 12 thèmes |
| Artefacts | **1 000** | 20 types × 10 matériaux × 5 aspects |
| Tournois archétypes | 13 | catalogue manuel original |
| Professions | 60+ | catalogue manuel extensible |
| Factions | 30+ | références de genre + créations originales |
| Ingrédients | 40 | inspirations botaniques prudentes + fantasy |

## Techniques par rareté

- Ordinaire : 4 500
- Peu commune : 2 500
- Rare : 1 500
- Supérieure : 850
- Légendaire : 450
- Mythique : 150
- Divine : **50**

Total : **10 000**.

La rareté n'est pas un multiplicateur universel. Une technique Divine de cuisine est un sommet de cuisine, pas une attaque de niveau divin. Chaque technique conserve domaine, puissance, complexité, coût de qi, coût d'endurance, risque et rang martial recommandé.

## Monstres

Les cinq étapes sont : Sauvage → Éveillé → Bête spirituelle → Roi → Ancêtre.

Chaque définition possède : code stable, famille, mutation, stade, rareté, puissance, intelligence, régime, tempérament, habitats, traits et liens d'évolution précédent/suivant.

## Validation

`GameContent.Build(seed)` appelle `ContentValidator.Validate()` et refuse de démarrer si :

- le nombre de techniques n'est pas 10 000 ;
- le nombre de techniques Divines n'est pas 50 ;
- les quotas de rareté divergent ;
- les codes ne sont pas uniques ;
- les monstres ne sont pas exactement 5 000 ;
- une lignée de monstre ne contient pas ses cinq stades ;
- les artefacts ne sont pas exactement 1 000 ;
- le catalogue des métiers devient anormalement petit ;
- l'origine Namgung disparaît du catalogue.
