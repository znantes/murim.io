# MURIM — Living World Foundation

Le projet vise un RPG Murim pour Windows où le joueur **contrôle un habitant ordinaire du monde** plutôt qu'un héros prédestiné.

## Branche de fondation

La nouvelle architecture vit dans `src/Murim.Simulation/` et les documents de conception dans `docs/`.

Points déjà posés :

- monde autonome qui avance pendant les actions du joueur ;
- population initiale configurable (10 000 PNJ par défaut) ;
- naissance dans n'importe quel milieu, y compris grandes familles/sectes et naissance monstrueuse très rare ;
- identité, foyer, âge, conjoint, enfants, métier, personnalité, relations et rumeurs ;
- 10 000 techniques déterministes avec 7 raretés et 50 techniques Divines ;
- rangs Murim de Troisième rang à Vie-et-Mort ;
- 5 000 monstres/formes avec mutations et évolutions ;
- 60+ métiers civils, martiaux, impériaux et clandestins ;
- factions orthodoxes, familles, démoniaques, extérieures, criminelles, impériales et commerciales ;
- 1 260 pilules fictives, 1 000 artefacts et 40 ingrédients initiaux ;
- relations multidimensionnelles avec oubli progressif et souvenirs-ancrages ;
- propagation de rumeurs avec crédibilité, ambiguïté, anxiété, distorsion et sources indépendantes ;
- événements et tournois pouvant se produire sans le joueur.

## Lire en premier

1. `docs/research/MURIM_RESEARCH.md` — recherches et sources.
2. `docs/design/GAME_DESIGN_BIBLE.md` — règles de conception et architecture.
3. `docs/design/CONTENT_COUNTS.md` — nombres garantis par les validateurs.
4. `src/Murim.Simulation/Content/ContentRegistry.cs` — point d'entrée des catalogues.

## Création du monde

```csharp
var (world, content, engine) = LivingWorldFactory.Create(
    seed: 190724,
    npcPopulation: 10_000);

// Le joueur n'est qu'un ID de PNJ dans WorldState.
var player = world.PlayerNpc;

// Une action de 6 heures fait avancer tout le monde de 6 heures.
engine.AdvanceMinutes(world, 6 * 60);
```

## Interface prévue

Godot 4 servira de client Windows : illustration du lieu, zones cliquables, narration, actions contextuelles et commandes texte. Clics et commandes doivent produire les mêmes actions de simulation.

Les anciens prototypes `web/` et `src/World/` sont conservés comme références pendant la migration ; la nouvelle bibliothèque compilable doit rester isolée sous `src/Murim.Simulation/`.
