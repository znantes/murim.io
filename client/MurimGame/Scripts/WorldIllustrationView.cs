using Godot;
using Murim.Simulation;

namespace Murim.Game;

/// <summary>
/// Reusable image surface for locations, districts and facilities. External Content/Images files are
/// preferred in exported builds so the art library can grow without rebuilding the executable. Imported
/// res:// assets remain a fallback for editor/dev usage.
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
        ExternalContentLocator.EnsureWritableFolders();
    }

    public bool ShowPlace(PlaceIllustrationProfile profile, IReadOnlyList<string> candidatePaths)
    {
        // External artwork is intentionally checked first. This allows artists to add/replace imagery
        // in Content/Images without touching Murim-Beta.exe or Murim-Beta.pck.
        foreach (var externalPath in ExternalContentLocator.ExternalCandidates(candidatePaths))
        {
            if (!File.Exists(externalPath)) continue;
            try
            {
                var loadedImage = Image.LoadFromFile(externalPath);
                if (loadedImage is null || loadedImage.IsEmpty()) continue;
                image.Texture = ImageTexture.CreateFromImage(loadedImage);
                image.Visible = true;
                fallback.Visible = false;
                TooltipText = $"{profile.Name}\n{externalPath}";
                return true;
            }
            catch
            {
                // A corrupt optional artwork file must never prevent the simulation from starting.
            }
        }

        // Development fallback: imported Godot resources can still be bundled for essential/core art.
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
        var visualSeed = unchecked((uint)profile.VisualSeed);
        var firstExternal = ExternalContentLocator.ExternalCandidates(candidatePaths).LastOrDefault()
            ?? Path.Combine(ExternalContentLocator.ContentRoot, "Images", "World", "_missing", "default.webp");
        fallback.Text = $"ILLUSTRATION À PRODUIRE\n\n{profile.Name}\n{profile.SceneDescription}\n\nIdentité visuelle #{visualSeed:X8}";
        TooltipText = $"Illustration externe attendue :\n{firstExternal}";
        return false;
    }
}
