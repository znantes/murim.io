using Murim.Simulation;

namespace Murim.World;

public sealed class WorldState
{
    public GameTime Time { get; } = new();
    public Dictionary<Guid, Npc> Npcs { get; } = new();

    public void AddNpc(Npc npc)
    {
        ArgumentNullException.ThrowIfNull(npc);
        Npcs[npc.Id] = npc;
    }
}
