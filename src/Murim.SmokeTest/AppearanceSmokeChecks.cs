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
            var originalLocation = observer.CurrentLocationId;
            observer.CurrentLocationId = subject.CurrentLocationId;
            var impression = perception.Observe(world, portraits, observer, subject);
            Check(impression.Appeal is >= 0 and <= 1, "Subjective appearance impression must be normalized.");

            var otherLocation = world.Locations.Keys.FirstOrDefault(id => id != subject.CurrentLocationId);
            if (otherLocation != Guid.Empty)
            {
                observer.CurrentLocationId = otherLocation;
                var rejectedRemoteObservation = false;
                try { perception.Observe(world, portraits, observer, subject); }
                catch (InvalidOperationException) { rejectedRemoteObservation = true; }
                Check(rejectedRemoteObservation, "Appearance perception must not work remotely across locations.");
            }
            observer.CurrentLocationId = originalLocation;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
