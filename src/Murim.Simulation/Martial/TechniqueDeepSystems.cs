namespace Murim.Simulation;

public enum StanceFamily { Rooted, Mobile, Narrow, Wide, Coiled, Relaxed, Mounted, None }
public enum BreathPattern { Natural, LongCycle, ShortBurst, Held, Reverse, Rhythmic, Silent, None }
public enum ForceMethod { Direct, Whipping, Spiraling, Sinking, Explosive, Continuous, Borrowed, Precision, None }
public enum TrajectoryPattern { Linear, Arc, Circle, Spiral, BrokenRhythm, Returning, Vertical, Hidden, None }
public enum TechniqueIntent { Pressure, Intercept, Evade, BreakGuard, Control, Deceive, Endure, Heal, Craft, Observe }
public enum TechniqueTarget { Centerline, Limbs, Joints, Weapon, Balance, Meridian, Breath, Space, Material, None }

public sealed record TechniqueGrammarProfile(
    int TechniqueId,
    StanceFamily Stance,
    BreathPattern Breath,
    ForceMethod Force,
    TrajectoryPattern Trajectory,
    TechniqueIntent Intent,
    TechniqueTarget Target,
    double RhythmComplexity,
    double InternalExternalCoupling,
    double TimingStrictness,
    double RequiredReach,
    double RequiredMobility);

public sealed class TechniqueGrammarSystem
{
    private readonly Dictionary<int, TechniqueGrammarProfile> cache = new();
    public TechniqueGrammarProfile ProfileFor(TechniqueDefinition technique)
    {
        if (cache.TryGetValue(technique.Id, out var p)) return p;
        var rng = new Random(HashCode.Combine(technique.Id, technique.Code, 771));
        T Pick<T>() where T : struct, Enum { var values = Enum.GetValues<T>(); return values[rng.Next(values.Length - 1)]; }
        var internalArt = technique.Domain is TechniqueDomain.InternalCultivation or TechniqueDomain.Healing or TechniqueDomain.Qinggong or TechniqueDomain.Palm or TechniqueDomain.Finger;
        var reach = technique.Domain switch { TechniqueDomain.Spear => .92, TechniqueDomain.Staff => .78, TechniqueDomain.Archery => 1.0, TechniqueDomain.Sword => .62, TechniqueDomain.Saber => .56, TechniqueDomain.Grappling => .08, _ => .35 + rng.NextDouble() * .35 };
        var mobility = technique.Domain switch { TechniqueDomain.Qinggong or TechniqueDomain.Movement or TechniqueDomain.Leg => .82, TechniqueDomain.Grappling => .48, TechniqueDomain.Spear => .58, _ => .22 + rng.NextDouble() * .52 };
        p = new TechniqueGrammarProfile(technique.Id, Pick<StanceFamily>(), internalArt ? Pick<BreathPattern>() : BreathPattern.Natural, Pick<ForceMethod>(), Pick<TrajectoryPattern>(), Pick<TechniqueIntent>(), Pick<TechniqueTarget>(), rng.NextDouble(), internalArt ? .55 + rng.NextDouble() * .44 : rng.NextDouble() * .50, .18 + rng.NextDouble() * .80, reach, mobility);
        cache[technique.Id] = p; return p;
    }
}

public sealed record BodyCompatibilityResult(double Score, IReadOnlyList<string> Advantages, IReadOnlyList<string> Difficulties);
public sealed class TechniqueBodyCompatibilitySystem
{
    private readonly TechniqueContextSystem contextSystem = new();
    private readonly TechniqueGrammarSystem grammarSystem = new();

    public BodyCompatibilityResult Evaluate(Npc npc, TechniqueDefinition technique)
    {
        var context = contextSystem.ProfileFor(technique);
        var grammar = grammarSystem.ProfileFor(technique);
        var advantages = new List<string>(); var difficulties = new List<string>();
        var strength = npc.Physiology.Physical.StrengthPotential / 100.0;
        var mobility = (npc.Physiology.Physical.Coordination + npc.Physiology.Physical.Flexibility) / 200.0;
        var stamina = (npc.Physiology.Endurance.MuscularEndurance + npc.Physiology.Endurance.WorkCapacity) / 200.0;
        var qi = (npc.Physiology.Qi.Control + npc.Physiology.Qi.Stability) / 200.0;
        var focus = (npc.Physiology.Spirit.Focus + npc.Physiology.Spirit.Perception) / 200.0;
        double Fit(double capacity, double demand) => 1 - Math.Max(0, demand - capacity);
        var score = Fit(strength, context.StrengthDemand) * .22 + Fit(mobility, Math.Max(context.MobilityDemand, grammar.RequiredMobility)) * .24 + Fit(stamina, technique.StaminaCost / 12.0) * .16 + Fit(qi, context.QiControlDemand) * .20 + Fit(focus, context.PrecisionDemand) * .18;
        if (strength >= context.StrengthDemand + .15) advantages.Add("force physique adaptée"); else if (strength + .12 < context.StrengthDemand) difficulties.Add("demande de force élevée");
        if (mobility >= grammar.RequiredMobility + .12) advantages.Add("mobilité adaptée"); else if (mobility + .10 < grammar.RequiredMobility) difficulties.Add("mobilité insuffisante pour l'exécution idéale");
        if (qi + .12 < context.QiControlDemand) difficulties.Add("contrôle du Qi encore insuffisant");
        if (focus >= context.PrecisionDemand + .15) advantages.Add("bonne précision et lecture du rythme");
        return new BodyCompatibilityResult(Math.Clamp(score, .05, 1), advantages, difficulties);
    }
}

public sealed record TechniqueMatchupResult(double AdvantageA, string Explanation);
public sealed class TechniqueCounterSystem
{
    private readonly TechniqueContextSystem contexts = new();
    private readonly TechniqueGrammarSystem grammar = new();

    public TechniqueMatchupResult Compare(TechniqueDefinition a, TechniqueDefinition b, TechniqueExecutionContext environment)
    {
        var pa = contexts.ProfileFor(a); var pb = contexts.ProfileFor(b);
        var ga = grammar.ProfileFor(a); var gb = grammar.ProfileFor(b);
        var aRange = 1 - Math.Abs((int)pa.PreferredRange - (int)environment.Range) * .22;
        var bRange = 1 - Math.Abs((int)pb.PreferredRange - (int)environment.Range) * .22;
        var aSpace = environment.SpaceAvailable + (1 - environment.SpaceAvailable) * pa.TightSpaceSuitability;
        var bSpace = environment.SpaceAvailable + (1 - environment.SpaceAvailable) * pb.TightSpaceSuitability;
        var aFoot = environment.FootingQuality + (1 - environment.FootingQuality) * pa.BadFootingSuitability;
        var bFoot = environment.FootingQuality + (1 - environment.FootingQuality) * pb.BadFootingSuitability;
        var aTactical = pa.Deception * .24 + pa.ArmorPenetration * environment.OpponentArmor * .24 + (ga.Intent == TechniqueIntent.Intercept ? .12 : 0);
        var bTactical = pb.Deception * .24 + pb.ArmorPenetration * environment.OpponentArmor * .24 + (gb.Intent == TechniqueIntent.Intercept ? .12 : 0);
        var delta = Math.Clamp((aRange * aSpace * aFoot + aTactical) - (bRange * bSpace * bFoot + bTactical), -.75, .75);
        var explanation = Math.Abs(delta) < .08 ? "Aucun contre net dans ce contexte." : delta > 0 ? "La première technique possède un avantage contextuel, pas absolu." : "La seconde technique possède un avantage contextuel, pas absolu.";
        return new TechniqueMatchupResult(delta, explanation);
    }
}

public enum ObservationGiftTier { Normal, Gifted, Genius, LegendaryMirror }
public sealed record EraMirrorTalent(int EraStartYear, int EraEndYear, Guid NpcId, string Epithet);
public sealed record TechniqueObservationResult(double VisibleStructure, double InternalUnderstanding, double CopyFidelity, bool CanAttemptReconstruction, ObservationGiftTier GiftTier, string Summary);

/// <summary>At most one "Mirror Mind" is selected for a broad era. The gift improves observation, not bodily compatibility or available Qi.</summary>
public sealed class LegendaryMirrorMindSystem
{
    private readonly Dictionary<(int Start, int End), EraMirrorTalent> mirrors = new();
    public IReadOnlyCollection<EraMirrorTalent> Mirrors => mirrors.Values;

    public EraMirrorTalent? SelectForEra(WorldState world, int eraStartYear, int spanYears = 60)
    {
        spanYears = Math.Clamp(spanYears, 30, 120); var end = eraStartYear + spanYears - 1;
        if (mirrors.TryGetValue((eraStartYear, end), out var existing)) return existing;
        var cohort = world.Npcs.Values.Where(n => BirthYear(n) >= eraStartYear && BirthYear(n) <= end).ToArray();
        if (cohort.Length == 0) return null;
        var chosen = cohort.OrderByDescending(MirrorPotential).ThenBy(n => n.Id).First();
        var talent = new EraMirrorTalent(eraStartYear, end, chosen.Id, "Esprit du Miroir Céleste");
        mirrors[(eraStartYear, end)] = talent; return talent;
    }

    public bool IsMirror(Npc npc) => mirrors.Values.Any(x => x.NpcId == npc.Id);
    private static int BirthYear(Npc n) => (int)Math.Floor((n.Identity.BirthDay - 1) / 365.0) + 1;
    private static double MirrorPotential(Npc n) => n.Physiology.Spirit.Perception * .34 + n.Physiology.Spirit.Memory * .34 + n.Physiology.Spirit.Focus * .22 + n.Physiology.Spirit.QiSensitivity * .10;
}

public sealed class TechniqueObservationSystem
{
    private readonly TechniqueContextSystem contexts = new();

    public TechniqueObservationResult Observe(WorldState world, Npc observer, Npc performer, TechniqueDefinition technique, TechniqueExecutionContext environment, LegendaryMirrorMindSystem mirrorMinds)
    {
        if (observer.CurrentLocationId != performer.CurrentLocationId) return new TechniqueObservationResult(0, 0, 0, false, ObservationGiftTier.Normal, "La technique n'a pas été observée directement.");
        var mind = observer.Physiology.Spirit;
        var difficulty = contexts.ObservationDifficulty(technique, environment);
        var mirror = mirrorMinds.IsMirror(observer);
        var rawSight = Math.Clamp((mind.Perception * .42 + mind.Focus * .30 + mind.Memory * .28) / 100.0, 0, 1);
        var tier = mirror ? ObservationGiftTier.LegendaryMirror : rawSight >= .82 ? ObservationGiftTier.Genius : rawSight >= .68 ? ObservationGiftTier.Gifted : ObservationGiftTier.Normal;
        var visible = mirror ? Math.Clamp(.94 + rawSight * .06 - difficulty * .04, .88, 1) : Math.Clamp(rawSight * (1 - difficulty * .62), 0, .88);
        var internalUnderstanding = mirror
            ? Math.Clamp(.82 + mind.QiSensitivity / 650.0 - difficulty * .08, .72, .995)
            : Math.Clamp(visible * (mind.QiSensitivity / 100.0) * (1 - difficulty * .45), 0, .72);
        var cap = tier switch { ObservationGiftTier.Normal => .48, ObservationGiftTier.Gifted => .64, ObservationGiftTier.Genius => .79, _ => .985 };
        var fidelity = Math.Clamp((visible * .62 + internalUnderstanding * .38), 0, cap);
        var canReconstruct = fidelity >= .46;
        var summary = mirror
            ? "L'Esprit du Miroir saisit presque instantanément rythme, trajectoire et circulation visibles ; les limites du corps et du Qi restent toutefois réelles."
            : canReconstruct ? "L'observateur peut tenter une reconstruction incomplète, avec risque d'erreurs." : "L'observation ne suffit pas pour copier correctement la technique.";
        return new TechniqueObservationResult(visible, internalUnderstanding, fidelity, canReconstruct, tier, summary);
    }
}

public sealed record TechniqueSecretLayer(int TechniqueId, bool HasHiddenLayer, string PublicReading, string HiddenPrinciple, double DiscoveryDifficulty, string UnlockCondition);
public sealed class TechniqueSecretLayerSystem
{
    private readonly Dictionary<int, TechniqueSecretLayer> cache = new();
    public TechniqueSecretLayer For(TechniqueDefinition technique)
    {
        if (cache.TryGetValue(technique.Id, out var s)) return s;
        var rng = new Random(HashCode.Combine(technique.Id, 99173));
        var chance = technique.Rarity == Rarity.Ordinary ? .085 : .035;
        var hidden = rng.NextDouble() < chance;
        string[] principles = ["la respiration inverse révèle un second enchaînement", "le douzième pas doit être exécuté comme le premier", "les erreurs apparentes cachent un rythme alterné", "la circulation réelle suit les pauses plutôt que les mouvements", "l'art change lorsqu'il est pratiqué de la main opposée"];
        s = new TechniqueSecretLayer(technique.Id, hidden, hidden ? "La technique paraît plus simple qu'elle ne l'est." : "Aucune couche secrète connue.", hidden ? principles[rng.Next(principles.Length)] : string.Empty, hidden ? .58 + rng.NextDouble() * .38 : 1, hidden ? "maîtrise élevée, comparaison de manuscrits ou intuition exceptionnelle" : "aucune");
        cache[technique.Id] = s; return s;
    }
}

public sealed class MuscleMemorySystem
{
    public double ReactivationMultiplier(TechniqueProgress progress, long currentDay)
    {
        var yearsIdle = Math.Max(0, currentDay - progress.LastPracticedDay) / 365.0;
        var formerMastery = ((int)progress.Stage + 1) / 7.0;
        return Math.Clamp(1 + formerMastery * 2.4 * Math.Exp(-yearsIdle / 26.0), 1, 3.35);
    }

    public double RetainedExecutionFraction(TechniqueProgress progress, long currentDay)
    {
        var yearsIdle = Math.Max(0, currentDay - progress.LastPracticedDay) / 365.0;
        var anchor = .18 + ((int)progress.Stage / 6.0) * .48;
        return Math.Clamp(anchor + (1 - anchor) * Math.Exp(-yearsIdle / 5.5), anchor, 1);
    }
}

public sealed record TechniqueLineageEdge(int ParentTechniqueId, int ChildTechniqueId, Guid? InnovatorNpcId, long Day, double Fidelity, string ChangeReason);
public sealed class TechniqueGenealogySystem
{
    private readonly List<TechniqueLineageEdge> edges = new();
    public IReadOnlyList<TechniqueLineageEdge> Edges => edges;
    public TechniqueLineageEdge Derive(int parentId, int childId, Guid? innovatorId, long day, double fidelity, string reason)
    {
        if (parentId == childId) throw new InvalidOperationException("A technique cannot descend from itself.");
        var edge = new TechniqueLineageEdge(parentId, childId, innovatorId, day, Math.Clamp(fidelity, 0, 1), reason); edges.Add(edge); return edge;
    }
    public IReadOnlyList<TechniqueLineageEdge> AncestorsOf(int techniqueId, int maxDepth = 12)
    {
        var output = new List<TechniqueLineageEdge>(); var frontier = new HashSet<int> { techniqueId };
        for (var depth = 0; depth < maxDepth && frontier.Count > 0; depth++)
        {
            var layer = edges.Where(e => frontier.Contains(e.ChildTechniqueId)).ToArray(); output.AddRange(layer); frontier = layer.Select(e => e.ParentTechniqueId).ToHashSet();
        }
        return output;
    }
}

public sealed class EnvironmentalTechniqueAdaptation
{
    public Guid NpcId { get; init; }
    public int TechniqueId { get; init; }
    public Dictionary<TerrainCondition, double> PracticeHours { get; } = new();
}

public sealed class EnvironmentalTrainingSystem
{
    private readonly Dictionary<(Guid NpcId, int TechniqueId), EnvironmentalTechniqueAdaptation> states = new();
    public EnvironmentalTechniqueAdaptation Practice(Npc npc, TechniqueDefinition technique, TerrainCondition terrain, double hours)
    {
        var key = (npc.Id, technique.Id);
        if (!states.TryGetValue(key, out var s)) { s = new EnvironmentalTechniqueAdaptation { NpcId = npc.Id, TechniqueId = technique.Id }; states[key] = s; }
        s.PracticeHours[terrain] = s.PracticeHours.GetValueOrDefault(terrain) + Math.Max(0, hours); return s;
    }
    public double ContextBonus(Npc npc, TechniqueDefinition technique, TerrainCondition terrain)
    {
        if (!states.TryGetValue((npc.Id, technique.Id), out var s)) return 1;
        var direct = s.PracticeHours.GetValueOrDefault(terrain);
        var other = s.PracticeHours.Where(kv => kv.Key != terrain).Sum(kv => kv.Value);
        return Math.Clamp(1 + Math.Log10(1 + direct) * .09 + Math.Log10(1 + other) * .012, 1, 1.32);
    }
}
