namespace Murim.Simulation;

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
    public List<FacialMark> Marks { get; } = new();
}

public sealed record FacialMark(string Code, string Description, double X, double Y, double Size, bool Permanent);

public sealed class PortraitGeneticsSystem
{
    private readonly Dictionary<Guid, PortraitGenome> genomes = new();
    private readonly Dictionary<Guid, PortraitAppearanceState> appearances = new();

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
        var rng = new Random(HashSeed(npcId, householdId));
        return new PortraitGenome
        {
            FaceWidth = Bell(rng), JawWidth = Bell(rng), JawLength = Bell(rng), CheekboneHeight = Bell(rng), CheekboneWidth = Bell(rng),
            NoseWidth = Bell(rng), NoseLength = Bell(rng), NoseBridge = Bell(rng), EyeSize = Bell(rng), EyeSpacing = Bell(rng), EyeTilt = Bell(rng),
            BrowHeight = Bell(rng), LipFullness = Bell(rng), ChinProjection = Bell(rng), EarSize = Bell(rng), SkinTone = Bell(rng),
            HairPigment = Bell(rng), EyePigment = Bell(rng), HairWave = Bell(rng), HairDensity = Bell(rng), FacialHairPotential = Bell(rng),
            FrecklePotential = Bell(rng), ScarTendency = Bell(rng), HairStyleSeed = rng.Next()
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
        double Mix(double x, double y)
        {
            var dominance = .35 + rng.NextDouble() * .30;
            var mutation = NextGaussian(rng) * .035;
            return Math.Clamp(x * dominance + y * (1 - dominance) + mutation, 0, 1);
        }
        return new PortraitGenome
        {
            FaceWidth = Mix(a.FaceWidth, b.FaceWidth), JawWidth = Mix(a.JawWidth, b.JawWidth), JawLength = Mix(a.JawLength, b.JawLength),
            CheekboneHeight = Mix(a.CheekboneHeight, b.CheekboneHeight), CheekboneWidth = Mix(a.CheekboneWidth, b.CheekboneWidth),
            NoseWidth = Mix(a.NoseWidth, b.NoseWidth), NoseLength = Mix(a.NoseLength, b.NoseLength), NoseBridge = Mix(a.NoseBridge, b.NoseBridge),
            EyeSize = Mix(a.EyeSize, b.EyeSize), EyeSpacing = Mix(a.EyeSpacing, b.EyeSpacing), EyeTilt = Mix(a.EyeTilt, b.EyeTilt),
            BrowHeight = Mix(a.BrowHeight, b.BrowHeight), LipFullness = Mix(a.LipFullness, b.LipFullness), ChinProjection = Mix(a.ChinProjection, b.ChinProjection),
            EarSize = Mix(a.EarSize, b.EarSize), SkinTone = Mix(a.SkinTone, b.SkinTone), HairPigment = Mix(a.HairPigment, b.HairPigment),
            EyePigment = Mix(a.EyePigment, b.EyePigment), HairWave = Mix(a.HairWave, b.HairWave), HairDensity = Mix(a.HairDensity, b.HairDensity),
            FacialHairPotential = Mix(a.FacialHairPotential, b.FacialHairPotential), FrecklePotential = Mix(a.FrecklePotential, b.FrecklePotential),
            ScarTendency = Mix(a.ScarTendency, b.ScarTendency), HairStyleSeed = rng.Next()
        };
    }

    public void UpdateVisibleAge(Npc npc, WorldClock clock)
    {
        var age = npc.AgeYears(clock);
        var appearance = AppearanceFor(npc);
        appearance.ApparentAge = Math.Max(0, age + npc.Body.Fatigue * .025 + appearance.SunExposure * .04 - npc.Physiology.Endurance.RecoveryEfficiency * .015);
        appearance.WrinkleAmount = Math.Clamp((appearance.ApparentAge - 28) / 55.0, 0, 1);
        appearance.GreyHairAmount = Math.Clamp((appearance.ApparentAge - 38) / 48.0, 0, 1);
        appearance.Pallor = Math.Clamp((70 - npc.Body.Health) / 70.0, 0, .8);
        appearance.Bruising = Math.Clamp(npc.Injuries.Where(x => x.Active && x.Region is BodyRegion.Face or BodyRegion.Head).Sum(x => (int)x.Severity) * .15, 0, 1);
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

    public IReadOnlyDictionary<string, double> BlendShapeWeights(Npc npc)
    {
        var g = GetOrCreate(npc);
        var a = AppearanceFor(npc);
        return new Dictionary<string, double>
        {
            ["face_width"] = g.FaceWidth, ["jaw_width"] = g.JawWidth, ["jaw_length"] = g.JawLength,
            ["cheekbone_height"] = g.CheekboneHeight, ["cheekbone_width"] = g.CheekboneWidth,
            ["nose_width"] = g.NoseWidth, ["nose_length"] = g.NoseLength, ["nose_bridge"] = g.NoseBridge,
            ["eye_size"] = g.EyeSize, ["eye_spacing"] = g.EyeSpacing, ["eye_tilt"] = g.EyeTilt,
            ["brow_height"] = g.BrowHeight, ["lip_fullness"] = g.LipFullness, ["chin_projection"] = g.ChinProjection,
            ["ear_size"] = g.EarSize, ["age_wrinkles"] = a.WrinkleAmount, ["facial_weight"] = a.FacialWeight
        };
    }

    private static double Bell(Random rng) => Math.Clamp(.5 + NextGaussian(rng) * .16, 0, 1);
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
