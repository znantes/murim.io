namespace Murim.World;

public sealed class Npc
{
    public Guid Id { get; } = Guid.NewGuid();
    public Identity Identity { get; } = new();
    public Body Body { get; } = new();
    public Mind Mind { get; } = new();
    public Personality Personality { get; } = new();
    public InheritanceProfile Inheritance { get; } = new();
    public LifeHistory History { get; } = new();
    public List<Relationship> Relationships { get; } = new();
    public Profession Profession { get; } = new();
    public HashSet<Guid> KnownLocationIds { get; } = new();
    public List<KnowledgeEntry> Knowledge { get; } = new();
    public Inventory Inventory { get; } = new();
    public Needs Needs { get; } = new();

    public BirthContext Birth { get; internal set; } = new();
    public Guid? CurrentFamilyId { get; private set; }
    public Guid? CurrentLocationId { get; private set; }
    public int AgeYears { get; private set; }
    public int AgeDays { get; private set; }
    public bool IsAlive { get; private set; } = true;
    public double Wealth { get; private set; }

    public void AdvanceAge(int years) { if (years < 0) throw new ArgumentOutOfRangeException(nameof(years)); AdvanceDays(years * 365); }
    public void AdvanceDays(int days) { if (days < 0) throw new ArgumentOutOfRangeException(nameof(days)); AgeDays += days; AgeYears = AgeDays / 365; }
    public void ApplyWealthChange(double amount) => Wealth = Math.Max(0, Wealth + amount);
    public void JoinFamily(Guid familyId) => CurrentFamilyId = familyId;
    public void SetLocation(Guid locationId) => CurrentLocationId = locationId;
    public void DiscoverLocation(Guid locationId) => KnownLocationIds.Add(locationId);
    public void Learn(KnowledgeEntry entry)
    {
        var existing = Knowledge.FirstOrDefault(k => k.EntityId == entry.EntityId && k.Kind == entry.Kind);
        if (existing is null) Knowledge.Add(entry);
        else existing.Confidence = Math.Max(existing.Confidence, entry.Confidence);
    }
    public void Die() => IsAlive = false;
}
