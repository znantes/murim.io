using Murim.Simulation;

namespace Murim.World;

public sealed class NpcAutonomySystem
{
    private sealed class PendingTravel
    {
        public Guid DestinationId { get; init; }
        public Guid BuildingId { get; init; }
        public int RemainingMinutes { get; set; }
        public double DistanceKm { get; init; }
    }

    private readonly Dictionary<Guid, PendingTravel> _pendingTravels = new();
    private int _minutesUntilNextTick;
    public List<string> LastActions { get; } = new();
    public int TickMinutes { get; set; } = 30;

    public void Advance(WorldState world, int minutes)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (minutes < 0) throw new ArgumentOutOfRangeException(nameof(minutes));

        AdvancePendingTravels(world, minutes);
        _minutesUntilNextTick -= minutes;
        while (_minutesUntilNextTick <= 0)
        {
            _minutesUntilNextTick += Math.Max(1, TickMinutes);
            Tick(world);
        }
    }

    private void AdvancePendingTravels(WorldState world, int minutes)
    {
        foreach (var pair in _pendingTravels.ToList())
        {
            var npcId = pair.Key;
            var travel = pair.Value;
            if (!world.Npcs.TryGetValue(npcId, out var npc) || !npc.IsAlive)
            {
                _pendingTravels.Remove(npcId);
                continue;
            }

            travel.RemainingMinutes -= minutes;
            if (travel.RemainingMinutes > 0) continue;

            if (!world.Geography.Locations.TryGetValue(travel.DestinationId, out var destination))
            {
                _pendingTravels.Remove(npcId);
                continue;
            }

            npc.SetLocation(travel.DestinationId);
            npc.History.Add("Déplacement", npc.AgeYears, $"Arrive à {destination.Name} après un trajet de {travel.DistanceKm:0.#} km.");
            LastActions.Add($"{npc.Identity.DisplayName} arrive à {destination.Name}.");
            _pendingTravels.Remove(npcId);
        }
    }

    private void Tick(WorldState world)
    {
        LastActions.Clear();
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive && n.Id != world.PlayerNpc?.Id).OrderBy(n => n.Id))
        {
            if (npc.AgeYears < 4) continue;
            if (_pendingTravels.ContainsKey(npc.Id)) continue;
            if (TrySatisfyCriticalNeed(world, npc)) continue;
            if (TryLearnUsefulLocation(world, npc)) continue;
            if (TryTravelToWork(world, npc)) continue;
            TryWork(world, npc);
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

    private bool TryLearnUsefulLocation(WorldState world, Npc npc)
    {
        if (npc.CurrentLocationId is not Guid currentLocationId) return false;
        if (!world.Employment.Contracts.TryGetValue(npc.Id, out var contract) || contract.BuildingId is not Guid buildingId) return false;
        if (!world.Buildings.Buildings.TryGetValue(buildingId, out var building)) return false;
        if (npc.KnownLocationIds.Contains(building.LocationId)) return false;

        var teacher = world.Npcs.Values
            .Where(other => other.IsAlive && other.Id != npc.Id && other.CurrentLocationId == currentLocationId && other.KnownLocationIds.Contains(building.LocationId))
            .OrderByDescending(other => other.Relationships.FirstOrDefault(r => r.ToNpcId == npc.Id)?.Trust ?? 0.2)
            .ThenBy(other => other.Id)
            .FirstOrDefault();
        if (teacher is null) return false;

        var trust = teacher.Relationships.FirstOrDefault(r => r.ToNpcId == npc.Id)?.Trust ?? 0.2;
        if (trust < 0.1) return false;

        npc.DiscoverLocation(building.LocationId);
        npc.Learn(new KnowledgeEntry
        {
            EntityId = building.LocationId,
            Kind = KnowledgeKind.Location,
            Confidence = Math.Clamp(0.55 + trust * 0.4, 0.25, 0.95),
            LearnedDay = world.Time.Day,
            SourceNpcId = teacher.Id,
            Summary = $"Indique où se trouve {building.Name}."
        });
        npc.History.Add("Connaissance", npc.AgeYears, $"Apprend de {teacher.Identity.DisplayName} où se trouve {building.Name}.");
        teacher.History.Add("Connaissance", teacher.AgeYears, $"Indique à {npc.Identity.DisplayName} où se trouve {building.Name}.");
        LastActions.Add($"{npc.Identity.DisplayName} apprend où aller travailler.");
        return true;
    }

    private bool TryTravelToWork(WorldState world, Npc npc)
    {
        if (world.Time.Period is Murim.Simulation.TimePeriod.Night || npc.Needs.Fatigue >= 65 || !world.Employment.IsEmployed(npc)) return false;
        if (!world.Employment.Contracts.TryGetValue(npc.Id, out var contract)) return false;
        if (contract.BuildingId is not Guid buildingId || !world.Buildings.Buildings.TryGetValue(buildingId, out var building)) return false;
        if (npc.CurrentLocationId == building.LocationId) return false;
        if (!npc.KnownLocationIds.Contains(building.LocationId)) return false;

        var plan = world.Travel.Plan(world, npc, building.LocationId, MovementMethod.Walk);
        if (plan is null) return false;

        _pendingTravels[npc.Id] = new PendingTravel
        {
            DestinationId = building.LocationId,
            BuildingId = building.Id,
            RemainingMinutes = plan.DurationMinutes,
            DistanceKm = plan.DistanceKm
        };
        npc.History.Add("Déplacement", npc.AgeYears, $"Part vers {world.Geography.Locations[building.LocationId].Name} pour rejoindre son travail ({plan.DistanceKm:0.#} km, environ {plan.DurationMinutes} min à pied).");
        LastActions.Add($"{npc.Identity.DisplayName} part travailler.");
        return true;
    }

    private bool TryWork(WorldState world, Npc npc)
    {
        // Employment.WorkHour pays exactly one hour. Only execute it on the hour so
        // the 30-minute autonomy tick cannot accidentally double an NPC's wage.
        if (world.Time.Period is Murim.Simulation.TimePeriod.Night || world.Time.MinuteOfDay % 60 != 0 || npc.Needs.Fatigue >= 65 || !world.Employment.IsEmployed(npc)) return false;
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
}
