using System.Security.Cryptography;
using System.Text;

namespace Murim.Simulation;

public enum InstitutionDomainKind { Family, Sect }
public enum InstitutionScale { Poor = 0, Small = 1, Medium = 2, Great = 3 }
public enum InstitutionCondition { Thriving, Stable, Strained, Declining, Ruined }
public enum FacilityAccess { Public, Guest, Member, Inner, Elder, Leader, Restricted }
public enum FacilityState { Active, Closed, Abandoned, Ruined }
public enum FacilityKind
{
    Gate, Gatehouse, Courtyard, CommonRoom, MainRoom, MainHall, GuestHall, CouncilHall, AncestralHall, Shrine,
    Bedroom, JuniorQuarters, ElderQuarters, LeaderResidence, BranchResidence, Dormitory, ServantQuarters,
    Kitchen, Refectory, BanquetHall, Well, Bathhouse, Storehouse, Warehouse, Treasury, AccountingOffice,
    Stable, CarriageYard, Garden, MedicinalGarden, HerbValley, Clinic, Infirmary, AlchemyHall, Forge, Workshop,
    TrainingYard, TrainingGround, WeaponHall, Armory, Library, ScripturePavilion, MartialArchive, SecretVault,
    FamilySchool, OuterDisciplesCourt, InnerDisciplesCourt, CoreDisciplesCourt, MissionHall, MeritHall,
    MeditationHall, MeditationCave, FormationHall, PunishmentHall, MemorialGarden, Cemetery, Tombs,
    PilgrimCourt, SelectionHall, BeastPens, BellTower, DrumTower, Watchtower, HiddenPassage, ForbiddenGround
}

public sealed class DomainFacility
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public FacilityKind Kind { get; init; }
    public InstitutionScale MinimumScale { get; init; }
    public FacilityAccess Access { get; init; }
    public int Capacity { get; init; }
    public bool Martial { get; init; }
    public bool Economic { get; init; }
    public bool Residential { get; init; }
    public FacilityState State { get; set; } = FacilityState.Active;
    public double PhysicalCondition { get; set; } = 1;
    public string Description { get; init; } = string.Empty;
}

public sealed record DomainConnection(Guid FromFacilityId, Guid ToFacilityId, bool Concealed = false);

public sealed class InstitutionDomain
{
    public Guid Id { get; init; }
    public InstitutionDomainKind Kind { get; init; }
    public Guid ParentWorldLocationId { get; init; }
    public Guid? HouseholdId { get; init; }
    public Guid? FactionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public InstitutionScale HistoricalPeakScale { get; set; }
    public InstitutionScale CurrentScale { get; set; }
    public InstitutionCondition Condition { get; set; } = InstitutionCondition.Stable;
    public double Wealth { get; set; }
    public double Prestige { get; set; }
    public List<DomainFacility> Facilities { get; } = new();
    public List<DomainConnection> Connections { get; } = new();

    public IEnumerable<DomainFacility> ActiveFacilities => Facilities.Where(f => f.State == FacilityState.Active);
}

/// <summary>
/// Generates internal compounds for households and sects. A poor household, an ordinary clan courtyard,
/// a prosperous family estate and a great martial family do not expose the same physical world. Likewise,
/// sects scale from a handful of halls to mountain-sized institutions. When a domain declines, excess rooms
/// are abandoned instead of disappearing, preserving visible history for future exploration.
/// </summary>
public sealed class InstitutionDomainSystem
{
    private readonly Dictionary<Guid, InstitutionDomain> domains = new();
    public IReadOnlyDictionary<Guid, InstitutionDomain> Domains => domains;

    public IReadOnlyDictionary<Guid, InstitutionDomain> BuildAll(WorldState world)
    {
        domains.Clear();

        foreach (var household in world.Households.Values)
        {
            FactionDefinition? faction = household.FactionId is Guid factionId ? world.Factions.GetValueOrDefault(factionId) : null;
            // Sect-linked households live inside the sect domain; great families still receive a true family estate.
            if (faction is not null && faction.Type is FactionType.Sect or FactionType.Cult) continue;
            var domain = BuildFamilyDomain(world, household, faction);
            domains[domain.Id] = domain;
        }

        foreach (var faction in world.Factions.Values.Where(f => f.Type is FactionType.Sect or FactionType.Cult))
        {
            var domain = BuildSectDomain(world, faction);
            domains[domain.Id] = domain;
        }

        return domains;
    }

    public InstitutionDomain? ForHousehold(Guid householdId) => domains.Values.FirstOrDefault(d => d.HouseholdId == householdId);
    public InstitutionDomain? ForFaction(Guid factionId) => domains.Values.FirstOrDefault(d => d.FactionId == factionId && d.Kind == InstitutionDomainKind.Sect);

    public InstitutionScale FamilyScale(Household household, FactionDefinition? faction = null)
    {
        if (faction?.Type == FactionType.GreatFamily) return InstitutionScale.Great;
        var score = household.Wealth * .62 + household.Prestige * .38;
        return score switch
        {
            < 18 => InstitutionScale.Poor,
            < 43 => InstitutionScale.Small,
            < 72 => InstitutionScale.Medium,
            _ => InstitutionScale.Great
        };
    }

    public InstitutionScale SectScale(FactionDefinition faction) => faction.Prestige switch
    {
        < 60 => InstitutionScale.Small,
        < 82 => InstitutionScale.Medium,
        _ => InstitutionScale.Great
    };

    public void Reevaluate(InstitutionDomain domain, double newWealth, double newPrestige)
    {
        domain.Wealth = Math.Clamp(newWealth, 0, 100);
        domain.Prestige = Math.Clamp(newPrestige, 0, 100);
        var oldScale = domain.CurrentScale;
        var newScale = domain.Kind == InstitutionDomainKind.Family
            ? ScoreToFamilyScale(domain.Wealth * .62 + domain.Prestige * .38)
            : domain.Prestige < 60 ? InstitutionScale.Small : domain.Prestige < 82 ? InstitutionScale.Medium : InstitutionScale.Great;

        domain.CurrentScale = newScale;
        if (newScale > domain.HistoricalPeakScale) domain.HistoricalPeakScale = newScale;
        domain.Condition = newScale < oldScale ? InstitutionCondition.Declining : newScale > oldScale ? InstitutionCondition.Thriving : InstitutionCondition.Stable;

        foreach (var facility in domain.Facilities)
        {
            if (facility.MinimumScale <= newScale)
            {
                if (facility.State == FacilityState.Abandoned) facility.State = FacilityState.Active;
            }
            else if (facility.State == FacilityState.Active)
            {
                facility.State = FacilityState.Abandoned;
                facility.PhysicalCondition = Math.Min(facility.PhysicalCondition, .78);
            }
        }
    }

    public void DamageFacility(InstitutionDomain domain, Guid facilityId, double damage)
    {
        var facility = domain.Facilities.FirstOrDefault(f => f.Id == facilityId) ?? throw new ArgumentException("Facility not found.", nameof(facilityId));
        facility.PhysicalCondition = Math.Clamp(facility.PhysicalCondition - Math.Max(0, damage), 0, 1);
        if (facility.PhysicalCondition <= .08) facility.State = FacilityState.Ruined;
        else if (facility.PhysicalCondition <= .28) facility.State = FacilityState.Closed;
    }

    public bool CanEnter(Npc npc, InstitutionDomain domain, DomainFacility facility)
    {
        if (facility.State != FacilityState.Active) return false;
        if (facility.Access == FacilityAccess.Public) return true;

        var isFamilyMember = domain.HouseholdId is Guid householdId && npc.HouseholdId == householdId;
        var isFactionMember = domain.FactionId is Guid factionId && (npc.PrimaryFactionId == factionId || npc.AffiliationIds.Contains(factionId));
        var member = isFamilyMember || isFactionMember;

        return facility.Access switch
        {
            FacilityAccess.Guest => member || npc.Knowledge.Contains($"invited:{domain.Id}"),
            FacilityAccess.Member => member,
            FacilityAccess.Inner => member && (npc.AgeYears(new WorldClock()) >= 0 || npc.Knowledge.Contains($"access:{facility.Id}")),
            FacilityAccess.Elder => npc.Knowledge.Contains($"access:{facility.Id}") || npc.Knowledge.Contains($"role:elder:{domain.Id}"),
            FacilityAccess.Leader => npc.Knowledge.Contains($"access:{facility.Id}") || npc.Knowledge.Contains($"role:leader:{domain.Id}"),
            FacilityAccess.Restricted => npc.Knowledge.Contains($"access:{facility.Id}"),
            _ => false
        };
    }

    private InstitutionDomain BuildFamilyDomain(WorldState world, Household household, FactionDefinition? faction)
    {
        var scale = FamilyScale(household, faction);
        var name = faction?.Type == FactionType.GreatFamily ? faction.Name : $"Foyer {household.FamilyName}";
        var domain = NewDomain(InstitutionDomainKind.Family, household.HomeLocationId, household.Id, faction?.Id, name, scale, household.Wealth, household.Prestige);
        AddFamilyFacilities(domain);
        BuildDefaultConnections(domain);
        ApplyInitialScale(domain);
        return domain;
    }

    private InstitutionDomain BuildSectDomain(WorldState world, FactionDefinition faction)
    {
        var parent = BestLocationForFaction(world, faction);
        var scale = SectScale(faction);
        var domain = NewDomain(InstitutionDomainKind.Sect, parent.Id, null, faction.Id, faction.Name, scale, Math.Clamp(faction.Prestige * .75, 15, 100), faction.Prestige);
        AddSectFacilities(domain);
        BuildDefaultConnections(domain);
        ApplyInitialScale(domain);
        return domain;
    }

    private static InstitutionDomain NewDomain(InstitutionDomainKind kind, Guid parent, Guid? householdId, Guid? factionId, string name, InstitutionScale scale, double wealth, double prestige)
    {
        var owner = householdId?.ToString() ?? factionId?.ToString() ?? name;
        return new InstitutionDomain
        {
            Id = StableGuid($"domain:{kind}:{owner}"), Kind = kind, ParentWorldLocationId = parent, HouseholdId = householdId, FactionId = factionId,
            Name = name, HistoricalPeakScale = scale, CurrentScale = scale, Wealth = wealth, Prestige = prestige
        };
    }

    private static void AddFamilyFacilities(InstitutionDomain d)
    {
        // Poor household: everything is compressed into a handful of spaces.
        F(d, "common", "Pièce commune", FacilityKind.CommonRoom, InstitutionScale.Poor, FacilityAccess.Member, 8, residential: true, description: "Pièce principale servant à manger, travailler, parler et parfois dormir.");
        F(d, "hearth", "Coin cuisine et foyer", FacilityKind.Kitchen, InstitutionScale.Poor, FacilityAccess.Member, 4, economic: true, description: "Foyer simple pour préparer les repas et chauffer la maison.");
        F(d, "sleep", "Coin de couchage", FacilityKind.Bedroom, InstitutionScale.Poor, FacilityAccess.Member, 6, residential: true, description: "Couchages familiaux peu séparés.");
        F(d, "shared_yard", "Petite cour ou espace devant la maison", FacilityKind.Courtyard, InstitutionScale.Poor, FacilityAccess.Public, 10, description: "Espace de travail, séchage et conversations avec les voisins.");

        // Small family: one proper courtyard and dedicated rooms.
        F(d, "gate", "Porte du foyer", FacilityKind.Gate, InstitutionScale.Small, FacilityAccess.Public, 6);
        F(d, "small_court", "Cour familiale", FacilityKind.Courtyard, InstitutionScale.Small, FacilityAccess.Guest, 18);
        F(d, "main_room", "Salle principale", FacilityKind.MainRoom, InstitutionScale.Small, FacilityAccess.Guest, 14);
        F(d, "family_rooms", "Chambres familiales", FacilityKind.Bedroom, InstitutionScale.Small, FacilityAccess.Member, 12, residential: true);
        F(d, "kitchen", "Cuisine", FacilityKind.Kitchen, InstitutionScale.Small, FacilityAccess.Member, 6, economic: true);
        F(d, "store", "Réserve", FacilityKind.Storehouse, InstitutionScale.Small, FacilityAccess.Member, 5, economic: true);
        F(d, "workshop", "Petit atelier", FacilityKind.Workshop, InstitutionScale.Small, FacilityAccess.Member, 5, economic: true);
        F(d, "family_shrine", "Autel familial", FacilityKind.Shrine, InstitutionScale.Small, FacilityAccess.Member, 6);

        // Medium family: an extended household with outer/inner separation and servants or employees.
        F(d, "outer_court", "Cour extérieure", FacilityKind.Courtyard, InstitutionScale.Medium, FacilityAccess.Guest, 40);
        F(d, "guest_hall", "Pavillon des invités", FacilityKind.GuestHall, InstitutionScale.Medium, FacilityAccess.Guest, 25);
        F(d, "main_hall", "Grand salon familial", FacilityKind.MainHall, InstitutionScale.Medium, FacilityAccess.Member, 35);
        F(d, "inner_court", "Cour intérieure", FacilityKind.Courtyard, InstitutionScale.Medium, FacilityAccess.Inner, 35);
        F(d, "elder_quarters", "Quartiers des anciens", FacilityKind.ElderQuarters, InstitutionScale.Medium, FacilityAccess.Inner, 14, residential: true);
        F(d, "junior_quarters", "Quartiers des jeunes générations", FacilityKind.JuniorQuarters, InstitutionScale.Medium, FacilityAccess.Member, 24, residential: true);
        F(d, "study", "Étude familiale", FacilityKind.Library, InstitutionScale.Medium, FacilityAccess.Member, 12);
        F(d, "ancestral", "Salle ancestrale", FacilityKind.AncestralHall, InstitutionScale.Medium, FacilityAccess.Inner, 30);
        F(d, "training_yard", "Cour d'entraînement", FacilityKind.TrainingYard, InstitutionScale.Medium, FacilityAccess.Member, 30, martial: true);
        F(d, "garden", "Jardin familial", FacilityKind.Garden, InstitutionScale.Medium, FacilityAccess.Member, 30);
        F(d, "stable", "Écurie", FacilityKind.Stable, InstitutionScale.Medium, FacilityAccess.Member, 16, economic: true);
        F(d, "warehouse", "Entrepôt familial", FacilityKind.Warehouse, InstitutionScale.Medium, FacilityAccess.Member, 20, economic: true);
        F(d, "servants", "Quartiers des domestiques et employés", FacilityKind.ServantQuarters, InstitutionScale.Medium, FacilityAccess.Member, 30, residential: true);

        // Great family: a self-contained political, martial and economic institution.
        F(d, "grand_gate", "Grande porte du domaine", FacilityKind.Gatehouse, InstitutionScale.Great, FacilityAccess.Public, 30);
        F(d, "ceremonial_court", "Cour des cérémonies", FacilityKind.Courtyard, InstitutionScale.Great, FacilityAccess.Guest, 150);
        F(d, "council", "Salle du conseil familial", FacilityKind.CouncilHall, InstitutionScale.Great, FacilityAccess.Elder, 45);
        F(d, "patriarch", "Pavillon du chef de famille", FacilityKind.LeaderResidence, InstitutionScale.Great, FacilityAccess.Leader, 20, residential: true);
        F(d, "branch_courts", "Cours des branches familiales", FacilityKind.BranchResidence, InstitutionScale.Great, FacilityAccess.Inner, 120, residential: true);
        F(d, "elder_court", "Cour des anciens", FacilityKind.ElderQuarters, InstitutionScale.Great, FacilityAccess.Elder, 45, residential: true);
        F(d, "school", "École familiale", FacilityKind.FamilySchool, InstitutionScale.Great, FacilityAccess.Member, 80);
        F(d, "great_library", "Grande bibliothèque familiale", FacilityKind.Library, InstitutionScale.Great, FacilityAccess.Member, 60);
        F(d, "martial_archive", "Pavillon des arts martiaux", FacilityKind.MartialArchive, InstitutionScale.Great, FacilityAccess.Inner, 35, martial: true);
        F(d, "secret_vault", "Archive martiale scellée", FacilityKind.SecretVault, InstitutionScale.Great, FacilityAccess.Restricted, 10, martial: true);
        F(d, "main_training", "Grand terrain d'entraînement", FacilityKind.TrainingGround, InstitutionScale.Great, FacilityAccess.Member, 180, martial: true);
        F(d, "weapon_hall", "Pavillon des armes", FacilityKind.WeaponHall, InstitutionScale.Great, FacilityAccess.Inner, 40, martial: true, economic: true);
        F(d, "forge", "Forge du domaine", FacilityKind.Forge, InstitutionScale.Great, FacilityAccess.Member, 35, economic: true);
        F(d, "clinic", "Clinique familiale", FacilityKind.Clinic, InstitutionScale.Great, FacilityAccess.Member, 30);
        F(d, "medicinal_garden", "Jardin médicinal", FacilityKind.MedicinalGarden, InstitutionScale.Great, FacilityAccess.Inner, 45, economic: true);
        F(d, "alchemy", "Pavillon d'alchimie", FacilityKind.AlchemyHall, InstitutionScale.Great, FacilityAccess.Inner, 28, economic: true);
        F(d, "banquet", "Salle des banquets", FacilityKind.BanquetHall, InstitutionScale.Great, FacilityAccess.Guest, 120);
        F(d, "servant_district", "Quartier des domestiques", FacilityKind.ServantQuarters, InstitutionScale.Great, FacilityAccess.Member, 150, residential: true);
        F(d, "carriage", "Cour des voitures et chevaux", FacilityKind.CarriageYard, InstitutionScale.Great, FacilityAccess.Member, 50, economic: true);
        F(d, "treasury", "Trésorerie familiale", FacilityKind.Treasury, InstitutionScale.Great, FacilityAccess.Restricted, 12, economic: true);
        F(d, "accounts", "Bureau des comptes", FacilityKind.AccountingOffice, InstitutionScale.Great, FacilityAccess.Inner, 25, economic: true);
        F(d, "warehouses", "Grands entrepôts", FacilityKind.Warehouse, InstitutionScale.Great, FacilityAccess.Member, 80, economic: true);
        F(d, "memorial", "Jardin mémoriel", FacilityKind.MemorialGarden, InstitutionScale.Great, FacilityAccess.Inner, 60);
        F(d, "tombs", "Tombes ancestrales", FacilityKind.Tombs, InstitutionScale.Great, FacilityAccess.Inner, 70);
        F(d, "watchtower", "Tour de garde", FacilityKind.Watchtower, InstitutionScale.Great, FacilityAccess.Member, 12, martial: true);
        F(d, "hidden_passage", "Passage dissimulé", FacilityKind.HiddenPassage, InstitutionScale.Great, FacilityAccess.Restricted, 6);
        F(d, "forbidden", "Terrain familial interdit", FacilityKind.ForbiddenGround, InstitutionScale.Great, FacilityAccess.Restricted, 12, martial: true);
    }

    private static void AddSectFacilities(InstitutionDomain d)
    {
        // Small sect: enough to function as a real school, but little redundancy or luxury.
        F(d, "mountain_gate", "Porte de la secte", FacilityKind.Gate, InstitutionScale.Small, FacilityAccess.Public, 20);
        F(d, "main_hall", "Salle principale", FacilityKind.MainHall, InstitutionScale.Small, FacilityAccess.Member, 45);
        F(d, "training_yard", "Cour d'entraînement", FacilityKind.TrainingYard, InstitutionScale.Small, FacilityAccess.Member, 60, martial: true);
        F(d, "outer_dorm", "Dortoir des disciples", FacilityKind.Dormitory, InstitutionScale.Small, FacilityAccess.Member, 55, residential: true);
        F(d, "refectory", "Réfectoire", FacilityKind.Refectory, InstitutionScale.Small, FacilityAccess.Member, 60);
        F(d, "kitchen", "Cuisine de la secte", FacilityKind.Kitchen, InstitutionScale.Small, FacilityAccess.Member, 15, economic: true);
        F(d, "small_library", "Petite bibliothèque", FacilityKind.Library, InstitutionScale.Small, FacilityAccess.Member, 18);
        F(d, "storehouse", "Réserve commune", FacilityKind.Storehouse, InstitutionScale.Small, FacilityAccess.Member, 20, economic: true);
        F(d, "herb_patch", "Jardin d'herbes", FacilityKind.MedicinalGarden, InstitutionScale.Small, FacilityAccess.Member, 20, economic: true);
        F(d, "infirmary", "Infirmerie", FacilityKind.Infirmary, InstitutionScale.Small, FacilityAccess.Member, 12);
        F(d, "leader_house", "Résidence du maître de secte", FacilityKind.LeaderResidence, InstitutionScale.Small, FacilityAccess.Leader, 10, residential: true);

        // Medium sect: distinct outer and inner disciple institutions appear.
        F(d, "outer_court", "Cour des disciples externes", FacilityKind.OuterDisciplesCourt, InstitutionScale.Medium, FacilityAccess.Member, 110, martial: true);
        F(d, "inner_court", "Cour des disciples internes", FacilityKind.InnerDisciplesCourt, InstitutionScale.Medium, FacilityAccess.Inner, 80, martial: true);
        F(d, "inner_dorm", "Quartiers des disciples internes", FacilityKind.Dormitory, InstitutionScale.Medium, FacilityAccess.Inner, 65, residential: true);
        F(d, "scripture", "Pavillon des écritures", FacilityKind.ScripturePavilion, InstitutionScale.Medium, FacilityAccess.Member, 35);
        F(d, "mission", "Pavillon des missions", FacilityKind.MissionHall, InstitutionScale.Medium, FacilityAccess.Member, 35);
        F(d, "merit", "Pavillon du mérite", FacilityKind.MeritHall, InstitutionScale.Medium, FacilityAccess.Member, 25);
        F(d, "meditation", "Salle de méditation", FacilityKind.MeditationHall, InstitutionScale.Medium, FacilityAccess.Member, 50, martial: true);
        F(d, "elder_residence", "Résidences des anciens", FacilityKind.ElderQuarters, InstitutionScale.Medium, FacilityAccess.Elder, 30, residential: true);
        F(d, "guest_pavilion", "Pavillon des invités", FacilityKind.GuestHall, InstitutionScale.Medium, FacilityAccess.Guest, 35);
        F(d, "armory", "Armurerie", FacilityKind.Armory, InstitutionScale.Medium, FacilityAccess.Inner, 30, martial: true, economic: true);
        F(d, "workshop", "Ateliers de la secte", FacilityKind.Workshop, InstitutionScale.Medium, FacilityAccess.Member, 30, economic: true);
        F(d, "herb_garden", "Jardin médicinal intérieur", FacilityKind.MedicinalGarden, InstitutionScale.Medium, FacilityAccess.Inner, 45, economic: true);
        F(d, "punishment", "Salle de discipline", FacilityKind.PunishmentHall, InstitutionScale.Medium, FacilityAccess.Inner, 20);
        F(d, "ancestor_shrine", "Sanctuaire des fondateurs", FacilityKind.AncestralHall, InstitutionScale.Medium, FacilityAccess.Inner, 30);
        F(d, "cemetery", "Cimetière de la secte", FacilityKind.Cemetery, InstitutionScale.Medium, FacilityAccess.Member, 50);
        F(d, "stable", "Écuries", FacilityKind.Stable, InstitutionScale.Medium, FacilityAccess.Member, 30, economic: true);
        F(d, "warehouse", "Entrepôts", FacilityKind.Warehouse, InstitutionScale.Medium, FacilityAccess.Member, 45, economic: true);

        // Great sect: effectively a mountain town with specialized institutions and forbidden layers.
        F(d, "lower_gate", "Porte inférieure", FacilityKind.Gatehouse, InstitutionScale.Great, FacilityAccess.Public, 60);
        F(d, "pilgrim_court", "Cour des visiteurs et pèlerins", FacilityKind.PilgrimCourt, InstitutionScale.Great, FacilityAccess.Public, 180);
        F(d, "selection", "Hall de sélection des nouveaux disciples", FacilityKind.SelectionHall, InstitutionScale.Great, FacilityAccess.Guest, 120);
        F(d, "outer_district", "Quartier des disciples externes", FacilityKind.OuterDisciplesCourt, InstitutionScale.Great, FacilityAccess.Member, 350, residential: true);
        F(d, "east_training", "Terrain d'entraînement oriental", FacilityKind.TrainingGround, InstitutionScale.Great, FacilityAccess.Member, 220, martial: true);
        F(d, "west_training", "Terrain d'entraînement occidental", FacilityKind.TrainingGround, InstitutionScale.Great, FacilityAccess.Member, 220, martial: true);
        F(d, "core_court", "Cour des disciples principaux", FacilityKind.CoreDisciplesCourt, InstitutionScale.Great, FacilityAccess.Inner, 100, martial: true, residential: true);
        F(d, "great_scripture", "Grande bibliothèque des écritures", FacilityKind.ScripturePavilion, InstitutionScale.Great, FacilityAccess.Inner, 80);
        F(d, "technique_archive", "Archive des techniques", FacilityKind.MartialArchive, InstitutionScale.Great, FacilityAccess.Inner, 55, martial: true);
        F(d, "sealed_vault", "Pavillon scellé", FacilityKind.SecretVault, InstitutionScale.Great, FacilityAccess.Restricted, 12, martial: true);
        F(d, "council", "Salle du conseil des anciens", FacilityKind.CouncilHall, InstitutionScale.Great, FacilityAccess.Elder, 55);
        F(d, "leader_palace", "Résidence du maître de secte", FacilityKind.LeaderResidence, InstitutionScale.Great, FacilityAccess.Leader, 35, residential: true);
        F(d, "elder_peak", "Quartier des pics des anciens", FacilityKind.ElderQuarters, InstitutionScale.Great, FacilityAccess.Elder, 100, residential: true);
        F(d, "meditation_caves", "Grottes de méditation", FacilityKind.MeditationCave, InstitutionScale.Great, FacilityAccess.Inner, 40, martial: true);
        F(d, "herb_valley", "Vallée médicinale", FacilityKind.HerbValley, InstitutionScale.Great, FacilityAccess.Inner, 140, economic: true);
        F(d, "alchemy_hall", "Hall d'alchimie", FacilityKind.AlchemyHall, InstitutionScale.Great, FacilityAccess.Inner, 65, economic: true);
        F(d, "forge_hall", "Hall des forges", FacilityKind.Forge, InstitutionScale.Great, FacilityAccess.Member, 70, economic: true);
        F(d, "formation_hall", "Pavillon des formations", FacilityKind.FormationHall, InstitutionScale.Great, FacilityAccess.Inner, 45, martial: true);
        F(d, "beast_pens", "Enclos des bêtes et montures", FacilityKind.BeastPens, InstitutionScale.Great, FacilityAccess.Member, 80, economic: true);
        F(d, "great_infirmary", "Pavillon médical", FacilityKind.Clinic, InstitutionScale.Great, FacilityAccess.Member, 80);
        F(d, "great_refectory", "Grand réfectoire", FacilityKind.Refectory, InstitutionScale.Great, FacilityAccess.Member, 300);
        F(d, "bathhouse", "Bains de la secte", FacilityKind.Bathhouse, InstitutionScale.Great, FacilityAccess.Member, 90);
        F(d, "treasury", "Trésorerie de la secte", FacilityKind.Treasury, InstitutionScale.Great, FacilityAccess.Restricted, 18, economic: true);
        F(d, "grand_warehouse", "Grands entrepôts", FacilityKind.Warehouse, InstitutionScale.Great, FacilityAccess.Member, 120, economic: true);
        F(d, "bell", "Tour de la cloche", FacilityKind.BellTower, InstitutionScale.Great, FacilityAccess.Member, 15);
        F(d, "drum", "Tour du tambour", FacilityKind.DrumTower, InstitutionScale.Great, FacilityAccess.Member, 15);
        F(d, "watch", "Tours de surveillance", FacilityKind.Watchtower, InstitutionScale.Great, FacilityAccess.Member, 25, martial: true);
        F(d, "founder_tombs", "Tombeaux des maîtres passés", FacilityKind.Tombs, InstitutionScale.Great, FacilityAccess.Inner, 90);
        F(d, "secret_tunnel", "Tunnel secret", FacilityKind.HiddenPassage, InstitutionScale.Great, FacilityAccess.Restricted, 10);
        F(d, "forbidden_cliff", "Falaise interdite", FacilityKind.ForbiddenGround, InstitutionScale.Great, FacilityAccess.Restricted, 25, martial: true);
    }

    private static void F(InstitutionDomain d, string code, string name, FacilityKind kind, InstitutionScale min, FacilityAccess access, int capacity,
        bool martial = false, bool economic = false, bool residential = false, string description = "")
    {
        d.Facilities.Add(new DomainFacility
        {
            Id = StableGuid($"facility:{d.Id}:{code}"), Code = code, Name = name, Kind = kind, MinimumScale = min, Access = access,
            Capacity = capacity, Martial = martial, Economic = economic, Residential = residential, Description = description
        });
    }

    private static void BuildDefaultConnections(InstitutionDomain d)
    {
        var ordered = d.Facilities.OrderBy(f => f.MinimumScale).ThenBy(f => f.Code).ToArray();
        for (var i = 1; i < ordered.Length; i++) d.Connections.Add(new DomainConnection(ordered[i - 1].Id, ordered[i].Id));
        var secret = d.Facilities.Where(f => f.Kind is FacilityKind.HiddenPassage or FacilityKind.SecretVault or FacilityKind.ForbiddenGround).ToArray();
        foreach (var facility in secret)
        {
            var anchor = d.Facilities.FirstOrDefault(f => f.Access is FacilityAccess.Inner or FacilityAccess.Elder) ?? ordered[0];
            d.Connections.Add(new DomainConnection(anchor.Id, facility.Id, Concealed: true));
        }
    }

    private static void ApplyInitialScale(InstitutionDomain d)
    {
        foreach (var facility in d.Facilities)
            if (facility.MinimumScale > d.CurrentScale) facility.State = FacilityState.Abandoned;
    }

    private static InstitutionScale ScoreToFamilyScale(double score) => score switch
    {
        < 18 => InstitutionScale.Poor,
        < 43 => InstitutionScale.Small,
        < 72 => InstitutionScale.Medium,
        _ => InstitutionScale.Great
    };

    private static WorldLocation BestLocationForFaction(WorldState world, FactionDefinition faction)
    {
        var codeToken = faction.Code switch
        {
            "shaolin" => "Mont Song", "wudang" => "Mont Wudang", "mount_hua" => "Mont Hua", "emei" => "Mont Emei", "kunlun" => "Mont Kunlun",
            _ => null
        };
        if (codeToken is not null && world.Locations.Values.FirstOrDefault(l => l.Name == codeToken) is { } exact) return exact;
        return world.Locations.Values.OrderByDescending(l => RegionMatch(l.Region, faction.Region)).ThenByDescending(l => l.Tags.Contains("sect")).First();
    }

    private static int RegionMatch(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase) ? 2 : a.Contains(b, StringComparison.OrdinalIgnoreCase) || b.Contains(a, StringComparison.OrdinalIgnoreCase) ? 1 : 0;

    private static Guid StableGuid(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("murim-domain:" + text));
        return new Guid(hash.AsSpan(0, 16));
    }
}
