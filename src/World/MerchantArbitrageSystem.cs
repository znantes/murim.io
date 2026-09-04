namespace Murim.World;

public sealed class MerchantRun
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid MerchantNpcId { get; init; }
    public Guid ItemId { get; init; }
    public Guid OriginLocationId { get; init; }
    public Guid DestinationLocationId { get; init; }
    public int Quantity { get; init; }
    public double PurchaseUnitPrice { get; init; }
    public double ExpectedSaleUnitPrice { get; init; }
    public double TransportBudget { get; init; }
    public long DepartureDay { get; init; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.InTransit;
}

public sealed class MerchantArbitrageSystem
{
    private readonly Dictionary<Guid, MerchantRun> _runs = new();
    public IReadOnlyDictionary<Guid, MerchantRun> Runs => _runs;

    public void AdvanceDay(WorldState world)
    {
        foreach (var merchant in world.Npcs.Values.Where(n => n.IsAlive && n.AgeYears >= 16 && n.Profession.Type == ProfessionType.Merchant))
        {
            if (_runs.Values.Any(r => r.MerchantNpcId == merchant.Id && r.Status == ShipmentStatus.InTransit)) continue;
            if (merchant.CurrentLocationId is null || merchant.Wealth < 3) continue;

            MerchantRun? best = null;
            foreach (var item in world.Inventory.Items.Values)
            {
                var origin = merchant.CurrentLocationId.Value;
                var source = world.Commerce.Businesses.Values.Where(b => b.Active && b.LocationId == origin)
                    .SelectMany(b => b.Stock.Where(s => s.ItemId == item.Id && s.Quantity >= 2).Select(s => (Business:b, Stock:s)))
                    .OrderBy(x => x.Stock.Quantity).FirstOrDefault();
                if (source.Business is null) continue;
                var buy = source.Business.Stock.First(s => s.ItemId == item.Id);
                var purchase = world.Commerce.CurrentPrice(world, source.Business, buy);
                foreach (var destination in world.Geography.Locations.Values.Where(l => l.Id != origin))
                {
                    var target = world.Commerce.Businesses.Values.Where(b => b.Active && b.LocationId == destination.Id)
                        .SelectMany(b => b.Stock.Where(s => s.ItemId == item.Id).Select(s => (Business:b, Stock:s)))
                        .OrderByDescending(x => x.Stock.TargetQuantity - x.Stock.Quantity).FirstOrDefault();
                    if (target.Business is null) continue;
                    var sale = world.Commerce.CurrentPrice(world, target.Business, target.Stock);
                    var distance = world.Geography.GetRouteDistance(origin, destination.Id);
                    if (double.IsInfinity(distance)) continue;
                    var qty = Math.Min(3, buy.Quantity);
                    var transport = qty * item.WeightKg * .04 + distance * .08;
                    var margin = sale * qty - purchase * qty - transport;
                    if (margin <= Math.Max(0.5, purchase * qty * .15)) continue;
                    if (merchant.Wealth < purchase * qty + transport) continue;
                    if (best is null || margin > (best.ExpectedSaleUnitPrice - best.PurchaseUnitPrice) * best.Quantity - best.TransportBudget)
                        best = new MerchantRun { MerchantNpcId=merchant.Id, ItemId=item.Id, OriginLocationId=origin, DestinationLocationId=destination.Id, Quantity=qty, PurchaseUnitPrice=purchase, ExpectedSaleUnitPrice=sale, TransportBudget=transport, DepartureDay=world.Time.Day };
                }
            }
            if (best is null) continue;
            var stock = world.Commerce.Businesses.Values.SelectMany(b => b.Stock.Select(s => (Business:b,Stock:s))).FirstOrDefault(x => x.Business.LocationId == best.OriginLocationId && x.Stock.ItemId == best.ItemId && x.Stock.Quantity >= best.Quantity);
            if (stock.Business is null) continue;
            var total = best.PurchaseUnitPrice * best.Quantity + best.TransportBudget;
            if (merchant.Wealth < total) continue;
            merchant.ApplyWealthChange(-total);
            stock.Stock.Quantity -= best.Quantity;
            var arrival = world.Time.Day * 120L + world.Time.MinuteOfDay + Math.Max(30, (int)Math.Ceiling(world.Geography.GetRouteDistance(best.OriginLocationId,best.DestinationLocationId)/7*60));
            var run = best;
            _runs[run.Id] = run;
            merchant.SetLocation(best.DestinationLocationId);
            merchant.History.Add("Commerce", 0, $"Voyage marchand vers {world.Geography.Locations[best.DestinationLocationId].Name} avec {best.Quantity} unité(s) de {world.Inventory.Items[best.ItemId].Name}.");
            run.Status = ShipmentStatus.InTransit;
        }

        foreach (var run in _runs.Values.Where(r => r.Status == ShipmentStatus.InTransit).ToList())
        {
            if (!world.Npcs.TryGetValue(run.MerchantNpcId, out var merchant) || !merchant.IsAlive) { run.Status=ShipmentStatus.Cancelled; continue; }
            var business = world.Commerce.Businesses.Values.Where(b => b.Active && b.LocationId == run.DestinationLocationId).SelectMany(b => b.Stock.Where(s => s.ItemId == run.ItemId).Select(s => b)).FirstOrDefault();
            if (business is null) continue;
            var stock = business.Stock.First(s => s.ItemId == run.ItemId);
            var actual = world.Commerce.CurrentPrice(world,business,stock);
            var revenue = actual * run.Quantity;
            merchant.ApplyWealthChange(revenue);
            run.Status=ShipmentStatus.Delivered;
            merchant.History.Add("Commerce", 0, $"Revente de {run.Quantity} unité(s) à {actual:0.##} par unité.");
        }
    }

    public string Describe(WorldState world, Npc npc)
    {
        var active = _runs.Values.Where(r => r.MerchantNpcId == npc.Id && r.Status == ShipmentStatus.InTransit);
        return active.Any() ? string.Join(" ; ", active.Select(r => $"Route commerciale : {r.Quantity} unité(s) vers {world.Geography.Locations[r.DestinationLocationId].Name}.")) : "Aucun voyage marchand en cours.";
    }
}
