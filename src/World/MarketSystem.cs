namespace Murim.World;

public sealed class MarketQuote
{
    public Guid LocationId { get; init; }
    public Guid ItemId { get; init; }
    public double Price { get; init; }
    public double Supply { get; init; }
    public double Demand { get; init; }
    public double Pressure { get; init; }
    public long Day { get; init; }
}

public sealed class MarketSystem
{
    private readonly Dictionary<(Guid LocationId, Guid ItemId), MarketQuote> _quotes = new();
    public IReadOnlyDictionary<(Guid LocationId, Guid ItemId), MarketQuote> Quotes => _quotes;

    public double Price(WorldState world, Guid locationId, Guid itemId, double baseValue)
    {
        var stock = world.Commerce.Businesses.Values.Where(b => b.LocationId == locationId && b.Active)
            .SelectMany(b => b.Stock.Where(s => s.ItemId == itemId).Select(s => (double)s.Quantity)).Sum();
        var transit = world.Logistics.Shipments.Values.Where(s => s.Status == ShipmentStatus.InTransit && s.DestinationLocationId == locationId && s.ItemId == itemId).Sum(s => (double)s.RemainingQuantity);
        var recentDemand = world.Commerce.Transactions.Where(t => t.ItemId == itemId && t.Day >= world.Time.Day - 7 && world.Commerce.Businesses.TryGetValue(t.BusinessId, out var b) && b.LocationId == locationId).Sum(t => t.Quantity);
        var supply = stock + transit * .65;
        var demand = Math.Max(1, recentDemand + 2);
        var pressure = Math.Clamp((demand - supply) / demand, -1, 2);
        return Math.Max(.01, baseValue * (1 + pressure * .75));
    }

    public void AdvanceDay(WorldState world)
    {
        foreach (var location in world.Geography.Locations.Values)
        {
            foreach (var item in world.Inventory.Items.Values)
            {
                var businesses = world.Commerce.Businesses.Values.Where(b => b.LocationId == location.Id && b.Active).ToList();
                if (businesses.Count == 0) continue;
                var stock = businesses.SelectMany(b => b.Stock.Where(s => s.ItemId == item.Id)).Sum(s => (double)s.Quantity);
                var transit = world.Logistics.Shipments.Values.Where(s => s.Status == ShipmentStatus.InTransit && s.DestinationLocationId == location.Id && s.ItemId == item.Id).Sum(s => (double)s.RemainingQuantity);
                var demand = Math.Max(1, world.Commerce.Transactions.Where(t => t.ItemId == item.Id && t.Day >= world.Time.Day - 7 && world.Commerce.Businesses.TryGetValue(t.BusinessId, out var b) && b.LocationId == location.Id).Sum(t => t.Quantity) + 2);
                var supply = stock + transit * .65;
                var pressure = Math.Clamp((demand - supply) / demand, -1, 2);
                _quotes[(location.Id, item.Id)] = new MarketQuote { LocationId=location.Id, ItemId=item.Id, Price=Math.Max(.01,item.BaseValue*(1+pressure*.75)), Supply=supply, Demand=demand, Pressure=pressure, Day=world.Time.Day };
            }
        }
    }

    public MarketQuote? Quote(Guid locationId, Guid itemId) => _quotes.GetValueOrDefault((locationId, itemId));
    public string Describe(WorldState world, Guid locationId, Guid itemId)
    {
        var q = Quote(locationId, itemId);
        if (q is null) return "Marché local indisponible.";
        var state = q.Pressure > .35 ? "forte demande / pénurie" : q.Pressure < -.2 ? "surplus" : "marché équilibré";
        var name = world.Inventory.Items.TryGetValue(itemId, out var item) ? item.Name : "objet";
        return $"{name} : {q.Price:0.##} — offre {q.Supply:0.#}, demande {q.Demand:0.#} ({state}).";
    }
}
