namespace Murim.World;

public enum LocationType
{
    Capital,
    City,
    Town,
    Village,
    Hamlet,
    Fortress,
    Sect,
    Temple,
    Market,
    Port,
    Wilderness,
    Ruins,
    Mine,
    Farm,
    Estate
}

public sealed class Location
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "Lieu inconnu";
    public LocationType Type { get; set; } = LocationType.Wilderness;
    public string Region { get; set; } = "Inconnue";
    public double X { get; set; }
    public double Y { get; set; }
    public int Population { get; set; }
    public int DangerLevel { get; set; }
    public Guid? ParentLocationId { get; set; }
}
