namespace Murim.World;

public sealed class ParentLifeGenerator
{
    public (Family FatherOriginFamily, Family MotherOriginFamily, Npc Father, Npc Mother) CreateParentPair(
        string playerFamilyName,
        string culture,
        string region,
        FamilyOrigin playerOrigin,
        int seed,
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var fatherOrigin = CreateOriginFamily($"{playerFamilyName} — lignée paternelle", playerOrigin, seed + 11, world);
        var motherOrigin = CreateOriginFamily($"{playerFamilyName} — lignée maternelle", playerOrigin, seed + 22, world);

        var father = CreateParent(playerFamilyName, "Male", culture, region, fatherOrigin, seed + 31);
        var mother = CreateParent(playerFamilyName, "Female", culture, region, motherOrigin, seed + 41);
        world.AddNpc(father);
        world.AddNpc(mother);

        father.History.Add("Rencontre", father.AgeYears - 3, $"Rencontre avec {mother.Identity.DisplayName}.");
        mother.History.Add("Rencontre", mother.AgeYears - 3, $"Rencontre avec {father.Identity.DisplayName}.");
        father.History.Add("Union", father.AgeYears - 2, $"Début d'une union avec {mother.Identity.DisplayName}.");
        mother.History.Add("Union", mother.AgeYears - 2, $"Début d'une union avec {father.Identity.DisplayName}.");
        father.History.Add("Foyer", father.AgeYears - 1, "Installation d'un foyer commun.");
        mother.History.Add("Foyer", mother.AgeYears - 1, "Installation d'un foyer commun.");

        return (fatherOrigin, motherOrigin, father, mother);
    }

    private static Family CreateOriginFamily(string familyName, FamilyOrigin origin, int seed, WorldState world)
    {
        var random = new Random(seed);
        var family = new Family
        {
            Name = familyName,
            Origin = origin,
            SocialStatus = SocialStatusFor(origin)
        };
        world.AddFamily(family);

        // Generation -2: grandparents of the player's parents.
        var grandfather = CreateRelative(familyName, random, "Grand-parent", "Male", origin, 65 + random.Next(20));
        var grandmother = CreateRelative(familyName, random, "Grand-parent", "Female", origin, 60 + random.Next(20));
        world.AddNpc(grandfather);
        world.AddNpc(grandmother);
        family.FatherId = grandfather.Id;
        family.MotherId = grandmother.Id;

        // Generation -1: parent of the future parent, born into this branch.
        var ancestorChild = CreateRelative(familyName, random, "Ancêtre", random.Next(2) == 0 ? "Male" : "Female", origin, 35 + random.Next(15));
        ancestorChild.Birth = new BirthContext
        {
            FatherId = grandfather.Id,
            MotherId = grandmother.Id,
            FamilyId = family.Id,
            Culture = "Unknown",
            Region = "Unknown",
            SocialOrigin = familyName
        };
        ancestorChild.History.Add("Naissance", 0, $"Naissance de {ancestorChild.Identity.DisplayName} dans cette branche.");
        ancestorChild.History.Add("Enfance", 6, "Grandit auprès de ses parents et de sa communauté.");
        ancestorChild.History.Add("Transmission", 16, "Reçoit des savoirs familiaux transmis sur plusieurs générations.");
        world.AddNpc(ancestorChild);
        family.ChildrenIds.Add(ancestorChild.Id);

        return family;
    }

    private static Npc CreateRelative(string familyName, Random random, string role, string sex, FamilyOrigin origin, int age)
    {
        var npc = new Npc { Birth = new BirthContext { SocialOrigin = familyName } };
        npc.Identity.GivenName = $"{role}-{random.Next(1000, 9999)}";
        npc.Identity.FamilyName = familyName;
        npc.Identity.Sex = sex;
        npc.Body.Health = 0.55 + random.NextDouble() * 0.35;
        npc.Inheritance.PhysicalPotential = 0.30 + random.NextDouble() * 0.55;
        npc.Inheritance.MentalPotential = 0.30 + random.NextDouble() * 0.55;
        npc.Inheritance.RecoveryPotential = 0.30 + random.NextDouble() * 0.55;
        npc.Inheritance.LearningPotential = 0.30 + random.NextDouble() * 0.55;
        npc.Inheritance.InternalEnergyPotential = 0.20 + random.NextDouble() * 0.60;
        npc.AdvanceAge(age);
        npc.History.Add("Naissance", 0, $"Naissance dans une famille {SocialStatusFor(origin).ToLowerInvariant()}.");
        npc.History.Add("Enfance", 6, "Grandit dans son milieu familial et culturel.");
        npc.History.Add("Vie adulte", 20, "Construit sa vie, ses relations et ses ressources.");
        return npc;
    }

    private static Npc CreateParent(string familyName, string sex, string culture, string region, Family originFamily, int seed)
    {
        var random = new Random(seed);
        var npc = new Npc
        {
            Birth = new BirthContext
            {
                FamilyId = originFamily.Id,
                Culture = culture,
                Region = region,
                SocialOrigin = originFamily.Name
            }
        };

        npc.Identity.GivenName = sex == "Male" ? $"Père-{random.Next(1000, 9999)}" : $"Mère-{random.Next(1000, 9999)}";
        npc.Identity.FamilyName = familyName;
        npc.Identity.Sex = sex;

        npc.Body.HeightCm = sex == "Male" ? 165 + random.NextDouble() * 25 : 155 + random.NextDouble() * 20;
        npc.Body.WeightKg = 48 + random.NextDouble() * 35;
        npc.Body.Strength = Trait(random); npc.Body.Speed = Trait(random); npc.Body.Endurance = Trait(random);
        npc.Body.Flexibility = Trait(random); npc.Body.Coordination = Trait(random); npc.Body.Recovery = Trait(random);
        npc.Body.PainTolerance = Trait(random); npc.Body.Health = 0.70 + random.NextDouble() * 0.30;
        npc.Mind.Intelligence = Trait(random); npc.Mind.Memory = Trait(random); npc.Mind.Concentration = Trait(random);
        npc.Mind.Willpower = Trait(random); npc.Mind.Perception = Trait(random); npc.Mind.LearningAbility = Trait(random);
        npc.Mind.MentalResilience = Trait(random);

        npc.Inheritance.PhysicalPotential = Average(npc.Body.Strength, npc.Body.Speed, npc.Body.Endurance);
        npc.Inheritance.MentalPotential = Average(npc.Mind.Intelligence, npc.Mind.Memory, npc.Mind.Perception);
        npc.Inheritance.RecoveryPotential = npc.Body.Recovery;
        npc.Inheritance.LearningPotential = npc.Mind.LearningAbility;
        npc.Inheritance.InternalEnergyPotential = Trait(random);

        npc.Personality.Ambition = random.NextDouble(); npc.Personality.Courage = random.NextDouble();
        npc.Personality.Prudence = random.NextDouble(); npc.Personality.Impulsivity = random.NextDouble();
        npc.Personality.Patience = random.NextDouble(); npc.Personality.Discipline = random.NextDouble();
        npc.Personality.Sociability = random.NextDouble(); npc.Personality.Empathy = random.NextDouble();
        npc.Personality.Pride = random.NextDouble(); npc.Personality.Curiosity = random.NextDouble();

        var age = 25 + random.Next(20);
        npc.AdvanceAge(age);
        npc.History.Add("Naissance", 0, $"Naissance dans la famille {originFamily.Name}.");
        npc.History.Add("Enfance", 5, "Grandit au sein de sa famille.");
        npc.History.Add("Éducation", 8, "Reçoit une éducation adaptée à son milieu.");
        npc.History.Add("Formation", 13, "Développe ses premières compétences.");
        npc.History.Add("Profession", 18, "Entre progressivement dans la vie professionnelle.");
        npc.History.Add("Vie adulte", Math.Min(22, age), "Devient indépendant et construit son propre réseau.");
        return npc;
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

    private static double Trait(Random random) => 0.25 + random.NextDouble() * 0.70;
    private static double Average(params double[] values) => values.Sum() / values.Length;
}
