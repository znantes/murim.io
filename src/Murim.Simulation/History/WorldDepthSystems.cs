namespace Murim.Simulation;

public enum CalendarKind { ImperialReign, SectFounding, LocalDisaster, ScholarCycle }
public sealed record HistoricalCalendar(Guid Id, string Name, CalendarKind Kind, long OriginDay, string YearPrefix, string? FounderOrEvent = null);

public sealed class HistoricalCalendarSystem
{
    private readonly Dictionary<Guid, HistoricalCalendar> calendars = new();
    public IReadOnlyCollection<HistoricalCalendar> Calendars => calendars.Values;

    public HistoricalCalendar Register(string name, CalendarKind kind, long originDay, string yearPrefix, string? founderOrEvent = null)
    {
        var calendar = new HistoricalCalendar(Guid.NewGuid(), name, kind, Math.Max(1, originDay), yearPrefix, founderOrEvent);
        calendars[calendar.Id] = calendar;
        return calendar;
    }

    public string Format(long worldDay, HistoricalCalendar calendar)
    {
        var elapsed = worldDay - calendar.OriginDay;
        if (elapsed < 0) return $"{Math.Abs(elapsed) / 365 + 1} an(s) avant {calendar.Name}";
        var year = (int)(elapsed / 365) + 1;
        var day = (int)(elapsed % 365) + 1;
        return $"{calendar.YearPrefix} {year}, jour {day}";
    }

    public IReadOnlyList<string> ParallelDates(long worldDay) => calendars.Values.OrderBy(c => c.OriginDay).Select(c => $"{c.Name} : {Format(worldDay, c)}").ToArray();
}

public enum RuinState { Fresh, Weathered, Overgrown, Collapsed, Buried, Forgotten }
public sealed class RuinSite
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SourceLocationId { get; init; }
    public string OriginalName { get; init; } = string.Empty;
    public long DestroyedDay { get; init; }
    public string Cause { get; init; } = string.Empty;
    public double StructuralIntegrity { get; set; } = 1;
    public double VegetationReclaim { get; set; }
    public double LootedFraction { get; set; }
    public double BurialFraction { get; set; }
    public bool PubliclyKnown { get; set; } = true;
    public HashSet<string> SurvivingFeatures { get; } = new(StringComparer.OrdinalIgnoreCase);
    public RuinState State { get; set; } = RuinState.Fresh;
}

public sealed class RuinSystem
{
    private readonly Dictionary<Guid, RuinSite> sites = new();
    public IReadOnlyDictionary<Guid, RuinSite> Sites => sites;

    public RuinSite Create(WorldLocation location, long day, string cause, IEnumerable<string>? survivingFeatures = null)
    {
        var site = new RuinSite { SourceLocationId = location.Id, OriginalName = location.Name, DestroyedDay = day, Cause = cause };
        foreach (var feature in survivingFeatures ?? ["fondations", "puits", "murs", "archives dispersées"]) site.SurvivingFeatures.Add(feature);
        sites[site.Id] = site;
        return site;
    }

    public void Advance(RuinSite site, long currentDay, double climateHarshness, double humanTraffic)
    {
        var years = Math.Max(0, (currentDay - site.DestroyedDay) / 365.0);
        site.StructuralIntegrity = Math.Clamp(1 - years * (.006 + climateHarshness * .008), 0, 1);
        site.VegetationReclaim = Math.Clamp(years * (.012 + climateHarshness * .004), 0, 1);
        site.LootedFraction = Math.Clamp(site.LootedFraction + humanTraffic * Math.Min(1, years / 80.0) * .08, 0, 1);
        site.BurialFraction = Math.Clamp(years * climateHarshness * .003, 0, 1);
        site.State = site.BurialFraction > .78 ? RuinState.Buried : site.StructuralIntegrity < .12 ? RuinState.Collapsed : site.VegetationReclaim > .65 ? RuinState.Overgrown : years > 8 ? RuinState.Weathered : RuinState.Fresh;
        if (years > 120 && humanTraffic < .08) { site.State = RuinState.Forgotten; site.PubliclyKnown = false; }
    }
}

public sealed class LineageBranch
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? ParentBranchId { get; init; }
    public string CurrentFamilyName { get; set; } = string.Empty;
    public string OriginFamilyName { get; init; } = string.Empty;
    public long FoundedDay { get; init; }
    public string SeparationReason { get; init; } = string.Empty;
    public HashSet<Guid> FounderNpcIds { get; } = new();
    public HashSet<Guid> KnownMemberIds { get; } = new();
    public double PublicKnowledgeOfOrigin { get; set; } = 1;
}

public sealed class LineageEvolutionSystem
{
    private readonly Dictionary<Guid, LineageBranch> branches = new();
    public IReadOnlyDictionary<Guid, LineageBranch> Branches => branches;

    public LineageBranch Found(string familyName, long day, IEnumerable<Guid> founders)
    {
        var b = new LineageBranch { CurrentFamilyName = familyName, OriginFamilyName = familyName, FoundedDay = day };
        foreach (var id in founders) { b.FounderNpcIds.Add(id); b.KnownMemberIds.Add(id); }
        branches[b.Id] = b; return b;
    }

    public LineageBranch Split(LineageBranch parent, string newName, long day, string reason, IEnumerable<Guid> founders)
    {
        var b = new LineageBranch { ParentBranchId = parent.Id, CurrentFamilyName = newName, OriginFamilyName = parent.OriginFamilyName, FoundedDay = day, SeparationReason = reason, PublicKnowledgeOfOrigin = parent.PublicKnowledgeOfOrigin * .88 };
        foreach (var id in founders) { b.FounderNpcIds.Add(id); b.KnownMemberIds.Add(id); }
        branches[b.Id] = b; return b;
    }

    public void FadeOriginKnowledge(LineageBranch branch, long days, bool archivesPreserved) => branch.PublicKnowledgeOfOrigin = Math.Clamp(branch.PublicKnowledgeOfOrigin * Math.Exp(-days / (archivesPreserved ? 30000.0 : 9000.0)), 0, 1);
}

public enum LocationChangeKind { Expansion, Decline, Fortification, FireDamage, FloodDamage, NewRoad, RoadLost, NewDistrict, Abandonment, Reconstruction, Renaming }
public sealed record LocationChangeRecord(Guid Id, Guid LocationId, long Day, LocationChangeKind Kind, string Description, double Magnitude);

public sealed class MapEvolutionSystem
{
    private readonly List<LocationChangeRecord> changes = new();
    public IReadOnlyList<LocationChangeRecord> Changes => changes;

    public WorldLocation Apply(WorldState world, Guid locationId, long day, LocationChangeKind kind, string description, double magnitude)
    {
        if (!world.Locations.TryGetValue(locationId, out var old)) throw new ArgumentException("Unknown location.", nameof(locationId));
        magnitude = Math.Clamp(magnitude, 0, 1);
        var capacity = old.PopulationCapacity;
        var safety = old.Safety;
        var tags = old.Tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        switch (kind)
        {
            case LocationChangeKind.Expansion: capacity = (int)Math.Round(capacity * (1 + magnitude * .45)); tags.Add("expanded"); break;
            case LocationChangeKind.Decline: capacity = Math.Max(1, (int)Math.Round(capacity * (1 - magnitude * .35))); tags.Add("declining"); break;
            case LocationChangeKind.Fortification: safety = Math.Clamp(safety + magnitude * .24, 0, 1); tags.Add("fortified"); break;
            case LocationChangeKind.FireDamage: safety = Math.Clamp(safety - magnitude * .18, 0, 1); tags.Add("fire-damaged"); break;
            case LocationChangeKind.FloodDamage: safety = Math.Clamp(safety - magnitude * .16, 0, 1); tags.Add("flood-damaged"); break;
            case LocationChangeKind.NewRoad: tags.Add("trade-road"); break;
            case LocationChangeKind.RoadLost: tags.Remove("trade-road"); tags.Add("isolated"); break;
            case LocationChangeKind.Abandonment: capacity = Math.Max(1, capacity / 20); safety = Math.Clamp(safety - .25, 0, 1); tags.Add("abandoned"); break;
            case LocationChangeKind.Reconstruction: tags.Remove("fire-damaged"); tags.Remove("flood-damaged"); safety = Math.Clamp(safety + magnitude * .15, 0, 1); break;
            case LocationChangeKind.NewDistrict: capacity = (int)Math.Round(capacity * (1 + magnitude * .20)); tags.Add("new-district"); break;
            case LocationChangeKind.Renaming: tags.Add("renamed"); break;
        }
        var updated = old with { PopulationCapacity = capacity, Safety = safety, Tags = tags.ToArray() };
        world.Locations[locationId] = updated;
        changes.Add(new LocationChangeRecord(Guid.NewGuid(), locationId, day, kind, description, magnitude));
        return updated;
    }
}

public enum LawStatus { Active, Suspended, Repealed, Superseded, ForgottenButCitable }
public sealed class HistoricalLaw
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public long EnactedDay { get; init; }
    public Guid? IssuerFactionId { get; init; }
    public LawStatus Status { get; set; } = LawStatus.Active;
    public Guid? SupersededByLawId { get; set; }
    public double InstitutionalMemory { get; set; } = 1;
    public List<Guid> CitedPrecedentLawIds { get; } = new();
}

public sealed class LegalMemorySystem
{
    private readonly Dictionary<Guid, HistoricalLaw> laws = new();
    public IReadOnlyDictionary<Guid, HistoricalLaw> Laws => laws;
    public HistoricalLaw Enact(string title, string text, long day, Guid? issuer = null, IEnumerable<Guid>? precedent = null)
    {
        var law = new HistoricalLaw { Title = title, Text = text, EnactedDay = day, IssuerFactionId = issuer };
        foreach (var id in precedent ?? []) law.CitedPrecedentLawIds.Add(id);
        laws[law.Id] = law; return law;
    }
    public void Repeal(Guid id) { if (laws.TryGetValue(id, out var law)) law.Status = LawStatus.Repealed; }
    public IEnumerable<HistoricalLaw> SearchPrecedent(string keyword) => laws.Values.Where(l => l.InstitutionalMemory > .05 && (l.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) || l.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase))).OrderByDescending(l => l.InstitutionalMemory);
    public void Age(long days) { foreach (var law in laws.Values) law.InstitutionalMemory = Math.Clamp(law.InstitutionalMemory * Math.Exp(-days / 50000.0), 0, 1); }
}

public sealed class RegionalCultureState
{
    public string Region { get; init; } = string.Empty;
    public Dictionary<string, double> Practices { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, double> Dialects { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, double> Foods { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, double> DressStyles { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class CultureDiffusionSystem
{
    public void Migrate(RegionalCultureState source, RegionalCultureState destination, double migrantShare)
    {
        migrantShare = Math.Clamp(migrantShare, 0, .5);
        Diffuse(source.Practices, destination.Practices, migrantShare);
        Diffuse(source.Dialects, destination.Dialects, migrantShare * .75);
        Diffuse(source.Foods, destination.Foods, migrantShare * 1.15);
        Diffuse(source.DressStyles, destination.DressStyles, migrantShare * .85);
    }
    private static void Diffuse(Dictionary<string, double> source, Dictionary<string, double> dest, double share)
    {
        foreach (var (key, value) in source) dest[key] = Math.Clamp(dest.GetValueOrDefault(key) * (1 - share) + value * share, 0, 1);
    }
}

public sealed record HistoricalTruth(Guid Id, long Day, string WhatActuallyHappened, double SurvivingEvidence);
public sealed record HistoricalClaim(Guid Id, Guid TruthId, string Version, string Source, long RecordedDay, double Accuracy, double Authority, bool Official);

public sealed class HistoricalTruthSystem
{
    private readonly Dictionary<Guid, HistoricalTruth> truths = new();
    private readonly List<HistoricalClaim> claims = new();
    public IReadOnlyDictionary<Guid, HistoricalTruth> Truths => truths;
    public IReadOnlyList<HistoricalClaim> Claims => claims;

    public HistoricalTruth RecordTruth(long day, string reality, double evidence = .8)
    {
        var t = new HistoricalTruth(Guid.NewGuid(), day, reality, Math.Clamp(evidence, 0, 1)); truths[t.Id] = t; return t;
    }
    public HistoricalClaim RecordClaim(HistoricalTruth truth, string version, string source, long day, double accuracy, double authority, bool official)
    {
        var c = new HistoricalClaim(Guid.NewGuid(), truth.Id, version, source, day, Math.Clamp(accuracy, 0, 1), Math.Clamp(authority, 0, 1), official); claims.Add(c); return c;
    }
    public bool CanExposeContradiction(HistoricalTruth truth, double researcherSkill, double evidenceAccess)
        => truth.SurvivingEvidence * Math.Clamp(evidenceAccess, 0, 1) * (.35 + Math.Clamp(researcherSkill, 0, 100) / 130.0) >= .52;
}
