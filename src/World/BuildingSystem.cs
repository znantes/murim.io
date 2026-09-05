using Murim.Simulation;

namespace Murim.World;

public enum BuildingType
{
    House,
    Inn,
    Shop,
    Workshop,
    Clinic,
    Temple,
    SectHall,
    Fortress,
    Warehouse,
    Farmhouse,
    Government
}

public enum BuildingAccess
{
    Public,
    Residents,
    Customers,
    Workers,
    Members,
    Restricted
}

public sealed class Building
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "Bâtiment inconnu";
    public BuildingType Type { get; set; } = BuildingType.House;
    public BuildingAccess Access { get; set; } = BuildingAccess.Public;
    public Guid LocationId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; } = 10;
    public bool OpenMorning { get; set; } = true;
    public bool OpenAfternoon { get; set; } = true;
    public bool OpenEvening { get; set; } = false;
    public bool OpenNight { get; set; } = false;
    public List<Guid> ResidentNpcIds { get; } = new();
    public List<Guid> WorkerNpcIds { get; } = new();

    public bool IsOpen(TimePeriod period) => period switch
    {
        TimePeriod.Morning => OpenMorning,
        TimePeriod.Afternoon => OpenAfternoon,
        TimePeriod.Evening => OpenEvening,
        TimePeriod.Night => OpenNight,
        _ => false
    };
}

public sealed class BuildingSystem
{
    public Dictionary<Guid, Building> Buildings { get; } = new();

    public void Add(Building building)
    {
        ArgumentNullException.ThrowIfNull(building);
        Buildings[building.Id] = building;
    }

    public IEnumerable<Building> AtLocation(Guid locationId) => Buildings.Values.Where(b => b.LocationId == locationId);

    public bool TryGet(Guid id, out Building? building) => Buildings.TryGetValue(id, out building);

    public bool CanEnter(WorldState world, Npc actor, Building building, out string reason)
    {
        if (!building.IsOpen(world.Time.Period) && building.Access is not BuildingAccess.Residents)
        {
            reason = $"{building.Name} est fermé pendant la période {world.Time.Period}.";
            return false;
        }

        var allowed = building.Access switch
        {
            BuildingAccess.Public or BuildingAccess.Customers => true,
            BuildingAccess.Residents => building.ResidentNpcIds.Contains(actor.Id),
            BuildingAccess.Workers => building.WorkerNpcIds.Contains(actor.Id),
            BuildingAccess.Members => false,
            BuildingAccess.Restricted => false,
            _ => false
        };

        if (!allowed)
        {
            reason = $"L'accès à {building.Name} est réservé ({building.Access}).";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public IEnumerable<Npc> Occupants(WorldState world, Building building)
    {
        return world.Npcs.Values.Where(n => n.IsAlive && n.CurrentBuildingId == building.Id).Take(building.Capacity);
    }

    public void InitializeStarterBuildings(WorldState world)
    {
        var village = world.Geography.Locations.Values.First(l => l.Name == "Village du Berceau");
        var market = world.Geography.Locations.Values.First(l => l.Name == "Bourg de la Rivière");
        var temple = world.Geography.Locations.Values.First(l => l.Name == "Temple de l’Aube");

        Add(new Building { Name = "Maison du Berceau", Type = BuildingType.House, Access = BuildingAccess.Residents, LocationId = village.Id, Description = "Une maison familiale simple où les habitants dorment et vivent.", Capacity = 8, OpenMorning = true, OpenAfternoon = true, OpenEvening = true, OpenNight = true });
        Add(new Building { Name = "Auberge de la Rivière", Type = BuildingType.Inn, Access = BuildingAccess.Customers, LocationId = market.Id, Description = "Une auberge où voyageurs, marchands et habitants se rencontrent.", Capacity = 30, OpenMorning = true, OpenAfternoon = true, OpenEvening = true, OpenNight = false });
        Add(new Building { Name = "Échoppe du Tisserand", Type = BuildingType.Shop, Access = BuildingAccess.Customers, LocationId = market.Id, Description = "Une petite boutique de tissus et vêtements.", Capacity = 12, OpenMorning = true, OpenAfternoon = true, OpenEvening = false, OpenNight = false });
        Add(new Building { Name = "Atelier du Forgeron", Type = BuildingType.Workshop, Access = BuildingAccess.Customers, LocationId = market.Id, Description = "Un atelier où l'on fabrique et répare des outils et armes.", Capacity = 10, OpenMorning = true, OpenAfternoon = true, OpenEvening = false, OpenNight = false });
        Add(new Building { Name = "Dispensaire du Bourg", Type = BuildingType.Clinic, Access = BuildingAccess.Public, LocationId = market.Id, Description = "Un lieu de soins pour les habitants et voyageurs.", Capacity = 15, OpenMorning = true, OpenAfternoon = true, OpenEvening = true, OpenNight = false });
        Add(new Building { Name = "Sanctuaire de l’Aube", Type = BuildingType.Temple, Access = BuildingAccess.Public, LocationId = temple.Id, Description = "Un sanctuaire ouvert aux croyants et aux curieux.", Capacity = 40, OpenMorning = true, OpenAfternoon = true, OpenEvening = true, OpenNight = false });
    }

    public void AdvanceDay(WorldState world)
    {
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive && n.CurrentBuildingId is not null))
        {
            if (!Buildings.TryGetValue(npc.CurrentBuildingId.Value, out var building) || !building.IsOpen(world.Time.Period))
                npc.ExitBuilding();
        }
    }
}
