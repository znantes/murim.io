namespace Murim.Simulation;

public enum FacialGrowthStage { Infant, Child, Adolescent, YoungAdult, MatureAdult, Elder }
public enum BodyBuildCategory { VeryLean, Lean, Average, Broad, Round, VeryRound }

public sealed class PortraitGenome
{
    public double FaceWidth { get; set; }
    public double JawWidth { get; set; }
    public double JawLength { get; set; }
    public double CheekboneHeight { get; set; }
    public double CheekboneWidth { get; set; }
    public double NoseWidth { get; set; }
    public double NoseLength { get; set; }
    public double NoseBridge { get; set; }
    public double EyeSize { get; set; }
    public double EyeSpacing { get; set; }
    public double EyeTilt { get; set; }
    public double BrowHeight { get; set; }
    public double LipFullness { get; set; }
    public double ChinProjection { get; set; }
    public double EarSize { get; set; }
    public double SkinTone { get; set; }
    public double HairPigment { get; set; }
    public double EyePigment { get; set; }
    public double HairWave { get; set; }
    public double HairDensity { get; set; }
    public double FacialHairPotential { get; set; }
    public double FrecklePotential { get; set; }
    public double ScarTendency { get; set; }

    // Diversity parameters. They deliberately prevent the renderer from converging on one idealized face.
    public double FacialAsymmetry { get; set; }
    public double FacialAdipositySetPoint { get; set; }
    public double SkinTextureVariation { get; set; }
    public double BlemishPotential { get; set; }
    public double UnderEyeDepth { get; set; }
    public double DentalIrregularity { get; set; }
    public double FacialDistinctiveness { get; set; }

    // Useful later for full-body portraits / character cards.
    public double BodyFrameWidth { get; set; }
    public double BodyAdipositySetPoint { get; set; }
    public double HeightPotential { get; set; }
    public double MuscleResponse { get; set; }
    public double ShoulderHipBalance { get; set; }

    // The same genes are expressed differently through growth.
    public double ChildhoodRoundness { get; set; }
    public double AdolescentLengthening { get; set; }
    public double AdultBoneDefinition { get; set; }
    public double AgingSoftTissueChange { get; set; }

    public int HairStyleSeed { get; set; }
    public PortraitGenome Clone() => (PortraitGenome)MemberwiseClone();
}

public sealed class PortraitAppearanceState
{
    public double ApparentAge { get; set; }
    public double WrinkleAmount { get; set; }
    public double GreyHairAmount { get; set; }
    public double SunExposure { get; set; }
    public double Pallor { get; set; }
    public double Bruising { get; set; }
    public double FacialWeight { get; set; }
    public double SkinRoughness { get; set; }
    public double BlemishAmount { get; set; }
    public double EyeBagAmount { get; set; }
    public double FacialSagging { get; set; }
    public double TemporarySwelling { get; set; }
    public FacialGrowthStage GrowthStage { get; set; }
    public List<FacialMark> Marks { get; } = new();
}

public sealed record FacialMark(string Code, string Description, double X, double Y, double Size, bool Permanent);

public sealed class PortraitGeneticsSystem
{
    private readonly Dictionary<Guid, PortraitGenome> genomes = new();
    private readonly Dictionary<Guid, PortraitAppearanceState> appearances = new();

    public void InitializeWorld(WorldState world)
    {
        foreach (var npc in world.Npcs.Values) GetOrCreate(npc);
        foreach (var child in world.Npcs.Values.Where(n => n.ParentIds.Count >= 2))
        {
            if (!world.Npcs.TryGetValue(child.ParentIds[0], out var a) || !world.Npcs.TryGetValue(child.ParentIds[1], out var b)) continue;
            Inherit(child, a, b);
        }
        foreach (var npc in world.Npcs.Values) UpdateVisibleAge(npc, world.Clock);
    }

    public PortraitGenome GetOrCreate(Npc npc)
    {
        if (genomes.TryGetValue(npc.Id, out var existing)) return existing;
        var created = CreateFounder(npc.Id, npc.HouseholdId);
        genomes[npc.Id] = created;
        return created;
    }

    public PortraitAppearanceState AppearanceFor(Npc npc)
    {
        if (appearances.TryGetValue(npc.Id, out var existing)) return existing;
        var created = new PortraitAppearanceState();
        appearances[npc.Id] = created;
        return created;
    }

    public PortraitGenome CreateFounder(Guid npcId, Guid? householdId = null)
    {
        var individual = new Random(HashSeed(npcId, householdId));
        var family = householdId is Guid h ? new Random(HashSeed(h, null)) : null;
        double Trait(double spread = .18)
        {
            var personal = Bell(individual, spread);
            if (family is null) return personal;
            var familial = Bell(family, spread);
            return Math.Clamp(personal * .72 + familial * .28, 0, 1);
        }
        double RareAsymmetry()
        {
            var baseValue = Math.Abs(NextGaussian(individual)) * .055;
            if (individual.NextDouble() < .035) baseValue += .12 + individual.NextDouble() * .20;
            return Math.Clamp(baseValue, 0, .55);
        }
        double BroadDistribution(double center = .5, double spread = .24) => Math.Clamp(center + NextGaussian(individual) * spread, 0, 1);

        return new PortraitGenome
        {
            FaceWidth = Trait(.20), JawWidth = Trait(.20), JawLength = Trait(.20), CheekboneHeight = Trait(), CheekboneWidth = Trait(.20),
            NoseWidth = Trait(.21), NoseLength = Trait(.21), NoseBridge = Trait(.20), EyeSize = Trait(.20), EyeSpacing = Trait(.17), EyeTilt = Trait(.18),
            BrowHeight = Trait(.18), LipFullness = Trait(.22), ChinProjection = Trait(.21), EarSize = Trait(.20), SkinTone = Trait(.24),
            HairPigment = Trait(.23), EyePigment = Trait(.21), HairWave = Trait(.26), HairDensity = Trait(.22), FacialHairPotential = Trait(.27),
            FrecklePotential = BroadDistribution(.30, .28), ScarTendency = BroadDistribution(.35, .24),
            FacialAsymmetry = RareAsymmetry(), FacialAdipositySetPoint = BroadDistribution(), SkinTextureVariation = BroadDistribution(.38, .22),
            BlemishPotential = BroadDistribution(.27, .25), UnderEyeDepth = BroadDistribution(.34, .22), DentalIrregularity = BroadDistribution(.24, .24),
            FacialDistinctiveness = BroadDistribution(.50, .23), BodyFrameWidth = BroadDistribution(), BodyAdipositySetPoint = BroadDistribution(.48, .26),
            HeightPotential = BroadDistribution(), MuscleResponse = BroadDistribution(), ShoulderHipBalance = BroadDistribution(),
            ChildhoodRoundness = BroadDistribution(.68, .16), AdolescentLengthening = BroadDistribution(.55, .17),
            AdultBoneDefinition = BroadDistribution(.50, .20), AgingSoftTissueChange = BroadDistribution(.50, .22), HairStyleSeed = individual.Next()
        };
    }

    public PortraitGenome Inherit(Npc child, Npc parentA, Npc parentB)
    {
        var created = Inherit(GetOrCreate(parentA), GetOrCreate(parentB), child.Id);
        genomes[child.Id] = created;
        return created;
    }

    public PortraitGenome Inherit(PortraitGenome a, PortraitGenome b, Guid childId)
    {
        var rng = new Random(HashSeed(childId, null));
        double Mix(double x, double y, double mutationScale = .04)
        {
            var dominance = .28 + rng.NextDouble() * .44;
            var mutation = NextGaussian(rng) * mutationScale;
            return Math.Clamp(x * dominance + y * (1 - dominance) + mutation, 0, 1);
        }
        return new PortraitGenome
        {
            FaceWidth = Mix(a.FaceWidth, b.FaceWidth), JawWidth = Mix(a.JawWidth, b.JawWidth), JawLength = Mix(a.JawLength, b.JawLength),
            CheekboneHeight = Mix(a.CheekboneHeight, b.CheekboneHeight), CheekboneWidth = Mix(a.CheekboneWidth, b.CheekboneWidth),
            NoseWidth = Mix(a.NoseWidth, b.NoseWidth), NoseLength = Mix(a.NoseLength, b.NoseLength), NoseBridge = Mix(a.NoseBridge, b.NoseBridge),
            EyeSize = Mix(a.EyeSize, b.EyeSize), EyeSpacing = Mix(a.EyeSpacing, b.EyeSpacing, .025), EyeTilt = Mix(a.EyeTilt, b.EyeTilt),
            BrowHeight = Mix(a.BrowHeight, b.BrowHeight), LipFullness = Mix(a.LipFullness, b.LipFullness), ChinProjection = Mix(a.ChinProjection, b.ChinProjection),
            EarSize = Mix(a.EarSize, b.EarSize), SkinTone = Mix(a.SkinTone, b.SkinTone, .025), HairPigment = Mix(a.HairPigment, b.HairPigment, .025),
            EyePigment = Mix(a.EyePigment, b.EyePigment, .025), HairWave = Mix(a.HairWave, b.HairWave), HairDensity = Mix(a.HairDensity, b.HairDensity),
            FacialHairPotential = Mix(a.FacialHairPotential, b.FacialHairPotential), FrecklePotential = Mix(a.FrecklePotential, b.FrecklePotential),
            ScarTendency = Mix(a.ScarTendency, b.ScarTendency), FacialAsymmetry = Mix(a.FacialAsymmetry, b.FacialAsymmetry, .025),
            FacialAdipositySetPoint = Mix(a.FacialAdipositySetPoint, b.FacialAdipositySetPoint), SkinTextureVariation = Mix(a.SkinTextureVariation, b.SkinTextureVariation),
            BlemishPotential = Mix(a.BlemishPotential, b.BlemishPotential), UnderEyeDepth = Mix(a.UnderEyeDepth, b.UnderEyeDepth),
            DentalIrregularity = Mix(a.DentalIrregularity, b.DentalIrregularity), FacialDistinctiveness = Mix(a.FacialDistinctiveness, b.FacialDistinctiveness),
            BodyFrameWidth = Mix(a.BodyFrameWidth, b.BodyFrameWidth), BodyAdipositySetPoint = Mix(a.BodyAdipositySetPoint, b.BodyAdipositySetPoint),
            HeightPotential = Mix(a.HeightPotential, b.HeightPotential), MuscleResponse = Mix(a.MuscleResponse, b.MuscleResponse),
            ShoulderHipBalance = Mix(a.ShoulderHipBalance, b.ShoulderHipBalance), ChildhoodRoundness = Mix(a.ChildhoodRoundness, b.ChildhoodRoundness),
            AdolescentLengthening = Mix(a.AdolescentLengthening, b.AdolescentLengthening), AdultBoneDefinition = Mix(a.AdultBoneDefinition, b.AdultBoneDefinition),
            AgingSoftTissueChange = Mix(a.AgingSoftTissueChange, b.AgingSoftTissueChange), HairStyleSeed = rng.Next()
        };
    }

    public void UpdateVisibleAge(Npc npc, WorldClock clock)
    {
        var age = npc.AgeYears(clock);
        var g = GetOrCreate(npc);
        var a = AppearanceFor(npc);
        a.GrowthStage = GrowthStageAt(age);
        a.ApparentAge = Math.Max(0, age + npc.Body.Fatigue * .025 + a.SunExposure * .04 - npc.Physiology.Endurance.RecoveryEfficiency * .015);
        a.WrinkleAmount = Math.Clamp((a.ApparentAge - 27) / 55.0, 0, 1) * (.65 + g.AgingSoftTissueChange * .7);
        a.GreyHairAmount = Math.Clamp((a.ApparentAge - 36) / 50.0, 0, 1);
        a.Pallor = Math.Clamp((70 - npc.Body.Health) / 70.0, 0, .8);
        a.Bruising = Math.Clamp(npc.Injuries.Where(x => x.Active && x.Region is BodyRegion.Face or BodyRegion.Head).Sum(x => (int)x.Severity) * .15, 0, 1);
        a.FacialWeight = Math.Clamp(g.FacialAdipositySetPoint + (age < 7 ? g.ChildhoodRoundness * .18 : 0) - Math.Max(0, npc.Body.Hunger - 60) / 240.0, 0, 1);
        a.SkinRoughness = Math.Clamp(g.SkinTextureVariation * .55 + a.WrinkleAmount * .48 + a.SunExposure * .004, 0, 1);
        var adolescentFactor = age is >= 11 and <= 22 ? 1.0 : .35;
        a.BlemishAmount = Math.Clamp(g.BlemishPotential * adolescentFactor + npc.Body.Fatigue / 420.0, 0, 1);
        a.EyeBagAmount = Math.Clamp(g.UnderEyeDepth * .35 + npc.Body.SleepDebt / 120.0 + npc.Body.Fatigue / 180.0 + a.WrinkleAmount * .25, 0, 1);
        a.FacialSagging = Math.Clamp((a.ApparentAge - 40) / 55.0, 0, 1) * g.AgingSoftTissueChange;
    }

    public static FacialGrowthStage GrowthStageAt(int age) => age switch
    {
        < 3 => FacialGrowthStage.Infant,
        < 10 => FacialGrowthStage.Child,
        < 18 => FacialGrowthStage.Adolescent,
        < 32 => FacialGrowthStage.YoungAdult,
        < 60 => FacialGrowthStage.MatureAdult,
        _ => FacialGrowthStage.Elder
    };

    public BodyBuildCategory BodyBuildFor(Npc npc)
    {
        var g = GetOrCreate(npc);
        return g.BodyAdipositySetPoint switch
        {
            < .16 => BodyBuildCategory.VeryLean,
            < .32 => BodyBuildCategory.Lean,
            < .62 => BodyBuildCategory.Average,
            < .76 => BodyBuildCategory.Broad,
            < .91 => BodyBuildCategory.Round,
            _ => BodyBuildCategory.VeryRound
        };
    }

    public void SyncPermanentMarks(Npc npc)
    {
        var appearance = AppearanceFor(npc);
        foreach (var injury in npc.Injuries.Where(i => i.Permanent && i.Region is BodyRegion.Face or BodyRegion.Head))
        {
            var code = $"injury:{injury.Id}";
            if (appearance.Marks.Any(m => m.Code == code)) continue;
            var rng = new Random(HashSeed(injury.Id, null));
            appearance.Marks.Add(new FacialMark(code, injury.DisplayText, .2 + rng.NextDouble() * .6, .18 + rng.NextDouble() * .62, .03 + rng.NextDouble() * .10, true));
        }
    }

    public IReadOnlyDictionary<string, double> GrowthBlendShapeWeights(Npc npc, int age)
    {
        var g = GetOrCreate(npc);
        double Infant() => age >= 3 ? 0 : 1 - age / 3.0;
        double Child() => age < 3 ? age / 3.0 : age < 10 ? 1 - (age - 3) / 7.0 * .35 : Math.Max(0, 1 - (age - 10) / 5.0);
        double Adolescent() => age < 9 ? 0 : age < 18 ? Math.Clamp((age - 9) / 9.0, 0, 1) : Math.Clamp(1 - (age - 18) / 8.0, 0, 1);
        double Mature() => Math.Clamp((age - 28) / 24.0, 0, 1);
        double Elder() => Math.Clamp((age - 55) / 30.0, 0, 1);
        return new Dictionary<string, double>
        {
            ["growth_infant"] = Infant(),
            ["growth_child_roundness"] = Child() * g.ChildhoodRoundness,
            ["growth_adolescent_lengthening"] = Adolescent() * g.AdolescentLengthening,
            ["growth_adult_definition"] = Math.Clamp((age - 17) / 12.0, 0, 1) * g.AdultBoneDefinition,
            ["growth_mature"] = Mature(),
            ["growth_elder_soft_tissue"] = Elder() * g.AgingSoftTissueChange
        };
    }

    public IReadOnlyDictionary<string, double> BlendShapeWeights(Npc npc)
    {
        var g = GetOrCreate(npc);
        var a = AppearanceFor(npc);
        var weights = new Dictionary<string, double>
        {
            ["face_width"] = g.FaceWidth, ["jaw_width"] = g.JawWidth, ["jaw_length"] = g.JawLength,
            ["cheekbone_height"] = g.CheekboneHeight, ["cheekbone_width"] = g.CheekboneWidth,
            ["nose_width"] = g.NoseWidth, ["nose_length"] = g.NoseLength, ["nose_bridge"] = g.NoseBridge,
            ["eye_size"] = g.EyeSize, ["eye_spacing"] = g.EyeSpacing, ["eye_tilt"] = g.EyeTilt,
            ["brow_height"] = g.BrowHeight, ["lip_fullness"] = g.LipFullness, ["chin_projection"] = g.ChinProjection,
            ["ear_size"] = g.EarSize, ["face_asymmetry"] = g.FacialAsymmetry, ["facial_adiposity"] = a.FacialWeight,
            ["skin_texture"] = a.SkinRoughness, ["skin_blemishes"] = a.BlemishAmount, ["under_eye"] = a.EyeBagAmount,
            ["age_wrinkles"] = a.WrinkleAmount, ["age_sagging"] = a.FacialSagging, ["grey_hair"] = a.GreyHairAmount,
            ["pallor"] = a.Pallor, ["bruising"] = a.Bruising, ["swelling"] = a.TemporarySwelling
        };
        foreach (var pair in GrowthBlendShapeWeights(npc, (int)Math.Round(a.ApparentAge))) weights[pair.Key] = pair.Value;
        return weights;
    }

    public static double StructuralDistinctiveness(PortraitGenome g)
    {
        var traits = new[] { g.FaceWidth, g.JawWidth, g.JawLength, g.CheekboneHeight, g.NoseWidth, g.NoseLength, g.EyeSize, g.EyeSpacing, g.LipFullness, g.ChinProjection };
        return Math.Clamp(traits.Average(x => Math.Abs(x - .5) * 2) * .75 + g.FacialDistinctiveness * .25, 0, 1);
    }

    private static double Bell(Random rng, double spread = .16) => Math.Clamp(.5 + NextGaussian(rng) * spread, 0, 1);
    private static double NextGaussian(Random rng)
    {
        var u1 = Math.Max(1e-12, rng.NextDouble());
        var u2 = rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }
    private static int HashSeed(Guid id, Guid? secondary)
    {
        var h = 17;
        foreach (var b in id.ToByteArray()) h = unchecked(h * 31 + b);
        if (secondary is Guid s) foreach (var b in s.ToByteArray()) h = unchecked(h * 31 + b);
        return h;
    }
}
