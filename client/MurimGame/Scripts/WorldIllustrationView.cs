using Godot;
using Murim.Simulation;

namespace Murim.Game;

/// <summary>
/// Reusable image surface for locations, districts and facilities. It tries the most specific visual
/// variant first and gracefully displays the place identity when artwork has not been produced yet.
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

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 260);
        var stack = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        AddChild(stack);
        image.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        fallback.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        stack.AddChild(image);
        stack.AddChild(fallback);
    }

    public bool ShowPlace(PlaceIllustrationProfile profile, IReadOnlyList<string> candidatePaths)
    {
        foreach (var path in candidatePaths)
        {
            if (!ResourceLoader.Exists(path)) continue;
            var texture = GD.Load<Texture2D>(path);
            if (texture is null) continue;
            image.Texture = texture;
            image.Visible = true;
            fallback.Visible = false;
            TooltipText = $"{profile.Name}\n{path}";
            return true;
        }

        image.Texture = null;
        image.Visible = false;
        fallback.Visible = true;
        fallback.Text = $"ILLUSTRATION À PRODUIRE\n\n{profile.Name}\n{profile.SceneDescription}\n\nIdentité visuelle #{Math.Abs(profile.VisualSeed):X8}";
        TooltipText = $"Déposer une image dans :\n{profile.AssetDirectory}/default.webp";
        return false;
    }
}
