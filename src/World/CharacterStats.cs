namespace Murim.World;

/// <summary>
/// Core character attributes used by the Murim simulation.
/// Values are intentionally separate from a single "power" score: context,
/// fatigue, age, injuries and reputation can all change an outcome.
/// </summary>
public sealed class CharacterStats
{
    public double Strength { get; set; } = 3;
    public double Spirit { get; set; } = 3;
    public double Constitution { get; set; } = 3;
    public double Agility { get; set; } = 3;
    public double Intelligence { get; set; } = 3;
    public double Reputation { get; set; }

    public double MartialPotential =>
        Strength * 0.24 +
        Spirit * 0.24 +
        Constitution * 0.20 +
        Agility * 0.18 +
        Intelligence * 0.14;

    public double PhysicalCapacity =>
        Strength * 0.42 + Constitution * 0.38 + Agility * 0.20;

    public double InternalControl =>
        Spirit * 0.58 + Intelligence * 0.22 + Constitution * 0.20;

    public double LearningRate =>
        0.55 + Math.Clamp(Intelligence * 0.035 + Spirit * 0.02, 0, 1.45);

    public double SocialAccess => Math.Clamp(0.5 + Reputation * 0.01, 0, 2.0);

    public void Clamp()
    {
        Strength = Math.Clamp(Strength, 0, 100);
        Spirit = Math.Clamp(Spirit, 0, 100);
        Constitution = Math.Clamp(Constitution, 0, 100);
        Agility = Math.Clamp(Agility, 0, 100);
        Intelligence = Math.Clamp(Intelligence, 0, 100);
        Reputation = Math.Clamp(Reputation, -100, 100);
    }
}

public enum TrainingRisk
{
    Safe,
    Strenuous,
    Dangerous,
    Critical
}

public sealed record TrainingAssessment(
    TrainingRisk Risk,
    double SuccessChance,
    double InjuryChance,
    string Explanation);

/// <summary>
/// Makes training difficult without turning high attributes into automatic wins.
/// </summary>
public static class TrainingRules
{
    public static TrainingAssessment Assess(CharacterStats stats, int ageYears, double intensity)
    {
        intensity = Math.Clamp(intensity, 0, 1);
        var agePenalty = ageYears < 8 ? 0.35 : ageYears < 13 ? 0.15 : ageYears > 55 ? 0.12 : 0;
        var bodyReadiness = stats.Constitution * 0.55 + stats.Spirit * 0.25 + stats.Agility * 0.20;
        var control = stats.Spirit * 0.55 + stats.Intelligence * 0.25 + stats.Constitution * 0.20;

        var success = Math.Clamp(
            0.30 + bodyReadiness * 0.025 + control * 0.015 - intensity * 0.30 - agePenalty,
            0.03, 0.97);

        var injury = Math.Clamp(
            intensity * 0.48 + Math.Max(0, 5 - stats.Constitution) * 0.045 + agePenalty * 0.45,
            0.01, 0.92);

        var risk = injury >= 0.55 ? TrainingRisk.Critical :
                   injury >= 0.35 ? TrainingRisk.Dangerous :
                   injury >= 0.18 ? TrainingRisk.Strenuous : TrainingRisk.Safe;

        var explanation = risk switch
        {
            TrainingRisk.Critical => "L'intensité dépasse ce que le corps et le contrôle du qi peuvent supporter durablement.",
            TrainingRisk.Dangerous => "La progression est possible, mais une blessure ou un contrecoup est probable.",
            TrainingRisk.Strenuous => "L'entraînement est exigeant et demande récupération et alimentation suffisantes.",
            _ => "L'entraînement reste compatible avec l'état actuel du personnage."
        };

        return new TrainingAssessment(risk, success, injury, explanation);
    }
}

/// <summary>
/// Resolves a contested physical action using several attributes instead of one power number.
/// </summary>
public static class ContestRules
{
    public static double AttackScore(CharacterStats actor, double distance, double fatigue)
    {
        var rangeFactor = 1 + Math.Clamp(1 - distance / 20.0, -0.35, 0.25);
        var fatigueFactor = Math.Clamp(1 - fatigue / 140.0, 0.35, 1);
        return (actor.Strength * 0.42 + actor.Agility * 0.22 + actor.Spirit * 0.24 + actor.Intelligence * 0.12)
               * rangeFactor * fatigueFactor;
    }

    public static double DefenseScore(CharacterStats actor, double fatigue)
    {
        var fatigueFactor = Math.Clamp(1 - fatigue / 120.0, 0.30, 1);
        return (actor.Constitution * 0.36 + actor.Agility * 0.34 + actor.Spirit * 0.20 + actor.Intelligence * 0.10)
               * fatigueFactor;
    }

    public static double SocialOutcome(CharacterStats actor, double trust, bool hostileAudience)
    {
        var trustFactor = Math.Clamp(trust, -1, 1);
        var pressure = hostileAudience ? -0.35 : 0;
        return actor.Reputation * 0.65 + actor.Intelligence * 0.20 + actor.Spirit * 0.15 + trustFactor * 10 + pressure * 10;
    }
}
