namespace Murim.World;

/// <summary>
/// Generates small deterministic world events for NPCs without player input.
/// The system is deliberately lightweight: it creates history entries and can
/// later be extended to invoke richer simulation systems (relationships, work,
/// travel, crime, etc.) without changing the scheduler contract.
/// </summary>
public sealed class AutonomousWorldEventSystem
{
    private readonly HashSet<long> _scheduledDays = new();

    public void EnsureScheduled(WorldState world)
    {
        var day = world.Time.Day;
        if (!_scheduledDays.Add(day))
            return;

        var candidates = world.Npcs.Values.Where(n => n.IsAlive).OrderBy(n => n.Id).ToArray();
        if (candidates.Length < 2)
            return;

        var random = new Random(HashCode.Combine(world.WorldSeed, (int)day));
        var first = candidates[random.Next(candidates.Length)];
        Npc second;
        do
        {
            second = candidates[random.Next(candidates.Length)];
        } while (second.Id == first.Id);

        var minute = 15 + random.Next(90);
        world.Events.Schedule(
            world.Time,
            minute,
            "Rencontre autonome",
            $"{first.Identity.Name} croise {second.Identity.Name} sans intervention du joueur.",
            state => ResolveEncounter(state, first.Id, second.Id));
    }

    private static void ResolveEncounter(WorldState world, Guid firstId, Guid secondId)
    {
        if (!world.Npcs.TryGetValue(firstId, out var first) ||
            !world.Npcs.TryGetValue(secondId, out var second) ||
            !first.IsAlive || !second.IsAlive)
            return;

        first.History.Add("Rencontre", first.AgeYears,
            $"Croise {second.Identity.Name} au cours d'une activité autonome.");
        second.History.Add("Rencontre", second.AgeYears,
            $"Croise {first.Identity.Name} au cours d'une activité autonome.");
    }
}
