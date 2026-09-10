using Murim.Simulation;

public static class UrbanVisualSmokeChecks
{
    public static void Run(LivingWorldRuntime runtime)
    {
        var world = runtime.World;
        Check(runtime.Illustrations.Profiles.Count >= world.Locations.Count, "Illustration catalog cannot be smaller than the world-location catalog.");

        foreach (var location in world.Locations.Values)
        {
            var profile = runtime.Illustrations.For(location.Id);
            Check(profile.SubjectKind == IllustrationSubjectKind.WorldLocation, $"World location {location.Name} is missing its illustration identity.");
            Check(profile.AssetDirectory.StartsWith("res://Assets/World/", StringComparison.Ordinal), "Illustration asset path must live under the Godot world-assets directory.");
            var candidates = runtime.Illustrations.CandidateAssetPaths(location.Id, IllustrationSeason.Winter, IllustrationTime.Night, IllustrationWeather.Snow);
            Check(candidates.Count >= 5 && candidates[^1].EndsWith("default.webp", StringComparison.Ordinal), "Illustration lookup must fall back to default.webp.");
        }

        var hanyang = world.Locations.Values.First(l => l.Name == "Cité de Hanyang");
        var hanyangDistricts = runtime.Districts.ForLocation(hanyang.Id);
        Check(hanyangDistricts.Any(d => d.Kind == DistrictKind.CentralMarket), "Hanyang needs a central market district.");
        Check(hanyangDistricts.Any(d => d.Kind == DistrictKind.Smiths), "Hanyang needs a blacksmith district.");
        Check(hanyangDistricts.Any(d => d.Kind == DistrictKind.Pleasure), "Hanyang needs an entertainment/pleasure district.");
        Check(hanyangDistricts.Any(d => d.Kind == DistrictKind.Guilds), "Hanyang needs a guild district.");

        var pleasure = hanyangDistricts.First(d => d.Kind == DistrictKind.Pleasure);
        Check(pleasure.Venues.Any(v => v.Category == "theatre" && !v.AdultOnly), "Pleasure district should contain ordinary cultural venues.");
        Check(pleasure.Venues.Any(v => v.AdultOnly), "Pleasure district should support age-restricted establishments without making the entire street adult-only.");

        var child = world.Npcs.Values.First(n => n.AgeYears(world.Clock) < 18);
        var adultVenue = pleasure.Venues.First(v => v.AdultOnly);
        Check(!runtime.Districts.CanEnterVenue(child, adultVenue, world.Clock), "Children must not enter adult-only pleasure venues.");
        var adult = world.Npcs.Values.First(n => n.AgeYears(world.Clock) >= 18);
        Check(runtime.Districts.CanEnterVenue(adult, adultVenue, world.Clock), "Adult age check should not block an adult by age alone.");

        var port = world.Locations.Values.First(l => l.Name == "Port de la Rivière Rouge");
        Check(runtime.Districts.ForLocation(port.Id).Any(d => d.Kind == DistrictKind.Docks), "A major port needs docks.");

        foreach (var district in runtime.Districts.All)
        {
            Check(runtime.Illustrations.Profiles.ContainsKey(district.Id), $"District {district.Name} lacks an illustration profile.");
            foreach (var venue in district.Venues)
                Check(runtime.Illustrations.Profiles.ContainsKey(venue.Id), $"Venue {venue.Name} lacks an illustration profile.");
        }

        var facilities = runtime.InstitutionDomains.Domains.Values.SelectMany(d => d.Facilities).ToArray();
        Check(facilities.Length > 0, "Institution facilities must exist before image coverage is checked.");
        Check(facilities.All(f => runtime.Illustrations.Profiles.ContainsKey(f.Id)), "Every family/sect facility needs an illustration profile.");

        var firstProfile = runtime.Illustrations.For(hanyang.Id);
        runtime.RebuildSpatialCatalogs();
        var rebuiltProfile = runtime.Illustrations.For(hanyang.Id);
        Check(firstProfile.VisualSeed == rebuiltProfile.VisualSeed && firstProfile.AssetDirectory == rebuiltProfile.AssetDirectory,
            "Visual identity must remain deterministic after rebuilding spatial catalogs.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
