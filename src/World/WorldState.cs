using Murim.Simulation;

namespace Murim.World;

public sealed class WorldState
{
    public GameTime Time { get; } = new();
    public Dictionary<Guid, Npc> Npcs { get; } = new();

    public Npc? PlayerNpc { get; private set; }

    public void AddNpc(Npc npc)
    {
        ArgumentNullException.ThrowIfNull(npc);
        Npcs[npc.Id] = npc;
    }

    public Npc CreatePlayerAtBirth(int seed, string familyName = "Unknown")
    {
        var generator = new BirthGenerator(seed);
        var npc = generator.CreateNewborn(familyName);
        AddNpc(npc);
        PlayerNpc = npc;
        return npc;
    }
}
