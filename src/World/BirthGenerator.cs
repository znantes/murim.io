namespace Murim.World;

public sealed class BirthGenerator
{
    public Npc CreateNewborn(BirthContext context, int seed, Npc? father = null, Npc? mother = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (father is null && mother is null)
            return CreateFromPopulation(context, seed);

        var random = new Random(seed);
        var npc = new Npc { Birth = context };

        npc.Identity.GivenName = $"Newborn-{random.Next(1000, 9999)}";
        npc.Identity.FamilyName = ResolveFamilyName(context, father, mother);
        npc.Identity.Sex = random.Next(2) == 0 ? "Female" : "Male";

        var physical = Inherit(father?.Inheritance.PhysicalPotential, mother?.Inheritance.PhysicalPotential, random);
        var mental = Inherit(father?.Inheritance.MentalPotential, mother?.Inheritance.MentalPotential, random);
        var recovery = Inherit(father?.Inheritance.RecoveryPotential, mother?.Inheritance.RecoveryPotential, random);
        var learning = Inherit(father?.Inheritance.LearningPotential, mother?.Inheritance.LearningPotential, random);
        var internalEnergy = Inherit(father?.Inheritance.InternalEnergyPotential, mother?.Inheritance.InternalEnergyPotential, random);

        npc.Inheritance.PhysicalPotential = physical;
        npc.Inheritance.MentalPotential = mental;
        npc.Inheritance.RecoveryPotential = recovery;
        npc.Inheritance.LearningPotential = learning;
        npc.Inheritance.InternalEnergyPotential = internalEnergy;

        npc.Body.HeightCm = 45 + random.NextDouble() * 8;
        npc.Body.WeightKg = 2.5 + random.NextDouble() * 2.0;
        npc.Body.Strength = ChildTrait(physical, random);
        npc.Body.Speed = ChildTrait(physical, random);
        npc.Body.Endurance = ChildTrait(physical, random);
        npc.Body.Flexibility = ChildTrait(physical, random);
        npc.Body.Coordination = ChildTrait(physical, random);
        npc.Body.Recovery = ChildTrait(recovery, random);
        npc.Body.PainTolerance = ChildTrait(physical, random);
        npc.Body.Health = 0.65 + random.NextDouble() * 0.30;

        npc.Mind.Intelligence = ChildTrait(mental, random);
        npc.Mind.Memory = ChildTrait(mental, random);
        npc.Mind.Concentration = ChildTrait(mental, random);
        npc.Mind.Willpower = ChildTrait(mental, random);
        npc.Mind.Perception = ChildTrait(mental, random);
        npc.Mind.LearningAbility = ChildTrait(learning, random);
        npc.Mind.MentalResilience = ChildTrait(mental, random);

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

        return npc;
    }

    private Npc CreateFromPopulation(BirthContext context, int seed)
    {
        var random = new Random(seed);
        var npc = new Npc { Birth = context };
        npc.Identity.GivenName = $"Newborn-{random.Next(1000, 9999)}";
        npc.Identity.FamilyName = context.SocialOrigin;
        npc.Identity.Sex = random.Next(2) == 0 ? "Female" : "Male";

        var physical = Natural(random, 0.25, 0.75);
        var mental = Natural(random, 0.25, 0.80);
        var recovery = Natural(random, 0.25, 0.80);
        var learning = Natural(random, 0.20, 0.90);
        var internalEnergy = Natural(random, 0.15, 0.75);

        npc.Inheritance.PhysicalPotential = physical;
        npc.Inheritance.MentalPotential = mental;
        npc.Inheritance.RecoveryPotential = recovery;
        npc.Inheritance.LearningPotential = learning;
        npc.Inheritance.InternalEnergyPotential = internalEnergy;

        npc.Body.HeightCm = 45 + random.NextDouble() * 8;
        npc.Body.WeightKg = 2.5 + random.NextDouble() * 2.0;
        npc.Body.Strength = ChildTrait(physical, random);
        npc.Body.Speed = ChildTrait(physical, random);
        npc.Body.Endurance = ChildTrait(physical, random);
        npc.Body.Flexibility = ChildTrait(physical, random);
        npc.Body.Coordination = ChildTrait(physical, random);
        npc.Body.Recovery = ChildTrait(recovery, random);
        npc.Body.PainTolerance = ChildTrait(physical, random);
        npc.Body.Health = 0.65 + random.NextDouble() * 0.30;

        npc.Mind.Intelligence = ChildTrait(mental, random);
        npc.Mind.Memory = ChildTrait(mental, random);
        npc.Mind.Concentration = ChildTrait(mental, random);
        npc.Mind.Willpower = ChildTrait(mental, random);
        npc.Mind.Perception = ChildTrait(mental, random);
        npc.Mind.LearningAbility = ChildTrait(learning, random);
        npc.Mind.MentalResilience = ChildTrait(mental, random);

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
        return npc;
    }

    private static double Inherit(double? father, double? mother, Random random)
    {
        if (father.HasValue && mother.HasValue)
            return Clamp((father.Value + mother.Value) / 2.0 + GaussianLike(random) * 0.08);
        return Clamp((father ?? mother ?? 0.5) + GaussianLike(random) * 0.10);
    }

    private static double ChildTrait(double potential, Random random) => Clamp(potential * 0.55 + random.NextDouble() * 0.25);
    private static double Natural(Random random, double min, double max) => min + random.NextDouble() * (max - min);
    private static double GaussianLike(Random random) => (random.NextDouble() + random.NextDouble() + random.NextDouble() - 1.5) / 1.5;
    private static double Clamp(double value) => Math.Clamp(value, 0.01, 0.99);

    private static string ResolveFamilyName(BirthContext context, Npc? father, Npc? mother)
    {
        if (!string.IsNullOrWhiteSpace(context.SocialOrigin) && context.SocialOrigin != "Unknown")
            return context.SocialOrigin;
        return father?.Identity.FamilyName ?? mother?.Identity.FamilyName ?? "Unknown";
    }
}
