namespace Murim.Simulation;

public enum RealmSubRank { Entry, Established, Extreme }
public enum TechniqueMasteryStage { Learned, Familiar, Practiced, Proficient, Expert, Mastered, Perfected }

public sealed class MartialProgressionState
{
    public MartialRealm Realm { get; set; } = MartialRealm.Untrained;
    public RealmSubRank SubRank { get; set; } = RealmSubRank.Entry;
    public double StageProgress { get; set; }
    public double Foundation { get; set; } = 10;
    public double Insight { get; set; }
    public double BottleneckPressure { get; set; }
    public long LastBreakthroughDay { get; set; }
    public bool AtBottleneck => StageProgress >= 99;
    public string DisplayLabel => Realm == MartialRealm.Untrained ? "Non-pratiquant" : $"{RealmLabel(Realm)} — {SubRankLabel(SubRank)}";

    public static string SubRankLabel(RealmSubRank s) => s switch { RealmSubRank.Entry => "entrée", RealmSubRank.Established => "accompli", RealmSubRank.Extreme => "extrême", _ => s.ToString() };
    public static string RealmLabel(MartialRealm r) => MartialRealmCatalog.All.FirstOrDefault(x => x.Realm == r)?.FrenchLabel ?? r.ToString();
}

public sealed record MartialTrainingResult(double ProgressGain, double FoundationGain, double InsightGain, double FatigueGain, bool ReadyForBreakthrough, string Summary);
public sealed record BreakthroughResult(bool Success, MartialRealm OldRealm, RealmSubRank OldSubRank, MartialRealm NewRealm, RealmSubRank NewSubRank, double Chance, string Summary);

public sealed class MartialProgressionSystem
{
    private readonly Random random;
    public MartialProgressionSystem(int seed) => random = new Random(seed);

    public MartialTrainingResult Train(Npc npc, double hours, double intensity, double instructionQuality = .5)
    {
        hours = Math.Clamp(hours, 0, 24 * 14); intensity = Math.Clamp(intensity, .05, 1); instructionQuality = Math.Clamp(instructionQuality, 0, 1);
        var state = npc.Martial; var physique = npc.Physiology.Physical; var spirit = npc.Physiology.Spirit; var endurance = npc.Physiology.Endurance; var qi = npc.Physiology.Qi;
        var fatiguePenalty = Math.Clamp(1 - npc.Body.Fatigue / 115.0, .08, 1);
        var injuryPenalty = Math.Clamp(1 - npc.Injuries.Where(i => i.Active).Sum(i => i.FunctionalLoss) * .08, .35, 1);
        var learning = (.45 + spirit.Focus / 150.0 + spirit.Memory / 260.0 + instructionQuality * .42) * fatiguePenalty * injuryPenalty;
        var realmDifficulty = 1 + (int)state.Realm * .35 + (int)state.SubRank * .18;
        var progress = hours * (.18 + intensity * .34) * learning / realmDifficulty;
        var foundationGain = hours * (.018 + (1 - intensity) * .018) * (physique.Coordination + endurance.WorkCapacity) / 100.0;
        var insightGain = hours * .01 * (spirit.Focus + spirit.Perception + qi.Control) / 150.0 * (.6 + instructionQuality);
        var fatigueGain = hours * (0.10 + intensity * .22) * (1.35 - endurance.MuscularEndurance / 180.0);

        state.StageProgress = Math.Clamp(state.StageProgress + progress, 0, 100);
        state.Foundation = Math.Clamp(state.Foundation + foundationGain, 0, 100);
        state.Insight = Math.Clamp(state.Insight + insightGain, 0, 100);
        if (state.AtBottleneck) state.BottleneckPressure = Math.Clamp(state.BottleneckPressure + hours * .015, 0, 100);
        npc.Body.Fatigue = Math.Clamp(npc.Body.Fatigue + fatigueGain, 0, 100);
        npc.Body.Stamina = Math.Clamp(npc.Body.Stamina - hours * intensity * .6, 0, 100);

        var ready = state.AtBottleneck && state.Foundation >= FoundationRequirement(state) && qi.Control >= QiControlRequirement(state);
        return new MartialTrainingResult(progress, foundationGain, insightGain, fatigueGain, ready,
            ready ? $"{state.DisplayLabel} a atteint son goulot d'étranglement ; le passage doit être tenté." : $"Progression dans {state.DisplayLabel} : {state.StageProgress:0.#} %." );
    }

    public BreakthroughResult TryBreakthrough(Npc npc, InjurySystem injuries, long day)
    {
        var state = npc.Martial; var oldRealm = state.Realm; var oldSub = state.SubRank; var target = NextStage(oldRealm, oldSub);
        if (target is null) return new BreakthroughResult(false, oldRealm, oldSub, oldRealm, oldSub, 0, "Aucun rang normal ne dépasse cet état.");

        var qi = npc.Physiology.Qi; var spirit = npc.Physiology.Spirit; var endurance = npc.Physiology.Endurance;
        var fatiguePenalty = npc.Body.Fatigue / 100.0 * .35;
        var severityPenalty = npc.Injuries.Where(i => i.Active).Sum(i => (int)i.Severity * .025);
        var leapPenalty = target.Value.Realm != oldRealm ? .10 + (int)target.Value.Realm * .018 : .035;
        var chance = Math.Clamp(.08 + state.StageProgress / 100.0 * .20 + state.Foundation / 100.0 * .20 + qi.Control / 100.0 * .18 + qi.Stability / 100.0 * .12 + state.Insight / 100.0 * .12 + spirit.Willpower / 100.0 * .08 + endurance.RecoveryEfficiency / 100.0 * .05 - fatiguePenalty - severityPenalty - leapPenalty, .01, .94);
        if (!state.AtBottleneck || state.Foundation < FoundationRequirement(state) || qi.Control < QiControlRequirement(state)) chance *= .18;

        if (random.NextDouble() < chance)
        {
            state.Realm = target.Value.Realm; state.SubRank = target.Value.SubRank; state.StageProgress = 0; state.BottleneckPressure = 0;
            state.Foundation = Math.Clamp(state.Foundation * .78, 5, 100); state.Insight = Math.Clamp(state.Insight * .72, 0, 100); state.LastBreakthroughDay = day;
            return new BreakthroughResult(true, oldRealm, oldSub, state.Realm, state.SubRank, chance, $"Passage réussi : {state.DisplayLabel}.");
        }

        state.StageProgress = Math.Max(65, state.StageProgress - (8 + random.NextDouble() * 18)); state.Foundation = Math.Max(0, state.Foundation - (2 + random.NextDouble() * 8)); npc.Body.Fatigue = Math.Clamp(npc.Body.Fatigue + 18, 0, 100);
        var injurySeverity = chance < .18 ? InjurySeverity.Severe : chance < .35 ? InjurySeverity.Moderate : InjurySeverity.Light;
        injuries.ApplyTrauma(npc, BodyRegion.MeridianNetwork, BodySide.Center, InjuryKind.QiDeviation, injurySeverity, day, "échec de passage de rang");
        return new BreakthroughResult(false, oldRealm, oldSub, oldRealm, oldSub, chance, "Le passage échoue et perturbe la circulation interne.");
    }

    private static double FoundationRequirement(MartialProgressionState s) => Math.Clamp(12 + (int)s.Realm * 7 + (int)s.SubRank * 4, 10, 82);
    private static double QiControlRequirement(MartialProgressionState s) => Math.Clamp(4 + (int)s.Realm * 8 + (int)s.SubRank * 3, 0, 92);
    private static (MartialRealm Realm, RealmSubRank SubRank)? NextStage(MartialRealm realm, RealmSubRank sub)
    {
        if (realm == MartialRealm.Untrained) return (MartialRealm.ThirdRate, RealmSubRank.Entry);
        if (sub == RealmSubRank.Entry) return (realm, RealmSubRank.Established);
        if (sub == RealmSubRank.Established) return (realm, RealmSubRank.Extreme);
        if (realm == MartialRealm.LifeDeath) return null;
        return ((MartialRealm)((int)realm + 1), RealmSubRank.Entry);
    }
}

public sealed class TechniqueProgress
{
    public int TechniqueId { get; init; }
    public long LearnedDay { get; init; }
    public long LastPracticedDay { get; set; }
    public double PracticePoints { get; set; }
    public double Comprehension { get; set; }
    public double ExecutionConsistency { get; set; }
    public double PersonalAdaptation { get; set; }
    public int FailedExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public TechniqueMasteryStage Stage => PracticePoints switch { < 10 => TechniqueMasteryStage.Learned, < 35 => TechniqueMasteryStage.Familiar, < 90 => TechniqueMasteryStage.Practiced, < 180 => TechniqueMasteryStage.Proficient, < 340 => TechniqueMasteryStage.Expert, < 620 => TechniqueMasteryStage.Mastered, _ => TechniqueMasteryStage.Perfected };
}

public sealed record TechniquePracticeResult(TechniqueMasteryStage Before, TechniqueMasteryStage After, double PracticeGain, double EffectivePotency, double FailureRisk, string Summary);
public sealed record TechniqueVariant(Guid Id, int ParentTechniqueId, Guid CreatorNpcId, long CreatedDay, string Name, double PotencyModifier, double CostModifier, IReadOnlyList<string> NewTags, string OriginStory);

public sealed class TechniqueMasterySystem
{
    private readonly Random random;
    public TechniqueMasterySystem(int seed) => random = new Random(seed);

    public TechniqueProgress Learn(Npc npc, TechniqueDefinition technique, long day)
    {
        if (npc.Techniques.TryGetValue(technique.Id, out var known)) return known;
        var progress = new TechniqueProgress { TechniqueId = technique.Id, LearnedDay = day, LastPracticedDay = day, Comprehension = 2, ExecutionConsistency = 1 };
        npc.Techniques[technique.Id] = progress; return progress;
    }

    public TechniquePracticeResult Practice(Npc npc, TechniqueDefinition technique, double hours, double teacherQuality, double feedbackQuality, long day, InjurySystem injuries)
    {
        hours = Math.Clamp(hours, 0, 24 * 7); teacherQuality = Math.Clamp(teacherQuality, 0, 1); feedbackQuality = Math.Clamp(feedbackQuality, 0, 1);
        var p = Learn(npc, technique, day); var before = p.Stage; var spirit = npc.Physiology.Spirit; var qi = npc.Physiology.Qi;
        var fatiguePenalty = Math.Clamp(1 - npc.Body.Fatigue / 115.0, .08, 1); var realmGap = Math.Max(0, (int)technique.RecommendedRealm - (int)npc.Martial.Realm);
        var complexityPenalty = Math.Clamp(1.18 - technique.Complexity * .45 - realmGap * .13, .12, 1.2);
        var learningQuality = (.45 + teacherQuality * .28 + feedbackQuality * .24 + spirit.Focus / 250.0 + spirit.Memory / 300.0) * fatiguePenalty * complexityPenalty;
        var gain = hours * (.42 + learningQuality) * Math.Max(.12, 1 - p.PracticePoints / 900.0);
        p.PracticePoints = Math.Clamp(p.PracticePoints + gain, 0, 1000); p.Comprehension = Math.Clamp(p.Comprehension + gain * (.08 + spirit.Perception / 900.0), 0, 100);
        p.ExecutionConsistency = Math.Clamp(p.ExecutionConsistency + gain * (.07 + feedbackQuality * .04), 0, 100); p.LastPracticedDay = day;
        var failureRisk = Math.Clamp(technique.InjuryRisk + realmGap * .035 + npc.Body.Fatigue / 100.0 * .12 - p.ExecutionConsistency / 100.0 * .08 - qi.Control / 100.0 * .06, .001, .65);
        if (random.NextDouble() < failureRisk)
        {
            p.FailedExecutions++;
            if (technique.Domain is TechniqueDomain.InternalCultivation or TechniqueDomain.BodyTempering)
                injuries.ApplyTrauma(npc, BodyRegion.MeridianNetwork, BodySide.Center, InjuryKind.MeridianTrauma, failureRisk > .3 ? InjurySeverity.Moderate : InjurySeverity.Light, day, $"mauvaise exécution de {technique.Name}");
        }
        else p.SuccessfulExecutions++;
        npc.Body.Fatigue = Math.Clamp(npc.Body.Fatigue + hours * (.15 + technique.StaminaCost * .018), 0, 100);
        var after = p.Stage; return new TechniquePracticeResult(before, after, gain, EffectivePotency(npc, technique), failureRisk,
            after != before ? $"{technique.Name} atteint la maîtrise {StageLabel(after)}." : $"La maîtrise de {technique.Name} progresse.");
    }

    public double EffectivePotency(Npc npc, TechniqueDefinition technique)
    {
        if (!npc.Techniques.TryGetValue(technique.Id, out var p)) return 0;
        var mastery = p.Stage switch { TechniqueMasteryStage.Learned => .18, TechniqueMasteryStage.Familiar => .31, TechniqueMasteryStage.Practiced => .49, TechniqueMasteryStage.Proficient => .72, TechniqueMasteryStage.Expert => .96, TechniqueMasteryStage.Mastered => 1.18, TechniqueMasteryStage.Perfected => 1.48, _ => .1 };
        var comprehension = .72 + p.Comprehension / 250.0; var consistency = .65 + p.ExecutionConsistency / 285.0; var fatigue = Math.Clamp(1 - npc.Body.Fatigue / 135.0, .28, 1); var adaptation = 1 + p.PersonalAdaptation * .0025;
        return technique.Potency * mastery * comprehension * consistency * fatigue * adaptation;
    }

    public void AdvanceForgetting(Npc npc, long currentDay)
    {
        foreach (var p in npc.Techniques.Values)
        {
            var idle = Math.Max(0, currentDay - p.LastPracticedDay); if (idle < 120) continue;
            var retention = p.Stage switch { TechniqueMasteryStage.Learned => .00075, TechniqueMasteryStage.Familiar => .00052, TechniqueMasteryStage.Practiced => .00032, TechniqueMasteryStage.Proficient => .00018, TechniqueMasteryStage.Expert => .00010, TechniqueMasteryStage.Mastered => .000045, TechniqueMasteryStage.Perfected => .000012, _ => .0005 };
            var loss = (idle - 120) * retention * Math.Max(1, p.PracticePoints); p.PracticePoints = Math.Max(1, p.PracticePoints - loss); p.ExecutionConsistency = Math.Max(5, p.ExecutionConsistency - (idle - 120) * retention * 20);
        }
    }

    public TechniqueVariant? TryCreateVariant(Npc npc, TechniqueDefinition technique, long day, string? chosenName = null)
    {
        if (!npc.Techniques.TryGetValue(technique.Id, out var p) || (int)p.Stage < (int)TechniqueMasteryStage.Mastered) return null;
        if (day - p.LearnedDay < 365L * 20) return null;
        var creativeChance = Math.Clamp(.03 + p.Comprehension / 600.0 + npc.Physiology.Spirit.Perception / 900.0, .03, .32); if (random.NextDouble() > creativeChance) return null;
        p.PersonalAdaptation = Math.Clamp(p.PersonalAdaptation + 6 + random.NextDouble() * 12, 0, 100);
        return new TechniqueVariant(Guid.NewGuid(), technique.Id, npc.Id, day, chosenName ?? $"{technique.Name} — variante de {npc.Identity.GivenName}", .92 + random.NextDouble() * .20, .88 + random.NextDouble() * .22,
            ["personal_variant", technique.Domain.ToString().ToLowerInvariant()], $"Variante née après {Math.Max(20, (day - p.LearnedDay) / 365)} années de pratique.");
    }

    public static string StageLabel(TechniqueMasteryStage stage) => stage switch { TechniqueMasteryStage.Learned => "apprise", TechniqueMasteryStage.Familiar => "familière", TechniqueMasteryStage.Practiced => "pratiquée", TechniqueMasteryStage.Proficient => "compétente", TechniqueMasteryStage.Expert => "experte", TechniqueMasteryStage.Mastered => "maîtrisée", TechniqueMasteryStage.Perfected => "perfectionnée", _ => stage.ToString() };
}
