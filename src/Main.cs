using Godot;
using Murim.Simulation;
using Murim.World;

namespace Murim;

public partial class Main : Node
{
    private WorldState _world = null!; private PlayerCommandService _commands = null!; private ContextualActionSystem _context = null!;
    private Label _status = null!; private Label _contextLabel = null!; private TextEdit _input = null!; private VBoxContainer _actionList = null!;

    public override void _Ready()
    {
        _world = new WorldState(); _world.CreatePlayerAtBirth(seed: 20260903, familyName: "Murim"); _commands = new PlayerCommandService(); _context = new ContextualActionSystem();
        var root = new VBoxContainer(); root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); root.AddThemeConstantOverride("separation", 8); AddChild(root);
        root.AddChild(new Label { Text = "MURIM.IO — Vie ordinaire, monde extraordinaire", HorizontalAlignment = HorizontalAlignment.Center });
        _status = new Label(); root.AddChild(_status); _contextLabel = new Label(); root.AddChild(_contextLabel); _actionList = new VBoxContainer(); root.AddChild(_actionList);
        _input = new TextEdit { PlaceholderText = "Écris ce que tu veux faire…", CustomMinimumSize = new Vector2(0, 80) }; root.AddChild(_input);
        var button = new Button { Text = "Exécuter la commande" }; button.Pressed += ExecuteCommand; root.AddChild(button); Refresh();
    }

    private void ExecuteCommand() { var result = _commands.Execute(_world, _input.Text); _status.Text = result.Feedback + $"\nTemps consommé : {result.MinutesSpent} min."; _input.Text = string.Empty; Refresh(); }

    private void ExecuteContextual(string command)
    {
        _input.Text = command; ExecuteCommand();
    }

    private void Refresh()
    {
        var p = _world.PlayerNpc; if (p is null) return;
        var location = p.CurrentLocationId is Guid id && _world.Geography.Locations.TryGetValue(id, out var l) ? l.Name : "inconnu";
        var building = p.CurrentBuildingId is Guid bid && _world.Buildings.TryGet(bid, out var b) ? b.Name : "extérieur";
        _status.Text = $"Jour {_world.Time.Day} · {_world.Time.Period} · {location} · {building} · {p.Identity.DisplayName} · âge {p.AgeYears}";
        _contextLabel.Text = "Actions contextuelles :";
        foreach (var child in _actionList.GetChildren()) child.QueueFree();
        foreach (var action in _context.GetAvailable(_world, p).Take(14))
        {
            var button = new Button { Text = action.Label, TooltipText = action.Command, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            var command = action.Command; button.Pressed += () => ExecuteContextual(command); _actionList.AddChild(button);
        }
    }
}
