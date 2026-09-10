using Murim.Simulation;

var (world, content, engine) = LivingWorldFactory.Create(seed: 190724, npcPopulation: 10_000);

Console.WriteLine($"Techniques: {content.Techniques.Count}");
Console.WriteLine($"Techniques divines: {content.Techniques.Count(t => t.Rarity == Rarity.Divine)}");
Console.WriteLine($"Monstres/formes: {content.Monsters.Count}");
Console.WriteLine($"Pilules: {content.Pills.Count}");
Console.WriteLine($"Artefacts: {content.Artifacts.Count}");
Console.WriteLine($"Professions: {content.Professions.Count}");
Console.WriteLine($"Factions: {content.Factions.Count}");
Console.WriteLine($"PNJ initiaux: {world.Npcs.Count}");
Console.WriteLine($"Joueur = PNJ: {world.PlayerNpc?.Identity.DisplayName ?? "aucun"}");

var dayBefore = world.Clock.Day;
engine.AdvanceMinutes(world, 30 * 1440);
Console.WriteLine($"Simulation: jour {dayBefore} -> {world.Clock.Day}");
Console.WriteLine($"Événements persistants: {world.Events.Count}");
Console.WriteLine($"Rumeurs: {world.Rumors.Count}");
Console.WriteLine($"Population vivante: {world.Npcs.Values.Count(n => n.IsAlive)}");

ContentValidator.Validate(content);
WorldIntegrity.NormalizeAndValidate(world);
Console.WriteLine("Validation OK");
