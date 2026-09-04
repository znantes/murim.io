namespace Murim.World;

public sealed class GeographySystem
{
    public Dictionary<Guid, Location> Locations { get; } = new();
    public Dictionary<Guid, Road> Roads { get; } = new();

    public void AddLocation(Location location)
    {
        ArgumentNullException.ThrowIfNull(location);
        Locations[location.Id] = location;
    }

    public void AddRoad(Road road)
    {
        ArgumentNullException.ThrowIfNull(road);
        if (!Locations.ContainsKey(road.FromLocationId) || !Locations.ContainsKey(road.ToLocationId))
            throw new InvalidOperationException("Une route doit relier deux lieux existants.");
        Roads[road.Id] = road;
    }

    public IEnumerable<Road> RoadsFrom(Guid locationId) =>
        Roads.Values.Where(r => r.IsOpen && (r.FromLocationId == locationId || r.ToLocationId == locationId));

    public double GetRouteDistance(Guid from, Guid to)
    {
        if (from == to) return 0;
        var distances = new Dictionary<Guid, double> { [from] = 0 };
        var pending = new HashSet<Guid> { from };
        while (pending.Count > 0)
        {
            var current = pending.OrderBy(id => distances[id]).First();
            pending.Remove(current);
            foreach (var road in RoadsFrom(current))
            {
                var next = road.FromLocationId == current ? road.ToLocationId : road.FromLocationId;
                var candidate = distances[current] + road.DistanceKm;
                if (!distances.TryGetValue(next, out var known) || candidate < known)
                {
                    distances[next] = candidate;
                    pending.Add(next);
                }
            }
        }
        return distances.TryGetValue(to, out var result) ? result : double.PositiveInfinity;
    }

    public Location CreateLocation(string name, LocationType type, string region, double x, double y, int population, int danger)
    {
        var location = new Location
        {
            Name = name, Type = type, Region = region, X = x, Y = y,
            Population = population, DangerLevel = danger
        };
        AddLocation(location);
        return location;
    }

    public void GenerateStarterRegion(int seed)
    {
        if (Locations.Count > 0) return;
        var random = new Random(seed);
        var region = "Région du Berceau";
        var home = CreateLocation("Village du Berceau", LocationType.Village, region, 0, 0, random.Next(300, 1200), 1);
        var market = CreateLocation("Bourg de la Rivière", LocationType.Town, region, 12, 3, random.Next(1000, 4000), 2);
        var city = CreateLocation("Ville de Longyuan", LocationType.City, region, 38, 8, random.Next(12000, 40000), 3);
        var wild = CreateLocation("Forêt des Pins Noirs", LocationType.Wilderness, region, -10, 18, 0, 6);
        var shrine = CreateLocation("Temple de l'Aube", LocationType.Temple, region, 8, -12, random.Next(20, 100), 1);

        AddRoad(new Road { FromLocationId = home.Id, ToLocationId = market.Id, DistanceKm = 12, Quality = RoadQuality.Road, DangerLevel = 1 });
        AddRoad(new Road { FromLocationId = market.Id, ToLocationId = city.Id, DistanceKm = 27, Quality = RoadQuality.StoneRoad, DangerLevel = 2 });
        AddRoad(new Road { FromLocationId = home.Id, ToLocationId = wild.Id, DistanceKm = 21, Quality = RoadQuality.Trail, DangerLevel = 6 });
        AddRoad(new Road { FromLocationId = home.Id, ToLocationId = shrine.Id, DistanceKm = 13, Quality = RoadQuality.DirtRoad, DangerLevel = 2 });
    }
}
