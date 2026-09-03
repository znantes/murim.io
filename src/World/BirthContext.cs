namespace Murim.World;

public sealed class BirthContext
{
    public Guid? FatherId { get; init; }
    public Guid? MotherId { get; init; }
    public Guid? FamilyId { get; init; }
    public string Culture { get; init; } = "Unknown";
    public string SocialOrigin { get; init; } = "Unknown";
    public string Region { get; init; } = "Unknown";
}
