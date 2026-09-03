using Godot;
using Murim.World;

namespace Murim;

public partial class Main : Node
{
    private WorldState _world = null!;

    public override void _Ready()
    {
        _world = new WorldState();
        var player = _world.CreatePlayerAtBirth(seed: 20260903, familyName: "Murim");

        GD.Print($"Murim.io initialized — Day {_world.Time.Day}, {_world.Time.Period}");
        GD.Print($"Born: {player.Identity.DisplayName} ({player.Identity.Sex})");
        GD.Print($"Body: {player.Body.HeightCm:F1} cm / {player.Body.WeightKg:F2} kg");
        GD.Print($"Intelligence potential: {player.Mind.Intelligence:F2}");
        GD.Print($"Learning potential: {player.Mind.LearningAbility:F2}");
    }
}
