namespace Murim.World;

public enum FamilyOrigin
{
    Common,
    Imperial,
    Noble,
    Martial,
    Merchant,
    Religious,
    Criminal,
    Secretive,
    Rural
}

public sealed class Family
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "Unknown Family";
    public FamilyOrigin Origin { get; set; } = FamilyOrigin.Common;
    public string SocialStatus { get; set; } = "Common";
    public Guid? FatherId { get; set; }
    public Guid? MotherId { get; set; }
    public Guid? ParentFamilyId { get; set; }
    public List<Guid> ChildrenIds { get; } = new();
    public List<Guid> MemberIds { get; } = new();
}
