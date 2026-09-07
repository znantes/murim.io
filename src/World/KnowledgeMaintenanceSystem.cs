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
            foreach (var entry in npc.Knowledge)
            {
                var age = Math.Max(0, world.Time.Day - entry.LastConfirmedDay);
                if (age == 0)
                    continue;

                var halfLife = entry.Kind switch
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
            }
        }
    }
}
