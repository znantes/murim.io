namespace Murim.Simulation;

public enum RelationshipLabel { Stranger, Acquaintance, Friend, CloseFriend, Rival, Enemy, Lover, Spouse, Family, Mentor, Disciple, Ally }
public enum MemoryValence { VeryNegative = -2, Negative = -1, Neutral = 0, Positive = 1, VeryPositive = 2 }

public sealed record RelationshipMemory(
    Guid Id,
    long Day,
    string Summary,
    MemoryValence Valence,
    double EmotionalWeight,
    bool IsAnchor);

public sealed class RelationshipState
{
    public Guid OtherNpcId { get; init; }
    public double Familiarity { get; set; }
    public double Affection { get; set; }
    public double Trust { get; set; }
    public double Respect { get; set; }
    public double Attraction { get; set; }
    public double Fear { get; set; }
    public double Resentment { get; set; }
    public double Obligation { get; set; }
    public double Rivalry { get; set; }
    public double KinshipStrength { get; set; }
    public long LastContactDay { get; set; }
    public double TypicalContactIntervalDays { get; set; } = 30;
    public List<RelationshipMemory> Memories { get; } = new();

    public RelationshipLabel Label(bool married)
    {
        if (married) return RelationshipLabel.Spouse;
        if (KinshipStrength >= 0.6) return RelationshipLabel.Family;
        if (Resentment > 65 && Trust < -25) return RelationshipLabel.Enemy;
        if (Rivalry > 60) return RelationshipLabel.Rival;
        if (Attraction > 60 && Affection > 55) return RelationshipLabel.Lover;
        if (Affection > 70 && Trust > 55) return RelationshipLabel.CloseFriend;
        if (Affection > 40 && Trust > 20) return RelationshipLabel.Friend;
        return Familiarity > 20 ? RelationshipLabel.Acquaintance : RelationshipLabel.Stranger;
    }
}

public sealed class RelationshipSystem
{
    private const double Ln2 = 0.6931471805599453;

    public RelationshipState GetOrCreate(Npc owner, Npc other, long day)
    {
        if (owner.Relationships.TryGetValue(other.Id, out var existing)) return existing;
        var relation = new RelationshipState { OtherNpcId = other.Id, LastContactDay = day };
        owner.Relationships[other.Id] = relation;
        return relation;
    }

    public void SetKinship(Npc a, Npc b, double strength, long day)
    {
        var ab = GetOrCreate(a, b, day);
        var ba = GetOrCreate(b, a, day);
        ab.KinshipStrength = ba.KinshipStrength = Math.Clamp(strength, 0, 1);
        ab.Familiarity = Math.Max(ab.Familiarity, 25);
        ba.Familiarity = Math.Max(ba.Familiarity, 25);
    }

    public void RecordInteraction(Npc actor, Npc other, long day, double warmth, double reliability, double respect, double attraction = 0, string? memory = null, bool anchor = false)
    {
        Apply(GetOrCreate(actor, other, day), day, warmth, reliability, respect, attraction, memory, anchor);
        Apply(GetOrCreate(other, actor, day), day, warmth * 0.85, reliability * 0.9, respect * 0.9, attraction * 0.85, memory, anchor);
    }

    private static void Apply(RelationshipState r, long day, double warmth, double reliability, double respect, double attraction, string? memory, bool anchor)
    {
        var gap = Math.Max(1, day - r.LastContactDay);
        r.TypicalContactIntervalDays = Math.Clamp(r.TypicalContactIntervalDays * 0.8 + gap * 0.2, 1, 3650);
        r.LastContactDay = day;
        r.Familiarity = Clamp100(r.Familiarity + 3 + Math.Abs(warmth) * 0.04);
        r.Affection = ClampSigned(r.Affection + warmth);
        r.Trust = ClampSigned(r.Trust + reliability);
        r.Respect = ClampSigned(r.Respect + respect);
        r.Attraction = ClampSigned(r.Attraction + attraction);
        if (warmth < 0) r.Resentment = Clamp100(r.Resentment + -warmth * 0.5);
        if (!string.IsNullOrWhiteSpace(memory))
        {
            var valence = warmth > 8 ? MemoryValence.VeryPositive : warmth > 1 ? MemoryValence.Positive : warmth < -8 ? MemoryValence.VeryNegative : warmth < -1 ? MemoryValence.Negative : MemoryValence.Neutral;
            r.Memories.Add(new RelationshipMemory(Guid.NewGuid(), day, memory!, valence, Math.Clamp(Math.Abs(warmth) + Math.Abs(reliability) + Math.Abs(respect), 1, 100), anchor));
            if (r.Memories.Count > 40) r.Memories.RemoveRange(0, r.Memories.Count - 40);
        }
    }

    public void AdvanceDecay(WorldState world, long days)
    {
        if (days <= 0) return;
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive))
        {
            foreach (var r in npc.Relationships.Values)
            {
                if (!world.Npcs.TryGetValue(r.OtherNpcId, out var other) || !other.IsAlive) continue;
                var noContactDays = Math.Max(0, world.Clock.Day - r.LastContactDay);
                if (noContactDays <= r.TypicalContactIntervalDays * 1.5) continue;

                var kin = r.KinshipStrength;
                var anchored = r.Memories.Where(m => m.IsAnchor).Sum(m => m.EmotionalWeight) / 100.0;
                var friendshipHalfLife = 365.0 * (1.2 + kin * 3.0 + Math.Min(3, anchored));
                var trustHalfLife = 365.0 * (2.0 + kin * 4.0 + Math.Min(4, anchored));
                var attractionHalfLife = 365.0 * (0.8 + Math.Min(2.5, anchored));
                var familiarityHalfLife = 365.0 * (4.0 + kin * 6.0);

                r.Affection = DecayToward(r.Affection, EmotionalFloor(r, positive: true), days, friendshipHalfLife);
                r.Trust = DecayToward(r.Trust, 0, days, trustHalfLife);
                r.Attraction = DecayToward(r.Attraction, 0, days, attractionHalfLife);
                r.Familiarity = Math.Max(0, DecayToward(r.Familiarity, kin * 20, days, familiarityHalfLife));
                r.Resentment = Math.Max(0, DecayToward(r.Resentment, 0, days, 365.0 * (2.5 + Math.Min(3, anchored))));
                r.Rivalry = Math.Max(0, DecayToward(r.Rivalry, 0, days, 365.0 * 1.5));
            }
        }
    }

    private static double EmotionalFloor(RelationshipState r, bool positive)
    {
        var anchors = r.Memories.Where(m => m.IsAnchor && (positive ? m.Valence > MemoryValence.Neutral : m.Valence < MemoryValence.Neutral));
        var weight = anchors.Sum(m => m.EmotionalWeight);
        return Math.Clamp(weight * 0.08 + r.KinshipStrength * 8, 0, 35);
    }

    private static double DecayToward(double value, double target, long days, double halfLifeDays)
    {
        var factor = Math.Exp(-Ln2 * days / Math.Max(1, halfLifeDays));
        return target + (value - target) * factor;
    }

    private static double ClampSigned(double value) => Math.Clamp(value, -100, 100);
    private static double Clamp100(double value) => Math.Clamp(value, 0, 100);
}

public enum RumorStance { Rejects, Doubts, Unsure, Believes, Certain }
public enum RumorMutation { None, Omission, Exaggeration, Softening, SourceShortening, BlameShift, PrestigeInflation }

public sealed class Rumor
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? UnderlyingEventId { get; init; }
    public Guid OriginNpcId { get; init; }
    public Guid? SubjectNpcId { get; init; }
    public Guid? LocationId { get; init; }
    public string Topic { get; init; } = string.Empty;
    public string Claim { get; init; } = string.Empty;
    public double Truth { get; init; } = 1;
    public double Ambiguity { get; init; } = 0.5;
    public double Importance { get; init; } = 0.5;
    public double Anxiety { get; init; } = 0.2;
    public double SourceCredibility { get; init; } = 0.5;
    public double Secrecy { get; init; }
    public long CreatedDay { get; init; }
    public int HopCount { get; set; }
    public double Distortion { get; set; }
    public RumorMutation LastMutation { get; set; }
}

public sealed class RumorBelief
{
    public Guid RumorId { get; init; }
    public double Confidence { get; set; }
    public RumorStance Stance { get; set; }
    public Guid HeardFromNpcId { get; set; }
    public long LastHeardDay { get; set; }
    public int IndependentSources { get; set; } = 1;
}

public sealed record RumorTransmission(Guid RumorId, Guid FromNpcId, Guid ToNpcId, long Day, bool Shared, double Probability, RumorMutation Mutation);

public sealed class RumorSystem
{
    private readonly Random random;
    public List<RumorTransmission> TransmissionLog { get; } = new();
    public RumorSystem(int seed) => random = new Random(seed);

    public Rumor Publish(WorldState world, Npc source, string topic, string claim, double truth, double ambiguity, double importance, double anxiety, double sourceCredibility, Guid? subjectId = null, Guid? locationId = null, Guid? eventId = null)
    {
        var rumor = new Rumor
        {
            OriginNpcId = source.Id, Topic = topic, Claim = claim, Truth = Math.Clamp(truth, 0, 1),
            Ambiguity = Math.Clamp(ambiguity, 0, 1), Importance = Math.Clamp(importance, 0, 1),
            Anxiety = Math.Clamp(anxiety, 0, 1), SourceCredibility = Math.Clamp(sourceCredibility, 0, 1),
            SubjectNpcId = subjectId, LocationId = locationId, UnderlyingEventId = eventId, CreatedDay = world.Clock.Day
        };
        world.Rumors[rumor.Id] = rumor;
        source.RumorBeliefs[rumor.Id] = new RumorBelief { RumorId = rumor.Id, Confidence = sourceCredibility, Stance = sourceCredibility > 0.75 ? RumorStance.Certain : RumorStance.Believes, HeardFromNpcId = source.Id, LastHeardDay = world.Clock.Day };
        return rumor;
    }

    public RumorTransmission TryTransmit(WorldState world, Npc from, Npc to, Rumor rumor)
    {
        var relation = from.Relationships.GetValueOrDefault(to.Id);
        var trust = relation is null ? 0.45 : Math.Clamp((relation.Trust + 100) / 200.0, 0, 1);
        var relevance = 0.35 + to.Personality.Curiosity * 0.25 + to.Personality.Anxiety * rumor.Anxiety * 0.25 + to.Personality.Talkativeness * 0.15;
        if (rumor.SubjectNpcId == to.Id) relevance += 0.25;
        var credibility = rumor.SourceCredibility * 0.55 + trust * 0.45;
        var uncertaintyDrive = Math.Clamp(rumor.Ambiguity * 0.55 + rumor.Anxiety * 0.30 + rumor.Importance * 0.45, 0, 1.4);
        var secrecyPenalty = 1 - rumor.Secrecy * Math.Max(0.1, to.Personality.Loyalty);
        var p = Math.Clamp((0.08 + relevance * 0.36 + credibility * 0.28 + uncertaintyDrive * 0.28) * secrecyPenalty, 0.01, 0.97);
        var shared = random.NextDouble() < p;
        var mutation = shared ? PickMutation(from, to, rumor) : RumorMutation.None;

        if (shared)
        {
            rumor.HopCount++;
            rumor.LastMutation = mutation;
            rumor.Distortion = Math.Clamp(rumor.Distortion + DistortionDelta(mutation, from, rumor), 0, 1);
            UpdateBelief(to, from, rumor, credibility);
        }
        var transmission = new RumorTransmission(rumor.Id, from.Id, to.Id, world.Clock.Day, shared, p, mutation);
        TransmissionLog.Add(transmission);
        return transmission;
    }

    public void HearIndependentConfirmation(Npc listener, Npc source, Rumor rumor, double sourceCredibility, long day)
    {
        if (!listener.RumorBeliefs.TryGetValue(rumor.Id, out var belief))
        {
            UpdateBelief(listener, source, rumor, sourceCredibility);
            return;
        }
        belief.IndependentSources++;
        belief.LastHeardDay = day;
        var redundancyBoost = 1 - Math.Exp(-belief.IndependentSources * 0.35);
        belief.Confidence = Math.Clamp(belief.Confidence * 0.72 + sourceCredibility * 0.18 + redundancyBoost * 0.18, 0, 1);
        belief.Stance = StanceFor(belief.Confidence);
    }

    public void Correct(Npc listener, Rumor rumor, double evidenceQuality, long day)
    {
        if (!listener.RumorBeliefs.TryGetValue(rumor.Id, out var belief)) return;
        var correction = Math.Clamp(evidenceQuality, 0, 1) * (0.55 + listener.Personality.Patience * 0.25);
        belief.Confidence = Math.Clamp(belief.Confidence * (1 - correction), 0, 1);
        belief.LastHeardDay = day;
        belief.Stance = StanceFor(belief.Confidence);
    }

    private void UpdateBelief(Npc listener, Npc source, Rumor rumor, double credibility)
    {
        var bias = 0.75 + listener.Personality.Gullibility * 0.35 + listener.Personality.Anxiety * rumor.Anxiety * 0.2;
        var confidence = Math.Clamp((credibility * 0.55 + rumor.Importance * 0.15 + (1 - rumor.Distortion) * 0.15 + rumor.Ambiguity * 0.05) * bias, 0, 1);
        listener.RumorBeliefs[rumor.Id] = new RumorBelief { RumorId = rumor.Id, Confidence = confidence, Stance = StanceFor(confidence), HeardFromNpcId = source.Id, LastHeardDay = rumor.CreatedDay };
    }

    private RumorMutation PickMutation(Npc from, Npc to, Rumor rumor)
    {
        var chance = 0.12 + rumor.HopCount * 0.025 + from.Personality.Anxiety * 0.08 + from.Personality.Gullibility * 0.08;
        if (random.NextDouble() > Math.Clamp(chance, 0, 0.75)) return RumorMutation.None;
        var roll = random.Next(6);
        return roll switch
        {
            0 => RumorMutation.Omission,
            1 => RumorMutation.Exaggeration,
            2 => RumorMutation.Softening,
            3 => RumorMutation.SourceShortening,
            4 => RumorMutation.BlameShift,
            _ => RumorMutation.PrestigeInflation
        };
    }

    private static double DistortionDelta(RumorMutation mutation, Npc speaker, Rumor rumor) => mutation switch
    {
        RumorMutation.None => 0,
        RumorMutation.Omission => 0.025,
        RumorMutation.SourceShortening => 0.015,
        RumorMutation.Softening => 0.02,
        RumorMutation.Exaggeration => 0.05 + speaker.Personality.Anxiety * 0.03,
        RumorMutation.BlameShift => 0.07,
        RumorMutation.PrestigeInflation => 0.045,
        _ => 0.02
    };

    private static RumorStance StanceFor(double c) => c < 0.15 ? RumorStance.Rejects : c < 0.35 ? RumorStance.Doubts : c < 0.6 ? RumorStance.Unsure : c < 0.85 ? RumorStance.Believes : RumorStance.Certain;
}
