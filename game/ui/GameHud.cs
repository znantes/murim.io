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
    private TabContainer _tabs = null!;
    private double _realTimeAccumulator;
    private readonly Dictionary<string, RichTextLabel> _panels = new();

    public void Initialize(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        _world = world;
        BuildUi();
        Refresh();
        AddLog("Le monde est vivant. Ton personnage vient de naître.");
        AddLog("Commande : « Observe », « Va au Bourg de la Rivière », « Dors »…");
    }

    public override void _Process(double delta)
    {
        if (_world?.PlayerNpc is null || delta <= 0) return;
        _realTimeAccumulator += delta;
        var advanced = false;
        while (_realTimeAccumulator >= 1.0)
        {
            _realTimeAccumulator -= 1.0;
            _world.AdvanceMinutes(1);
            advanced = true;
        }
        if (advanced) Refresh();
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

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        margin.AddChild(root);

        var title = new Label { Text = "MURIM.IO — UNE VIE PARMI DES MILLIERS", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeFontSizeOverride("font_size", 26);
        root.AddChild(title);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 12);
        root.AddChild(header);
        _time = AddHeaderLabel(header, "🌅 Jour 1 — Matin");
        _character = AddHeaderLabel(header, "Personnage");
        _location = AddHeaderLabel(header, "Lieu");
        _needs = AddHeaderLabel(header, "Besoins");

        _tabs = new TabContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        root.AddChild(_tabs);
        AddProfileTab("Profil", BuildProfileText);
        AddProfileTab("Corps", BuildBodyText);
        AddProfileTab("Connaissances", BuildKnowledgeText);
        AddProfileTab("Engagements", BuildCommitmentsText);
        AddProfileTab("Arts martiaux", BuildMartialText);
        AddProfileTab("Relations", BuildRelationsText);
        AddProfileTab("Inventaire", BuildInventoryText);
        AddProfileTab("Journal", BuildJournalText);
        AddProfileTab("Monde", BuildWorldText);

        var separator = new HSeparator();
        root.AddChild(separator);

        _log = new RichTextLabel { BbcodeEnabled = true, ScrollFollowing = true, FitContent = false };
        _log.CustomMinimumSize = new Vector2(0, 105);
        root.AddChild(_log);

        var hint = new Label { Text = "Que veux-tu faire ? Le monde continue d'avancer même sans commande.", AutowrapMode = TextServer.AutowrapMode.WordSmart };
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

    private void AddProfileTab(string title, Func<Npc, string> formatter)
    {
        var panel = new RichTextLabel { BbcodeEnabled = true, FitContent = false, ScrollActive = true };
        panel.Name = title.Replace(" ", string.Empty);
        panel.SetMeta("formatter", formatter.Method.Name);
        _tabs.AddChild(panel);
        _panels[title] = panel;
    }

    private static Label AddHeaderLabel(HBoxContainer parent, string text)
    {
        var label = new Label { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", 14);
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

        _time.Text = $"Jour {_world.Time.Day} — {PeriodIcon(_world.Time.Period)}";
        _character.Text = $"{player.Identity.DisplayName} — {player.AgeYears} an(s)";
        var location = player.CurrentLocationId is Guid id && _world.Geography.Locations.TryGetValue(id, out var loc) ? loc.Name : "Inconnu";
        _location.Text = $"📍 {location}";
        _needs.Text = $"Faim {player.Needs.Hunger:0} · Soif {player.Needs.Thirst:0} · Fatigue {player.Needs.Fatigue:0}";

        _panels["Profil"].Text = BuildProfileText(player);
        _panels["Corps"].Text = BuildBodyText(player);
        _panels["Connaissances"].Text = BuildKnowledgeText(player);
        _panels["Engagements"].Text = BuildCommitmentsText(player);
        _panels["Arts martiaux"].Text = BuildMartialText(player);
        _panels["Relations"].Text = BuildRelationsText(player);
        _panels["Inventaire"].Text = BuildInventoryText(player);
        _panels["Journal"].Text = BuildJournalText(player);
        _panels["Monde"].Text = BuildWorldText(player);
    }

    private string BuildProfileText(Npc player) =>
        $"[font_size=20][b]{player.Identity.DisplayName}[/b][/font_size]\n\n" +
        $"Âge : {player.AgeYears} an(s)\n" +
        $"Profession : {player.Profession.Type} · compétence {player.Profession.Skill}\n" +
        $"Revenus quotidiens : {player.Profession.DailyIncome:0.##}\n" +
        $"Patrimoine : {player.Wealth:0.##}\n\n" +
        "[b]Esprit[/b]\n" +
        $"Intelligence {player.Mind.Intelligence:0} · mémoire {player.Mind.Memory:0} · volonté {player.Mind.Willpower:0}\n" +
        $"Apprentissage {player.Mind.LearningAbility:0} · perception {player.Mind.Perception:0} · curiosité {player.Mind.Curiosity:0}\n\n" +
        "[b]Personnalité[/b]\n" +
        $"Ambition {player.Personality.Ambition:0} · prudence {player.Personality.Prudence:0} · sociabilité {player.Personality.Sociability:0}\n" +
        $"Courage {player.Personality.Courage:0} · empathie {player.Personality.Empathy:0} · discipline {player.Personality.Discipline:0}";

    private string BuildBodyText(Npc player) =>
        "[font_size=20][b]État physique[/b][/font_size]\n\n" +
        $"Taille : {player.Body.HeightCm:0.0} cm · poids : {player.Body.WeightKg:0.0} kg\n" +
        $"Santé : {player.Body.Health:0.0}\n" +
        $"Force {player.Body.Strength:0.0} · vitesse {player.Body.Speed:0.0} · endurance {player.Body.Endurance:0.0}\n" +
        $"Coordination {player.Body.Coordination:0.0} · souplesse {player.Body.Flexibility:0.0} · récupération {player.Body.Recovery:0.0}\n" +
        $"Douleur actuelle : {player.CurrentPain:0.0} · mobilité : {player.Mobility:P0}\n\n" +
        (player.Conditions.Count == 0 ? "Aucune condition physique connue." : string.Join("\n", player.Conditions.Select(c => $"• {c.Name} — gravité {c.Severity:0.0}, douleur {c.Pain:0.0}")));

    private string BuildKnowledgeText(Npc player)
    {
        var entries = player.Knowledge.Take(40).Select(k => $"• [{k.Kind}] {k.Summary} — confiance {k.Confidence:P0}");
        return "[font_size=20][b]Ce que le personnage sait[/b][/font_size]\n\n" +
               (entries.Any() ? string.Join("\n", entries) : "Le personnage ne possède encore presque aucune connaissance.");
    }

    private string BuildCommitmentsText(Npc player) =>
        "[font_size=20][b]Engagements et travail[/b][/font_size]\n\n" +
        $"Profession actuelle : {player.Profession.Type}\n" +
        $"Statut actif : {(player.Profession.IsActive ? "oui" : "non")}\n" +
        $"Organisation : {(player.Profession.OrganizationId is Guid id ? id.ToString() : "aucune")}\n" +
        $"Revenus : {player.Profession.DailyIncome:0.##}/jour\n" +
        $"Dépenses : {player.Profession.DailyExpense:0.##}/jour";

    private string BuildMartialText(Npc player)
    {
        var techniques = player.Martial.Techniques.Select(t => $"• {t.TechniqueId} — maîtrise {t.Proficiency:0.00}/{t.Potential:0.00}");
        return "[font_size=20][b]Arts martiaux[/b][/font_size]\n\n" +
               $"Discipline physique : {player.Martial.PhysicalDiscipline:0.0}\n" +
               $"Expérience de combat : {player.Martial.CombatExperience:0.0}\n" +
               $"Capacité énergétique : {player.Martial.InternalEnergyCapacity:0.0}\n" +
               $"Contrôle énergétique : {player.Martial.InternalEnergyControl:0.0}\n\n" +
               (techniques.Any() ? string.Join("\n", techniques) : "Aucune technique martiale connue.");
    }

    private string BuildRelationsText(Npc player)
    {
        var relations = player.Relationships.Where(r => r.IsActive).Take(40).Select(r =>
        {
            var name = _world.Npcs.TryGetValue(r.ToNpcId, out var other) ? other.Identity.DisplayName : "Inconnu";
            return $"• {name} — {r.Type} · affinité {r.Affinity:+0.00;-0.00;0.00} · confiance {r.Trust:P0} · respect {r.Respect:P0}";
        });
        return "[font_size=20][b]Relations[/b][/font_size]\n\n" +
               (relations.Any() ? string.Join("\n", relations) : "Aucune relation connue.");
    }

    private string BuildInventoryText(Npc player)
    {
        var entries = player.Inventory.Entries.Select(e =>
        {
            var name = _world.Inventory.Items.TryGetValue(e.ItemId, out var item) ? item.Name : "Objet inconnu";
            return $"• {name} × {e.Quantity}";
        });
        return "[font_size=20][b]Inventaire[/b][/font_size]\n\n" +
               $"Poids : {_world.Inventory.WeightOf(player.Inventory):0.00} kg\n" +
               $"Valeur estimée : {_world.Inventory.ValueOf(player.Inventory):0.00}\n\n" +
               (entries.Any() ? string.Join("\n", entries) : "Inventaire vide.");
    }

    private string BuildJournalText(Npc player)
    {
        var events = player.History.Events.TakeLast(40).Reverse().Select(e => $"• {e.AgeYears} an(s) — [b]{e.Type}[/b] : {e.Description}");
        return "[font_size=20][b]Journal de vie[/b][/font_size]\n\n" +
               (events.Any() ? string.Join("\n", events) : "La vie du personnage commence ici.");
    }

    private string BuildWorldText(Npc player)
    {
        var locations = _world.Geography.Locations.Values.OrderBy(l => l.Type).ThenBy(l => l.Name)
            .Select(l => $"• {l.Name} — {l.Type} · population {l.Population} · danger {l.DangerLevel}");
        var buildings = _world.Buildings.AtLocation(player.CurrentLocationId ?? Guid.Empty)
            .Select(b => $"• {b.Name} — {b.Type} · {(b.IsOpen(_world.Time.Period) ? "ouvert" : "fermé")}");
        return "[font_size=20][b]Monde connu[/b][/font_size]\n\n" +
               "[b]Lieux[/b]\n" + string.Join("\n", locations) + "\n\n" +
               "[b]Bâtiments du lieu actuel[/b]\n" + (buildings.Any() ? string.Join("\n", buildings) : "Aucun bâtiment connu ici.");
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
