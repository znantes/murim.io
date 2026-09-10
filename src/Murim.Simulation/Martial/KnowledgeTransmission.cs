namespace Murim.Simulation;

public sealed record ArtifactLegacyEntry(long Day, Guid? NpcId, string Event, double HistoricalWeight);
public sealed class ArtifactLegacy
{
    public int ArtifactId { get; init; }
    public double HistoricalPrestige { get; set; }
    public List<ArtifactLegacyEntry> History { get; } = new();
}

public sealed class KnowledgeTransmissionSystem
{
    private readonly Random random;
    public KnowledgeTransmissionSystem(int seed) => random = new Random(seed);

    public ManuscriptRecord CreateManuscript(int techniqueId, string title, double completeness, double authenticity, long day, Guid? authorId, string provenance, bool corrupted = false)
    {
        return new ManuscriptRecord
        {
            TechniqueId = techniqueId, Title = title, Completeness = Math.Clamp(completeness, .01, 1), Authenticity = Math.Clamp(authenticity, 0, 1),
            CreatedDay = day, AuthorNpcId = authorId, Provenance = provenance, DeliberatelyCorrupted = corrupted
        };
    }

    public ManuscriptRecord Copy(ManuscriptRecord source, Npc copyist, long day)
    {
        var skill = copyist.Skills.GetValueOrDefault("literacy") * .6 + copyist.Skills.GetValueOrDefault("calligraphy") * .4;
        var errorRisk = Math.Clamp(.22 - skill / 550.0 + copyist.Body.Fatigue / 650.0, .005, .28);
        var lost = random.NextDouble() < errorRisk ? random.NextDouble() * .08 : 0;
        var authenticity = Math.Clamp(source.Authenticity - errorRisk * .08 - (source.DeliberatelyCorrupted ? .02 : 0), 0, 1);
        return CreateManuscript(source.TechniqueId, source.Title, source.Completeness - lost, authenticity, day, copyist.Id,
            $"Copie de « {source.Title} » par {copyist.Identity.DisplayName}", source.DeliberatelyCorrupted);
    }

    public double ReconstructTechnique(IEnumerable<ManuscriptRecord> fragments, Npc researcher)
    {
        var set = fragments.ToArray(); if (set.Length == 0) return 0;
        var rawCoverage = 1 - set.Aggregate(1.0, (missing, f) => missing * (1 - f.Completeness * f.Authenticity));
        var scholarship = researcher.Skills.GetValueOrDefault("scholarship");
        var perception = researcher.Physiology.Spirit.Perception;
        return Math.Clamp(rawCoverage * (.62 + scholarship / 250.0 + perception / 500.0), 0, 1);
    }

    public ManuscriptRecord ForgeFalseManual(int techniqueId, string title, Npc creator, long day, double deceptionQuality)
    {
        var authenticityAppearance = Math.Clamp(.25 + deceptionQuality * .7, 0, .96);
        return CreateManuscript(techniqueId, title, .55 + random.NextDouble() * .4, authenticityAppearance, day, creator.Id,
            "Origine volontairement falsifiée", true);
    }

    public HistoricalRecord RecordHistory(WorldState world, WorldEvent ev, string officialAccount, double censorship)
    {
        var record = new HistoricalRecord { UnderlyingEventId = ev.Id, Day = ev.Day, ActualAccount = ev.Summary, OfficialAccount = officialAccount, OfficialCensorship = Math.Clamp(censorship, 0, 1) };
        world.HistoricalRecords[record.Id] = record; return record;
    }

    public string DistortHistoricalVersion(HistoricalRecord record, string region, int generations)
    {
        if (record.RegionalVersions.TryGetValue(region, out var known)) return known;
        var drift = Math.Clamp(generations * .08 + random.NextDouble() * .15, 0, .85);
        var prefix = drift switch { < .15 => "Les chroniques locales rapportent que ", < .35 => "On raconte dans la région que ", < .6 => "La tradition affirme que ", _ => "Une légende ancienne prétend que " };
        var version = prefix + (drift > .55 ? "les protagonistes accomplirent des exploits probablement amplifiés autour de cet événement." : record.OfficialAccount);
        record.RegionalVersions[region] = version; return version;
    }

    public ArtifactLegacy RegisterArtifactEvent(WorldState world, int artifactId, long day, Guid? wielderId, string description, double importance)
    {
        if (!world.ArtifactLegacies.TryGetValue(artifactId, out var legacy))
        {
            legacy = new ArtifactLegacy { ArtifactId = artifactId }; world.ArtifactLegacies[artifactId] = legacy;
        }
        legacy.History.Add(new ArtifactLegacyEntry(day, wielderId, description, Math.Clamp(importance, 0, 1)));
        legacy.HistoricalPrestige = Math.Clamp(legacy.HistoricalPrestige + importance * 12, 0, 100);
        return legacy;
    }
}
