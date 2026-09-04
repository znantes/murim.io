using Godot;
using Murim.Game;
using Murim.World;

namespace Murim;

public partial class Main : Node
{
    private WorldState _world = null!;

    public override void _Ready()
    {
        _world = new WorldState();
        _world.CreatePlayerAtBirth(seed: 20260903, familyName: "Murim");

        var hud = new GameHud();
        AddChild(hud);
        hud.Initialize(_world);
    }
}
