using System.Security.Cryptography;
using System.Text;

namespace Murim.Simulation;

public enum SettlementScale { Hamlet = 0, Village = 1, Town = 2, City = 3, GreatCity = 4, Capital = 5 }
public enum DistrictKind
{
    Residential, CentralMarket, Merchants, Smiths, Artisans, Pleasure, TeaAndInns, Food, Medicine, Apothecaries,
    Scholars, BooksAndPaper, Temples, Administration, Noble, Poor, Docks, Riverfront, Warehouses, Military,
    Caravan, HorseMarket, Textile, Guilds, Foreigners, Underworld, Gardens
}
public enum DistrictState { Thriving, Active, Strained, Declining, Abandoned, Ruined }
public enum IllustrationSubjectKind { WorldLocation, District, Venue, InstitutionFacility }
public enum IllustrationSeason { Spring, Summer, Autumn, Winter }
public enum IllustrationTime { Dawn, Day, Dusk, Night }
public enum IllustrationWeather { Clear, Rain, Snow, Fog, Storm }

public sealed class DistrictVenue
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public bool AdultOnly { get; init; }
    public bool CriminalRisk { get; init; }
    public bool PubliclyVisible { get; init; } = true;
}

public sealed class SettlementDistrict
{
    public Guid Id { get; init; }
    public Guid ParentLocationId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DistrictKind Kind { get; init; }
    public SettlementScale SettlementScale { get; init; }
    public DistrictState State { get; set; } = DistrictState.Active;
    public double PopulationShare { get; init; }
    public double Wealth { get; set; }
    public double Safety { get; set; }
    public double NightActivity { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public List<DistrictVenue> Venues { get; } = new();
}

/// <summary>
/// Creates internal urban geography for settlements. Districts are not a universal checklist: scale,
/// economy, river access, military presence and local specialties determine what exists. A pleasure
/// quarter is treated as an entertainment/economic district; some individual venues can be adult-only,
/// while streets, theatres, restaurants and tea houses remain ordinary physical places in the city.
/// </summary>
public sealed class SettlementDistrictSystem
{
    private readonly Dictionary<Guid, List<SettlementDistrict>> byLocation = new();
    private readonly int seed;
    public SettlementDistrictSystem(int seed) => this.seed = seed;
    public IReadOnlyDictionary<Guid, List<SettlementDistrict>> ByLocation => byLocation;
    public IEnumerable<SettlementDistrict> All => byLocation.Values.SelectMany(x => x);

    public void BuildAll(WorldState world)
    {
        byLocation.Clear();
        foreach (var location in world.Locations.Values.Where(IsSettlement))
            byLocation[location.Id] = BuildFor(location);
    }

    public IReadOnlyList<SettlementDistrict> ForLocation(Guid locationId)
        => byLocation.TryGetValue(locationId, out var districts) ? districts : [];

    public SettlementScale ScaleOf(WorldLocation location)
    {
        if (location.Tags.Contains("imperial") && location.PopulationCapacity >= 200_000) return SettlementScale.Capital;
        return location.PopulationCapacity switch
        {
            < 1_200 => SettlementScale.Hamlet,
            < 7_500 => SettlementScale.Village,
            < 40_000 => SettlementScale.Town,
            < 120_000 => SettlementScale.City,
            < 260_000 => SettlementScale.GreatCity,
            _ => SettlementScale.Capital
        };
    }

    public bool CanEnterVenue(Npc npc, DistrictVenue venue, WorldClock clock)
        => !venue.AdultOnly || npc.AgeYears(clock) >= 18;

    private List<SettlementDistrict> BuildFor(WorldLocation location)
    {
        var scale = ScaleOf(location);
        var tags = new HashSet<string>(location.Tags, StringComparer.OrdinalIgnoreCase);
        var specs = new List<(DistrictKind Kind, SettlementScale Min, bool Enabled)>
        {
            (DistrictKind.Residential, SettlementScale.Hamlet, true),
            (DistrictKind.CentralMarket, SettlementScale.Village, true),
            (DistrictKind.TeaAndInns, SettlementScale.Village, true),
            (DistrictKind.Temples, SettlementScale.Village, true),
            (DistrictKind.Artisans, SettlementScale.Village, true),
            (DistrictKind.Merchants, SettlementScale.Town, true),
            (DistrictKind.Smiths, SettlementScale.Town, tags.Contains("mining") || tags.Contains("mine") || tags.Contains("military") || tags.Contains("market") || scale >= SettlementScale.City),
            (DistrictKind.Food, SettlementScale.Town, true),
            (DistrictKind.Medicine, SettlementScale.Town, tags.Contains("medicine") || tags.Contains("herbs") || scale >= SettlementScale.City),
            (DistrictKind.Apothecaries, SettlementScale.Town, tags.Contains("medicine") || tags.Contains("herbs") || scale >= SettlementScale.City),
            (DistrictKind.Administration, SettlementScale.Town, true),
            (DistrictKind.Caravan, SettlementScale.Town, tags.Contains("caravan") || tags.Contains("trade") || tags.Contains("market")),
            (DistrictKind.HorseMarket, SettlementScale.Town, tags.Contains("horses") || tags.Contains("caravan") || tags.Contains("military")),
            (DistrictKind.Riverfront, SettlementScale.Town, tags.Contains("river")),
            (DistrictKind.Docks, SettlementScale.Town, tags.Contains("river") || tags.Contains("sea") || location.Type.Contains("Port", StringComparison.OrdinalIgnoreCase)),
            (DistrictKind.Pleasure, SettlementScale.Town, true),
            (DistrictKind.Poor, SettlementScale.City, true),
            (DistrictKind.Noble, SettlementScale.City, true),
            (DistrictKind.Warehouses, SettlementScale.City, tags.Contains("trade") || tags.Contains("market") || tags.Contains("river") || tags.Contains("sea")),
            (DistrictKind.Military, SettlementScale.City, tags.Contains("military") || tags.Contains("imperial") || scale >= SettlementScale.GreatCity),
            (DistrictKind.Guilds, SettlementScale.City, tags.Contains("guilds") || tags.Contains("craft") || tags.Contains("market") || scale >= SettlementScale.GreatCity),
            (DistrictKind.Textile, SettlementScale.City, tags.Contains("craft") || scale >= SettlementScale.GreatCity),
            (DistrictKind.Scholars, SettlementScale.City, tags.Contains("academy") || tags.Contains("school") || tags.Contains("imperial") || scale >= SettlementScale.GreatCity),
            (DistrictKind.BooksAndPaper, SettlementScale.GreatCity, tags.Contains("academy") || tags.Contains("school") || tags.Contains("imperial")),
            (DistrictKind.Foreigners, SettlementScale.GreatCity, tags.Contains("trade") || tags.Contains("port") || tags.Contains("sea") || tags.Contains("caravan")),
            (DistrictKind.Underworld, SettlementScale.City, tags.Contains("underworld") || tags.Contains("black_market") || tags.Contains("smuggling") || scale >= SettlementScale.GreatCity),
            (DistrictKind.Gardens, SettlementScale.GreatCity, true)
        };

        var districts = new List<SettlementDistrict>();
        foreach (var spec in specs.Where(s => scale >= s.Min && s.Enabled))
        {
            var district = CreateDistrict(location, scale, spec.Kind);
            AddVenues(district);
            districts.Add(district);
        }

        return districts.OrderBy(d => (int)d.Kind).ToList();
    }

    private SettlementDistrict CreateDistrict(WorldLocation location, SettlementScale scale, DistrictKind kind)
    {
        var hash = StableInt($"{seed}:{location.Id}:{kind}");
        var unit = (hash & 0x7fffffff) / (double)int.MaxValue;
        var safetyBias = kind switch
        {
            DistrictKind.Poor => -.20, DistrictKind.Underworld => -.36, DistrictKind.Pleasure => -.08,
            DistrictKind.Noble => .15, DistrictKind.Administration => .12, DistrictKind.Military => .10, _ => 0
        };
        var wealthBias = kind switch
        {
            DistrictKind.Noble => .32, DistrictKind.Merchants => .22, DistrictKind.Poor => -.38,
            DistrictKind.Underworld => -.12, DistrictKind.Smiths => .04, _ => 0
        };
        return new SettlementDistrict
        {
            Id = StableGuid($"district:{location.Id}:{kind}"), ParentLocationId = location.Id, Code = kind.ToString().ToLowerInvariant(),
            Name = DistrictName(kind), Kind = kind, SettlementScale = scale,
            PopulationShare = Math.Clamp(.03 + unit * .13, .02, .18), Wealth = Math.Clamp(.5 + wealthBias + (unit - .5) * .18, .04, .98),
            Safety = Math.Clamp(location.Safety + safetyBias + (unit - .5) * .08, .03, .99),
            NightActivity = NightActivity(kind), Tags = DistrictTags(kind)
        };
    }

    private static string DistrictName(DistrictKind kind) => kind switch
    {
        DistrictKind.Residential => "Quartiers résidentiels",
        DistrictKind.CentralMarket => "Quartier du Grand Marché",
        DistrictKind.Merchants => "Quartier des Marchands",
        DistrictKind.Smiths => "Quartier des Forgerons",
        DistrictKind.Artisans => "Quartier des Artisans",
        DistrictKind.Pleasure => "Quartier des Lanternes et des Plaisirs",
        DistrictKind.TeaAndInns => "Quartier des Auberges et Maisons de Thé",
        DistrictKind.Food => "Rue des Cuisines et Échoppes",
        DistrictKind.Medicine => "Quartier des Médecins",
        DistrictKind.Apothecaries => "Rue des Apothicaires",
        DistrictKind.Scholars => "Quartier des Lettrés",
        DistrictKind.BooksAndPaper => "Rue des Libraires et Papetiers",
        DistrictKind.Temples => "Quartier des Temples",
        DistrictKind.Administration => "Quartier Administratif",
        DistrictKind.Noble => "Quartier des Grandes Maisons",
        DistrictKind.Poor => "Quartier Populaire",
        DistrictKind.Docks => "Quartier des Docks",
        DistrictKind.Riverfront => "Quartier des Quais",
        DistrictKind.Warehouses => "Quartier des Entrepôts",
        DistrictKind.Military => "Quartier des Casernes",
        DistrictKind.Caravan => "Quartier des Caravanes",
        DistrictKind.HorseMarket => "Marché aux Chevaux",
        DistrictKind.Textile => "Quartier des Tisserands et Teinturiers",
        DistrictKind.Guilds => "Quartier des Guildes",
        DistrictKind.Foreigners => "Quartier des Voyageurs Étrangers",
        DistrictKind.Underworld => "Quartier des Ruelles Grises",
        DistrictKind.Gardens => "Quartier des Jardins",
        _ => kind.ToString()
    };

    private static double NightActivity(DistrictKind kind) => kind switch
    {
        DistrictKind.Pleasure => .98, DistrictKind.TeaAndInns => .82, DistrictKind.Food => .72,
        DistrictKind.Underworld => .77, DistrictKind.Docks => .58, DistrictKind.Military => .55,
        DistrictKind.CentralMarket => .36, DistrictKind.Smiths => .22, _ => .18
    };

    private static IReadOnlyList<string> DistrictTags(DistrictKind kind) => kind switch
    {
        DistrictKind.Pleasure => ["entertainment", "music", "theatre", "tea", "rumors", "nightlife"],
        DistrictKind.Smiths => ["forge", "weapons", "tools", "craft", "smoke"],
        DistrictKind.Medicine or DistrictKind.Apothecaries => ["medicine", "herbs", "clinics"],
        DistrictKind.Docks or DistrictKind.Riverfront => ["water", "boats", "trade"],
        DistrictKind.Underworld => ["crime", "secrecy", "rumors"],
        DistrictKind.Scholars or DistrictKind.BooksAndPaper => ["books", "school", "calligraphy"],
        _ => [kind.ToString().ToLowerInvariant()]
    };

    private static void AddVenues(SettlementDistrict d)
    {
        void V(string code, string name, string category, bool adult = false, bool criminal = false)
            => d.Venues.Add(new DistrictVenue { Id = StableGuid($"venue:{d.Id}:{code}"), Code = code, Name = name, Category = category, AdultOnly = adult, CriminalRisk = criminal });

        switch (d.Kind)
        {
            case DistrictKind.Smiths:
                V("weapon_forge", "Forge d'armes", "forge"); V("tool_forge", "Forge d'outils", "forge"); V("charcoal", "Dépôt de charbon", "supply"); V("metal_market", "Marché du métal", "market"); break;
            case DistrictKind.Pleasure:
                V("theatre", "Théâtre des Lanternes", "theatre"); V("music_house", "Maison de musique", "music"); V("late_tea", "Maison de thé nocturne", "tea");
                V("courtesan_house", "Maison de courtisanes", "adult_entertainment", adult: true); V("gaming_house", "Maison de jeux", "adult_entertainment", adult: true); V("night_restaurant", "Restaurant de nuit", "food"); break;
            case DistrictKind.CentralMarket:
                V("grain", "Marché aux grains", "market"); V("general", "Marché général", "market"); V("auction", "Cour des enchères", "market"); break;
            case DistrictKind.Merchants:
                V("merchant_hall", "Maison des marchands", "guild"); V("money_house", "Maison de change", "finance"); V("pawn", "Prêteur sur gages", "finance"); break;
            case DistrictKind.Artisans:
                V("wood", "Ateliers de charpentiers", "craft"); V("pottery", "Ateliers de potiers", "craft"); V("leather", "Ateliers du cuir", "craft"); break;
            case DistrictKind.Medicine:
                V("clinic", "Clinique", "medicine"); V("bone_setter", "Maison du rebouteux", "medicine"); V("travelling_doctor", "Cour des médecins itinérants", "medicine"); break;
            case DistrictKind.Apothecaries:
                V("herb_market", "Marché aux herbes", "herbs"); V("apothecary", "Grande apothicairerie", "herbs"); V("drying_yard", "Cour de séchage", "herbs"); break;
            case DistrictKind.TeaAndInns:
                V("inn", "Grande auberge", "inn"); V("tea", "Maison de thé", "tea"); V("storyteller", "Salle des conteurs", "entertainment"); break;
            case DistrictKind.Docks:
                V("passenger_pier", "Quai des voyageurs", "dock"); V("cargo_pier", "Quai des marchandises", "dock"); V("boatwright", "Chantier de bateaux", "craft"); break;
            case DistrictKind.Underworld:
                V("hidden_market", "Marché clandestin", "crime", criminal: true); V("fence", "Receleur", "crime", criminal: true); V("backroom", "Arrière-salle sans enseigne", "crime", adult: true, criminal: true); break;
            case DistrictKind.Administration:
                V("magistrate", "Yamen du magistrat", "government"); V("records", "Bureau des registres", "government"); V("tax", "Bureau des taxes", "government"); break;
            case DistrictKind.Scholars:
                V("academy", "Académie locale", "school"); V("exam_lodging", "Maison des candidats", "school"); V("calligraphy", "Atelier de calligraphie", "craft"); break;
            case DistrictKind.BooksAndPaper:
                V("bookshop", "Librairie", "books"); V("printer", "Imprimerie", "books"); V("paper", "Papeterie", "craft"); break;
            case DistrictKind.Military:
                V("barracks", "Caserne", "military"); V("drill", "Terrain d'exercice", "military"); V("armory", "Arsenal", "military"); break;
            case DistrictKind.Temples:
                V("temple", "Grand temple local", "religion"); V("shrine", "Sanctuaire de quartier", "religion"); V("charity", "Cuisine charitable", "charity"); break;
            case DistrictKind.Food:
                V("noodle", "Maison de nouilles", "food"); V("dumpling", "Échoppe de raviolis", "food"); V("roast", "Rôtisserie", "food"); break;
            case DistrictKind.Textile:
                V("weaver", "Atelier de tissage", "craft"); V("dyer", "Cour des teinturiers", "craft"); V("tailor", "Maison des tailleurs", "craft"); break;
            case DistrictKind.Caravan:
                V("caravanserai", "Caravansérail", "travel"); V("freight", "Cour de fret", "trade"); V("escort_office", "Bureau d'escorte", "escort"); break;
            default:
                V("street", "Rue principale du quartier", "street"); V("courtyard", "Cour commune", "residential"); break;
        }
    }

    private static bool IsSettlement(WorldLocation l)
    {
        var t = l.Type.ToLowerInvariant();
        if (t.Contains("ville") || t.Contains("cité") || t.Contains("village") || t.Contains("hameau") || t.Contains("port") || t.Contains("relais") || t.Contains("académie")) return true;
        return l.Tags.Any(x => x is "market" or "trade" or "imperial" or "guilds");
    }

    private static Guid StableGuid(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("murim-urban:" + value));
        return new Guid(bytes.AsSpan(0, 16));
    }

    private static int StableInt(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("murim-urban-int:" + value));
        return BitConverter.ToInt32(bytes, 0);
    }
}

public sealed record PlaceIllustrationProfile(
    Guid SubjectId,
    IllustrationSubjectKind SubjectKind,
    string Name,
    string Region,
    string AssetFamily,
    string AssetDirectory,
    int VisualSeed,
    IReadOnlyList<string> Tags,
    string SceneDescription);

/// <summary>
/// Gives every world location, district, venue and institution facility a deterministic visual identity.
/// Final artwork can be hand-made, pre-generated or rendered from reusable scene kits. The simulation never
/// downloads web imagery at runtime. Asset lookup prefers exact season/time/weather images and falls back to
/// progressively more generic artwork, so every place remains displayable while the art library grows.
/// </summary>
public sealed class WorldIllustrationCatalog
{
    private readonly Dictionary<Guid, PlaceIllustrationProfile> profiles = new();
    private readonly int seed;
    public WorldIllustrationCatalog(int seed) => this.seed = seed;
    public IReadOnlyDictionary<Guid, PlaceIllustrationProfile> Profiles => profiles;

    public void BuildAll(WorldState world, SettlementDistrictSystem districts, InstitutionDomainSystem domains)
    {
        profiles.Clear();
        foreach (var location in world.Locations.Values)
            Add(location.Id, IllustrationSubjectKind.WorldLocation, location.Name, location.Region, FamilyForLocation(location), location.Tags, DescribeLocation(location));

        foreach (var district in districts.All)
        {
            var parent = world.Locations[district.ParentLocationId];
            Add(district.Id, IllustrationSubjectKind.District, district.Name, parent.Region, $"district_{district.Kind.ToString().ToLowerInvariant()}", district.Tags, $"{district.Name} de {parent.Name}, activité et architecture cohérentes avec {parent.Region}.");
            foreach (var venue in district.Venues)
                Add(venue.Id, IllustrationSubjectKind.Venue, venue.Name, parent.Region, $"venue_{venue.Category}", district.Tags.Concat([venue.Category]).ToArray(), $"{venue.Name}, dans {district.Name} à {parent.Name}.");
        }

        foreach (var domain in domains.Domains.Values)
        {
            var parent = world.Locations[domain.ParentWorldLocationId];
            foreach (var facility in domain.Facilities)
                Add(facility.Id, IllustrationSubjectKind.InstitutionFacility, facility.Name, parent.Region, $"facility_{facility.Kind.ToString().ToLowerInvariant()}", [domain.Kind.ToString().ToLowerInvariant(), domain.CurrentScale.ToString().ToLowerInvariant(), facility.Kind.ToString().ToLowerInvariant()], $"{facility.Name} de {domain.Name}, état {facility.State}, échelle {domain.CurrentScale}.");
        }
    }

    public PlaceIllustrationProfile For(Guid subjectId)
        => profiles.TryGetValue(subjectId, out var profile) ? profile : throw new KeyNotFoundException($"No illustration profile for {subjectId}.");

    public IReadOnlyList<string> CandidateAssetPaths(Guid subjectId, IllustrationSeason season, IllustrationTime time, IllustrationWeather weather)
    {
        var p = For(subjectId); var root = p.AssetDirectory;
        var s = season.ToString().ToLowerInvariant(); var t = time.ToString().ToLowerInvariant(); var w = weather.ToString().ToLowerInvariant();
        return [
            $"{root}/{s}_{t}_{w}.webp",
            $"{root}/{s}_{t}.webp",
            $"{root}/{s}.webp",
            $"{root}/{t}.webp",
            $"{root}/default.webp"
        ];
    }

    private void Add(Guid id, IllustrationSubjectKind kind, string name, string region, string family, IReadOnlyList<string> tags, string description)
    {
        var slug = Slug(name);
        profiles[id] = new PlaceIllustrationProfile(id, kind, name, region, family,
            $"res://Assets/World/{kind}/{slug}_{id.ToString("N")[..8]}", StableInt($"{seed}:{kind}:{id}"), tags, description);
    }

    private static string FamilyForLocation(WorldLocation l)
    {
        var tags = new HashSet<string>(l.Tags, StringComparer.OrdinalIgnoreCase);
        if (tags.Contains("sect")) return "sect_mountain";
        if (tags.Contains("great_family") || tags.Contains("estate")) return "family_estate";
        if (tags.Contains("forest")) return "wild_forest";
        if (tags.Contains("desert")) return "desert";
        if (tags.Contains("sea")) return "coast_islands";
        if (tags.Contains("ruins")) return "ruins";
        if (tags.Contains("mine")) return "mine";
        if (tags.Contains("imperial")) return "imperial_city";
        if (tags.Contains("market")) return "settlement_market";
        return "regional_landscape";
    }

    private static string DescribeLocation(WorldLocation l)
        => $"{l.Type} de {l.Region}; identité visuelle stable, population potentielle {l.PopulationCapacity}, thèmes: {string.Join(", ", l.Tags)}.";

    private static string Slug(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_').ToArray();
        return string.Join('_', new string(chars).Split('_', StringSplitOptions.RemoveEmptyEntries));
    }

    private static int StableInt(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("murim-illustration:" + value));
        return BitConverter.ToInt32(bytes, 0);
    }
}
