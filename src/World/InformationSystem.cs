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
    public double Polarity { get; init; }
    public InformationReliability Reliability { get; set; } = InformationReliability.Rumor;
    public long CreatedDay { get; init; }
    public HashSet<Guid> HeardByNpcIds { get; } = new();
}

public sealed class InformationSystem
{
    public List<InformationItem> Items { get; } = new();

    public InformationItem Publish(WorldState world, Npc source, string topic, string content, Guid? subjectNpcId = null, Guid? locationId = null, InformationReliability reliability = InformationReliability.Unverified, double polarity = 0)
    {
        var item = new InformationItem
        {
            SourceNpcId = source.Id,
            SubjectNpcId = subjectNpcId,
            LocationId = locationId,
            Topic = topic,
            Content = content,
            Polarity = Math.Clamp(polarity, -1, 1),
            Reliability = reliability,
            CreatedDay = world.Time.Day
        };
        item.HeardByNpcIds.Add(source.Id);
        Items.Add(item);
        return item;
    }

    public IEnumerable<InformationItem> HeardBy(Npc npc) => Items.Where(i => i.HeardByNpcIds.Contains(npc.Id) || npc.KnownLocationIds.Contains(i.LocationId ?? Guid.Empty) || i.SubjectNpcId == npc.Id);

    public InformationItem? Spread(WorldState world, Npc from, Npc to, InformationItem original)
    {
        if (from.CurrentLocationId is null || to.CurrentLocationId != from.CurrentLocationId) return null;
        if (original.HeardByNpcIds.Contains(to.Id)) return null;

        var trust = from.Relationships.FirstOrDefault(r => r.ToNpcId == to.Id)?.Trust ?? 0.2;
        var reliability = original.Reliability;
        if (trust < 0.25 && reliability == InformationReliability.Verified) reliability = InformationReliability.Plausible;
        if (trust < 0.1) reliability = InformationReliability.Rumor;
        if (reliability > InformationReliability.Rumor && from.Personality.Sociability < 0.2) reliability--;

        var item = Publish(world, from, original.Topic, original.Content, original.SubjectNpcId, original.LocationId, reliability, original.Polarity);
        foreach (var heardId in original.HeardByNpcIds) item.HeardByNpcIds.Add(heardId);
        item.HeardByNpcIds.Add(to.Id);

        if (original.SubjectNpcId is Guid subjectId && subjectId != to.Id)
        {
            var confidence = ReliabilityConfidence(reliability) * Math.Clamp(0.65 + trust * 0.5, 0.25, 1.0);
            to.Learn(new KnowledgeEntry
            {
                EntityId = subjectId,
                Kind = KnowledgeKind.Person,
                Confidence = confidence,
                LearnedDay = world.Time.Day,
                SourceNpcId = from.Id,
                Summary = original.Content
            });
            ApplyReputation(world, to, subjectId, item, trust);
        }

        return item;
    }

    private static double ReliabilityConfidence(InformationReliability reliability) => reliability switch
    {
        InformationReliability.Verified => 1.0,
        InformationReliability.Plausible => 0.72,
        InformationReliability.Unverified => 0.48,
        _ => 0.25
    };

    private static void ApplyReputation(WorldState world, Npc listener, Guid subjectId, InformationItem item, double trust)
    {
        if (Math.Abs(item.Polarity) < 0.01 || subjectId == listener.Id) return;
        var belief = ReliabilityConfidence(item.Reliability) * Math.Clamp(0.5 + trust, 0.2, 1.5);
        var delta = item.Polarity * belief * 3.0;
        world.Reputation.Apply(world, subjectId, delta, "Local", 1);
        listener.History.Add("Réputation", listener.AgeYears, $"Forme une opinion sur {world.Npcs.GetValueOrDefault(subjectId)?.Identity.Name ?? "un habitant"} après avoir entendu une information.");
    }
}
