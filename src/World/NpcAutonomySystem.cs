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

        LastActions.Clear();
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

            npc.SetLocation(destination.Id);
            npc.History.Add("Déplacement", npc.AgeYears, $"Arrive à {destination.Name} après un trajet de {travel.DistanceKm:0.#} km.");
            LastActions.Add($"{npc.Identity.DisplayName} arrive à {destination.Name}.");
            _pendingTravels.Remove(npcId);
        }
    }

    private void Tick(WorldState world)
    {
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive && n.Id != world.PlayerNpc?.Id).OrderBy(n => n.Id))
        {
            if (npc.AgeYears < 4) continue;
            if (_pendingTravels.ContainsKey(npc.Id)) continue;
            if (TrySatisfyCriticalNeed(world, npc)) continue;
            if (TryAcquireCriticalNeed(world, npc)) continue;
            if (TrySeekNeedAssistance(world, npc)) continue;
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

    private bool TryAcquireCriticalNeed(WorldState world, Npc npc)
    {
        if (npc.Needs.Thirst >= 70 && TryBuyAndConsume(world, npc, "Eau", ItemCategory.Food, 25, "boit")) return true;
        if (npc.Needs.Hunger >= 70 && TryBuyAndConsume(world, npc, "Pain de campagne", ItemCategory.Food, 35, "mange")) return true;
        if (npc.Needs.Hunger >= 70 && TryBuyFirstFoodAndConsume(world, npc, 25)) return true;
        return false;
    }

    private bool TryBuyAndConsume(WorldState world, Npc npc, string itemName, ItemCategory category, double relief, string verb)
    {
        var item = world.Inventory.FindByName(itemName);
        if (item is null || item.Category != category || !item.Consumable) return false;
        var business = FindBusinessSelling(world, npc, item.Id);
        if (business is null) return false;
        if (!world.Commerce.Buy(world, npc, business, item.Id, 1, out var total)) return false;
        if (!npc.Inventory.Remove(item.Id)) return false;

        if (verb == "boit") npc.Needs.Drink(relief);
        else npc.Needs.Eat(relief);
        npc.History.Add("Commerce", npc.AgeYears, $"Achète {item.Name} pour {total:0.##} puis {verb} {item.Name}.");
        LastActions.Add($"{npc.Identity.DisplayName} achète {item.Name} et le consomme.");
        return true;
    }

    private bool TryBuyFirstFoodAndConsume(WorldState world, Npc npc, double relief)
    {
        var business = world.Commerce.Businesses.Values
            .Where(b => b.Active && b.LocationId == npc.CurrentLocationId && b.IsOpen(world.Time.Period) && b.OwnerNpcId != npc.Id)
            .OrderBy(b => b.Type == CommerceType.FoodStall ? 0 : 1)
            .ThenBy(b => b.Id)
            .FirstOrDefault(b => b.Stock.Any(s => s.Quantity > 0 && world.Inventory.Items.TryGetValue(s.ItemId, out var item) && item.Category == ItemCategory.Food && item.Consumable));
        if (business is null) return false;

        foreach (var stock in business.Stock.Where(s => s.Quantity > 0).ToList())
        {
            if (!world.Inventory.Items.TryGetValue(stock.ItemId, out var item) || item.Category != ItemCategory.Food || !item.Consumable) continue;
            if (!world.Commerce.Buy(world, npc, business, item.Id, 1, out var total)) continue;
            if (!npc.Inventory.Remove(item.Id)) continue;
            npc.Needs.Eat(relief);
            npc.History.Add("Commerce", npc.AgeYears, $"Achète {item.Name} pour {total:0.##} puis le mange.");
            LastActions.Add($"{npc.Identity.DisplayName} achète {item.Name} et le consomme.");
            return true;
        }
        return false;
    }

    private static CommerceBusiness? FindBusinessSelling(WorldState world, Npc npc, Guid itemId)
        => world.Commerce.Businesses.Values
            .Where(b => b.Active && b.LocationId == npc.CurrentLocationId && b.OwnerNpcId != npc.Id && b.IsOpen(world.Time.Period))
            .Where(b => b.Stock.Any(s => s.ItemId == itemId && s.Quantity > 0))
            .OrderBy(b => b.Type == CommerceType.FoodStall ? 0 : 1)
            .ThenBy(b => b.Id)
            .FirstOrDefault();

    private bool TrySeekNeedAssistance(WorldState world, Npc npc)
    {
        var urgentThirst = npc.Needs.Thirst >= 85;
        var urgentHunger = npc.Needs.Hunger >= 85;
        if (!urgentThirst && !urgentHunger) return false;
        if (npc.CurrentLocationId is not Guid locationId) return false;

        var preferred = urgentThirst
            ? world.Inventory.FindByName("Eau")
            : world.Inventory.FindByName("Pain de campagne");
        if (preferred is null || !preferred.Consumable || preferred.Category != ItemCategory.Food) return false;

        var helper = world.Npcs.Values
            .Where(other => other.IsAlive && other.Id != npc.Id && other.CurrentLocationId == locationId)
            .Where(other => other.Inventory.Entries.Any(e => e.ItemId == preferred.Id && e.Quantity > 0))
            .Select(other => new
            {
                Npc = other,
                Trust = other.Relationships.FirstOrDefault(r => r.ToNpcId == npc.Id)?.Trust ?? 0
            })
            .Where(x => x.Trust >= 0.25)
            .OrderByDescending(x => x.Trust)
            .ThenBy(x => x.Npc.Id)
            .FirstOrDefault();
        if (helper is null) return false;

        if (!helper.Npc.Inventory.Remove(preferred.Id)) return false;
        npc.Inventory.Add(preferred);
        var relief = urgentThirst ? 25 : 35;
        if (urgentThirst) npc.Needs.Drink(relief);
        else npc.Needs.Eat(relief);

        npc.History.Add("Entraide", npc.AgeYears, $"Reçoit {preferred.Name} de {helper.Npc.Identity.DisplayName} pour soulager un besoin urgent.");
        helper.Npc.History.Add("Entraide", helper.Npc.AgeYears, $"Aide {npc.Identity.DisplayName} en lui donnant {preferred.Name}.");
        LastActions.Add($"{helper.Npc.Identity.DisplayName} aide {npc.Identity.DisplayName}.");
        return true;
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
        if (world.Time.Period is TimePeriod.Night || npc.Needs.Fatigue >= 65 || !world.Employment.IsEmployed(npc)) return false;
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
        if (world.Time.Period is TimePeriod.Night || world.Time.MinuteOfDay % 60 != 0 || npc.Needs.Fatigue >= 65 || !world.Employment.IsEmployed(npc)) return false;
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
