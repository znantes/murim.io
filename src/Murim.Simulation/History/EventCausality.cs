namespace Murim.Simulation;

public enum CausalMechanism
{
    DirectAction, Retaliation, ResourceShortage, EconomicPressure, PoliticalPressure, SuccessionVacuum,
    MigrationPressure, EcologicalPressure, RumorEscalation, LegalPrecedent, InfrastructureDamage, PersonalObligation,
    FamilyConflict, FactionConflict, NaturalDisaster, Unknown
}

public sealed record EventCausalLink(
    Guid CauseEventId,
    Guid EffectEventId,
    CausalMechanism Mechanism,
    double Strength,
    long DelayDays,
    string Explanation);

public sealed class EventCausalitySystem
{
    private readonly Dictionary<Guid, List<EventCausalLink>> causesByEffect = new();
    private readonly Dictionary<Guid, List<EventCausalLink>> effectsByCause = new();

    public EventCausalLink Link(WorldState world, Guid causeEventId, Guid effectEventId, CausalMechanism mechanism, double strength, string explanation)
    {
        var cause = world.Events.FirstOrDefault(e => e.Id == causeEventId) ?? throw new ArgumentException("Cause event not found.", nameof(causeEventId));
        var effect = world.Events.FirstOrDefault(e => e.Id == effectEventId) ?? throw new ArgumentException("Effect event not found.", nameof(effectEventId));
        if (effect.Day < cause.Day) throw new InvalidOperationException("An effect cannot occur before its recorded cause.");
        if (causeEventId == effectEventId) throw new InvalidOperationException("An event cannot cause itself.");

        var link = new EventCausalLink(causeEventId, effectEventId, mechanism, Math.Clamp(strength, 0, 1), effect.Day - cause.Day, explanation);
        Get(causesByEffect, effectEventId).Add(link);
        Get(effectsByCause, causeEventId).Add(link);
        return link;
    }

    public IReadOnlyList<EventCausalLink> CausesOf(Guid eventId) => causesByEffect.GetValueOrDefault(eventId) ?? [];
    public IReadOnlyList<EventCausalLink> EffectsOf(Guid eventId) => effectsByCause.GetValueOrDefault(eventId) ?? [];

    public IReadOnlyList<(WorldEvent Event, int Depth, double CumulativeStrength)> TraceRootCauses(WorldState world, Guid eventId, int maxDepth = 6)
    {
        maxDepth = Math.Clamp(maxDepth, 1, 20);
        var output = new List<(WorldEvent Event, int Depth, double CumulativeStrength)>();
        var visited = new HashSet<Guid> { eventId };
        var queue = new Queue<(Guid EventId, int Depth, double Strength)>();
        queue.Enqueue((eventId, 0, 1));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.Depth >= maxDepth) continue;
            foreach (var link in CausesOf(current.EventId).OrderByDescending(x => x.Strength))
            {
                if (!visited.Add(link.CauseEventId)) continue;
                var cause = world.Events.FirstOrDefault(e => e.Id == link.CauseEventId);
                if (cause is null) continue;
                var strength = current.Strength * link.Strength;
                output.Add((cause, current.Depth + 1, strength));
                queue.Enqueue((cause.Id, current.Depth + 1, strength));
            }
        }
        return output.OrderBy(x => x.Depth).ThenByDescending(x => x.CumulativeStrength).ToArray();
    }

    public CausalMechanism SuggestMechanism(WorldEvent cause, WorldEvent effect)
    {
        if ((cause.Type is WorldEventType.Drought or WorldEventType.Flood or WorldEventType.Fire) && (effect.Type is WorldEventType.Shortage or WorldEventType.Famine))
            return CausalMechanism.ResourceShortage;
        if (cause.Type == WorldEventType.Death && effect.Type == WorldEventType.Succession)
            return CausalMechanism.SuccessionVacuum;
        if ((cause.Type is WorldEventType.BanditAttack or WorldEventType.Raid) && (effect.Type is WorldEventType.Migration or WorldEventType.Shortage))
            return CausalMechanism.InfrastructureDamage;
        if (cause.Type == WorldEventType.RumorWave && (effect.Type is WorldEventType.Scandal or WorldEventType.Feud))
            return CausalMechanism.RumorEscalation;
        if (cause.Type == WorldEventType.ImperialEdict && (effect.Type is WorldEventType.TaxChange or WorldEventType.Arrest or WorldEventType.Migration))
            return CausalMechanism.LegalPrecedent;
        if (cause.FactionIds.Intersect(effect.FactionIds).Any() && (effect.Type is WorldEventType.Feud or WorldEventType.Coup or WorldEventType.SectSplit))
            return CausalMechanism.FactionConflict;
        return CausalMechanism.Unknown;
    }

    private static List<EventCausalLink> Get(Dictionary<Guid, List<EventCausalLink>> map, Guid id)
    {
        if (map.TryGetValue(id, out var list)) return list;
        list = new List<EventCausalLink>();
        map[id] = list;
        return list;
    }
}
