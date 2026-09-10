namespace Murim.Simulation;

public enum TournamentPhase { Announced, Recommendations, RegionalQualifiers, MainEvent, Completed, Cancelled }

public sealed class TournamentInstance
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string ArchetypeCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public long AnnouncementDay { get; init; }
    public long StartDay { get; init; }
    public long EndDay { get; init; }
    public TournamentPhase Phase { get; set; }
    public Guid HostLocationId { get; init; }
    public List<Guid> RecommendedNpcIds { get; } = new();
    public List<Guid> QualifiedNpcIds { get; } = new();
    public Guid? DragonTitleNpcId { get; set; }
    public Guid? PhoenixTitleNpcId { get; set; }
    public Dictionary<Guid, double> PrestigeGained { get; } = new();
}

public static class ExtendedTournamentCatalog
{
    public static TournamentArchetype DragonPhoenix { get; } = new(
        "dragon_phoenix_gathering", "Assemblée du Dragon et du Phénix", TournamentFormat.MixedTests, TournamentScope.Murim, 14,
        MartialRealm.ThirdRate, MartialRealm.Transformation, false, 96,
        [TechniqueDomain.Sword, TechniqueDomain.Saber, TechniqueDomain.Spear, TechniqueDomain.Fist, TechniqueDomain.Palm, TechniqueDomain.Qinggong, TechniqueDomain.HiddenWeapon, TechniqueDomain.Strategy],
        ["titre générationnel de Dragon ou Phénix", "renommée dans le Murim", "recommandations de maîtres", "alliances et rivalités", "offres de recrutement"],
        "Grand rassemblement générationnel inspiré du trope Murim 용봉지회/龍鳳之會 : recommandations, qualifications et exposition politique des jeunes talents.");

    public static IReadOnlyList<TournamentArchetype> All { get; } = [.. TournamentCatalog.All, DragonPhoenix];
}

public sealed class DragonPhoenixTournamentSystem
{
    private readonly Random random;
    public DragonPhoenixTournamentSystem(int seed) => random = new Random(seed);

    public TournamentInstance Schedule(WorldState world, Guid hostLocationId, int daysUntilStart = 120, int durationDays = 12)
    {
        if (!world.Locations.ContainsKey(hostLocationId)) throw new ArgumentException("Lieu de tournoi inconnu.", nameof(hostLocationId));
        var instance = new TournamentInstance
        {
            ArchetypeCode = ExtendedTournamentCatalog.DragonPhoenix.Code,
            Name = ExtendedTournamentCatalog.DragonPhoenix.Name,
            AnnouncementDay = world.Clock.Day, StartDay = world.Clock.Day + Math.Max(30, daysUntilStart),
            EndDay = world.Clock.Day + Math.Max(30, daysUntilStart) + Math.Max(3, durationDays), Phase = TournamentPhase.Announced, HostLocationId = hostLocationId
        };
        world.Tournaments[instance.Id] = instance;
        world.Events.Add(new WorldEvent(Guid.NewGuid(), WorldEventType.TournamentAnnouncement, world.Clock.Day, hostLocationId, [], [],
            $"{instance.Name} est annoncée. Les grandes factions commencent à recommander leurs jeunes talents.", .92, .78, .10, []));
        return instance;
    }

    public bool Recommend(WorldState world, TournamentInstance tournament, Npc candidate, Guid recommendingFactionId)
    {
        if (!world.Factions.ContainsKey(recommendingFactionId) || candidate.AgeYears(world.Clock) < 14 || candidate.AgeYears(world.Clock) > 32) return false;
        if ((int)candidate.Martial.Realm < (int)MartialRealm.ThirdRate || (int)candidate.Martial.Realm > (int)MartialRealm.Transformation) return false;
        if (!candidate.AffiliationIds.Contains(recommendingFactionId) && candidate.PrimaryFactionId != recommendingFactionId) return false;
        if (!tournament.RecommendedNpcIds.Contains(candidate.Id)) tournament.RecommendedNpcIds.Add(candidate.Id);
        tournament.Phase = TournamentPhase.Recommendations; return true;
    }

    public void RunQualifiers(WorldState world, TournamentInstance tournament)
    {
        tournament.Phase = TournamentPhase.RegionalQualifiers;
        var scored = tournament.RecommendedNpcIds.Select(id => world.Npcs.GetValueOrDefault(id)).Where(n => n is { IsAlive: true }).Cast<Npc>()
            .Select(n => (Npc: n, Score: CandidateScore(n) + random.NextDouble() * 8)).OrderByDescending(x => x.Score).Take(96).ToArray();
        tournament.QualifiedNpcIds.Clear(); tournament.QualifiedNpcIds.AddRange(scored.Select(x => x.Npc.Id));
    }

    public void Resolve(WorldState world, TournamentInstance tournament)
    {
        tournament.Phase = TournamentPhase.MainEvent;
        var candidates = tournament.QualifiedNpcIds.Select(id => world.Npcs.GetValueOrDefault(id)).Where(n => n is { IsAlive: true }).Cast<Npc>()
            .Select(n => (Npc: n, Score: CandidateScore(n) + random.NextDouble() * 15)).OrderByDescending(x => x.Score).ToArray();
        if (candidates.Length == 0) { tournament.Phase = TournamentPhase.Cancelled; return; }
        tournament.DragonTitleNpcId = candidates[0].Npc.Id;
        tournament.PhoenixTitleNpcId = candidates.Length > 1 ? candidates[1].Npc.Id : candidates[0].Npc.Id;
        foreach (var item in candidates.Take(16).Select((x, i) => (x.Npc, Index: i))) tournament.PrestigeGained[item.Npc.Id] = Math.Max(2, 18 - item.Index);
        tournament.Phase = TournamentPhase.Completed;
        world.Events.Add(new WorldEvent(Guid.NewGuid(), WorldEventType.TournamentResult, tournament.EndDay, tournament.HostLocationId,
            candidates.Take(8).Select(x => x.Npc.Id).ToArray(), [], $"{tournament.Name} s'achève ; ses résultats alimenteront rivalités et rumeurs pendant des années.", .98, .82, .08, []));
    }

    private static double CandidateScore(Npc npc)
    {
        var realm = (int)npc.Martial.Realm * 15 + (int)npc.Martial.SubRank * 4 + npc.Martial.StageProgress * .08;
        var body = npc.Physiology.Physical.Coordination * .12 + npc.Physiology.Endurance.WorkCapacity * .08;
        var mind = npc.Physiology.Spirit.Perception * .09 + npc.Physiology.Spirit.Willpower * .06;
        var mastery = npc.Techniques.Values.OrderByDescending(t => t.PracticePoints).Take(3).Sum(t => t.PracticePoints / 90.0);
        var injury = npc.Injuries.Where(i => i.Active).Sum(i => i.FunctionalLoss * 10);
        return realm + body + mind + mastery - injury;
    }
}
