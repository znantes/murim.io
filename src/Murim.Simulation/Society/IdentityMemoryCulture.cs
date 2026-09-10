namespace Murim.Simulation;

public sealed record AliasIdentity(Guid Id, string Name, string Context, long FirstUsedDay, double Exposure, bool IsWantedIdentity = false);
public sealed class LanguageCompetency
{
    public string LanguageCode { get; init; } = "common";
    public string DialectCode { get; set; } = "central";
    public double Listening { get; set; }
    public double Speaking { get; set; }
    public double Reading { get; set; }
    public double Writing { get; set; }
}

public enum DiagnosticSubject { Injury, Qi, Technique, Illness, Identity, Artifact }
public sealed record PerceivedCondition(Guid Id, DiagnosticSubject Subject, string TargetKey, string BelievedConclusion, double Confidence, long LearnedDay, Guid? SourceNpcId, bool HasBeenCorrected = false);

public sealed class GriefState
{
    public Guid DeceasedNpcId { get; init; }
    public long DeathDay { get; init; }
    public double Intensity { get; set; }
    public double Acceptance { get; set; }
    public bool WasPresentAtDeath { get; init; }
}

public sealed class MoralDebt
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid CreditorNpcId { get; init; }
    public Guid DebtorNpcId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public double Weight { get; set; }
    public long CreatedDay { get; init; }
    public bool Inheritable { get; init; }
    public bool Settled { get; set; }
}

public sealed class FactionCultureState
{
    public Guid FactionId { get; init; }
    public double Discipline { get; set; } = .5;
    public double Meritocracy { get; set; } = .5;
    public double Nepotism { get; set; } = .25;
    public double Secrecy { get; set; } = .5;
    public double Mercy { get; set; } = .5;
    public double Tradition { get; set; } = .65;
    public double Corruption { get; set; } = .12;
    public long LastShiftDay { get; set; }
}

public sealed class HistoricalRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? UnderlyingEventId { get; init; }
    public long Day { get; init; }
    public string ActualAccount { get; init; } = string.Empty;
    public string OfficialAccount { get; set; } = string.Empty;
    public Dictionary<string, string> RegionalVersions { get; } = new(StringComparer.OrdinalIgnoreCase);
    public double OfficialCensorship { get; set; }
}

public sealed class ManuscriptRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int TechniqueId { get; init; }
    public string Title { get; init; } = string.Empty;
    public double Completeness { get; set; }
    public double Authenticity { get; set; } = 1;
    public Guid? AuthorNpcId { get; init; }
    public long CreatedDay { get; init; }
    public string Provenance { get; set; } = string.Empty;
    public bool DeliberatelyCorrupted { get; set; }
}

public sealed class SocialMemorySystem
{
    public void RegisterDeath(WorldState world, Npc deceased)
    {
        foreach (var observer in world.Npcs.Values.Where(n => n.IsAlive && n.Id != deceased.Id))
        {
            if (!observer.Relationships.TryGetValue(deceased.Id, out var relation)) continue;
            var attachment = Math.Clamp((Math.Max(0, relation.Affection) + Math.Max(0, relation.Familiarity) * .45 + relation.KinshipStrength * 70) / 170.0, 0, 1);
            if (attachment < .08) continue;
            observer.Grief[deceased.Id] = new GriefState
            {
                DeceasedNpcId = deceased.Id, DeathDay = world.Clock.Day,
                Intensity = Math.Clamp(20 + attachment * 75, 0, 100), Acceptance = 2,
                WasPresentAtDeath = observer.CurrentLocationId == deceased.CurrentLocationId
            };
        }
        TransferInheritedDebts(world, deceased);
    }

    public void AdvanceDay(WorldState world)
    {
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive))
        {
            foreach (var grief in npc.Grief.Values)
            {
                var days = Math.Max(1, world.Clock.Day - grief.DeathDay);
                var relationMemory = npc.Relationships.GetValueOrDefault(grief.DeceasedNpcId)?.Memories.Where(m => m.IsAnchor).Sum(m => m.EmotionalWeight) ?? 0;
                var resilience = npc.Personality.Empathy * .15 + npc.Personality.Patience * .25 + npc.Physiology.Spirit.EmotionalStability / 100.0 * .35;
                grief.Acceptance = Math.Clamp(grief.Acceptance + .035 + resilience * .04, 0, 100);
                var floor = Math.Clamp(relationMemory * .025, 0, 18);
                grief.Intensity = Math.Max(floor, grief.Intensity * Math.Exp(-0.6931471805599453 / (140 + days * .025)));
            }
        }
    }

    private static void TransferInheritedDebts(WorldState world, Npc deceased)
    {
        var inheritable = world.MoralDebts.Values.Where(d => !d.Settled && d.DebtorNpcId == deceased.Id && d.Inheritable).ToArray();
        var heirs = deceased.ChildIds.Select(id => world.Npcs.GetValueOrDefault(id)).Where(n => n is { IsAlive: true }).Cast<Npc>().ToArray();
        if (heirs.Length == 0) return;
        foreach (var debt in inheritable)
        {
            debt.Settled = true;
            foreach (var heir in heirs)
            {
                var inherited = new MoralDebt { CreditorNpcId = debt.CreditorNpcId, DebtorNpcId = heir.Id, Reason = $"Héritage moral : {debt.Reason}", Weight = debt.Weight / heirs.Length * .65, CreatedDay = world.Clock.Day, Inheritable = false };
                world.MoralDebts[inherited.Id] = inherited;
            }
        }
    }
}

public sealed class FactionCultureSystem
{
    public FactionCultureState GetOrCreate(WorldState world, Guid factionId)
    {
        if (world.FactionCultures.TryGetValue(factionId, out var state)) return state;
        state = new FactionCultureState { FactionId = factionId, LastShiftDay = world.Clock.Day };
        world.FactionCultures[factionId] = state;
        return state;
    }

    public void ApplyLeadershipEra(WorldState world, Guid factionId, double discipline, double meritocracy, double secrecy, double mercy, double corruption)
    {
        var c = GetOrCreate(world, factionId); const double inertia = .82;
        c.Discipline = Blend(c.Discipline, discipline, inertia); c.Meritocracy = Blend(c.Meritocracy, meritocracy, inertia);
        c.Secrecy = Blend(c.Secrecy, secrecy, inertia); c.Mercy = Blend(c.Mercy, mercy, inertia); c.Corruption = Blend(c.Corruption, corruption, inertia);
        c.Nepotism = Math.Clamp(c.Nepotism * .9 + (1 - meritocracy) * .1, 0, 1); c.LastShiftDay = world.Clock.Day;
    }
    private static double Blend(double oldValue, double newValue, double inertia) => Math.Clamp(oldValue * inertia + newValue * (1 - inertia), 0, 1);
}

public sealed class DiagnosisSystem
{
    private readonly Random random;
    public DiagnosisSystem(int seed) => random = new Random(seed);

    public PerceivedCondition ObserveInjury(Npc observer, Npc target, InjuryRecord injury, long day, double examinationQuality)
    {
        var skill = observer.Skills.GetValueOrDefault("medicine");
        var evidence = Math.Clamp(examinationQuality * .45 + skill / 100.0 * .38 + observer.Physiology.Spirit.Perception / 100.0 * .17, 0, 1);
        var correct = random.NextDouble() < (.35 + evidence * .62);
        var conclusion = correct ? injury.DisplayText : Misread(injury);
        var perceived = new PerceivedCondition(Guid.NewGuid(), DiagnosticSubject.Injury, injury.Id.ToString(), conclusion, Math.Clamp(.25 + evidence * .7, 0, .98), day, observer.Id);
        observer.PerceivedConditions.Add(perceived); return perceived;
    }

    private static string Misread(InjuryRecord injury) => injury.Kind switch
    {
        InjuryKind.Fracture => "Probable forte contusion, fracture non confirmée",
        InjuryKind.InternalTrauma => "Douleur abdominale d'origine incertaine",
        InjuryKind.QiDeviation => "Fatigue et circulation irrégulière supposées",
        InjuryKind.MeridianTrauma => "Blessure musculaire supposée",
        _ => $"Blessure mal identifiée près de {injury.Region}"
    };
}

public sealed class AliasSystem
{
    public AliasIdentity AddAlias(Npc npc, string name, string context, long day, bool wanted = false)
    {
        var alias = new AliasIdentity(Guid.NewGuid(), name, context, day, 0, wanted); npc.Aliases.Add(alias); return alias;
    }

    public void ExposeAlias(Npc npc, Guid aliasId, double amount)
    {
        var index = npc.Aliases.FindIndex(a => a.Id == aliasId); if (index < 0) return;
        var old = npc.Aliases[index]; npc.Aliases[index] = old with { Exposure = Math.Clamp(old.Exposure + amount, 0, 1) };
    }
}
