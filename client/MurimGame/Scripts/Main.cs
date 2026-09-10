using Godot;
using Murim.Simulation;

namespace Murim.Game;

public partial class Main : Control
{
    private LivingWorldRuntime _runtime = null!;
    private PlayerCommandFacade _commands = null!;
    private Label _identity = null!;
    private Label _clock = null!;
    private Label _condition = null!;
    private Label _locationTitle = null!;
    private Label _locationSubtitle = null!;
    private VBoxContainer _hotspots = null!;
    private RichTextLabel _story = null!;
    private LineEdit _command = null!;
    private VBoxContainer _nearby = null!;
    private VBoxContainer _knowledge = null!;
    private readonly List<string> _chronicle = new();
    private string _localPlace = string.Empty;

    public override void _Ready()
    {
        _runtime = LivingWorldFactory.CreateRuntime(seed: 190724, npcPopulation: 10_000);
        _commands = new PlayerCommandFacade(_runtime);
        BuildInterface();

        var player = _runtime.World.PlayerNpc!;
        var location = _runtime.World.Locations[player.CurrentLocationId];
        _localPlace = location.Name;
        AddStory(player.AgeYears(_runtime.World.Clock) == 0
            ? $"Vous venez de naître à {location.Name}. Le monde existait avant vous et continuera sans attendre vos choix."
            : $"Vous vous trouvez à {location.Name}. Le monde poursuit sa propre histoire autour de vous.");
        RefreshAll();
    }

    private void BuildInterface()
    {
        var background = new ColorRect
        {
            Color = new Color("090c11"),
            MouseFilter = MouseFilterEnum.Ignore
        };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var outer = new MarginContainer();
        outer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        outer.AddThemeConstantOverride("margin_left", 14);
        outer.AddThemeConstantOverride("margin_right", 14);
        outer.AddThemeConstantOverride("margin_top", 14);
        outer.AddThemeConstantOverride("margin_bottom", 14);
        AddChild(outer);

        var columns = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", 12);
        outer.AddChild(columns);

        columns.AddChild(BuildLeftPanel());
        columns.AddChild(BuildCenterPanel());
        columns.AddChild(BuildRightPanel());
    }

    private Control BuildLeftPanel()
    {
        var panel = MakePanel(290);
        var box = (VBoxContainer)panel.GetChild(0);
        box.AddChild(Heading("PERSONNAGE", 22));
        _identity = BodyLabel(); box.AddChild(_identity);
        box.AddChild(Heading("ÉTAT OBSERVÉ", 16));
        _condition = BodyLabel(); box.AddChild(_condition);
        box.AddChild(Heading("TEMPS", 16));
        _clock = BodyLabel(); box.AddChild(_clock);

        box.AddChild(Heading("ACTIONS RAPIDES", 16));
        box.AddChild(ActionButton("Attendre 1 jour", () => ExecuteCommand("attendre 1 jour")));
        box.AddChild(ActionButton("S'entraîner 1 semaine", () => ExecuteCommand("s'entraîner 1 semaine")));
        box.AddChild(ActionButton("Observer son état", () => ExecuteCommand("statut")));
        return panel;
    }

    private Control BuildCenterPanel()
    {
        var panel = MakePanel();
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var box = (VBoxContainer)panel.GetChild(0);

        _locationTitle = Heading("LIEU", 24); box.AddChild(_locationTitle);
        _locationSubtitle = BodyLabel(); box.AddChild(_locationSubtitle);

        var illustration = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0, 330),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        var illustrationStyle = new StyleBoxFlat { BgColor = new Color("151c26"), BorderColor = new Color("3a4656") };
        illustrationStyle.SetBorderWidthAll(1); illustrationStyle.SetCornerRadiusAll(8);
        illustration.AddThemeStyleboxOverride("panel", illustrationStyle);
        box.AddChild(illustration);

        var illustrationMargin = new MarginContainer();
        illustrationMargin.AddThemeConstantOverride("margin_left", 18); illustrationMargin.AddThemeConstantOverride("margin_right", 18);
        illustrationMargin.AddThemeConstantOverride("margin_top", 18); illustrationMargin.AddThemeConstantOverride("margin_bottom", 18);
        illustration.AddChild(illustrationMargin);

        var illustrationBox = new VBoxContainer(); illustrationMargin.AddChild(illustrationBox);
        var hint = BodyLabel();
        hint.Text = "ILLUSTRATION DU LIEU — les boutons ci-dessous sont les zones cliquables. Une vraie illustration remplacera ce fond sans changer la logique.";
        hint.Modulate = new Color("aeb8c5"); illustrationBox.AddChild(hint);
        _hotspots = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _hotspots.AddThemeConstantOverride("separation", 8); illustrationBox.AddChild(_hotspots);

        box.AddChild(Heading("CHRONIQUE", 16));
        _story = new RichTextLabel
        {
            BbcodeEnabled = false,
            FitContent = false,
            CustomMinimumSize = new Vector2(0, 220),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            ScrollActive = true
        };
        box.AddChild(_story);

        var commandRow = new HBoxContainer();
        _command = new LineEdit
        {
            PlaceholderText = "Commande : attendre 3 jours, s'entraîner 1 semaine, statut…",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        _command.TextSubmitted += OnCommandSubmitted;
        commandRow.AddChild(_command);
        commandRow.AddChild(ActionButton("Exécuter", SubmitCommand));
        box.AddChild(commandRow);
        return panel;
    }

    private Control BuildRightPanel()
    {
        var panel = MakePanel(330);
        var box = (VBoxContainer)panel.GetChild(0);
        box.AddChild(Heading("MONDE PROCHE", 20));
        var nearbyScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 300), SizeFlagsVertical = SizeFlags.ExpandFill };
        _nearby = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        nearbyScroll.AddChild(_nearby); box.AddChild(nearbyScroll);

        box.AddChild(Heading("CE QUE VOUS SAVEZ", 16));
        var knowledgeScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 190), SizeFlagsVertical = SizeFlags.ExpandFill };
        _knowledge = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        knowledgeScroll.AddChild(_knowledge); box.AddChild(knowledgeScroll);
        return panel;
    }

    private PanelContainer MakePanel(float minimumWidth = 0)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(minimumWidth, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
        var style = new StyleBoxFlat { BgColor = new Color("101720"), BorderColor = new Color("293443") };
        style.SetBorderWidthAll(1); style.SetCornerRadiusAll(10); panel.AddThemeStyleboxOverride("panel", style);
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14); margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 14); margin.AddThemeConstantOverride("margin_bottom", 14);
        panel.AddChild(margin);
        var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 8); margin.AddChild(box);
        // Move the box so callers can consistently retrieve the content container.
        panel.RemoveChild(margin); panel.AddChild(box);
        box.AddThemeConstantOverride("margin_left", 0);
        return panel;
    }

    private static Label Heading(string text, int size)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.Modulate = new Color("e4cf9a");
        return label;
    }

    private static Label BodyLabel()
    {
        return new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
    }

    private static Button ActionButton(string text, Action action)
    {
        var button = new Button { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        button.Pressed += action; return button;
    }

    private void OnCommandSubmitted(string text) => ExecuteCommand(text);
    private void SubmitCommand() => ExecuteCommand(_command.Text);

    private void ExecuteCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;
        var result = _commands.Execute(command);
        AddStory($"> {command}\n{result}");
        _command.Clear();
        RefreshAll();
    }

    private void RefreshAll()
    {
        var world = _runtime.World;
        var player = world.PlayerNpc!;
        var location = world.Locations[player.CurrentLocationId];
        var age = player.AgeYears(world.Clock);

        _identity.Text = $"{player.Identity.DisplayName}\n{age} an(s)\nOrigine : {player.Identity.SocialOrigin}";
        _clock.Text = $"An {world.Clock.Year}, jour {world.Clock.DayOfYear}\nJour absolu : {world.Clock.Day}\nHeure : {world.Clock.MinuteOfDay / 60:00}:{world.Clock.MinuteOfDay % 60:00}";
        _condition.Text = string.Join("\n", _runtime.PlayerConditionLines());
        _locationTitle.Text = _localPlace.Length == 0 ? location.Name : _localPlace;
        _locationSubtitle.Text = $"{location.Name} · {location.Region} · {location.Type}";
        RefreshHotspots(location, age);
        RefreshNearby(player, location);
        RefreshKnowledge(player, location);
        _story.Text = string.Join("\n\n", _chronicle.TakeLast(40));
        _story.ScrollToLine(Math.Max(0, _story.GetLineCount() - 1));
    }

    private void RefreshHotspots(WorldLocation location, int age)
    {
        ClearChildren(_hotspots);
        foreach (var spot in LocalSpots(location, age))
        {
            var button = ActionButton(spot, () => VisitSpot(spot, age));
            button.CustomMinimumSize = new Vector2(0, 38);
            _hotspots.AddChild(button);
        }
    }

    private IEnumerable<string> LocalSpots(WorldLocation location, int age)
    {
        if (age < 3) return ["Observer la pièce", "Écouter les voix", "Regarder vers la lumière", "Dormir"];
        if (age < 7) return ["Pièce familiale", "Cour proche", "Cuisine", "Jardin proche", "Rester auprès d'un adulte"];
        if (location.Type.Contains("Domaine", StringComparison.OrdinalIgnoreCase)) return ["Bibliothèque familiale", "Cour familiale", "Cuisine", "Pavillon principal", "Jardin", "Grande porte"];
        if (location.Type.Contains("secte", StringComparison.OrdinalIgnoreCase) || location.Tags.Contains("sect")) return ["Bibliothèque", "Cour d'entraînement", "Réfectoire", "Pavillon des anciens", "Sentier de montagne"];
        if (location.Type.Contains("Village", StringComparison.OrdinalIgnoreCase) || location.Type.Contains("Hameau", StringComparison.OrdinalIgnoreCase)) return ["Maison", "Puits", "Place du village", "Atelier", "Chemin extérieur"];
        if (location.Type.Contains("ville", StringComparison.OrdinalIgnoreCase) || location.Type.Contains("Cité", StringComparison.OrdinalIgnoreCase)) return ["Marché", "Maison de thé", "Rue des artisans", "Administration", "Porte de la ville"];
        return ["Abri", "Chemin", "Environs", "Point d'eau", "Retour"];
    }

    private void VisitSpot(string spot, int age)
    {
        if (age < 3)
        {
            _runtime.AdvanceMinutes(30);
            AddStory(spot switch
            {
                "Observer la pièce" => "Vos yeux suivent lentement les formes proches. Vous ne savez pas encore nommer ce que vous voyez.",
                "Écouter les voix" => "Des voix familières se distinguent du bruit. Certains sons commencent à revenir souvent.",
                "Regarder vers la lumière" => "La lumière change au fil du temps. Le monde est encore surtout sensations, chaleur et silhouettes.",
                _ => "Le sommeil vous emporte. Pour l'instant, grandir est déjà une activité essentielle."
            });
        }
        else
        {
            _localPlace = spot;
            _runtime.AdvanceMinutes(10);
            AddStory(DescribeSpot(spot));
        }
        RefreshAll();
    }

    private static string DescribeSpot(string spot) => spot switch
    {
        "Bibliothèque familiale" => "Vous approchez de la bibliothèque familiale. L'accès, les livres visibles et ce que vous pouvez comprendre dépendront de votre âge, de vos relations et des permissions reçues.",
        "Bibliothèque" => "Des rayonnages contiennent chroniques, registres et peut-être des manuels. Connaître l'existence d'un ouvrage ne signifie pas avoir le droit de le lire.",
        "Cour d'entraînement" => "Des pratiquants travaillent leurs fondations. Vous pouvez observer sans que cela garantisse qu'un maître s'intéressera à vous.",
        "Marché" => "Marchands, travailleurs et voyageurs se croisent. Prix, rumeurs et personnes présentes évoluent même lorsque vous êtes ailleurs.",
        "Maison de thé" => "Une maison de thé est un lieu idéal pour entendre des histoires — vraies, fausses ou déformées.",
        "Grande porte" or "Porte de la ville" or "Chemin extérieur" => "Au-delà commence un espace plus vaste. Voyager prendra du temps et le monde changera pendant le trajet.",
        _ => $"Vous vous rendez vers : {spot}. Rien ne garantit qu'un événement important vous y attende."
    };

    private void RefreshNearby(Npc player, WorldLocation location)
    {
        ClearChildren(_nearby);
        var present = _runtime.World.Npcs.Values.Where(n => n.IsAlive && n.Id != player.Id && n.CurrentLocationId == player.CurrentLocationId).Take(8).ToArray();
        if (present.Length == 0) _nearby.AddChild(TextCard("Personne que vous remarquez pour l'instant."));
        foreach (var npc in present)
        {
            var relation = player.Relationships.GetValueOrDefault(npc.Id);
            var knownName = relation is not null && relation.Familiarity >= 15;
            var label = knownName ? npc.Identity.DisplayName : "Personne inconnue";
            var activity = _runtime.Routines.CurrentActivity(npc, _runtime.World.Clock);
            _nearby.AddChild(TextCard($"{label}\n{activity}"));
        }

        var visibleEvents = _runtime.World.Events
            .Where(e => e.Day <= _runtime.World.Clock.Day && e.LocationId == location.Id && (e.DirectWitnessNpcIds.Contains(player.Id) || e.Publicity >= .75))
            .OrderByDescending(e => e.Day).Take(4).ToArray();
        foreach (var ev in visibleEvents) _nearby.AddChild(TextCard($"Jour {ev.Day} — {ev.Summary}"));
    }

    private void RefreshKnowledge(Npc player, WorldLocation location)
    {
        ClearChildren(_knowledge);
        _knowledge.AddChild(TextCard($"Lieu actuel : {location.Name}"));
        if (player.Identity.IsMonsterBorn && player.Identity.SpeciesCode is not null) _knowledge.AddChild(TextCard($"Nature : {player.Identity.SpeciesCode}"));
        foreach (var entry in player.Knowledge.Take(8)) _knowledge.AddChild(TextCard(entry));
        if (player.Knowledge.Count == 0) _knowledge.AddChild(TextCard("Vous ne connaissez presque rien du vaste Murim."));
        var qiText = player.AgeYears(_runtime.World.Clock) < 10 && player.Physiology.Spirit.QiSensitivity < 45
            ? "Qi : vous ne savez pas encore évaluer votre propre constitution."
            : $"Qi perçu : {player.Physiology.Qi.MutationName}";
        _knowledge.AddChild(TextCard(qiText));
    }

    private static Label TextCard(string text)
    {
        var label = BodyLabel(); label.Text = text; label.CustomMinimumSize = new Vector2(0, 42); return label;
    }

    private void AddStory(string text)
    {
        _chronicle.Add($"[Jour {_runtime.World.Clock.Day}] {text}");
        if (_chronicle.Count > 200) _chronicle.RemoveRange(0, _chronicle.Count - 200);
    }

    private static void ClearChildren(Node node)
    {
        foreach (var child in node.GetChildren()) child.QueueFree();
    }
}
