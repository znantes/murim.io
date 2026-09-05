namespace Murim.World;

public sealed class SaveGameManifest
{
    public const int CurrentVersion = 1;
    public int Version { get; init; } = CurrentVersion;
    public int WorldSeed { get; init; }
    public long Day { get; init; }
    public int MinuteOfDay { get; init; }
    public Guid? PlayerNpcId { get; init; }
    public int NpcCount { get; init; }
    public int FamilyCount { get; init; }
    public string SimulationVersion { get; init; } = "1.0";
}

public sealed class SaveGameSystem
{
    public SaveGameManifest CreateManifest(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return new SaveGameManifest
        {
            WorldSeed = world.WorldSeed,
            Day = world.Time.Day,
            MinuteOfDay = world.Time.MinuteOfDay,
            PlayerNpcId = world.PlayerNpc?.Id,
            NpcCount = world.Npcs.Count,
            FamilyCount = world.Families.Count
        };
    }

    public string SerializeManifest(WorldState world) =>
        System.Text.Json.JsonSerializer.Serialize(CreateManifest(world), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

    public SaveGameManifest LoadManifest(string json)
    {
        var manifest = System.Text.Json.JsonSerializer.Deserialize<SaveGameManifest>(json)
            ?? throw new InvalidDataException("Sauvegarde vide ou invalide.");
        if (manifest.Version != SaveGameManifest.CurrentVersion)
            throw new InvalidDataException($"Version de sauvegarde {manifest.Version} non supportée.");
        return manifest;
    }
}
