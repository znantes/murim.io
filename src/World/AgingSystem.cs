namespace Murim.World;

public sealed class AgingSystem
{
    public void AdvanceDays(Npc npc, int days)
    {
        if (days <= 0 || !npc.IsAlive) return;
        var oldAge = npc.AgeYears;
        npc.AdvanceDays(days);
        var newAge = npc.AgeYears;
        if (newAge == oldAge) return;

        for (var age = oldAge + 1; age <= newAge; age++) ApplyMilestone(npc, age);
    }

    private static void ApplyMilestone(Npc npc, int age)
    {
        if (age <= 3)
        {
            npc.Body.HeightCm = Math.Min(100, npc.Body.HeightCm + 7 + npc.Inheritance.PhysicalPotential * 0.03);
            npc.Body.WeightKg = Math.Max(npc.Body.WeightKg, 3 + age * 2.5);
            npc.Body.Strength += 0.6 + npc.Inheritance.PhysicalPotential * 0.01;
            npc.Body.Endurance += 0.5;
            npc.Body.Coordination += 0.7;
            return;
        }

        if (age <= 12)
        {
            npc.Body.HeightCm += 4.5 + npc.Inheritance.PhysicalPotential * 0.015;
            npc.Body.WeightKg += 1.5 + npc.Inheritance.PhysicalPotential * 0.005;
            npc.Body.Strength += 1.2 + npc.Inheritance.PhysicalPotential * 0.01;
            npc.Body.Speed += 0.8;
            npc.Body.Endurance += 0.9;
            return;
        }

        if (age <= 20)
        {
            npc.Body.HeightCm += 2.5 + npc.Inheritance.PhysicalPotential * 0.01;
            npc.Body.WeightKg += 1.0;
            npc.Body.Strength += 2.0 + npc.Inheritance.PhysicalPotential * 0.015;
            npc.Body.Speed += 1.1;
            npc.Body.Endurance += 1.0;
            return;
        }

        if (age <= 40)
        {
            npc.Body.Recovery = Math.Min(100, npc.Body.Recovery + 0.1);
            return;
        }

        var decline = age switch
        {
            <= 55 => 0.15,
            <= 70 => 0.35,
            _ => 0.65
        };
        npc.Body.Strength = Math.Max(0, npc.Body.Strength - decline);
        npc.Body.Speed = Math.Max(0, npc.Body.Speed - decline * 0.7);
        npc.Body.Endurance = Math.Max(0, npc.Body.Endurance - decline * 0.8);
        npc.Body.Recovery = Math.Max(0, npc.Body.Recovery - decline * 0.5);
        npc.Body.Flexibility = Math.Max(0, npc.Body.Flexibility - decline * 0.4);
    }
}
