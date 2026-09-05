namespace Murim.World;

public sealed class PerceptionResult
{
    public Guid NpcId { get; init; }
    public Guid LocationId { get; init; }
    public List<Guid> VisibleNpcIds { get; } = new();
    public List<string> Observations { get; } = new();
}

public sealed class PerceptionSystem
{
    public PerceptionResult Observe(WorldState world, Npc npc)
    {
        if (npc.CurrentLocationId is not Guid locationId)
            throw new InvalidOperationException("Le NPC doit être localisé pour observer son environnement.");

        var result = new PerceptionResult { NpcId = npc.Id, LocationId = locationId };
        var location = world.Geography.Locations[locationId];
        result.Observations.Add($"Lieu : {location.Name}.");
        result.Observations.Add($"Type : {location.Type}.");

        var weather = world.Environment.Get(locationId);
        result.Observations.Add($"Météo observée : {weather.Weather}, visibilité {weather.VisibilityKm:0.#} km.");

        foreach (var other in world.Npcs.Values.Where(n => n.IsAlive && n.Id != npc.Id && n.CurrentLocationId == locationId))
        {
            result.VisibleNpcIds.Add(other.Id);
            if (!npc.Relationships.Any(r => r.ToNpcId == other.Id))
                result.Observations.Add($"Une personne inconnue est présente : {other.Identity.DisplayName}.");
            else
                result.Observations.Add($"Vous reconnaissez {other.Identity.DisplayName}.");
        }
        return result;
    }
}
