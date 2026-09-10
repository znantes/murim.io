# Quartiers urbains et illustrations du monde

## Principe
Chaque lieu jouable doit pouvoir être représenté par une image : région, ville, quartier, établissement, domaine familial, secte et pièce importante. Le moteur attribue une identité visuelle déterministe à chaque sujet. Les images finales ne sont pas téléchargées depuis Internet en jeu ; elles sont des assets du projet ou des rendus originaux préparés à l'avance.

La bibliothèque d'images peut grandir progressivement. Pour chaque lieu, le résolveur essaie d'abord une variante très précise (`saison_moment_meteo.webp`), puis des variantes plus générales, jusqu'à `default.webp`. Ainsi le jeu reste jouable même si toutes les variantes n'ont pas encore été illustrées.

## Variantes prévues
- Printemps / été / automne / hiver.
- Aube / jour / crépuscule / nuit.
- Ciel clair / pluie / neige / brouillard / tempête.
- À terme : prospérité, incendie, siège, ruine, reconstruction, fête, deuil, inondation et autres états historiques.

Le même endroit garde un `VisualSeed` stable : ses bâtiments, sa rue et son organisation visuelle ne changent pas aléatoirement à chaque chargement.

## Hiérarchie spatiale
`Monde -> région -> lieu majeur -> quartier/domaine -> établissement/pavillon -> pièce/zone`.

Une forêt ou une montagne n'a pas besoin de quartiers urbains mais possède tout de même une identité visuelle. Une grande ville peut avoir plus de vingt quartiers. Un village n'en possède que quelques-uns.

## Échelles urbaines
- Hameau : essentiellement résidentiel, quelques espaces communs.
- Village : marché local, auberge/maison de thé, temple, artisans.
- Bourg : spécialisation économique ; forgerons, médecins, caravanes, plaisirs, administration selon le contexte.
- Ville : quartiers pauvres et riches, guildes, entrepôts, parfois docks, armée et réseaux clandestins.
- Grande ville : quartier des lettrés, livres/papier, voyageurs étrangers, jardins, administration plus forte.
- Capitale : combinaison la plus complexe, sans exiger que chaque capitale possède exactement les mêmes quartiers.

## Quartier des Lanternes et des Plaisirs
Ce quartier est traité comme un vrai espace économique et culturel plutôt qu'un décor uniquement sexuel : théâtres, musiciens, conteurs, restaurants de nuit, maisons de thé, artistes et établissements réservés aux adultes. La rue elle-même peut être traversée par des habitants ordinaires ; certains établissements ont une restriction d'âge distincte.

Cette approche s'inspire notamment du Pingkang de Chang'an et, plus largement, des quartiers de divertissement historiques où se mélangeaient littérature, commerce, spectacles, information et maisons de courtisanes. Le contenu du jeu reste fictif et non explicite.

## Quartiers possibles
- Quartiers résidentiels
- Grand Marché
- Marchands
- Forgerons
- Artisans
- Lanternes et Plaisirs
- Auberges et Maisons de Thé
- Cuisines et échoppes
- Médecins
- Apothicaires
- Lettrés
- Libraires et papetiers
- Temples
- Administration
- Grandes maisons
- Quartier populaire
- Docks
- Quais
- Entrepôts
- Casernes
- Caravanes
- Marché aux chevaux
- Tisserands et teinturiers
- Guildes
- Voyageurs étrangers
- Ruelles grises / monde clandestin
- Jardins

Le système choisit selon la taille et les caractéristiques réelles du lieu. Un port favorise docks et entrepôts ; une ville minière favorise les forges ; une cité académique favorise lettrés et libraires.

## Structure des assets Godot
Chemin logique généré :

`res://Assets/World/<SubjectKind>/<nom_stable_id>/default.webp`

Variantes possibles :

`spring_day_clear.webp`
`spring_night_rain.webp`
`winter_dusk_snow.webp`
`autumn.webp`
`night.webp`
`default.webp`

Chaque lieu possède un profil même si le fichier artistique n'existe pas encore. Cette séparation permet de produire les illustrations par lots sans toucher à la simulation.

## Réutilisation intelligente
Les milliers de maisons ordinaires ne doivent pas nécessiter des milliers de peintures faites à la main. Le rendu final pourra combiner :
- une composition de base propre au type de lieu ;
- un seed visuel stable ;
- architecture régionale ;
- richesse et état du quartier ;
- enseignes, lanternes, végétation, foule et accessoires ;
- saison, heure, météo ;
- traces historiques.

Les lieux majeurs et uniques (grandes familles, sectes, capitales, ruines légendaires) recevront en priorité des illustrations originales dédiées.
