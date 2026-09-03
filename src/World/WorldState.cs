using Murim.Simulation;

namespace Murim.World;

public sealed class WorldState
{
    public GameTime Time { get; } = new();
    public Dictionary<Guid, Npc> Npcs { get; } = new();
    public Dictionary<Guid, Family> Families { get; } = new();

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

    public Npc CreatePlayerAtBirth(int seed, string familyName = "Unknown")
    {
        var family = new Family { Name = familyName };
        AddFamily(family);

        const string culture = "Unknown";
        const string region = "Unknown";

        var parentLife = new ParentLifeGenerator();
        var parents = parentLife.CreateParentPair(
            playerFamilyName: familyName,
            culture,
            region,
            seed + 1,
            this);

        family.FatherId = parents.Father.Id;
        family.MotherId = parents.Mother.Id;

        var context = new BirthContext
        {
            FamilyId = family.Id,
            FatherId = parents.Father.Id,
            MotherId = parents.Mother.Id,
            Culture = culture,
            SocialOrigin = familyName,
            Region = region
        };

        var generator = new BirthGenerator();
        var npc = generator.CreateNewborn(context, seed + 2, parents.Father, parents.Mother);
        AddNpc(npc);
        family.ChildrenIds.Add(npc.Id);

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
        family.FatherId = father.Id;
        family.MotherId = mother.Id;
        family.ChildrenIds.Add(child.Id);
        father.History.Add("Naissance de l'enfant", father.AgeYears, $"Naissance de {child.Identity.DisplayName}.");
        mother.History.Add("Naissance de l'enfant", mother.AgeYears, $"Naissance de {child.Identity.DisplayName}.");
        return child;
    }
}
