namespace Murim.World;

public sealed class FamilyLifeSystem
{
    public List<string> LastEvents { get; } = new();

    public void AdvanceDay(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        LastEvents.Clear();

        foreach (var npc in world.Npcs.Values)
        {
            if (!npc.IsAlive) continue;
            SimulateWorkAndEconomy(npc, world.Time.Day);
        }

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

    private void SimulateWorkAndEconomy(Npc npc, long day)
    {
        if (npc.AgeYears < 12) return;
        if (npc.AgeYears < 18)
        {
            npc.ApplyWealthChange(-npc.Profession.DailyExpense * 0.25);
            return;
        }

        if (npc.Profession.Type == ProfessionType.None)
            AssignProfession(npc, day);

        var productivity = 0.70 + npc.Profession.Skill / 100.0 * 0.60;
        var income = npc.Profession.DailyIncome * productivity;
        npc.ApplyWealthChange(income - npc.Profession.DailyExpense);

        if (day % 30 == 0)
            npc.History.Add("Travail", npc.AgeYears, $"Travaille comme {ProfessionName(npc.Profession.Type)} et gère ses ressources.");
    }

    private static void AssignProfession(Npc npc, long day)
    {
        var random = DeterministicRandom(day.GetHashCode(), npc.Id, day / 30);
        var type = random.Next(100) switch
        {
            < 18 => ProfessionType.Farmer,
            < 30 => ProfessionType.Craftsman,
            < 40 => ProfessionType.Merchant,
            < 50 => ProfessionType.Servant,
            < 58 => ProfessionType.Guard,
            < 65 => ProfessionType.Hunter,
            < 72 => ProfessionType.Scholar,
            < 79 => ProfessionType.Healer,
            < 86 => ProfessionType.Courier,
            < 93 => ProfessionType.MartialPractitioner,
            _ => ProfessionType.Fisher
        };

        npc.Profession.Type = type;
        npc.Profession.Skill = random.Next(15, 56);
        npc.Profession.DailyIncome = type switch
        {
            ProfessionType.Merchant => 3.0,
            ProfessionType.Healer or ProfessionType.Scholar => 2.6,
            ProfessionType.Craftsman => 2.4,
            ProfessionType.Guard or ProfessionType.MartialPractitioner => 2.2,
            ProfessionType.Courier => 2.0,
            _ => 1.5
        };
        npc.Profession.DailyExpense = 0.7 + random.NextDouble() * 0.8;
        npc.ApplyWealthChange(2.0 + random.NextDouble() * 8.0);
        npc.History.Add("Profession", npc.AgeYears, $"Commence une activité comme {ProfessionName(type)}.");
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

    private static string ProfessionName(ProfessionType type) => type switch
    {
        ProfessionType.Farmer => "agriculteur",
        ProfessionType.Hunter => "chasseur",
        ProfessionType.Craftsman => "artisan",
        ProfessionType.Merchant => "marchand",
        ProfessionType.Guard => "garde",
        ProfessionType.Soldier => "soldat",
        ProfessionType.Healer => "soigneur",
        ProfessionType.Scholar => "érudit",
        ProfessionType.Servant => "serviteur",
        ProfessionType.Fisher => "pêcheur",
        ProfessionType.Courier => "messager",
        ProfessionType.MartialPractitioner => "pratiquant martial",
        ProfessionType.Official => "fonctionnaire",
        ProfessionType.Teacher => "enseignant",
        ProfessionType.Criminal => "criminel",
        _ => "sans profession"
    };

    private static Random DeterministicRandom(int worldSeed, Guid npcId, long day)
    {
        var bytes = npcId.ToByteArray();
        var guidHash = BitConverter.ToInt32(bytes, 0) ^ BitConverter.ToInt32(bytes, 8);
        return new Random(HashCode.Combine(worldSeed, guidHash, day));
    }
}
