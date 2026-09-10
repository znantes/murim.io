namespace Murim.Simulation;

public sealed record GameContent(
    IReadOnlyList<TechniqueDefinition> Techniques,
    IReadOnlyList<MonsterDefinition> Monsters,
    IReadOnlyList<ProfessionDefinition> Professions,
    IReadOnlyList<FactionDefinition> Factions,
    IReadOnlyList<IngredientDefinition> Ingredients,
    IReadOnlyList<PillDefinition> Pills,
    IReadOnlyList<ArtifactDefinition> Artifacts,
    IReadOnlyList<TournamentArchetype> Tournaments)
{
    public static GameContent Build(int seed = 190724)
    {
        var content = new GameContent(
            TechniqueGenerator.Generate(seed + 1),
            MonsterGenerator.Generate(seed + 2),
            ProfessionCatalog.All,
            FactionCatalog.All,
            IngredientCatalog.All,
            PillGenerator.Generate(),
            ArtifactGenerator.Generate(seed + 3),
            TournamentCatalog.All);
        ContentValidator.Validate(content);
        return content;
    }
}

public static class ContentValidator
{
    public static void Validate(GameContent content)
    {
        TechniqueGenerator.Validate(content.Techniques);
        MonsterGenerator.Validate(content.Monsters);
        if (content.Artifacts.Count != ArtifactGenerator.ExpectedCount) throw new InvalidOperationException("Artifact catalogue count is invalid.");
        EnsureUnique(content.Techniques.Select(x => x.Code), "technique");
        EnsureUnique(content.Monsters.Select(x => x.Code), "monster");
        EnsureUnique(content.Professions.Select(x => x.Code), "profession");
        EnsureUnique(content.Factions.Select(x => x.Code), "faction");
        EnsureUnique(content.Ingredients.Select(x => x.Code), "ingredient");
        EnsureUnique(content.Pills.Select(x => x.Code), "pill");
        EnsureUnique(content.Artifacts.Select(x => x.Code), "artifact");
        EnsureUnique(content.Tournaments.Select(x => x.Code), "tournament");
        if (content.Techniques.Count(t => t.Rarity == Rarity.Divine) != 50) throw new InvalidOperationException("Exactly 50 Divine techniques are required.");
        if (content.Professions.Count < 60) throw new InvalidOperationException("Profession catalogue is unexpectedly small.");
        if (!content.Factions.Any(f => f.Code == "namgung")) throw new InvalidOperationException("Namgung birth origin is missing.");
    }

    private static void EnsureUnique(IEnumerable<string> values, string label)
    {
        var all = values.ToArray();
        if (all.Distinct(StringComparer.OrdinalIgnoreCase).Count() != all.Length)
            throw new InvalidOperationException($"Duplicate {label} code detected.");
    }
}

public static class WorldIntegrity
{
    public static void NormalizeAndValidate(WorldState world)
    {
        var households = world.Households.Values.GroupBy(h => h.Id).Select(g => g.First()).ToArray();
        world.Households.Clear();
        foreach (var household in households) world.Households[household.Id] = household;

        foreach (var npc in world.Npcs.Values)
        {
            if (!world.Locations.ContainsKey(npc.CurrentLocationId)) throw new InvalidOperationException($"NPC {npc.Id} has an unknown location.");
            if (npc.HouseholdId is Guid householdId && !world.Households.ContainsKey(householdId)) npc.HouseholdId = null;
            if (npc.PrimaryFactionId is Guid factionId && !world.Factions.ContainsKey(factionId)) npc.PrimaryFactionId = null;
        }
        if (world.PlayerNpcId is Guid playerId && !world.Npcs.ContainsKey(playerId)) throw new InvalidOperationException("Player NPC does not exist in the simulated population.");
    }
}

public static class LivingWorldFactory
{
    public static (WorldState World, GameContent Content, WorldSimulationEngine Engine) Create(int seed = 190724, int npcPopulation = 10_000)
    {
        var content = GameContent.Build(seed);
        var world = WorldBootstrap.Create(seed, npcPopulation);
        WorldIntegrity.NormalizeAndValidate(world);
        var engine = new WorldSimulationEngine(seed);
        return (world, content, engine);
    }
}
