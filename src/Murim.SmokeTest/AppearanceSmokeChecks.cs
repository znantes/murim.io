using Murim.Simulation;

public static class AppearanceSmokeChecks
{
    public static void Run(WorldState world)
    {
        var portraits = new PortraitGeneticsSystem();
        portraits.InitializeWorld(world);
        var sample = world.Npcs.Values.Take(5_000).ToArray();
        var genomes = sample.Select(portraits.GetOrCreate).ToArray();

        Check(genomes.Any(g => g.BodyAdipositySetPoint >= .82), "Population must include naturally round/heavier body builds.");
        Check(genomes.Any(g => g.BodyAdipositySetPoint <= .20), "Population must include naturally lean body builds.");
        Check(genomes.Any(g => g.FacialAdipositySetPoint >= .78), "Population must include rounder faces.");
        Check(genomes.Any(g => g.FacialAdipositySetPoint <= .22), "Population must include angular/leaner faces.");
        Check(genomes.Any(g => g.FacialAsymmetry >= .16), "Population must include visible facial asymmetry instead of uniformly idealized faces.");
        Check(genomes.Any(g => g.BlemishPotential >= .65), "Population must include substantial skin variation.");

        var anyNpc = sample[0];
        var infant = portraits.GrowthBlendShapeWeights(anyNpc, 1);
        var teen = portraits.GrowthBlendShapeWeights(anyNpc, 15);
        var elder = portraits.GrowthBlendShapeWeights(anyNpc, 72);
        Check(infant["growth_infant"] > .5, "Infant face morph must differ structurally from adult face.");
        Check(teen["growth_adolescent_lengthening"] > .25, "Adolescent face must have a dedicated growth morph.");
        Check(elder["growth_elder_soft_tissue"] > 0, "Elder face must have age-related soft-tissue change.");
        Check(PortraitGeneticsSystem.GrowthStageAt(1) == FacialGrowthStage.Infant && PortraitGeneticsSystem.GrowthStageAt(15) == FacialGrowthStage.Adolescent && PortraitGeneticsSystem.GrowthStageAt(70) == FacialGrowthStage.Elder,
            "Growth-stage classification is inconsistent.");

        Check(AppearancePerceptionSystem.MaxLegendaryBeauties == 4, "Legendary Murim beauties must remain fewer than five.");
        var subject = world.Npcs.Values.FirstOrDefault(n => n.IsAlive && n.Identity.Sex == Sex.Female && n.AgeYears(world.Clock) >= 18);
        var observer = world.Npcs.Values.FirstOrDefault(n => n.IsAlive && n.Id != subject?.Id && n.AgeYears(world.Clock) >= 18);
        if (subject is not null && observer is not null)
        {
            var perception = new AppearancePerceptionSystem();
            var originalObserverLocation = observer.CurrentLocationId;
            observer.CurrentLocationId = subject.CurrentLocationId;
            var impression = perception.Observe(world, portraits, observer, subject);
            Check(impression.Appeal is >= 0 and <= 1, "Subjective appearance impression must be normalized.");

            var attention = new AttentionPerceptionSystem();
            var goodObservation = attention.ObservePerson(observer, subject, new SensoryContext(SensoryChannel.Vision, LightQuality: 1, Noise: 0, Distance: .05, CrowdDensity: .05, Obstruction: 0, AttentionAllocation: 1, SubjectSalience: .9), world.Clock.Day);
            var poorObservation = attention.ObservePerson(observer, subject, new SensoryContext(SensoryChannel.Vision, LightQuality: .12, Noise: .8, Distance: .9, CrowdDensity: .9, Obstruction: .8, AttentionAllocation: .15, SubjectSalience: .2), world.Clock.Day);
            Check(goodObservation.Confidence > poorObservation.Confidence, "Attention/perception context must change observed detail.");
            Check(attention.AttentionSlots(observer) is >= 1 and <= 6, "Attention budget must be bounded.");

            var otherLocation = world.Locations.Keys.FirstOrDefault(id => id != subject.CurrentLocationId);
            if (otherLocation != Guid.Empty)
            {
                observer.CurrentLocationId = otherLocation;
                var rejectedRemoteObservation = false;
                try { perception.Observe(world, portraits, observer, subject); }
                catch (InvalidOperationException) { rejectedRemoteObservation = true; }
                Check(rejectedRemoteObservation, "Appearance perception must not work remotely across locations.");
                Check(attention.ObservePerson(observer, subject, new SensoryContext(SensoryChannel.Vision), world.Clock.Day).Detail == ObservationDetailLevel.Missed,
                    "General perception must not reveal a remote NPC.");
            }
            observer.CurrentLocationId = originalObserverLocation;
        }

        CheckHistoryCausality(world);
        CheckTechniqueContext(sample.First(n => n.AgeYears(world.Clock) >= 18));
    }

    private static void CheckHistoryCausality(WorldState world)
    {
        var locationId = world.Locations.Keys.First();
        var cause = new WorldEvent(Guid.NewGuid(), WorldEventType.Drought, world.Clock.Day, locationId, [], [], "Sécheresse de test.", .8, .8, .1, []);
        var effect = new WorldEvent(Guid.NewGuid(), WorldEventType.Famine, world.Clock.Day + 20, locationId, [], [], "Famine de test.", .9, .9, .1, []);
        world.Events.Add(cause);
        world.Events.Add(effect);
        var causality = new EventCausalitySystem();
        var mechanism = causality.SuggestMechanism(cause, effect);
        Check(mechanism == CausalMechanism.ResourceShortage, "Drought -> famine should suggest a resource-shortage causal mechanism.");
        causality.Link(world, cause.Id, effect.Id, mechanism, .88, "La sécheresse détruit une partie des récoltes.");
        var roots = causality.TraceRootCauses(world, effect.Id);
        Check(roots.Any(x => x.Event.Id == cause.Id && x.Depth == 1), "Historical causal graph must recover recorded root causes.");
    }

    private static void CheckTechniqueContext(Npc actor)
    {
        var technique = new TechniqueDefinition(991001, "CONTEXT-SWORD", "Lame contextuelle de test", TechniqueDomain.Sword, Rarity.Rare, 1.2, .55, 2, 2, .03, MartialRealm.FirstRate, ["martial"]);
        var contexts = new TechniqueContextSystem();
        var profileA = contexts.ProfileFor(technique);
        var profileB = contexts.ProfileFor(technique);
        Check(profileA == profileB, "Technique contextual profile must be deterministic.");
        var stable = contexts.EffectiveContextMultiplier(actor, technique, new TechniqueExecutionContext(profileA.PreferredRange, TerrainCondition.Stable, SpaceAvailable: 1, FootingQuality: 1, Visibility: 1, ToolQuality: 1));
        var unstable = contexts.EffectiveContextMultiplier(actor, technique, new TechniqueExecutionContext(profileA.PreferredRange, TerrainCondition.Ice, SpaceAvailable: 1, FootingQuality: .05, Visibility: 1, ToolQuality: 1));
        Check(stable >= unstable, "Bad footing must not improve a technique unless a later explicit adaptation system says so.");
        Check(contexts.ObservationDifficulty(technique, new TechniqueExecutionContext(profileA.PreferredRange, Visibility: .15, OpponentMobility: .9, TimePressure: .9)) > 0,
            "Technique observation must expose contextual difficulty.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
