namespace Murim.World;

public enum WarCasualtyStatus
{
    Wounded,
    SeverelyWounded,
    Dead,
    Missing
}

public sealed class WarCasualty
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid WarOrSiegeId { get; init; }
    public Guid NpcId { get; init; }
    public WarCasualtyStatus Status { get; init; }
    public long Day { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed class WarLocationDamage
{
    public Guid LocationId { get; init; }
    public double StructuralDamage { get; set; }
    public double InfrastructureDamage { get; set; }
    public double FoodShortage { get; set; }
    public double TradeDisruption { get; set; }
    public int DisplacedPopulation { get; set; }
    public int CivilianCasualties { get; set; }
    public long LastUpdatedDay { get; set; }
}

public sealed class MartialWarConsequencesSystem
{
    private readonly List<WarCasualty> _casualties = new();
    private readonly Dictionary<Guid, WarLocationDamage> _locations = new();

    public IReadOnlyList<WarCasualty> Casualties => _casualties;
    public IReadOnlyDictionary<Guid, WarLocationDamage> LocationDamage => _locations;

    public void ApplyBattleConsequences(WorldState world, Guid conflictId, IReadOnlyList<Npc> attackers, IReadOnlyList<Npc> defenders, int attackerCasualties, int defenderCasualties)
    {
        ApplySide(world, conflictId, attackers, attackerCasualties, "combattant attaquant");
        ApplySide(world, conflictId, defenders, defenderCasualties, "combattant défenseur");
    }

    public void ApplySiegeConsequences(WorldState world, Guid siegeId, Guid locationId, IReadOnlyList<Npc> attackers, IReadOnlyList<Npc> defenders, int attackerCasualties, int defenderCasualties, double progress)
    {
        ApplySide(world, siegeId, attackers, attackerCasualties, "assaillant");
        ApplySide(world, siegeId, defenders, defenderCasualties, "défenseur");

        var damage = GetLocationDamage(locationId, world.Time.Day);
        damage.StructuralDamage = Math.Clamp(damage.StructuralDamage + 0.8 + progress * 0.015, 0, 100);
        damage.InfrastructureDamage = Math.Clamp(damage.InfrastructureDamage + 0.5 + progress * 0.01, 0, 100);
        damage.FoodShortage = Math.Clamp(damage.FoodShortage + 0.7, 0, 100);
        damage.TradeDisruption = Math.Clamp(damage.TradeDisruption + 1.2, 0, 100);

        if (world.Geography.Locations.TryGetValue(locationId, out var location))
        {
            var displaced = (int)Math.Round(location.Population * Math.Clamp((damage.StructuralDamage - 40) / 400.0, 0, 0.02));
            damage.DisplacedPopulation = Math.Max(damage.DisplacedPopulation, displaced);
            if (damage.StructuralDamage >= 85)
                location.DangerLevel = Math.Min(100, location.DangerLevel + 2);
        }
    }

    public void ApplyOccupation(WorldState world, Guid locationId, Guid organizationId)
    {
        var damage = GetLocationDamage(locationId, world.Time.Day);
        damage.TradeDisruption = Math.Clamp(damage.TradeDisruption + 5, 0, 100);
        damage.FoodShortage = Math.Clamp(damage.FoodShortage + 3, 0, 100);
        if (world.Geography.Locations.TryGetValue(locationId, out var location))
            location.DangerLevel = Math.Min(100, location.DangerLevel + 1);
    }

    public void AdvanceDay(WorldState world)
    {
        foreach (var damage in _locations.Values)
        {
            damage.FoodShortage = Math.Max(0, damage.FoodShortage - 0.5);
            damage.TradeDisruption = Math.Max(0, damage.TradeDisruption - 0.25);
            damage.StructuralDamage = Math.Max(0, damage.StructuralDamage - 0.03);
            damage.InfrastructureDamage = Math.Max(0, damage.InfrastructureDamage - 0.05);
        }
    }

    private void ApplySide(WorldState world, Guid conflictId, IReadOnlyList<Npc> members, int casualties, string role)
    {
        if (casualties <= 0 || members.Count == 0) return;
        var candidates = members.Where(n => n.IsAlive).OrderBy(n => StableValue(world.WorldSeed, conflictId, n.Id)).ToList();
        if (candidates.Count == 0) return;

        for (var i = 0; i < casualties; i++)
        {
            var npc = candidates[i % candidates.Count];
            var roll = StableUnit(world.WorldSeed, conflictId, npc.Id, i);
            if (roll < 0.10)
            {
                npc.Die();
                foreach (var relation in npc.Relationships) relation.IsActive = false;
                Record(world, conflictId, npc, WarCasualtyStatus.Dead, $"Meurt au combat comme {role}.");
                RecordFamilyLoss(world, npc);
            }
            else
            {
                var severe = roll < 0.38;
                var condition = new PhysicalCondition
                {
                    Type = severe ? ConditionType.Fracture : ConditionType.Wound,
                    Name = severe ? "Blessure de guerre grave" : "Blessure de guerre",
                    Severity = severe ? 0.65 : 0.35,
                    Pain = severe ? 70 : 40,
                    MobilityPenalty = severe ? 0.35 : 0.15,
                    RecoveryRate = severe ? 0.006 : 0.015,
                    Treatable = true,
                    Contagious = false,
                    OnsetDay = world.Time.Day,
                    ExpectedDurationDays = severe ? 120 : 35
                };
                npc.Conditions.Add(condition);
                npc.History.Add("Guerre", npc.AgeYears, severe ? "Est grièvement blessé pendant la guerre." : "Est blessé pendant la guerre.");
                Record(world, conflictId, npc, severe ? WarCasualtyStatus.SeverelyWounded : WarCasualtyStatus.Wounded, condition.Name + ".");
            }
        }
    }

    private void Record(WorldState world, Guid conflictId, Npc npc, WarCasualtyStatus status, string description)
    {
        _casualties.Add(new WarCasualty { WarOrSiegeId = conflictId, NpcId = npc.Id, Status = status, Day = world.Time.Day, Description = description });
    }

    private static void RecordFamilyLoss(WorldState world, Npc dead)
    {
        if (dead.Birth.FamilyId is not Guid familyId || !world.Families.TryGetValue(familyId, out var family)) return;
        foreach (var memberId in family.MemberIds)
        {
            if (!world.Npcs.TryGetValue(memberId, out var member) || !member.IsAlive || member.Id == dead.Id) continue;
            member.History.Add("Deuil", member.AgeYears, $"Perd {dead.Identity.DisplayName} dans un conflit armé.");
            foreach (var relation in member.Relationships.Where(r => r.ToNpcId == dead.Id && r.IsActive))
                relation.Shift(-0.10, -0.20, -0.05);
        }
    }

    private WarLocationDamage GetLocationDamage(Guid locationId, long day)
    {
        if (_locations.TryGetValue(locationId, out var existing))
        {
            existing.LastUpdatedDay = day;
            return existing;
        }
        var created = new WarLocationDamage { LocationId = locationId, LastUpdatedDay = day };
        _locations[locationId] = created;
        return created;
    }

    private static int StableValue(int seed, Guid first, Guid second)
    {
        unchecked
        {
            var hash = seed ^ 0x9E3779B9;
            foreach (var b in first.ToByteArray()) hash = (hash ^ b) * 16777619;
            foreach (var b in second.ToByteArray()) hash = (hash ^ b) * 16777619;
            hash ^= hash >> 16;
            return hash;
        }
    }

    private static double StableUnit(int seed, Guid conflictId, Guid npcId, int index)
    {
        unchecked
        {
            var hash = StableValue(seed + index * 374761393, conflictId, npcId);
            return (uint)hash / (double)uint.MaxValue;
        }
    }
}
