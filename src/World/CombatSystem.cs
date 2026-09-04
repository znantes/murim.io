namespace Murim.World;

public enum CombatAction
{
    Strike,
    Guard,
    Evade,
    Grapple,
    Disarm,
    Flee
}

public sealed class CombatProfile
{
    public double UnarmedSkill { get; init; }
    public double DefenseSkill { get; init; }
    public double GrapplingSkill { get; init; }
    public double Awareness { get; init; }
}

public sealed class CombatResult
{
    public Guid AttackerId { get; init; }
    public Guid DefenderId { get; init; }
    public CombatAction Action { get; init; }
    public bool Hit { get; init; }
    public double Impact { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public PhysicalCondition? Condition { get; init; }
}

public sealed class CombatSystem
{
    public CombatResult Resolve(WorldState world, Npc attacker, Npc defender, CombatAction action)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(defender);
        if (!attacker.IsAlive || !defender.IsAlive) throw new InvalidOperationException("Un combattant est déjà mort.");
        if (attacker.CurrentLocationId != defender.CurrentLocationId) throw new InvalidOperationException("Les combattants doivent être au même endroit.");

        var seed = HashCode.Combine(world.WorldSeed, attacker.Id, defender.Id, world.Time.Day, world.Time.MinuteOfDay, (int)action);
        var random = new Random(seed);
        var attack = OffensiveScore(attacker, action);
        var defense = DefensiveScore(defender, action);
        var margin = attack - defense;
        var hitChance = Math.Clamp(0.5 + margin / 160.0, 0.05, 0.95);
        var hit = action is CombatAction.Strike or CombatAction.Grapple or CombatAction.Disarm && random.NextDouble() < hitChance;
        var impact = hit ? Math.Max(1, attack * (0.35 + random.NextDouble() * 0.65) - defense * 0.20) : 0;

        PhysicalCondition? condition = null;
        string outcome;
        if (action == CombatAction.Flee)
        {
            var escape = Math.Clamp(0.35 + (attacker.Body.Speed - defender.Body.Speed) / 100.0 + attacker.Personality.Courage * 0.1, 0.05, 0.9);
            hit = random.NextDouble() < escape;
            outcome = hit ? "Le combattant parvient à fuir." : "La fuite échoue.";
        }
        else if (!hit)
        {
            outcome = "L'action échoue ou est évitée.";
        }
        else
        {
            condition = BuildInjury(defender, impact, action, world.Time.Day, random);
            defender.Conditions.Add(condition);
            outcome = $"Impact réussi : {condition.Type}, gravité {condition.Severity:0.00}.");
            if (action == CombatAction.Disarm) outcome += " L'adversaire risque de perdre son arme.";
        }

        attacker.History.Add("Combat", attacker.AgeYears, $"Action {action} contre {defender.Identity.DisplayName} : {outcome}");
        defender.History.Add("Combat", defender.AgeYears, $"Subit une action {action} de {attacker.Identity.DisplayName} : {outcome}");
        return new CombatResult { AttackerId = attacker.Id, DefenderId = defender.Id, Action = action, Hit = hit, Impact = impact, Outcome = outcome, Condition = condition };
    }

    private static double OffensiveScore(Npc npc, CombatAction action)
    {
        var baseScore = npc.Body.Strength * 0.35 + npc.Body.Speed * 0.20 + npc.Body.Coordination * 0.20 + npc.Body.Endurance * 0.10 + npc.Mind.Perception * 0.15;
        var penalty = npc.Conditions.Sum(c => c.Severity * (c.MobilityPenalty + c.Pain / 100.0) * 12);
        var modifier = action switch { CombatAction.Grapple => npc.Body.Strength * 0.20, CombatAction.Disarm => npc.Body.Coordination * 0.20, _ => 0 };
        return Math.Max(1, baseScore + modifier - penalty);
    }

    private static double DefensiveScore(Npc npc, CombatAction action)
    {
        var baseScore = npc.Body.Speed * 0.30 + npc.Body.Coordination * 0.25 + npc.Body.Endurance * 0.10 + npc.Mind.Perception * 0.20 + npc.Mind.Concentration * 0.15;
        var penalty = npc.Conditions.Sum(c => c.Severity * (c.MobilityPenalty + c.Pain / 100.0) * 10);
        return Math.Max(1, baseScore - penalty + (action == CombatAction.Guard ? 15 : 0));
    }

    private static PhysicalCondition BuildInjury(Npc defender, double impact, CombatAction action, long day, Random random)
    {
        var severity = Math.Clamp(impact / 100.0, 0.05, 1.0);
        var type = action switch
        {
            CombatAction.Grapple => severity > 0.65 ? ConditionType.Sprain : ConditionType.Pain,
            CombatAction.Disarm => severity > 0.75 ? ConditionType.Fracture : ConditionType.Injury,
            _ => severity > 0.82 ? ConditionType.Fracture : severity > 0.55 ? ConditionType.Injury : ConditionType.Pain
        };
        var pain = Math.Clamp(15 + severity * 75 + random.NextDouble() * 10, 0, 100);
        return new PhysicalCondition
        {
            Type = type,
            Severity = severity,
            Pain = pain,
            MobilityPenalty = type == ConditionType.Fracture ? 0.45 * severity : 0.15 * severity,
            RecoveryRate = type == ConditionType.Fracture ? 0.008 : 0.018,
            OnsetDay = day,
            ExpectedDurationDays = type == ConditionType.Fracture ? 90 : 20,
            Treatable = true,
            Contagious = false,
            Description = $"Blessure liée à un combat ({action})."
        };
    }
}
