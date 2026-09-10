using Murim.Simulation;

public static class DeepWorldSmokeChecks
{
    public static void Run(WorldState world, IReadOnlyList<TechniqueDefinition> techniques)
    {
        Check(world.Locations.Count > 1, "Deep systems need more than one location.");
        var location = world.Locations.Values.First();
        var otherLocation = world.Locations.Values.First(x => x.Id != location.Id);

        var calendars = new HistoricalCalendarSystem();
        var imperial = calendars.Register("Règne du Sceau d'Azur", CalendarKind.ImperialReign, 1, "Année du règne");
        var local = calendars.Register("Après la Grande Crue", CalendarKind.LocalDisaster, 700, "Année après la crue");
        Check(calendars.Format(900, imperial) != calendars.Format(900, local), "Parallel calendars must date the same day differently.");

        var ruins = new RuinSystem();
        var ruin = ruins.Create(location, world.Clock.Day, "incendie ancien", ["puits", "stèle"]);
        ruins.Advance(ruin, world.Clock.Day + 150 * 365, .6, .02);
        Check(ruin.State is RuinState.Overgrown or RuinState.Collapsed or RuinState.Buried or RuinState.Forgotten, "Old ruins must visibly age.");

        var lineages = new LineageEvolutionSystem();
        var founders = world.Npcs.Values.Take(2).Select(n => n.Id).ToArray();
        var root = lineages.Found("Namgung", 1, founders);
        var split = lineages.Split(root, "Jin du Sud", 1500, "branche cadette partie au sud", founders.Take(1));
        lineages.FadeOriginKnowledge(split, 80 * 365, false);
        Check(split.OriginFamilyName == "Namgung" && split.CurrentFamilyName == "Jin du Sud" && split.PublicKnowledgeOfOrigin < 1, "Lineages must support renamed branches and fading ancestry knowledge.");

        var map = new MapEvolutionSystem();
        var beforeCapacity = world.Locations[location.Id].PopulationCapacity;
        map.Apply(world, location.Id, world.Clock.Day, LocationChangeKind.Expansion, "nouveau quartier marchand", .7);
        Check(world.Locations[location.Id].PopulationCapacity > beforeCapacity, "Map evolution must materially change a location.");

        var laws = new LegalMemorySystem();
        var law = laws.Enact("Décret des routes", "Les escortes doivent enregistrer les caravanes.", world.Clock.Day);
        laws.Repeal(law.Id); laws.Age(40 * 365);
        Check(laws.SearchPrecedent("escortes").Any(), "Repealed laws must remain historically searchable as precedent.");

        var sourceCulture = new RegionalCultureState { Region = "Nord" }; sourceCulture.Foods["nouilles froides"] = 1; sourceCulture.Dialects["nordique"] = 1;
        var destCulture = new RegionalCultureState { Region = "Sud" }; destCulture.Foods["nouilles froides"] = 0; destCulture.Dialects["nordique"] = 0;
        new CultureDiffusionSystem().Migrate(sourceCulture, destCulture, .20);
        Check(destCulture.Foods["nouilles froides"] > 0 && destCulture.Dialects["nordique"] > 0, "Migration must carry culture.");

        var truthSystem = new HistoricalTruthSystem();
        var truth = truthSystem.RecordTruth(world.Clock.Day, "Le général fut exécuté secrètement.", .9);
        truthSystem.RecordClaim(truth, "Le général mourut héroïquement au front.", "chronique impériale", world.Clock.Day + 20, .12, .95, true);
        Check(truthSystem.CanExposeContradiction(truth, 80, .9), "Strong evidence and scholarship should be able to expose historical lies.");

        var objects = new WorldObjectRegistry();
        var sword = objects.Create("sword_test", "Sabre de test", location.Id, world.Clock.Day);
        var holder = world.Npcs.Values.First();
        objects.Move(sword.Id, ObjectHolderKind.Npc, holder.Id, holder.CurrentLocationId, world.Clock.Day + 1);
        Check(objects.Objects[sword.Id].HolderId == holder.Id && objects.Objects[sword.Id].HolderKind == ObjectHolderKind.Npc, "Objects must occupy one concrete world state.");

        var pair = world.Npcs.Values.Where(n => n.IsAlive).Take(2).ToArray();
        Check(pair.Length == 2, "Need two NPCs for conversation memory test.");
        var original = pair[1].CurrentLocationId; pair[1].CurrentLocationId = pair[0].CurrentLocationId;
        var conversations = new ConversationMemorySystem();
        conversations.Record(world, pair[0], [pair[1]], "Je ne deviendrai jamais soldat.", .95, .8);
        Check(conversations.MemoriesOf(pair[1].Id).Count == 1, "Conversations must leave personal memories.");
        pair[1].CurrentLocationId = original;

        var codes = new SocialCodeSystem();
        var palaceLike = location with { Tags = location.Tags.Concat(["palace"]).ToArray() };
        var conduct = codes.Evaluate(codes.ForContext(palaceLike), SocialFormality.Informal, .8, true);
        Check(conduct.OffenseRisk > .3, "Social codes must make informality risky in ceremonial settings.");

        var seasons = new SeasonSystem();
        var winterClock = new WorldClock();
        winterClock.AdvanceMinutes(349 * 1440);
        var winter = seasons.ForDay(winterClock);
        Check(winter.Season == Season.Winter && winter.TravelDifficulty > .3, "Winter must affect travel.");

        var visualAge = new LocationVisualAgingSystem();
        visualAge.Advance(otherLocation.Id, 40 * 365, .5, .2, .4);
        visualAge.MarkHistoricalChange(otherLocation.Id, "ancienne porte murée");
        Check(visualAge.Get(otherLocation.Id).Weathering > 0 && visualAge.Get(otherLocation.Id).VisibleHistoryMarks.Contains("ancienne porte murée"), "Places must age visually and retain marks.");

        var portraits = new PortraitGeneticsSystem(); portraits.InitializeWorld(world);
        var appearanceEnvironment = new AppearanceEnvironmentSystem();
        var appearanceProfile = appearanceEnvironment.Update(world, portraits, holder, .4);
        Check(appearanceProfile.GeneticStructureWeight > .5 && appearanceProfile.CurrentPresentation is >= 0 and <= 1, "Appearance must separate inherited structure from living conditions.");

        var techA = techniques.First(); var techB = techniques.Skip(1).First();
        var grammar = new TechniqueGrammarSystem();
        Check(grammar.ProfileFor(techA).TechniqueId == techA.Id, "Technique grammar must be deterministic and attached to the technique.");
        var compatibility = new TechniqueBodyCompatibilitySystem().Evaluate(holder, techA);
        Check(compatibility.Score is > 0 and <= 1, "Body compatibility must be normalized.");
        var context = new TechniqueExecutionContext(TechniqueRangeBand.Close, TerrainCondition.Boat, .5, .35, .8, .2, .5, .6, .4);
        var matchup = new TechniqueCounterSystem().Compare(techA, techB, context);
        Check(matchup.AdvantageA is >= -.75 and <= .75, "Technique counters must be contextual rather than absolute.");

        var mirrorSystem = new LegendaryMirrorMindSystem();
        var birthYear = world.Npcs.Values.Select(n => (int)Math.Floor((n.Identity.BirthDay - 1) / 365.0) + 1).GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;
        var mirror = mirrorSystem.SelectForEra(world, birthYear, 60);
        Check(mirror is not null && mirrorSystem.Mirrors.Count == 1, "An era may have only one selected legendary mirror mind.");
        var mirrorNpc = world.Npcs[mirror!.NpcId]; var performer = world.Npcs.Values.First(n => n.Id != mirrorNpc.Id);
        var oldLocation = mirrorNpc.CurrentLocationId; mirrorNpc.CurrentLocationId = performer.CurrentLocationId;
        var observed = new TechniqueObservationSystem().Observe(world, mirrorNpc, performer, techA, context, mirrorSystem);
        Check(observed.GiftTier == ObservationGiftTier.LegendaryMirror && observed.CopyFidelity >= .80, "Legendary mirror mind must observe with exceptional fidelity.");
        mirrorNpc.CurrentLocationId = oldLocation;

        var secret = new TechniqueSecretLayerSystem().For(techA);
        Check(secret.TechniqueId == techA.Id, "Secret-layer profile must stay attached to its technique.");

        var oldProgress = new TechniqueProgress { TechniqueId = techA.Id, LearnedDay = 1, LastPracticedDay = 365, PracticePoints = 700, Comprehension = 90, ExecutionConsistency = 90 };
        var memory = new MuscleMemorySystem();
        Check(memory.ReactivationMultiplier(oldProgress, 20 * 365) > 1, "Former masters must relearn faster through muscle memory.");

        var genealogy = new TechniqueGenealogySystem();
        genealogy.Derive(techA.Id, 999999, holder.Id, world.Clock.Day, .81, "branche familiale");
        Check(genealogy.AncestorsOf(999999).Count == 1, "Technique genealogy must track derivation.");

        var environmental = new EnvironmentalTrainingSystem();
        environmental.Practice(holder, techA, TerrainCondition.Boat, 500);
        Check(environmental.ContextBonus(holder, techA, TerrainCondition.Boat) > environmental.ContextBonus(holder, techA, TerrainCondition.Snow), "Environmental training must stay primarily context-specific.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
