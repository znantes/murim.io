namespace Murim.World;

public enum RelationshipType
{
    Parent,
    Child,
    Sibling,
    Spouse,
    ExSpouse,
    RomanticInterest,
    Friend,
    Rival,
    Master,
    Student,
    Colleague,
    Acquaintance
}

public sealed class Relationship
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid FromNpcId { get; init; }
    public Guid ToNpcId { get; init; }
    public RelationshipType Type { get; set; }
    public double Affinity { get; set; }
    public double Trust { get; set; }
    public double Respect { get; set; }
    public bool IsActive { get; set; } = true;

    public void Shift(double affinityDelta, double trustDelta, double respectDelta)
    {
        Affinity = Math.Clamp(Affinity + affinityDelta, -1.0, 1.0);
        Trust = Math.Clamp(Trust + trustDelta, 0.0, 1.0);
        Respect = Math.Clamp(Respect + respectDelta, 0.0, 1.0);
    }
}
