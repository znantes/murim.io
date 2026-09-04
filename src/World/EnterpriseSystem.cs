namespace Murim.World;

public enum BusinessStatus { Active, Struggling, Bankrupt, Closed }

public sealed class BusinessLedger
{
    public Guid BusinessId { get; init; }
    public long Day { get; init; }
    public double Revenue { get; init; }
    public double RestockingCost { get; init; }
    public double OperatingCost { get; init; }
    public double WagePressure { get; init; }
    public double NetResult => Revenue - RestockingCost - OperatingCost - WagePressure;
}

public sealed class EnterpriseProfile
{
    public Guid BusinessId { get; init; }
    public BusinessStatus Status { get; set; } = BusinessStatus.Active;
    public double Capital { get; set; }
    public double Prosperity { get; set; } = 50;
    public int ConsecutiveLossDays { get; set; }
    public long FoundedDay { get; init; }
}

public sealed class EnterpriseSystem
{
    private readonly Dictionary<Guid, EnterpriseProfile> _profiles = new();
    public IReadOnlyDictionary<Guid, EnterpriseProfile> Profiles => _profiles;
    public List<BusinessLedger> Ledger { get; } = new();

    public EnterpriseProfile Register(WorldState world, CommerceBusiness business)
    {
        if (_profiles.TryGetValue(business.Id, out var existing)) return existing;
        var ownerWealth = world.Npcs.TryGetValue(business.OwnerNpcId, out var owner) ? owner.Wealth : 0;
        var profile = new EnterpriseProfile { BusinessId = business.Id, Capital = Math.Max(0, ownerWealth), FoundedDay = world.Time.Day };
        _profiles[business.Id] = profile;
        return profile;
    }

    public bool IsOperational(CommerceBusiness business) => _profiles.TryGetValue(business.Id, out var p) && p.Status is BusinessStatus.Active or BusinessStatus.Struggling;

    public void AdvanceDay(WorldState world)
    {
        foreach (var business in world.Commerce.Businesses.Values.ToList())
        {
            var profile = Register(world, business);
            if (profile.Status is BusinessStatus.Bankrupt or BusinessStatus.Closed) continue;
            if (!world.Npcs.TryGetValue(business.OwnerNpcId, out var owner) || !owner.IsAlive)
            {
                profile.Status = BusinessStatus.Closed;
                continue;
            }

            var revenue = world.Commerce.Transactions.Where(t => t.BusinessId == business.Id && t.Day == world.Time.Day - 1).Sum(t => t.UnitPrice * t.Quantity);
            var stockValue = business.Stock.Sum(s => world.Inventory.Items.TryGetValue(s.ItemId, out var item) ? item.BaseValue * s.Quantity : 0);
            var restockBudget = Math.Min(Math.Max(0, revenue * 0.35), Math.Max(0, owner.Wealth * 0.08));
            var restockingCost = Restock(world, business, restockBudget, revenue);
            var operatingCost = Math.Max(0.05, 0.25 + stockValue * 0.005);
            var wagePressure = world.Employment.Contracts.Values.Where(c => c.BusinessId == business.Id && c.Status == EmploymentStatus.Employed).Sum(c => c.WagePerHour * Math.Min(8, c.HoursPerDay));
            var result = revenue - restockingCost - operatingCost - wagePressure;

            if (owner.Wealth >= operatingCost) owner.ApplyWealthChange(-operatingCost);
            else result -= operatingCost;
            profile.Capital = Math.Max(0, profile.Capital + result);
            profile.Prosperity = Math.Clamp(profile.Prosperity + (result > 0 ? 2 : -3) + (revenue > 0 ? 1 : -1), 0, 100);
            profile.ConsecutiveLossDays = result < 0 ? profile.ConsecutiveLossDays + 1 : 0;
            profile.Status = profile.ConsecutiveLossDays >= 14 || (profile.Capital <= 0 && owner.Wealth <= 0) ? BusinessStatus.Bankrupt : profile.Prosperity < 25 ? BusinessStatus.Struggling : BusinessStatus.Active;

            if (profile.Status == BusinessStatus.Bankrupt)
            {
                foreach (var contract in world.Employment.Contracts.Values.Where(c => c.BusinessId == business.Id && c.Status == EmploymentStatus.Employed).ToList())
                    if (world.Npcs.TryGetValue(contract.EmployeeNpcId, out var employee)) world.Employment.Fire(world, employee, out _);
            }

            Ledger.Add(new BusinessLedger { BusinessId = business.Id, Day = world.Time.Day - 1, Revenue = revenue, RestockingCost = restockingCost, OperatingCost = operatingCost, WagePressure = wagePressure });
        }
    }

    private static double Restock(WorldState world, CommerceBusiness business, double budget, double demand)
    {
        if (budget <= 0 || business.Stock.Count == 0) return 0;
        var owner = world.Npcs.GetValueOrDefault(business.OwnerNpcId);
        if (owner is null) return 0;
        var spent = 0.0;
        var target = demand > 0 ? 2 : 1;
        foreach (var stock in business.Stock)
        {
            if (!world.Inventory.Items.TryGetValue(stock.ItemId, out var item)) continue;
            var desired = Math.Max(0, target - stock.Quantity);
            if (desired == 0) continue;
            var unitCost = Math.Max(0.01, item.BaseValue * 0.55);
            var amount = Math.Min(desired, (int)Math.Floor((budget - spent) / unitCost));
            if (amount <= 0) continue;
            var cost = amount * unitCost;
            if (owner.Wealth < cost) break;
            owner.ApplyWealthChange(-cost);
            stock.Quantity += amount;
            spent += cost;
            if (spent >= budget) break;
        }
        return spent;
    }

    public string Describe(WorldState world, CommerceBusiness business)
    {
        var p = Register(world, business);
        return $"{business.Name} — {p.Status}, prospérité {p.Prosperity:0}/100, capital {p.Capital:0.##}.";
    }
}
