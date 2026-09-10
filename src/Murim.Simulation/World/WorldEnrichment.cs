namespace Murim.Simulation;

public static class WorldEnrichment
{
    public static void Initialize(WorldState world, GameContent content, int seed)
    {
        var physiology = new PhysiologyGenerator(seed + 3001);
        var career = new CareerProgressionSystem();
        var random = new Random(seed + 3007);
        var professionsByCode = content.Professions.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);
        var martialPools = BuildMartialPools(content);

        foreach (var npc in world.Npcs.Values)
        {
            physiology.Initialize(npc);
            npc.Languages["common"] = new LanguageCompetency
            {
                LanguageCode = "common",
                DialectCode = DialectFor(world.Locations.GetValueOrDefault(npc.CurrentLocationId)?.Region),
                Listening = Math.Clamp(npc.AgeYears(world.Clock) * 12, 0, 100),
                Speaking = Math.Clamp((npc.AgeYears(world.Clock) - 1) * 11, 0, 100),
                Reading = npc.Skills.GetValueOrDefault("literacy"),
                Writing = npc.Skills.GetValueOrDefault("literacy") * .85
            };

            foreach (var record in npc.CareerHistory.Where(c => c.EndDay is null))
            {
                if (!professionsByCode.TryGetValue(record.ProfessionCode, out var profession)) continue;
                var standing = career.GetOrCreate(npc, profession, record.StartDay);
                standing.SeniorityDays = Math.Max(0, world.Clock.Day - record.StartDay);
                for (var i = 0; i < 6; i++)
                    if (!career.EvaluatePromotion(npc, profession, world.Clock.Day)) break;
            }

            SeedMartialHistory(npc, martialPools, random, world.Clock);
        }

        RepairGenealogy(world);
        SeedEconomy(world);
        SeedEcology(world, content, random);
        var culture = new FactionCultureSystem();
        foreach (var faction in world.Factions.Values) culture.GetOrCreate(world, faction.Id);
    }

    private static IReadOnlyDictionary<int, TechniqueDefinition[]> BuildMartialPools(GameContent content)
    {
        var martial = content.Techniques.Where(t => t.Tags.Contains("martial")).ToArray();
        var pools = new Dictionary<int, TechniqueDefinition[]>();
        for (var realm = 0; realm <= (int)MartialRealm.LifeDeath; realm++)
        {
            var maximumRecommendedRealm = realm + 1;
            pools[realm] = martial.Where(t => (int)t.RecommendedRealm <= maximumRecommendedRealm).ToArray();
        }
        return pools;
    }

    private static void SeedMartialHistory(Npc npc, IReadOnlyDictionary<int, TechniqueDefinition[]> martialPools, Random random, WorldClock clock)
    {
        var combat = npc.Skills.GetValueOrDefault("combat");
        if (npc.Identity.IsMonsterBorn || combat < 4) return;

        npc.Martial.Realm = combat switch
        {
            < 12 => MartialRealm.ThirdRate,
            < 24 => MartialRealm.SecondRate,
            < 39 => MartialRealm.FirstRate,
            < 55 => MartialRealm.Peak,
            < 69 => MartialRealm.TranscendentPeak,
            < 80 => MartialRealm.Transformation,
            < 90 => MartialRealm.Profound,
            _ => MartialRealm.LifeDeath
        };
        npc.Martial.SubRank = ((int)combat % 3) switch
        {
            0 => RealmSubRank.Entry,
            1 => RealmSubRank.Established,
            _ => RealmSubRank.Extreme
        };
        npc.Martial.StageProgress = Math.Clamp(combat * 1.7 % 100, 5, 98);
        npc.Martial.Foundation = Math.Clamp(20 + combat * .75, 10, 96);
        npc.Martial.Insight = Math.Clamp(combat * .65, 0, 95);
        npc.Physiology.Qi.Control = Math.Max(npc.Physiology.Qi.Control, Math.Clamp(combat * .9, 5, 96));
        npc.Body.Qi = Math.Max(npc.Body.Qi, combat * .45);

        var pool = martialPools.GetValueOrDefault((int)npc.Martial.Realm) ?? [];
        var wanted = Math.Clamp(1 + (int)npc.Martial.Realm / 2, 1, 5);
        foreach (var technique in SampleWithoutReplacement(pool, wanted, random))
        {
            var years = Math.Max(1, npc.AgeYears(clock) - 12);
            npc.Techniques[technique.Id] = new TechniqueProgress
            {
                TechniqueId = technique.Id,
                LearnedDay = npc.Identity.BirthDay + 12 * 365L,
                LastPracticedDay = clock.Day,
                PracticePoints = Math.Clamp(20 + combat * (1.5 + random.NextDouble() * 3), 10, 760),
                Comprehension = Math.Clamp(combat * (.75 + random.NextDouble() * .4), 3, 96),
                ExecutionConsistency = Math.Clamp(combat * (.7 + random.NextDouble() * .45), 3, 96),
                PersonalAdaptation = Math.Clamp(Math.Max(0, years - 15) * random.NextDouble() * .8, 0, 65)
            };
        }
    }

    private static IEnumerable<TechniqueDefinition> SampleWithoutReplacement(TechniqueDefinition[] pool, int count, Random random)
    {
        if (pool.Length == 0 || count <= 0) yield break;
        count = Math.Min(count, pool.Length);
        if (count == pool.Length)
        {
            foreach (var technique in pool) yield return technique;
            yield break;
        }

        var selected = new HashSet<int>();
        while (selected.Count < count) selected.Add(random.Next(pool.Length));
        foreach (var index in selected) yield return pool[index];
    }

    private static void RepairGenealogy(WorldState world)
    {
        foreach (var parent in world.Npcs.Values)
            foreach (var childId in parent.ChildIds.Distinct().ToArray())
                if (world.Npcs.TryGetValue(childId, out var child) && !child.ParentIds.Contains(parent.Id)) child.ParentIds.Add(parent.Id);

        foreach (var child in world.Npcs.Values)
            foreach (var parentId in child.ParentIds.Distinct().ToArray())
                if (world.Npcs.TryGetValue(parentId, out var parent) && !parent.ChildIds.Contains(child.Id)) parent.ChildIds.Add(child.Id);
    }

    private static void SeedEconomy(WorldState world)
    {
        foreach (var location in world.Locations.Values)
            world.RegionalEconomies[location.Id] = new RegionalEconomyState
            {
                LocationId = location.Id,
                TradeAccess = location.Safety < .3 ? .65 : 1,
                BanditPressure = Math.Max(0, .6 - location.Safety)
            };
    }

    private static void SeedEcology(WorldState world, GameContent content, Random random)
    {
        var wildLocations = world.Locations.Values.Where(l => l.Tags.Any(t => t is "monsters" or "forest" or "mountain" or "swamp")).ToArray();
        var baseMonsterForms = content.Monsters.Where(m => m.EvolutionStageIndex == 0).ToArray();
        foreach (var location in wildLocations)
        {
            foreach (var monster in SampleMonsters(baseMonsterForms, 16, random))
            {
                var key = $"{location.Id:N}:{monster.Code}";
                world.Ecology[key] = new SpeciesPopulationState
                {
                    SpeciesKey = monster.Code,
                    LocationId = location.Id,
                    Population = 8 + random.Next(60),
                    CarryingCapacity = 25 + random.Next(120),
                    Predator = monster.Diet is MonsterDiet.Carnivore or MonsterDiet.Parasite,
                    PreyKeys = []
                };
            }
        }
    }

    private static IEnumerable<MonsterDefinition> SampleMonsters(MonsterDefinition[] pool, int count, Random random)
    {
        if (pool.Length == 0 || count <= 0) yield break;
        count = Math.Min(count, pool.Length);
        var selected = new HashSet<int>();
        while (selected.Count < count) selected.Add(random.Next(pool.Length));
        foreach (var index in selected) yield return pool[index];
    }

    private static string DialectFor(string? region) => region switch
    {
        "Sud" or "Sud-est" => "southern",
        "Nord" or "Extrême Nord" => "northern",
        "Ouest" or "Extrême Ouest" => "western",
        "Est" => "eastern",
        _ => "central"
    };
}
