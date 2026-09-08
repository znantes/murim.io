namespace Murim.World;

public sealed class ReputationRecord
{
    public Guid NpcId { get; init; }
    public string Scope { get; init; } = "Local";
    public double Value { get; set; }
    public int WitnessCount { get; set; }
    public long LastUpdatedDay { get; set; }
}

/// <summary>
/// Reputation is local, witnessed and slow to change. A rumor should not instantly
/// make a character famous across the entire Murim.
/// </summary>
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
        var witnessCount = Math.Max(1, witnesses);

        // The first witnesses matter most. Ten witnesses do not make an action ten
        // times more important; they make it more credible.
        var credibility = 1.0 + Math.Log10(witnessCount) * 0.35;
        var localDelta = delta * credibility;

        record.Value = Math.Clamp(record.Value + localDelta, -100, 100);
        record.WitnessCount += Math.Max(0, witnesses);
        record.LastUpdatedDay = world.Time.Day;
    }

    /// <summary>
    /// Reputation naturally becomes less relevant when nobody talks about it.
    /// Extreme fame/infamy therefore requires repeated actions to maintain it.
    /// </summary>
    public void Advance(WorldState world, long days = 1)
    {
        if (days <= 0) return;

        foreach (var record in records.Values)
        {
            var elapsed = Math.Max(0, world.Time.Day - record.LastUpdatedDay);
            if (elapsed <= 30) continue;

            // Slow local memory loss: roughly 1% of the distance toward neutral per month.
            var months = (elapsed - 30) / 30.0;
            var factor = Math.Pow(0.99, months);
            record.Value *= factor;
            record.LastUpdatedDay = world.Time.Day;
        }
    }

    public double GetValue(Guid npcId, string scope = "Local") => Get(npcId, scope).Value;
}
