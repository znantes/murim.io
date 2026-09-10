# Bible de conception — Murim vivant

## Vision

**Une vie parmi des milliers. Aucun destin ne vous attend.**

Le joueur ne reçoit pas une classe héroïque, un système secret ou une quête principale. Il contrôle exactement le même type d'entité que les PNJ. Une partie commence par une naissance ; les parents, le lieu, le milieu social, la faction éventuelle, la santé, le potentiel et les événements du monde existent avant que le joueur puisse les comprendre.

Le jeu final vise Windows et une interface principalement **illustrée, point-and-click + commandes textuelles**. Une illustration représente le lieu perçu. Les zones cliquables sont les lieux/actions réellement connus du personnage. Une commande textuelle permet les mêmes actions de manière rapide.

## Piliers

### 1. Le joueur est un PNJ

`PlayerNpcId` est seulement l'identifiant de l'habitant actuellement contrôlé. Il n'existe aucun bonus caché `isPlayer`, aucun multiplicateur de chance, aucun spawn de maître autour de lui et aucune protection scénaristique.

### 2. Information locale

L'interface ne doit afficher que :

- ce que le personnage voit maintenant ;
- ce qu'il se souvient avoir appris ;
- ce qu'il croit à partir de témoignages/rumeurs ;
- ce que ses documents, cartes et contacts lui permettent de connaître.

La vérité complète appartient uniquement au moteur de simulation.

### 3. Monde autonome

Toute action a un coût temporel. Le monde avance pendant : sommeil, travail, voyage, entraînement, lecture, méditation, apprentissage, captivité et récupération.

### 4. Progression par pratique

Les compétences montent parce qu'elles sont utilisées. Le métier est une conséquence de la pratique et de l'emploi, pas un choix de classe.

### 5. Rareté ≠ maîtrise

Trouver une technique Divine ne permet pas de la maîtriser. Le personnage peut être incapable de la comprendre, se blesser, perdre le manuel, le vendre, ou mourir avant de l'apprendre.

### 6. Les relations ont une histoire

Une personne peut aimer, respecter et craindre quelqu'un simultanément. Les liens changent avec le contact, les souvenirs, les services rendus, les trahisons, la distance, le temps et les rumeurs.

### 7. Les conséquences peuvent être manquées

Un événement hors champ n'est pas un échec de contenu. Il fait partie de l'histoire du monde. Le joueur peut en découvrir des traces vingt ans plus tard.

---

## Architecture du dépôt

```text
murim.io/
├── START.md
├── index.html                     # prototype web historique
├── docs/
│   ├── design/
│   │   ├── GAME_DESIGN_BIBLE.md
│   │   └── CONTENT_COUNTS.md
│   └── research/
│       └── MURIM_RESEARCH.md
├── src/
│   ├── Murim.Simulation/
│   │   ├── Murim.Simulation.csproj
│   │   ├── Core/
│   │   │   └── CoreSimulation.cs
│   │   ├── Content/
│   │   │   └── ContentRegistry.cs
│   │   ├── Creatures/
│   │   │   └── CreatureContent.cs
│   │   ├── Events/
│   │   │   └── WorldEventTournament.cs
│   │   ├── Factions/
│   │   │   └── FactionContent.cs
│   │   ├── Items/
│   │   │   └── AlchemyArtifactContent.cs
│   │   ├── Martial/
│   │   │   └── MartialContent.cs
│   │   ├── Professions/
│   │   │   └── ProfessionContent.cs
│   │   ├── Society/
│   │   │   └── SocietySimulation.cs
│   │   └── World/
│   │       ├── LifeSimulation.cs
│   │       └── WorldBootstrap.cs
│   └── World/                     # prototypes C# précédents, conservés pour migration
└── web/                            # prototypes HTML précédents
```

Le nouveau projet C# est isolé dans `src/Murim.Simulation`. Cela évite que les anciens prototypes — qui contiennent actuellement plusieurs types d'inventaire concurrents — bloquent le futur build.

---

## Naissance et origine sociale

Une nouvelle vie tire une origine à partir de la population réelle du monde, pas d'une liste de bonus.

Origines possibles :

- hameau agricole ;
- village de pêcheurs ;
- ville marchande ;
- capitale impériale ;
- famille pauvre, moyenne ou aisée ;
- foyer d'artisan ;
- caravane ou agence d'escorte ;
- foyer lié à une secte ;
- branche secondaire d'une grande famille ;
- lignée principale extrêmement rare ;
- enfant/petit-enfant d'un ancien si la généalogie le permet ;
- famille de soldat, fonctionnaire, criminel, guérisseur, marchand, aubergiste, etc. ;
- naissance de monstre extrêmement rare.

Le rang social ne garantit pas le bonheur. Naître Namgung peut fournir accès et contraintes : obligations, concurrence, mariages politiques, surveillance, attentes familiales, impossibilité d'apprendre certains arts si le personnage est une branche secondaire.

### Enfance

0–2 ans : perception, attachement, sommeil, alimentation, maladie, reconnaissance des voix ; aucune action autonome complexe.

3–5 ans : langage, jeu, imitation, petites explorations domestiques.

6–9 ans : lecture éventuelle, tâches, apprentissage familial, découverte des règles sociales.

10–12 ans : apprentissage de métier, bases physiques et techniques adaptées.

13–15 ans : formations plus exigeantes possibles, mais restrictions liées au corps et à la culture locale.

16+ : autonomie croissante selon lieu, sexe social, richesse, lois, famille et faction ; le moteur ne doit pas confondre autonomie juridique et puissance martiale.

---

## Identité d'un PNJ

Chaque PNJ possède au minimum :

```text
Identité
  id
  prénom / nom
  sexe
  culture
  jour de naissance
  lieu de naissance
  origine sociale
  espèce éventuelle

Famille
  foyer
  parents
  enfants
  conjoint
  branches familiales
  héritage / dettes

Corps
  santé
  endurance
  qi
  faim / soif / sommeil / fatigue
  blessures
  maladies futures

Psychologie
  curiosité
  ambition
  empathie
  loyauté
  tolérance au risque
  sociabilité
  crédulité
  anxiété
  patience

Vie sociale
  relations multidimensionnelles
  réputation locale
  factions
  métier / historique de carrière
  richesse / possessions

Connaissance
  lieux connus
  personnes reconnues
  organisations connues
  techniques connues
  rumeurs crues
  événements observés
```

À terme, ajouter valeurs morales, habitudes, préférences, traumatismes, objectifs, obligations financières, langue/dialecte et routine hebdomadaire.

---

## Techniques — 10 000

Le générateur crée exactement 10 000 définitions stables pour un seed donné.

### Raretés

| Rareté | Quantité | Idée de disponibilité |
|---|---:|---|
| Ordinaire | 4 500 | foyers, soldats, écoles locales, métiers |
| Peu commune | 2 500 | artisans qualifiés, écoles régionales |
| Rare | 1 500 | sectes établies, maîtres, guildes riches |
| Supérieure | 850 | héritages protégés, experts |
| Légendaire | 450 | lignées majeures, ruines, archives |
| Mythique | 150 | secrets historiques et maîtres rarissimes |
| Divine | 50 | sommets mondiaux, souvent perdus ou incomplets |

### Domaines martiaux

Arts internes, trempe du corps, épée, sabre, lance, bâton, poing, paume, doigt, jambes, lutte, déplacement, qinggong, arc, armes cachées, formations.

### Domaines de métier

Soin, médecine, toxicologie fictive, alchimie, forge, armes, armures, cuisine, brassage, herboristerie, agriculture, chasse, pistage, dressage, pêche, mine, charpenterie, maçonnerie, couture, tissage, calligraphie, peinture, musique, érudition, enseignement, commerce, négociation, comptabilité, commandement, stratégie, espionnage, vol, assassinat fictif, escorte.

### Maîtrise d'une technique

À ajouter comme état par personnage :

```text
TechniqueKnowledge
  techniqueId
  compréhension 0..100
  maîtrise 0..100
  stabilité 0..100
  expérience réelle
  erreurs apprises
  variante personnelle
  enseignant/source
  date de dernière pratique
```

La non-pratique peut réduire la précision d'une technique sans supprimer complètement les automatismes anciens.

---

## Arts martiaux

Échelle sociale retenue :

```text
Non-pratiquant
Troisième rang (삼류)
Deuxième rang (이류)
Premier rang (일류)
Sommet (절정)
Sommet transcendant (초절정)
Transformation (화경)
Mystère profond (현경)
Vie et Mort (생사경)
```

Chaque rang a `Entrée / Établi / Sommet`. La promotion n'est pas déclenchée par une barre d'EXP unique ; elle demande un ensemble de conditions : contrôle, corps, respiration, maîtrise technique, expérience, blessures compatibles, compréhension et parfois enseignement.

Un pratiquant peut être extrêmement dangereux dans un contexte particulier tout en ayant un rang inférieur à un autre.

---

## Métiers

Le catalogue initial dépasse soixante professions. Exemples : paysan, riziculteur, cultivateur de thé, herboriste, bûcheron, mineur, pêcheur, chasseur, trappeur, dresseur, cuisinier, brasseur, boulanger, boucher, tailleur, tisserand, teinturier, charpentier, maçon, potier, papetier, fabricant d'encre, forgeron, forgeron d'armes, armurier, joaillier, sculpteur de jade, constructeur naval, marchand, colporteur, courtier, comptable, estimateur, aubergiste, tenancier de maison de thé, maître d'écurie, serviteur, intendant, scribe, précepteur, érudit, bibliothécaire, calligraphe, peintre, musicien, conteur, médecin, apothicaire, alchimiste, maître d'armes, disciple, intendant de secte, maître des formations, garde/capitaine d'escorte, messager, maître de caravane, batelier, garde du corps, chasseur de primes, constable, soldat, officier, stratège, enquêteur, courtier d'informations, espion, voleur, contrebandier, receleur, bandit, pirate fluvial, racketteur, assassin, empoisonneur fictif, pilleur de tombes, mercenaire, moine, taoïste, soigneur itinérant, artiste de rue, devin, mendiant.

### Carrière émergente

`ProfessionSystem.Train()` augmente une compétence avec rendement décroissant. `BestFit()` examine ce que le PNJ sait réellement faire. L'emploi et le titre doivent ensuite dépendre du marché local : être excellent cuisinier dans un hameau sans auberge n'offre pas les mêmes opportunités que dans une capitale.

---

## Relations

### Axes

- Familiarité : « est-ce que je connais encore cette personne ? »
- Affection : attachement positif/négatif.
- Confiance : probabilité subjective qu'elle tienne parole.
- Respect : valeur accordée à ses compétences ou principes.
- Attraction : dimension romantique potentielle.
- Peur : danger perçu.
- Ressentiment : griefs accumulés.
- Obligation : dette morale/sociale.
- Rivalité : compétition active.
- Parenté : résistance structurelle à l'oubli.

### Oubli

L'absence ne met pas automatiquement une relation à zéro. Les souvenirs marquants créent des ancres.

Exemple :

```text
An 12 : A sauve B lors d'une crue       -> mémoire positive forte, ancre
An 14 : A et B deviennent amis proches
An 17 : A part à l'armée
An 27 : première rencontre depuis 10 ans

Résultat possible :
familiarité : moyenne
confiance : affaiblie
amitié actuelle : faible/moyenne
attachement historique : encore présent
respect : élevé
```

Autre résultat possible si la séparation s'accompagne d'une rumeur de trahison : affection historique présente mais confiance négative.

---

## Rumeurs

### Vérité objective et croyance individuelle

Une rumeur n'est jamais stockée comme simple booléen « vraie/fausse » dans la tête des PNJ.

```text
Rumor
  événement source
  contenu
  vérité cachée
  ambiguïté
  importance
  anxiété
  crédibilité source
  secret
  nombre de sauts
  distorsion

RumorBelief par PNJ
  confiance
  position : rejette / doute / incertain / croit / certain
  dernière source
  nombre de sources indépendantes
  dernière date entendue
```

### Mutations

- omission ;
- exagération ;
- adoucissement ;
- raccourcissement de la source ;
- déplacement du blâme ;
- inflation du prestige.

### Impact réputationnel

Une rumeur n'altère la réputation que chez les personnes qui y sont exposées. On doit donc aller vers une **réputation perçue par groupe/localité**, pas une seule variable globale.

---

## Factions

### Orthodoxes

Shaolin, Wudang, Mont Hua, Emei, Kunlun, Qingcheng, Kongtong, Zhongnan, Diancang, Union des Mendiants comme références de genre ; Pin d'Azur et Rivière Silencieuse comme créations originales.

### Grandes familles

Namgung, Tang du Sichuan, Peng du Hebei, Zhuge, Murong. Naissance possible dans branche principale ou secondaire. Ajouter à terme : héritage, généalogie complète, mariages politiques, conseil des anciens, propriétés, serviteurs et branches cadettes.

### Démoniaques / interdites

Lotus Noir, Flamme Céleste, Sūtra Sanglant — originales. Elles ne doivent pas être une faction homogène « mauvaise » : luttes internes, civils dépendants, traditions, dissidents et pragmatiques rendent le système plus crédible.

### Extérieures

Palais de Glace du Nord, Palais des Cent Bêtes du Sud, Palais du Soleil de Verre, Palais de la Brume Marine.

### Criminelles

Alliance de la Forêt Verte, Dix-Huit Forts du Grand Fleuve, Marché des Lanternes Grises, réseaux de voleurs et contrebandiers.

### Impériales

Cour de Haedong, Bureau du Sceau Vermillon, Armée des Marches, Office Médical Impérial.

### Commerciales

Agence d'Escorte de la Rivière Azurée, Compagnie de la Caravane d'Or, Guilde du Registre de Jade.

---

## Monstres — 5 000

Matrice : `50 familles × 20 aspects × 5 évolutions = 5 000`.

Chaque créature a : identité d'espèce, rareté, habitat, régime, tempérament, intelligence, puissance, mutation, stade, parent évolutif et forme suivante.

La mutation doit ensuite être influencée par : climat, alimentation, exposition au qi, blessures, reproduction, artefacts, environnement et proximité de ruines.

Un monstre intelligent peut progressivement devenir un véritable acteur social plutôt qu'un simple ennemi.

---

## Alchimie, ingrédients, pilules, artefacts

### Ingrédients

Deux origines : `HistoricalInspiration` et `Fictional`. Les ingrédients réels n'ont aucun dosage ni effet médical réel dans le moteur ; leurs propriétés surnaturelles sont explicitement fictives.

### Pilules

1 260 variantes : récupération, fatigue, stabilité du qi, corps, concentration, soutien au franchissement, purification fictive, blessures, résistance au froid/chaleur, sommeil, nutrition, bêtes, artisanat, toxines fictives.

Une pilule possède pureté, puissance, durée de conservation et effets secondaires. La tolérance/répétition devra être ajoutée afin d'empêcher le spam de consommables.

### Artefacts

1 000 combinaisons de type × matériau × aspect. Un artefact a durabilité, efficacité de qi et prestige. Certains objets peuvent être célèbres même si leur capacité réelle est moyenne : réputation d'objet et puissance réelle doivent être distinctes.

---

## Événements du monde

Types initiaux : naissance, mort, mariage, divorce, blessure, maladie, récupération, changement de métier, apprentissage, promotion, duel, vendetta, raid, attaque de bandits, escorte réussie/ratée, essor commercial, pénurie, incendie, crue, sécheresse, famine, épidémie, migration, schisme, alliance, succession, coup d'État, édit impérial, taxe, arrestation, évasion, découverte, artefact trouvé, manuel retrouvé, monstre aperçu/attaque, tournoi, festival, funérailles, mariage, marché, caravane, disparition, scandale, vague de rumeurs.

Chaque événement a : lieu, date, acteurs, factions, témoins directs, publicité, importance, ambiguïté. **Il n'est pas automatiquement connu du joueur.**

---

## Tournois

Treize archétypes initiaux : rencontre de jeunes disciples, examen interne, Assemblée des Neuf Arts, Grande Conférence du Murim, épreuve du chef d'alliance, succession familiale, arène locale, épreuve d'escorte, examen militaire, alchimie, forge, cuisine, tournoi clandestin.

Le système futur doit générer : inscriptions, règles, divisions d'âge/rang, arbitres, blessures, forfaits, paris illégaux, triche, réputation, spectateurs, vendeurs, rumeurs et conséquences politiques.

---

## Interface Godot prévue

### Écran principal

```text
┌──────────────────────────────────────────────────────────────┐
│ Date / heure      Lieu perçu                         état    │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│             ILLUSTRATION DU LIEU ACTUEL                      │
│                                                              │
│  [zone bibliothèque] [personne] [porte] [objet visible]     │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│ Narration / observations / dialogue                          │
│ > Une cloche résonne derrière le mur.                       │
├──────────────────────────────────────────────────────────────┤
│ [actions contextuelles]                                      │
│ > commande __________________________________________ [OK]   │
└──────────────────────────────────────────────────────────────┘
```

Une zone inconnue peut n'afficher que « bâtiment », « porte », « sentier ». Après apprentissage : « Bibliothèque familiale », « pavillon des anciens », etc.

### Commandes

```text
observer
parler <personne>
aller <lieu>
demander <personne> à propos de <sujet>
travailler
cuisiner
lire <objet>
s'entraîner <technique>
méditer
attendre <durée>
dormir
inventaire
journal
souvenirs
rumeurs
```

Le parser doit convertir les commandes vers les mêmes `GameAction` que les clics afin qu'il n'existe pas deux systèmes de règles différents.

---

## Performance : 10 000 PNJ et monde autonome

Ne pas simuler chaque respiration de 10 000 PNJ à chaque frame.

### Niveaux de simulation

- **Tier A — lieu du joueur** : événements fins, interactions immédiates.
- **Tier B — région proche** : simulation horaire/journalière.
- **Tier C — monde lointain** : simulation agrégée journalière/hebdomadaire, mais événements majeurs persistants.
- **Tier D — histoire** : anciennes générations compactées en biographies, généalogies et archives.

Les événements importantes deviennent des objets persistants. Un PNJ lointain n'a pas besoin d'une animation de marche ; il a besoin d'une heure de départ, destination, route et risque.

---

## Sauvegarde

Sauvegarder au minimum : seed, horloge, PNJ modifiés, ménages, relations, événements persistants, rumeurs, factions, inventaires et état des catalogues. Les 10 000 techniques et 5 000 monstres étant déterministes, la sauvegarde peut stocker le seed et les modifications plutôt que recopier chaque définition.

---

## Règles anti-protagoniste

1. Aucun événement ne cible le joueur sans cause du monde.
2. Aucun butin n'est amélioré parce que le joueur est présent.
3. Aucun maître n'attend éternellement le joueur.
4. Un tournoi commence et finit à sa date.
5. Un PNJ peut mourir hors écran.
6. Un manuel peut être détruit sans que le joueur le voie.
7. Un amour peut choisir quelqu'un d'autre pendant dix ans d'absence.
8. Une faction peut gagner/perdre une guerre sans intervention du joueur.
9. La rareté d'origine du joueur suit la même distribution que celle des autres naissances.
10. La mort termine cette vie. Une nouvelle vie ne récupère pas automatiquement les connaissances de l'ancienne.

---

## Prochaines implémentations prioritaires

1. actions unifiées clic/commande + coût en temps ;
2. perception/knowledge branchés sur l'interface ;
3. généalogie stricte parent/grand-parent/fratrie ;
4. économie locale et prix dynamiques ;
5. inventaires + propriété des objets ;
6. apprentissage réel des 10 000 techniques ;
7. recrutement/expulsion/promotion des factions ;
8. déplacements et routes ;
9. maladie/blessure/récupération ;
10. tournoi complet ;
11. génération et placement d'illustrations ;
12. export Godot Windows `.exe`.
