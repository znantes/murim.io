namespace Murim.World;

public sealed class LifeHistory
{
    public List<LifeEvent> Events { get; } = new();

    public void Add(string type, int ageYears, string description)
    {
        Events.Add(new LifeEvent
        {
            Type = type,
            AgeYears = ageYears,
            Description = description
        });
    }
}

public sealed class LifeEvent
{
    public string Type { get; init; } = "Unknown";
    public int AgeYears { get; init; }
    public string Description { get; init; } = string.Empty;
}
