namespace Murim.Simulation;

public sealed class LifeCourseSystem
{
    private readonly Random random;
    private readonly RelationshipSystem relationships;
    private readonly ProfessionSystem professions = new();
    private static readonly string[] FemaleNames = ["Mira", "Seol", "Ara", "Hae", "Rin", "Yeon", "Sora", "Nari", "Yuna", "Minseo"];
    private static readonly string[] MaleNames = ["Jiwon", "Hyeon", "Min", "Ryu", "Dojin", "Gyeom", "Tae", "Jun", "Seok", "Won"];

    public LifeCourseSystem(int seed, RelationshipSystem relationships)
    {
        random = new Random(seed);
        this.relationships = relationships;
    }

    public void AdvanceOneDay(WorldState world)
    {
        var living = world.Npcs.Values.Where(n => n.IsAlive).ToArray();
        foreach (var npc in living)
        {
            AdvanceNeeds(npc);
            NaturalMortality(world, npc);
            PracticeOrdinaryLife(npc);
        }

        TryMarriages(world);
        TryFamilyBirths(world);
        MaybeChangeCareers(world);
    }

    private static void AdvanceNeeds(Npc npc)
    {
        var ageFactor = npc.Identity.IsMonsterBorn ? .8 : 1.0;
        npc.Body.Hunger = Math.Clamp(npc.Body.Hunger + 2.2 * ageFactor, 0, 100);
        npc.Body.Thirst = Math.Clamp(npc.Body.Thirst + 3.0 * ageFactor, 0, 100);
        npc.Body.SleepDebt = Math.Clamp(npc.Body.SleepDebt + 1.8, 0, 100);
        npc.Body.Fatigue = Math.Clamp(npc.Body.Fatigue + 1.2, 0, 100);
        if (npc.Body.Hunger > 92 || npc.Body.Thirst > 92) npc.Body.Health = Math.Max(0, npc.Body.Health - .8);
        if (npc.Body.SleepDebt > 95) npc.Body.Health = Math.Max(0, npc.Body.Health - .15);
    }

    private void NaturalMortality(WorldState world, Npc npc)
    {
        if (!npc.IsAlive) return;
        var age = npc.AgeYears(world.Clock);
        if (age < 55) return;
        var yearly = age switch
        {
            < 65 => .006,
            < 75 => .025,
            < 85 => .080,
            < 95 => .200,
            _ => .500
        };
        var healthFactor = Math.Clamp((110 - npc.Body.Health) / 75.0, .45, 2.2);
        if (random.NextDouble() < yearly / 365.0 * healthFactor) npc.Body.Health = 0;
    }

    private void PracticeOrdinaryLife(Npc npc)
    {
        if (npc.Identity.IsMonsterBorn) return;
        var current = npc.CareerHistory.LastOrDefault(c => c.EndDay is null);
        if (current is null) return;
        var profession = ProfessionCatalog.All.FirstOrDefault(p => p.Code == current.ProfessionCode);
        if (profession is null || profession.CoreSkills.Count == 0) return;
        var skill = profession.CoreSkills[random.Next(profession.CoreSkills.Count)];
        professions.Train(npc, skill, .8 + random.NextDouble() * 2.5, .7 + random.NextDouble() * .5);
    }

    private void TryMarriages(WorldState world)
    {
        var eligible = world.Npcs.Values.Where(n => n.IsAlive && !n.Identity.IsMonsterBorn && n.SpouseId is null && n.AgeYears(world.Clock) is >= 18 and <= 60)
            .GroupBy(n => n.CurrentLocationId);
        foreach (var locationGroup in eligible)
        {
            var people = locationGroup.OrderBy(_ => random.Next()).Take(120).ToArray();
            for (var i = 0; i + 1 < people.Length; i += 2)
            {
                var a = people[i]; var b = people[i + 1];
                if (a.Id == b.Id || a.HouseholdId == b.HouseholdId) continue;
                var relationA = relationships.GetOrCreate(a, b, world.Clock.Day);
                var relationB = relationships.GetOrCreate(b, a, world.Clock.Day);
                var compatibility = PersonalityCompatibility(a, b);
                var affection = (relationA.Affection + relationB.Affection + 200) / 400.0;
                var trust = (relationA.Trust + relationB.Trust + 200) / 400.0;
                var annualChance = .015 + compatibility * .035 + affection * .025 + trust * .02;
                if (random.NextDouble() >= annualChance / 365.0) continue;
                a.SpouseId = b.Id; b.SpouseId = a.Id;
                relationships.RecordInteraction(a, b, world.Clock.Day, 28, 25, 12, 24, "Leur union devient un souvenir structurant.", true);
                world.Events.Add(new WorldEvent(Guid.NewGuid(), WorldEventType.Marriage, world.Clock.Day, a.CurrentLocationId, [a.Id, b.Id], [],
                    $"{a.Identity.DisplayName} et {b.Identity.DisplayName} se marient.", .55, .45, .05, []));
            }
        }
    }

    private void TryFamilyBirths(WorldState world)
    {
        var couples = world.Npcs.Values.Where(n => n.IsAlive && n.SpouseId is not null && n.Id.CompareTo(n.SpouseId!.Value) < 0).ToArray();
        foreach (var a in couples)
        {
            if (!world.Npcs.TryGetValue(a.SpouseId!.Value, out var b) || !b.IsAlive || a.CurrentLocationId != b.CurrentLocationId) continue;
            var mother = a.Identity.Sex == Sex.Female ? a : b.Identity.Sex == Sex.Female ? b : null;
            if (mother is null) continue;
            var age = mother.AgeYears(world.Clock);
            if (age is < 18 or > 43) continue;
            if (mother.Body.Health < 35) continue;
            var household = ResolveHousehold(world, a, b);
            if (household is null) continue;
            var childrenAtHome = household.MemberIds.Select(id => world.Npcs.GetValueOrDefault(id)).Count(n => n is not null && n.AgeYears(world.Clock) < 16 && n.IsAlive);
            var wealthFactor = Math.Clamp(household.Wealth / 100.0, .2, 1.0);
            var annualChance = .12 * wealthFactor / (1 + childrenAtHome * .35);
            if (random.NextDouble() >= annualChance / 365.0) continue;
            var child = CreateChild(world, a, b, household);
            relationships.SetKinship(a, child, 1, world.Clock.Day);
            relationships.SetKinship(b, child, 1, world.Clock.Day);
            a.ChildIds.Add(child.Id); b.ChildIds.Add(child.Id);
            world.Events.Add(new WorldEvent(Guid.NewGuid(), WorldEventType.Birth, world.Clock.Day, child.CurrentLocationId, [a.Id, b.Id, child.Id], [],
                $"{child.Identity.DisplayName} naît dans le foyer {household.FamilyName}.", .25, .40, .02, []));
        }
    }

    private Npc CreateChild(WorldState world, Npc a, Npc b, Household household)
    {
        var sex = random.NextDouble() < .5 ? Sex.Female : Sex.Male;
        var name = sex == Sex.Female ? FemaleNames[random.Next(FemaleNames.Length)] : MaleNames[random.Next(MaleNames.Length)];
        var child = new Npc
        {
            Identity = new NpcIdentity(Guid.NewGuid(), name, household.FamilyName, sex, world.Clock.Day, a.CurrentLocationId, a.Identity.Culture,
                household.FactionId is null ? "naissance familiale" : "naissance dans un foyer lié à une faction"),
            CurrentLocationId = a.CurrentLocationId,
            HouseholdId = household.Id,
            PrimaryFactionId = household.FactionId,
            Personality = new PersonalityProfile
            {
                Curiosity = BlendTrait(a.Personality.Curiosity, b.Personality.Curiosity), Ambition = BlendTrait(a.Personality.Ambition, b.Personality.Ambition),
                Empathy = BlendTrait(a.Personality.Empathy, b.Personality.Empathy), Loyalty = BlendTrait(a.Personality.Loyalty, b.Personality.Loyalty),
                RiskTolerance = BlendTrait(a.Personality.RiskTolerance, b.Personality.RiskTolerance), Talkativeness = BlendTrait(a.Personality.Talkativeness, b.Personality.Talkativeness),
                Gullibility = BlendTrait(a.Personality.Gullibility, b.Personality.Gullibility), Anxiety = BlendTrait(a.Personality.Anxiety, b.Personality.Anxiety),
                Patience = BlendTrait(a.Personality.Patience, b.Personality.Patience)
            }
        };
        child.Body.Health = 70 + random.NextDouble() * 30;
        child.Body.Qi = random.NextDouble() * 2;
        child.Skills["language"] = 0; child.Skills["mobility"] = 0;
        world.Npcs[child.Id] = child; household.MemberIds.Add(child.Id);
        return child;
    }

    private Household? ResolveHousehold(WorldState world, Npc a, Npc b)
    {
        if (a.HouseholdId is Guid ah && world.Households.TryGetValue(ah, out var ha)) return ha;
        if (b.HouseholdId is Guid bh && world.Households.TryGetValue(bh, out var hb)) return hb;
        return null;
    }

    private void MaybeChangeCareers(WorldState world)
    {
        foreach (var npc in world.Npcs.Values.Where(n => n.IsAlive && !n.Identity.IsMonsterBorn && n.AgeYears(world.Clock) >= 12))
        {
            if (random.NextDouble() > .0012) continue;
            var best = professions.BestFit(npc);
            if (best is null) continue;
            var current = npc.CareerHistory.LastOrDefault(c => c.EndDay is null);
            if (current?.ProfessionCode == best.Code) continue;
            if (current is not null)
            {
                var index = npc.CareerHistory.LastIndexOf(current);
                npc.CareerHistory[index] = current with { EndDay = world.Clock.Day };
            }
            npc.CareerHistory.Add(new CareerRecord(best.Code, professions.RankFor(npc, best), world.Clock.Day));
            world.Events.Add(new WorldEvent(Guid.NewGuid(), WorldEventType.JobChange, world.Clock.Day, npc.CurrentLocationId, [npc.Id], [],
                $"{npc.Identity.DisplayName} commence à travailler comme {best.Name.ToLowerInvariant()}.", .12, .18, .02, []));
        }
    }

    private double PersonalityCompatibility(Npc a, Npc b)
    {
        var distance = Math.Abs(a.Personality.Empathy - b.Personality.Empathy) + Math.Abs(a.Personality.Loyalty - b.Personality.Loyalty) + Math.Abs(a.Personality.Patience - b.Personality.Patience);
        return Math.Clamp(1 - distance / 3.0 + random.NextDouble() * .15, 0, 1);
    }

    private double BlendTrait(double a, double b) => Math.Clamp((a + b) * .5 + (random.NextDouble() - .5) * .35, 0, 1);
}

public sealed class WorldSimulationEngine
{
    public RelationshipSystem Relationships { get; } = new();
    public RumorSystem Rumors { get; }
    public LifeCourseSystem LifeCourse { get; }
    public WorldEventSystem Events { get; }
    private readonly Random random;

    public WorldSimulationEngine(int seed)
    {
        random = new Random(seed);
        Rumors = new RumorSystem(seed + 11);
        LifeCourse = new LifeCourseSystem(seed + 29, Relationships);
        Events = new WorldEventSystem(seed + 47);
    }

    public void AdvanceMinutes(WorldState world, int minutes)
    {
        if (minutes <= 0) return;
        var oldDay = world.Clock.Day;
        world.Clock.AdvanceMinutes(minutes);
        var newDay = world.Clock.Day;
        var elapsedDays = newDay - oldDay;
        for (var i = 0L; i < elapsedDays; i++) AdvanceDay(world);
    }

    private void AdvanceDay(WorldState world)
    {
        LifeCourse.AdvanceOneDay(world);
        Relationships.AdvanceDecay(world, 1);
        var events = Events.GenerateDaily(world);
        foreach (var ev in events) SeedRumorFromEvent(world, ev);
        SpreadRumors(world);
    }

    private void SeedRumorFromEvent(WorldState world, WorldEvent ev)
    {
        if (ev.DirectWitnessNpcIds.Count == 0 && random.NextDouble() > ev.Publicity) return;
        Npc? source = ev.DirectWitnessNpcIds.Select(id => world.Npcs.GetValueOrDefault(id)).FirstOrDefault(n => n is not null);
        source ??= ev.LocationId is Guid loc ? world.Npcs.Values.FirstOrDefault(n => n.IsAlive && n.CurrentLocationId == loc) : null;
        if (source is null) return;
        Rumors.Publish(world, source, ev.Type.ToString(), ev.Summary, truth: .92, ambiguity: ev.Ambiguity, importance: ev.Importance,
            anxiety: ev.Type is WorldEventType.BanditAttack or WorldEventType.MonsterAttack or WorldEventType.Coup or WorldEventType.Famine ? .75 : .3,
            sourceCredibility: ev.DirectWitnessNpcIds.Contains(source.Id) ? .82 : .52, locationId: ev.LocationId, eventId: ev.Id);
    }

    private void SpreadRumors(WorldState world)
    {
        if (world.Rumors.Count == 0) return;
        foreach (var group in world.Npcs.Values.Where(n => n.IsAlive).GroupBy(n => n.CurrentLocationId))
        {
            var people = group.OrderBy(_ => random.Next()).Take(80).ToArray();
            if (people.Length < 2) continue;
            var rounds = Math.Min(20, people.Length / 2);
            for (var i = 0; i < rounds; i++)
            {
                var from = people[random.Next(people.Length)];
                var to = people[random.Next(people.Length)];
                if (from.Id == to.Id || from.RumorBeliefs.Count == 0) continue;
                var belief = from.RumorBeliefs.Values.ElementAt(random.Next(from.RumorBeliefs.Count));
                if (world.Rumors.TryGetValue(belief.RumorId, out var rumor)) Rumors.TryTransmit(world, from, to, rumor);
            }
        }
    }
}
