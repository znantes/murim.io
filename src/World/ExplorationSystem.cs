namespace Murim.World;

public enum DiscoveryType
{
    Place,
    Person,
    Track,
    Resource,
    Artifact,
    Danger,
    Event
}

public sealed class Discovery
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid NpcId { get; init; }
    public DiscoveryType Type { get; init; }
    public Guid? LocationId { get; init; }
    public string Description { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public long Day { get; init; }
}

public sealed class ExplorationSystem
{
    public List<Discovery> Discoveries { get; } = new();

    public Discovery Observe(WorldState world, Npc npc, Guid locationId)
    {
        var location = world.Geography.Locations[locationId];
        npc.DiscoverLocation(locationId);
        var confidence = Math.Clamp(0.45 + npc.Mind.Perception * 0.005 + npc.Mind.Intelligence * 0.003, 0, 1);
        var discovery = new Discovery
        {
            NpcId = npc.Id,
            Type = DiscoveryType.Place,
            LocationId = locationId,
            Description = $"{location.Name} est identifié comme {location.Type} dans la région {location.Region}.",
            Confidence = confidence,
            Day = world.Time.Day
        };
        Discoveries.Add(discovery);
        return discovery;
    }

    public Discovery DiscoverUnknownRoute(WorldState world, Npc npc, Guid destinationId)
    {
        var location = world.Geography.Locations[destinationId];
        var confidence = Math.Clamp(0.25 + npc.Mind.Perception * 0.004 + npc.Mind.Curiosity * 0.002, 0, 1);
        npc.DiscoverLocation(destinationId);
        var discovery = new Discovery
        {
            NpcId = npc.Id,
            Type = DiscoveryType.Place,
            LocationId = destinationId,
            Description = $"Après exploration, {npc.Identity.DisplayName} découvre {location.Name}.",
            Confidence = confidence,
            Day = world.Time.Day
        };
        Discoveries.Add(discovery);
        return discovery;
    }
}
