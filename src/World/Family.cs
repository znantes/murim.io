namespace Murim.World;

public sealed class Family
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "Unknown Family";
    public Guid? FatherId { get; set; }
    public Guid? MotherId { get; set; }
    public List<Guid> ChildrenIds { get; } = new();
}
