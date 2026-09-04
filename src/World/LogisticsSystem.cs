namespace Murim.World;

public enum ShipmentStatus { Pending, InTransit, Delivered, Lost, Cancelled }

public sealed class SupplyOrder
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid BusinessId { get; init; }
    public Guid BuyerNpcId { get; init; }
    public Guid SupplierNpcId { get; init; }
    public Guid ItemId { get; init; }
    public int Quantity { get; init; }
    public double MaxUnitCost { get; init; }
    public long CreatedDay { get; init; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
}

public sealed class Shipment
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid OrderId { get; init; }
    public Guid OriginLocationId { get; init; }
    public Guid DestinationLocationId { get; init; }
    public Guid ItemId { get; init; }
    public int Quantity { get; init; }
    public int RemainingQuantity { get; set; }
    public Guid SupplierNpcId { get; init; }
    public Guid BuyerNpcId { get; init; }
    public MovementMethod Method { get; init; }
    public double DistanceKm { get; init; }
    public double TransportCost { get; init; }
    public int Danger { get; init; }
    public long DepartureDay { get; init; }
    public int DepartureMinute { get; init; }
    public long ArrivalDay { get; init; }
    public int ArrivalMinute { get; init; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.InTransit;
    public string LastEvent { get; set; } = string.Empty;
}

public sealed class LogisticsRecord
{
    public long Day { get; init; }
    public Guid ShipmentId { get; init; }
    public ShipmentStatus Status { get; init; }
    public int Quantity { get; init; }
    public string Note { get; init; } = string.Empty;
}

public sealed class LogisticsSystem
{
    private readonly Dictionary<Guid, SupplyOrder> _orders = new();
    private readonly Dictionary<Guid, Shipment> _shipments = new();
    public IReadOnlyDictionary<Guid, SupplyOrder> Orders => _orders;
    public IReadOnlyDictionary<Guid, Shipment> Shipments => _shipments;
    public List<LogisticsRecord> History { get; } = new();

    public bool HasPendingOrder(Guid businessId, Guid itemId) => _orders.Values.Any(o => o.BusinessId == businessId && o.ItemId == itemId && o.Status is ShipmentStatus.Pending or ShipmentStatus.InTransit);

    public SupplyOrder? CreateOrder(WorldState world, CommerceBusiness business, Guid itemId, int quantity)
    {
        if (quantity <= 0 || !business.Active || HasPendingOrder(business.Id, itemId)) return null;
        var buyer = world.Npcs.GetValueOrDefault(business.OwnerNpcId);
        if (buyer is null || !buyer.IsAlive) return null;
        var supplier = world.Npcs.Values.Where(n => n.IsAlive && n.Id != buyer.Id && n.CurrentLocationId is not null)
            .Where(n => n.Inventory.Entries.Any(e => e.ItemId == itemId && e.Quantity > 0))
            .OrderBy(n => world.Geography.GetRouteDistance(business.LocationId, n.CurrentLocationId!.Value)).FirstOrDefault();
        if (supplier is null) return null;
        var order = new SupplyOrder { BusinessId = business.Id, BuyerNpcId = buyer.Id, SupplierNpcId = supplier.Id, ItemId = itemId, Quantity = quantity, MaxUnitCost = world.Inventory.Items.TryGetValue(itemId, out var item) ? item.BaseValue * 1.5 : 0, CreatedDay = world.Time.Day };
        _orders[order.Id] = order;
        TryDispatch(world, order);
        return order;
    }

    private void TryDispatch(WorldState world, SupplyOrder order)
    {
        if (order.Status != ShipmentStatus.Pending) return;
        if (!world.Npcs.TryGetValue(order.SupplierNpcId, out var supplier) || !supplier.IsAlive || supplier.CurrentLocationId is null) { order.Status = ShipmentStatus.Cancelled; return; }
        var entry = supplier.Inventory.Entries.FirstOrDefault(e => e.ItemId == order.ItemId);
        if (entry is null || entry.Quantity <= 0) return;
        var quantity = Math.Min(order.Quantity, entry.Quantity);
        if (!world.Inventory.Items.TryGetValue(order.ItemId, out var item)) { order.Status = ShipmentStatus.Cancelled; return; }
        var distance = world.Geography.GetRouteDistance(supplier.CurrentLocationId.Value, world.Commerce.Businesses[order.BusinessId].LocationId);
        if (double.IsInfinity(distance)) return;
        var method = MovementMethod.Cart;
        var origin = supplier.CurrentLocationId.Value;
        var destination = world.Commerce.Businesses[order.BusinessId].LocationId;
        var danger = Math.Clamp((world.Geography.Locations[destination].DangerLevel + world.Geography.Locations[origin].DangerLevel) / 2, 0, 10);
        var originWeather = world.Environment.Get(origin);
        var weatherPenalty = originWeather.RoadsImpacted ? 1.35 : (originWeather.Weather is WeatherType.Rain or WeatherType.Fog ? 1.15 : 1.0);
        var duration = Math.Max(30, (int)Math.Ceiling(distance / 7.0 * 60.0 * weatherPenalty));
        var transportCost = quantity * item.WeightKg * 0.04 + distance * 0.08 + danger * 0.15;
        if (supplier.Wealth < transportCost) return;
        supplier.Inventory.Remove(order.ItemId, quantity);
        supplier.ApplyWealthChange(-transportCost);
        var arrivalTotal = world.Time.Day * 120L + world.Time.MinuteOfDay + duration;
        var shipment = new Shipment { OrderId=order.Id, OriginLocationId=origin, DestinationLocationId=destination, ItemId=order.ItemId, Quantity=quantity, RemainingQuantity=quantity, SupplierNpcId=supplier.Id, BuyerNpcId=order.BuyerNpcId, Method=method, DistanceKm=distance, TransportCost=transportCost, Danger=danger, DepartureDay=world.Time.Day, DepartureMinute=world.Time.MinuteOfDay, ArrivalDay=arrivalTotal/120, ArrivalMinute=(int)(arrivalTotal%120), LastEvent=$"Départ de {world.Geography.Locations[origin].Name}." };
        _shipments[shipment.Id] = shipment;
        order.Status = ShipmentStatus.InTransit;
        History.Add(new LogisticsRecord { Day=world.Time.Day, ShipmentId=shipment.Id, Status=shipment.Status, Quantity=quantity, Note=$"Marchandise expédiée sur {distance:0.#} km." });
    }

    public void Advance(WorldState world)
    {
        foreach (var order in _orders.Values.Where(o => o.Status == ShipmentStatus.Pending).ToList()) TryDispatch(world, order);
        foreach (var shipment in _shipments.Values.Where(s => s.Status == ShipmentStatus.InTransit).ToList())
        {
            if (!world.Npcs.TryGetValue(shipment.BuyerNpcId, out var buyer) || !buyer.IsAlive) { shipment.Status=ShipmentStatus.Cancelled; History.Add(new LogisticsRecord{Day=world.Time.Day,ShipmentId=shipment.Id,Status=shipment.Status,Quantity=shipment.RemainingQuantity,Note="Destinataire indisponible."}); continue; }
            if (world.Time.Day * 120L + world.Time.MinuteOfDay < shipment.ArrivalDay * 120L + shipment.ArrivalMinute) continue;
            var weather = world.Environment.Get(shipment.DestinationLocationId);
            var risk = Math.Clamp(0.01 + shipment.Danger * 0.008 + (weather.RoadsImpacted ? 0.08 : 0) + (weather.Weather == WeatherType.Storm ? 0.06 : 0), 0, 0.65);
            var roll = new Random(HashCode.Combine(world.WorldSeed, shipment.Id, (int)world.Time.Day)).NextDouble();
            if (roll < risk)
            {
                var lost = Math.Max(1, (int)Math.Ceiling(shipment.RemainingQuantity * Math.Min(.9, risk + .2)));
                shipment.RemainingQuantity -= lost;
                shipment.LastEvent = $"Incident de transport : {lost} unité(s) perdues.";
                if (shipment.RemainingQuantity <= 0) { shipment.Status=ShipmentStatus.Lost; }
                History.Add(new LogisticsRecord{Day=world.Time.Day,ShipmentId=shipment.Id,Status=shipment.Status,Quantity=lost,Note=shipment.LastEvent});
                if (shipment.Status == ShipmentStatus.Lost) continue;
            }
            buyer.Inventory.Add(world.Inventory.Items[shipment.ItemId], shipment.RemainingQuantity);
            if (_orders.TryGetValue(shipment.OrderId, out var order)) order.Status=ShipmentStatus.Delivered;
            shipment.Status=ShipmentStatus.Delivered;
            History.Add(new LogisticsRecord{Day=world.Time.Day,ShipmentId=shipment.Id,Status=shipment.Status,Quantity=shipment.RemainingQuantity,Note="Livraison reçue par le commerce."});
        }
    }

    public int InTransitQuantity(Guid businessId, Guid itemId) => _shipments.Values.Where(s => s.Status == ShipmentStatus.InTransit && _orders.TryGetValue(s.OrderId, out var o) && o.BusinessId == businessId && s.ItemId == itemId).Sum(s => s.RemainingQuantity);
}
