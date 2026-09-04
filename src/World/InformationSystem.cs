namespace Murim.World;

public enum InformationReliability
{
    Rumor,
    Unverified,
    Plausible,
    Verified
}

public sealed class InformationItem
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid SourceNpcId { get; init; }
    public Guid? SubjectNpcId { get; init; }
    public Guid? LocationId { get; init; }
    public string Topic { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public InformationReliability Reliability { get; set; } = InformationReliability.Rumor;
    public long CreatedDay { get; init; }
}

public sealed class InformationSystem
{
    public List<InformationItem> Items { get; } = new();

    public InformationItem Publish(WorldState world, Npc source, string topic, string content, Guid? subjectNpcId = null, Guid? locationId = null, InformationReliability reliability = InformationReliability.Unverified)
    {
        var item = new InformationItem { SourceNpcId = source.Id, SubjectNpcId = subjectNpcId, LocationId = locationId, Topic = topic, Content = content, Reliability = reliability, CreatedDay = world.Time.Day };
        Items.Add(item);
        return item;
    }

    public IEnumerable<InformationItem> HeardBy(Npc npc) => Items.Where(i => npc.KnownLocationIds.Contains(i.LocationId ?? Guid.Empty) || i.SubjectNpcId == npc.Id);

    public InformationItem? Spread(WorldState world, Npc from, Npc to, InformationItem original)
    {
        if (from.CurrentLocationId is null || to.CurrentLocationId != from.CurrentLocationId) return null;
        var trust = from.Relationships.FirstOrDefault(r => r.ToNpcId == to.Id)?.Trust ?? 0.2;
        var reliability = original.Reliability;
        if (trust < 0.25 && reliability == InformationReliability.Verified) reliability = InformationReliability.Plausible;
        if (trust < 0.1) reliability = InformationReliability.Rumor;
        return Publish(world, from, original.Topic, original.Content, original.SubjectNpcId, original.LocationId, reliability);
    }
}
