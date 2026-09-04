namespace Murim.World;

public enum EmploymentStatus { Unemployed, Employed, Fired, Resigned }

public sealed class JobOffer
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid EmployerNpcId { get; init; }
    public Guid? BusinessId { get; init; }
    public Guid? BuildingId { get; init; }
    public ProfessionType ProfessionType { get; init; }
    public string Title { get; init; } = "Emploi";
    public double WagePerHour { get; set; }
    public int RequiredSkill { get; init; }
    public int MinimumAge { get; init; } = 12;
    public long PostedDay { get; init; }
    public bool Open { get; set; } = true;
}

public sealed class EmploymentContract
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid EmployeeNpcId { get; init; }
    public Guid EmployerNpcId { get; init; }
    public Guid? BusinessId { get; init; }
    public Guid? BuildingId { get; init; }
    public ProfessionType ProfessionType { get; init; }
    public double WagePerHour { get; set; }
    public int HoursPerDay { get; init; } = 8;
    public long StartDay { get; init; }
    public EmploymentStatus Status { get; set; } = EmploymentStatus.Employed;
    public int Performance { get; set; } = 50;
}

public sealed class CareerRecord
{
    public long Day { get; init; }
    public Guid EmployeeNpcId { get; init; }
    public ProfessionType ProfessionType { get; init; }
    public EmploymentStatus Status { get; init; }
    public double WagePerHour { get; init; }
    public string Note { get; init; } = string.Empty;
}

public sealed class EmploymentSystem
{
    private readonly Dictionary<Guid, JobOffer> _offers = new();
    private readonly Dictionary<Guid, EmploymentContract> _contracts = new();
    public IReadOnlyDictionary<Guid, JobOffer> Offers => _offers;
    public IReadOnlyDictionary<Guid, EmploymentContract> Contracts => _contracts;
    public List<CareerRecord> CareerHistory { get; } = new();
    public int MinimumWorkingAge { get; set; } = 12;

    public JobOffer Post(WorldState world, Npc employer, ProfessionType type, string title, double baseWage, int requiredSkill = 0, Guid? businessId = null, Guid? buildingId = null)
    {
        var offer = new JobOffer { EmployerNpcId = employer.Id, BusinessId = businessId, BuildingId = buildingId, ProfessionType = type, Title = title, WagePerHour = Math.Max(0.01, baseWage), RequiredSkill = Math.Clamp(requiredSkill, 0, 100), MinimumAge = MinimumWorkingAge, PostedDay = world.Time.Day };
        _offers[offer.Id] = offer;
        return offer;
    }

    public IEnumerable<JobOffer> AvailableFor(WorldState world, Npc npc)
    {
        return _offers.Values.Where(o => o.Open && world.Npcs.TryGetValue(o.EmployerNpcId, out var employer) && employer.IsAlive && npc.IsAlive && npc.AgeYears >= o.MinimumAge && npc.Profession.Skill >= o.RequiredSkill && (o.BuildingId is null || npc.CurrentLocationId == world.Buildings.Buildings[o.BuildingId.Value].LocationId));
    }

    public bool Apply(WorldState world, Npc employee, JobOffer offer, out string feedback)
    {
        feedback = string.Empty;
        if (!offer.Open || !employee.IsAlive || employee.AgeYears < offer.MinimumAge || employee.Profession.Skill < offer.RequiredSkill) { feedback = "Le candidat ne remplit pas les conditions."; return false; }
        if (_contracts.Values.Any(c => c.EmployeeNpcId == employee.Id && c.Status == EmploymentStatus.Employed)) { feedback = "Le candidat a déjà un emploi."; return false; }
        if (!world.Npcs.TryGetValue(offer.EmployerNpcId, out var employer) || !employer.IsAlive) { feedback = "L'employeur n'est plus disponible."; return false; }
        if (offer.BuildingId is not null && employee.CurrentLocationId != world.Buildings.Buildings[offer.BuildingId.Value].LocationId) { feedback = "Le poste est trop éloigné."; return false; }
        var contract = new EmploymentContract { EmployeeNpcId = employee.Id, EmployerNpcId = employer.Id, BusinessId = offer.BusinessId, BuildingId = offer.BuildingId, ProfessionType = offer.ProfessionType, WagePerHour = offer.WagePerHour, StartDay = world.Time.Day };
        _contracts[employee.Id] = contract; offer.Open = false; employee.Profession.Type = offer.ProfessionType; employee.Profession.DailyIncome = contract.WagePerHour * contract.HoursPerDay;
        employee.History.Add("Emploi", 0, $"Commence comme {offer.Title} pour {employer.Identity.Name}.");
        CareerHistory.Add(new CareerRecord { Day = world.Time.Day, EmployeeNpcId = employee.Id, ProfessionType = offer.ProfessionType, Status = EmploymentStatus.Employed, WagePerHour = contract.WagePerHour, Note = offer.Title });
        feedback = $"Embauché comme {offer.Title}, {contract.WagePerHour:0.##} par heure.";
        return true;
    }

    public bool Quit(WorldState world, Npc employee, out string feedback) => End(world, employee, EmploymentStatus.Resigned, "Démission");

    public bool Fire(WorldState world, Npc employee, out string feedback) => End(world, employee, EmploymentStatus.Fired, "Licenciement");

    private bool End(WorldState world, Npc employee, EmploymentStatus status, string note)
    {
        if (!_contracts.TryGetValue(employee.Id, out var contract) || contract.Status != EmploymentStatus.Employed) { feedback = "Aucun emploi actif."; return false; }
        contract.Status = status;
        CareerHistory.Add(new CareerRecord { Day = world.Time.Day, EmployeeNpcId = employee.Id, ProfessionType = contract.ProfessionType, Status = status, WagePerHour = contract.WagePerHour, Note = note });
        employee.History.Add(note, 0, $"{note} : {contract.ProfessionType}.");
        _contracts.Remove(employee.Id);
        return true;
    }

    public bool IsEmployed(Npc npc) => _contracts.TryGetValue(npc.Id, out var c) && c.Status == EmploymentStatus.Employed;

    public double HourlyWage(Npc npc) => _contracts.TryGetValue(npc.Id, out var c) && c.Status == EmploymentStatus.Employed ? c.WagePerHour : 0;

    public bool WorkHour(WorldState world, Npc employee, out double wage)
    {
        wage = 0;
        if (!_contracts.TryGetValue(employee.Id, out var contract) || contract.Status != EmploymentStatus.Employed) return false;
        if (!world.Npcs.TryGetValue(contract.EmployerNpcId, out var employer) || !employer.IsAlive) { Fire(world, employee, out _); return false; }
        if (employee.CurrentLocationId != employer.CurrentLocationId) return false;
        wage = contract.WagePerHour;
        if (employer.Wealth < wage) { contract.Performance = Math.Max(0, contract.Performance - 5); return false; }
        employer.ApplyWealthChange(-wage); employee.ApplyWealthChange(wage); contract.Performance = Math.Min(100, contract.Performance + 1);
        return true;
    }

    public void AdvanceDay(WorldState world)
    {
        EnsureBusinessJobs(world);
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive && n.AgeYears >= MinimumWorkingAge))
        {
            if (IsEmployed(npc))
            {
                if (_contracts.TryGetValue(npc.Id, out var contract) && contract.Performance >= 90 && world.Time.Day - contract.StartDay >= 30)
                {
                    contract.WagePerHour *= 1.05;
                    contract.Performance = 60;
                    npc.Profession.Skill = Math.Min(100, npc.Profession.Skill + 1);
                    npc.History.Add("Promotion", 0, $"Le salaire de {contract.ProfessionType} augmente à {contract.WagePerHour:0.##}/h.");
                }
                continue;
            }
            var candidates = AvailableFor(world, npc).OrderBy(o => Math.Abs(o.RequiredSkill - npc.Profession.Skill)).ThenByDescending(o => o.WagePerHour).Take(3).ToList();
            if (candidates.Count == 0) continue;
            var selected = candidates[world.WorldSeed == 0 ? 0 : Math.Abs(HashCode.Combine(world.WorldSeed, npc.Id, (int)world.Time.Day)) % candidates.Count];
            if (npc.Profession.Skill + 15 >= selected.RequiredSkill) Apply(world, npc, selected, out _);
        }
        foreach (var c in _contracts.Values.ToList())
        {
            if (!world.Npcs.TryGetValue(c.EmployeeNpcId, out var employee) || !employee.IsAlive) continue;
            if (!world.Npcs.TryGetValue(c.EmployerNpcId, out var employer) || !employer.IsAlive) { Fire(world, employee, out _); continue; }
            var wage = c.WagePerHour * Math.Min(c.HoursPerDay, 8);
            if (employer.Wealth >= wage) { employer.ApplyWealthChange(-wage); employee.ApplyWealthChange(wage); c.Performance = Math.Clamp(c.Performance + (employee.Profession.Skill >= 50 ? 2 : 1), 0, 100); }
            else { c.Performance = Math.Max(0, c.Performance - 10); if (c.Performance == 0) Fire(world, employee, out _); }
        }
    }

    private void EnsureBusinessJobs(WorldState world)
    {
        foreach (var business in world.Commerce.Businesses.Values)
        {
            var employer = world.Npcs.GetValueOrDefault(business.OwnerNpcId);
            if (employer is null || !employer.IsAlive) continue;
            if (_offers.Values.Any(o => o.Open && o.BusinessId == business.Id)) continue;
            var (type, title, wage, skill) = business.Type switch
            {
                CommerceType.Blacksmith => (ProfessionType.Craftsman, "Aide-forgeron", 0.45, 25),
                CommerceType.Apothecary => (ProfessionType.Healer, "Apprenti herboriste", 0.5, 30),
                CommerceType.Inn => (ProfessionType.Servant, "Employé d'auberge", 0.35, 10),
                CommerceType.MerchantHouse => (ProfessionType.Merchant, "Commis marchand", 0.55, 35),
                CommerceType.Clothier => (ProfessionType.Craftsman, "Aide-tailleur", 0.4, 20),
                CommerceType.FoodStall => (ProfessionType.Servant, "Aide-cuisinier", 0.3, 5),
                _ => (ProfessionType.Merchant, "Assistant de boutique", 0.3, 10)
            };
            Post(world, employer, type, title, wage, skill, business.Id, business.BuildingId);
        }
    }

    public string DescribeFor(WorldState world, Npc npc)
    {
        var offers = AvailableFor(world, npc).Take(6).Select(o => $"{o.Title} ({o.WagePerHour:0.##}/h, compétence {o.RequiredSkill})");
        return offers.Any() ? string.Join(" ; ", offers) : "Aucun emploi adapté trouvé.";
    }
}
