using Murim.Simulation;

namespace Murim.World;

public enum CommerceType { GeneralStore, FoodStall, Inn, Blacksmith, Apothecary, Clothier, MerchantHouse, MarketStall }
public sealed class ShopStock { public Guid ItemId { get; init; } public int Quantity { get; set; } public double PriceMultiplier { get; set; } = 1; public double TargetQuantity { get; set; } = 8; }
public sealed class CommerceBusiness
{
    public Guid Id { get; } = Guid.NewGuid(); public string Name { get; init; } = "Commerce"; public CommerceType Type { get; init; } public Guid LocationId { get; init; } public Guid? BuildingId { get; set; } public Guid OwnerNpcId { get; set; }
    public bool OpenMorning { get; init; } = true; public bool OpenAfternoon { get; init; } = true; public bool OpenEvening { get; init; } public bool OpenNight { get; init; }
    public List<Guid> StaffNpcIds { get; } = new(); public List<ShopStock> Stock { get; } = new(); public List<BusinessExpense> Expenses { get; } = new();
    public double Capital { get; set; } = 10; public double Reputation { get; set; } = 50; public double Prosperity { get; set; } = 50; public int ConsecutiveLossDays { get; set; } public bool Active { get; set; } = true;
    public bool IsOpen(TimePeriod period) => period switch { TimePeriod.Morning => OpenMorning, TimePeriod.Afternoon => OpenAfternoon, TimePeriod.Evening => OpenEvening, TimePeriod.Night => OpenNight, _ => false };
}
public sealed class BusinessExpense { public long Day { get; init; } public double Amount { get; init; } public string Reason { get; init; } = string.Empty; }
public sealed class BusinessAccount { public long Day { get; init; } public double Revenue { get; init; } public double Costs { get; init; } public double Profit { get; init; } public double Capital { get; init; } }
public sealed class CommerceTransaction { public long Day { get; init; } public int Minute { get; init; } public Guid BusinessId { get; init; } public Guid BuyerNpcId { get; init; } public Guid SellerNpcId { get; init; } public Guid ItemId { get; init; } public int Quantity { get; init; } public double UnitPrice { get; init; } }
public sealed class CommerceSystem
{
    private readonly Dictionary<Guid, CommerceBusiness> _businesses = new(); public IReadOnlyDictionary<Guid, CommerceBusiness> Businesses => _businesses; public List<CommerceTransaction> Transactions { get; } = new(); public List<BusinessAccount> Accounts { get; } = new();
    public CommerceBusiness AddBusiness(string name, CommerceType type, Guid locationId, Guid ownerNpcId, bool evening = false, Guid? buildingId = null) { var b = new CommerceBusiness { Name=name, Type=type, LocationId=locationId, OwnerNpcId=ownerNpcId, OpenEvening=evening, BuildingId=buildingId }; _businesses[b.Id]=b; return b; }
    public void AddStaff(CommerceBusiness business, Guid npcId) { if (!business.StaffNpcIds.Contains(npcId)) business.StaffNpcIds.Add(npcId); }
    public void AddStock(CommerceBusiness business, Guid itemId, int quantity, double priceMultiplier = 1) { if(quantity<=0)return; var s=business.Stock.FirstOrDefault(x=>x.ItemId==itemId); if(s is null) business.Stock.Add(new ShopStock{ItemId=itemId,Quantity=quantity,PriceMultiplier=Math.Clamp(priceMultiplier,.25,5),TargetQuantity=Math.Max(4,quantity)}); else s.Quantity+=quantity; }
    public CommerceBusiness? FindOpenBusiness(WorldState world,Npc buyer,CommerceType? type=null)=>_businesses.Values.FirstOrDefault(b=>b.Active&&b.LocationId==buyer.CurrentLocationId&&b.IsOpen(world.Time.Period)&&(!type.HasValue||b.Type==type.Value));
    public double CurrentPrice(WorldState world,CommerceBusiness business,ShopStock stock){if(!world.Inventory.Items.TryGetValue(stock.ItemId,out var item))return 0;var market=world.Markets.Price(world,business.LocationId,stock.ItemId,item.BaseValue);var local=market/Math.Max(.01,item.BaseValue);var transit=world.Logistics.InTransitQuantity(business.Id,stock.ItemId);var available=stock.Quantity+transit;var scarcity=1+Math.Max(0,stock.TargetQuantity-available)/Math.Max(1,stock.TargetQuantity)*.35;var prosperity=1+(50-business.Prosperity)/250;return Math.Max(.01,item.BaseValue*stock.PriceMultiplier*local*scarcity*prosperity);}
    public bool Buy(WorldState world,Npc buyer,CommerceBusiness business,Guid itemId,int quantity,out double total){total=0;if(quantity<=0||!business.Active||business.OwnerNpcId==buyer.Id||!business.IsOpen(world.Time.Period))return false;var stock=business.Stock.FirstOrDefault(s=>s.ItemId==itemId);if(stock is null||stock.Quantity<quantity||!world.Npcs.TryGetValue(business.OwnerNpcId,out var seller)||!seller.IsAlive)return false;var unit=CurrentPrice(world,business,stock);total=unit*quantity;if(buyer.Wealth<total||!world.Inventory.Items.TryGetValue(itemId,out var item))return false;buyer.ApplyWealthChange(-total);seller.ApplyWealthChange(total);business.Capital+=total;buyer.Inventory.Add(item,quantity);stock.Quantity-=quantity;Transactions.Add(new CommerceTransaction{Day=world.Time.Day,Minute=world.Time.MinuteOfDay,BusinessId=business.Id,BuyerNpcId=buyer.Id,SellerNpcId=seller.Id,ItemId=itemId,Quantity=quantity,UnitPrice=unit});business.Reputation=Math.Min(100,business.Reputation+.05);return true;}
    public void AdvanceDay(WorldState world)
    {
        foreach(var b in _businesses.Values.Where(x=>x.Active).ToList())
        {
            var revenue=Transactions.Where(t=>t.BusinessId==b.Id&&t.Day==world.Time.Day).Sum(t=>t.UnitPrice*t.Quantity); var payroll=b.StaffNpcIds.Select(id=>world.Employment.Contracts.TryGetValue(id,out var c)?c.WagePerHour*Math.Min(c.HoursPerDay,8):0).Sum(); var rent=Math.Max(.05,b.Type==CommerceType.MerchantHouse ? .35 : .12); var profit=revenue-payroll-rent; b.Capital+=profit;
            b.Prosperity=Math.Clamp(b.Prosperity+profit*2-(b.StaffNpcIds.Count==0?1:0),0,100); b.Reputation=Math.Clamp(b.Reputation+(profit>=0?.2:-.4),0,100); Accounts.Add(new BusinessAccount{Day=world.Time.Day,Revenue=revenue,Costs=payroll+rent,Profit=profit,Capital=b.Capital}); if(profit<0)b.ConsecutiveLossDays++;else b.ConsecutiveLossDays=0;
            foreach(var s in b.Stock){var missing=Math.Max(0,(int)Math.Round(s.TargetQuantity-s.Quantity-world.Logistics.InTransitQuantity(b.Id,s.ItemId)));if(missing>0)world.Logistics.CreateOrder(world,b,s.ItemId,missing);}
            if(b.Capital<-5||b.ConsecutiveLossDays>=14){b.Active=false;b.Reputation=Math.Max(0,b.Reputation-20);foreach(var id in b.StaffNpcIds.ToList())if(world.Npcs.TryGetValue(id,out var employee))world.Employment.Fire(world,employee,out _);b.StaffNpcIds.Clear();}
        }
    }
    public string Describe(WorldState world,CommerceBusiness business){var entries=business.Stock.Where(s=>s.Quantity>0).Select(s=>world.Inventory.Items.TryGetValue(s.ItemId,out var item)?$"{item.Name} ({CurrentPrice(world,business,s):0.##}, stock {s.Quantity}/{s.TargetQuantity:0.#}, transit {world.Logistics.InTransitQuantity(business.Id,s.ItemId)})":null).Where(x=>x is not null).Take(8);return $"{business.Name} — {(business.Active?(business.IsOpen(world.Time.Period)?"ouvert":"fermé"):"faillite")} ; prospérité {business.Prosperity:0.#} ; capital {business.Capital:0.##} ; personnel {business.StaffNpcIds.Count} : {string.Join(", ",entries)}.";}
}
