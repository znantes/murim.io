namespace Murim.Simulation;

public enum ObligationStatus { Proposed, Accepted, Refused, Fulfilled, Failed, Abandoned }
public enum ObligationKind { FamilyErrand, Delivery, WorkDuty, SectDuty, MilitaryOrder, Debt, Promise, Appointment, Escort, ApprenticeshipTask }
public enum GoalKind { Survive, Rest, Eat, Learn, Work, EarnMoney, ProtectFamily, Socialize, Courtship, Marry, RaiseChild, Train, SeekMaster, GainStatus, Travel, Revenge, Heal, RepayDebt, CreateWork, ServeFaction }

public sealed class NpcGoal
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid NpcId { get; init; }
    public GoalKind Kind { get; init; }
    public string Description { get; init; } = string.Empty;
    public double Priority { get; set; }
    public long CreatedDay { get; init; }
    public long? ExpiresDay { get; init; }
    public bool Completed { get; set; }
}

public sealed class Obligation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ObligationKind Kind { get; init; }
    public Guid RequesterNpcId { get; init; }
    public Guid PerformerNpcId { get; init; }
    public Guid? TargetNpcId { get; init; }
    public Guid? DestinationId { get; init; }
    public string Description { get; init; } = string.Empty;
    public long CreatedMinute { get; init; }
    public long DueMinute { get; init; }
    public ObligationStatus Status { get; set; } = ObligationStatus.Proposed;
    public double Importance { get; init; } = .5;
    public double DiscoveryDelayDays { get; init; }
    public long? ResolvedMinute { get; set; }
    public bool RequesterKnowsOutcome { get; set; }
}

public sealed class ObligationSystem
{
    public Obligation Request(WorldState world, Npc requester, Npc performer, ObligationKind kind, string description, long dueInMinutes, double importance, Guid? destinationId = null, Guid? targetNpcId = null)
    {
        var obligation = new Obligation
        {
            Kind = kind, RequesterNpcId = requester.Id, PerformerNpcId = performer.Id, TargetNpcId = targetNpcId, DestinationId = destinationId,
            Description = description, CreatedMinute = world.Clock.TotalMinutes, DueMinute = world.Clock.TotalMinutes + Math.Max(1, dueInMinutes),
            Importance = Math.Clamp(importance, 0, 1), DiscoveryDelayDays = 0.2 + importance * 2.5
        };
        world.Obligations[obligation.Id] = obligation;
        return obligation;
    }

    public void Accept(WorldState world, Guid obligationId)
    {
        if (!world.Obligations.TryGetValue(obligationId, out var o) || o.Status != ObligationStatus.Proposed) return;
        o.Status = ObligationStatus.Accepted;
    }

    public void Refuse(WorldState world, Guid obligationId, RelationshipSystem relationships)
    {
        if (!world.Obligations.TryGetValue(obligationId, out var o) || o.Status != ObligationStatus.Proposed) return;
        o.Status = ObligationStatus.Refused; o.ResolvedMinute = world.Clock.TotalMinutes; o.RequesterKnowsOutcome = true;
        if (world.Npcs.TryGetValue(o.RequesterNpcId, out var requester) && world.Npcs.TryGetValue(o.PerformerNpcId, out var performer))
        {
            var rel = relationships.GetOrCreate(requester, performer, world.Clock.Day);
            rel.Trust = Math.Clamp(rel.Trust - 1.5 * o.Importance, -100, 100);
            rel.Respect = Math.Clamp(rel.Respect - .5 * o.Importance, -100, 100);
        }
    }

    public bool Fulfill(WorldState world, Guid obligationId, RelationshipSystem relationships)
    {
        if (!world.Obligations.TryGetValue(obligationId, out var o) || o.Status is not (ObligationStatus.Accepted or ObligationStatus.Proposed)) return false;
        if (world.Clock.TotalMinutes > o.DueMinute) return false;
        o.Status = ObligationStatus.Fulfilled; o.ResolvedMinute = world.Clock.TotalMinutes; o.RequesterKnowsOutcome = true;
        if (world.Npcs.TryGetValue(o.RequesterNpcId, out var requester) && world.Npcs.TryGetValue(o.PerformerNpcId, out var performer))
        {
            var rel = relationships.GetOrCreate(requester, performer, world.Clock.Day);
            rel.Trust = Math.Clamp(rel.Trust + 4 + o.Importance * 5, -100, 100);
            rel.Respect = Math.Clamp(rel.Respect + 2 + o.Importance * 3, -100, 100);
            relationships.RecordInteraction(requester, performer, world.Clock.Day, 2 + o.Importance * 4, 4 + o.Importance * 6, 2 + o.Importance * 3, memory: $"Engagement tenu : {o.Description}", anchor: o.Importance > .8);
        }
        return true;
    }

    public void Advance(WorldState world, RelationshipSystem relationships)
    {
        foreach (var o in world.Obligations.Values.Where(x => x.Status == ObligationStatus.Accepted && world.Clock.TotalMinutes > x.DueMinute))
        {
            o.Status = ObligationStatus.Failed; o.ResolvedMinute = o.DueMinute;
        }
        foreach (var o in world.Obligations.Values.Where(x => x.Status == ObligationStatus.Failed && !x.RequesterKnowsOutcome && x.ResolvedMinute is not null))
        {
            var discoverAt = o.ResolvedMinute.Value + (long)(o.DiscoveryDelayDays * 1440);
            if (world.Clock.TotalMinutes < discoverAt) continue;
            o.RequesterKnowsOutcome = true;
            if (!world.Npcs.TryGetValue(o.RequesterNpcId, out var requester) || !world.Npcs.TryGetValue(o.PerformerNpcId, out var performer)) continue;
            var rel = relationships.GetOrCreate(requester, performer, world.Clock.Day);
            var promisePenalty = 7 + o.Importance * 11;
            rel.Trust = Math.Clamp(rel.Trust - promisePenalty, -100, 100);
            rel.Respect = Math.Clamp(rel.Respect - 2 - o.Importance * 5, -100, 100);
            rel.Resentment = Math.Clamp(rel.Resentment + o.Importance * 4, 0, 100);
            relationships.RecordInteraction(requester, performer, world.Clock.Day, -2 - o.Importance * 5, -promisePenalty * .4, -1 - o.Importance * 3, memory: $"Promesse non tenue : {o.Description}", anchor: o.Importance > .85);
        }
    }

    public double ReliabilityPerception(WorldState world, Guid observerId, Guid performerId)
    {
        var known = world.Obligations.Values.Where(o => o.RequesterNpcId == observerId && o.PerformerNpcId == performerId && o.RequesterKnowsOutcome && o.Status is ObligationStatus.Fulfilled or ObligationStatus.Failed).ToArray();
        if (known.Length == 0) return .5;
        var weight = known.Sum(o => .25 + o.Importance); var success = known.Where(o => o.Status == ObligationStatus.Fulfilled).Sum(o => .25 + o.Importance);
        return weight <= 0 ? .5 : success / weight;
    }
}

public sealed class GoalSystem
{
    public void RefreshGoals(WorldState world, Npc npc)
    {
        var existing = world.Goals.Values.Where(g => g.NpcId == npc.Id && !g.Completed && (g.ExpiresDay is null || g.ExpiresDay >= world.Clock.Day)).ToArray();
        if (existing.Length > 8) return;
        AddNeedGoal(world, npc, GoalKind.Eat, "Trouver de quoi manger", npc.Body.Hunger / 100.0);
        AddNeedGoal(world, npc, GoalKind.Rest, "Se reposer", Math.Max(npc.Body.SleepDebt, npc.Body.Fatigue) / 100.0);
        if (npc.Injuries.Any(i => i.Active && i.Severity >= InjurySeverity.Moderate)) AddNeedGoal(world, npc, GoalKind.Heal, "Faire soigner ses blessures", .85);
        if (npc.AgeYears(world.Clock) >= 10 && npc.Personality.Curiosity > .6) AddNeedGoal(world, npc, GoalKind.Learn, "Apprendre quelque chose de nouveau", .3 + npc.Personality.Curiosity * .45);
        if (npc.AgeYears(world.Clock) >= 13 && npc.Personality.Ambition > .68) AddNeedGoal(world, npc, GoalKind.GainStatus, "Améliorer sa position dans le monde", .25 + npc.Personality.Ambition * .55);
        if (npc.AgeYears(world.Clock) >= 16 && npc.CareerHistory.Count == 0) AddNeedGoal(world, npc, GoalKind.Work, "Trouver un moyen de gagner sa vie", .62);
        if (npc.SpouseId is not null || npc.ChildIds.Count > 0) AddNeedGoal(world, npc, GoalKind.ProtectFamily, "Préserver son foyer", .35 + npc.Personality.Loyalty * .55);
    }

    private static void AddNeedGoal(WorldState world, Npc npc, GoalKind kind, string description, double priority)
    {
        if (priority < .2 || world.Goals.Values.Any(g => g.NpcId == npc.Id && g.Kind == kind && !g.Completed)) return;
        var goal = new NpcGoal { NpcId = npc.Id, Kind = kind, Description = description, Priority = Math.Clamp(priority, 0, 1), CreatedDay = world.Clock.Day, ExpiresDay = world.Clock.Day + 30 };
        world.Goals[goal.Id] = goal;
    }
}
