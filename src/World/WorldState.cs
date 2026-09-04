using Murim.Simulation;

namespace Murim.World;

public sealed class WorldState
{
    public GameTime Time { get; } = new();
    public Dictionary<Guid, Npc> Npcs { get; } = new();
    public Dictionary<Guid, Family> Families { get; } = new();
    public FamilyLifeSystem FamilyLife { get; } = new();
    public AutonomousSocialLifeSystem SocialLife { get; } = new();
    public SocialRelationshipSystem Relationships { get; } = new();
    public GeographySystem Geography { get; } = new();
    public TravelSystem Travel { get; } = new();
    public EnvironmentSystem Environment { get; } = new();
    public InformationSystem Information { get; } = new();
    public ReputationSystem Reputation { get; } = new();
    public ExplorationSystem Exploration { get; } = new();
    public PerceptionSystem Perception { get; } = new();
    public InventorySystem Inventory { get; } = new();
    public SurvivalSystem Survival { get; } = new();
    public AgingSystem Aging { get; } = new();
    public MedicineSystem Medicine { get; } = new();
    public MartialTrainingSystem Martial { get; } = new();
    public MartialOrganizationSystem MartialOrganizations { get; } = new();
    public MartialMentorshipSystem Mentorships { get; } = new();
    public MartialConflictSystem MartialConflicts { get; } = new();
    public int WorldSeed { get; private set; }
    public Npc? PlayerNpc { get; private set; }

    public void AddNpc(Npc npc) { ArgumentNullException.ThrowIfNull(npc); Npcs[npc.Id] = npc; }
    public void AddFamily(Family family) { ArgumentNullException.ThrowIfNull(family); Families[family.Id] = family; }

    public void AdvanceMinutes(int minutes)
    {
        if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
        Survival.Advance(this, minutes);
        var oldDay = Time.Day;
        Time.AdvanceMinutes(minutes);
        var elapsedDays = Time.Day - oldDay;
        for (var i = 0L; i < elapsedDays; i++)
        {
            foreach (var npc in Npcs.Values.Where(n => n.IsAlive)) Aging.AdvanceDays(npc, 1);
            Environment.AdvanceDay(this, WorldSeed);
            Medicine.AdvanceDay(this);
            FamilyLife.AdvanceDay(this);
            SocialLife.AdvanceDay(this);
            Relationships.AdvanceDay(this);
            Mentorships.AdvanceDay(this);
            MartialConflicts.AdvanceDay(this);
        }
    }

    public Npc CreatePlayerAtBirth(int seed, string familyName = "Unknown", FamilyOrigin? forcedOrigin = null)
    {
        WorldSeed = seed;
        Geography.GenerateStarterRegion(seed);
        foreach (var location in Geography.Locations.Values) Environment.Get(location.Id);
        RegisterStarterItems();
        RegisterStarterMartialArts();
        var home = Geography.Locations.Values.First(l => l.Type == LocationType.Village);
        var random = new Random(seed);
        var origin = forcedOrigin ?? RollOrigin(random);
        var actualFamilyName = origin == FamilyOrigin.Imperial ? $"Maison impériale {familyName}" : familyName;
        var family = new Family { Name = actualFamilyName, Origin = origin, SocialStatus = SocialStatusFor(origin) };
        AddFamily(family);
        const string culture = "Unknown"; const string region = "Région du Berceau";
        var parents = new ParentLifeGenerator().CreateParentPair(actualFamilyName, culture, region, origin, seed + 1, this);
        family.FatherId = parents.Father.Id; family.MotherId = parents.Mother.Id;
        family.MemberIds.Add(parents.Father.Id); family.MemberIds.Add(parents.Mother.Id);
        parents.Father.JoinFamily(family.Id); parents.Mother.JoinFamily(family.Id);
        var context = new BirthContext { FamilyId = family.Id, FatherId = parents.Father.Id, MotherId = parents.Mother.Id, Culture = culture, SocialOrigin = actualFamilyName, Region = region };
        var npc = new BirthGenerator().CreateNewborn(context, seed + 2, parents.Father, parents.Mother);
        AddNpc(npc); family.ChildrenIds.Add(npc.Id); family.MemberIds.Add(npc.Id); npc.JoinFamily(family.Id);
        foreach (var location in Geography.Locations.Values.Where(l => l.Type is LocationType.Village or LocationType.Town or LocationType.Market)) npc.DiscoverLocation(location.Id);
        npc.SetLocation(home.Id); parents.Father.SetLocation(home.Id); parents.Mother.SetLocation(home.Id);
        parents.Father.DiscoverLocation(home.Id); parents.Mother.DiscoverLocation(home.Id);
        FamilyLife.LinkParentChild(parents.Father, npc); FamilyLife.LinkParentChild(parents.Mother, npc); FamilyLife.LinkSpouses(parents.Father, parents.Mother);
        npc.History.Add("Naissance", 0, $"Naissance au sein de la famille {family.Name}, à {home.Name}.");
        PlayerNpc = npc;
        return npc;
    }

    private void RegisterStarterItems()
    {
        Inventory.Register("Riz", ItemCategory.Food, 0.5, 1.0, true);
        Inventory.Register("Eau", ItemCategory.Food, 1.0, 0.5, true);
        Inventory.Register("Herbe médicinale", ItemCategory.Medicine, 0.1, 3.0, true);
        Inventory.Register("Bois", ItemCategory.Material, 1.0, 0.8);
        Inventory.Register("Outil simple", ItemCategory.Tool, 2.0, 12.0, false, 100);
    }

    private void RegisterStarterMartialArts()
    {
        Martial.Register("Pas du Berceau", "Tradition locale", MartialTechniqueCategory.Footwork, 12, 2, 0, 0, 4, 4);
        Martial.Register("Poing du Travailleur", "Tradition locale", MartialTechniqueCategory.Strike, 18, 4, 0, 5, 0, 5);
        Martial.Register("Garde de la Rivière", "Tradition locale", MartialTechniqueCategory.Defense, 20, 3, 0, 0, 3, 6);
        Martial.Register("Respiration du Matin", "Tradition locale", MartialTechniqueCategory.Internal, 35, 2, 2, 0, 0, 0);
    }

    public Npc CreateChild(int seed, Family family, Npc father, Npc mother, string culture, string region)
    {
        var context = new BirthContext { FamilyId = family.Id, FatherId = father.Id, MotherId = mother.Id, Culture = culture, SocialOrigin = family.Name, Region = region };
        var child = new BirthGenerator().CreateNewborn(context, seed, father, mother);
        AddNpc(child); family.ChildrenIds.Add(child.Id); family.MemberIds.Add(child.Id); child.JoinFamily(family.Id);
        if (father.CurrentLocationId is Guid locationId) { child.SetLocation(locationId); child.DiscoverLocation(locationId); }
        father.History.Add("Naissance de l'enfant", father.AgeYears, $"Naissance de {child.Identity.DisplayName}.");
        mother.History.Add("Naissance de l'enfant", mother.AgeYears, $"Naissance de {child.Identity.DisplayName}.");
        return child;
    }

    private static FamilyOrigin RollOrigin(Random random) => random.Next(100) switch { < 70 => FamilyOrigin.Common, < 80 => FamilyOrigin.Rural, < 88 => FamilyOrigin.Merchant, < 94 => FamilyOrigin.Martial, < 98 => FamilyOrigin.Noble, _ => FamilyOrigin.Imperial };
    private static string SocialStatusFor(FamilyOrigin origin) => origin switch { FamilyOrigin.Imperial => "Impérial", FamilyOrigin.Noble => "Noble", FamilyOrigin.Martial => "Martial", FamilyOrigin.Merchant => "Marchand", FamilyOrigin.Religious => "Religieux", FamilyOrigin.Criminal => "Criminel", FamilyOrigin.Secretive => "Secret", FamilyOrigin.Rural => "Rural", _ => "Commun" };
}
