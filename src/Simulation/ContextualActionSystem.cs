using Murim.World;

namespace Murim.Simulation;

public enum ContextualActionKind
{
    Talk,
    Travel,
    Examine,
    Enter,
    Work,
    Buy,
    Sell,
    AskDirections,
    Follow,
    Help,
    Refuse,
    PickUp,
    Investigate,
    DetectDanger
}

public sealed class ContextualAction
{
    public ContextualActionKind Kind { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public Guid? TargetNpcId { get; init; }
    public Guid? TargetLocationId { get; init; }
    public bool RequiresKnowledge { get; init; }
}

public sealed class ContextualActionSystem
{
    public IReadOnlyList<ContextualAction> GetAvailable(WorldState world, Npc actor)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(actor);
        if (!actor.IsAlive) return Array.Empty<ContextualAction>();

        var actions = new List<ContextualAction>();
        if (actor.CurrentLocationId is not Guid locationId || !world.Geography.Locations.TryGetValue(locationId, out var location))
            return actions;

        foreach (var npc in world.Npcs.Values
                     .Where(n => n.IsAlive && n.Id != actor.Id && n.CurrentLocationId == locationId)
                     .OrderBy(n => n.Identity.DisplayName, StringComparer.Ordinal)
                     .Take(12))
        {
            actions.Add(new()
            {
                Kind = ContextualActionKind.Talk,
                Label = $"Parler à {npc.Identity.DisplayName}",
                Command = $"Parle à {npc.Identity.DisplayName}",
                TargetNpcId = npc.Id
            });
            actions.Add(new()
            {
                Kind = ContextualActionKind.AskDirections,
                Label = $"Demander son chemin à {npc.Identity.DisplayName}",
                Command = $"Demande ton chemin à {npc.Identity.DisplayName}",
                TargetNpcId = npc.Id
            });
            actions.Add(new()
            {
                Kind = ContextualActionKind.Follow,
                Label = $"Suivre {npc.Identity.DisplayName}",
                Command = $"Suis {npc.Identity.DisplayName}",
                TargetNpcId = npc.Id
            });
        }

        var nearby = world.Geography.RoadsFrom(locationId)
            .Where(r => actor.KnownLocationIds.Contains(r.ToLocationId))
            .Select(r => world.Geography.Locations.TryGetValue(r.ToLocationId, out var destination) ? destination : null)
            .Where(l => l is not null)
            .Cast<Location>()
            .OrderBy(l => l.Name, StringComparer.Ordinal);

        foreach (var destination in nearby)
        {
            actions.Add(new()
            {
                Kind = ContextualActionKind.Travel,
                Label = $"Aller à {destination.Name}",
                Command = $"Va à {destination.Name}",
                TargetLocationId = destination.Id,
                RequiresKnowledge = true
            });
        }

        actions.Add(new()
        {
            Kind = ContextualActionKind.Examine,
            Label = $"Examiner {location.Name}",
            Command = $"Examine {location.Name}",
            TargetLocationId = location.Id
        });

        if (location.Type is LocationType.City or LocationType.Town or LocationType.Village or LocationType.Hamlet or LocationType.Estate)
        {
            actions.Add(new() { Kind = ContextualActionKind.Work, Label = "Chercher du travail", Command = "Cherche du travail" });
            actions.Add(new() { Kind = ContextualActionKind.Buy, Label = "Chercher à acheter", Command = "Cherche à acheter quelque chose" });
            actions.Add(new() { Kind = ContextualActionKind.Sell, Label = "Chercher à vendre", Command = "Cherche à vendre quelque chose" });
        }

        if (location.Type is LocationType.Sect or LocationType.Temple or LocationType.Fortress or LocationType.Estate)
            actions.Add(new() { Kind = ContextualActionKind.Enter, Label = "Entrer", Command = "Entre" });

        actions.Add(new() { Kind = ContextualActionKind.Investigate, Label = "Enquêter sur les alentours", Command = "Enquête sur les alentours" });
        actions.Add(new() { Kind = ContextualActionKind.DetectDanger, Label = "Évaluer les dangers", Command = "Détecte les dangers" });

        return actions;
    }
}
