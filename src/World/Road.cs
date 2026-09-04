namespace Murim.World;

public enum RoadQuality
{
    Trail,
    DirtRoad,
    Road,
    StoneRoad,
    ImperialRoute
}

public sealed class Road
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid FromLocationId { get; init; }
    public Guid ToLocationId { get; init; }
    public double DistanceKm { get; init; }
    public RoadQuality Quality { get; init; } = RoadQuality.DirtRoad;
    public bool IsOpen { get; set; } = true;
    public int DangerLevel { get; init; }
}
