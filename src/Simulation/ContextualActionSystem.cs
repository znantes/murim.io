using Murim.World;

namespace Murim.Simulation;

public enum ContextualActionKind { Talk, Travel, Examine, Enter, Exit, Work, Buy, Sell, AskDirections, Follow, Help, Refuse, PickUp, Investigate, DetectDanger }

public sealed class ContextualAction
{
    public ContextualActionKind Kind { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public Guid? TargetNpcId { get; init; }
    public Guid? TargetLocationId { get; init; }
    public Guid? TargetBuildingId { get; init; }
    public bool RequiresKnowledge { get; init; }
}

public sealed class ContextualActionSystem
{
    public IReadOnlyList<ContextualAction> GetAvailable(WorldState world, Npc actor)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(actor); if (!actor.IsAlive) return Array.Empty<ContextualAction>();
        if (actor.CurrentLocationId is not Guid locationId || !world.Geography.Locations.TryGetValue(locationId, out var location)) return Array.Empty<ContextualAction>();
        var actions = new List<ContextualAction>();
        if (actor.CurrentBuildingId is Guid currentBuildingId && world.Buildings.TryGet(currentBuildingId, out var current))
        {
            actions.Add(new() { Kind = ContextualActionKind.Exit, Label = $"Sortir de {current.Name}", Command = "Sors" });
            actions.Add(new() { Kind = ContextualActionKind.Examine, Label = $"Observer l'intérieur de {current.Name}", Command = $"Examine {current.Name}", TargetBuildingId = current.Id });
            if (current.Type is BuildingType.Shop or BuildingType.Inn or BuildingType.Workshop) { actions.Add(new() { Kind = ContextualActionKind.Buy, Label = "Acheter ici", Command = "Cherche à acheter quelque chose", TargetBuildingId = current.Id }); actions.Add(new() { Kind = ContextualActionKind.Sell, Label = "Vendre ici", Command = "Cherche à vendre quelque chose", TargetBuildingId = current.Id }); }
            if (current.Type is BuildingType.Workshop or BuildingType.Shop or BuildingType.Inn or BuildingType.Farmhouse) actions.Add(new() { Kind = ContextualActionKind.Work, Label = "Travailler ici", Command = "Travaille", TargetBuildingId = current.Id });
        }
        else
        {
            foreach (var building in world.Buildings.AtLocation(locationId).Where(b => b.IsOpen(world.Time.Period)).OrderBy(b => b.Name, StringComparer.Ordinal))
            {
                actions.Add(new() { Kind = ContextualActionKind.Enter, Label = $"Entrer dans {building.Name}", Command = $"Entre dans {building.Name}", TargetBuildingId = building.Id });
                var occupants = world.Buildings.Occupants(world, building).Count();
                if (occupants > 0) actions.Add(new() { Kind = ContextualActionKind.Examine, Label = $"Observer {building.Name} ({occupants} personne(s))", Command = $"Examine {building.Name}", TargetBuildingId = building.Id });
            }
        }
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive && n.Id != actor.Id && n.CurrentLocationId == locationId && n.CurrentBuildingId == actor.CurrentBuildingId).OrderBy(n => n.Identity.DisplayName, StringComparer.Ordinal).Take(12))
        {
            actions.Add(new() { Kind = ContextualActionKind.Talk, Label = $"Parler à {npc.Identity.DisplayName}", Command = $"Parle à {npc.Identity.DisplayName}", TargetNpcId = npc.Id });
            actions.Add(new() { Kind = ContextualActionKind.AskDirections, Label = $"Demander son chemin à {npc.Identity.DisplayName}", Command = $"Demande ton chemin à {npc.Identity.DisplayName}", TargetNpcId = npc.Id });
            actions.Add(new() { Kind = ContextualActionKind.Follow, Label = $"Suivre {npc.Identity.DisplayName}", Command = $"Suis {npc.Identity.DisplayName}", TargetNpcId = npc.Id });
        }
        foreach (var destination in world.Geography.RoadsFrom(locationId).Where(r => actor.KnownLocationIds.Contains(r.ToLocationId)).Select(r => world.Geography.Locations.TryGetValue(r.ToLocationId, out var d) ? d : null).Where(d => d is not null).Cast<Location>().OrderBy(l => l.Name, StringComparer.Ordinal))
            actions.Add(new() { Kind = ContextualActionKind.Travel, Label = $"Aller à {destination.Name}", Command = $"Va à {destination.Name}", TargetLocationId = destination.Id, RequiresKnowledge = true });
        actions.Add(new() { Kind = ContextualActionKind.Examine, Label = $"Examiner {location.Name}", Command = $"Examine {location.Name}", TargetLocationId = location.Id });
        actions.Add(new() { Kind = ContextualActionKind.Investigate, Label = "Enquêter sur les alentours", Command = "Enquête sur les alentours" });
        actions.Add(new() { Kind = ContextualActionKind.DetectDanger, Label = "Évaluer les dangers", Command = "Détecte les dangers" });
        return actions;
    }
}
