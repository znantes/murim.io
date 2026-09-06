namespace Murim.World;

/// <summary>
/// Lets autonomous NPCs transmit small pieces of information they personally
/// observed. Trust affects how reliable the transmitted information remains.
/// </summary>
public sealed class AutonomousInformationSystem
{
    private readonly HashSet<long> _processedDays = new();

    public void AdvanceDay(WorldState world)
    {
        if (!_processedDays.Add(world.Time.Day))
            return;

        foreach (var source in world.Npcs.Values.Where(n => n.IsAlive && n.CurrentLocationId is not null).OrderBy(n => n.Id))
        {
            var candidates = world.Npcs.Values
                .Where(n => n.IsAlive && n.Id != source.Id && n.CurrentLocationId == source.CurrentLocationId)
                .OrderBy(n => n.Id)
                .Take(6)
                .ToArray();

            if (candidates.Length == 0)
                continue;

            var random = new Random(DeterministicRandomSeed.Create(world.WorldSeed + 211, world.Time.Day, source.Id));
            if (random.NextDouble() >= Math.Clamp(0.12 + source.Personality.Sociability * 0.30, 0.05, 0.45))
                continue;

            var target = candidates[random.Next(candidates.Length)];
            var known = world.Information.HeardBy(source)
                .Where(i => i.SubjectNpcId is not null && i.SubjectNpcId != source.Id)
                .OrderByDescending(i => i.CreatedDay)
                .FirstOrDefault();

            if (known is not null)
            {
                world.Information.Spread(world, source, target, known);
                source.History.Add("Rumeur", source.AgeYears, $"Transmet une information à {target.Identity.DisplayName}.");
                target.History.Add("Rumeur", target.AgeYears, $"Entend une information transmise par {source.Identity.DisplayName}.");
                continue;
            }

            var locationId = source.KnownLocationIds
                .Where(id => id != source.CurrentLocationId && !target.KnownLocationIds.Contains(id) && world.Geography.Locations.ContainsKey(id))
                .OrderBy(id => id)
                .FirstOrDefault();
            if (locationId == Guid.Empty || !world.Geography.Locations.TryGetValue(locationId, out var location))
                continue;

            var locationInfo = world.Information.Publish(
                world,
                source,
                "Lieu",
                $"{source.Identity.DisplayName} connaît {location.Name} et peut indiquer comment y aller.",
                locationId: locationId,
                reliability: InformationReliability.Verified);
            world.Information.Spread(world, source, target, locationInfo);
            source.History.Add("Information", source.AgeYears, $"Indique à {target.Identity.DisplayName} comment rejoindre {location.Name}.");
            target.History.Add("Information", target.AgeYears, $"Apprend l'existence de {location.Name} grâce à {source.Identity.DisplayName}.");
        }
    }
}
