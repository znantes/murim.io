namespace Murim.World;

public sealed class SocialRelationshipSystem
{
    public List<string> LastEvents { get; } = new();

    public void AdvanceDay(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        LastEvents.Clear();
        var adults = world.Npcs.Values.Where(n => n.IsAlive && n.AgeYears >= 18).ToList();
        if (adults.Count < 2) return;

        foreach (var npc in adults) ProcessExistingRelationships(world, npc);
        CreateNewBonds(world, adults);
        ProgressRomances(world, adults);
    }

    private void ProcessExistingRelationships(WorldState world, Npc npc)
    {
        foreach (var relation in npc.Relationships.Where(r => r.IsActive).ToList())
        {
            if (!world.Npcs.TryGetValue(relation.ToNpcId, out var other) || !other.IsAlive) continue;
            if (relation.Type is RelationshipType.Parent or RelationshipType.Child or RelationshipType.Sibling or RelationshipType.Master or RelationshipType.Student) continue;
            var random = DeterministicRandom(world.WorldSeed + 101, npc.Id, other.Id, world.Time.Day);
            if (relation.Type == RelationshipType.Friend && random.NextDouble() < 0.015) relation.Shift(0.01, 0.008, 0.006);
            if (relation.Type == RelationshipType.Spouse) SimulateMarriageStress(world, npc, other, relation, random);
            if (relation.Type == RelationshipType.Rival && random.NextDouble() < 0.02) relation.Shift(-0.015, -0.01, 0.005);
        }
    }

    private void CreateNewBonds(WorldState world, List<Npc> adults)
    {
        var attempts = Math.Min(10, adults.Count * 2);
        for (var i = 0; i < attempts; i++)
        {
            var first = adults[(int)((world.Time.Day * 7 + i * 13) % adults.Count)];
            var random = DeterministicRandom(world.WorldSeed + 109, first.Id, Guid.Empty, world.Time.Day + i);
            var second = adults[random.Next(adults.Count)];
            if (!CanInteract(first, second) || HasCloseBond(first, second)) continue;
            var affinity = -0.20 + random.NextDouble() * 0.65;
            var trust = 0.15 + random.NextDouble() * 0.30;
            var respect = 0.15 + random.NextDouble() * 0.35;
            if (affinity >= 0.30)
            {
                AddBidirectional(first, second, RelationshipType.Friend, affinity, trust, respect);
                first.History.Add("Amitié", first.AgeYears, $"Développe une amitié avec {second.Identity.DisplayName}.");
                second.History.Add("Amitié", second.AgeYears, $"Développe une amitié avec {first.Identity.DisplayName}.");
                LastEvents.Add($"Amitié entre {first.Identity.DisplayName} et {second.Identity.DisplayName}.");
            }
            else if (affinity <= -0.05)
            {
                AddBidirectional(first, second, RelationshipType.Rival, affinity, trust * 0.5, respect * 0.5);
                LastEvents.Add($"Rivalité naissante entre {first.Identity.DisplayName} et {second.Identity.DisplayName}.");
            }
            else AddBidirectional(first, second, RelationshipType.Acquaintance, affinity, trust, respect);
        }
    }

    private void ProgressRomances(WorldState world, List<Npc> adults)
    {
        foreach (var first in adults)
        foreach (var relation in first.Relationships.Where(r => r.IsActive && r.Type == RelationshipType.Friend).ToList())
        {
            if (!world.Npcs.TryGetValue(relation.ToNpcId, out var second) || !second.IsAlive || !CanInteract(first, second)) continue;
            var random = DeterministicRandom(world.WorldSeed + 127, first.Id, second.Id, world.Time.Day);
            var attraction = (first.Personality.Sociability + first.Personality.Empathy + second.Personality.Sociability + second.Personality.Empathy) / 4.0 + random.NextDouble() * 0.45 - 0.10;
            if (relation.Affinity > 0.55 && relation.Trust > 0.50 && attraction > 0.58 && random.NextDouble() < 0.035)
            {
                ConvertToRomance(first, second, relation);
                LastEvents.Add($"Une attirance apparaît entre {first.Identity.DisplayName} et {second.Identity.DisplayName}.");
            }
        }

        foreach (var first in adults)
        foreach (var relation in first.Relationships.Where(r => r.IsActive && r.Type == RelationshipType.RomanticInterest).ToList())
        {
            if (!world.Npcs.TryGetValue(relation.ToNpcId, out var second) || !second.IsAlive) continue;
            var random = DeterministicRandom(world.WorldSeed + 131, first.Id, second.Id, world.Time.Day);
            relation.Shift(0.01 + random.NextDouble() * 0.015, 0.005, 0.003);
            if (relation.Affinity > 0.78 && relation.Trust > 0.68 && random.NextDouble() < 0.02) CreateUnion(world, first, second);
            else if (relation.Affinity < 0.05 && random.NextDouble() < 0.04) EndRomance(first, second, relation);
        }
    }

    private void SimulateMarriageStress(WorldState world, Npc first, Npc second, Relationship relation, Random random)
    {
        var compatibility = (first.Personality.Patience + second.Personality.Patience + first.Personality.Empathy + second.Personality.Empathy) / 4.0;
        if (random.NextDouble() < 0.008 && compatibility < 0.45)
        {
            relation.Shift(-0.08, -0.07, -0.03);
            FindRelation(second, first, RelationshipType.Spouse)?.Shift(-0.08, -0.07, -0.03);
            LastEvents.Add($"Tension conjugale entre {first.Identity.DisplayName} et {second.Identity.DisplayName}.");
        }
        if (relation.Affinity < 0.05 && relation.Trust < 0.25 && random.NextDouble() < 0.03)
        {
            relation.IsActive = false; relation.Type = RelationshipType.ExSpouse;
            var reverse = FindRelation(second, first, RelationshipType.Spouse);
            if (reverse is not null) { reverse.IsActive = false; reverse.Type = RelationshipType.ExSpouse; }
            first.History.Add("Séparation", first.AgeYears, $"Séparation de {second.Identity.DisplayName}.");
            second.History.Add("Séparation", second.AgeYears, $"Séparation de {first.Identity.DisplayName}.");
            LastEvents.Add($"Séparation de {first.Identity.DisplayName} et {second.Identity.DisplayName}.");
        }
    }

    private void CreateUnion(WorldState world, Npc first, Npc second)
    {
        if (!CanInteract(first, second) || FindRelation(first, second, RelationshipType.Spouse) is not null) return;
        var family = new Family
        {
            Name = $"Branche {first.Identity.FamilyName}-{second.Identity.FamilyName}",
            Origin = first.CurrentFamilyId is Guid id && world.Families.TryGetValue(id, out var existing) ? existing.Origin : FamilyOrigin.Common,
            SocialStatus = "Foyer indépendant",
            FatherId = first.Identity.Sex == "Male" ? first.Id : second.Id,
            MotherId = first.Identity.Sex == "Female" ? first.Id : second.Id,
            ParentFamilyId = first.CurrentFamilyId
        };
        world.AddFamily(family);
        family.MemberIds.Add(first.Id); family.MemberIds.Add(second.Id);
        first.JoinFamily(family.Id); second.JoinFamily(family.Id);
        AddBidirectional(first, second, RelationshipType.Spouse, 0.82, 0.72, 0.60);
        first.History.Add("Union", first.AgeYears, $"S'unit avec {second.Identity.DisplayName} et fonde {family.Name}.");
        second.History.Add("Union", second.AgeYears, $"S'unit avec {first.Identity.DisplayName} et fonde {family.Name}.");
        LastEvents.Add($"Union de {first.Identity.DisplayName} et {second.Identity.DisplayName} : nouvelle branche familiale.");
    }

    private static void ConvertToRomance(Npc first, Npc second, Relationship friend)
    {
        friend.Type = RelationshipType.RomanticInterest; friend.Affinity = Math.Max(friend.Affinity, 0.60); friend.Trust = Math.Max(friend.Trust, 0.52);
        var reverse = FindRelation(second, first, RelationshipType.Friend);
        if (reverse is not null) { reverse.Type = RelationshipType.RomanticInterest; reverse.Affinity = Math.Max(reverse.Affinity, 0.60); reverse.Trust = Math.Max(reverse.Trust, 0.52); }
    }

    private static void EndRomance(Npc first, Npc second, Relationship relation)
    {
        relation.IsActive = false; relation.Type = RelationshipType.ExSpouse;
        var reverse = FindRelation(second, first, RelationshipType.RomanticInterest);
        if (reverse is not null) { reverse.IsActive = false; reverse.Type = RelationshipType.ExSpouse; }
        first.History.Add("Rupture", first.AgeYears, $"Rupture avec {second.Identity.DisplayName}.");
        second.History.Add("Rupture", second.AgeYears, $"Rupture avec {first.Identity.DisplayName}.");
    }

    private static bool CanInteract(Npc first, Npc second)
    {
        if (first.Id == second.Id || !first.IsAlive || !second.IsAlive || first.AgeYears < 18 || second.AgeYears < 18) return false;
        return FindRelation(first, second, RelationshipType.Parent) is null && FindRelation(first, second, RelationshipType.Child) is null && FindRelation(first, second, RelationshipType.Sibling) is null;
    }

    private static bool HasCloseBond(Npc first, Npc second) => first.Relationships.Any(r => r.ToNpcId == second.Id && r.IsActive && r.Type is RelationshipType.Friend or RelationshipType.Rival or RelationshipType.RomanticInterest or RelationshipType.Spouse);
    private static Relationship? FindRelation(Npc first, Npc second, RelationshipType type) => first.Relationships.FirstOrDefault(r => r.ToNpcId == second.Id && r.Type == type && r.IsActive);

    private static void AddBidirectional(Npc first, Npc second, RelationshipType type, double affinity, double trust, double respect)
    {
        var a = first.Relationships.FirstOrDefault(r => r.ToNpcId == second.Id && r.IsActive);
        if (a is null) first.Relationships.Add(new Relationship { FromNpcId = first.Id, ToNpcId = second.Id, Type = type, Affinity = affinity, Trust = trust, Respect = respect });
        else { a.Type = type; a.Affinity = Math.Max(a.Affinity, affinity); a.Trust = Math.Max(a.Trust, trust); a.Respect = Math.Max(a.Respect, respect); }
        var b = second.Relationships.FirstOrDefault(r => r.ToNpcId == first.Id && r.IsActive);
        if (b is null) second.Relationships.Add(new Relationship { FromNpcId = second.Id, ToNpcId = first.Id, Type = type, Affinity = affinity, Trust = trust, Respect = respect });
        else { b.Type = type; b.Affinity = Math.Max(b.Affinity, affinity); b.Trust = Math.Max(b.Trust, trust); b.Respect = Math.Max(b.Respect, respect); }
    }

    private static Random DeterministicRandom(int seed, Guid first, Guid second, long day)
    {
        var a = first.ToByteArray(); var b = second.ToByteArray();
        var hash = BitConverter.ToInt32(a, 0) ^ BitConverter.ToInt32(a, 8) ^ BitConverter.ToInt32(b, 0) ^ BitConverter.ToInt32(b, 8);
        return new Random(HashCode.Combine(seed, hash, day));
    }
}
