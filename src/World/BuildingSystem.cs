using Murim.Simulation;

namespace Murim.World;

public enum BuildingType { House, Inn, Shop, Workshop, Clinic, Temple, Farmhouse, Warehouse, Administrative, School }
public enum BuildingAccess { Public, Customers, Workers, Members, FamilyOnly, Restricted }

public sealed class Building
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public BuildingType Type { get; init; }
    public BuildingAccess Access { get; init; }
    public Guid LocationId { get; init; }
    public string Description { get; init; } = string.Empty;
    public int Capacity { get; init; }
    public bool OpenMorning { get; init; }
    public bool OpenAfternoon { get; init; }
    public bool OpenEvening { get; init; }
    public bool OpenNight { get; init; }
    public List<Guid> ResidentNpcIds { get; } = new();
    public List<Guid> WorkerNpcIds { get; } = new();
    public List<Guid> MemberNpcIds { get; } = new();

    public bool IsOpen(TimePeriod period) => period switch
    {
        TimePeriod.Morning => OpenMorning,
        TimePeriod.Afternoon => OpenAfternoon,
        TimePeriod.Evening => OpenEvening,
        _ => OpenNight
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

    public bool CanEnter(WorldState world, Npc npc, Building building, out string reason)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(npc); ArgumentNullException.ThrowIfNull(building);
        if (!building.IsOpen(world.Time.Period)) { reason = "Le bâtiment est fermé à cette période."; return false; }
        if (building.Capacity > 0 && Occupants(world, building).Count() >= building.Capacity) { reason = "Le bâtiment est complet."; return false; }
        var allowed = building.Access switch
        {
            BuildingAccess.Public or BuildingAccess.Customers => true,
            BuildingAccess.Workers => building.WorkerNpcIds.Contains(npc.Id) || world.Employment.Contracts.TryGetValue(npc.Id, out var c) && c.BuildingId == building.Id,
            BuildingAccess.Members => building.MemberNpcIds.Contains(npc.Id),
            BuildingAccess.FamilyOnly => building.ResidentNpcIds.Contains(npc.Id),
            _ => false
        };
        if (!allowed) { reason = "Tu n'as pas accès à ce bâtiment."; return false; }
        reason = string.Empty;
        return true;
    }

    public IEnumerable<Npc> Occupants(WorldState world, Building building)
        => world.Npcs.Values.Where(n => n.IsAlive && n.CurrentBuildingId == building.Id);

    public void InitializeStarterBuildings(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        var village = world.Geography.Locations.Values.FirstOrDefault(l => l.Type == LocationType.Village)
            ?? throw new InvalidOperationException("La géographie initiale ne contient aucun village.");
        var market = world.Geography.Locations.Values.FirstOrDefault(l => l.Type == LocationType.Town)
            ?? throw new InvalidOperationException("La géographie initiale ne contient aucun bourg.");
        var temple = world.Geography.Locations.Values.FirstOrDefault(l => l.Type == LocationType.Temple)
            ?? throw new InvalidOperationException("La géographie initiale ne contient aucun temple.");

        Add(new Building { Name = "Maison du Berceau", Type = BuildingType.House, Access = BuildingAccess.FamilyOnly, LocationId = village.Id, Description = "Une petite maison familiale.", Capacity = 6, OpenMorning = true, OpenAfternoon = true, OpenEvening = true, OpenNight = true });
        Add(new Building { Name = "Auberge de la Rivière", Type = BuildingType.Inn, Access = BuildingAccess.Public, LocationId = village.Id, Description = "Une auberge simple où voyageurs et habitants peuvent manger et dormir.", Capacity = 24, OpenMorning = true, OpenAfternoon = true, OpenEvening = true, OpenNight = true });
        Add(new Building { Name = "Échoppe du Tisserand", Type = BuildingType.Shop, Access = BuildingAccess.Customers, LocationId = market.Id, Description = "Une boutique de tissus et d'articles courants.", Capacity = 12, OpenMorning = true, OpenAfternoon = true, OpenEvening = true });
        Add(new Building { Name = "Atelier du Forgeron", Type = BuildingType.Workshop, Access = BuildingAccess.Workers, LocationId = market.Id, Description = "Un atelier où sont fabriqués et réparés des outils et armes.", Capacity = 8, OpenMorning = true, OpenAfternoon = true, OpenEvening = true });
        Add(new Building { Name = "Dispensaire du Bourg", Type = BuildingType.Clinic, Access = BuildingAccess.Public, LocationId = market.Id, Description = "Un lieu de soins pour les habitants et voyageurs.", Capacity = 15, OpenMorning = true, OpenAfternoon = true, OpenEvening = true });
        Add(new Building { Name = "Sanctuaire de l’Aube", Type = BuildingType.Temple, Access = BuildingAccess.Public, LocationId = temple.Id, Description = "Un sanctuaire ouvert aux croyants et aux curieux.", Capacity = 40, OpenMorning = true, OpenAfternoon = true, OpenEvening = true });
    }

    public void AdvanceDay(WorldState world)
    {
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive && n.CurrentBuildingId is not null))
        {
            var currentBuildingId = npc.CurrentBuildingId;
            if (currentBuildingId is not Guid buildingId)
                continue;
            if (!Buildings.TryGetValue(buildingId, out var building) || !building.IsOpen(world.Time.Period))
                npc.ExitBuilding();
        }
    }
}
