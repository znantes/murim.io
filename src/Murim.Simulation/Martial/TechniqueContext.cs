namespace Murim.Simulation;

public enum TechniqueRangeBand { Contact, Close, Medium, Long }
public enum TerrainCondition { Stable, Uneven, Mud, Snow, Ice, Boat, Rooftop, Forest, NarrowInterior, OpenField }

public sealed record TechniqueExecutionContext(
    TechniqueRangeBand Range,
    TerrainCondition Terrain = TerrainCondition.Stable,
    double SpaceAvailable = .8,
    double FootingQuality = .8,
    double Visibility = .8,
    double OpponentArmor = .2,
    double OpponentMobility = .5,
    double ToolQuality = .6,
    double TimePressure = .4);

public sealed record TechniqueContextProfile(
    int TechniqueId,
    TechniqueRangeBand PreferredRange,
    double MobilityDemand,
    double StrengthDemand,
    double PrecisionDemand,
    double QiControlDemand,
    double TightSpaceSuitability,
    double BadFootingSuitability,
    double ArmorPenetration,
    double Deception,
    double ObservationOpacity,
    double TeachingDifficulty);

/// <summary>
/// Gives every technique a reproducible execution profile. Rarity does not decide matchups alone:
/// body, fatigue, injury, terrain, range and tool quality can reverse an apparent advantage.
/// </summary>
public sealed class TechniqueContextSystem
{
    private readonly Dictionary<int, TechniqueContextProfile> cache = new();

    public TechniqueContextProfile ProfileFor(TechniqueDefinition technique)
    {
        if (cache.TryGetValue(technique.Id, out var existing)) return existing;
        var rng = new Random(HashCode.Combine(technique.Id, technique.Code));
        double R(double min = .12, double max = .88) => min + rng.NextDouble() * (max - min);
        var combat = IsCombatDomain(technique.Domain);
        var profile = new TechniqueContextProfile(
            technique.Id,
            PreferredRangeFor(technique.Domain, rng),
            MobilityDemand: combat ? R(.25, .92) : R(.08, .62),
            StrengthDemand: StrengthDemandFor(technique.Domain, rng),
            PrecisionDemand: PrecisionDemandFor(technique.Domain, rng),
            QiControlDemand: QiDemandFor(technique),
            TightSpaceSuitability: TightSpaceFor(technique.Domain, rng),
            BadFootingSuitability: R(.12, .90),
            ArmorPenetration: combat ? ArmorPenetrationFor(technique.Domain, rng) : 0,
            Deception: combat ? R(.05, .92) : R(.02, .45),
            ObservationOpacity: Math.Clamp(technique.Complexity * .55 + R(.05, .45), 0, 1),
            TeachingDifficulty: Math.Clamp(technique.Complexity * .68 + R(.05, .35), 0, 1));
        cache[technique.Id] = profile;
        return profile;
    }

    public double EffectiveContextMultiplier(Npc actor, TechniqueDefinition technique, TechniqueExecutionContext context)
    {
        var p = ProfileFor(technique);
        var body = actor.Physiology.Physical;
        var qi = actor.Physiology.Qi;
        var endurance = actor.Physiology.Endurance;

        var rangeFit = 1 - Math.Abs((int)p.PreferredRange - (int)context.Range) * .19;
        var mobilityCapacity = (body.Coordination * .58 + endurance.WorkCapacity * .42) / 100.0;
        var strengthCapacity = body.StrengthPotential / 100.0;
        var precisionCapacity = (body.Coordination * .50 + actor.Physiology.Spirit.Focus * .50) / 100.0;
        var qiCapacity = (qi.Control * .65 + qi.Stability * .35) / 100.0;

        var mobilityFit = 1 - Math.Max(0, p.MobilityDemand - mobilityCapacity) * .55;
        var strengthFit = 1 - Math.Max(0, p.StrengthDemand - strengthCapacity) * .50;
        var precisionFit = 1 - Math.Max(0, p.PrecisionDemand - precisionCapacity) * .55;
        var qiFit = 1 - Math.Max(0, p.QiControlDemand - qiCapacity) * .60;
        var footingFit = context.FootingQuality + (1 - context.FootingQuality) * p.BadFootingSuitability;
        var spaceFit = context.SpaceAvailable + (1 - context.SpaceAvailable) * p.TightSpaceSuitability;
        var fatiguePenalty = 1 - Math.Clamp(actor.Body.Fatigue / 150.0, 0, .60);
        var injuryPenalty = 1 - FunctionalPenalty(actor, technique.Domain);
        var toolFit = ToolDependent(technique.Domain) ? .65 + context.ToolQuality * .35 : 1;

        var result = rangeFit * mobilityFit * strengthFit * precisionFit * qiFit * footingFit * spaceFit * fatiguePenalty * injuryPenalty * toolFit;
        if (IsCombatDomain(technique.Domain) && context.OpponentArmor > .45)
            result *= .82 + p.ArmorPenetration * .30;
        return Math.Clamp(result, .12, 1.35);
    }

    public double ObservationDifficulty(TechniqueDefinition technique, TechniqueExecutionContext context)
    {
        var p = ProfileFor(technique);
        var visibilityPenalty = 1 - Math.Clamp(context.Visibility, 0, 1);
        var speedPressure = Math.Clamp(context.OpponentMobility * .25 + context.TimePressure * .25, 0, .5);
        return Math.Clamp(p.ObservationOpacity * .65 + visibilityPenalty * .25 + speedPressure, 0, 1);
    }

    private static double FunctionalPenalty(Npc actor, TechniqueDomain domain)
    {
        double penalty = 0;
        if (domain is TechniqueDomain.Sword or TechniqueDomain.Saber or TechniqueDomain.Spear or TechniqueDomain.Staff or TechniqueDomain.HiddenWeapon or TechniqueDomain.Archery or TechniqueDomain.Forging or TechniqueDomain.WeaponSmithing or TechniqueDomain.Cooking)
        {
            var bestHand = Math.Max(InjurySystem.HandFunction(actor, BodySide.Left), InjurySystem.HandFunction(actor, BodySide.Right));
            penalty += (1 - bestHand) * .58;
        }
        if (domain is TechniqueDomain.Leg or TechniqueDomain.Movement or TechniqueDomain.Qinggong or TechniqueDomain.Tracking or TechniqueDomain.Hunting)
            penalty += (1 - InjurySystem.MobilityFunction(actor)) * .62;
        return Math.Clamp(penalty, 0, .75);
    }

    private static TechniqueRangeBand PreferredRangeFor(TechniqueDomain d, Random rng) => d switch
    {
        TechniqueDomain.Grappling or TechniqueDomain.Finger => TechniqueRangeBand.Contact,
        TechniqueDomain.Fist or TechniqueDomain.Palm or TechniqueDomain.Leg or TechniqueDomain.Saber => TechniqueRangeBand.Close,
        TechniqueDomain.Sword or TechniqueDomain.Staff or TechniqueDomain.HiddenWeapon => rng.NextDouble() < .7 ? TechniqueRangeBand.Medium : TechniqueRangeBand.Close,
        TechniqueDomain.Spear => TechniqueRangeBand.Medium,
        TechniqueDomain.Archery => TechniqueRangeBand.Long,
        _ => TechniqueRangeBand.Close
    };

    private static double StrengthDemandFor(TechniqueDomain d, Random rng) => d switch
    {
        TechniqueDomain.BodyTempering or TechniqueDomain.Grappling or TechniqueDomain.Staff or TechniqueDomain.Forging or TechniqueDomain.Mining => .58 + rng.NextDouble() * .35,
        TechniqueDomain.Spear or TechniqueDomain.Saber or TechniqueDomain.Masonry => .42 + rng.NextDouble() * .36,
        TechniqueDomain.Calligraphy or TechniqueDomain.Medicine or TechniqueDomain.Alchemy => .08 + rng.NextDouble() * .18,
        _ => .18 + rng.NextDouble() * .44
    };

    private static double PrecisionDemandFor(TechniqueDomain d, Random rng) => d switch
    {
        TechniqueDomain.HiddenWeapon or TechniqueDomain.Archery or TechniqueDomain.Finger or TechniqueDomain.Medicine or TechniqueDomain.Alchemy or TechniqueDomain.Calligraphy or TechniqueDomain.WeaponSmithing => .58 + rng.NextDouble() * .38,
        TechniqueDomain.Cooking or TechniqueDomain.Poison or TechniqueDomain.Healing or TechniqueDomain.Tailoring => .46 + rng.NextDouble() * .38,
        _ => .20 + rng.NextDouble() * .52
    };

    private static double QiDemandFor(TechniqueDefinition t) => t.Domain switch
    {
        TechniqueDomain.InternalCultivation or TechniqueDomain.Healing => Math.Clamp(.40 + t.Complexity * .55, 0, 1),
        TechniqueDomain.Qinggong or TechniqueDomain.Palm or TechniqueDomain.Finger => Math.Clamp(.24 + t.Complexity * .48, 0, 1),
        _ => Math.Clamp(t.QiCost / 12.0 + t.Complexity * .20, 0, .85)
    };

    private static double TightSpaceFor(TechniqueDomain d, Random rng) => d switch
    {
        TechniqueDomain.Grappling or TechniqueDomain.Finger or TechniqueDomain.Fist or TechniqueDomain.HiddenWeapon => .64 + rng.NextDouble() * .32,
        TechniqueDomain.Spear or TechniqueDomain.Staff or TechniqueDomain.Archery => .08 + rng.NextDouble() * .30,
        _ => .28 + rng.NextDouble() * .52
    };

    private static double ArmorPenetrationFor(TechniqueDomain d, Random rng) => d switch
    {
        TechniqueDomain.Spear or TechniqueDomain.Archery or TechniqueDomain.Finger => .52 + rng.NextDouble() * .40,
        TechniqueDomain.Sword or TechniqueDomain.Saber => .32 + rng.NextDouble() * .42,
        TechniqueDomain.Palm or TechniqueDomain.Fist => .18 + rng.NextDouble() * .42,
        _ => .15 + rng.NextDouble() * .35
    };

    private static bool ToolDependent(TechniqueDomain d) => d is TechniqueDomain.Sword or TechniqueDomain.Saber or TechniqueDomain.Spear or TechniqueDomain.Staff or TechniqueDomain.HiddenWeapon or TechniqueDomain.Archery or TechniqueDomain.Forging or TechniqueDomain.WeaponSmithing or TechniqueDomain.ArmorSmithing or TechniqueDomain.Cooking or TechniqueDomain.Carpentry or TechniqueDomain.Mining;

    private static bool IsCombatDomain(TechniqueDomain d) => d is TechniqueDomain.Sword or TechniqueDomain.Saber or TechniqueDomain.Spear or TechniqueDomain.Staff or TechniqueDomain.Fist or TechniqueDomain.Palm or TechniqueDomain.Finger or TechniqueDomain.Leg or TechniqueDomain.Movement or TechniqueDomain.Qinggong or TechniqueDomain.HiddenWeapon or TechniqueDomain.Archery or TechniqueDomain.Grappling or TechniqueDomain.Formation or TechniqueDomain.Assassination;
}
