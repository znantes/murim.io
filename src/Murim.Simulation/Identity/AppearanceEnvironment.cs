namespace Murim.Simulation;

public sealed record AppearanceEnvironmentProfile(
    double GeneticStructureWeight,
    double NutritionStability,
    double CareAccess,
    double Grooming,
    double ClothingQuality,
    double DentalCare,
    double WeatherExposure,
    double SleepStability,
    double CurrentPresentation);

/// <summary>
/// Separates inherited morphology from the way a life is visibly expressed. Wealth can improve
/// food stability, clothing or access to care, but never rewrites a genome and never guarantees beauty.
/// A poor NPC may be striking; a prestigious heir may be physically ordinary.
/// </summary>
public sealed class AppearanceEnvironmentSystem
{
    public AppearanceEnvironmentProfile Update(WorldState world, PortraitGeneticsSystem portraits, Npc npc, double seasonalHarshness = .25)
    {
        var genome = portraits.GetOrCreate(npc);
        var appearance = portraits.AppearanceFor(npc);
        world.Households.TryGetValue(npc.HouseholdId ?? Guid.Empty, out var household);

        var wealth = Math.Clamp((household?.Wealth ?? 28) / 100.0, 0, 1);
        var personalOrder = Math.Clamp((npc.Personality.Patience * .45 + npc.Personality.Loyalty * .20 + (1 - npc.Personality.RiskTolerance) * .20 + npc.Personality.Ambition * .15), 0, 1);
        var nutrition = Math.Clamp(.34 + wealth * .46 - npc.Body.Hunger / 180.0, .05, 1);
        var care = Math.Clamp(.18 + wealth * .58 + npc.Skills.GetValueOrDefault("medicine") / 500.0, .05, 1);
        var grooming = Math.Clamp(.18 + wealth * .35 + personalOrder * .40 - npc.Body.Fatigue / 350.0, .02, 1);
        var clothing = Math.Clamp(.12 + wealth * .72, .02, 1);
        var dental = Math.Clamp(.12 + wealth * .48 + care * .18 - genome.DentalIrregularity * .20, .02, 1);
        var weatherExposure = Math.Clamp(seasonalHarshness * (.55 + (1 - wealth) * .30), 0, 1);
        var sleep = Math.Clamp(1 - npc.Body.SleepDebt / 100.0, 0, 1);

        // Environment changes visible condition rather than inherited craniofacial structure.
        appearance.SunExposure = Math.Clamp(appearance.SunExposure * .96 + weatherExposure * .04, 0, 100);
        appearance.SkinRoughness = Math.Clamp(appearance.SkinRoughness + weatherExposure * .045 - care * .025, 0, 1);
        appearance.BlemishAmount = Math.Clamp(appearance.BlemishAmount + (1 - nutrition) * .025 + npc.Body.Fatigue / 900.0 - care * .018, 0, 1);
        appearance.EyeBagAmount = Math.Clamp(appearance.EyeBagAmount + (1 - sleep) * .07, 0, 1);
        appearance.FacialWeight = Math.Clamp(genome.FacialAdipositySetPoint + (nutrition - .5) * .16 - Math.Max(0, npc.Body.Hunger - 55) / 260.0, 0, 1);

        var healthPresentation = Math.Clamp(npc.Body.Health / 100.0 * .45 + sleep * .20 + nutrition * .20 + care * .15, 0, 1);
        var currentPresentation = Math.Clamp(healthPresentation * .54 + grooming * .20 + clothing * .14 + dental * .12, 0, 1);
        return new AppearanceEnvironmentProfile(.72, nutrition, care, grooming, clothing, dental, weatherExposure, sleep, currentPresentation);
    }

    public double FamilyMorphologicalResemblance(PortraitGeneticsSystem portraits, Npc child, Npc parentA, Npc parentB)
    {
        var c = portraits.GetOrCreate(child);
        var a = portraits.GetOrCreate(parentA);
        var b = portraits.GetOrCreate(parentB);
        double Fit(double cv, double av, double bv) => 1 - Math.Min(1, Math.Abs(cv - (av + bv) / 2.0) * 2.2);
        return Math.Clamp(new[]
        {
            Fit(c.FaceWidth, a.FaceWidth, b.FaceWidth), Fit(c.JawWidth, a.JawWidth, b.JawWidth),
            Fit(c.NoseWidth, a.NoseWidth, b.NoseWidth), Fit(c.NoseLength, a.NoseLength, b.NoseLength),
            Fit(c.EyeSpacing, a.EyeSpacing, b.EyeSpacing), Fit(c.LipFullness, a.LipFullness, b.LipFullness)
        }.Average(), 0, 1);
    }
}
