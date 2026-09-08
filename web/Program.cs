using System.Collections.Concurrent;
using Murim.Simulation;
using Murim.World;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<GameSessions>();
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/state", (HttpContext http, GameSessions sessions) =>
{
    var session = sessions.Get(http);
    return Results.Ok(GameView.From(session.World));
});

app.MapPost("/api/new", (HttpContext http, GameSessions sessions) =>
{
    var session = sessions.Reset(http);
    return Results.Ok(GameView.From(session.World));
});

app.MapPost("/api/action", (HttpContext http, GameSessions sessions, CommandRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Command))
        return Results.BadRequest(new { error = "Commande vide." });

    var session = sessions.Get(http);
    lock (session.Sync)
    {
        var result = session.Commands.Execute(session.World, request.Command.Trim());
        return Results.Ok(new { result, state = GameView.From(session.World) });
    }
});

app.Run();

public sealed record CommandRequest(string Command);

public sealed class GameSessions
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

    public GameSession Get(HttpContext http)
    {
        var id = http.Request.Cookies["murim-session"];
        if (string.IsNullOrWhiteSpace(id) || !_sessions.TryGetValue(id, out var session))
        {
            id = Guid.NewGuid().ToString("N");
            session = CreateSession();
            _sessions[id] = session;
            http.Response.Cookies.Append("murim-session", id, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, IsEssential = true });
        }
        return session;
    }

    public GameSession Reset(HttpContext http)
    {
        var id = http.Request.Cookies["murim-session"] ?? Guid.NewGuid().ToString("N");
        var session = CreateSession();
        _sessions[id] = session;
        http.Response.Cookies.Append("murim-session", id, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, IsEssential = true });
        return session;
    }

    private static GameSession CreateSession()
    {
        var world = new WorldState();
        world.CreatePlayerAtBirth(seed: 20260903, familyName: "Murim");
        return new GameSession(world);
    }
}

public sealed class GameSession(WorldState world)
{
    public WorldState World { get; } = world;
    public PlayerCommandService Commands { get; } = new();
    public object Sync { get; } = new();
}

public sealed record GameView(
    long Day,
    string Period,
    PlayerView Player,
    LocationView? Location,
    IReadOnlyList<PersonView> People,
    IReadOnlyList<LocationView> KnownLocations,
    IReadOnlyList<ActionView> Actions)
{
    public static GameView From(WorldState world)
    {
        var player = world.PlayerNpc!;
        var location = player.CurrentLocationId is Guid locationId && world.Geography.Locations.TryGetValue(locationId, out var current)
            ? new LocationView(current.Id, current.Name, current.Type.ToString(), current.Region, current.Population, current.DangerLevel)
            : null;

        var people = world.Npcs.Values
            .Where(n => n.IsAlive && n.Id != player.Id && n.CurrentLocationId == player.CurrentLocationId && n.CurrentBuildingId == player.CurrentBuildingId)
            .OrderBy(n => n.Identity.DisplayName, StringComparer.Ordinal)
            .Take(24)
            .Select(n => new PersonView(n.Id, n.Identity.DisplayName, n.AgeYears, n.Profession.Type.ToString(), n.Profession.Skill, n.Wealth, n.Martial.OverallPower))
            .ToArray();

        var knownLocations = player.KnownLocationIds
            .Where(id => world.Geography.Locations.ContainsKey(id))
            .Select(id => world.Geography.Locations[id])
            .OrderBy(l => l.Name, StringComparer.Ordinal)
            .Select(l => new LocationView(l.Id, l.Name, l.Type.ToString(), l.Region, l.Population, l.DangerLevel))
            .ToArray();

        var actions = player.CurrentLocationId is null
            ? Array.Empty<ActionView>()
            : world.ContextualActions.GetAvailable(world, player)
                .Select(a => new ActionView(a.Kind.ToString(), a.Label, a.Command, a.TargetNpcId, a.TargetLocationId, a.TargetBuildingId))
                .ToArray();

        return new GameView(
            world.Time.Day,
            world.Time.Period.ToString(),
            new PlayerView(player.Id, player.Identity.DisplayName, player.AgeYears, player.Body.Health, player.Wealth, player.Needs.Hunger, player.Needs.Thirst, player.Needs.Sleep, player.Needs.Fatigue, player.Needs.Comfort, player.CurrentPain, player.Profession.Type.ToString(), player.Profession.Skill, player.Martial.OverallPower),
            location,
            people,
            knownLocations,
            actions);
    }
}

public sealed record PlayerView(Guid Id, string Name, int Age, double Health, double Wealth, double Hunger, double Thirst, double Sleep, double Fatigue, double Comfort, double Pain, string Profession, double Skill, double MartialPower);
public sealed record LocationView(Guid Id, string Name, string Type, string Region, int Population, int Danger);
public sealed record PersonView(Guid Id, string Name, int Age, string Profession, double Skill, double Wealth, double MartialPower);
public sealed record ActionView(string Kind, string Label, string Command, Guid? TargetNpcId, Guid? TargetLocationId, Guid? TargetBuildingId);
