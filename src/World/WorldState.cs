using Murim.Simulation;

namespace Murim.World;

public sealed class WorldState
{
    public GameTime Time { get; } = new(); public Dictionary<Guid, Npc> Npcs { get; } = new(); public Dictionary<Guid, Family> Families { get; } = new();
    public FamilyLifeSystem FamilyLife { get; } = new(); public AutonomousSocialLifeSystem SocialLife { get; } = new(); public SocialRelationshipSystem Relationships { get; } = new();
    public GeographySystem Geography { get; } = new(); public TravelSystem Travel { get; } = new(); public EnvironmentSystem Environment { get; } = new();
    public InformationSystem Information { get; } = new(); public ReputationSystem Reputation { get; } = new(); public ExplorationSystem Exploration { get; } = new(); public PerceptionSystem Perception { get; } = new();
    public InventorySystem Inventory { get; } = new(); public CommerceSystem Commerce { get; } = new(); public EmploymentSystem Employment { get; } = new(); public EnterpriseSystem Enterprises { get; } = new(); public SurvivalSystem Survival { get; } = new(); public AgingSystem Aging { get; } = new(); public MedicineSystem Medicine { get; } = new();
    public MartialTrainingSystem Martial { get; } = new(); public MartialOrganizationSystem MartialOrganizations { get; } = new(); public MartialMentorshipSystem Mentorships { get; } = new();
    public MartialConflictSystem MartialConflicts { get; } = new(); public MartialWarSystem MartialWars { get; } = new(); public MartialTerritorySystem MartialTerritories { get; } = new();
    public MartialWarConsequencesSystem MartialWarConsequences { get; } = new(); public ContextualActionSystem ContextualActions { get; } = new(); public BuildingSystem Buildings { get; } = new();
    public int WorldSeed { get; private set; } public Npc? PlayerNpc { get; private set; }
    public void AddNpc(Npc npc) { ArgumentNullException.ThrowIfNull(npc); Npcs[npc.Id] = npc; }
    public void AddFamily(Family family) { ArgumentNullException.ThrowIfNull(family); Families[family.Id] = family; }
    public void AdvanceMinutes(int minutes)
    {
        if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes)); Survival.Advance(this, minutes); var oldDay = Time.Day; Time.AdvanceMinutes(minutes); var elapsedDays = Time.Day - oldDay;
        for (var i = 0L; i < elapsedDays; i++) { foreach (var npc in Npcs.Values.Where(n => n.IsAlive)) Aging.AdvanceDays(npc, 1); Environment.AdvanceDay(this, WorldSeed); Medicine.AdvanceDay(this); FamilyLife.AdvanceDay(this); SocialLife.AdvanceDay(this); Relationships.AdvanceDay(this); Mentorships.AdvanceDay(this); Employment.AdvanceDay(this); Enterprises.AdvanceDay(this); MartialConflicts.AdvanceDay(this); MartialWarConsequences.AdvanceDay(this); MartialTerritories.AdvanceDay(this); Buildings.AdvanceDay(this); }
    }
    public void CreatePlayerAtBirth(int seed, string familyName)
    {
        WorldSeed = seed; Geography.GenerateStarterRegion(seed); Environment.Initialize(Geography, seed); Buildings.InitializeStarterBuildings(this); MartialOrganizations.RegisterTerritorialAuthorities(this);
        var bread = Inventory.Register("Pain de campagne", ItemCategory.Food, 0.4, 1, true); var water = Inventory.Register("Eau", ItemCategory.Food, 1, 0.2, true); var cloth = Inventory.Register("Tissu simple", ItemCategory.Clothing, 0.3, 2); var medicine = Inventory.Register("Herbes médicinales", ItemCategory.Medicine, 0.1, 3, true);
        var origin = FamilyLife.RollFamilyOrigin(seed, familyName); AddFamily(origin.Family); AddNpc(origin.Father); AddNpc(origin.Mother); AddNpc(origin.Child);
        var home = Geography.Locations.Values.First(l => l.Name == "Village du Berceau"); origin.Father.SetLocation(home.Id); origin.Mother.SetLocation(home.Id); origin.Child.SetLocation(home.Id);
        var familyHome = Buildings.Buildings.Values.First(b => b.LocationId == home.Id && b.Type == BuildingType.House); familyHome.ResidentNpcIds.Add(origin.Father.Id); familyHome.ResidentNpcIds.Add(origin.Mother.Id); familyHome.ResidentNpcIds.Add(origin.Child.Id); origin.Father.EnterBuilding(familyHome.Id); origin.Mother.EnterBuilding(familyHome.Id); origin.Child.EnterBuilding(familyHome.Id);
        origin.Child.DiscoverLocation(home.Id); foreach (var l in Geography.Locations.Values.Where(l => l.Type is LocationType.Town or LocationType.Market)) origin.Child.DiscoverLocation(l.Id);
        Relationships.Link(origin.Father.Id, origin.Child.Id, RelationshipType.Parent, 0.8, 0.8, 0.5); Relationships.Link(origin.Mother.Id, origin.Child.Id, RelationshipType.Parent, 0.8, 0.8, 0.5); Relationships.Link(origin.Father.Id, origin.Mother.Id, RelationshipType.Spouse, 0.6, 0.8, 0.5);
        origin.Child.History.Add("Naissance", 0, $"Naît à {home.Name} dans la famille {familyName}."); PlayerNpc = origin.Child;
        var shopBuilding = new Building { Name = "Échoppe de Lin", Type = BuildingType.Shop, Access = BuildingAccess.Customers, LocationId = home.Id, Description = "Une boutique familiale où l'on vend les produits du village.", Capacity = 12, OpenMorning = true, OpenAfternoon = true, OpenEvening = true, OpenNight = false }; Buildings.Add(shopBuilding);
        var shop = Commerce.AddBusiness("Échoppe de Lin", CommerceType.GeneralStore, home.Id, origin.Father.Id, evening: true, buildingId: shopBuilding.Id); Commerce.AddStock(shop, bread.Id, 12, 1.1); Commerce.AddStock(shop, water.Id, 20); Commerce.AddStock(shop, cloth.Id, 6, 1.15); Commerce.AddStock(shop, medicine.Id, 4, 1.3); Enterprises.Register(this, shop);
        Employment.Post(this, origin.Father, ProfessionType.Merchant, "Assistant de boutique", Math.Max(0.3, origin.Father.Profession.DailyIncome / 8.0), Math.Max(0, origin.Father.Profession.Skill), shop.Id, shopBuilding.Id);
    }
}
