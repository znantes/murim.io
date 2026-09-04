namespace Murim.Simulation;

public static class ObservationFormatter
{
    public static string ToText(ObservationResult result)
    {
        var lines = new List<string> { result.Description };
        if (result.EnvironmentalSigns.Count > 0) lines.Add("Environnement : " + string.Join(" · ", result.EnvironmentalSigns));
        if (result.PointsOfInterest.Count > 0) lines.Add("Points d'intérêt : " + string.Join(" · ", result.PointsOfInterest));
        if (result.People.Count > 0) lines.Add("Personnes : " + string.Join(" · ", result.People));
        if (result.Warnings.Count > 0) lines.Add("⚠ " + string.Join(" ", result.Warnings));
        return string.Join("\n", lines);
    }
}
