using Godot;
using Murim.Simulation;
using Murim.World;

namespace Murim;

public partial class Main : Node
{
    private WorldState _world = null!;
    private PlayerCommandService _commands = null!;
    private ContextualActionSystem _context = null!;
    private Label _status = null!;
    private Label _contextLabel = null!;
    private TextEdit _input = null!;

    public override void _Ready()
    {
        _world = new WorldState();
        _world.CreatePlayerAtBirth(seed: 20260903, familyName: "Murim");
        _commands = new PlayerCommandService();
        _context = new ContextualActionSystem();

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 10);
        AddChild(root);

        var title = new Label { Text = "MURIM.IO — Vie ordinaire, monde extraordinaire", HorizontalAlignment = HorizontalAlignment.Center };
        root.AddChild(title);
        _status = new Label(); root.AddChild(_status);
        _contextLabel = new Label(); root.AddChild(_contextLabel);
        _input = new TextEdit { PlaceholderText = "Écris ce que tu veux faire…", CustomMinimumSize = new Vector2(0, 90) };
        root.AddChild(_input);
        var button = new Button { Text = "Exécuter la commande" };
        button.Pressed += ExecuteCommand;
        root.AddChild(button);
        Refresh();
    }

    private void ExecuteCommand()
    {
        var result = _commands.Execute(_world, _input.Text);
        _status.Text = result.Feedback + $"\nTemps consommé : {result.MinutesSpent} min.";
        _input.Text = string.Empty;
        Refresh();
    }

    private void Refresh()
    {
        var p = _world.PlayerNpc;
        if (p is null) return;
        var location = p.CurrentLocationId is Guid id && _world.Geography.Locations.TryGetValue(id, out var l) ? l.Name : "inconnu";
        _status.Text = $"Jour {_world.Time.Day} · {_world.Time.Period} · {location} · {p.Identity.DisplayName} · âge {p.AgeYears}";
        var actions = _context.GetAvailable(_world, p).Take(10).Select(a => "• " + a.Label);
        _contextLabel.Text = "Actions contextuelles :\n" + string.Join("\n", actions);
    }
}
