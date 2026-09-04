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

            if (known is null)
                continue;

            world.Information.Spread(world, source, target, known);
            source.History.Add("Rumeur", source.AgeYears, $"Transmet une information à {target.Identity.Name}.");
            target.History.Add("Rumeur", target.AgeYears, $"Entend une information transmise par {source.Identity.Name}.");
        }
    }
}
