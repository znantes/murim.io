namespace Murim.World;

public enum RelationshipType
{
    Parent,
    Child,
    Sibling,
    Spouse,
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
}
