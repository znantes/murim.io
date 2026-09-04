namespace Murim.World;

public enum NeedType
{
    Hunger,
    Thirst,
    Sleep,
    Fatigue,
    Comfort
}

public sealed class Needs
{
    public double Hunger { get; private set; }
    public double Thirst { get; private set; }
    public double Sleep { get; private set; }
    public double Fatigue { get; private set; }
    public double Comfort { get; private set; } = 50;

    public void AdvanceMinutes(int minutes, int ageYears)
    {
        var ageFactor = ageYears < 4 ? 1.35 : 1.0;
        Hunger = Math.Clamp(Hunger + minutes * 0.035 * ageFactor, 0, 100);
        Thirst = Math.Clamp(Thirst + minutes * 0.05 * ageFactor, 0, 100);
        Sleep = Math.Clamp(Sleep + minutes * 0.025 * ageFactor, 0, 100);
        Fatigue = Math.Clamp(Fatigue + minutes * 0.03 * ageFactor, 0, 100);
        Comfort = Math.Clamp(Comfort + (50 - Comfort) * Math.Min(1, minutes / 720.0), 0, 100);
    }

    public void Eat(double relief) => Hunger = Math.Clamp(Hunger - Math.Max(0, relief), 0, 100);
    public void Drink(double relief) => Thirst = Math.Clamp(Thirst - Math.Max(0, relief), 0, 100);
    public void SleepFor(double relief) { Sleep = Math.Clamp(Sleep - relief, 0, 100); Fatigue = Math.Clamp(Fatigue - relief * 1.2, 0, 100); }
}

public sealed class SurvivalSystem
{
    public void Advance(WorldState world, int minutes)
    {
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive))
        {
            npc.Needs.AdvanceMinutes(minutes, npc.AgeYears);
            ApplyConsequences(npc);
        }
    }

    private static void ApplyConsequences(Npc npc)
    {
        var severe = npc.Needs.Hunger >= 95 || npc.Needs.Thirst >= 95 || npc.Needs.Fatigue >= 95;
        if (severe)
            npc.Body.Health = Math.Max(0, npc.Body.Health - 0.05);

        if (npc.Body.Health <= 0)
            npc.Die();
    }
}
