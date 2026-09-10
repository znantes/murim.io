namespace Murim.Simulation;

public enum Sex { Female, Male, Intersex }
public enum LifeStage { Infant, Child, Adolescent, Adult, Elder }
public enum Rarity { Ordinary, Uncommon, Rare, Superior, Legendary, Mythic, Divine }
public enum ReputationAlignment { Respected, Neutral, Shunned, Criminal, Feared }
public enum FactionOrientation { Orthodox, Neutral, Unorthodox, Demonic, Imperial, Commercial, Criminal, OuterRegion }
public enum FactionType { Sect, GreatFamily, Clan, Palace, Guild, Alliance, BanditStronghold, ImperialOffice, Cult, EscortAgency, IntelligenceNetwork, MerchantCompany }
public enum MasteryStage { Entry, Established, Peak }

public sealed class WorldClock
{
    public long TotalMinutes { get; private set; }
    public long Day => TotalMinutes / 1440 + 1;
    public int MinuteOfDay => (int)(TotalMinutes % 1440);
    public int Year => (int)((Day - 1) / 365) + 1;
    public int DayOfYear => (int)((Day - 1) % 365) + 1;
    public void AdvanceMinutes(int minutes) => TotalMinutes += Math.Max(0, minutes);
}

public sealed record WorldLocation(Guid Id, string Name, string Region, string Type, int PopulationCapacity, double Safety, IReadOnlyList<string> Tags);

public sealed class PersonalityProfile
{
    public double Curiosity { get; init; }
    public double Ambition { get; init; }
    public double Empathy { get; init; }
    public double Loyalty { get; init; }
    public double RiskTolerance { get; init; }
    public double Talkativeness { get; init; }
    public double Gullibility { get; init; }
    public double Anxiety { get; init; }
    public double Patience { get; init; }
}

public sealed class BodyState
{
    public double Health { get; set; } = 100;
    public double Stamina { get; set; } = 100;
    public double Qi { get; set; }
    public double Hunger { get; set; }
    public double Thirst { get; set; }
    public double SleepDebt { get; set; }
    public double Fatigue { get; set; }
    public bool IsAlive => Health > 0;
}

public sealed record NpcIdentity(
    Guid Id,
    string GivenName,
    string FamilyName,
    Sex Sex,
    long BirthDay,
    Guid BirthPlaceId,
    string Culture,
    string SocialOrigin,
    bool IsMonsterBorn = false,
    string? SpeciesCode = null)
{
    public string DisplayName => string.IsNullOrWhiteSpace(FamilyName) ? GivenName : $"{FamilyName} {GivenName}";
}

public sealed class Npc
{
    public NpcIdentity Identity { get; init; } = null!;
    public Guid Id => Identity.Id;
    public Guid CurrentLocationId { get; set; }
    public Guid? HouseholdId { get; set; }
    public Guid? PrimaryFactionId { get; set; }
    public BodyState Body { get; } = new();
    public PersonalityProfile Personality { get; init; } = new();
    public Dictionary<string, double> Skills { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<Guid, RelationshipState> Relationships { get; } = new();
    public Dictionary<Guid, RumorBelief> RumorBeliefs { get; } = new();
    public HashSet<string> Knowledge { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<CareerRecord> CareerHistory { get; } = new();
    public List<Guid> AffiliationIds { get; } = new();
    public List<Guid> ParentIds { get; } = new();
    public List<Guid> ChildIds { get; } = new();
    public Guid? SpouseId { get; set; }
    public bool IsAlive => Body.IsAlive;

    public int AgeYears(WorldClock clock) => (int)Math.Max(0, (clock.Day - Identity.BirthDay) / 365);
    public LifeStage LifeStage(WorldClock clock)
    {
        var age = AgeYears(clock);
        return age < 3 ? LifeStage.Infant : age < 10 ? LifeStage.Child : age < 16 ? LifeStage.Adolescent : age < 60 ? LifeStage.Adult : LifeStage.Elder;
    }
}

public sealed class Household
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FamilyName { get; init; } = string.Empty;
    public Guid HomeLocationId { get; init; }
    public double Wealth { get; set; }
    public double Prestige { get; set; }
    public Guid? FactionId { get; set; }
    public HashSet<Guid> MemberIds { get; } = new();
}

public sealed class WorldState
{
    public int Seed { get; init; }
    public WorldClock Clock { get; } = new();
    public Dictionary<Guid, Npc> Npcs { get; } = new();
    public Dictionary<Guid, Household> Households { get; } = new();
    public Dictionary<Guid, WorldLocation> Locations { get; } = new();
    public Dictionary<Guid, FactionDefinition> Factions { get; } = new();
    public List<WorldEvent> Events { get; } = new();
    public Dictionary<Guid, Rumor> Rumors { get; } = new();
    public Guid? PlayerNpcId { get; set; }
    public Npc? PlayerNpc => PlayerNpcId is Guid id && Npcs.TryGetValue(id, out var npc) ? npc : null;
}

public sealed class BirthSystem
{
    private readonly Random random;
    public double MonsterBirthProbability { get; init; } = 0.0015;

    private static readonly string[] GivenFemale = ["Mira", "Seol", "Ara", "Hae", "Rin", "Yeon", "Sora", "Nari", "Yuna", "Minseo"];
    private static readonly string[] GivenMale = ["Jiwon", "Hyeon", "Min", "Ryu", "Dojin", "Gyeom", "Tae", "Jun", "Seok", "Won"];
    private static readonly string[] CommonFamilies = ["Han", "Seo", "Baek", "Yun", "Kang", "Jin", "Choi", "Moon", "Im", "Jang", "Park", "Heo"];

    public BirthSystem(int seed) => random = new Random(seed);

    public Npc CreateBirth(WorldState world, IReadOnlyList<MonsterDefinition> monsters)
    {
        if (world.Locations.Count == 0) throw new InvalidOperationException("A location is required before births can occur.");
        var place = world.Locations.Values.ElementAt(random.Next(world.Locations.Count));
        var roll = random.NextDouble();
        var sex = roll < 0.4975 ? Sex.Female : roll < 0.995 ? Sex.Male : Sex.Intersex;
        var monsterBorn = random.NextDouble() < MonsterBirthProbability && monsters.Count > 0;
        var lowStageMonsters = monsterBorn ? monsters.Where(m => m.EvolutionStageIndex <= 1).ToArray() : Array.Empty<MonsterDefinition>();
        var species = monsterBorn && lowStageMonsters.Length > 0 ? lowStageMonsters[random.Next(lowStageMonsters.Length)] : null;

        Household? household = null;
        if (!monsterBorn && world.Households.Count > 0 && random.NextDouble() < 0.86)
            household = WeightedHousehold(world.Households.Values.ToArray());

        var family = monsterBorn ? string.Empty : household?.FamilyName ?? CommonFamilies[random.Next(CommonFamilies.Length)];
        var given = sex == Sex.Female ? GivenFemale[random.Next(GivenFemale.Length)] : GivenMale[random.Next(GivenMale.Length)];
        var origin = household is null ? (monsterBorn ? "créature née dans le monde sauvage" : "foyer ordinaire") :
            household.FactionId is not null && household.Prestige > 75 ? "lignée prestigieuse liée à une faction" :
            household.Wealth > 70 ? "famille aisée" : household.Wealth < 20 ? "famille pauvre" : "famille moyenne";

        var identity = new NpcIdentity(Guid.NewGuid(), given, family, sex, world.Clock.Day, household?.HomeLocationId ?? place.Id,
            "Murim central", origin, monsterBorn, species?.Code);
        var npc = new Npc
        {
            Identity = identity,
            CurrentLocationId = identity.BirthPlaceId,
            HouseholdId = household?.Id,
            PrimaryFactionId = household?.FactionId,
            Personality = RandomPersonality()
        };
        npc.Body.Qi = monsterBorn ? 2 + random.NextDouble() * 8 : random.NextDouble() * 2;
        npc.Skills["language"] = 0;
        npc.Skills["mobility"] = 0;
        world.Npcs[npc.Id] = npc;
        household?.MemberIds.Add(npc.Id);
        return npc;
    }

    private Household WeightedHousehold(Household[] households)
    {
        var total = households.Sum(h => Math.Max(1, 120 - h.Prestige));
        var roll = random.NextDouble() * total;
        foreach (var h in households)
        {
            roll -= Math.Max(1, 120 - h.Prestige);
            if (roll <= 0) return h;
        }
        return households[^1];
    }

    private PersonalityProfile RandomPersonality() => new()
    {
        Curiosity = random.NextDouble(), Ambition = random.NextDouble(), Empathy = random.NextDouble(), Loyalty = random.NextDouble(),
        RiskTolerance = random.NextDouble(), Talkativeness = random.NextDouble(), Gullibility = random.NextDouble(), Anxiety = random.NextDouble(), Patience = random.NextDouble()
    };
}
