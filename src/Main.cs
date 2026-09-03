using Godot;
using Murim.World;

namespace Murim;

public partial class Main : Node
{
    private WorldState _world = null!;

    public override void _Ready()
    {
        _world = new WorldState();
        GD.Print($"Murim.io initialized — Day {_world.Time.Day}, {_world.Time.Period}");
    }
}
