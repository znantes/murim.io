namespace Murim.World;

public sealed class DiagnosisResult
{
    public Guid NpcId { get; init; }
    public bool Accurate { get; init; }
    public List<Guid> IdentifiedConditionIds { get; } = new();
    public string Summary { get; init; } = string.Empty;
}

public sealed class MedicineSystem
{
    public PhysicalCondition Inflict(Npc npc, ConditionType type, string name, double severity, double pain, long day, bool contagious = false, int durationDays = 0, Guid? sourceNpcId = null)
    {
        var condition = new PhysicalCondition
        {
            Type = type,
            Name = name,
            Severity = Math.Clamp(severity, 0, 1),
            Pain = Math.Clamp(pain, 0, 1),
            MobilityPenalty = Math.Clamp(severity * 0.8, 0, 1),
            RecoveryRate = type == ConditionType.Fracture ? 0.006 : 0.02,
            Contagious = contagious,
            OnsetDay = day,
            ExpectedDurationDays = Math.Max(0, durationDays),
            SourceNpcId = sourceNpcId
        };
        npc.Conditions.Add(condition);
        return condition;
    }

    public DiagnosisResult Diagnose(Npc healer, Npc patient, long day)
    {
        var accuracy = Math.Clamp(0.35 + healer.Profession.Skill * 0.006 + healer.Mind.Intelligence * 0.003 + healer.Mind.Perception * 0.002, 0, 0.99);
        var result = new DiagnosisResult { NpcId = patient.Id, Accurate = accuracy >= 0.65 };
        foreach (var condition in patient.Conditions)
        {
            var roll = Deterministic01(healer.Id, patient.Id, day, condition.Id);
            if (roll <= accuracy) result.IdentifiedConditionIds.Add(condition.Id);
        }
        result.Summary = result.IdentifiedConditionIds.Count == 0
            ? "Le diagnostic reste incertain."
            : $"{result.IdentifiedConditionIds.Count} condition(s) identifiée(s).";
        return result;
    }

    public bool Treat(Npc healer, Npc patient, Guid conditionId, long day)
    {
        var condition = patient.Conditions.FirstOrDefault(c => c.Id == conditionId);
        if (condition is null || !condition.Treatable) return false;
        var effectiveness = Math.Clamp(0.15 + healer.Profession.Skill * 0.008 + healer.Mind.Intelligence * 0.002, 0.05, 0.85);
        condition.Severity = Math.Clamp(condition.Severity - effectiveness, 0, 1);
        condition.Pain = Math.Clamp(condition.Pain - effectiveness * 0.8, 0, 1);
        condition.MobilityPenalty = Math.Clamp(condition.MobilityPenalty - effectiveness * 0.6, 0, 1);
        if (condition.Severity <= 0.01) patient.Conditions.Remove(condition);
        patient.History.Add("Soin", patient.AgeYears, $"Soins reçus pour {condition.Name}.");
        return true;
    }

    public void AdvanceDay(WorldState world)
    {
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive))
        {
            foreach (var condition in npc.Conditions.ToList())
            {
                var recovery = condition.RecoveryRate;
                if (npc.Needs.Fatigue > 70 || npc.Needs.Hunger > 70 || npc.Needs.Thirst > 70) recovery *= 0.45;
                condition.Severity = Math.Clamp(condition.Severity - recovery, 0, 1);
                condition.Pain = Math.Clamp(condition.Pain - recovery * 0.7, 0, 1);
                condition.MobilityPenalty = Math.Clamp(condition.MobilityPenalty - recovery * 0.5, 0, 1);
                if (condition.Severity <= 0.01) npc.Conditions.Remove(condition);
            }
        }
        SpreadContagiousConditions(world);
    }

    private static void SpreadContagiousConditions(WorldState world)
    {
        foreach (var locationGroup in world.Npcs.Values.Where(n => n.IsAlive && n.CurrentLocationId is not null).GroupBy(n => n.CurrentLocationId))
        {
            var sick = locationGroup.Where(n => n.Conditions.Any(c => c.Contagious && c.Severity > 0.25)).ToList();
            if (sick.Count == 0) continue;
            foreach (var target in locationGroup.Where(n => n.Conditions.All(c => !c.Contagious)).ToList())
            {
                var source = sick[(int)(Hash(target.Id, world.Time.Day) % (ulong)sick.Count)];
                var condition = source.Conditions.First(c => c.Contagious && c.Severity > 0.25);
                if (Deterministic01(source.Id, target.Id, world.Time.Day, condition.Id) < 0.035)
                    new MedicineSystem().Inflict(target, condition.Type, condition.Name, 0.2, 0.15, world.Time.Day, true, condition.ExpectedDurationDays, source.Id);
            }
        }
    }

    private static double Deterministic01(Guid a, Guid b, long day, Guid c) => (Hash(a, day) ^ Hash(b, day + 17) ^ Hash(c, day + 31)) / (double)ulong.MaxValue;
    private static ulong Hash(Guid id, long salt)
    {
        var bytes = id.ToByteArray(); ulong h = 1469598103934665603UL ^ (ulong)salt;
        foreach (var b in bytes) h = (h ^ b) * 1099511628211UL;
        return h;
    }
}
