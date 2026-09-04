using Murim.Simulation;

namespace Murim.World;

public sealed class WorldState
{
    public GameTime Time { get; } = new();
    public Dictionary<Guid, Npc> Npcs { get; } = new();
    public Dictionary<Guid, Family> Families { get; } = new();
    public FamilyLifeSystem FamilyLife { get; } = new();
    public int WorldSeed { get; private set; }
    public Npc? PlayerNpc { get; private set; }

    public void AddNpc(Npc npc)
    {
        ArgumentNullException.ThrowIfNull(npc);
        Npcs[npc.Id] = npc;
    }

    public void AddFamily(Family family)
    {
        ArgumentNullException.ThrowIfNull(family);
        Families[family.Id] = family;
    }

    public void AdvanceMinutes(int minutes)
    {
        if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
        var oldDay = Time.Day;
        Time.AdvanceMinutes(minutes);
        var elapsedDays = Time.Day - oldDay;
        for (var i = 0L; i < elapsedDays; i++)
            FamilyLife.AdvanceDay(this);
    }

    public Npc CreatePlayerAtBirth(int seed, string familyName = "Unknown", FamilyOrigin? forcedOrigin = null)
    {
        WorldSeed = seed;
        var random = new Random(seed);
        var origin = forcedOrigin ?? RollOrigin(random);
        var actualFamilyName = origin == FamilyOrigin.Imperial
            ? $"Maison impériale {familyName}"
            : familyName;

        var family = new Family
        {
            Name = actualFamilyName,
            Origin = origin,
            SocialStatus = SocialStatusFor(origin)
        };
        AddFamily(family);

        const string culture = "Unknown";
        const string region = "Unknown";

        var parentLife = new ParentLifeGenerator();
        var parents = parentLife.CreateParentPair(actualFamilyName, culture, region, origin, seed + 1, this);
        family.FatherId = parents.Father.Id;
        family.MotherId = parents.Mother.Id;

        var context = new BirthContext
        {
            FamilyId = family.Id,
            FatherId = parents.Father.Id,
            MotherId = parents.Mother.Id,
            Culture = culture,
            SocialOrigin = actualFamilyName,
            Region = region
        };

        var generator = new BirthGenerator();
        var npc = generator.CreateNewborn(context, seed + 2, parents.Father, parents.Mother);
        AddNpc(npc);
        family.ChildrenIds.Add(npc.Id);
        FamilyLife.LinkParentChild(parents.Father, npc);
        FamilyLife.LinkParentChild(parents.Mother, npc);
        FamilyLife.LinkSpouses(parents.Father, parents.Mother);
        parents.Father.History.Add("Naissance de l'enfant", parents.Father.AgeYears, $"Naissance de {npc.Identity.DisplayName}.");
        parents.Mother.History.Add("Naissance de l'enfant", parents.Mother.AgeYears, $"Naissance de {npc.Identity.DisplayName}.");
        npc.History.Add("Naissance", 0, $"Naissance au sein de la famille {family.Name}.");

        PlayerNpc = npc;
        return npc;
    }

    public Npc CreateChild(int seed, Family family, Npc father, Npc mother, string culture, string region)
    {
        ArgumentNullException.ThrowIfNull(family);
        ArgumentNullException.ThrowIfNull(father);
        ArgumentNullException.ThrowIfNull(mother);

        var context = new BirthContext
        {
            FamilyId = family.Id,
            FatherId = father.Id,
            MotherId = mother.Id,
            Culture = culture,
            SocialOrigin = family.Name,
            Region = region
        };

        var generator = new BirthGenerator();
        var child = generator.CreateNewborn(context, seed, father, mother);
        AddNpc(child);
        family.ChildrenIds.Add(child.Id);
        father.History.Add("Naissance de l'enfant", father.AgeYears, $"Naissance de {child.Identity.DisplayName}.");
        mother.History.Add("Naissance de l'enfant", mother.AgeYears, $"Naissance de {child.Identity.DisplayName}.");
        return child;
    }

    private static FamilyOrigin RollOrigin(Random random)
    {
        var roll = random.Next(100);
        return roll switch
        {
            < 70 => FamilyOrigin.Common,
            < 80 => FamilyOrigin.Rural,
            < 88 => FamilyOrigin.Merchant,
            < 94 => FamilyOrigin.Martial,
            < 98 => FamilyOrigin.Noble,
            _ => FamilyOrigin.Imperial
        };
    }

    private static string SocialStatusFor(FamilyOrigin origin) => origin switch
    {
        FamilyOrigin.Imperial => "Impérial",
        FamilyOrigin.Noble => "Noble",
        FamilyOrigin.Martial => "Martial",
        FamilyOrigin.Merchant => "Marchand",
        FamilyOrigin.Religious => "Religieux",
        FamilyOrigin.Criminal => "Criminel",
        FamilyOrigin.Secretive => "Secret",
        FamilyOrigin.Rural => "Rural",
        _ => "Commun"
    };
}
