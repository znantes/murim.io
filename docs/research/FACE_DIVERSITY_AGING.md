# Recherche — diversité faciale, vieillissement et portraits

## Objectif
Éviter un générateur où tous les PNJ, notamment toutes les femmes, convergent vers un même idéal esthétique. Le portrait doit rester reconnaissable au fil d'une vie tout en changeant réellement de morphologie entre bébé, enfance, adolescence, âge adulte et vieillesse.

## Résultats utiles
- L'asymétrie faciale légère est normale dans la population. Elle ne doit donc pas être supprimée automatiquement par le générateur.
- La quantité de tissu adipeux influence la forme faciale ; un système de portraits doit pouvoir produire des visages plus ronds comme plus anguleux.
- La croissance entre enfance et adolescence modifie réellement la forme 3D du visage. Un simple shader de rides ne suffit pas à représenter l'âge.
- Texture et état visible de la peau influencent fortement l'âge apparent et la perception d'un visage. Ils doivent être séparés de la géométrie génétique.
- Les recherches sur l'attractivité mettent en évidence plusieurs facteurs moyens (symétrie, prototypicalité, peau, etc.) mais aussi d'importantes différences individuelles et culturelles. Le jeu ne doit donc pas posséder une vérité universelle « beauté = X/100 » visible par tous.

## Architecture retenue
1. **Genome** : proportions osseuses, pigmentation, cheveux, adiposité de base, asymétrie, texture potentielle, corpulence, traits familiaux.
2. **Croissance** : morphs spécifiques bébé/enfant/adolescent/adulte/senior qui changent la géométrie sans changer l'identité génétique.
3. **État temporaire** : fatigue, manque de sommeil, pâleur, ecchymoses, gonflement, variation de poids, exposition au soleil.
4. **Séquelles** : cicatrices permanentes synchronisées avec les blessures réelles de la simulation.
5. **Perception sociale** : préférences propres à l'observateur. Une réputation de beauté n'existe qu'après des rencontres réelles et des transmissions sociales.
6. **Beautés légendaires** : au plus quatre femmes adultes vivantes peuvent posséder simultanément une réputation de « grande beauté du Murim ». Ce n'est pas un bonus de gameplay ni une preuve d'une beauté objectivement universelle.

## Pipeline graphique prévu
MakeHuman/Blender sert à construire un maillage humain de référence et ses morph targets. Le résultat est exporté en glTF/GLB. Godot applique ensuite les paramètres du genome aux blend shapes, ajoute les états temporaires et rend le visage avec un SubViewport vers l'interface 2D.

Cette méthode permet de conserver la même identité faciale durant des décennies, de faire ressembler les enfants à leurs parents, de faire persister les cicatrices et de produire des milliers de visages sans stocker une image indépendante pour chaque PNJ.

## Sources consultées
- MakeHuman Community, « Modeling the body » : https://static.makehumancommunity.org/makehuman/docs/modeling_the_body.html
- Koudelová et al., croissance faciale 3D de 7 à 17 ans, PLoS One / PubMed : https://pubmed.ncbi.nlm.nih.gov/30794623/
- Imaizumi et al., changements 3D du visage avec l'âge, PubMed : https://pubmed.ncbi.nlm.nih.gov/25381651/
- Thiesen et al., revue sur l'asymétrie faciale, PubMed : https://pubmed.ncbi.nlm.nih.gov/26691977/
- Coetzee et al., adiposité faciale et perception, PubMed : https://pubmed.ncbi.nlm.nih.gov/21354874/
- Windhager et al., graisse corporelle et forme du visage, PubMed : https://pubmed.ncbi.nlm.nih.gov/24105760/
- Samson et al., état visible de la peau et perception faciale, PubMed : https://pubmed.ncbi.nlm.nih.gov/19889046/
- Little et al., revue sur l'attractivité faciale et les différences individuelles, PubMed : https://pubmed.ncbi.nlm.nih.gov/21536551/
