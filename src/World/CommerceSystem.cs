namespace Murim.World;

public enum CommerceType
{
    GeneralStore,
    FoodStall,
    Inn,
    Blacksmith,
    Apothecary,
    Clothier,
    MerchantHouse,
    MarketStall
}

public sealed class ShopStock
{
    public Guid ItemId { get; init; }
    public int Quantity { get; set; }
    public double PriceMultiplier { get; set; } = 1.0;
}

public sealed class CommerceBusiness
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; init; } = "Commerce";
    public CommerceType Type { get; init; }
    public Guid LocationId { get; init; }
    public Guid? BuildingId { get; set; }
    public Guid OwnerNpcId { get; set; }
    public bool OpenMorning { get; init; } = true;
    public bool OpenAfternoon { get; init; } = true;
    public bool OpenEvening { get; init; }
    public bool OpenNight { get; init; }
    public List<ShopStock> Stock { get; } = new();

    public bool IsOpen(TimePeriod period) => period switch
    {
        TimePeriod.Morning => OpenMorning,
        TimePeriod.Afternoon => OpenAfternoon,
        TimePeriod.Evening => OpenEvening,
        TimePeriod.Night => OpenNight,
        _ => false
    };
}

public sealed class CommerceTransaction
{
    public long Day { get; init; }
    public int Minute { get; init; }
    public Guid BusinessId { get; init; }
    public Guid BuyerNpcId { get; init; }
    public Guid SellerNpcId { get; init; }
    public Guid ItemId { get; init; }
    public int Quantity { get; init; }
    public double UnitPrice { get; init; }
}

public sealed class CommerceSystem
{
    private readonly Dictionary<Guid, CommerceBusiness> _businesses = new();
    public IReadOnlyDictionary<Guid, CommerceBusiness> Businesses => _businesses;
    public List<CommerceTransaction> Transactions { get; } = new();

    public CommerceBusiness AddBusiness(string name, CommerceType type, Guid locationId, Guid ownerNpcId, bool evening = false)
    {
        var business = new CommerceBusiness { Name = name, Type = type, LocationId = locationId, OwnerNpcId = ownerNpcId, OpenEvening = evening };
        _businesses[business.Id] = business;
        return business;
    }

    public void AddStock(CommerceBusiness business, Guid itemId, int quantity, double priceMultiplier = 1.0)
    {
        if (quantity <= 0) return;
        var stock = business.Stock.FirstOrDefault(s => s.ItemId == itemId);
        if (stock is null) business.Stock.Add(new ShopStock { ItemId = itemId, Quantity = quantity, PriceMultiplier = Math.Clamp(priceMultiplier, 0.25, 5) });
        else stock.Quantity += quantity;
    }

    public CommerceBusiness? FindOpenBusiness(WorldState world, Npc buyer, CommerceType? type = null)
    {
        return _businesses.Values.FirstOrDefault(b => b.LocationId == buyer.CurrentLocationId && b.IsOpen(world.Time.Period) && (!type.HasValue || b.Type == type.Value));
    }

    public double CurrentPrice(WorldState world, CommerceBusiness business, ShopStock stock)
    {
        if (!world.Inventory.Items.TryGetValue(stock.ItemId, out var item)) return 0;
        var scarcity = 1.0 + Math.Max(0, 3 - stock.Quantity) * 0.12;
        return Math.Max(0.01, item.BaseValue * stock.PriceMultiplier * scarcity);
    }

    public bool Buy(WorldState world, Npc buyer, CommerceBusiness business, Guid itemId, int quantity, out double total)
    {
        total = 0;
        if (quantity <= 0 || business.OwnerNpcId == buyer.Id || !business.IsOpen(world.Time.Period)) return false;
        var stock = business.Stock.FirstOrDefault(s => s.ItemId == itemId);
        if (stock is null || stock.Quantity < quantity || !world.Npcs.TryGetValue(business.OwnerNpcId, out var seller) || !seller.IsAlive) return false;
        var unit = CurrentPrice(world, business, stock); total = unit * quantity;
        if (buyer.Wealth < total) return false;
        if (!world.Inventory.Items.TryGetValue(itemId, out var item)) return false;
        buyer.ApplyWealthChange(-total); seller.ApplyWealthChange(total); buyer.Inventory.Add(item, quantity); stock.Quantity -= quantity;
        Transactions.Add(new CommerceTransaction { Day = world.Time.Day, Minute = world.Time.MinuteOfDay, BusinessId = business.Id, BuyerNpcId = buyer.Id, SellerNpcId = seller.Id, ItemId = itemId, Quantity = quantity, UnitPrice = unit });
        return true;
    }

    public string Describe(WorldState world, CommerceBusiness business)
    {
        var entries = business.Stock.Where(s => s.Quantity > 0).Select(s => world.Inventory.Items.TryGetValue(s.ItemId, out var item) ? $"{item.Name} ({CurrentPrice(world, business, s):0.##}, stock {s.Quantity})" : null).Where(x => x is not null).Take(8);
        return $"{business.Name} — {(business.IsOpen(world.Time.Period) ? "ouvert" : "fermé")} : {string.Join(", ", entries)}";
    }
}
