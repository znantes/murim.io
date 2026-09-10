using Godot;
using Murim.Simulation;

namespace Murim.Game;

/// <summary>
/// Location image surface. Exact external Content/Images artwork wins, then imported exact resources,
/// then a polished core illustration template selected from the place identity. Missing final artwork
/// therefore never falls back to a black debug box.
/// </summary>
public partial class WorldIllustrationView : PanelContainer
{
    private readonly TextureRect image = new()
    {
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
        SizeFlagsVertical = SizeFlags.ExpandFill,
        MouseFilter = MouseFilterEnum.Ignore
    };

    private readonly Label fallback = new()
    {
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
        SizeFlagsVertical = SizeFlags.ExpandFill,
        MouseFilter = MouseFilterEnum.Ignore
    };

    private readonly Label caption = new()
    {
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Center,
        MouseFilter = MouseFilterEnum.Ignore
    };

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 330);
        var style = new StyleBoxFlat { BgColor = new Color("d8d2c4"), BorderColor = new Color("d1c5ae") };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(16);
        AddThemeStyleboxOverride("panel", style);

        var stack = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, ClipContents = true };
        AddChild(stack);
        image.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        fallback.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        stack.AddChild(image);
        stack.AddChild(fallback);

        var captionPanel = new PanelContainer();
        captionPanel.SetAnchorsPreset(LayoutPreset.BottomWide);
        captionPanel.OffsetTop = -54;
        captionPanel.OffsetBottom = -10;
        captionPanel.OffsetLeft = 12;
        captionPanel.OffsetRight = -12;
        var captionStyle = new StyleBoxFlat { BgColor = new Color(0.08f, .10f, .12f, .72f) };
        captionStyle.SetCornerRadiusAll(10);
        captionPanel.AddThemeStyleboxOverride("panel", captionStyle);
        captionPanel.MouseFilter = MouseFilterEnum.Ignore;
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        captionPanel.AddChild(margin);
        caption.AddThemeColorOverride("font_color", new Color("f5efe2"));
        caption.AddThemeFontSizeOverride("font_size", 15);
        margin.AddChild(caption);
        stack.AddChild(captionPanel);

        ExternalContentLocator.EnsureWritableFolders();
    }

    public bool ShowPlace(PlaceIllustrationProfile profile, IReadOnlyList<string> candidatePaths)
    {
        caption.Text = $"{profile.Name}   ·   {profile.Region}";

        foreach (var externalPath in ExternalContentLocator.ExternalCandidates(candidatePaths))
        {
            if (!File.Exists(externalPath)) continue;
            try
            {
                var loaded = Image.LoadFromFile(externalPath);
                if (loaded is null || loaded.IsEmpty()) continue;
                SetTexture(ImageTexture.CreateFromImage(loaded), $"{profile.Name}\n{externalPath}");
                return true;
            }
            catch
            {
                // Optional external art must never prevent the simulation from starting.
            }
        }

        foreach (var path in candidatePaths)
        {
            if (!ResourceLoader.Exists(path)) continue;
            var texture = GD.Load<Texture2D>(path);
            if (texture is null) continue;
            SetTexture(texture, $"{profile.Name}\n{path}");
            return true;
        }

        var coreTemplate = CoreTemplateFor(profile);
        if (ResourceLoader.Exists(coreTemplate))
        {
            var texture = GD.Load<Texture2D>(coreTemplate);
            if (texture is not null)
            {
                SetTexture(texture, $"Illustration bêta · {profile.AssetFamily}\nLe fichier final pourra être remplacé depuis Content/Images.");
                return true;
            }
        }

        image.Texture = null;
        image.Visible = false;
        fallback.Visible = true;
        fallback.AddThemeColorOverride("font_color", new Color("514b43"));
        fallback.AddThemeFontSizeOverride("font_size", 16);
        var seed = unchecked((uint)profile.VisualSeed);
        fallback.Text = $"{profile.Name}\n\nIllustration en préparation\nIdentité visuelle #{seed:X8}";
        return false;
    }

    private void SetTexture(Texture2D texture, string tooltip)
    {
        image.Texture = texture;
        image.Visible = true;
        fallback.Visible = false;
        TooltipText = tooltip;
    }

    private static string CoreTemplateFor(PlaceIllustrationProfile profile)
    {
        var value = $"{profile.AssetFamily} {profile.Name} {string.Join(' ', profile.Tags)}".ToLowerInvariant();
        if (value.Contains("library") || value.Contains("archive") || value.Contains("scripture") || value.Contains("biblioth")) return "res://Assets/World/Templates/library.svg";
        if (value.Contains("courtyard") || value.Contains("training") || value.Contains("cour") || value.Contains("yard")) return "res://Assets/World/Templates/courtyard.svg";
        if (value.Contains("smith") || value.Contains("forge") || value.Contains("forger")) return "res://Assets/World/Templates/smiths.svg";
        if (value.Contains("pleasure") || value.Contains("lanterne") || value.Contains("entertainment")) return "res://Assets/World/Templates/lantern_district.svg";
        if (value.Contains("market") || value.Contains("city") || value.Contains("ville") || value.Contains("district") || value.Contains("quartier")) return "res://Assets/World/Templates/city.svg";
        if (value.Contains("bedroom") || value.Contains("dormitory") || value.Contains("commonroom") || value.Contains("mainroom") || value.Contains("pièce") || value.Contains("piece")) return "res://Assets/World/Templates/family_room.svg";
        return "res://Assets/World/Templates/landscape.svg";
    }
}
