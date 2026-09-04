namespace Murim.World;

public sealed class ReputationRecord
{
    public Guid NpcId { get; init; }
    public string Scope { get; init; } = "Local";
    public double Value { get; set; }
    public int WitnessCount { get; set; }
    public long LastUpdatedDay { get; set; }
}

public sealed class ReputationSystem
{
    private readonly Dictionary<(Guid, string), ReputationRecord> records = new();

    public ReputationRecord Get(Guid npcId, string scope = "Local")
    {
        var key = (npcId, scope);
        if (!records.TryGetValue(key, out var record))
        {
            record = new ReputationRecord { NpcId = npcId, Scope = scope };
            records[key] = record;
        }
        return record;
    }

    public void Apply(WorldState world, Guid npcId, double delta, string scope, int witnesses = 1)
    {
        var record = Get(npcId, scope);
        record.Value = Math.Clamp(record.Value + delta, -100, 100);
        record.WitnessCount += Math.Max(0, witnesses);
        record.LastUpdatedDay = world.Time.Day;
    }

    public double GetValue(Guid npcId, string scope = "Local") => Get(npcId, scope).Value;
}
