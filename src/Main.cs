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
        var family = _world.Families[player.Birth.FamilyId!.Value];
        var father = _world.Npcs[family.FatherId!.Value];
        var mother = _world.Npcs[family.MotherId!.Value];

        GD.Print($"Murim.io initialized — Day {_world.Time.Day}, {_world.Time.Period}");
        GD.Print($"Family: {family.Name}");
        GD.Print($"Father: {father.Identity.DisplayName}, age {father.AgeYears}");
        GD.Print($"Mother: {mother.Identity.DisplayName}, age {mother.AgeYears}");
        GD.Print($"Born: {player.Identity.DisplayName} ({player.Identity.Sex})");
        GD.Print($"Inherited physical potential: {player.Inheritance.PhysicalPotential:F2}");
        GD.Print($"Inherited mental potential: {player.Inheritance.MentalPotential:F2}");
        GD.Print($"Inherited learning potential: {player.Inheritance.LearningPotential:F2}");
    }
}
