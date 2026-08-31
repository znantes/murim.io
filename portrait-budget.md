# Murim — architecture média et portraits IA

## Objectif
Préparer une banque visuelle de 10 000+ identités de PNJ sans charger toutes les images dans le navigateur.

## Règles
- Un PNJ important possède une identité visuelle persistante.
- Les portraits doivent évoluer avec l'âge, les blessures, les maladies, les vêtements, la faction et les événements.
- Les familles, sectes, clans démoniaques et groupes non martiaux disposent d'identités visuelles distinctes.
- Les enfants peuvent hériter de caractéristiques visuelles de leurs parents.
- Les portraits réels de production seront générés par IA et stockés en WebP optimisé; aucun SVG ne sert de portrait final.

## Performance
- Chargement lazy: seuls les portraits visibles ou nécessaires sont chargés.
- Les variantes d'âge sont regroupées par PNJ (`0,1,3,6,10,13,16,20,25,30,40,50,60,70,80,90`).
- Le jeu ne télécharge pas toute la banque au démarrage.
- Les petites vignettes utilisent une résolution inférieure aux portraits détaillés.
- Les images peuvent être compressées en WebP avec plusieurs niveaux de qualité.
- Les lieux, techniques, manuels, factions, armes et autres illustrations suivent le même principe de chargement à la demande.

## Estimation indicative
La taille finale dépend de la résolution et de la compression. À 500 Ko/image, 10 000 images représentent environ 5 Go. À 1 Mo/image, environ 10 Go. C'est pourquoi la banque complète doit être découpée et chargée à la demande.

## Extension
La cible de 10 000 identités n'est pas une limite technique: de nouvelles identités peuvent être ajoutées lorsque le monde génère de nouveaux PNJ.
