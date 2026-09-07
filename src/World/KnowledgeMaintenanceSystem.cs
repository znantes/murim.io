namespace Murim.World;

public sealed class KnowledgeMaintenanceSystem
{
    private readonly HashSet<long> _processedDays = new();

    public void AdvanceDay(WorldState world)
    {
        if (!_processedDays.Add(world.Time.Day))
            return;

        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive).OrderBy(n => n.Id))
        {
            foreach (var entry in npc.Knowledge.ToList())
            {
                if (IsDirectlyObservable(world, npc, entry))
                {
                    entry.Confidence = 1.0;
                    entry.LastConfirmedDay = world.Time.Day;
                    entry.ConfirmationCount = Math.Min(100, entry.ConfirmationCount + 1);
                    continue;
                }

                var age = Math.Max(0, world.Time.Day - entry.LastConfirmedDay);
                if (age == 0)
                    continue;

                var subjectIsDead = entry.Kind == KnowledgeKind.Person
                    && world.Npcs.TryGetValue(entry.EntityId, out var person)
                    && !person.IsAlive;
                var halfLife = subjectIsDead ? 2.0 : entry.Kind switch
                {
                    KnowledgeKind.Event => 7.0,
                    KnowledgeKind.Person => 45.0,
                    KnowledgeKind.Location => 120.0,
                    KnowledgeKind.Resource => 30.0,
                    KnowledgeKind.Technique => 180.0,
                    _ => 90.0
                };

                var decay = Math.Pow(0.5, age / halfLife);
                entry.Confidence = Math.Clamp(entry.Confidence * decay, 0, 1);

                if (subjectIsDead && entry.Confidence < 0.05)
                    npc.Knowledge.Remove(entry);
            }
        }
    }

    private static bool IsDirectlyObservable(WorldState world, Npc npc, KnowledgeEntry entry)
    {
        if (npc.CurrentLocationId is not Guid currentLocationId)
            return false;

        if (entry.Kind == KnowledgeKind.Location)
            return entry.EntityId == currentLocationId;

        if (entry.Kind == KnowledgeKind.Person && world.Npcs.TryGetValue(entry.EntityId, out var person))
            return person.IsAlive && person.CurrentLocationId == currentLocationId;

        return false;
    }
}
