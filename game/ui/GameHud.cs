using Godot;
using Murim.Simulation;
using Murim.World;

namespace Murim.Game;

public partial class GameHud : Control
{
    private readonly PlayerCommandService _commands = new();
    private WorldState _world = null!;
    private Label _time = null!;
    private Label _character = null!;
    private Label _location = null!;
    private Label _needs = null!;
    private RichTextLabel _log = null!;
    private LineEdit _input = null!;

    public void Initialize(WorldState world)
    {
        _world = world;
        BuildUi();
        Refresh();
        AddLog("Le monde est vivant. Ton personnage vient de naître.");
        AddLog("Commande : « Observe », « Va au Bourg de la Rivière », « Dors »…");
    }

    private void BuildUi()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var background = new ColorRect { Color = new Color(0.035f, 0.045f, 0.065f, 1f) };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_top", 22);
        margin.AddThemeConstantOverride("margin_bottom", 22);
        AddChild(margin);

        var root = new VBoxContainer { Theme = null };
        root.AddThemeConstantOverride("separation", 12);
        margin.AddChild(root);

        var title = new Label { Text = "MURIM.IO", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 28);
        root.AddChild(title);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 18);
        root.AddChild(header);
        _time = AddHeaderLabel(header, "🌅 Jour 1 — Matin");
        _character = AddHeaderLabel(header, "Personnage");
        _location = AddHeaderLabel(header, "Lieu");
        _needs = AddHeaderLabel(header, "Besoins");

        var separator = new HSeparator();
        root.AddChild(separator);

        _log = new RichTextLabel { BbcodeEnabled = true, ScrollFollowing = true };
        _log.SizeFlagsVertical = SizeFlags.ExpandFill;
        _log.Text = "";
        root.AddChild(_log);

        var hint = new Label { Text = "Que veux-tu faire ?", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        root.AddChild(hint);

        var commandRow = new HBoxContainer();
        commandRow.AddThemeConstantOverride("separation", 8);
        root.AddChild(commandRow);
        _input = new LineEdit { PlaceholderText = "Ex. : Observe les alentours", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _input.TextSubmitted += OnCommandSubmitted;
        commandRow.AddChild(_input);
        var send = new Button { Text = "Agir" };
        send.Pressed += SubmitCommand;
        commandRow.AddChild(send);
        _input.GrabFocus();
    }

    private static Label AddHeaderLabel(HBoxContainer parent, string text)
    {
        var label = new Label { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", 15);
        parent.AddChild(label);
        return label;
    }

    private void SubmitCommand() => OnCommandSubmitted(_input.Text);

    private void OnCommandSubmitted(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var result = _commands.Execute(_world, text);
        AddLog(result.Success ? $"> {text}\n{result.Feedback}  [temps : {result.MinutesSpent} min]" : $"> {text}\n⚠ {result.Feedback}");
        Refresh();
        _input.Clear();
        _input.GrabFocus();
    }

    private void Refresh()
    {
        var player = _world.PlayerNpc;
        if (player is null) return;
        _time.Text = $"Jour {_world.Time.Day} — {PeriodIcon(_world.Time.Period)} {_world.Time.Period}";
        _character.Text = $"{player.Identity.DisplayName} — {player.AgeYears} an(s)";
        var location = player.CurrentLocationId is Guid id && _world.Geography.Locations.TryGetValue(id, out var loc) ? loc.Name : "Inconnu";
        _location.Text = $"📍 {location}";
        _needs.Text = $"Faim {player.Needs.Hunger:0} · Soif {player.Needs.Thirst:0} · Fatigue {player.Needs.Fatigue:0}";
    }

    private void AddLog(string text)
    {
        if (_log is null) return;
        _log.AppendText(text.Replace("[", "\\[") + "\n\n");
    }

    private static string PeriodIcon(TimePeriod period) => period switch
    {
        TimePeriod.Morning => "🌅 Matin",
        TimePeriod.Afternoon => "☀️ Après-midi",
        TimePeriod.Evening => "🌙 Soir",
        _ => "🌌 Nuit"
    };
}
