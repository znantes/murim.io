using Murim.Simulation;

namespace Murim.World;

public sealed class WorldState
{
    public GameTime Time { get; } = new(); public Dictionary<Guid, Npc> Npcs { get; } = new(); public Dictionary<Guid, Family> Families { get; } = new();
    public FamilyLifeSystem FamilyLife { get; } = new(); public AutonomousSocialLifeSystem SocialLife { get; } = new(); public SocialRelationshipSystem Relationships { get; } = new();
    public GeographySystem Geography { get; } = new(); public TravelSystem Travel { get; } = new(); public EnvironmentSystem Environment { get; } = new();
    public InformationSystem Information { get; } = new(); public ReputationSystem Reputation { get; } = new(); public ExplorationSystem Exploration { get; } = new(); public PerceptionSystem Perception { get; } = new();
    public InventorySystem Inventory { get; } = new(); public SurvivalSystem Survival { get; } = new(); public AgingSystem Aging { get; } = new(); public MedicineSystem Medicine { get; } = new();
    public MartialTrainingSystem Martial { get; } = new(); public MartialOrganizationSystem MartialOrganizations { get; } = new(); public MartialMentorshipSystem Mentorships { get; } = new();
    public MartialConflictSystem MartialConflicts { get; } = new(); public MartialWarSystem MartialWars { get; } = new(); public MartialTerritorySystem MartialTerritories { get; } = new();
    public MartialWarConsequencesSystem MartialWarConsequences { get; } = new(); public ContextualActionSystem ContextualActions { get; } = new();
    public int WorldSeed { get; private set; } public Npc? PlayerNpc { get; private set; }

    public void AddNpc(Npc npc) { ArgumentNullException.ThrowIfNull(npc); Npcs[npc.Id] = npc; }
    public void AddFamily(Family family) { ArgumentNullException.ThrowIfNull(family); Families[family.Id] = family; }

    public void AdvanceMinutes(int minutes)
    {
        if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
        Survival.Advance(this, minutes); var oldDay = Time.Day; Time.AdvanceMinutes(minutes); var elapsedDays = Time.Day - oldDay;
        for (var i = 0L; i < elapsedDays; i++)
        {
            foreach (var npc in Npcs.Values.Where(n => n.IsAlive)) Aging.AdvanceDays(npc, 1);
            Environment.AdvanceDay(this, WorldSeed); Medicine.AdvanceDay(this); FamilyLife.AdvanceDay(this); SocialLife.AdvanceDay(this); Relationships.AdvanceDay(this);
            Mentorships.AdvanceDay(this); MartialConflicts.AdvanceDay(this); MartialWarConsequences.AdvanceDay(this); MartialTerritories.AdvanceDay(this);
        }
    }

    public void CreatePlayerAtBirth(int seed, string familyName)
    {
        WorldSeed = seed; Geography.GenerateStarterRegion(seed); Environment.Initialize(Geography, seed);
        MartialOrganizations.RegisterTerritorialAuthorities(this); Inventory.Register("Pain de campagne", ItemCategory.Food, 0.4, 1, true);
        var origin = FamilyLife.RollFamilyOrigin(seed, familyName); AddFamily(origin.Family); AddNpc(origin.Father); AddNpc(origin.Mother); AddNpc(origin.Child);
        var home = Geography.Locations.Values.First(l => l.Name == "Village du Berceau");
        origin.Father.SetLocation(home.Id); origin.Mother.SetLocation(home.Id); origin.Child.SetLocation(home.Id);
        origin.Child.DiscoverLocation(home.Id); foreach (var l in Geography.Locations.Values.Where(l => l.Type is LocationType.Town or LocationType.Market)) origin.Child.DiscoverLocation(l.Id);
        Relationships.Link(origin.Father.Id, origin.Child.Id, RelationshipType.Parent, 0.8, 0.8, 0.5); Relationships.Link(origin.Mother.Id, origin.Child.Id, RelationshipType.Parent, 0.8, 0.8, 0.5);
        Relationships.Link(origin.Father.Id, origin.Mother.Id, RelationshipType.Spouse, 0.6, 0.8, 0.5);
        origin.Child.History.Add("Naissance", 0, $"Naît à {home.Name} dans la famille {familyName}."); PlayerNpc = origin.Child;
    }
}
