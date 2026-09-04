namespace Murim.World;

public enum MartialConflictType { Rivalry, TerritoryDispute, BloodFeud, Tournament, Alliance, War }

public sealed class MartialConflict
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid FirstOrganizationId { get; init; }
    public Guid SecondOrganizationId { get; init; }
    public MartialConflictType Type { get; init; }
    public double Tension { get; set; }
    public long StartedDay { get; init; }
    public bool Active { get; set; } = true;
    public string Cause { get; init; } = string.Empty;
}

public sealed class MartialConflictSystem
{
    private readonly Dictionary<Guid, MartialConflict> _conflicts = new();
    public IReadOnlyDictionary<Guid, MartialConflict> Conflicts => _conflicts;

    public MartialConflict Create(MartialOrganization first, MartialOrganization second, MartialConflictType type, WorldState world, string cause, double initialTension = 20)
    {
        if (first.Id == second.Id) throw new InvalidOperationException("Une organisation ne peut pas entrer en conflit avec elle-même.");
        var existing = _conflicts.Values.FirstOrDefault(c => c.Active && ((c.FirstOrganizationId == first.Id && c.SecondOrganizationId == second.Id) || (c.FirstOrganizationId == second.Id && c.SecondOrganizationId == first.Id)));
        if (existing is not null) return existing;
        var conflict = new MartialConflict { FirstOrganizationId = first.Id, SecondOrganizationId = second.Id, Type = type, Tension = Math.Clamp(initialTension, 0, 100), StartedDay = world.Time.Day, Cause = cause };
        _conflicts[conflict.Id] = conflict;
        first.Reputation = Math.Clamp(first.Reputation - 0.2, -100, 100);
        second.Reputation = Math.Clamp(second.Reputation - 0.2, -100, 100);
        Publish(world, first, $"Conflit {type}", $"{first.Name} entre en conflit avec {second.Name}. Cause : {cause}.", -0.25);
        return conflict;
    }

    public bool Escalate(MartialConflict conflict, WorldState world, double amount, string cause)
    {
        if (!conflict.Active) return false;
        conflict.Tension = Math.Clamp(conflict.Tension + amount, 0, 100);
        if (amount > 0.01 && world.MartialOrganizations.Organizations.TryGetValue(conflict.FirstOrganizationId, out var first) && world.MartialOrganizations.Organizations.TryGetValue(conflict.SecondOrganizationId, out var second))
        {
            Publish(world, first, "Escalade", $"La tension avec {second.Name} augmente : {cause}.", -0.35);
            Publish(world, second, "Escalade", $"La tension avec {first.Name} augmente : {cause}.", -0.35);
        }
        return true;
    }

    public bool Resolve(MartialConflict conflict, WorldState world, string reason)
    {
        if (!conflict.Active) return false;
        conflict.Active = false;
        conflict.Tension = Math.Max(0, conflict.Tension - 25);
        if (world.MartialOrganizations.Organizations.TryGetValue(conflict.FirstOrganizationId, out var first) && world.MartialOrganizations.Organizations.TryGetValue(conflict.SecondOrganizationId, out var second))
        {
            first.Reputation = Math.Clamp(first.Reputation + 0.5, -100, 100);
            second.Reputation = Math.Clamp(second.Reputation + 0.5, -100, 100);
            Publish(world, first, "Paix", $"Le conflit avec {second.Name} prend fin : {reason}.", 0.35);
            Publish(world, second, "Paix", $"Le conflit avec {first.Name} prend fin : {reason}.", 0.35);
        }
        return true;
    }

    public void AdvanceDay(WorldState world)
    {
        foreach (var conflict in _conflicts.Values.Where(c => c.Active).ToList())
        {
            if (!world.MartialOrganizations.Organizations.TryGetValue(conflict.FirstOrganizationId, out var first) || !world.MartialOrganizations.Organizations.TryGetValue(conflict.SecondOrganizationId, out var second)) { conflict.Active = false; continue; }
            var pressure = (first.Reputation + second.Reputation) / 200.0;
            var drift = conflict.Type == MartialConflictType.War ? 0.8 : 0.15;
            conflict.Tension = Math.Clamp(conflict.Tension + drift - pressure * 0.1, 0, 100);
            if (conflict.Tension >= 80 && conflict.Type != MartialConflictType.War) conflict.Tension = Math.Min(100, conflict.Tension + 0.2);
            if (conflict.Tension >= 95) Publish(world, first, "Tension", $"Le conflit avec {second.Name} atteint un niveau critique.", -0.65);
        }
    }

    private static void Publish(WorldState world, MartialOrganization source, string topic, string content, double polarity)
    {
        var sourceNpc = world.Npcs.Values.FirstOrDefault(n => n.IsAlive && n.Profession.OrganizationId == source.Id);
        if (sourceNpc is not null) world.Information.Publish(world, sourceNpc, topic, content, null, sourceNpc.CurrentLocationId, InformationReliability.Unverified, polarity);
    }
}
