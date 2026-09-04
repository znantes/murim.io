using Murim.World;

namespace Murim.Simulation;

public sealed class ObservationResult
{
    public string LocationName { get; init; } = "Lieu inconnu";
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> People { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> PointsOfInterest { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> EnvironmentalSigns { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed class ObservationSystem
{
    public ObservationResult Observe(WorldState world, Npc observer)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(observer);

        if (!observer.IsAlive) throw new InvalidOperationException("Un personnage mort ne peut pas observer le monde.");
        if (observer.CurrentLocationId is not Guid locationId || !world.Geography.Locations.TryGetValue(locationId, out var location))
            return new() { Description = "Tu ne parviens pas à déterminer précisément où tu te trouves." };

        var people = world.Npcs.Values
            .Where(n => n.IsAlive && n.Id != observer.Id && n.CurrentLocationId == locationId)
            .OrderBy(n => n.Identity.DisplayName, StringComparer.Ordinal)
            .Take(12)
            .Select(n => DescribePerson(observer, n))
            .ToList();

        var interests = new List<string> { $"{location.Name} ({location.Type})" };
        if (location.Population > 0) interests.Add($"Population estimée : {location.Population}");
        if (location.DangerLevel > 0) interests.Add($"Niveau de danger connu : {location.DangerLevel}/100");

        var environment = world.Environment.Get(locationId);
        var signs = new List<string> { $"Météo : {environment.Weather}" };
        var warnings = new List<string>();
        if (location.DangerLevel >= 60) warnings.Add("Le lieu présente un danger important.");
        if (environment.Weather.Contains("storm", StringComparison.OrdinalIgnoreCase) || environment.Weather.Contains("orage", StringComparison.OrdinalIgnoreCase))
            warnings.Add("Les conditions météorologiques sont difficiles.");

        var description = people.Count == 0
            ? $"Tu observes {location.Name}. Aucun autre personnage connu n'est immédiatement visible."
            : $"Tu observes {location.Name}. {people.Count} personne(s) sont perceptibles autour de toi.";

        return new ObservationResult
        {
            LocationName = location.Name,
            Description = description,
            People = people,
            PointsOfInterest = interests,
            EnvironmentalSigns = signs,
            Warnings = warnings
        };
    }

    private static string DescribePerson(Npc observer, Npc other)
    {
        var relation = observer.Relationships.FirstOrDefault(r => r.ToNpcId == other.Id && r.IsActive);
        var relationText = relation is null ? "connaissance non établie" : relation.Type.ToString();
        return $"{other.Identity.DisplayName} — {relationText}";
    }
}
