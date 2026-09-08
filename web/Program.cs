using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Murim.Simulation;
using Murim.World;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<GameSessions>();
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/state", (HttpContext http, GameSessions sessions) => Results.Ok(GameView.From(sessions.Get(http).World)));
app.MapPost("/api/new", (HttpContext http, GameSessions sessions) => Results.Ok(GameView.From(sessions.Reset(http).World)));
app.MapPost("/api/action", (HttpContext http, GameSessions sessions, CommandRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Command)) return Results.BadRequest(new { error = "Commande vide." });
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
            id = Guid.NewGuid().ToString("N"); session = CreateSession(); _sessions[id] = session;
            http.Response.Cookies.Append("murim-session", id, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, IsEssential = true });
        }
        return session;
    }
    public GameSession Reset(HttpContext http)
    {
        var id = http.Request.Cookies["murim-session"] ?? Guid.NewGuid().ToString("N");
        var session = CreateSession(); _sessions[id] = session;
        http.Response.Cookies.Append("murim-session", id, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, IsEssential = true });
        return session;
    }
    private static GameSession CreateSession()
    {
        var world = new WorldState(); world.CreatePlayerAtBirth(20260903, "Murim"); return new GameSession(world);
    }
}

public sealed class GameSession(WorldState world)
{
    public WorldState World { get; } = world;
    public PlayerCommandService Commands { get; } = new();
    public object Sync { get; } = new();
}

public sealed record GameView(long Day, string Period, PlayerView Player, LocationView? Location, IReadOnlyList<PersonView> People, IReadOnlyList<LocationView> KnownLocations, IReadOnlyList<ActionView> Actions, IReadOnlyList<InventoryView> Inventory, IReadOnlyList<RelationshipView> Relationships, IReadOnlyList<KnowledgeView> Knowledge, IReadOnlyList<TechniqueView> Techniques, WorldSummaryView World)
{
    public static GameView From(WorldState world)
    {
        var player = world.PlayerNpc!;
        var location = player.CurrentLocationId is Guid locationId && world.Geography.Locations.TryGetValue(locationId, out var current)
            ? new LocationView(current.Id, current.Name, current.Type.ToString(), current.Region, current.Population, current.DangerLevel) : null;
        var people = world.Npcs.Values.Where(n => n.IsAlive && n.Id != player.Id && n.CurrentLocationId == player.CurrentLocationId && n.CurrentBuildingId == player.CurrentBuildingId)
            .OrderBy(n => n.Identity.DisplayName, StringComparer.Ordinal).Take(32)
            .Select(n => new PersonView(n.Id, n.Identity.DisplayName, n.AgeYears, n.Profession.Type.ToString(), n.Profession.Skill, n.Wealth)).ToArray();
        var knownLocations = player.KnownLocationIds.Where(id => world.Geography.Locations.ContainsKey(id)).Select(id => world.Geography.Locations[id])
            .OrderBy(l => l.Name, StringComparer.Ordinal).Select(l => new LocationView(l.Id, l.Name, l.Type.ToString(), l.Region, l.Population, l.DangerLevel)).ToArray();
        var actions = player.CurrentLocationId is null ? Array.Empty<ActionView>() : world.ContextualActions.GetAvailable(world, player)
            .Select(a => new ActionView(a.Kind.ToString(), a.Label, a.Command, a.TargetNpcId, a.TargetLocationId, a.TargetBuildingId)).ToArray();
        var inventory = player.Inventory.Entries.Where(e => e.Quantity > 0)
            .Select(e => world.Inventory.Items.TryGetValue(e.ItemId, out var item) ? new InventoryView(e.ItemId, item.Name, item.Category.ToString(), e.Quantity, item.BasePrice) : null)
            .Where(v => v is not null).Cast<InventoryView>().OrderBy(v => v.Name, StringComparer.Ordinal).ToArray();
        var relationships = player.Relationships.Where(r => r.IsActive && world.Npcs.TryGetValue(r.ToNpcId, out var n) && n.IsAlive)
            .Select(r => new RelationshipView(r.ToNpcId, world.Npcs[r.ToNpcId].Identity.DisplayName, r.Type.ToString(), r.Trust, r.Respect, r.Affinity)).OrderByDescending(r => r.Trust).Take(32).ToArray();
        var knowledge = player.Knowledge.OrderByDescending(k => k.Confidence).ThenBy(k => k.Summary, StringComparer.Ordinal).Take(80)
            .Select(k => new KnowledgeView(k.EntityId, k.Kind.ToString(), k.Confidence, k.LearnedDay, k.LastConfirmedDay, k.ConfirmationCount, k.Summary)).ToArray();
        var techniques = world.Martial.Techniques.Values.OrderBy(t => t.Name, StringComparer.Ordinal).Select(t => new TechniqueView(t.Id, t.Name)).ToArray();
        return new GameView(world.Time.Day, world.Time.Period.ToString(),
            new PlayerView(player.Id, player.Identity.DisplayName, player.AgeYears, player.Body.Health, player.Wealth, player.Needs.Hunger, player.Needs.Thirst, player.Needs.Sleep, player.Needs.Fatigue, player.Needs.Comfort, player.CurrentPain, player.Profession.Type.ToString(), player.Profession.Skill, player.Martial.GetType().Name, player.CurrentFamilyId),
            location, people, knownLocations, actions, inventory, relationships, knowledge, techniques,
            new WorldSummaryView(world.Npcs.Count(n => n.Value.IsAlive), world.Npcs.Count, world.Geography.Locations.Count, world.Families.Count, world.Buildings.Buildings.Count, world.Commerce.Businesses.Count, world.Martial.Techniques.Count, player.KnownLocationIds.Count, player.Knowledge.Count));
    }
}

public sealed record PlayerView(Guid Id, string Name, int Age, double Health, double Wealth, double Hunger, double Thirst, double Sleep, double Fatigue, double Comfort, double Pain, string Profession, double Skill, string MartialProfile, Guid? FamilyId);
public sealed record LocationView(Guid Id, string Name, string Type, string Region, int Population, int Danger);
public sealed record PersonView(Guid Id, string Name, int Age, string Profession, double Skill, double Wealth);
public sealed record ActionView(string Kind, string Label, string Command, Guid? TargetNpcId, Guid? TargetLocationId, Guid? TargetBuildingId);
public sealed record InventoryView(Guid Id, string Name, string Category, int Quantity, double BasePrice);
public sealed record RelationshipView(Guid NpcId, string Name, string Type, double Trust, double Respect, double Affinity);
public sealed record KnowledgeView(Guid Id, string Kind, double Confidence, long LearnedDay, long LastConfirmedDay, int Confirmations, string Summary);
public sealed record TechniqueView(Guid Id, string Name);
public sealed record WorldSummaryView(int LivingNpcs, int TotalNpcs, int Locations, int Families, int Buildings, int Businesses, int Techniques, int KnownLocations, int KnowledgeEntries);
