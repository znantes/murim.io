namespace Murim.World;

public enum ConditionType
{
    Injury,
    Wound,
    Bleeding,
    Fracture,
    Infection,
    Fever,
    Illness,
    Poisoning,
    Malnutrition,
    Exhaustion,
    ChronicPain,
    Scar
}

public sealed class PhysicalCondition
{
    public Guid Id { get; } = Guid.NewGuid();
    public ConditionType Type { get; init; }
    public string Name { get; init; } = "Condition inconnue";
    public double Severity { get; set; }
    public double Pain { get; set; }
    public double MobilityPenalty { get; set; }
    public double RecoveryRate { get; set; } = 0.02;
    public bool Treatable { get; init; } = true;
    public bool Contagious { get; init; }
    public long OnsetDay { get; init; }
    public int ExpectedDurationDays { get; init; }
    public Guid? SourceNpcId { get; init; }

    public bool IsSevere => Severity >= 0.7;
}
