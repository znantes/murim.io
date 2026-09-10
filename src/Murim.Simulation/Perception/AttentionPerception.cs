namespace Murim.Simulation;

public enum ObservationDetailLevel { Missed, Vague, Basic, Clear, Detailed }
public enum SensoryChannel { Vision, Hearing, Smell, Touch, QiSense }

public sealed record SensoryContext(
    SensoryChannel Channel,
    double LightQuality = .8,
    double Noise = .2,
    double Distance = .2,
    double CrowdDensity = .2,
    double Obstruction = 0,
    double AttentionAllocation = .7,
    double SubjectSalience = .5);

public sealed record ObservationResult(
    ObservationDetailLevel Detail,
    double Confidence,
    bool RecognizedIdentity,
    IReadOnlyList<string> Details,
    string Summary);

/// <summary>
/// Converts physical proximity into limited knowledge. A crowded, dark or noisy scene does not
/// automatically expose every person and event to the player. The exact same rules can be used for NPC witnesses.
/// </summary>
public sealed class AttentionPerceptionSystem
{
    public ObservationResult ObservePerson(Npc observer, Npc target, SensoryContext context, long day)
    {
        if (observer.CurrentLocationId != target.CurrentLocationId)
            return new ObservationResult(ObservationDetailLevel.Missed, 0, false, [], "La personne est hors de votre environnement immédiat.");

        var spirit = observer.Physiology.Spirit;
        var baseSkill = spirit.Perception / 100.0;
        var fatiguePenalty = Math.Clamp(observer.Body.Fatigue / 135.0 + observer.Body.SleepDebt / 180.0, 0, .75);
        var injuryPenalty = observer.Injuries.Where(i => i.Active && i.Region is BodyRegion.Eye or BodyRegion.Head)
            .Sum(i => i.FunctionalLoss * .45);
        var environment = ChannelQuality(context);
        var attention = Math.Clamp(context.AttentionAllocation, 0, 1);
        var salience = Math.Clamp(context.SubjectSalience, 0, 1);
        var score = Math.Clamp((.18 + baseSkill * .58 + salience * .24) * environment * (.35 + attention * .65) - fatiguePenalty * .35 - injuryPenalty, 0, 1);
        var detail = score switch
        {
            < .12 => ObservationDetailLevel.Missed,
            < .30 => ObservationDetailLevel.Vague,
            < .52 => ObservationDetailLevel.Basic,
            < .76 => ObservationDetailLevel.Clear,
            _ => ObservationDetailLevel.Detailed
        };

        var relation = observer.Relationships.GetValueOrDefault(target.Id);
        var familiarity = relation?.Familiarity ?? 0;
        var yearsSinceContact = relation is null ? 999 : Math.Max(0, day - relation.LastContactDay) / 365.0;
        var recognitionBase = familiarity / 100.0 + spirit.Memory / 250.0 - Math.Min(.4, yearsSinceContact * .018);
        var recognition = detail >= ObservationDetailLevel.Basic && score + recognitionBase * .45 >= .56;

        var details = new List<string>();
        if (detail >= ObservationDetailLevel.Vague) details.Add("silhouette générale");
        if (detail >= ObservationDetailLevel.Basic) details.Add("vêtements et posture visibles");
        if (detail >= ObservationDetailLevel.Clear) details.Add("traits du visage et signes physiques remarquables");
        if (detail >= ObservationDetailLevel.Detailed) details.Add("petites marques, fatigue apparente et gestes fins");

        var summary = detail switch
        {
            ObservationDetailLevel.Missed => "Vous ne remarquez pas suffisamment cette personne.",
            ObservationDetailLevel.Vague => "Vous distinguez surtout une silhouette.",
            ObservationDetailLevel.Basic => recognition ? $"Vous pensez reconnaître {target.Identity.DisplayName}." : "Vous observez une personne sans l'identifier avec certitude.",
            ObservationDetailLevel.Clear => recognition ? $"Vous reconnaissez {target.Identity.DisplayName} et plusieurs détails de son apparence." : "Le visage est visible, mais l'identité ne vous revient pas.",
            _ => recognition ? $"Vous observez attentivement {target.Identity.DisplayName}." : "Vous voyez nettement cette personne, sans savoir qui elle est."
        };
        return new ObservationResult(detail, score, recognition, details, summary);
    }

    public int AttentionSlots(Npc observer, double urgency = .3)
    {
        var focus = observer.Physiology.Spirit.Focus;
        var fatigue = observer.Body.Fatigue;
        var slots = 1 + (int)Math.Floor(focus / 28.0 + Math.Clamp(urgency, 0, 1) * 2 - fatigue / 45.0);
        return Math.Clamp(slots, 1, 6);
    }

    private static double ChannelQuality(SensoryContext c)
    {
        var distance = 1 - Math.Clamp(c.Distance, 0, 1) * .65;
        var crowd = 1 - Math.Clamp(c.CrowdDensity, 0, 1) * .45;
        var obstruction = 1 - Math.Clamp(c.Obstruction, 0, 1) * .80;
        return c.Channel switch
        {
            SensoryChannel.Vision => Math.Clamp(c.LightQuality, .05, 1) * distance * crowd * obstruction,
            SensoryChannel.Hearing => (1 - Math.Clamp(c.Noise, 0, 1) * .75) * distance * crowd,
            SensoryChannel.Smell => distance * (1 - Math.Clamp(c.Noise, 0, 1) * .05),
            SensoryChannel.Touch => c.Distance <= .05 ? 1 : .05,
            SensoryChannel.QiSense => distance * obstruction * .85,
            _ => .5
        };
    }
}
