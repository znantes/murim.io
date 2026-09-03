namespace Murim.World;

public sealed class BirthGenerator
{
    private readonly Random _random;

    public BirthGenerator(int seed)
    {
        _random = new Random(seed);
    }

    public Npc CreateNewborn(string familyName = "Unknown")
    {
        var npc = new Npc();
        npc.Identity.GivenName = $"Child-{_random.Next(1000, 9999)}";
        npc.Identity.FamilyName = familyName;
        npc.Identity.Sex = _random.Next(2) == 0 ? "Female" : "Male";

        npc.Body.HeightCm = 45 + _random.NextDouble() * 8;
        npc.Body.WeightKg = 2.5 + _random.NextDouble() * 2.0;
        npc.Body.Strength = Natural(0.25, 0.65);
        npc.Body.Speed = Natural(0.25, 0.65);
        npc.Body.Endurance = Natural(0.25, 0.65);
        npc.Body.Flexibility = Natural(0.35, 0.75);
        npc.Body.Coordination = Natural(0.25, 0.65);
        npc.Body.Recovery = Natural(0.25, 0.75);
        npc.Body.PainTolerance = Natural(0.20, 0.70);
        npc.Body.Health = Natural(0.45, 0.95);

        npc.Mind.Intelligence = Natural(0.25, 0.85);
        npc.Mind.Memory = Natural(0.25, 0.85);
        npc.Mind.Concentration = Natural(0.20, 0.75);
        npc.Mind.Willpower = Natural(0.20, 0.80);
        npc.Mind.Perception = Natural(0.25, 0.85);
        npc.Mind.LearningAbility = Natural(0.20, 0.90);
        npc.Mind.MentalResilience = Natural(0.20, 0.80);

        npc.Personality.Ambition = Natural(0.10, 0.90);
        npc.Personality.Courage = Natural(0.10, 0.90);
        npc.Personality.Prudence = Natural(0.10, 0.90);
        npc.Personality.Impulsivity = Natural(0.05, 0.95);
        npc.Personality.Patience = Natural(0.10, 0.90);
        npc.Personality.Discipline = Natural(0.10, 0.90);
        npc.Personality.Sociability = Natural(0.10, 0.90);
        npc.Personality.Empathy = Natural(0.10, 0.90);
        npc.Personality.Pride = Natural(0.05, 0.95);
        npc.Personality.Curiosity = Natural(0.10, 0.95);

        return npc;
    }

    private double Natural(double min, double max) => min + _random.NextDouble() * (max - min);
}
