namespace Murim.Simulation;

public enum BodySide { Left, Right, Center }
public enum BodyRegion
{
    Head, Face, Neck, Chest, Abdomen, Back, Shoulder, UpperArm, Elbow, Forearm, Wrist, Hand, Fingers,
    Hip, Thigh, Knee, LowerLeg, Ankle, Foot, Toes, Eye, Ear, InternalOrgans, MeridianNetwork, Dantian
}
public enum InjuryKind { Bruise, Cut, Laceration, Burn, Fracture, Dislocation, InternalTrauma, Concussion, NerveDamage, Scar, MeridianTrauma, QiDeviation, Amputation }
public enum InjurySeverity { Minor = 1, Light = 2, Moderate = 3, Severe = 4, Critical = 5 }
public enum QiMutationRarity { None, Uncommon, Rare, VeryRare, Mythic, Divine, Impossible }
public enum QiTendency { Balanced, Clear, Dense, Turbid, Chaotic, Leaking, YinLeaning, YangLeaning, DualFlow, Hollow }

public sealed class PhysicalProfile
{
    public double StrengthPotential { get; set; } = 50;
    public double Flexibility { get; set; } = 50;
    public double BoneRobustness { get; set; } = 50;
    public double HealingCapacity { get; set; } = 50;
    public double Coordination { get; set; } = 50;
    public double PainTolerance { get; set; } = 50;
}

public sealed class MentalProfile
{
    public double Focus { get; set; } = 50;
    public double Willpower { get; set; } = 50;
    public double EmotionalStability { get; set; } = 50;
    public double Perception { get; set; } = 50;
    public double Memory { get; set; } = 50;
    public double QiSensitivity { get; set; } = 20;
    public double DecisionFatigue { get; set; }
}

public sealed class EnduranceProfile
{
    public double AerobicEndurance { get; set; } = 50;
    public double MuscularEndurance { get; set; } = 50;
    public double RecoveryEfficiency { get; set; } = 50;
    public double WorkCapacity { get; set; } = 50;
}

public sealed class QiProfile
{
    public double Capacity { get; set; } = 10;
    public double Control { get; set; } = 5;
    public double Purity { get; set; } = 50;
    public double Stability { get; set; } = 50;
    public double Recovery { get; set; } = 20;
    public QiTendency Tendency { get; set; } = QiTendency.Balanced;
    public QiMutationRarity MutationRarity { get; set; } = QiMutationRarity.None;
    public string MutationName { get; set; } = "Aucune";
    public bool IsBeneficial { get; set; } = true;
}

public sealed class NpcPhysiology
{
    public PhysicalProfile Physical { get; } = new();
    public MentalProfile Spirit { get; } = new();
    public EnduranceProfile Endurance { get; } = new();
    public QiProfile Qi { get; } = new();
}

public sealed class InjuryRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public BodyRegion Region { get; init; }
    public BodySide Side { get; init; }
    public InjuryKind Kind { get; init; }
    public InjurySeverity Severity { get; set; }
    public long OccurredDay { get; init; }
    public string Cause { get; init; } = string.Empty;
    public double Pain { get; set; }
    public double HealingProgress { get; set; }
    public double FunctionalLoss { get; set; }
    public bool Permanent { get; set; }
    public bool Active { get; set; } = true;
    public bool PhantomSensation { get; set; }

    public string DisplayText
    {
        get
        {
            var severity = Severity switch
            {
                InjurySeverity.Minor => "Blessure mineure",
                InjurySeverity.Light => "Blessure légère",
                InjurySeverity.Moderate => "Blessure modérée",
                InjurySeverity.Severe => "Blessure grave",
                InjurySeverity.Critical => "Blessure critique",
                _ => "Blessure"
            };
            var permanence = Permanent ? " — séquelle permanente" : string.Empty;
            return $"{severity} — {RegionLabel(Region)} {SideLabel(Side)} — {KindLabel(Kind)}{permanence}".Replace("  ", " ").Trim();
        }
    }

    private static string RegionLabel(BodyRegion r) => r switch
    {
        BodyRegion.Head => "tête", BodyRegion.Face => "visage", BodyRegion.Neck => "cou", BodyRegion.Chest => "thorax",
        BodyRegion.Abdomen => "abdomen", BodyRegion.Back => "dos", BodyRegion.Shoulder => "épaule", BodyRegion.UpperArm => "bras",
        BodyRegion.Elbow => "coude", BodyRegion.Forearm => "avant-bras", BodyRegion.Wrist => "poignet", BodyRegion.Hand => "main",
        BodyRegion.Fingers => "doigts", BodyRegion.Hip => "hanche", BodyRegion.Thigh => "cuisse", BodyRegion.Knee => "genou",
        BodyRegion.LowerLeg => "jambe", BodyRegion.Ankle => "cheville", BodyRegion.Foot => "pied", BodyRegion.Toes => "orteils",
        BodyRegion.Eye => "œil", BodyRegion.Ear => "oreille", BodyRegion.InternalOrgans => "organes internes",
        BodyRegion.MeridianNetwork => "méridiens", BodyRegion.Dantian => "dantian", _ => r.ToString()
    };
    private static string SideLabel(BodySide s) => s switch { BodySide.Left => "gauche", BodySide.Right => "droite", _ => string.Empty };
    private static string KindLabel(InjuryKind k) => k switch
    {
        InjuryKind.Bruise => "contusion", InjuryKind.Cut => "coupure", InjuryKind.Laceration => "lacération", InjuryKind.Burn => "brûlure",
        InjuryKind.Fracture => "fracture", InjuryKind.Dislocation => "luxation", InjuryKind.InternalTrauma => "traumatisme interne",
        InjuryKind.Concussion => "commotion", InjuryKind.NerveDamage => "atteinte nerveuse", InjuryKind.Scar => "cicatrice",
        InjuryKind.MeridianTrauma => "traumatisme des méridiens", InjuryKind.QiDeviation => "déviation du Qi", InjuryKind.Amputation => "amputation",
        _ => k.ToString()
    };
}

public sealed class InjurySystem
{
    private readonly Random random;
    public InjurySystem(int seed) => random = new Random(seed);

    public InjuryRecord ApplyTrauma(Npc npc, BodyRegion region, BodySide side, InjuryKind kind, InjurySeverity severity, long day, string cause)
    {
        var baseLoss = severity switch { InjurySeverity.Minor => .02, InjurySeverity.Light => .06, InjurySeverity.Moderate => .14, InjurySeverity.Severe => .28, InjurySeverity.Critical => .48, _ => .05 };
        var permanent = kind == InjuryKind.Amputation;
        var record = new InjuryRecord
        {
            Region = region, Side = side, Kind = kind, Severity = severity, OccurredDay = day, Cause = cause,
            Pain = Math.Clamp((int)severity * 18 + random.NextDouble() * 12, 0, 100),
            FunctionalLoss = permanent ? AmputationLoss(region) : baseLoss,
            Permanent = permanent,
            PhantomSensation = permanent && random.NextDouble() < .55
        };
        npc.Injuries.Add(record);
        if (severity >= InjurySeverity.Severe) npc.Body.Health = Math.Max(0, npc.Body.Health - (int)severity * 4);
        return record;
    }

    public InjuryRecord Amputate(Npc npc, BodyRegion region, BodySide side, long day, string cause)
    {
        if (region is not (BodyRegion.Hand or BodyRegion.Forearm or BodyRegion.UpperArm or BodyRegion.Foot or BodyRegion.LowerLeg or BodyRegion.Thigh or BodyRegion.Fingers or BodyRegion.Toes))
            throw new ArgumentException("Cette région n'est pas gérée comme amputation de membre.", nameof(region));
        return ApplyTrauma(npc, region, side, InjuryKind.Amputation, InjurySeverity.Critical, day, cause);
    }

    public void AdvanceDay(Npc npc)
    {
        foreach (var injury in npc.Injuries.Where(i => i.Active).ToArray())
        {
            if (injury.Permanent)
            {
                injury.Pain = Math.Max(0, injury.Pain - .04 * npc.Physiology.Physical.PainTolerance / 50.0);
                continue;
            }
            var severityPenalty = 1 + (int)injury.Severity * .45;
            var recovery = (.22 + npc.Physiology.Physical.HealingCapacity / 250.0 + npc.Physiology.Endurance.RecoveryEfficiency / 350.0) / severityPenalty;
            recovery *= Math.Clamp(1 - npc.Body.Fatigue / 130.0, .25, 1);
            injury.HealingProgress = Math.Clamp(injury.HealingProgress + recovery, 0, 100);
            injury.Pain = Math.Max(0, injury.Pain - recovery * .55);
            injury.FunctionalLoss = Math.Max(0, injury.FunctionalLoss * (1 - recovery / 160.0));
            if (injury.HealingProgress >= 100)
            {
                injury.Active = false;
                injury.FunctionalLoss = 0;
            }
        }
    }

    public static double HandFunction(Npc npc, BodySide side)
    {
        var loss = npc.Injuries.Where(i => i.Active && i.Side == side && i.Region is BodyRegion.Hand or BodyRegion.Fingers or BodyRegion.Wrist or BodyRegion.Forearm or BodyRegion.UpperArm)
            .Sum(i => i.FunctionalLoss);
        return Math.Clamp(1 - loss, 0, 1);
    }

    public static double MobilityFunction(Npc npc)
    {
        var loss = npc.Injuries.Where(i => i.Active && i.Region is BodyRegion.Hip or BodyRegion.Thigh or BodyRegion.Knee or BodyRegion.LowerLeg or BodyRegion.Ankle or BodyRegion.Foot or BodyRegion.Toes)
            .Sum(i => i.FunctionalLoss * .55);
        return Math.Clamp(1 - loss, 0, 1);
    }

    private static double AmputationLoss(BodyRegion region) => region switch
    {
        BodyRegion.Fingers => .28, BodyRegion.Hand => .72, BodyRegion.Forearm => .88, BodyRegion.UpperArm => 1.0,
        BodyRegion.Toes => .18, BodyRegion.Foot => .55, BodyRegion.LowerLeg => .72, BodyRegion.Thigh => .92, _ => .5
    };
}

public sealed class PhysiologyGenerator
{
    private readonly Random random;
    public PhysiologyGenerator(int seed) => random = new Random(seed);

    public void Initialize(Npc npc, bool allowImpossible = false)
    {
        var p = npc.Physiology;
        p.Physical.StrengthPotential = Bell(50, 15); p.Physical.Flexibility = Bell(50, 15); p.Physical.BoneRobustness = Bell(50, 14);
        p.Physical.HealingCapacity = Bell(50, 13); p.Physical.Coordination = Bell(50, 15); p.Physical.PainTolerance = Bell(50, 16);
        p.Spirit.Focus = Bell(50, 16); p.Spirit.Willpower = Bell(50, 16); p.Spirit.EmotionalStability = Bell(50, 17);
        p.Spirit.Perception = Bell(50, 16); p.Spirit.Memory = Bell(50, 15); p.Spirit.QiSensitivity = Bell(25, 18);
        p.Endurance.AerobicEndurance = Bell(50, 14); p.Endurance.MuscularEndurance = Bell(50, 14); p.Endurance.RecoveryEfficiency = Bell(50, 13); p.Endurance.WorkCapacity = Bell(50, 15);
        p.Qi.Capacity = Bell(20, 12); p.Qi.Control = Bell(8, 8); p.Qi.Purity = Bell(50, 12); p.Qi.Stability = Bell(55, 12); p.Qi.Recovery = Bell(25, 12);
        RollQiMutation(p.Qi, allowImpossible);
        npc.Body.Stamina = Math.Clamp((p.Endurance.AerobicEndurance + p.Endurance.MuscularEndurance + p.Endurance.WorkCapacity) / 3.0, 10, 100);
    }

    private void RollQiMutation(QiProfile qi, bool allowImpossible)
    {
        var roll = random.NextDouble();
        if (allowImpossible && roll < 0.0000001)
        {
            qi.MutationRarity = QiMutationRarity.Impossible; qi.MutationName = "Souffle primordial du vide"; qi.Tendency = QiTendency.DualFlow; qi.IsBeneficial = true; return;
        }
        if (roll < 0.00001) { Set(qi, QiMutationRarity.Divine, "Résonance céleste", QiTendency.Clear, true, 28, 25, 25); return; }
        if (roll < 0.00035) { Set(qi, QiMutationRarity.Mythic, "Méridiens de jade", QiTendency.Dense, true, 18, 18, 18); return; }
        if (roll < 0.0035) { Set(qi, QiMutationRarity.VeryRare, "Constitution Yin pur", QiTendency.YinLeaning, true, 12, 12, 10); return; }
        if (roll < 0.018) { Set(qi, QiMutationRarity.Rare, random.NextDouble() < .5 ? "Dantian profond" : "Double courant", random.NextDouble() < .5 ? QiTendency.Dense : QiTendency.DualFlow, true, 8, 8, 6); return; }
        if (roll < 0.065)
        {
            var bad = random.NextDouble() < .42;
            Set(qi, QiMutationRarity.Uncommon, bad ? "Méridiens étroits" : "Méridiens clairs", bad ? QiTendency.Hollow : QiTendency.Clear, !bad, bad ? -5 : 4, bad ? -8 : 6, bad ? -4 : 5); return;
        }
        if (random.NextDouble() < .012)
        {
            qi.Tendency = random.NextDouble() < .5 ? QiTendency.Turbid : QiTendency.Leaking;
            qi.MutationName = qi.Tendency == QiTendency.Turbid ? "Qi trouble" : "Dantian fuyant";
            qi.IsBeneficial = false;
            qi.Purity = Math.Max(1, qi.Purity - 12); qi.Stability = Math.Max(1, qi.Stability - 10);
        }
    }

    private static void Set(QiProfile qi, QiMutationRarity rarity, string name, QiTendency tendency, bool beneficial, double capacity, double control, double stability)
    {
        qi.MutationRarity = rarity; qi.MutationName = name; qi.Tendency = tendency; qi.IsBeneficial = beneficial;
        qi.Capacity = Math.Clamp(qi.Capacity + capacity, 1, 100); qi.Control = Math.Clamp(qi.Control + control, 0, 100); qi.Stability = Math.Clamp(qi.Stability + stability, 1, 100);
    }

    private double Bell(double mean, double sd)
    {
        var u1 = 1.0 - random.NextDouble(); var u2 = 1.0 - random.NextDouble();
        var z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        return Math.Clamp(mean + z * sd, 1, 100);
    }
}
