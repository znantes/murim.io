namespace Murim.World;

public sealed class Identity
{
    public string GivenName { get; set; } = "Unknown";
    public string FamilyName { get; set; } = "Unknown";
    public string? Title { get; set; }
    public string Sex { get; set; } = "Unknown";

    public string DisplayName => string.IsNullOrWhiteSpace(FamilyName)
        ? GivenName
        : $"{GivenName} {FamilyName}";
}
