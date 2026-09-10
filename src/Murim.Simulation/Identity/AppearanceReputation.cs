namespace Murim.Simulation;

public enum BeautyRenownTier { None, Noticed, Local, Regional, Legendary }

public sealed record AppearanceImpression(Guid ObserverNpcId, Guid SubjectNpcId, long Day, double Appeal, double Memorability, string Region);

public sealed class BeautyRenownRecord
{
    public Guid SubjectNpcId { get; init; }
    public BeautyRenownTier Tier { get; set; }
    public int IndependentWitnesses { get; set; }
    public double AppealSum { get; set; }
    public double HighestAppeal { get; set; }
    public HashSet<string> Regions { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<Guid> ObserverIds { get; } = new();
    public string? Epithet { get; set; }
    public double AverageAppeal => IndependentWitnesses == 0 ? 0 : AppealSum / IndependentWitnesses;
}

/// <summary>
/// Beauty is treated as social perception, not an objective universal stat.
/// A face can be striking to one observer and ordinary to another. Only observations
/// that actually occur can create renown, so the UI never gains omniscient beauty labels.
/// </summary>
public sealed class AppearancePerceptionSystem
{
    public const int MaxLegendaryBeauties = 4;
    private readonly Dictionary<Guid, BeautyRenownRecord> renown = new();
    private readonly List<AppearanceImpression> impressions = new();

    public IReadOnlyDictionary<Guid, BeautyRenownRecord> Renown => renown;
    public IReadOnlyList<AppearanceImpression> Impressions => impressions;

    public AppearanceImpression Observe(WorldState world, PortraitGeneticsSystem portraits, Npc observer, Npc subject)
    {
        if (observer.CurrentLocationId != subject.CurrentLocationId)
            throw new InvalidOperationException("Observer and subject must be co-located for a direct appearance impression.");
        if (!world.Locations.TryGetValue(observer.CurrentLocationId, out var location))
            throw new InvalidOperationException("Observer location is missing.");

        portraits.UpdateVisibleAge(subject, world.Clock);
        var g = portraits.GetOrCreate(subject);
        var a = portraits.AppearanceFor(subject);
        var appeal = PerceivedAppeal(observer, subject, g, a);
        var memorability = Math.Clamp(PortraitGeneticsSystem.StructuralDistinctiveness(g) * .48 + Math.Abs(appeal - .5) * .42 + g.FacialAsymmetry * .10, 0, 1);
        var impression = new AppearanceImpression(observer.Id, subject.Id, world.Clock.Day, appeal, memorability, location.Region);
        impressions.Add(impression);

        // Renown is an adult social concept here. Children can still be recognized by face,
        // but are never ranked by this beauty-reputation system.
        if (subject.AgeYears(world.Clock) >= 18)
            RegisterAdultImpression(subject, impression, world);

        return impression;
    }

    public double PerceivedAppeal(Npc observer, Npc subject, PortraitGenome g, PortraitAppearanceState a)
    {
        // Personal preferences are deterministic and vary by observer.
        var preferredAdiposity = .27 + Unit(observer.Id, 11) * .48;
        var preferredDistinctiveness = .20 + Unit(observer.Id, 13) * .62;
        var symmetryImportance = .06 + Unit(observer.Id, 17) * .14;
        var skinImportance = .10 + Unit(observer.Id, 19) * .16;

        var averageness = 1 - Math.Clamp(new[] { g.FaceWidth, g.JawWidth, g.NoseWidth, g.NoseLength, g.EyeSpacing, g.ChinProjection }
            .Average(x => Math.Abs(x - .5) * 2), 0, 1);
        var distinctiveness = PortraitGeneticsSystem.StructuralDistinctiveness(g);
        var adiposityFit = 1 - Math.Abs(a.FacialWeight - preferredAdiposity);
        var symmetry = 1 - g.FacialAsymmetry;
        var visibleSkin = 1 - Math.Clamp(a.SkinRoughness * .45 + a.BlemishAmount * .35 + a.Bruising * .20, 0, 1);
        var distinctivenessFit = 1 - Math.Abs(distinctiveness - preferredDistinctiveness);

        // Familiarity and affection influence subjective attraction without changing the face itself.
        var relation = observer.Relationships.GetValueOrDefault(subject.Id);
        var familiarityBias = relation is null ? 0 : Math.Clamp((relation.Affection - 50) / 400.0, -.10, .10);

        var score = averageness * .24 + distinctivenessFit * .20 + adiposityFit * .18 + symmetry * symmetryImportance + visibleSkin * skinImportance;
        var normalization = .24 + .20 + .18 + symmetryImportance + skinImportance;
        return Math.Clamp(score / normalization + familiarityBias, 0, 1);
    }

    public BeautyRenownRecord? RecordFor(Guid npcId) => renown.GetValueOrDefault(npcId);

    private void RegisterAdultImpression(Npc subject, AppearanceImpression impression, WorldState world)
    {
        if (!renown.TryGetValue(subject.Id, out var record))
        {
            record = new BeautyRenownRecord { SubjectNpcId = subject.Id };
            renown[subject.Id] = record;
        }
        if (!record.ObserverIds.Add(impression.ObserverNpcId)) return;
        record.IndependentWitnesses++;
        record.AppealSum += impression.Appeal;
        record.HighestAppeal = Math.Max(record.HighestAppeal, impression.Appeal);
        record.Regions.Add(impression.Region);

        var average = record.AverageAppeal;
        record.Tier = average switch
        {
            >= .72 when record.IndependentWitnesses >= 3 => BeautyRenownTier.Noticed,
            _ => BeautyRenownTier.None
        };
        if (average >= .77 && record.IndependentWitnesses >= 8) record.Tier = BeautyRenownTier.Local;
        if (average >= .82 && record.IndependentWitnesses >= 18 && record.Regions.Count >= 2) record.Tier = BeautyRenownTier.Regional;

        // The classic "great beauty of the Murim" reputation is intentionally much rarer.
        // It is limited to adult women by the requested setting trope and requires broad independent recognition.
        if (subject.Identity.Sex == Sex.Female && average >= .87 && record.IndependentWitnesses >= 36 && record.Regions.Count >= 3)
        {
            var legendaryCount = renown.Values.Count(x => x.Tier == BeautyRenownTier.Legendary && x.SubjectNpcId != subject.Id);
            if (legendaryCount < MaxLegendaryBeauties)
            {
                record.Tier = BeautyRenownTier.Legendary;
                record.Epithet ??= GenerateEpithet(subject, world);
            }
        }
    }

    private static string GenerateEpithet(Npc subject, WorldState world)
    {
        var region = world.Locations.GetValueOrDefault(subject.CurrentLocationId)?.Region ?? "Murim";
        string[] nouns = ["Jade", "Prunier", "Lune", "Orchidée", "Neige", "Aube", "Lotus", "Brume"];
        var noun = nouns[(int)(Unit(subject.Id, 47) * nouns.Length) % nouns.Length];
        return $"Beauté du {noun} de {region}";
    }

    private static double Unit(Guid id, int salt)
    {
        var h = salt;
        foreach (var b in id.ToByteArray()) h = unchecked(h * 31 + b);
        return (uint)h / (double)uint.MaxValue;
    }
}
