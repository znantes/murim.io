namespace Murim.World;

public sealed class EnvironmentEffects
{
    public static int TravelMinutes(WorldState world, TravelPlan plan)
    {
        var state = world.Environment.Get(plan.ToLocationId);
        var multiplier = state.Weather switch
        {
            WeatherType.Clear => 1.0,
            WeatherType.Cloudy => 1.05,
            WeatherType.Rain => 1.2,
            WeatherType.Storm => 1.6,
            WeatherType.Fog => 1.3,
            WeatherType.Snow => 1.35,
            WeatherType.HeavySnow => 1.8,
            WeatherType.Heatwave => 1.25,
            _ => 1.0
        };
        return Math.Max(1, (int)Math.Ceiling(plan.DurationMinutes * multiplier));
    }

    public static int ActivityDifficulty(WorldState world, Guid locationId)
    {
        var state = world.Environment.Get(locationId);
        return state.Weather switch
        {
            WeatherType.Storm or WeatherType.HeavySnow => 3,
            WeatherType.Rain or WeatherType.Snow or WeatherType.Fog => 1,
            WeatherType.Heatwave => 2,
            _ => 0
        };
    }
}
