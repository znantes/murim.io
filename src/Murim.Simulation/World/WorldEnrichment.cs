namespace Murim.Simulation;

public static class WorldEnrichment
{
    public static void Initialize(WorldState world, GameContent content, int seed)
    {
        var physiology = new PhysiologyGenerator(seed + 3001);
        var career = new CareerProgressionSystem();
        var random = new Random(seed + 3007);

        foreach (var npc in world.Npcs.Values)
        {
            physiology.Initialize(npc);
            npc.Languages["common"] = new LanguageCompetency
            {
                LanguageCode = "common", DialectCode = DialectFor(world.Locations.GetValueOrDefault(npc.CurrentLocationId)?.Region),
                Listening = Math.Clamp(npc.AgeYears(world.Clock) * 12, 0, 100), Speaking = Math.Clamp((npc.AgeYears(world.Clock) - 1) * 11, 0, 100),
                Reading = npc.Skills.GetValueOrDefault("literacy"), Writing = npc.Skills.GetValueOrDefault("literacy") * .85
            };

            foreach (var record in npc.CareerHistory.Where(c => c.EndDay is null))
            {
                var profession = content.Professions.FirstOrDefault(p => p.Code == record.ProfessionCode); if (profession is null) continue;
                var standing = career.GetOrCreate(npc, profession, record.StartDay); standing.SeniorityDays = Math.Max(0, world.Clock.Day - record.StartDay);
                for (var i = 0; i < 6; i++) if (!career.EvaluatePromotion(npc, profession, world.Clock.Day)) break;
            }

            SeedMartialHistory(npc, content, random);
        }

        RepairGenealogy(world);
        SeedEconomy(world);
        SeedEcology(world, content, random);
        foreach (var faction in world.Factions.Values) new FactionCultureSystem().GetOrCreate(world, faction.Id);
    }

    private static void SeedMartialHistory(Npc npc, GameContent content, Random random)
    {
        var combat = npc.Skills.GetValueOrDefault("combat");
        if (npc.Identity.IsMonsterBorn || combat < 4) return;
        npc.Martial.Realm = combat switch
        {
            < 12 => MartialRealm.ThirdRate, < 24 => MartialRealm.SecondRate, < 39 => MartialRealm.FirstRate, < 55 => MartialRealm.Peak,
            < 69 => MartialRealm.TranscendentPeak, < 80 => MartialRealm.Transformation, < 90 => MartialRealm.Profound, _ => MartialRealm.LifeDeath
        };
        npc.Martial.SubRank = combat % 3 switch { < 1 => RealmSubRank.Entry, < 2 => RealmSubRank.Established, _ => RealmSubRank.Extreme };
        npc.Martial.StageProgress = Math.Clamp(combat * 1.7 % 100, 5, 98);
        npc.Martial.Foundation = Math.Clamp(20 + combat * .75, 10, 96); npc.Martial.Insight = Math.Clamp(combat * .65, 0, 95);
        npc.Physiology.Qi.Control = Math.Max(npc.Physiology.Qi.Control, Math.Clamp(combat * .9, 5, 96));
        npc.Body.Qi = Math.Max(npc.Body.Qi, combat * .45);

        var allowed = content.Techniques.Where(t => t.Tags.Contains("martial") && (int)t.RecommendedRealm <= (int)npc.Martial.Realm + 1).OrderBy(_ => random.Next()).Take(Math.Clamp(1 + (int)npc.Martial.Realm / 2, 1, 5)).ToArray();
        foreach (var technique in allowed)
        {
            var years = Math.Max(1, npc.AgeYears(new WorldClock()) - 12);
            npc.Techniques[technique.Id] = new TechniqueProgress
            {
                TechniqueId = technique.Id, LearnedDay = npc.Identity.BirthDay + 12 * 365L,
                LastPracticedDay = 1, PracticePoints = Math.Clamp(20 + combat * (1.5 + random.NextDouble() * 3), 10, 760),
                Comprehension = Math.Clamp(combat * (.75 + random.NextDouble() * .4), 3, 96), ExecutionConsistency = Math.Clamp(combat * (.7 + random.NextDouble() * .45), 3, 96),
                PersonalAdaptation = Math.Clamp(Math.Max(0, years - 15) * random.NextDouble() * .8, 0, 65)
            };
        }
    }

    private static void RepairGenealogy(WorldState world)
    {
        foreach (var parent in world.Npcs.Values)
        {
            foreach (var childId in parent.ChildIds.Distinct().ToArray())
            {
                if (!world.Npcs.TryGetValue(childId, out var child)) continue;
                if (!child.ParentIds.Contains(parent.Id)) child.ParentIds.Add(parent.Id);
            }
        }
        foreach (var child in world.Npcs.Values)
        {
            foreach (var parentId in child.ParentIds.Distinct().ToArray())
            {
                if (world.Npcs.TryGetValue(parentId, out var parent) && !parent.ChildIds.Contains(child.Id)) parent.ChildIds.Add(child.Id);
            }
        }
    }

    private static void SeedEconomy(WorldState world)
    {
        foreach (var location in world.Locations.Values)
            world.RegionalEconomies[location.Id] = new RegionalEconomyState { LocationId = location.Id, TradeAccess = location.Safety < .3 ? .65 : 1, BanditPressure = Math.Max(0, .6 - location.Safety) };
    }

    private static void SeedEcology(WorldState world, GameContent content, Random random)
    {
        var wildLocations = world.Locations.Values.Where(l => l.Tags.Any(t => t is "monsters" or "forest" or "mountain" or "swamp")).ToArray();
        foreach (var location in wildLocations)
        {
            foreach (var monster in content.Monsters.Where(m => m.EvolutionStageIndex == 0).OrderBy(_ => random.Next()).Take(16))
            {
                var key = $"{location.Id:N}:{monster.Code}";
                world.Ecology[key] = new SpeciesPopulationState
                {
                    SpeciesKey = monster.Code, LocationId = location.Id, Population = 8 + random.Next(60), CarryingCapacity = 25 + random.Next(120),
                    Predator = monster.Diet is MonsterDiet.Carnivore or MonsterDiet.Parasite,
                    PreyKeys = []
                };
            }
        }
    }

    private static string DialectFor(string? region) => region switch
    {
        "Sud" or "Sud-est" => "southern", "Nord" or "Extrême Nord" => "northern", "Ouest" or "Extrême Ouest" => "western", "Est" => "eastern", _ => "central"
    };
}
