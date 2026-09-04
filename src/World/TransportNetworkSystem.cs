namespace Murim.World;

public sealed class TransportRoute
{
    public Guid FromLocationId { get; init; }
    public Guid ToLocationId { get; init; }
    public double DistanceKm { get; init; }
    public int Danger { get; init; }
    public MovementMethod BestMethod { get; init; }
    public double WeatherFactor { get; set; } = 1;
}

public sealed class TransportIncident
{
    public long Day { get; init; }
    public Guid TravellerNpcId { get; init; }
    public Guid FromLocationId { get; init; }
    public Guid ToLocationId { get; init; }
    public int MinutesLost { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed class TransportNetworkSystem
{
    public List<TransportIncident> History { get; } = new();

    public TransportRoute? Route(WorldState world, Guid from, Guid to)
    {
        if (!world.Geography.Locations.ContainsKey(from) || !world.Geography.Locations.ContainsKey(to)) return null;
        var distance = world.Geography.GetRouteDistance(from, to);
        if (double.IsInfinity(distance)) return null;
        var danger = Math.Clamp((world.Geography.Locations[from].DangerLevel + world.Geography.Locations[to].DangerLevel) / 2, 0, 10);
        return new TransportRoute { FromLocationId=from, ToLocationId=to, DistanceKm=distance, Danger=danger, BestMethod=distance > 80 ? MovementMethod.Cart : MovementMethod.Walk };
    }

    public int TravelMinutes(WorldState world, Npc npc, Guid destination, MovementMethod method)
    {
        if (npc.CurrentLocationId is null) return -1;
        var route = Route(world, npc.CurrentLocationId.Value, destination);
        if (route is null) return -1;
        var speed = method switch { MovementMethod.Walk => 5.0, MovementMethod.Horse => 11.0, MovementMethod.Cart => 7.0, MovementMethod.Boat => 8.0, MovementMethod.MartialMovement => 8.0, _ => 5.0 };
        var weather = world.Environment.Get(route.FromLocationId);
        var factor = weather.RoadsImpacted ? 1.35 : weather.Weather is WeatherType.Rain or WeatherType.Fog ? 1.15 : weather.Weather == WeatherType.Storm ? 1.6 : 1;
        route.WeatherFactor = factor;
        return Math.Max(15, (int)Math.Ceiling(route.DistanceKm / speed * 60 * factor));
    }

    public bool ResolveIncident(WorldState world, Npc npc, Guid destination, MovementMethod method, int minutes, out int extraMinutes, out string eventText)
    {
        extraMinutes = 0; eventText = string.Empty;
        if (npc.CurrentLocationId is null) return false;
        var route = Route(world, npc.CurrentLocationId.Value, destination);
        if (route is null) return false;
        var weather = world.Environment.Get(route.FromLocationId);
        var risk = Math.Clamp(.005 + route.Danger * .006 + (weather.RoadsImpacted ? .04 : 0) + (weather.Weather == WeatherType.Storm ? .06 : 0), 0, .35);
        var roll = new Random(HashCode.Combine(world.WorldSeed, npc.Id, (int)world.Time.Day, world.Time.MinuteOfDay, destination)).NextDouble();
        if (roll >= risk) return false;
        extraMinutes = Math.Max(15, (int)Math.Ceiling(minutes * (.1 + roll * .35)));
        var descriptions = new[] { "Un détour imposé par l'état du chemin.", "Un incident mineur ralentit le voyage.", "La météo rend la progression difficile.", "Le voyageur doit contourner une zone dangereuse." };
        eventText = descriptions[Math.Abs(HashCode.Combine(npc.Id, destination, (int)world.Time.Day)) % descriptions.Length];
        History.Add(new TransportIncident { Day=world.Time.Day, TravellerNpcId=npc.Id, FromLocationId=route.FromLocationId, ToLocationId=destination, MinutesLost=extraMinutes, Description=eventText });
        npc.History.Add("Voyage", extraMinutes, eventText);
        return true;
    }
}
