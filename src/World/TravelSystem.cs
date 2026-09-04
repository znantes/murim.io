namespace Murim.World;

public enum MovementMethod
{
    Walk,
    Horse,
    Cart,
    Boat,
    MartialMovement
}

public sealed class TravelPlan
{
    public Guid NpcId { get; init; }
    public Guid FromLocationId { get; init; }
    public Guid ToLocationId { get; init; }
    public MovementMethod Method { get; init; }
    public double DistanceKm { get; init; }
    public int DurationMinutes { get; init; }
    public int DangerLevel { get; init; }
}

public sealed class TravelSystem
{
    public TravelPlan? Plan(WorldState world, Npc npc, Guid destinationId, MovementMethod method)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(npc);
        if (!npc.IsAlive || npc.CurrentLocationId is null || !world.Geography.Locations.ContainsKey(destinationId)) return null;
        if (!npc.KnownLocationIds.Contains(destinationId)) return null;

        var from = npc.CurrentLocationId.Value;
        var distance = world.Geography.GetRouteDistance(from, destinationId);
        if (double.IsInfinity(distance)) return null;

        var speed = method switch
        {
            MovementMethod.Horse => 12.0,
            MovementMethod.Cart => 7.0,
            MovementMethod.Boat => 10.0,
            MovementMethod.MartialMovement => Math.Max(18.0, 8.0 + npc.Body.Speed * 0.12),
            _ => Math.Max(4.0, 3.0 + npc.Body.Endurance * 0.05)
        };
        var duration = Math.Max(1, (int)Math.Ceiling(distance / speed * 60.0));
        return new TravelPlan
        {
            NpcId = npc.Id, FromLocationId = from, ToLocationId = destinationId,
            Method = method, DistanceKm = distance, DurationMinutes = duration,
            DangerLevel = world.Geography.Locations[destinationId].DangerLevel
        };
    }

    public bool Execute(WorldState world, TravelPlan plan)
    {
        if (!world.Npcs.TryGetValue(plan.NpcId, out var npc) || !npc.IsAlive) return false;
        if (npc.CurrentLocationId != plan.FromLocationId || !npc.KnownLocationIds.Contains(plan.ToLocationId)) return false;
        world.AdvanceMinutes(plan.DurationMinutes);
        npc.SetLocation(plan.ToLocationId);
        npc.History.Add("Déplacement", npc.AgeYears, $"Voyage vers {world.Geography.Locations[plan.ToLocationId].Name} ({plan.DistanceKm:0.#} km, {plan.Method}).");
        return true;
    }
}
