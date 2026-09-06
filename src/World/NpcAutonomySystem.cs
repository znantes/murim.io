using Murim.Simulation;

namespace Murim.World;

public sealed class NpcAutonomySystem
{
    private int _minutesUntilNextTick;
    public List<string> LastActions { get; } = new();
    public int TickMinutes { get; set; } = 30;

    public void Advance(WorldState world, int minutes)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));
        _minutesUntilNextTick -= minutes;
        while (_minutesUntilNextTick <= 0)
        {
            _minutesUntilNextTick += Math.Max(1, TickMinutes);
            Tick(world);
        }
    }

    private void Tick(WorldState world)
    {
        LastActions.Clear();
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive && n.Id != world.PlayerNpc?.Id).OrderBy(n => n.Id))
        {
            if (npc.AgeYears < 4) continue;
            var random = new Random(DeterministicRandomSeed.Create(world.WorldSeed + 71, world.Time.Day * 120 + world.Time.MinuteOfDay, npc.Id));
            if (TrySatisfyCriticalNeed(world, npc)) continue;
            if (TryWork(world, npc)) continue;
            if (random.NextDouble() < 0.35) TrySocialize(world, npc, random);
        }
    }

    private bool TrySatisfyCriticalNeed(WorldState world, Npc npc)
    {
        if (npc.Needs.Thirst >= 70 && TryConsume(world, npc, ItemCategory.Food, "Eau", 25, "boit")) return true;
        if (npc.Needs.Hunger >= 70 && TryConsume(world, npc, ItemCategory.Food, "Pain de campagne", 35, "mange")) return true;
        if (npc.Needs.Hunger >= 70 && TryConsumeFirst(world, npc, ItemCategory.Food, 25, "mange")) return true;
        if (npc.Needs.Fatigue >= 75 || npc.Needs.Sleep >= 75)
        {
            npc.Needs.SleepFor(35);
            npc.History.Add("Repos", npc.AgeYears, "Prend un temps de repos pour récupérer.");
            LastActions.Add($"{npc.Identity.DisplayName} se repose.");
            return true;
        }
        return false;
    }

    private bool TryConsume(WorldState world, Npc npc, ItemCategory category, string preferredName, double relief, string verb)
    {
        var item = world.Inventory.FindByName(preferredName);
        if (item is null || item.Category != category || !item.Consumable) return false;
        var entry = npc.Inventory.Entries.FirstOrDefault(e => e.ItemId == item.Id);
        if (entry is null || entry.Quantity <= 0) return false;
        if (!npc.Inventory.Remove(item.Id)) return false;
        if (preferredName == "Eau") npc.Needs.Drink(relief);
        else npc.Needs.Eat(relief);
        npc.History.Add("Survie", npc.AgeYears, $"{verb} {item.Name}.");
        LastActions.Add($"{npc.Identity.DisplayName} {verb} {item.Name}.");
        return true;
    }

    private bool TryConsumeFirst(WorldState world, Npc npc, ItemCategory category, double relief, string verb)
    {
        foreach (var entry in npc.Inventory.Entries.ToList())
        {
            if (!world.Inventory.Items.TryGetValue(entry.ItemId, out var item) || item.Category != category || !item.Consumable) continue;
            if (!npc.Inventory.Remove(item.Id)) continue;
            npc.Needs.Eat(relief);
            npc.History.Add("Survie", npc.AgeYears, $"{verb} {item.Name}.");
            LastActions.Add($"{npc.Identity.DisplayName} {verb} {item.Name}.");
            return true;
        }
        return false;
    }

    private bool TryWork(WorldState world, Npc npc)
    {
        if (world.Time.Period is Murim.Simulation.TimePeriod.Night || npc.Needs.Fatigue >= 65 || !world.Employment.IsEmployed(npc)) return false;
        if (!world.Employment.Contracts.TryGetValue(npc.Id, out var contract)) return false;
        if (contract.BuildingId is not Guid buildingId || !world.Buildings.Buildings.TryGetValue(buildingId, out var building)) return false;
        if (npc.CurrentLocationId != building.LocationId) return false;
        if (npc.CurrentBuildingId != building.Id)
        {
            if (!world.Buildings.CanEnter(world, npc, building, out _)) return false;
            npc.EnterBuilding(building.Id);
        }
        if (!world.Employment.WorkHour(world, npc, out var wage)) return false;
        npc.Needs.Exert(4);
        npc.History.Add("Travail", npc.AgeYears, $"Travaille comme {contract.ProfessionType} et gagne {wage:0.##}.");
        LastActions.Add($"{npc.Identity.DisplayName} travaille.");
        return true;
    }

    private bool TrySocialize(WorldState world, Npc npc, Random random)
    {
        if (npc.CurrentLocationId is not Guid locationId) return false;
        var other = world.Npcs.Values.Where(n => n.IsAlive && n.Id != npc.Id && n.Id != world.PlayerNpc?.Id && n.CurrentLocationId == locationId && n.AgeYears >= 4).OrderBy(n => n.Id).ToList();
        if (other.Count == 0) return false;
        var target = other[random.Next(other.Count)];
        var relationship = npc.Relationships.FirstOrDefault(r => r.ToNpcId == target.Id && r.IsActive);
        if (relationship is null)
        {
            relationship = new Relationship { FromNpcId = npc.Id, ToNpcId = target.Id, Type = RelationshipType.Acquaintance, Affinity = 0.05, Trust = 0.1, Respect = 0.1 };
            npc.Relationships.Add(relationship);
            var reverse = target.Relationships.FirstOrDefault(r => r.ToNpcId == npc.Id && r.IsActive);
            if (reverse is null) target.Relationships.Add(new Relationship { FromNpcId = target.Id, ToNpcId = npc.Id, Type = RelationshipType.Acquaintance, Affinity = 0.05, Trust = 0.1, Respect = 0.1 });
        }
        relationship.Shift(0.02, 0.01, 0.01);
        npc.History.Add("Vie sociale", npc.AgeYears, $"Échange quelques mots avec {target.Identity.DisplayName}.");
        LastActions.Add($"{npc.Identity.DisplayName} parle avec {target.Identity.DisplayName}.");
        return true;
    }
}
