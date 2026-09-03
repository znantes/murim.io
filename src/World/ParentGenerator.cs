namespace Murim.World;

public sealed class ParentGenerator
{
    public (Npc Father, Npc Mother) CreateParents(string familyName, string culture, string region, int seed)
    {
        var random = new Random(seed);
        var father = CreateParent(familyName, "Male", culture, region, random.Next());
        var mother = CreateParent(familyName, "Female", culture, region, random.Next());
        return (father, mother);
    }

    private static Npc CreateParent(string familyName, string sex, string culture, string region, int seed)
    {
        var random = new Random(seed);
        var npc = new Npc
        {
            Birth = new BirthContext
            {
                Culture = culture,
                Region = region,
                SocialOrigin = familyName
            }
        };

        npc.Identity.GivenName = sex == "Male" ? $"Father-{random.Next(1000, 9999)}" : $"Mother-{random.Next(1000, 9999)}";
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

        npc.AdvanceAge(25 + random.Next(20));
        return npc;
    }

    private static double Trait(Random random) => 0.25 + random.NextDouble() * 0.70;
    private static double Average(params double[] values) => values.Sum() / values.Length;
}
