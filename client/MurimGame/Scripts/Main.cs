using Godot;
using Murim.Simulation;

namespace Murim.Game;

public partial class Main : Control
{
    private enum LocalScene { BirthRoom, FamilyRoom, ResidenceCourtyard, FamilyLibrary, SettlementOverview, District }

    private LivingWorldRuntime _runtime = null!;
    private PlayerCommandFacade _commands = null!;
    private PortraitGeneticsSystem _portraits = null!;
    private LocalScene _scene = LocalScene.BirthRoom;
    private Guid? _selectedDistrictId;

    private Label _identity = null!;
    private Label _portrait = null!;
    private Label _clock = null!;
    private Label _condition = null!;
    private Label _locationTitle = null!;
    private Label _locationSubtitle = null!;
    private WorldIllustrationView _illustration = null!;
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
        _portraits = new PortraitGeneticsSystem();
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
        outer.AddThemeConstantOverride("margin_left", 14); outer.AddThemeConstantOverride("margin_right", 14);
        outer.AddThemeConstantOverride("margin_top", 14); outer.AddThemeConstantOverride("margin_bottom", 14);
        AddChild(outer);

        var columns = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", 12);
        outer.AddChild(columns);
        columns.AddChild(BuildLeftPanel()); columns.AddChild(BuildCenterPanel()); columns.AddChild(BuildRightPanel());
    }

    private Control BuildLeftPanel()
    {
        var panel = MakePanel(300, out var box);
        box.AddChild(Heading("PERSONNAGE", 22));
        _portrait = BodyLabel(); _portrait.CustomMinimumSize = new Vector2(0, 150);
        _portrait.HorizontalAlignment = HorizontalAlignment.Center; _portrait.VerticalAlignment = VerticalAlignment.Center;
        box.AddChild(_portrait);
        _identity = BodyLabel(); box.AddChild(_identity);
        box.AddChild(Heading("ÉTAT OBSERVÉ", 16)); _condition = BodyLabel(); box.AddChild(_condition);
        box.AddChild(Heading("TEMPS", 16)); _clock = BodyLabel(); box.AddChild(_clock);
        box.AddChild(Heading("ACTIONS RAPIDES", 16));
        box.AddChild(ActionButton("Attendre 1 jour", () => ExecuteCommand("attendre 1 jour")));
        box.AddChild(ActionButton("Vivre 1 mois", () => ExecuteCommand("attendre 1 mois")));
        box.AddChild(ActionButton("Observer son état", () => ExecuteCommand("statut")));
        return panel;
    }

    private Control BuildCenterPanel()
    {
        var panel = MakePanel(0, out var box); panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _locationTitle = Heading("LIEU", 24); box.AddChild(_locationTitle);
        _locationSubtitle = BodyLabel(); box.AddChild(_locationSubtitle);

        _illustration = new WorldIllustrationView { CustomMinimumSize = new Vector2(0, 300), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        box.AddChild(_illustration);
        box.AddChild(Heading("ZONES ET ACTIONS", 15));
        var hotspotScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 180), SizeFlagsVertical = SizeFlags.ExpandFill };
        _hotspots = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _hotspots.AddThemeConstantOverride("separation", 8); hotspotScroll.AddChild(_hotspots); box.AddChild(hotspotScroll);

        box.AddChild(Heading("CHRONIQUE PERSONNELLE", 16));
        _story = new RichTextLabel { BbcodeEnabled = false, FitContent = false, CustomMinimumSize = new Vector2(0, 190), SizeFlagsVertical = SizeFlags.ExpandFill, ScrollActive = true };
        box.AddChild(_story);

        var commandRow = new HBoxContainer();
        _command = new LineEdit { PlaceholderText = "Commande : quartiers, forgerons, plaisirs, bibliothèque, attendre 1 semaine…", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _command.TextSubmitted += OnCommandSubmitted; commandRow.AddChild(_command); commandRow.AddChild(ActionButton("Exécuter", SubmitCommand)); box.AddChild(commandRow);
        return panel;
    }

    private Control BuildRightPanel()
    {
        var panel = MakePanel(340, out var box);
        box.AddChild(Heading("MONDE PROCHE", 20));
        var nearbyScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 310), SizeFlagsVertical = SizeFlags.ExpandFill };
        _nearby = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; nearbyScroll.AddChild(_nearby); box.AddChild(nearbyScroll);
        box.AddChild(Heading("CE QUE VOUS SAVEZ", 16));
        var knowledgeScroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 200), SizeFlagsVertical = SizeFlags.ExpandFill };
        _knowledge = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; knowledgeScroll.AddChild(_knowledge); box.AddChild(knowledgeScroll);
        return panel;
    }

    private static PanelContainer MakePanel(float minimumWidth, out VBoxContainer box)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(minimumWidth, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
        var style = new StyleBoxFlat { BgColor = new Color("101720"), BorderColor = new Color("293443") };
        style.SetBorderWidthAll(1); style.SetCornerRadiusAll(10); panel.AddThemeStyleboxOverride("panel", style);
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14); margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 14); margin.AddThemeConstantOverride("margin_bottom", 14); panel.AddChild(margin);
        box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 8); margin.AddChild(box); return panel;
    }

    private static Label Heading(string text, int size)
    {
        var label = new Label { Text = text }; label.AddThemeFontSizeOverride("font_size", size); label.Modulate = new Color("e4cf9a"); return label;
    }

    private static Label BodyLabel() => new() { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };

    private static Button ActionButton(string text, Action action)
    {
        var button = new Button { Text = text, SizeFlagsHorizontal = SizeFlags.ExpandFill }; button.Pressed += action; return button;
    }

    private void OnCommandSubmitted(string text) => ExecuteCommand(text);
    private void SubmitCommand() => ExecuteCommand(_command.Text);

    private void ExecuteCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;
        var normalized = command.Trim().ToLowerInvariant();
        string result;
        if (TryNavigation(normalized, out var navigationResult)) result = navigationResult; else result = _commands.Execute(command);
        AddStory($"> {command}\n{result}"); _command.Clear(); RefreshAll();
    }

    private bool TryNavigation(string command, out string result)
    {
        var player = _runtime.World.PlayerNpc!; var age = player.AgeYears(_runtime.World.Clock);
        if (command is "foyer" or "aller foyer" or "maison")
        {
            _scene = LocalScene.FamilyRoom; _selectedDistrictId = null; _runtime.AdvanceMinutes(5); result = "Vous revenez auprès de votre foyer."; return true;
        }
        if (command.Contains("résidence") || command.Contains("residence") || command == "cour")
        {
            if (age < 3) { result = "Vous êtes trop jeune pour vous déplacer seul jusque-là."; return true; }
            _scene = LocalScene.ResidenceCourtyard; _selectedDistrictId = null; _runtime.AdvanceMinutes(10); result = "Vous gagnez la cour de la résidence."; return true;
        }
        if (command.Contains("bibliothèque") || command.Contains("bibliotheque"))
        {
            if (age < 7) { result = "Vous connaissez peut-être ce mot, mais vous êtes encore trop jeune pour vous y rendre seul."; return true; }
            _scene = LocalScene.FamilyLibrary; _selectedDistrictId = null; _runtime.AdvanceMinutes(10); result = "Vous entrez dans la bibliothèque familiale. Les ouvrages accessibles dépendent de ce que votre famille vous autorise à consulter."; return true;
        }
        if (command is "quartiers" or "ville" or "sortir" or "aller en ville")
        {
            if (!CanExploreSettlement(age, out result)) return true;
            _scene = LocalScene.SettlementOverview; _selectedDistrictId = null; _runtime.AdvanceMinutes(15); result = "Vous rejoignez les rues principales et considérez les quartiers que vous connaissez."; return true;
        }

        var district = FindDistrictFromCommand(command);
        if (district is not null)
        {
            if (!CanExploreSettlement(age, out result)) return true;
            _selectedDistrictId = district.Id; _scene = LocalScene.District; _runtime.AdvanceMinutes(15); result = $"Vous vous rendez dans {district.Name}."; return true;
        }

        if (command == "retour")
        {
            if (_scene == LocalScene.District) { _scene = LocalScene.SettlementOverview; _selectedDistrictId = null; result = "Vous revenez vers les artères principales."; }
            else if (_scene == LocalScene.SettlementOverview) { _scene = LocalScene.ResidenceCourtyard; result = "Vous retournez vers votre résidence."; }
            else { _scene = _scene == LocalScene.FamilyLibrary ? LocalScene.ResidenceCourtyard : LocalScene.FamilyRoom; result = "Vous revenez sur vos pas."; }
            _runtime.AdvanceMinutes(5); return true;
        }
        result = string.Empty; return false;
    }

    private bool CanExploreSettlement(int age, out string result)
    {
        var player = _runtime.World.PlayerNpc!;
        var districts = _runtime.Districts.ForLocation(player.CurrentLocationId);
        if (districts.Count == 0) { result = "Cet endroit ne possède pas de quartiers urbains structurés."; return false; }
        if (age < 10) { result = "Vous êtes encore trop jeune pour parcourir seul les quartiers. Une future action permettra de demander à un adulte de vous accompagner."; return false; }
        result = string.Empty; return true;
    }

    private SettlementDistrict? FindDistrictFromCommand(string command)
    {
        var player = _runtime.World.PlayerNpc!;
        var districts = _runtime.Districts.ForLocation(player.CurrentLocationId);
        string[] words = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        SettlementDistrict? direct = districts.FirstOrDefault(d => command.Contains(d.Name.ToLowerInvariant(), StringComparison.Ordinal));
        if (direct is not null) return direct;
        return districts.FirstOrDefault(d => d.Kind switch
        {
            DistrictKind.Smiths => words.Any(w => w.StartsWith("forger", StringComparison.Ordinal)),
            DistrictKind.Pleasure => words.Any(w => w.StartsWith("plais", StringComparison.Ordinal) || w.StartsWith("lanterne", StringComparison.Ordinal)),
            DistrictKind.CentralMarket => words.Any(w => w.StartsWith("march", StringComparison.Ordinal)),
            DistrictKind.Medicine => words.Any(w => w.StartsWith("médec", StringComparison.Ordinal) || w.StartsWith("medec", StringComparison.Ordinal)),
            DistrictKind.Docks => words.Any(w => w.StartsWith("dock", StringComparison.Ordinal) || w.StartsWith("port", StringComparison.Ordinal)),
            DistrictKind.Scholars => words.Any(w => w.StartsWith("lettr", StringComparison.Ordinal) || w.StartsWith("acad", StringComparison.Ordinal)),
            DistrictKind.Underworld => words.Any(w => w.StartsWith("clandest", StringComparison.Ordinal) || w.StartsWith("ruelle", StringComparison.Ordinal)),
            _ => false
        });
    }

    private void RefreshAll()
    {
        var world = _runtime.World; var player = world.PlayerNpc!; var location = world.Locations[player.CurrentLocationId]; var age = player.AgeYears(world.Clock);
        _portraits.UpdateVisibleAge(player, world.Clock); _portraits.SyncPermanentMarks(player);
        var genome = _portraits.GetOrCreate(player); var appearance = _portraits.AppearanceFor(player);
        _portrait.Text = $"PORTRAIT PARAMÉTRIQUE\nvisage #{Math.Abs(player.Id.GetHashCode()):X6}\nâge apparent {appearance.ApparentAge:0.#} ans\ntraits héréditaires actifs\nvisage {genome.FaceWidth:0.00} · mâchoire {genome.JawWidth:0.00} · yeux {genome.EyeSize:0.00}";
        _identity.Text = $"{player.Identity.DisplayName}\n{age} an(s)\nOrigine : {player.Identity.SocialOrigin}";
        _clock.Text = $"An {world.Clock.Year}, jour {world.Clock.DayOfYear}\nJour absolu : {world.Clock.Day}\nHeure : {world.Clock.MinuteOfDay / 60:00}:{world.Clock.MinuteOfDay % 60:00}";
        _condition.Text = string.Join("\n", _runtime.PlayerConditionLines());
        _locationTitle.Text = SceneName(_scene, location, age); _locationSubtitle.Text = SceneSubtitle(location);
        RefreshIllustration(player, location); RefreshHotspots(age); RefreshNearby(player, location); RefreshKnowledge(player, location);
        _story.Text = string.Join("\n\n", _chronicle.TakeLast(50));
    }

    private void RefreshIllustration(Npc player, WorldLocation location)
    {
        Guid subjectId = location.Id;
        if (_scene == LocalScene.District && _selectedDistrictId is Guid districtId) subjectId = districtId;
        else if (_scene is LocalScene.BirthRoom or LocalScene.FamilyRoom or LocalScene.ResidenceCourtyard or LocalScene.FamilyLibrary)
        {
            InstitutionDomain? domain = player.HouseholdId is Guid householdId ? _runtime.InstitutionDomains.ForHousehold(householdId) : null;
            if (domain is null && player.PrimaryFactionId is Guid factionId) domain = _runtime.InstitutionDomains.ForFaction(factionId);
            if (domain is not null)
            {
                var preferredKinds = _scene switch
                {
                    LocalScene.BirthRoom => new[] { FacilityKind.Bedroom, FacilityKind.JuniorQuarters, FacilityKind.Dormitory, FacilityKind.CommonRoom },
                    LocalScene.FamilyRoom => new[] { FacilityKind.CommonRoom, FacilityKind.MainRoom, FacilityKind.MainHall, FacilityKind.Dormitory },
                    LocalScene.ResidenceCourtyard => new[] { FacilityKind.Courtyard, FacilityKind.OuterDisciplesCourt, FacilityKind.TrainingYard },
                    LocalScene.FamilyLibrary => new[] { FacilityKind.Library, FacilityKind.ScripturePavilion, FacilityKind.MartialArchive },
                    _ => Array.Empty<FacilityKind>()
                };
                var facility = preferredKinds.Select(kind => domain.ActiveFacilities.FirstOrDefault(f => f.Kind == kind)).FirstOrDefault(f => f is not null);
                if (facility is not null && _runtime.Illustrations.Profiles.ContainsKey(facility.Id)) subjectId = facility.Id;
            }
        }
        var profile = _runtime.Illustrations.For(subjectId);
        var season = SeasonFor(_runtime.World.Clock.DayOfYear); var time = TimeFor(_runtime.World.Clock.MinuteOfDay);
        var weather = season == IllustrationSeason.Winter && location.Tags.Contains("snow") ? IllustrationWeather.Snow : IllustrationWeather.Clear;
        _illustration.ShowPlace(profile, _runtime.Illustrations.CandidateAssetPaths(subjectId, season, time, weather));
    }

    private static IllustrationSeason SeasonFor(int day) => day switch { <= 90 => IllustrationSeason.Spring, <= 181 => IllustrationSeason.Summer, <= 273 => IllustrationSeason.Autumn, _ => IllustrationSeason.Winter };
    private static IllustrationTime TimeFor(int minute) => minute switch { < 360 => IllustrationTime.Night, < 540 => IllustrationTime.Dawn, < 1080 => IllustrationTime.Day, < 1260 => IllustrationTime.Dusk, _ => IllustrationTime.Night };

    private void RefreshHotspots(int age)
    {
        ClearChildren(_hotspots);
        foreach (var (label, action) in SceneActions(age)) { var button = ActionButton(label, action); button.CustomMinimumSize = new Vector2(0, 40); _hotspots.AddChild(button); }
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
            yield return ("Observer la cour", () => { _runtime.AdvanceMinutes(30); AddStory("Parents, visiteurs, employés ou disciples traversent les lieux selon leurs propres routines."); RefreshAll(); });
            if (age >= 7) yield return ("Bibliothèque", () => Visit(LocalScene.FamilyLibrary, 10, "Vous vous dirigez vers la bibliothèque accessible de votre environnement."));
            else yield return ("Bibliothèque — pas encore accessible seul", () => AddStoryAndRefresh("Vous savez qu'un endroit contient des livres, mais les adultes ne vous laissent pas encore y aller seul."));
            if (_runtime.Districts.ForLocation(_runtime.World.PlayerNpc!.CurrentLocationId).Count > 0)
                yield return ("Sortir vers les quartiers", () => OpenSettlement(age));
            yield break;
        }

        if (_scene == LocalScene.FamilyLibrary)
        {
            yield return ("Lire les registres familiaux", ReadFamilyRegister); yield return ("Chercher un livre sur le Qi", ReadQiIntroduction);
            yield return ("Chercher des manuels martiaux", SearchMartialManuals); yield return ("Retourner dans la cour", () => Visit(LocalScene.ResidenceCourtyard, 10, "Vous quittez la bibliothèque.")); yield break;
        }

        if (_scene == LocalScene.SettlementOverview)
        {
            yield return ("Retourner à la résidence", () => Visit(LocalScene.ResidenceCourtyard, 15, "Vous quittez les rues principales et retournez vers votre résidence."));
            foreach (var district in _runtime.Districts.ForLocation(_runtime.World.PlayerNpc!.CurrentLocationId))
            {
                var copy = district;
                yield return ($"{copy.Name}", () => EnterDistrict(copy));
            }
            yield break;
        }

        if (_scene == LocalScene.District && CurrentDistrict() is SettlementDistrict current)
        {
            yield return ("Retour à la vue des quartiers", () => { _scene = LocalScene.SettlementOverview; _selectedDistrictId = null; _runtime.AdvanceMinutes(8); AddStory("Vous revenez vers une artère reliant plusieurs quartiers."); RefreshAll(); });
            foreach (var venue in current.Venues)
            {
                var copy = venue;
                var suffix = copy.AdultOnly ? " — réservé aux adultes" : string.Empty;
                yield return ($"{copy.Name}{suffix}", () => EnterVenue(current, copy));
            }
        }
    }

    private void OpenSettlement(int age)
    {
        if (!CanExploreSettlement(age, out var reason)) { AddStoryAndRefresh(reason); return; }
        _scene = LocalScene.SettlementOverview; _selectedDistrictId = null; _runtime.AdvanceMinutes(15); AddStory("Vous rejoignez les rues et chemins reliant les différents quartiers."); RefreshAll();
    }

    private void EnterDistrict(SettlementDistrict district)
    {
        _selectedDistrictId = district.Id; _scene = LocalScene.District; _runtime.AdvanceMinutes(15); AddStory($"Vous entrez dans {district.Name}. L'ambiance, les métiers, les odeurs et la foule diffèrent des autres parties de la ville."); RefreshAll();
    }

    private void EnterVenue(SettlementDistrict district, DistrictVenue venue)
    {
        var player = _runtime.World.PlayerNpc!;
        if (!_runtime.Districts.CanEnterVenue(player, venue, _runtime.World.Clock)) { AddStoryAndRefresh("Cet établissement est réservé aux adultes ; on vous refuse l'entrée."); return; }
        _runtime.AdvanceMinutes(10); player.Knowledge.Add($"Vous connaissez l'existence de {venue.Name} dans {district.Name}.");
        AddStory($"Vous vous approchez de {venue.Name}. Cette première version du client représente déjà son emplacement réel ; ses interactions détaillées seront ajoutées avec les systèmes de métiers, commerces et conversations."); RefreshAll();
    }

    private SettlementDistrict? CurrentDistrict()
        => _selectedDistrictId is Guid id ? _runtime.Districts.All.FirstOrDefault(d => d.Id == id) : null;

    private void InfantAction(string text)
    {
        _runtime.AdvanceMinutes(30); var player = _runtime.World.PlayerNpc!;
        player.Skills["observation"] = Math.Clamp(player.Skills.GetValueOrDefault("observation") + .03, 0, 100);
        player.Skills["language"] = Math.Clamp(player.Skills.GetValueOrDefault("language") + .015, 0, 100); AddStory(text); RefreshAll();
    }

    private void Visit(LocalScene scene, int minutes, string text)
    {
        _scene = scene; _selectedDistrictId = null; _runtime.AdvanceMinutes(minutes); AddStory(text); RefreshAll();
    }

    private void ReadFamilyRegister()
    {
        var player = _runtime.World.PlayerNpc!;
        if (player.AgeYears(_runtime.World.Clock) < 7) { AddStoryAndRefresh("Les caractères sont encore trop difficiles à lire seul."); return; }
        _runtime.AdvanceMinutes(45); player.Knowledge.Add("Vous avez commencé à reconnaître certains noms et branches de votre famille dans les registres.");
        player.Skills["literacy"] = Math.Clamp(player.Skills.GetValueOrDefault("literacy") + .15, 0, 100);
        AddStory("Vous passez du temps sur les registres familiaux. Beaucoup de passages restent obscurs, mais quelques noms commencent à avoir un sens."); RefreshAll();
    }

    private void ReadQiIntroduction()
    {
        var player = _runtime.World.PlayerNpc!;
        if (player.AgeYears(_runtime.World.Clock) < 8) { AddStoryAndRefresh("Le texte sur la respiration et le Qi est encore trop abstrait pour vous."); return; }
        _runtime.AdvanceMinutes(60); player.Knowledge.Add("Vous avez lu une introduction élémentaire au vocabulaire du Qi. Cela ne révèle pas automatiquement votre propre constitution.");
        player.Skills["literacy"] = Math.Clamp(player.Skills.GetValueOrDefault("literacy") + .18, 0, 100);
        AddStory("Le livre décrit souffle, posture et vocabulaire du Qi. Vous comprenez des mots, pas encore la vérité complète de votre propre corps."); RefreshAll();
    }

    private void SearchMartialManuals()
    {
        var player = _runtime.World.PlayerNpc!; _runtime.AdvanceMinutes(40); var age = player.AgeYears(_runtime.World.Clock);
        if (age < 10) { AddStory("Vous repérez des ouvrages gardés hors de portée. Savoir qu'ils existent ne signifie ni les comprendre ni avoir la permission de les lire."); RefreshAll(); return; }
        var ordinary = _runtime.Content.Techniques.First(t => t.Rarity == Rarity.Ordinary && (t.Domain is TechniqueDomain.Fist or TechniqueDomain.Palm or TechniqueDomain.InternalCultivation));
        player.Knowledge.Add($"Vous avez aperçu le titre d'un manuel : {ordinary.Name}.");
        AddStory($"Vous trouvez la trace du manuel « {ordinary.Name} ». Pour l'instant, vous n'en connaissez que l'existence ; il faudra obtenir l'accès, le lire et le comprendre avant de pouvoir l'apprendre."); RefreshAll();
    }

    private void AddStoryAndRefresh(string text) { AddStory(text); RefreshAll(); }

    private string SceneName(LocalScene scene, WorldLocation location, int age) => scene switch
    {
        LocalScene.BirthRoom => age < 1 ? "Pièce aux formes encore inconnues" : "Pièce familiale",
        LocalScene.FamilyRoom => "Foyer familial", LocalScene.ResidenceCourtyard => "Cour de la résidence", LocalScene.FamilyLibrary => "Bibliothèque",
        LocalScene.SettlementOverview => location.Name, LocalScene.District => CurrentDistrict()?.Name ?? location.Name, _ => location.Name
    };

    private string SceneSubtitle(WorldLocation location)
    {
        var district = CurrentDistrict();
        return district is null ? $"{location.Name} · {location.Region} · {location.Type}" : $"{location.Name} · {district.Kind} · sécurité locale estimée {district.Safety:0.00}";
    }

    private void RefreshNearby(Npc player, WorldLocation location)
    {
        ClearChildren(_nearby);
        var present = _runtime.World.Npcs.Values.Where(n => n.IsAlive && n.Id != player.Id && n.CurrentLocationId == player.CurrentLocationId).Take(8).ToArray();
        if (present.Length == 0) _nearby.AddChild(TextCard("Personne que vous remarquez pour l'instant."));
        foreach (var npc in present)
        {
            var relation = player.Relationships.GetValueOrDefault(npc.Id); var knownName = relation is not null && relation.Familiarity >= 15;
            var label = knownName ? npc.Identity.DisplayName : "Personne inconnue"; _portraits.GetOrCreate(npc); _portraits.UpdateVisibleAge(npc, _runtime.World.Clock);
            var apparentAge = _portraits.AppearanceFor(npc).ApparentAge; var activity = _runtime.Routines.CurrentActivity(npc, _runtime.World.Clock);
            _nearby.AddChild(TextCard($"{label}\nâge apparent ~{apparentAge:0} · {activity}"));
        }
        var visibleEvents = _runtime.World.Events.Where(e => e.Day <= _runtime.World.Clock.Day && e.LocationId == location.Id && (e.DirectWitnessNpcIds.Contains(player.Id) || e.Publicity >= .75)).OrderByDescending(e => e.Day).Take(4).ToArray();
        foreach (var ev in visibleEvents) _nearby.AddChild(TextCard($"Jour {ev.Day} — {ev.Summary}"));
    }

    private void RefreshKnowledge(Npc player, WorldLocation location)
    {
        ClearChildren(_knowledge); _knowledge.AddChild(TextCard($"Lieu actuel : {location.Name}"));
        if (CurrentDistrict() is SettlementDistrict district) _knowledge.AddChild(TextCard($"Quartier actuel : {district.Name}"));
        foreach (var entry in player.Knowledge.Take(8)) _knowledge.AddChild(TextCard(entry));
        if (player.Knowledge.Count == 0) _knowledge.AddChild(TextCard("Vous ne connaissez presque rien du vaste Murim."));
        var qiText = player.AgeYears(_runtime.World.Clock) < 10 && player.Physiology.Spirit.QiSensitivity < 45 ? "Qi : vous ne savez pas encore évaluer votre propre constitution." : $"Qi perçu : {player.Physiology.Qi.MutationName}";
        _knowledge.AddChild(TextCard(qiText));
    }

    private static Label TextCard(string text) { var label = BodyLabel(); label.Text = text; label.CustomMinimumSize = new Vector2(0, 42); return label; }

    private void AddStory(string text)
    {
        _chronicle.Add($"[Jour {_runtime.World.Clock.Day}] {text}"); if (_chronicle.Count > 250) _chronicle.RemoveRange(0, _chronicle.Count - 250);
    }

    private static void ClearChildren(Node node) { foreach (var child in node.GetChildren()) child.QueueFree(); }
}
