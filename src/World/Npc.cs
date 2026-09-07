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
    public List<PhysicalCondition> Conditions { get; } = new();
    public MartialProfile Martial { get; } = new();

    public BirthContext Birth { get; internal set; } = new();
    public Guid? CurrentFamilyId { get; private set; }
    public Guid? CurrentLocationId { get; private set; }
    public Guid? CurrentBuildingId { get; private set; }
    public int AgeYears { get; private set; }
    public int AgeDays { get; private set; }
    public bool IsAlive { get; private set; } = true;
    public double Wealth { get; private set; }
    public double CurrentPain => Math.Clamp(Conditions.Sum(c => c.Pain), 0, 100);
    public double Mobility => Math.Clamp(1 - Conditions.Sum(c => c.MobilityPenalty), 0.05, 1);

    public void AdvanceAge(int years) { if (years < 0) throw new ArgumentOutOfRangeException(nameof(years)); AdvanceDays(years * 365); }
    public void AdvanceDays(int days) { if (days < 0) throw new ArgumentOutOfRangeException(nameof(days)); AgeDays += days; AgeYears = AgeDays / 365; }
    public void ApplyWealthChange(double amount) => Wealth = Math.Max(0, Wealth + amount);
    public void JoinFamily(Guid familyId) => CurrentFamilyId = familyId;
    public void SetLocation(Guid locationId) { CurrentLocationId = locationId; CurrentBuildingId = null; }
    public void EnterBuilding(Guid buildingId) => CurrentBuildingId = buildingId;
    public void ExitBuilding() => CurrentBuildingId = null;
    public void DiscoverLocation(Guid locationId) => KnownLocationIds.Add(locationId);
    public void Learn(KnowledgeEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        entry.Confidence = Math.Clamp(entry.Confidence, 0, 1);
        if (entry.LastConfirmedDay < entry.LearnedDay)
            entry.LastConfirmedDay = entry.LearnedDay;
        if (entry.ConfirmationCount < 1)
            entry.ConfirmationCount = 1;

        var existing = Knowledge.FirstOrDefault(k => k.EntityId == entry.EntityId && k.Kind == entry.Kind);
        if (existing is null)
        {
            Knowledge.Add(entry);
            return;
        }

        existing.Confidence = Math.Clamp(Math.Max(existing.Confidence, entry.Confidence) + 0.08 * Math.Min(4, existing.ConfirmationCount), 0, 1);
        existing.LastConfirmedDay = Math.Max(existing.LastConfirmedDay, entry.LastConfirmedDay);
        existing.ConfirmationCount = Math.Min(100, existing.ConfirmationCount + 1);
    }
    public void Die() { IsAlive = false; CurrentBuildingId = null; }
}
