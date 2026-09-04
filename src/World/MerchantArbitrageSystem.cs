namespace Murim.World;

public sealed class MerchantRun
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid MerchantNpcId { get; init; }
    public Guid ItemId { get; init; }
    public Guid OriginLocationId { get; init; }
    public Guid DestinationLocationId { get; init; }
    public int Quantity { get; set; }
    public double PurchaseUnitPrice { get; init; }
    public double ExpectedSaleUnitPrice { get; init; }
    public double TransportBudget { get; init; }
    public long DepartureTick { get; init; }
    public long ArrivalTick { get; set; }
    public int DelayMinutes { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.InTransit;
    public string LastEvent { get; set; } = string.Empty;
}

public sealed class MerchantArbitrageSystem
{
    private readonly Dictionary<Guid, MerchantRun> _runs = new();
    public IReadOnlyDictionary<Guid, MerchantRun> Runs => _runs;

    public void AdvanceDay(WorldState world)
    {
        StartRuns(world);
        ResolveRuns(world);
    }

    private void StartRuns(WorldState world)
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
                    .SelectMany(b => b.Stock.Where(s => s.ItemId == item.Id && s.Quantity >= 2).Select(s => (Business:b, Stock:s))).FirstOrDefault();
                if (source.Business is null) continue;
                var purchase = world.Commerce.CurrentPrice(world, source.Business, source.Stock);
                foreach (var destination in world.Geography.Locations.Values.Where(l => l.Id != origin))
                {
                    var target = world.Commerce.Businesses.Values.Where(b => b.Active && b.LocationId == destination.Id)
                        .SelectMany(b => b.Stock.Where(s => s.ItemId == item.Id).Select(s => (Business:b, Stock:s)))
                        .OrderByDescending(x => x.Stock.TargetQuantity - x.Stock.Quantity).FirstOrDefault();
                    if (target.Business is null) continue;
                    var distance = world.Geography.GetRouteDistance(origin, destination.Id);
                    if (double.IsInfinity(distance)) continue;
                    var sale = world.Commerce.CurrentPrice(world, target.Business, target.Stock);
                    var qty = Math.Min(3, source.Stock.Quantity);
                    var transport = qty * item.WeightKg * .04 + distance * .08;
                    var margin = (sale - purchase) * qty - transport;
                    if (margin <= Math.Max(.5, purchase * qty * .15) || merchant.Wealth < purchase * qty + transport) continue;
                    if (best is null || margin > (best.ExpectedSaleUnitPrice - best.PurchaseUnitPrice) * best.Quantity - best.TransportBudget)
                        best = new MerchantRun { MerchantNpcId=merchant.Id, ItemId=item.Id, OriginLocationId=origin, DestinationLocationId=destination.Id, Quantity=qty, PurchaseUnitPrice=purchase, ExpectedSaleUnitPrice=sale, TransportBudget=transport, DepartureTick=world.Time.Day*120L+world.Time.MinuteOfDay };
                }
            }
            if (best is null) continue;
            var sourceStock = world.Commerce.Businesses.Values.SelectMany(b => b.Stock.Select(s => (Business:b, Stock:s))).FirstOrDefault(x => x.Business.LocationId == best.OriginLocationId && x.Stock.ItemId == best.ItemId && x.Stock.Quantity >= best.Quantity);
            if (sourceStock.Business is null) continue;
            var total = best.PurchaseUnitPrice * best.Quantity + best.TransportBudget;
            if (merchant.Wealth < total) continue;
            merchant.ApplyWealthChange(-total);
            sourceStock.Stock.Quantity -= best.Quantity;
            var minutes = Math.Max(30, (int)Math.Ceiling(world.Geography.GetRouteDistance(best.OriginLocationId, best.DestinationLocationId) / 7 * 60));
            var weather = world.Environment.Get(best.OriginLocationId);
            if (weather.RoadsImpacted) minutes = (int)Math.Ceiling(minutes * 1.35);
            else if (weather.Weather is WeatherType.Rain or WeatherType.Fog) minutes = (int)Math.Ceiling(minutes * 1.15);
            else if (weather.Weather == WeatherType.Storm) minutes = (int)Math.Ceiling(minutes * 1.6);
            best.ArrivalTick = best.DepartureTick + minutes;
            best.Status = ShipmentStatus.InTransit;
            best.LastEvent = $"Départ vers {world.Geography.Locations[best.DestinationLocationId].Name} avec {best.Quantity} unité(s).";
            _runs[best.Id] = best;
            merchant.History.Add("Commerce", 0, best.LastEvent);
        }
    }

    private void ResolveRuns(WorldState world)
    {
        var now = world.Time.Day * 120L + world.Time.MinuteOfDay;
        foreach (var run in _runs.Values.Where(r => r.Status == ShipmentStatus.InTransit).ToList())
        {
            if (!world.Npcs.TryGetValue(run.MerchantNpcId, out var merchant) || !merchant.IsAlive) { run.Status=ShipmentStatus.Lost; run.LastEvent="Le marchand disparaît avant son retour."; continue; }
            if (now < run.ArrivalTick) continue;
            var weather = world.Environment.Get(run.DestinationLocationId);
            var risk = Math.Clamp(.01 + (world.Geography.Locations[run.DestinationLocationId].DangerLevel * .008) + (weather.RoadsImpacted ? .06 : 0) + (weather.Weather == WeatherType.Storm ? .08 : 0), 0, .55);
            var roll = new Random(HashCode.Combine(world.WorldSeed, run.Id, (int)run.ArrivalTick)).NextDouble();
            if (roll < risk)
            {
                if (roll < risk * .35) { run.Quantity = Math.Max(0, run.Quantity - 1); run.LastEvent="Une unité de la cargaison est perdue pendant le voyage."; }
                else { run.DelayMinutes = Math.Max(30, (int)Math.Ceiling(60 * (risk + .1))); run.ArrivalTick += run.DelayMinutes; run.LastEvent="Un incident impose un détour et retarde la livraison."; merchant.History.Add("Commerce", run.DelayMinutes, run.LastEvent); continue; }
            }
            var business = world.Commerce.Businesses.Values.Where(b => b.Active && b.LocationId == run.DestinationLocationId && b.OwnerNpcId != merchant.Id)
                .SelectMany(b => b.Stock.Where(s => s.ItemId == run.ItemId).Select(s => (Business:b, Stock:s)))
                .OrderByDescending(x => x.Stock.TargetQuantity - x.Stock.Quantity).FirstOrDefault();
            if (business.Business is null || run.Quantity <= 0) { run.Status = run.Quantity <= 0 ? ShipmentStatus.Lost : ShipmentStatus.Cancelled; run.LastEvent = "Aucun acheteur adapté ne peut recevoir la cargaison."; continue; }
            var sale = world.Commerce.CurrentPrice(world, business.Business, business.Stock);
            if (!world.Npcs.TryGetValue(business.Business.OwnerNpcId, out var buyer) || !buyer.IsAlive || buyer.Wealth < sale * run.Quantity) { run.LastEvent="Le commerce destinataire ne dispose pas de liquidités suffisantes."; continue; }
            buyer.ApplyWealthChange(-sale * run.Quantity);
            merchant.ApplyWealthChange(sale * run.Quantity);
            business.Stock.Quantity += run.Quantity;
            run.Status = ShipmentStatus.Delivered;
            run.LastEvent = $"Livraison vendue à {business.Business.Name} pour {sale:0.##} par unité.";
            merchant.SetLocation(run.DestinationLocationId);
            merchant.History.Add("Commerce", 0, run.LastEvent);
        }
    }

    public string Describe(WorldState world, Npc npc)
    {
        var active = _runs.Values.Where(r => r.MerchantNpcId == npc.Id && r.Status == ShipmentStatus.InTransit);
        return active.Any() ? string.Join(" ; ", active.Select(r => $"Route commerciale : {r.Quantity} unité(s) vers {world.Geography.Locations[r.DestinationLocationId].Name}, arrivée prévue jour {r.ArrivalTick / 120L}, minute {r.ArrivalTick % 120}.")) : "Aucun voyage marchand en cours.";
    }
}
