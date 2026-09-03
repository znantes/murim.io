namespace Murim.World;

public sealed class ParentLifeGenerator
{
    public (Family FatherOriginFamily, Family MotherOriginFamily, Npc Father, Npc Mother) CreateParentPair(
        string playerFamilyName,
        string culture,
        string region,
        int seed,
        WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var fatherOrigin = CreateOriginFamily($"{playerFamilyName} — lignée paternelle", seed + 11, world);
        var motherOrigin = CreateOriginFamily($"{playerFamilyName} — lignée maternelle", seed + 22, world);

        var father = CreateParent(
            familyName: playerFamilyName,
            sex: "Male",
            culture,
            region,
            originFamily: fatherOrigin,
            seed: seed + 31);

        var mother = CreateParent(
            familyName: playerFamilyName,
            sex: "Female",
            culture,
            region,
            originFamily: motherOrigin,
            seed: seed + 41);

        world.AddNpc(father);
        world.AddNpc(mother);

        father.History.Add("Rencontre", father.AgeYears - 3, $"Rencontre avec {mother.Identity.DisplayName} lors d'un déplacement dans la région.");
        mother.History.Add("Rencontre", mother.AgeYears - 3, $"Rencontre avec {father.Identity.DisplayName} lors d'un déplacement dans la région.");
        father.History.Add("Union", father.AgeYears - 2, $"Début d'une union avec {mother.Identity.DisplayName}.");
        mother.History.Add("Union", mother.AgeYears - 2, $"Début d'une union avec {father.Identity.DisplayName}.");
        father.History.Add("Projet familial", father.AgeYears - 1, $"Installation d'un foyer commun avec {mother.Identity.DisplayName}.");
        mother.History.Add("Projet familial", mother.AgeYears - 1, $"Installation d'un foyer commun avec {father.Identity.DisplayName}.");

        return (fatherOrigin, motherOrigin, father, mother);
    }

    private static Family CreateOriginFamily(string familyName, int seed, WorldState world)
    {
        var random = new Random(seed);
        var family = new Family { Name = familyName };
        world.AddFamily(family);

        // Two older relatives anchor each parent in a real ancestral branch.
        var elder = CreateRelative(familyName, random, "Elder");
        var elderMate = CreateRelative(familyName, random, "ElderMate");
        world.AddNpc(elder);
        world.AddNpc(elderMate);
        family.FatherId = elder.Id;
        family.MotherId = elderMate.Id;
        return family;
    }

    private static Npc CreateRelative(string familyName, Random random, string role)
    {
        var npc = new Npc
        {
            Birth = new BirthContext { SocialOrigin = familyName }
        };
        npc.Identity.GivenName = $"{role}-{random.Next(1000, 9999)}";
        npc.Identity.FamilyName = familyName;
        npc.Identity.Sex = role == "Elder" ? "Male" : "Female";
        npc.Body.Health = 0.55 + random.NextDouble() * 0.35;
        npc.Inheritance.PhysicalPotential = 0.30 + random.NextDouble() * 0.55;
        npc.Inheritance.MentalPotential = 0.30 + random.NextDouble() * 0.55;
        npc.Inheritance.RecoveryPotential = 0.30 + random.NextDouble() * 0.55;
        npc.Inheritance.LearningPotential = 0.30 + random.NextDouble() * 0.55;
        npc.Inheritance.InternalEnergyPotential = 0.20 + random.NextDouble() * 0.60;
        npc.AdvanceAge(55 + random.Next(20));
        npc.History.Add("Naissance", 0, "Naissance dans cette lignée familiale.");
        npc.History.Add("Vie adulte", 20, "Construction progressive d'une vie familiale et sociale.");
        return npc;
    }

    private static Npc CreateParent(
        string familyName,
        string sex,
        string culture,
        string region,
        Family originFamily,
        int seed)
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

        npc.Identity.GivenName = sex == "Male"
            ? $"Père-{random.Next(1000, 9999)}"
            : $"Mère-{random.Next(1000, 9999)}";
        npc.Identity.FamilyName = familyName;
        npc.Identity.Sex = sex;

        npc.Body.HeightCm = sex == "Male" ? 165 + random.NextDouble() * 25 : 155 + random.NextDouble() * 20;
        npc.Body.WeightKg = 48 + random.NextDouble() * 35;
        npc.Body.Strength = Trait(random);
        npc.Body.Speed = Trait(random);
        npc.Body.Endurance = Trait(random);
        npc.Body.Flexibility = Trait(random);
        npc.Body.Coordination = Trait(random);
        npc.Body.Recovery = Trait(random);
        npc.Body.PainTolerance = Trait(random);
        npc.Body.Health = 0.70 + random.NextDouble() * 0.30;

        npc.Mind.Intelligence = Trait(random);
        npc.Mind.Memory = Trait(random);
        npc.Mind.Concentration = Trait(random);
        npc.Mind.Willpower = Trait(random);
        npc.Mind.Perception = Trait(random);
        npc.Mind.LearningAbility = Trait(random);
        npc.Mind.MentalResilience = Trait(random);

        npc.Inheritance.PhysicalPotential = Average(npc.Body.Strength, npc.Body.Speed, npc.Body.Endurance);
        npc.Inheritance.MentalPotential = Average(npc.Mind.Intelligence, npc.Mind.Memory, npc.Mind.Perception);
        npc.Inheritance.RecoveryPotential = npc.Body.Recovery;
        npc.Inheritance.LearningPotential = npc.Mind.LearningAbility;
        npc.Inheritance.InternalEnergyPotential = Trait(random);

        npc.Personality.Ambition = random.NextDouble();
        npc.Personality.Courage = random.NextDouble();
        npc.Personality.Prudence = random.NextDouble();
        npc.Personality.Impulsivity = random.NextDouble();
        npc.Personality.Patience = random.NextDouble();
        npc.Personality.Discipline = random.NextDouble();
        npc.Personality.Sociability = random.NextDouble();
        npc.Personality.Empathy = random.NextDouble();
        npc.Personality.Pride = random.NextDouble();
        npc.Personality.Curiosity = random.NextDouble();

        var age = 25 + random.Next(20);
        npc.AdvanceAge(age);
        npc.History.Add("Naissance", 0, $"Naissance dans la famille {originFamily.Name}.");
        npc.History.Add("Enfance", 5, "Grandit au sein de sa famille et découvre son environnement.");
        npc.History.Add("Éducation", 8, "Commence une éducation adaptée à sa culture et à son milieu social.");
        npc.History.Add("Formation", 13, "Développe ses premières compétences et centres d'intérêt.");
        npc.History.Add("Profession", 18, "Entre progressivement dans la vie professionnelle.");
        npc.History.Add("Vie adulte", Math.Min(22, age), "Devient indépendant et construit son propre réseau social.");

        return npc;
    }

    private static double Trait(Random random) => 0.25 + random.NextDouble() * 0.70;
    private static double Average(params double[] values) => values.Sum() / values.Length;
}
