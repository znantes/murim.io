namespace Murim.World;

public sealed class Npc
{
    public Guid Id { get; } = Guid.NewGuid();
    public Identity Identity { get; } = new();
    public Body Body { get; } = new();
    public Mind Mind { get; } = new();
    public Personality Personality { get; } = new();

    public int AgeYears { get; private set; }
    public bool IsAlive { get; private set; } = true;

    public void AdvanceAge(int years)
    {
        if (years < 0) throw new ArgumentOutOfRangeException(nameof(years));
        AgeYears += years;
    }

    public void Die() => IsAlive = false;
}
