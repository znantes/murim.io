namespace Murim.World;

/// <summary>
/// Schedules small world events that happen without player input.
/// Encounters now pass through a lightweight social interaction pipeline:
/// observation -> interpretation -> reaction -> relationship -> memory.
/// </summary>
public sealed class AutonomousWorldEventSystem
{
    private readonly HashSet<long> _scheduledDays = new();

    public void EnsureScheduled(WorldState world)
    {
        var day = world.Time.Day;
        if (!_scheduledDays.Add(day))
            return;

        var candidates = world.Npcs.Values
            .Where(n => n.IsAlive && n.CurrentLocationId is not null)
            .GroupBy(n => n.CurrentLocationId!.Value)
            .SelectMany(group => group.OrderBy(n => n.Id).Take(8))
            .ToArray();

        if (candidates.Length < 2)
            return;

        var random = new Random(DeterministicRandomSeed.Create(world.WorldSeed, day));
        var first = candidates[random.Next(candidates.Length)];
        var sameLocation = candidates
            .Where(n => n.Id != first.Id && n.CurrentLocationId == first.CurrentLocationId)
            .ToArray();
        if (sameLocation.Length == 0)
            return;

        var second = sameLocation[random.Next(sameLocation.Length)];
        var minute = 15 + random.Next(90);

        world.Events.Schedule(
            world.Time,
            minute,
            "Interaction sociale autonome",
            $"{first.Identity.Name} rencontre {second.Identity.Name} sans intervention du joueur.",
            state => ResolveEncounter(state, first.Id, second.Id));
    }

    private static void ResolveEncounter(WorldState world, Guid firstId, Guid secondId)
    {
        if (!world.Npcs.TryGetValue(firstId, out var first) ||
            !world.Npcs.TryGetValue(secondId, out var second) ||
            !first.IsAlive || !second.IsAlive ||
            first.CurrentLocationId != second.CurrentLocationId)
            return;

        if (AreFamilyRelations(first, second))
        {
            AddMemory(first, second, "Reconnaît un membre de sa famille lors d'une rencontre.");
            AddMemory(second, first, "Reconnaît un membre de sa famille lors d'une rencontre.");
            return;
        }

        var random = new Random(DeterministicRandomSeed.Create(world.WorldSeed + 17, world.Time.Day, first.Id, second.Id));
        var existing = first.Relationships.FirstOrDefault(r => r.ToNpcId == second.Id && r.IsActive);
        var compatibility = (first.Personality.Sociability + first.Personality.Empathy + second.Personality.Sociability + second.Personality.Empathy) / 4.0;
        var warmth = compatibility + random.NextDouble() * 0.35 - 0.15;

        if (existing is null)
        {
            var type = warmth >= 0.62 ? RelationshipType.Friend : warmth < 0.28 ? RelationshipType.Rival : RelationshipType.Acquaintance;
            var affinity = Math.Clamp((warmth - 0.5) * 1.4, -0.35, 0.65);
            var trust = Math.Clamp(0.18 + first.Personality.Empathy * 0.15 + second.Personality.Empathy * 0.10, 0.05, 0.55);
            var respect = Math.Clamp(0.20 + (first.Personality.Patience + second.Personality.Patience) * 0.10, 0.05, 0.55);
            LinkBidirectional(first, second, type, affinity, trust, respect);
            RecordReaction(first, second, type);
            RecordReaction(second, first, type);
            return;
        }

        var before = existing.Type;
        var delta = warmth >= 0.55 ? 0.05 : warmth < 0.25 ? -0.06 : 0.015;
        var trustDelta = warmth >= 0.55 ? 0.025 : warmth < 0.25 ? -0.03 : 0.01;
        existing.Shift(delta, trustDelta, delta * 0.5);
        FindReverse(second, first)?.Shift(delta, trustDelta, delta * 0.5);

        if (existing.Type == RelationshipType.Acquaintance && existing.Affinity >= 0.45 && existing.Trust >= 0.35)
        {
            existing.Type = RelationshipType.Friend;
            FindReverse(second, first)?.Type = RelationshipType.Friend;
        }
        else if (existing.Type == RelationshipType.Acquaintance && existing.Affinity <= -0.20)
        {
            existing.Type = RelationshipType.Rival;
            FindReverse(second, first)?.Type = RelationshipType.Rival;
        }

        RecordMemory(first, second, before, existing.Type);
        RecordMemory(second, first, before, FindReverse(second, first)?.Type ?? existing.Type);
    }

    private static void RecordReaction(Npc observer, Npc other, RelationshipType type)
    {
        var description = type switch
        {
            RelationshipType.Friend => $"Réagit chaleureusement à la présence de {other.Identity.Name}.",
            RelationshipType.Rival => $"Se montre méfiant envers {other.Identity.Name}.",
            _ => $"Observe {other.Identity.Name} et fait connaissance."
        };
        AddMemory(observer, other, description);
    }

    private static void RecordMemory(Npc observer, Npc other, RelationshipType before, RelationshipType after)
    {
        var description = before == after
            ? $"Échange avec {other.Identity.Name}; le lien évolue légèrement."
            : $"Après un échange avec {other.Identity.Name}, le lien devient {after}.";
        AddMemory(observer, other, description);
    }

    private static void AddMemory(Npc observer, Npc other, string description)
    {
        observer.History.Add("Interaction sociale", observer.AgeYears, description);
    }

    private static bool AreFamilyRelations(Npc first, Npc second)
    {
        return first.Relationships.Any(r => r.IsActive && r.ToNpcId == second.Id &&
            r.Type is RelationshipType.Parent or RelationshipType.Child or RelationshipType.Sibling) ||
            second.Relationships.Any(r => r.IsActive && r.ToNpcId == first.Id &&
            r.Type is RelationshipType.Parent or RelationshipType.Child or RelationshipType.Sibling);
    }

    private static void LinkBidirectional(Npc first, Npc second, RelationshipType type, double affinity, double trust, double respect)
    {
        first.Relationships.Add(new Relationship { FromNpcId = first.Id, ToNpcId = second.Id, Type = type, Affinity = affinity, Trust = trust, Respect = respect });
        second.Relationships.Add(new Relationship { FromNpcId = second.Id, ToNpcId = first.Id, Type = type, Affinity = affinity, Trust = trust, Respect = respect });
    }

    private static Relationship? FindReverse(Npc observer, Npc other)
        => observer.Relationships.FirstOrDefault(r => r.ToNpcId == other.Id && r.IsActive);
}
