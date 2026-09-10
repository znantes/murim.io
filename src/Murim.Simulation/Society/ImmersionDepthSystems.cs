namespace Murim.Simulation;

public enum ObjectHolderKind { Ground, Npc, Container, Building, ShopStock, CarriedByAnimal, Lost, Destroyed }
public sealed class WorldObjectInstance
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string DefinitionCode { get; init; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ObjectHolderKind HolderKind { get; set; }
    public Guid? HolderId { get; set; }
    public Guid? LocationId { get; set; }
    public double Condition { get; set; } = 1;
    public long LastMovedDay { get; set; }
    public string ProvenanceNote { get; set; } = string.Empty;
}

public sealed class WorldObjectRegistry
{
    private readonly Dictionary<Guid, WorldObjectInstance> objects = new();
    public IReadOnlyDictionary<Guid, WorldObjectInstance> Objects => objects;

    public WorldObjectInstance Create(string code, string name, Guid locationId, long day)
    {
        var item = new WorldObjectInstance { DefinitionCode = code, DisplayName = name, HolderKind = ObjectHolderKind.Ground, LocationId = locationId, LastMovedDay = day };
        objects[item.Id] = item; return item;
    }

    public void Move(Guid objectId, ObjectHolderKind holderKind, Guid? holderId, Guid? locationId, long day)
    {
        if (!objects.TryGetValue(objectId, out var item)) throw new ArgumentException("Unknown object.", nameof(objectId));
        if (item.HolderKind == ObjectHolderKind.Destroyed) throw new InvalidOperationException("Destroyed objects cannot be moved.");
        item.HolderKind = holderKind; item.HolderId = holderId; item.LocationId = locationId; item.LastMovedDay = day;
    }

    public void Destroy(Guid objectId, long day)
    {
        if (!objects.TryGetValue(objectId, out var item)) return;
        item.HolderKind = ObjectHolderKind.Destroyed; item.HolderId = null; item.LastMovedDay = day; item.Condition = 0;
    }

    public IReadOnlyList<WorldObjectInstance> AtLocation(Guid locationId) => objects.Values.Where(x => x.LocationId == locationId && x.HolderKind != ObjectHolderKind.Destroyed).ToArray();
}

public sealed record RememberedUtterance(Guid Id, long Day, Guid SpeakerNpcId, Guid ListenerNpcId, string Text, double Confidence, double EmotionalWeight, bool PersonallyHeard, Guid? ConversationId);
public sealed class ConversationMemorySystem
{
    private readonly Dictionary<Guid, List<RememberedUtterance>> byListener = new();
    public IReadOnlyList<RememberedUtterance> MemoriesOf(Guid npcId) => byListener.GetValueOrDefault(npcId) ?? [];

    public Guid Record(WorldState world, Npc speaker, IEnumerable<Npc> listeners, string text, double clarity = .9, double emotionalWeight = .2)
    {
        var conversationId = Guid.NewGuid();
        foreach (var listener in listeners.Where(l => l.Id != speaker.Id && l.CurrentLocationId == speaker.CurrentLocationId))
        {
            var memorySkill = listener.Physiology.Spirit.Memory / 100.0;
            var confidence = Math.Clamp(clarity * (.55 + memorySkill * .45), 0, 1);
            var remembered = new RememberedUtterance(Guid.NewGuid(), world.Clock.Day, speaker.Id, listener.Id, text, confidence, Math.Clamp(emotionalWeight, 0, 1), true, conversationId);
            Get(listener.Id).Add(remembered);
        }
        return conversationId;
    }

    public string Recall(Guid listenerId, Guid memoryId, long currentDay)
    {
        var m = Get(listenerId).FirstOrDefault(x => x.Id == memoryId) ?? throw new ArgumentException("Memory not found.", nameof(memoryId));
        var ageYears = Math.Max(0, currentDay - m.Day) / 365.0;
        var retained = m.Confidence * Math.Exp(-ageYears / (3.5 + m.EmotionalWeight * 14));
        return retained >= .72 ? m.Text : retained >= .38 ? $"Souvenir approximatif : {m.Text}" : "Le souvenir est devenu trop vague pour être cité avec certitude.";
    }

    private List<RememberedUtterance> Get(Guid id)
    {
        if (byListener.TryGetValue(id, out var list)) return list;
        list = new List<RememberedUtterance>(); byListener[id] = list; return list;
    }
}

public enum SocialFormality { Familial, Informal, Respectful, Formal, Ceremonial }
public sealed record SocialCodeProfile(string Code, SocialFormality ExpectedFormality, double SeniorityWeight, double RankWeight, double FamilyWeight, double GenderConventionWeight, double OutsiderSuspicion, double FaceSavingImportance);
public sealed record SocialConductResult(double OffenseRisk, double RespectSignal, string Interpretation);

public sealed class SocialCodeSystem
{
    public SocialCodeProfile ForContext(WorldLocation location, FactionDefinition? faction = null)
    {
        var ceremonial = location.Tags.Any(t => t.Contains("palace", StringComparison.OrdinalIgnoreCase) || t.Contains("temple", StringComparison.OrdinalIgnoreCase));
        var village = location.Type.Contains("Village", StringComparison.OrdinalIgnoreCase);
        var criminal = faction?.Orientation is FactionOrientation.Criminal or FactionOrientation.Demonic;
        return ceremonial
            ? new("ceremonial", SocialFormality.Ceremonial, .82, .88, .45, .22, .38, .86)
            : criminal
                ? new("underworld", SocialFormality.Respectful, .40, .68, .22, .08, .72, .64)
                : village
                    ? new("village", SocialFormality.Informal, .58, .22, .72, .18, .26, .44)
                    : new("urban", SocialFormality.Respectful, .52, .48, .38, .12, .32, .58);
    }

    public SocialConductResult Evaluate(SocialCodeProfile code, SocialFormality used, double statusGap, bool publicSetting)
    {
        var gap = Math.Abs((int)used - (int)code.ExpectedFormality);
        var underFormal = (int)used < (int)code.ExpectedFormality;
        var offense = Math.Clamp(gap * .16 + Math.Max(0, statusGap) * code.RankWeight * .28 + (publicSetting ? code.FaceSavingImportance * .16 : 0) + (underFormal ? .14 : 0), 0, 1);
        var respect = Math.Clamp(1 - gap * .20 - offense * .25, 0, 1);
        var text = offense > .65 ? "Le comportement risque d'être jugé insultant." : offense > .32 ? "Le comportement paraît maladroit ou trop familier." : "Le comportement correspond globalement aux attentes locales.";
        return new SocialConductResult(offense, respect, text);
    }
}

public enum Season { Spring, Summer, Autumn, Winter }
public sealed record SeasonalState(Season Season, double Daylight, double TravelDifficulty, double CropActivity, double HerbAvailability, double ColdStress, double HeatStress, double MudRisk, string Description);

public sealed class SeasonSystem
{
    public SeasonalState ForDay(WorldClock clock, double regionalColdness = .5, double monsoon = .35)
    {
        var d = clock.DayOfYear;
        var season = d < 91 ? Season.Spring : d < 183 ? Season.Summer : d < 274 ? Season.Autumn : Season.Winter;
        return season switch
        {
            Season.Spring => new(season, .57, .30 + monsoon * .30, .88, .84, .08 + regionalColdness * .12, .05, .28 + monsoon * .48, "Dégel, semis, crues et routes parfois boueuses."),
            Season.Summer => new(season, .70, .16 + monsoon * .18, .92, .78, 0, .20 + (1 - regionalColdness) * .35, .10 + monsoon * .24, "Journées longues, croissance des cultures, chaleur et orages."),
            Season.Autumn => new(season, .52, .14, .74, .58, .06, .04, .08, "Récoltes, foires, réserves et premiers froids."),
            _ => new(season, .37, .36 + regionalColdness * .42, .10, .12, .30 + regionalColdness * .55, 0, .08, "Froid, cols difficiles, journées courtes et consommation des réserves.")
        };
    }
}

public sealed record MundaneEventTemplate(string Code, string Summary, double TypicalImportance, string[] LocationTags);
public static class MundaneEventCatalog
{
    public static IReadOnlyList<MundaneEventTemplate> All { get; } =
    [
        new("roof_leak", "Une toiture commence à fuir après la pluie.", .06, ["village", "residence"]),
        new("lost_sandal", "Un enfant perd une sandale dans la rue.", .02, ["village", "city"]),
        new("broken_cart", "Une roue de charrette se brise et bloque momentanément le passage.", .07, ["road", "market"]),
        new("family_argument", "Deux membres d'un foyer se disputent à voix basse.", .08, ["residence", "village"]),
        new("merchant_arrival", "Un petit marchand ambulant installe ses paniers pour la journée.", .09, ["village", "market"]),
        new("sick_child", "Un enfant du voisinage reste au lit avec une maladie banale.", .08, ["residence", "village"]),
        new("laundry_day", "Plusieurs foyers profitent du beau temps pour laver et sécher le linge.", .02, ["village", "city"]),
        new("minor_repair", "Un artisan répare une porte, une enseigne ou une clôture.", .03, ["city", "village"]),
        new("kitchen_smoke", "Une cuisine enfume momentanément une cour.", .02, ["residence", "inn"]),
        new("street_storyteller", "Un conteur attire un petit cercle de curieux.", .05, ["market", "city"])
    ];
}

public sealed class LocationVisualAgeState
{
    public Guid LocationId { get; init; }
    public double Weathering { get; set; }
    public double VegetationMaturity { get; set; }
    public double Maintenance { get; set; } = .5;
    public double ProsperityDisplay { get; set; } = .5;
    public int BuildingGeneration { get; set; }
    public HashSet<string> VisibleHistoryMarks { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class LocationVisualAgingSystem
{
    private readonly Dictionary<Guid, LocationVisualAgeState> states = new();
    public LocationVisualAgeState Get(Guid locationId)
    {
        if (states.TryGetValue(locationId, out var s)) return s;
        s = new LocationVisualAgeState { LocationId = locationId }; states[locationId] = s; return s;
    }

    public void Advance(Guid locationId, long days, double climateHarshness, double maintenance, double prosperity)
    {
        var s = Get(locationId);
        s.Maintenance = Math.Clamp(maintenance, 0, 1); s.ProsperityDisplay = Math.Clamp(prosperity, 0, 1);
        s.Weathering = Math.Clamp(s.Weathering + days / 3650.0 * (.18 + climateHarshness * .32) - maintenance * days / 3650.0 * .22, 0, 1);
        s.VegetationMaturity = Math.Clamp(s.VegetationMaturity + days / 7300.0 * (.45 + climateHarshness * .10), 0, 1);
    }

    public void MarkHistoricalChange(Guid locationId, string visualMark)
    {
        var s = Get(locationId); s.VisibleHistoryMarks.Add(visualMark); s.BuildingGeneration++;
    }
}
