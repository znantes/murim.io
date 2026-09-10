using Godot;
using Murim.Simulation;

namespace Murim.Game;

public partial class Main : Control
{
    private enum LocalScene { BirthRoom, FamilyRoom, ResidenceCourtyard, FamilyLibrary }

    private LivingWorldRuntime _runtime = null!;
    private PlayerCommandFacade _commands = null!;
    private PortraitGeneticsSystem _portraits = null!;
    private LocalScene _scene = LocalScene.BirthRoom;

    private Label _identity = null!;
    private Label _portrait = null!;
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

    public override void _Ready()
    {
        _runtime = LivingWorldFactory.CreateRuntime(seed: 190724, npcPopulation: 10_000);
        _commands = new PlayerCommandFacade(_runtime);
        _portraits = new PortraitGeneticsSystem(190724 + 811);
        _portraits.InitializeWorld(_runtime.World);
        BuildInterface();

        var player = _runtime.World.PlayerNpc!;
        var location = _runtime.World.Locations[player.CurrentLocationId];
        _scene = player.AgeYears(_runtime.World.Clock) < 3 ? LocalScene.BirthRoom : LocalScene.FamilyRoom;
        AddStory($"Vous commencez votre vie à {location.Name}. Vous n'êtes pas le centre du monde : les autres habitants ont déjà leurs familles, leurs projets et leurs problèmes.");
        RefreshAll();
    }

    private void BuildInterface()
    {
        var background = new ColorRect { Color = new Color("090c11"), MouseFilter = MouseFilterEnum.Ignore };
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
        var panel = MakePanel(300, out var box);
        box.AddChild(Heading("PERSONNAGE", 22));
        _portrait = BodyLabel();
        _portrait.CustomMinimumSize = new Vector2(0, 150);
        _portrait.HorizontalAlignment = HorizontalAlignment.Center;
        _portrait.VerticalAlignment = VerticalAlignment.Center;
        box.AddChild(_portrait);
        _identity = BodyLabel(); box.AddChild(_identity);
        box.AddChild(Heading("ÉTAT OBSERVÉ", 16));
        _condition = BodyLabel(); box.AddChild(_condition);
        box.AddChild(Heading("TEMPS", 16));
        _clock = BodyLabel(); box.AddChild(_clock);
        box.AddChild(Heading("ACTIONS RAPIDES", 16));
        box.AddChild(ActionButton("Attendre 1 jour", () => ExecuteCommand("attendre 1 jour")));
        box.AddChild(ActionButton("Vivre 1 mois", () => ExecuteCommand("attendre 1 mois")));
        box.AddChild(ActionButton("Observer son état", () => ExecuteCommand("statut")));
        return panel;
    }

    private Control BuildCenterPanel()
    {
        var panel = MakePanel(0, out var box);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _locationTitle = Heading("LIEU", 24); box.AddChild(_locationTitle);
        _locationSubtitle = BodyLabel(); box.AddChild(_locationSubtitle);

        var illustration = new PanelContainer { CustomMinimumSize = new Vector2(0, 345), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var style = new StyleBoxFlat { BgColor = new Color("151c26"), BorderColor = new Color("3a4656") };
        style.SetBorderWidthAll(1); style.SetCornerRadiusAll(8);
        illustration.AddThemeStyleboxOverride("panel", style);
        box.AddChild(illustration);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18); margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_top", 18); margin.AddThemeConstantOverride("margin_bottom", 18);
        illustration.AddChild(margin);
        var illustrationBox = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        margin.AddChild(illustrationBox);
        var hint = BodyLabel();
        hint.Text = "SCÈNE INTERACTIVE — les zones deviennent plus précises à mesure que votre personnage apprend à reconnaître son environnement. Les illustrations finales seront posées derrière ces zones cliquables.";
        hint.Modulate = new Color("aeb8c5");
        illustrationBox.AddChild(hint);
        _hotspots = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _hotspots.AddThemeConstantOverride("separation", 8);
        illustrationBox.AddChild(_hotspots);

        box.AddChild(Heading("CHRONIQUE PERSONNELLE", 16));
        _story = new RichTextLabel { BbcodeEnabled = false, FitContent = false, CustomMinimumSize = new Vector2(0, 220), SizeFlagsVertical = SizeFlags.ExpandFill, ScrollActive = true };
        box.AddChild(_story);

        var commandRow = new HBoxContainer();
        _command = new LineEdit { PlaceholderText = "Commande : attendre 1 an, foyer, résidence, bibliothèque, statut…", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _command.TextSubmitted += OnCommandSubmitted;
        commandRow.AddChild(_command);
        commandRow.AddChild(ActionButton("Exécuter", SubmitCommand));
        box.AddChild(commandRow);
        return panel;
    }

    private Control BuildRightPanel()
    {
        var panel = MakePanel(340, out var box);
        box.AddChild(Heading("MONDE PROCHE", 20));
        var nearbyScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 310), SizeFlagsVertical = SizeFlags.ExpandFill };
        _nearby = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        nearbyScroll.AddChild(_nearby); box.AddChild(nearbyScroll);
        box.AddChild(Heading("CE QUE VOUS SAVEZ", 16));
        var knowledgeScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 200), SizeFlagsVertical = SizeFlags.ExpandFill };
        _knowledge = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        knowledgeScroll.AddChild(_knowledge); box.AddChild(knowledgeScroll);
        return panel;
    }

    private static PanelContainer MakePanel(float minimumWidth, out VBoxContainer box)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(minimumWidth, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
        var style = new StyleBoxFlat { BgColor = new Color("101720"), BorderColor = new Color("293443") };
        style.SetBorderWidthAll(1); style.SetCornerRadiusAll(10); panel.AddThemeStyleboxOverride("panel", style);
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14); margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 14); margin.AddThemeConstantOverride("margin_bottom", 14);
        panel.AddChild(margin);
        box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 8);
        margin.AddChild(box);
        return panel;
    }

    private static Label Heading(string text, int size)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.Modulate = new Color("e4cf9a");
        return label;
    }

    private static Label BodyLabel() => new() { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };

    private static Button ActionButton(string text, Action action)
    {
        var button = new Button { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        button.Pressed += action;
        return button;
    }

    private void OnCommandSubmitted(string text) => ExecuteCommand(text);
    private void SubmitCommand() => ExecuteCommand(_command.Text);

    private void ExecuteCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;
        var normalized = command.Trim().ToLowerInvariant();
        string result;
        if (TryNavigation(normalized, out var navigationResult)) result = navigationResult;
        else result = _commands.Execute(command);
        AddStory($"> {command}\n{result}");
        _command.Clear();
        RefreshAll();
    }

    private bool TryNavigation(string command, out string result)
    {
        var age = _runtime.World.PlayerNpc!.AgeYears(_runtime.World.Clock);
        if (command is "foyer" or "aller foyer" or "maison")
        {
            _scene = LocalScene.FamilyRoom; _runtime.AdvanceMinutes(5); result = "Vous revenez auprès de votre foyer."; return true;
        }
        if (command.Contains("résidence") || command.Contains("residence") || command.Contains("cour"))
        {
            if (age < 3) { result = "Vous êtes trop jeune pour vous déplacer seul jusque-là."; return true; }
            _scene = LocalScene.ResidenceCourtyard; _runtime.AdvanceMinutes(10); result = "Vous gagnez la cour de la résidence."; return true;
        }
        if (command.Contains("bibliothèque") || command.Contains("bibliotheque"))
        {
            if (age < 7) { result = "Vous connaissez peut-être ce mot, mais vous êtes encore trop jeune pour vous y rendre seul."; return true; }
            _scene = LocalScene.FamilyLibrary; _runtime.AdvanceMinutes(10); result = "Vous entrez dans la bibliothèque familiale. Les ouvrages accessibles dépendent de ce que votre famille vous autorise à consulter."; return true;
        }
        if (command is "retour")
        {
            _scene = _scene == LocalScene.FamilyLibrary ? LocalScene.ResidenceCourtyard : LocalScene.FamilyRoom;
            _runtime.AdvanceMinutes(5); result = "Vous revenez sur vos pas."; return true;
        }
        result = string.Empty; return false;
    }

    private void RefreshAll()
    {
        var world = _runtime.World;
        var player = world.PlayerNpc!;
        var location = world.Locations[player.CurrentLocationId];
        var age = player.AgeYears(world.Clock);
        _portraits.UpdateVisibleAge(player, world.Clock);
        _portraits.SyncPermanentMarks(player);
        var genome = _portraits.GetOrCreate(player);
        var appearance = _portraits.AppearanceFor(player);

        _portrait.Text = $"PORTRAIT PARAMÉTRIQUE\nvisage #{Math.Abs(player.Id.GetHashCode()):X6}\nâge apparent {appearance.ApparentAge:0.#} ans\ntraits héréditaires actifs\nvisage {genome.FaceWidth:0.00} · mâchoire {genome.JawWidth:0.00} · yeux {genome.EyeSize:0.00}";
        _identity.Text = $"{player.Identity.DisplayName}\n{age} an(s)\nOrigine : {player.Identity.SocialOrigin}";
        _clock.Text = $"An {world.Clock.Year}, jour {world.Clock.DayOfYear}\nJour absolu : {world.Clock.Day}\nHeure : {world.Clock.MinuteOfDay / 60:00}:{world.Clock.MinuteOfDay % 60:00}";
        _condition.Text = string.Join("\n", _runtime.PlayerConditionLines());
        _locationTitle.Text = SceneName(_scene, location, age);
        _locationSubtitle.Text = $"{location.Name} · {location.Region} · {location.Type}";
        RefreshHotspots(age);
        RefreshNearby(player, location);
        RefreshKnowledge(player, location);
        _story.Text = string.Join("\n\n", _chronicle.TakeLast(50));
    }

    private void RefreshHotspots(int age)
    {
        ClearChildren(_hotspots);
        foreach (var (label, action) in SceneActions(age))
        {
            var button = ActionButton(label, action);
            button.CustomMinimumSize = new Vector2(0, 40);
            _hotspots.AddChild(button);
        }
    }

    private IEnumerable<(string Label, Action Action)> SceneActions(int age)
    {
        if (age < 3)
        {
            yield return ("Observer les silhouettes", () => InfantAction("Vos yeux suivent les silhouettes familières sans encore connaître tous leurs noms."));
            yield return ("Écouter les voix", () => InfantAction("Certaines voix deviennent rassurantes. Les sons répétés commencent à former votre monde."));
            yield return ("Jouer / essayer de bouger", () => InfantAction("Vous expérimentez maladroitement votre corps et l'espace autour de vous."));
            yield return ("Dormir", () => { _runtime.AdvanceMinutes(180); AddStory("Vous vous endormez. Pendant ce temps, la maison et le reste du monde continuent à vivre."); RefreshAll(); });
            yield break;
        }

        if (_scene is LocalScene.BirthRoom or LocalScene.FamilyRoom)
        {
            yield return ("Observer le foyer", () => Visit(LocalScene.FamilyRoom, 20, "Vous observez les habitudes de votre famille : repas, travail, discussions et petits désaccords."));
            yield return ("Aller dans la cour", () => Visit(LocalScene.ResidenceCourtyard, 10, "Vous quittez la pièce familiale et rejoignez la cour."));
            yield return ("Passer du temps avec la famille", () => { _runtime.AdvanceMinutes(120); AddStory("Vous passez du temps avec les personnes de votre foyer. De petites interactions construisent lentement les relations."); RefreshAll(); });
            yield break;
        }

        if (_scene == LocalScene.ResidenceCourtyard)
        {
            yield return ("Retourner au foyer", () => Visit(LocalScene.FamilyRoom, 10, "Vous retournez dans les pièces de votre foyer."));
            yield return ("Observer la cour familiale", () => { _runtime.AdvanceMinutes(30); AddStory("Serviteurs, parents, visiteurs et travailleurs traversent la cour selon leurs propres routines."); RefreshAll(); });
            if (age >= 7) yield return ("Bibliothèque familiale", () => Visit(LocalScene.FamilyLibrary, 10, "Vous vous dirigez vers la bibliothèque familiale."));
            else yield return ("Bibliothèque familiale — pas encore accessible seul", () => AddStoryAndRefresh("Vous savez qu'un bâtiment contient des livres, mais les adultes ne vous laissent pas encore y aller seul."));
            yield break;
        }

        if (_scene == LocalScene.FamilyLibrary)
        {
            yield return ("Lire les registres familiaux", ReadFamilyRegister);
            yield return ("Chercher un livre sur le Qi", ReadQiIntroduction);
            yield return ("Chercher des manuels martiaux", SearchMartialManuals);
            yield return ("Retourner dans la cour", () => Visit(LocalScene.ResidenceCourtyard, 10, "Vous quittez la bibliothèque."));
        }
    }

    private void InfantAction(string text)
    {
        _runtime.AdvanceMinutes(30);
        var player = _runtime.World.PlayerNpc!;
        player.Skills["observation"] = Math.Clamp(player.Skills.GetValueOrDefault("observation") + .03, 0, 100);
        player.Skills["language"] = Math.Clamp(player.Skills.GetValueOrDefault("language") + .015, 0, 100);
        AddStory(text);
        RefreshAll();
    }

    private void Visit(LocalScene scene, int minutes, string text)
    {
        _scene = scene;
        _runtime.AdvanceMinutes(minutes);
        AddStory(text);
        RefreshAll();
    }

    private void ReadFamilyRegister()
    {
        var player = _runtime.World.PlayerNpc!;
        if (player.AgeYears(_runtime.World.Clock) < 7) { AddStoryAndRefresh("Les caractères sont encore trop difficiles à lire seul."); return; }
        _runtime.AdvanceMinutes(45);
        player.Knowledge.Add("Vous avez commencé à reconnaître certains noms et branches de votre famille dans les registres.");
        player.Skills["literacy"] = Math.Clamp(player.Skills.GetValueOrDefault("literacy") + .15, 0, 100);
        AddStory("Vous passez du temps sur les registres familiaux. Beaucoup de passages restent obscurs, mais quelques noms commencent à avoir un sens.");
        RefreshAll();
    }

    private void ReadQiIntroduction()
    {
        var player = _runtime.World.PlayerNpc!;
        if (player.AgeYears(_runtime.World.Clock) < 8) { AddStoryAndRefresh("Le texte sur la respiration et le Qi est encore trop abstrait pour vous."); return; }
        _runtime.AdvanceMinutes(60);
        player.Knowledge.Add("Vous avez lu une introduction élémentaire au vocabulaire du Qi. Cela ne révèle pas automatiquement votre propre constitution.");
        player.Skills["literacy"] = Math.Clamp(player.Skills.GetValueOrDefault("literacy") + .18, 0, 100);
        AddStory("Le livre décrit souffle, posture et vocabulaire du Qi. Vous comprenez des mots, pas encore la vérité complète de votre propre corps.");
        RefreshAll();
    }

    private void SearchMartialManuals()
    {
        var player = _runtime.World.PlayerNpc!;
        _runtime.AdvanceMinutes(40);
        var age = player.AgeYears(_runtime.World.Clock);
        if (age < 10)
        {
            AddStory("Vous repérez des ouvrages gardés hors de portée. Savoir qu'ils existent ne signifie ni les comprendre ni avoir la permission de les lire.");
            RefreshAll(); return;
        }
        var ordinary = _runtime.Content.Techniques.First(t => t.Rarity == Rarity.Ordinary && t.Domain is TechniqueDomain.Fist or TechniqueDomain.Palm or TechniqueDomain.InternalCultivation);
        player.Knowledge.Add($"Vous avez aperçu le titre d'un manuel : {ordinary.Name}.");
        AddStory($"Vous trouvez la trace du manuel « {ordinary.Name} ». Pour l'instant, vous n'en connaissez que l'existence ; il faudra obtenir l'accès, le lire et le comprendre avant de pouvoir l'apprendre.");
        RefreshAll();
    }

    private void AddStoryAndRefresh(string text) { AddStory(text); RefreshAll(); }

    private static string SceneName(LocalScene scene, WorldLocation location, int age) => scene switch
    {
        LocalScene.BirthRoom => age < 1 ? "Pièce aux formes encore inconnues" : "Pièce familiale",
        LocalScene.FamilyRoom => "Foyer familial",
        LocalScene.ResidenceCourtyard => "Cour de la résidence",
        LocalScene.FamilyLibrary => "Bibliothèque familiale",
        _ => location.Name
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
            _portraits.GetOrCreate(npc);
            _portraits.UpdateVisibleAge(npc, _runtime.World.Clock);
            var apparentAge = _portraits.AppearanceFor(npc).ApparentAge;
            var activity = _runtime.Routines.CurrentActivity(npc, _runtime.World.Clock);
            _nearby.AddChild(TextCard($"{label}\nâge apparent ~{apparentAge:0} · {activity}"));
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
        if (_chronicle.Count > 250) _chronicle.RemoveRange(0, _chronicle.Count - 250);
    }

    private static void ClearChildren(Node node)
    {
        foreach (var child in node.GetChildren()) child.QueueFree();
    }
}
