using Godot;
using Murim.Simulation;

namespace Murim.Game;

public partial class ModernMain : Control
{
    private enum LocalScene { Home, Courtyard, Library, Settlement, District }

    private LivingWorldRuntime runtime = null!;
    private PlayerCommandFacade commands = null!;
    private PortraitGeneticsSystem portraits = null!;
    private LocalScene scene = LocalScene.Home;
    private Guid? selectedDistrictId;

    private Control? startupRoot;
    private Label? startupLabel;
    private CharacterVisualView playerVisual = null!;
    private Label identity = null!;
    private Label condition = null!;
    private Label clock = null!;
    private Label headerPlace = null!;
    private Label headerTime = null!;
    private Label locationTitle = null!;
    private Label locationSubtitle = null!;
    private WorldIllustrationView illustration = null!;
    private GridContainer actions = null!;
    private RichTextLabel chronicle = null!;
    private LineEdit command = null!;
    private VBoxContainer familyBox = null!;
    private VBoxContainer nearbyBox = null!;
    private VBoxContainer knowledgeBox = null!;
    private readonly List<string> story = new();

    private static readonly Color Page = new("e8e2d7");
    private static readonly Color Paper = new("f7f3ea");
    private static readonly Color PaperAlt = new("eee8dc");
    private static readonly Color Ink = new("28343a");
    private static readonly Color Muted = new("68747a");
    private static readonly Color Accent = new("9a6b38");
    private static readonly Color AccentSoft = new("d9c29d");
    private static readonly Color Border = new("d3c8b6");
    private static readonly Color Deep = new("31464a");

    public override async void _Ready()
    {
        ShowStartup();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var task = System.Threading.Tasks.Task.Run(() => LivingWorldFactory.CreateRuntime(seed: 190724, npcPopulation: 10_000));
            while (!task.IsCompleted)
            {
                if (startupLabel is not null)
                    startupLabel.Text = $"Le monde se met en mouvement…\n\n10 000 vies, familles, métiers et histoires\n{sw.Elapsed.TotalSeconds:0.0} s";
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            runtime = await task;
            commands = new PlayerCommandFacade(runtime);
            portraits = new PortraitGeneticsSystem();

            var player = runtime.World.PlayerNpc ?? throw new InvalidOperationException("Aucun personnage joueur n'a été créé.");
            EnsurePortrait(player);
            scene = player.AgeYears(runtime.World.Clock) < 3 ? LocalScene.Home : LocalScene.Home;

            HideStartup();
            BuildInterface();
            AddStory("Votre vie commence. Le monde ne vous attend pas : autour de vous, des milliers de personnes vivent déjà leurs propres histoires.");
            RefreshAll();
            GD.Print($"Murim modern UI startup completed; NPCs={runtime.World.Npcs.Count}");
        }
        catch (Exception ex)
        {
            GD.PrintErr(ex.ToString());
            if (startupLabel is null) ShowStartup();
            if (startupLabel is not null)
                startupLabel.Text = $"Le monde n'a pas pu démarrer.\n\n{ex.GetType().Name}: {ex.Message}\n\nConsultez les journaux du jeu.";
        }
    }

    private void ShowStartup()
    {
        var bg = new ColorRect { Color = Page };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bg.AddChild(center);
        var card = Card(600, out var box, 24);
        center.AddChild(card);
        var title = Title("MURIM", 34, Deep); title.HorizontalAlignment = HorizontalAlignment.Center; box.AddChild(title);
        var sub = Text("UNE VIE PARMI DES MILLIERS", 15, Accent); sub.HorizontalAlignment = HorizontalAlignment.Center; box.AddChild(sub);
        startupLabel = Text("Le monde se met en mouvement…", 17, Ink); startupLabel.HorizontalAlignment = HorizontalAlignment.Center; box.AddChild(startupLabel);
        startupRoot = bg;
    }

    private void HideStartup()
    {
        startupRoot?.QueueFree();
        startupRoot = null;
        startupLabel = null;
    }

    private void BuildInterface()
    {
        var bg = new ColorRect { Color = Page, MouseFilter = MouseFilterEnum.Ignore };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var rootMargin = new MarginContainer();
        rootMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        rootMargin.AddThemeConstantOverride("margin_left", 16);
        rootMargin.AddThemeConstantOverride("margin_right", 16);
        rootMargin.AddThemeConstantOverride("margin_top", 14);
        rootMargin.AddThemeConstantOverride("margin_bottom", 14);
        AddChild(rootMargin);

        var page = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        page.AddThemeConstantOverride("separation", 12);
        rootMargin.AddChild(page);
        page.AddChild(BuildTopBar());

        var columns = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", 12);
        page.AddChild(columns);
        columns.AddChild(BuildCharacterPanel());
        columns.AddChild(BuildWorldPanel());
        columns.AddChild(BuildPeoplePanel());
    }

    private Control BuildTopBar()
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(0, 58), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var style = Rounded(Deep, Deep, 14, 0);
        panel.AddThemeStyleboxOverride("panel", style);
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18); margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_top", 8); margin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(margin);
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        margin.AddChild(row);
        var left = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddChild(left);
        left.AddChild(Title("MURIM", 21, new Color("f8f1e5")));
        headerPlace = Text("Monde vivant", 13, new Color("d8e0dc")); left.AddChild(headerPlace);
        headerTime = Text("", 14, new Color("f1d69e")); headerTime.HorizontalAlignment = HorizontalAlignment.Right; headerTime.VerticalAlignment = VerticalAlignment.Center; row.AddChild(headerTime);
        return panel;
    }

    private Control BuildCharacterPanel()
    {
        var panel = Card(305, out var box, 14);
        panel.SizeFlagsVertical = SizeFlags.ExpandFill;
        box.AddChild(SectionHeader("VOTRE PERSONNAGE"));

        var visualFrame = new PanelContainer { CustomMinimumSize = new Vector2(0, 270) };
        visualFrame.AddThemeStyleboxOverride("panel", Rounded(new Color("eee7da"), Border, 14, 1));
        playerVisual = new CharacterVisualView { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        visualFrame.AddChild(playerVisual);
        box.AddChild(visualFrame);

        identity = Text("", 16, Ink); box.AddChild(identity);
        box.AddChild(Divider());
        box.AddChild(SectionHeader("CORPS & ÉTAT"));
        condition = Text("", 14, Ink); box.AddChild(condition);
        box.AddChild(Divider());
        box.AddChild(SectionHeader("TEMPS"));
        clock = Text("", 14, Ink); box.AddChild(clock);

        var quick = new GridContainer { Columns = 1, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        quick.AddChild(ActionButton("Observer mon état", () => Execute("statut"), false));
        quick.AddChild(ActionButton("Attendre 1 jour", () => Execute("attendre 1 jour"), false));
        quick.AddChild(ActionButton("Vivre 1 mois", () => Execute("attendre 1 mois"), false));
        box.AddChild(quick);
        return panel;
    }

    private Control BuildWorldPanel()
    {
        var panel = Card(0, out var box, 14);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel.SizeFlagsVertical = SizeFlags.ExpandFill;

        locationTitle = Title("Lieu", 27, Ink); box.AddChild(locationTitle);
        locationSubtitle = Text("", 14, Muted); box.AddChild(locationSubtitle);
        illustration = new WorldIllustrationView { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 340) };
        box.AddChild(illustration);

        box.AddChild(SectionHeader("QUE FAIRE ICI ?"));
        actions = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        actions.AddThemeConstantOverride("h_separation", 8); actions.AddThemeConstantOverride("v_separation", 8);
        box.AddChild(actions);

        box.AddChild(SectionHeader("CHRONIQUE"));
        chronicle = new RichTextLabel
        {
            BbcodeEnabled = false,
            FitContent = false,
            CustomMinimumSize = new Vector2(0, 120),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            ScrollActive = true,
            SelectionEnabled = true
        };
        chronicle.AddThemeColorOverride("default_color", Ink);
        chronicle.AddThemeFontSizeOverride("normal_font_size", 14);
        box.AddChild(chronicle);

        var commandCard = new PanelContainer();
        commandCard.AddThemeStyleboxOverride("panel", Rounded(PaperAlt, Border, 11, 1));
        var commandMargin = new MarginContainer();
        commandMargin.AddThemeConstantOverride("margin_left", 10); commandMargin.AddThemeConstantOverride("margin_right", 8);
        commandMargin.AddThemeConstantOverride("margin_top", 7); commandMargin.AddThemeConstantOverride("margin_bottom", 7);
        commandCard.AddChild(commandMargin);
        var commandRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        commandRow.AddThemeConstantOverride("separation", 8); commandMargin.AddChild(commandRow);
        command = new LineEdit { PlaceholderText = "Écrivez une action : bibliothèque, dormir, attendre 1 semaine…", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        command.AddThemeColorOverride("font_color", Ink); command.AddThemeColorOverride("font_placeholder_color", Muted);
        command.TextSubmitted += _ => SubmitCommand(); commandRow.AddChild(command);
        commandRow.AddChild(ActionButton("Exécuter", SubmitCommand));
        box.AddChild(commandCard);
        return panel;
    }

    private Control BuildPeoplePanel()
    {
        var panel = Card(360, out var box, 14);
        panel.SizeFlagsVertical = SizeFlags.ExpandFill;
        box.AddChild(SectionHeader("FAMILLE"));
        familyBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        familyBox.AddThemeConstantOverride("separation", 8); box.AddChild(familyBox);
        box.AddChild(Divider());
        box.AddChild(SectionHeader("AUTOUR DE VOUS"));
        var nearbyScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 250), SizeFlagsVertical = SizeFlags.ExpandFill };
        nearbyBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; nearbyBox.AddThemeConstantOverride("separation", 8);
        nearbyScroll.AddChild(nearbyBox); box.AddChild(nearbyScroll);
        box.AddChild(Divider());
        box.AddChild(SectionHeader("CE QUE VOUS SAVEZ"));
        var knowScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 135), SizeFlagsVertical = SizeFlags.ExpandFill };
        knowledgeBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; knowledgeBox.AddThemeConstantOverride("separation", 5);
        knowScroll.AddChild(knowledgeBox); box.AddChild(knowScroll);
        return panel;
    }

    private void RefreshAll()
    {
        var world = runtime.World;
        var player = world.PlayerNpc!;
        var location = world.Locations[player.CurrentLocationId];
        var age = player.AgeYears(world.Clock);
        var genome = EnsurePortrait(player);
        portraits.UpdateVisibleAge(player, world.Clock);
        portraits.SyncPermanentMarks(player);
        var appearance = portraits.AppearanceFor(player);
        playerVisual.SetCharacter(player, genome, appearance);

        identity.Text = $"{player.Identity.DisplayName}\n{age} an{(age > 1 ? "s" : "")} · {SexLabel(player.Identity.Sex)}\n{player.Identity.SocialOrigin}";
        condition.Text = string.Join("\n", runtime.PlayerConditionLines());
        clock.Text = $"An {world.Clock.Year} · jour {world.Clock.DayOfYear}\n{world.Clock.MinuteOfDay / 60:00}:{world.Clock.MinuteOfDay % 60:00} · {SeasonLabel(world.Clock.DayOfYear)}";
        headerPlace.Text = $"{location.Name} · {location.Region}";
        headerTime.Text = $"{SeasonLabel(world.Clock.DayOfYear)}   ·   {world.Clock.MinuteOfDay / 60:00}:{world.Clock.MinuteOfDay % 60:00}";
        locationTitle.Text = SceneName(age, location);
        locationSubtitle.Text = SceneSubtitle(location, age);
        RefreshIllustration(player, location);
        RefreshActions(age);
        RefreshFamily(player);
        RefreshNearby(player, location);
        RefreshKnowledge(player, location);
        chronicle.Text = string.Join("\n\n", story.TakeLast(60));
    }

    private void RefreshFamily(Npc player)
    {
        Clear(familyBox);
        if (player.ParentIds.Count == 0)
        {
            familyBox.AddChild(Text("Vos liens familiaux ne sont pas encore établis clairement.", 13, Muted));
            return;
        }
        foreach (var id in player.ParentIds.Take(2))
        {
            if (!runtime.World.Npcs.TryGetValue(id, out var parent)) continue;
            var role = parent.Identity.Sex == Sex.Female ? "Mère" : parent.Identity.Sex == Sex.Male ? "Père" : "Parent";
            familyBox.AddChild(PersonCard(parent, role, player, true));
        }
    }

    private void RefreshNearby(Npc player, WorldLocation location)
    {
        Clear(nearbyBox);
        var people = runtime.World.Npcs.Values
            .Where(n => n.IsAlive && n.Id != player.Id && n.CurrentLocationId == location.Id && !player.ParentIds.Contains(n.Id))
            .OrderByDescending(n => player.Relationships.ContainsKey(n.Id))
            .ThenBy(n => n.Id)
            .Take(7)
            .ToList();
        if (people.Count == 0)
        {
            nearbyBox.AddChild(Text("Personne n'attire particulièrement votre attention ici.", 13, Muted));
            return;
        }
        foreach (var npc in people)
            nearbyBox.AddChild(PersonCard(npc, KnownName(player, npc), player, false));
    }

    private Control PersonCard(Npc npc, string title, Npc observer, bool family)
    {
        var card = new PanelContainer { CustomMinimumSize = new Vector2(0, family ? 116 : 104) };
        card.AddThemeStyleboxOverride("panel", Rounded(PaperAlt, Border, 11, 1));
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8); margin.AddThemeConstantOverride("margin_right", 9);
        margin.AddThemeConstantOverride("margin_top", 7); margin.AddThemeConstantOverride("margin_bottom", 7);
        card.AddChild(margin);
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 10); margin.AddChild(row);

        var g = EnsurePortrait(npc); portraits.UpdateVisibleAge(npc, runtime.World.Clock); portraits.SyncPermanentMarks(npc);
        var visual = new CharacterVisualView(); visual.SetCharacter(npc, g, portraits.AppearanceFor(npc), true); row.AddChild(visual);
        var info = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; row.AddChild(info);
        var name = family ? $"{title} · {npc.Identity.DisplayName}" : title;
        info.AddChild(Title(name, 15, Ink));
        var ageKnown = family || observer.Relationships.ContainsKey(npc.Id);
        info.AddChild(Text(ageKnown ? $"{npc.AgeYears(runtime.World.Clock)} ans" : $"âge apparent ~{Math.Max(0, (int)Math.Round(portraits.AppearanceFor(npc).ApparentAge))}", 12, Muted));
        var injury = npc.Injuries.Where(i => i.Active || i.Permanent).OrderByDescending(i => i.Severity).FirstOrDefault();
        info.AddChild(Text(injury is null ? "Aucune blessure visible" : injury.DisplayText, 11, injury is null ? new Color("60776b") : new Color("a54f51")));
        return card;
    }

    private string KnownName(Npc observer, Npc subject)
    {
        if (observer.Relationships.ContainsKey(subject.Id) || observer.ChildIds.Contains(subject.Id) || observer.SpouseId == subject.Id)
            return subject.Identity.DisplayName;
        return "Personne inconnue";
    }

    private void RefreshKnowledge(Npc player, WorldLocation location)
    {
        Clear(knowledgeBox);
        knowledgeBox.AddChild(Text($"Lieu actuel : {location.Name}", 13, Ink));
        knowledgeBox.AddChild(Text(player.Knowledge.Count == 0 ? "Votre compréhension du monde est encore très limitée." : $"Connaissances mémorisées : {player.Knowledge.Count}", 12, Muted));
        if (player.AgeYears(runtime.World.Clock) < 7)
            knowledgeBox.AddChild(Text("Qi : vous ne savez pas encore évaluer votre constitution.", 12, Muted));
        else if (player.Physiology.Spirit.QiSensitivity < 35)
            knowledgeBox.AddChild(Text("Qi : sensations encore difficiles à interpréter.", 12, Muted));
        else
            knowledgeBox.AddChild(Text($"Qi perçu : tendance {player.Physiology.Qi.Tendency}", 12, Muted));
    }

    private void RefreshActions(int age)
    {
        Clear(actions);
        foreach (var (label, callback) in SceneActions(age))
            actions.AddChild(ActionButton(label, callback));
    }

    private IEnumerable<(string Label, Action Callback)> SceneActions(int age)
    {
        if (age < 3)
        {
            yield return ("Observer les silhouettes", () => SimpleAction(20, "Vous suivez des yeux les silhouettes et les mouvements familiers."));
            yield return ("Écouter les voix", () => SimpleAction(20, "Les voix, les pas et les bruits de la maison composent peu à peu votre monde."));
            yield return ("Jouer / bouger", () => SimpleAction(30, "Vous expérimentez votre corps : mains, jambes, équilibre et coordination."));
            yield return ("Dormir", () => SimpleAction(180, "Vous vous endormez. La maison et le monde continuent de vivre."));
            yield break;
        }

        if (scene == LocalScene.Home)
        {
            yield return ("Observer le foyer", () => SimpleAction(30, "Vous observez les habitudes, travaux et petites conversations de votre foyer."));
            yield return ("Aller dans la cour", () => Visit(LocalScene.Courtyard, 10, "Vous rejoignez la cour de la résidence."));
            yield return ("Passer du temps en famille", () => SimpleAction(120, "Vous passez du temps auprès de votre famille. Les relations se construisent dans ces moments ordinaires."));
            yield return ("Dormir quelques heures", () => SimpleAction(240, "Vous vous reposez pendant que le reste du monde continue."));
            yield break;
        }

        if (scene == LocalScene.Courtyard)
        {
            yield return ("Retourner au foyer", () => Visit(LocalScene.Home, 10, "Vous retournez auprès de votre foyer."));
            yield return ("Observer la cour", () => SimpleAction(30, "Employés, parents, visiteurs ou disciples traversent les lieux selon leurs propres activités."));
            if (age >= 7) yield return ("Bibliothèque", () => Visit(LocalScene.Library, 10, "Vous entrez dans la bibliothèque accessible de votre famille."));
            else yield return ("Voir la bibliothèque", () => AddOnly("Vous savez qu'il existe un endroit rempli de livres, mais vous n'êtes pas encore autorisé à vous y rendre seul."));
            if (runtime.Districts.ForLocation(runtime.World.PlayerNpc!.CurrentLocationId).Count > 0)
                yield return ("Sortir vers la ville", () => OpenSettlement(age));
            if (age >= 7) yield return ("Travailler ses fondations", () => Execute("s'entraîner 1 jour"));
            yield break;
        }

        if (scene == LocalScene.Library)
        {
            yield return ("Lire les registres", ReadFamilyRegisters);
            yield return ("Chercher un texte sur le Qi", ReadQiBook);
            yield return ("Parcourir les manuels martiaux", BrowseManuals);
            yield return ("Retourner dans la cour", () => Visit(LocalScene.Courtyard, 10, "Vous quittez la bibliothèque."));
            yield break;
        }

        if (scene == LocalScene.Settlement)
        {
            foreach (var district in CurrentDistricts().Take(10))
            {
                var d = district;
                yield return (d.Name, () => EnterDistrict(d));
            }
            yield return ("Retourner à la résidence", () => Visit(LocalScene.Courtyard, 15, "Vous revenez vers votre résidence."));
            yield break;
        }

        if (scene == LocalScene.District)
        {
            var district = SelectedDistrict();
            if (district is not null)
            {
                foreach (var venue in district.Venues.Take(6))
                {
                    var v = venue;
                    yield return (v.Name, () => VisitVenue(v));
                }
            }
            yield return ("Observer le quartier", () => SimpleAction(30, "Vous prenez le temps d'observer les habitants, commerces et mouvements du quartier."));
            yield return ("Retour aux quartiers", () => Visit(LocalScene.Settlement, 10, "Vous retournez vers les artères principales."));
        }
    }

    private void VisitVenue(DistrictVenue venue)
    {
        var player = runtime.World.PlayerNpc!;
        if (!runtime.Districts.CanEnterVenue(player, venue, runtime.World.Clock))
        {
            AddOnly("Cet établissement ne vous est pas accessible à votre âge.");
            return;
        }
        runtime.AdvanceMinutes(20);
        AddStory($"Vous vous approchez de {venue.Name}. Le lieu existe réellement dans ce quartier ; ses interactions détaillées seront enrichies au fil des prochaines versions.");
        RefreshAll();
    }

    private void OpenSettlement(int age)
    {
        if (age < 10) { AddOnly("Vous êtes encore trop jeune pour parcourir seul les quartiers de la ville."); return; }
        scene = LocalScene.Settlement; selectedDistrictId = null; runtime.AdvanceMinutes(15); AddStory("Vous rejoignez les rues principales et regardez les quartiers que vous connaissez."); RefreshAll();
    }

    private void EnterDistrict(SettlementDistrict district)
    {
        scene = LocalScene.District; selectedDistrictId = district.Id; runtime.AdvanceMinutes(15); AddStory($"Vous vous rendez dans {district.Name}."); RefreshAll();
    }

    private void ReadFamilyRegisters()
    {
        var player = runtime.World.PlayerNpc!; runtime.AdvanceMinutes(60); player.Knowledge.Add("family:registers");
        AddStory("Vous parcourez des registres familiaux. Des noms, des alliances et des dates commencent à donner une forme au passé de votre lignée."); RefreshAll();
    }

    private void ReadQiBook()
    {
        var player = runtime.World.PlayerNpc!; runtime.AdvanceMinutes(90); player.Knowledge.Add("qi:vocabulary");
        AddStory("Le texte parle de respiration, de circulation et de dantian. Comprendre les mots ne signifie pas encore savoir sentir ni contrôler le Qi."); RefreshAll();
    }

    private void BrowseManuals()
    {
        var player = runtime.World.PlayerNpc!; runtime.AdvanceMinutes(75);
        var candidate = runtime.Content.Techniques.Where(t => t.Rarity is Rarity.Ordinary or Rarity.Uncommon).OrderBy(t => t.Id).Skip(Math.Abs(player.Id.GetHashCode()) % 100).FirstOrDefault();
        if (candidate is null) { AddOnly("Vous ne trouvez rien que vous puissiez identifier clairement."); return; }
        player.Knowledge.Add($"technique_seen:{candidate.Id}");
        AddStory($"Vous repérez un manuel intitulé « {candidate.Name} ». Vous savez désormais que cet art existe ; l'avoir vu ne signifie ni l'avoir compris ni l'avoir appris."); RefreshAll();
    }

    private void SubmitCommand()
    {
        Execute(command.Text);
        command.Clear();
    }

    private void Execute(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;
        var normalized = raw.Trim().ToLowerInvariant();
        string result;
        if (TryNavigate(normalized, out var nav)) result = nav;
        else result = commands.Execute(raw);
        AddStory($"> {raw}\n{result}");
        RefreshAll();
    }

    private bool TryNavigate(string text, out string result)
    {
        var age = runtime.World.PlayerNpc!.AgeYears(runtime.World.Clock);
        if (text.Contains("biblioth"))
        {
            if (age < 7) { result = "Vous êtes encore trop jeune pour vous y rendre seul."; return true; }
            scene = LocalScene.Library; selectedDistrictId = null; runtime.AdvanceMinutes(10); result = "Vous rejoignez la bibliothèque."; return true;
        }
        if (text is "foyer" or "maison" or "aller foyer") { scene = LocalScene.Home; selectedDistrictId = null; runtime.AdvanceMinutes(8); result = "Vous revenez au foyer."; return true; }
        if (text.Contains("cour") || text.Contains("résidence") || text.Contains("residence")) { scene = LocalScene.Courtyard; selectedDistrictId = null; runtime.AdvanceMinutes(10); result = "Vous rejoignez la cour."; return true; }
        if (text is "quartiers" or "ville" or "sortir" or "aller en ville")
        {
            if (age < 10) { result = "Vous êtes trop jeune pour parcourir seul la ville."; return true; }
            scene = LocalScene.Settlement; selectedDistrictId = null; runtime.AdvanceMinutes(15); result = "Vous rejoignez les quartiers de la ville."; return true;
        }
        var district = FindDistrict(text);
        if (district is not null)
        {
            if (age < 10) { result = "Vous êtes trop jeune pour vous rendre seul dans ce quartier."; return true; }
            scene = LocalScene.District; selectedDistrictId = district.Id; runtime.AdvanceMinutes(15); result = $"Vous vous rendez dans {district.Name}."; return true;
        }
        if (text == "retour")
        {
            scene = scene switch { LocalScene.District => LocalScene.Settlement, LocalScene.Settlement => LocalScene.Courtyard, LocalScene.Library => LocalScene.Courtyard, _ => LocalScene.Home };
            if (scene != LocalScene.District) selectedDistrictId = null;
            runtime.AdvanceMinutes(6); result = "Vous revenez sur vos pas."; return true;
        }
        result = string.Empty; return false;
    }

    private SettlementDistrict? FindDistrict(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return CurrentDistricts().FirstOrDefault(d => text.Contains(d.Name.ToLowerInvariant(), StringComparison.Ordinal))
            ?? CurrentDistricts().FirstOrDefault(d => d.Kind switch
            {
                DistrictKind.Smiths => words.Any(w => w.StartsWith("forger", StringComparison.Ordinal)),
                DistrictKind.Pleasure => words.Any(w => w.StartsWith("plais", StringComparison.Ordinal) || w.StartsWith("lanterne", StringComparison.Ordinal)),
                DistrictKind.CentralMarket => words.Any(w => w.StartsWith("march", StringComparison.Ordinal)),
                DistrictKind.Medicine => words.Any(w => w.StartsWith("médec", StringComparison.Ordinal) || w.StartsWith("medec", StringComparison.Ordinal)),
                DistrictKind.Docks => words.Any(w => w.StartsWith("dock", StringComparison.Ordinal) || w.StartsWith("port", StringComparison.Ordinal)),
                DistrictKind.Scholars => words.Any(w => w.StartsWith("lettr", StringComparison.Ordinal) || w.StartsWith("acad", StringComparison.Ordinal)),
                _ => false
            });
    }

    private IReadOnlyList<SettlementDistrict> CurrentDistricts()
        => runtime.Districts.ForLocation(runtime.World.PlayerNpc!.CurrentLocationId);

    private SettlementDistrict? SelectedDistrict()
        => selectedDistrictId is Guid id ? CurrentDistricts().FirstOrDefault(d => d.Id == id) : null;

    private void RefreshIllustration(Npc player, WorldLocation location)
    {
        Guid subjectId = location.Id;
        if (scene == LocalScene.District && selectedDistrictId is Guid district) subjectId = district;
        else if (scene is LocalScene.Home or LocalScene.Courtyard or LocalScene.Library)
        {
            InstitutionDomain? domain = player.HouseholdId is Guid householdId ? runtime.InstitutionDomains.ForHousehold(householdId) : null;
            if (domain is null && player.PrimaryFactionId is Guid factionId) domain = runtime.InstitutionDomains.ForFaction(factionId);
            if (domain is not null)
            {
                var kinds = scene switch
                {
                    LocalScene.Home => new[] { FacilityKind.Bedroom, FacilityKind.JuniorQuarters, FacilityKind.Dormitory, FacilityKind.CommonRoom, FacilityKind.MainRoom },
                    LocalScene.Courtyard => new[] { FacilityKind.Courtyard, FacilityKind.OuterDisciplesCourt, FacilityKind.TrainingYard },
                    LocalScene.Library => new[] { FacilityKind.Library, FacilityKind.ScripturePavilion, FacilityKind.MartialArchive },
                    _ => Array.Empty<FacilityKind>()
                };
                var facility = kinds.Select(k => domain.ActiveFacilities.FirstOrDefault(f => f.Kind == k)).FirstOrDefault(f => f is not null);
                if (facility is not null && runtime.Illustrations.Profiles.ContainsKey(facility.Id)) subjectId = facility.Id;
            }
        }
        var profile = runtime.Illustrations.For(subjectId);
        var season = Season(runtime.World.Clock.DayOfYear); var time = Time(runtime.World.Clock.MinuteOfDay);
        illustration.ShowPlace(profile, runtime.Illustrations.CandidateAssetPaths(subjectId, season, time, IllustrationWeather.Clear));
    }

    private string SceneName(int age, WorldLocation location)
    {
        if (age < 3 && scene == LocalScene.Home) return "Pièce familiale";
        return scene switch
        {
            LocalScene.Home => "Foyer familial",
            LocalScene.Courtyard => "Cour de la résidence",
            LocalScene.Library => "Bibliothèque familiale",
            LocalScene.Settlement => location.Name,
            LocalScene.District => SelectedDistrict()?.Name ?? "Quartier",
            _ => location.Name
        };
    }

    private string SceneSubtitle(WorldLocation location, int age)
    {
        if (age < 3) return $"{location.Name} · votre monde est encore petit, mais il vit autour de vous.";
        return scene == LocalScene.District && SelectedDistrict() is { } d
            ? $"{location.Name} · richesse {d.Wealth:P0} · sécurité perçue {d.Safety:P0}"
            : $"{location.Name} · {location.Region} · {location.Type}";
    }

    private void SimpleAction(int minutes, string text) { runtime.AdvanceMinutes(minutes); AddStory(text); RefreshAll(); }
    private void Visit(LocalScene target, int minutes, string text) { scene = target; selectedDistrictId = null; runtime.AdvanceMinutes(minutes); AddStory(text); RefreshAll(); }
    private void AddOnly(string text) { AddStory(text); RefreshAll(); }
    private void AddStory(string text) { story.Add($"[Jour {runtime.World.Clock.Day}] {text}"); }

    private PortraitGenome EnsurePortrait(Npc npc)
    {
        if (npc.ParentIds.Count >= 2 && runtime.World.Npcs.TryGetValue(npc.ParentIds[0], out var a) && runtime.World.Npcs.TryGetValue(npc.ParentIds[1], out var b))
        {
            portraits.GetOrCreate(a); portraits.GetOrCreate(b); return portraits.Inherit(npc, a, b);
        }
        return portraits.GetOrCreate(npc);
    }

    private static IllustrationSeason Season(int day) => day switch { <= 90 => IllustrationSeason.Spring, <= 181 => IllustrationSeason.Summer, <= 273 => IllustrationSeason.Autumn, _ => IllustrationSeason.Winter };
    private static IllustrationTime Time(int minute) => minute switch { < 360 => IllustrationTime.Night, < 540 => IllustrationTime.Dawn, < 1080 => IllustrationTime.Day, < 1260 => IllustrationTime.Dusk, _ => IllustrationTime.Night };
    private static string SeasonLabel(int day) => Season(day) switch { IllustrationSeason.Spring => "Printemps", IllustrationSeason.Summer => "Été", IllustrationSeason.Autumn => "Automne", _ => "Hiver" };
    private static string SexLabel(Sex sex) => sex switch { Sex.Female => "Femme", Sex.Male => "Homme", _ => "Intersexe" };

    private static PanelContainer Card(float width, out VBoxContainer box, int padding)
    {
        var card = new PanelContainer { CustomMinimumSize = new Vector2(width, 0), SizeFlagsHorizontal = width == 0 ? SizeFlags.ExpandFill : SizeFlags.ShrinkBegin, SizeFlagsVertical = SizeFlags.ExpandFill };
        card.AddThemeStyleboxOverride("panel", Rounded(Paper, Border, 15, 1));
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", padding); margin.AddThemeConstantOverride("margin_right", padding);
        margin.AddThemeConstantOverride("margin_top", padding); margin.AddThemeConstantOverride("margin_bottom", padding);
        card.AddChild(margin);
        box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 8); margin.AddChild(box);
        return card;
    }

    private static StyleBoxFlat Rounded(Color fill, Color border, int radius, int borderWidth)
    {
        var style = new StyleBoxFlat { BgColor = fill, BorderColor = border };
        style.SetCornerRadiusAll(radius); style.SetBorderWidthAll(borderWidth); return style;
    }

    private static Label Title(string value, int size, Color color)
    {
        var l = new Label { Text = value, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        l.AddThemeFontSizeOverride("font_size", size); l.AddThemeColorOverride("font_color", color); return l;
    }

    private static Label SectionHeader(string value)
    {
        var l = Title(value, 13, Accent); l.Uppercase = true; return l;
    }

    private static Label Text(string value, int size, Color color)
    {
        var l = new Label { Text = value, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        l.AddThemeFontSizeOverride("font_size", size); l.AddThemeColorOverride("font_color", color); return l;
    }

    private static HSeparator Divider()
    {
        var sep = new HSeparator(); sep.AddThemeColorOverride("separator", Border); return sep;
    }

    private static Button ActionButton(string text, Action action, bool accent = true)
    {
        var b = new Button { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 42) };
        b.AddThemeFontSizeOverride("font_size", 13);
        b.AddThemeColorOverride("font_color", Ink);
        var normal = Rounded(accent ? new Color("eee4d2") : PaperAlt, accent ? AccentSoft : Border, 9, 1);
        var hover = Rounded(new Color("e1d2b7"), Accent, 9, 1);
        var pressed = Rounded(new Color("d5c09a"), Accent, 9, 1);
        b.AddThemeStyleboxOverride("normal", normal); b.AddThemeStyleboxOverride("hover", hover); b.AddThemeStyleboxOverride("pressed", pressed);
        b.Pressed += action; return b;
    }

    private static void Clear(Node node)
    {
        foreach (var child in node.GetChildren()) child.QueueFree();
    }
}
