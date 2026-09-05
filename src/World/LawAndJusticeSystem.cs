namespace Murim.World;

public enum CrimeType { Theft, Assault, Fraud, Trespass }
public enum JusticeOutcome { Acquitted, Warning, Fine, Restitution, Imprisonment }

public sealed class CrimeRecord
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid OffenderNpcId { get; init; }
    public Guid VictimNpcId { get; init; }
    public Guid? LocationId { get; init; }
    public CrimeType Type { get; init; }
    public double Severity { get; init; }
    public long Day { get; init; }
    public bool Reported { get; set; }
    public bool Resolved { get; set; }
    public JusticeOutcome? Outcome { get; set; }
}

public sealed class LawAndJusticeSystem
{
    private readonly List<CrimeRecord> _crimes = new();
    public IReadOnlyList<CrimeRecord> Crimes => _crimes;

    public CrimeRecord Report(WorldState world, Npc victim, Npc offender, CrimeType type, double severity, string description)
    {
        var crime = new CrimeRecord { OffenderNpcId = offender.Id, VictimNpcId = victim.Id, LocationId = victim.CurrentLocationId, Type = type, Severity = Math.Clamp(severity, 0, 1), Day = world.Time.Day, Reported = true };
        _crimes.Add(crime);
        world.Reputation.Apply(world, offender.Id, -Math.Clamp(severity * 3, 0.5, 6), "Local", 1);
        victim.History.Add("Justice", victim.AgeYears, $"Signale un crime commis par {offender.Identity.DisplayName}.");
        world.Information.Publish(world, victim, "Crime", description, offender.Id, victim.CurrentLocationId, InformationReliability.Unverified, -Math.Clamp(severity, 0, 1));
        return crime;
    }

    public JusticeOutcome Resolve(WorldState world, CrimeRecord crime)
    {
        if (crime.Resolved) return crime.Outcome ?? JusticeOutcome.Acquitted;
        if (!world.Npcs.TryGetValue(crime.OffenderNpcId, out var offender) || !world.Npcs.TryGetValue(crime.VictimNpcId, out var victim))
        {
            crime.Resolved = true; crime.Outcome = JusticeOutcome.Acquitted; return crime.Outcome.Value;
        }
        var witnesses = crime.Reported ? 1 : 0;
        var evidence = Math.Clamp((crime.Severity * 0.55) + witnesses * 0.18, 0, 1);
        var outcome = evidence < 0.35 ? JusticeOutcome.Acquitted : evidence < 0.52 ? JusticeOutcome.Warning : evidence < 0.72 ? JusticeOutcome.Fine : evidence < 0.9 ? JusticeOutcome.Restitution : JusticeOutcome.Imprisonment;
        crime.Outcome = outcome; crime.Resolved = true;
        ApplyOutcome(world, crime, offender, victim, outcome);
        return outcome;
    }

    public void AdvanceDay(WorldState world)
    {
        foreach (var crime in _crimes.Where(c => c.Reported && !c.Resolved && c.Day < world.Time.Day).Take(24).ToList()) Resolve(world, crime);
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive))
        {
            var reputation = world.Reputation.GetValue(npc.Id, "Local");
            if (reputation < -35) npc.History.Add("Justice", npc.AgeYears, "Sa réputation locale rend les contrôles et interactions plus difficiles.");
        }
        if (_crimes.Count > 2000) _crimes.RemoveRange(0, _crimes.Count - 2000);
    }

    private static void ApplyOutcome(WorldState world, CrimeRecord crime, Npc offender, Npc victim, JusticeOutcome outcome)
    {
        switch (outcome)
        {
            case JusticeOutcome.Warning:
                offender.History.Add("Justice", offender.AgeYears, "Reçoit un avertissement pour un crime signalé.");
                break;
            case JusticeOutcome.Fine:
                var fine = Math.Min(offender.Wealth, 5 + crime.Severity * 25);
                offender.ApplyWealthChange(-fine);
                offender.History.Add("Justice", offender.AgeYears, $"Paie une amende de {fine:0.##}.");
                break;
            case JusticeOutcome.Restitution:
                var restitution = Math.Min(offender.Wealth, 5 + crime.Severity * 20);
                offender.ApplyWealthChange(-restitution); victim.ApplyWealthChange(restitution);
                offender.History.Add("Justice", offender.AgeYears, "Doit restituer une partie du préjudice.");
                victim.History.Add("Justice", victim.AgeYears, "Obtient une restitution après le jugement.");
                break;
            case JusticeOutcome.Imprisonment:
                offender.History.Add("Justice", offender.AgeYears, "Est condamné à une peine d'emprisonnement.");
                offender.SetLocation(victim.CurrentLocationId ?? Guid.Empty);
                world.Reputation.Apply(world, offender.Id, -2, "Local", 2);
                break;
        }
        var polarity = outcome == JusticeOutcome.Acquitted ? 0.05 : -0.1;
        world.Information.Publish(world, victim, "Jugement", $"Le crime de {offender.Identity.DisplayName} aboutit à : {outcome}.", offender.Id, victim.CurrentLocationId, InformationReliability.Plausible, polarity);
    }
}
