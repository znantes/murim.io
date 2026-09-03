namespace Murim.World;

public sealed class Npc
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "Unknown";
    public int AgeYears { get; set; }
    public bool IsAlive { get; private set; } = true;

    public void Die()
    {
        if (!IsAlive)
            return;

        IsAlive = false;
    }
}
