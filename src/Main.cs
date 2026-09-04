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

        PrintHistory("Father history", father);
        PrintHistory("Mother history", mother);
        GD.Print($"Origin family branches: {_world.Families.Count - 1}");
        GD.Print($"Living NPCs generated: {_world.Npcs.Values.Count(n => n.IsAlive)}");

        // The world keeps living even while the player does nothing.
        _world.AdvanceMinutes(120);
        GD.Print($"After autonomous simulation — Day {_world.Time.Day}, {_world.Time.Period}");
        GD.Print($"Autonomous events: {_world.FamilyLife.LastEvents.Count}");
        foreach (var eventText in _world.FamilyLife.LastEvents)
            GD.Print($"[WORLD] {eventText}");
    }

    private static void PrintHistory(string label, Npc npc)
    {
        GD.Print($"--- {label} ---");
        foreach (var lifeEvent in npc.History.Events)
            GD.Print($"Age {lifeEvent.AgeYears}: {lifeEvent.Type} — {lifeEvent.Description}");
    }
}
