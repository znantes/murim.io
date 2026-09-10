namespace Murim.Simulation;

public sealed class MartialGenerationRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int BirthYearStart { get; init; }
    public int BirthYearEnd { get; init; }
    public long EvaluatedDay { get; set; }
    public string CanonicalName { get; set; } = string.Empty;
    public Dictionary<string, string> RegionalNames { get; } = new(StringComparer.OrdinalIgnoreCase);
    public double MartialDensity { get; set; }
    public double ExceptionalTalentDensity { get; set; }
    public double ViolenceIndex { get; set; }
    public double InnovationIndex { get; set; }
    public double PoliticalImpact { get; set; }
    public List<Guid> NotableNpcIds { get; } = new();
    public List<string> Reasons { get; } = new();
}

public sealed class MartialGenerationSystem
{
    private readonly Dictionary<(int Start, int End), MartialGenerationRecord> records = new();
    public IReadOnlyCollection<MartialGenerationRecord> Records => records.Values;

    public MartialGenerationRecord Evaluate(WorldState world, int birthYearStart, int spanYears = 12)
    {
        spanYears = Math.Clamp(spanYears, 5, 30);
        var end = birthYearStart + spanYears - 1;
        var key = (birthYearStart, end);
        if (!records.TryGetValue(key, out var record))
        {
            record = new MartialGenerationRecord { BirthYearStart = birthYearStart, BirthYearEnd = end };
            records[key] = record;
        }

        var cohort = world.Npcs.Values.Where(n => BirthYear(n) >= birthYearStart && BirthYear(n) <= end).ToArray();
        if (cohort.Length == 0)
        {
            record.CanonicalName = $"Génération {birthYearStart}-{end}";
            return record;
        }

        var cohortIds = cohort.Select(n => n.Id).ToHashSet();
        var martial = cohort.Where(n => n.Martial.Realm != MartialRealm.Untrained).ToArray();
        var elites = cohort.Where(n => n.Martial.Realm >= MartialRealm.Peak).ToArray();
        var creatorIds = world.TechniqueVariants.Select(v => v.CreatorNpcId).ToHashSet();
        var innovators = cohort.Count(n => creatorIds.Contains(n.Id));
        var deaths = cohort.Count(n => !n.IsAlive);
        var relatedEvents = world.Events.Where(e => e.ActorNpcIds.Any(cohortIds.Contains)).ToArray();
        var eventCounts = relatedEvents.SelectMany(e => e.ActorNpcIds).Where(cohortIds.Contains).GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        var violentEvents = relatedEvents.Count(e => e.Type is WorldEventType.Duel or WorldEventType.Feud or WorldEventType.Raid or WorldEventType.BanditAttack or WorldEventType.MonsterAttack or WorldEventType.Coup);
        var politicalEvents = relatedEvents.Count(e => e.Type is WorldEventType.SectSplit or WorldEventType.SectAlliance or WorldEventType.Succession or WorldEventType.Coup or WorldEventType.ImperialEdict);

        record.EvaluatedDay = world.Clock.Day;
        record.MartialDensity = martial.Length / (double)cohort.Length;
        record.ExceptionalTalentDensity = elites.Length / (double)cohort.Length;
        record.InnovationIndex = Math.Clamp(innovators / Math.Max(1.0, cohort.Length * .01), 0, 1);
        record.ViolenceIndex = Math.Clamp((violentEvents + deaths * .03) / Math.Max(1.0, cohort.Length * .08), 0, 1);
        record.PoliticalImpact = Math.Clamp(politicalEvents / Math.Max(1.0, cohort.Length * .01), 0, 1);

        record.NotableNpcIds.Clear();
        record.NotableNpcIds.AddRange(cohort.OrderByDescending(n => Notability(n, eventCounts.GetValueOrDefault(n.Id))).Take(12).Select(n => n.Id));
        record.Reasons.Clear();
        record.CanonicalName = ChooseName(record);
        record.Reasons.Add($"densité martiale {record.MartialDensity:P0}");
        record.Reasons.Add($"densité de maîtres {record.ExceptionalTalentDensity:P1}");
        if (record.ViolenceIndex > .6) record.Reasons.Add("époque particulièrement violente");
        if (record.InnovationIndex > .55) record.Reasons.Add("nombreuses variantes et créations martiales");
        if (record.PoliticalImpact > .5) record.Reasons.Add("fort impact sur les sectes, familles ou l'Empire");
        return record;
    }

    public string NameForRegion(MartialGenerationRecord record, string region, int seed)
    {
        if (record.RegionalNames.TryGetValue(region, out var known)) return known;
        var rng = new Random(HashCode.Combine(record.BirthYearStart, record.BirthYearEnd, region, seed));
        string[] violent = ["Génération Sanglante", "Génération des Lames Brisées", "Génération des Funérailles", "Génération de la Cendre Rouge"];
        string[] brilliant = ["Génération des Cent Lames", "Génération des Étoiles Montantes", "Génération du Jade Éclatant", "Génération des Neuf Prodiges"];
        string[] innovative = ["Génération des Mille Voies", "Génération des Arts Nouveaux", "Génération des Manuels Vivants", "Génération des Maîtres Errants"];
        string[] political = ["Génération des Bannières", "Génération des Serments Brisés", "Génération des Trois Alliances", "Génération des Héritiers"];
        string[] quiet = ["Génération Silencieuse", "Génération du Long Printemps", "Génération des Rivières Calmes", "Génération Ordinaire"];
        var pool = record.ViolenceIndex > .65 ? violent : record.ExceptionalTalentDensity > .035 ? brilliant : record.InnovationIndex > .5 ? innovative : record.PoliticalImpact > .55 ? political : quiet;
        var name = pool[rng.Next(pool.Length)];
        record.RegionalNames[region] = name;
        return name;
    }

    private static string ChooseName(MartialGenerationRecord r)
    {
        if (r.ViolenceIndex > .78 && r.ExceptionalTalentDensity > .02) return "Génération des Dragons Sanglants";
        if (r.ExceptionalTalentDensity > .05) return "Génération des Cent Lames";
        if (r.InnovationIndex > .62) return "Génération des Mille Voies";
        if (r.PoliticalImpact > .62) return "Génération des Bannières";
        if (r.ViolenceIndex < .2 && r.MartialDensity < .08) return "Génération Silencieuse";
        return "Génération des Rivières Changeantes";
    }

    private static int BirthYear(Npc npc) => (int)Math.Floor((npc.Identity.BirthDay - 1) / 365.0) + 1;
    private static double Notability(Npc npc, int eventCount)
    {
        var realm = (int)npc.Martial.Realm * 12 + (int)npc.Martial.SubRank * 2;
        var techniques = npc.Techniques.Values.Sum(t => (int)t.Stage + 1) * .7;
        var events = eventCount * .8;
        var reputation = npc.CareerStandings.Values.Sum(c => c.EmployerTrust) * .02;
        return realm + techniques + events + reputation;
    }
}
