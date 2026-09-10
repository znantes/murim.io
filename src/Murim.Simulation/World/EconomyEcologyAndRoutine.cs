namespace Murim.Simulation;

public sealed class RegionalEconomyState
{
    public Guid LocationId { get; init; }
    public double FoodPriceIndex { get; set; } = 1;
    public double WeaponDemand { get; set; } = 1;
    public double MedicineDemand { get; set; } = 1;
    public double EscortDemand { get; set; } = 1;
    public double LaborDemand { get; set; } = 1;
    public double TradeAccess { get; set; } = 1;
    public double RefugeePressure { get; set; }
    public double BanditPressure { get; set; }
}

public sealed class SpeciesPopulationState
{
    public string SpeciesKey { get; init; } = string.Empty;
    public Guid LocationId { get; init; }
    public double Population { get; set; }
    public double CarryingCapacity { get; set; }
    public bool Predator { get; init; }
    public IReadOnlyList<string> PreyKeys { get; init; } = [];
}

public sealed record DailyRoutineBlock(int StartMinute, int EndMinute, string Activity, double Probability);
public sealed class NpcRoutine
{
    public Guid NpcId { get; init; }
    public List<DailyRoutineBlock> Blocks { get; } = new();
}

public sealed class EconomyEcologySystem
{
    private readonly Random random;
    public EconomyEcologySystem(int seed) => random = new Random(seed);

    public RegionalEconomyState GetEconomy(WorldState world, Guid locationId)
    {
        if (world.RegionalEconomies.TryGetValue(locationId, out var e)) return e;
        e = new RegionalEconomyState { LocationId = locationId }; world.RegionalEconomies[locationId] = e; return e;
    }

    public void ApplyWarShock(WorldState world, IEnumerable<Guid> affectedLocations, double intensity)
    {
        intensity = Math.Clamp(intensity, 0, 1);
        foreach (var id in affectedLocations)
        {
            var e = GetEconomy(world, id);
            e.FoodPriceIndex = Math.Clamp(e.FoodPriceIndex + intensity * .35, .4, 5); e.WeaponDemand = Math.Clamp(e.WeaponDemand + intensity * .7, .2, 5);
            e.MedicineDemand = Math.Clamp(e.MedicineDemand + intensity * .55, .2, 5); e.EscortDemand = Math.Clamp(e.EscortDemand + intensity * .6, .2, 5);
            e.TradeAccess = Math.Clamp(e.TradeAccess - intensity * .3, .05, 1.5); e.RefugeePressure = Math.Clamp(e.RefugeePressure + intensity * .45, 0, 2); e.BanditPressure = Math.Clamp(e.BanditPressure + intensity * .2, 0, 2);
        }
    }

    public void AdvanceDay(WorldState world)
    {
        foreach (var e in world.RegionalEconomies.Values)
        {
            e.FoodPriceIndex = Drift(e.FoodPriceIndex, 1, .0025); e.WeaponDemand = Drift(e.WeaponDemand, 1, .003); e.MedicineDemand = Drift(e.MedicineDemand, 1, .003);
            e.EscortDemand = Drift(e.EscortDemand, 1, .003); e.LaborDemand = Drift(e.LaborDemand, 1, .002); e.TradeAccess = Drift(e.TradeAccess, 1, .002);
            e.RefugeePressure = Drift(e.RefugeePressure, 0, .0018); e.BanditPressure = Drift(e.BanditPressure, 0, .0015);
        }
        AdvanceEcology(world);
    }

    private void AdvanceEcology(WorldState world)
    {
        foreach (var group in world.Ecology.Values.GroupBy(x => x.LocationId))
        {
            var all = group.ToArray();
            foreach (var species in all)
            {
                if (species.CarryingCapacity <= 0) continue;
                var ratio = species.Population / species.CarryingCapacity;
                var growth = species.Population * .0008 * (1 - ratio);
                if (species.Predator)
                {
                    var preyAvailability = species.PreyKeys.Count == 0 ? 1 : species.PreyKeys.Select(k => all.FirstOrDefault(p => p.SpeciesKey == k)).Where(p => p is not null).Cast<SpeciesPopulationState>().DefaultIfEmpty().Average(p => p is null ? .4 : Math.Clamp(p.Population / Math.Max(1, p.CarryingCapacity), 0, 2));
                    growth *= .35 + preyAvailability;
                }
                else
                {
                    var predators = all.Where(p => p.Predator && p.PreyKeys.Contains(species.SpeciesKey)).Sum(p => p.Population / Math.Max(1, p.CarryingCapacity));
                    growth -= species.Population * predators * .00018;
                }
                species.Population = Math.Clamp(species.Population + growth, 0, species.CarryingCapacity * 2.5);
            }
        }
    }

    private double Drift(double value, double target, double rate) => value + (target - value) * rate + (random.NextDouble() - .5) * rate * .12;
}

public sealed class RoutineSystem
{
    public NpcRoutine BuildRoutine(Npc npc)
    {
        var routine = new NpcRoutine { NpcId = npc.Id };
        routine.Blocks.Add(new DailyRoutineBlock(0, 360, "dormir", .96));
        routine.Blocks.Add(new DailyRoutineBlock(360, 480, "toilette, repas, foyer", .92));
        var currentCareer = npc.CareerHistory.LastOrDefault(c => c.EndDay is null);
        routine.Blocks.Add(currentCareer is null ? new DailyRoutineBlock(480, 720, "chercher du travail ou aider le foyer", .55) : new DailyRoutineBlock(480, 720, $"travailler : {currentCareer.ProfessionCode}", .87));
        routine.Blocks.Add(new DailyRoutineBlock(720, 780, "repas et conversations", .88));
        routine.Blocks.Add(currentCareer is null ? new DailyRoutineBlock(780, 1020, "activités personnelles", .65) : new DailyRoutineBlock(780, 1020, $"travailler : {currentCareer.ProfessionCode}", .82));
        routine.Blocks.Add(new DailyRoutineBlock(1020, 1200, npc.Personality.Ambition > .65 ? "étude ou entraînement personnel" : "foyer et sociabilité", .67));
        routine.Blocks.Add(new DailyRoutineBlock(1200, 1440, "repos", .94));
        return routine;
    }

    public string CurrentActivity(Npc npc, WorldClock clock)
    {
        var routine = BuildRoutine(npc); var minute = clock.MinuteOfDay;
        var block = routine.Blocks.FirstOrDefault(b => minute >= b.StartMinute && minute < b.EndMinute);
        return block?.Activity ?? "activité imprévue";
    }
}
