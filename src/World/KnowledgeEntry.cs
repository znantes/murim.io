namespace Murim.World;

public enum KnowledgeKind
{
    Location,
    Person,
    Organization,
    Technique,
    Event,
    Object,
    Resource
}

public sealed class KnowledgeEntry
{
    public Guid EntityId { get; init; }
    public KnowledgeKind Kind { get; init; }
    public double Confidence { get; set; }
    public long LearnedDay { get; init; }
    public long LastConfirmedDay { get; set; }
    public int ConfirmationCount { get; set; }
    public Guid? SourceNpcId { get; init; }
    public string Summary { get; init; } = string.Empty;
}
