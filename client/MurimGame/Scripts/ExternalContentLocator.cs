using Godot;

namespace Murim.Game;

/// <summary>
/// Resolves optional content that lives next to the exported game instead of inside Murim-Beta.pck.
/// This keeps the executable/runtime stable while allowing the art library to grow independently.
/// </summary>
public static class ExternalContentLocator
{
    public static string GameRoot
    {
        get
        {
            if (OS.HasFeature("editor"))
                return ProjectSettings.GlobalizePath("res://");

            var executable = OS.GetExecutablePath();
            var root = Path.GetDirectoryName(executable);
            return string.IsNullOrWhiteSpace(root) ? "." : root;
        }
    }

    public static string ContentRoot => OS.HasFeature("editor")
        ? Path.Combine(GameRoot, "ExternalContent")
        : Path.Combine(GameRoot, "Content");

    public static string LogsRoot => OS.HasFeature("editor")
        ? ProjectSettings.GlobalizePath("user://logs")
        : Path.Combine(GameRoot, "Logs");

    public static string CacheRoot => ProjectSettings.GlobalizePath("user://cache");

    public static IEnumerable<string> ExternalCandidates(IEnumerable<string> resourceCandidates)
    {
        foreach (var resourcePath in resourceCandidates)
        {
            if (string.IsNullOrWhiteSpace(resourcePath)) continue;

            var normalized = resourcePath.Replace('\\', '/');
            if (normalized.StartsWith("res://Assets/", StringComparison.OrdinalIgnoreCase))
            {
                var relative = normalized["res://Assets/".Length..]
                    .Replace('/', Path.DirectorySeparatorChar);
                yield return Path.Combine(ContentRoot, "Images", relative);
            }
        }
    }

    public static void EnsureWritableFolders()
    {
        TryCreateDirectory(LogsRoot);
        TryCreateDirectory(CacheRoot);
    }

    private static void TryCreateDirectory(string path)
    {
        try { Directory.CreateDirectory(path); }
        catch { /* A read-only install must not prevent the game from starting. */ }
    }
}
