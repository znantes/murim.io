namespace Murim.World;

public sealed class FamilyLifeSystem
{
    public List<string> LastEvents { get; } = new();

    public void AdvanceDay(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        LastEvents.Clear();

        foreach (var npc in world.Npcs.Values)
            if (npc.IsAlive)
                npc.AdvanceDays(1);

        foreach (var family in world.Families.Values.ToList())
            SimulateFamilyDay(world, family);

        HandleNaturalDeaths(world);
    }

    public void LinkParentChild(Npc parent, Npc child) =>
        AddBidirectional(parent, child, RelationshipType.Parent, RelationshipType.Child, 0.65, 0.75, 0.55);

    public void LinkSiblings(Npc first, Npc second) =>
        AddBidirectional(first, second, RelationshipType.Sibling, RelationshipType.Sibling, 0.45, 0.55, 0.35);

    public void LinkSpouses(Npc first, Npc second) =>
        AddBidirectional(first, second, RelationshipType.Spouse, RelationshipType.Spouse, 0.70, 0.70, 0.55);

    private void SimulateFamilyDay(WorldState world, Family family)
    {
        var members = world.Npcs.Values.Where(n => n.IsAlive && n.Birth.FamilyId == family.Id).ToList();
        if (members.Count == 0) return;

        foreach (var adult in members.Where(n => n.AgeYears >= 18))
        {
            var random = DeterministicRandom(world.WorldSeed, adult.Id, world.Time.Day);
            if (random.NextDouble() < 0.015)
            {
                adult.History.Add("Étape de vie", adult.AgeYears, "Poursuit son activité et fait évoluer sa situation personnelle.");
                LastEvents.Add($"{adult.Identity.DisplayName} poursuit sa vie autonome.");
            }
        }

        if (family.FatherId is not Guid fatherId || family.MotherId is not Guid motherId) return;
        if (!world.Npcs.TryGetValue(fatherId, out var father) || !world.Npcs.TryGetValue(motherId, out var mother)) return;
        if (!father.IsAlive || !mother.IsAlive || father.AgeYears < 18 || mother.AgeYears < 18) return;

        LinkSpouses(father, mother);
        TryBirth(world, family, father, mother);
        TryFamilyConflict(father, mother, world.Time.Day, world.WorldSeed);
    }

    private void TryBirth(WorldState world, Family family, Npc father, Npc mother)
    {
        if (family.ChildrenIds.Count >= 8 || father.AgeYears > 65 || mother.AgeYears > 50) return;

        var random = DeterministicRandom(world.WorldSeed, father.Id, world.Time.Day);
        if (random.NextDouble() >= 0.0025) return;

        var child = world.CreateChild(random.Next(), family, father, mother, father.Birth.Culture, father.Birth.Region);
        LinkParentChild(father, child);
        LinkParentChild(mother, child);
        LastEvents.Add($"Naissance de {child.Identity.DisplayName} dans la famille {family.Name}.");
    }

    private void TryFamilyConflict(Npc first, Npc second, long day, int worldSeed)
    {
        var relationship = first.Relationships.FirstOrDefault(r => r.ToNpcId == second.Id && r.Type == RelationshipType.Spouse && r.IsActive);
        if (relationship is null) return;

        var random = DeterministicRandom(worldSeed + 17, first.Id, day);
        if (random.NextDouble() >= 0.003) return;

        var severity = 0.04 + random.NextDouble() * 0.10;
        relationship.Shift(-severity, -severity * 0.8, -severity * 0.4);
        var reverse = second.Relationships.FirstOrDefault(r => r.ToNpcId == first.Id && r.Type == RelationshipType.Spouse && r.IsActive);
        reverse?.Shift(-severity, -severity * 0.8, -severity * 0.4);
        first.History.Add("Conflit familial", first.AgeYears, $"Tension avec {second.Identity.DisplayName}.");
        second.History.Add("Conflit familial", second.AgeYears, $"Tension avec {first.Identity.DisplayName}.");
        LastEvents.Add($"Conflit entre {first.Identity.DisplayName} et {second.Identity.DisplayName}.");
    }

    private static void AddBidirectional(Npc first, Npc second, RelationshipType firstType, RelationshipType secondType, double affinity, double trust, double respect)
    {
        if (!first.Relationships.Any(r => r.ToNpcId == second.Id && r.Type == firstType))
            first.Relationships.Add(new Relationship { FromNpcId = first.Id, ToNpcId = second.Id, Type = firstType, Affinity = affinity, Trust = trust, Respect = respect });
        if (!second.Relationships.Any(r => r.ToNpcId == first.Id && r.Type == secondType))
            second.Relationships.Add(new Relationship { FromNpcId = second.Id, ToNpcId = first.Id, Type = secondType, Affinity = affinity, Trust = trust, Respect = respect });
    }

    private void HandleNaturalDeaths(WorldState world)
    {
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive && n.AgeYears >= 90).ToList())
        {
            var random = DeterministicRandom(world.WorldSeed + 91, npc.Id, world.Time.Day);
            var chance = Math.Min(0.30, 0.01 + (npc.AgeYears - 90) * 0.06);
            if (random.NextDouble() >= chance) continue;

            npc.Die();
            npc.History.Add("Décès", npc.AgeYears, "Meurt de causes naturelles.");
            LastEvents.Add($"Décès naturel de {npc.Identity.DisplayName}, à {npc.AgeYears} ans.");
            foreach (var relation in npc.Relationships) relation.IsActive = false;
        }
    }

    private static Random DeterministicRandom(int worldSeed, Guid npcId, long day)
    {
        var bytes = npcId.ToByteArray();
        var guidHash = BitConverter.ToInt32(bytes, 0) ^ BitConverter.ToInt32(bytes, 8);
        return new Random(HashCode.Combine(worldSeed, guidHash, day));
    }
}
