namespace Murim.World;

public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}

public enum WeatherType
{
    Clear,
    Cloudy,
    Rain,
    Storm,
    Fog,
    Snow,
    HeavySnow,
    Heatwave
}

public sealed class EnvironmentState
{
    public Season Season { get; internal set; } = Season.Spring;
    public WeatherType Weather { get; internal set; } = WeatherType.Clear;
    public double TemperatureC { get; internal set; } = 15;
    public double VisibilityKm { get; internal set; } = 20;
    public double Precipitation { get; internal set; }
    public double WindKph { get; internal set; } = 5;
    public bool RoadsImpacted { get; internal set; }
}

public sealed class EnvironmentSystem
{
    private readonly Dictionary<Guid, EnvironmentState> states = new();

    public EnvironmentState Get(Guid locationId)
    {
        if (!states.TryGetValue(locationId, out var state))
        {
            state = new EnvironmentState();
            states[locationId] = state;
        }
        return state;
    }

    public void AdvanceDay(WorldState world, int seed)
    {
        var random = new Random(HashCode.Combine(seed, (int)world.Time.Day));
        foreach (var location in world.Geography.Locations.Values)
        {
            var state = Get(location.Id);
            state.Season = SeasonForDay(world.Time.Day);
            state.Weather = RollWeather(random, state.Season, location.Type);
            state.TemperatureC = TemperatureFor(state.Season, state.Weather, random);
            state.Precipitation = state.Weather switch
            {
                WeatherType.Rain => 0.7,
                WeatherType.Storm => 1.0,
                WeatherType.Snow => 0.8,
                WeatherType.HeavySnow => 1.0,
                _ => 0.0
            };
            state.VisibilityKm = state.Weather switch
            {
                WeatherType.Fog => 1.5,
                WeatherType.Storm => 4,
                WeatherType.HeavySnow => 2,
                WeatherType.Snow => 7,
                _ => 20
            };
            state.WindKph = state.Weather == WeatherType.Storm ? random.Next(35, 80) : random.Next(2, 18);
            state.RoadsImpacted = state.Weather is WeatherType.Storm or WeatherType.HeavySnow;
        }
    }

    private static Season SeasonForDay(long day) => ((day - 1) / 30 % 4) switch
    {
        0 => Season.Spring,
        1 => Season.Summer,
        2 => Season.Autumn,
        _ => Season.Winter
    };

    private static WeatherType RollWeather(Random random, Season season, LocationType type)
    {
        var roll = random.Next(100);
        if (season == Season.Winter && roll < 12) return roll < 4 ? WeatherType.HeavySnow : WeatherType.Snow;
        if (season == Season.Summer && roll < 6) return WeatherType.Heatwave;
        if (type == LocationType.Wilderness && roll < 12) return WeatherType.Fog;
        if (roll < 5) return WeatherType.Storm;
        if (roll < 20) return WeatherType.Rain;
        if (roll < 42) return WeatherType.Cloudy;
        return WeatherType.Clear;
    }

    private static double TemperatureFor(Season season, WeatherType weather, Random random)
    {
        var baseTemperature = season switch { Season.Spring => 13, Season.Summer => 27, Season.Autumn => 12, _ => 2 };
        var modifier = weather switch { WeatherType.Heatwave => 9, WeatherType.Snow => -5, WeatherType.HeavySnow => -9, WeatherType.Storm => -2, _ => 0 };
        return baseTemperature + modifier + random.NextDouble() * 6 - 3;
    }
}
