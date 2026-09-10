namespace Murim.Simulation;

public static class WorldBootstrap
{
    public static WorldState Create(int seed = 190724, int initialNpcCount = 10_000)
    {
        initialNpcCount = Math.Clamp(initialNpcCount, 100, 100_000);
        var world = new WorldState { Seed = seed };
        foreach (var faction in FactionCatalog.All) world.Factions[faction.Id] = faction;
        AddLocations(world);
        AddHouseholds(world, seed);
        var content = GameContent.Build(seed);
        new PopulationGenerator(seed + 101).Populate(world, content.Monsters, initialNpcCount);
        return world;
    }

    private static void AddLocations(WorldState world)
    {
        void Add(string name, string region, string type, int cap, double safety, params string[] tags)
        {
            var id = StableLocationId(name);
            world.Locations[id] = new WorldLocation(id, name, region, type, cap, safety, tags);
        }

        Add("Capitale de Cheonhwa", "Plaine centrale", "Cité impériale", 350_000, .82, "imperial", "market", "academy");
        Add("Cité de Hanyang", "Plaine centrale", "Grande ville", 180_000, .74, "market", "guilds", "river");
        Add("Cité de Seoryeong", "Sud", "Grande ville", 130_000, .69, "market", "medicine", "craft");
        Add("Port de la Rivière Rouge", "Est", "Port", 95_000, .58, "river", "trade", "smuggling");
        Add("Ville de Gaeun", "Ouest", "Ville-frontière", 60_000, .47, "caravan", "military", "market");
        Add("Village de Qinghe", "Plaine centrale", "Village", 4_500, .86, "farming", "river");
        Add("Village de Woljeong", "Sud", "Village", 2_700, .80, "tea", "herbs");
        Add("Village de Biryong", "Nord", "Village", 3_200, .68, "mining", "mountain");
        Add("Hameau des Saules", "Est", "Hameau", 850, .89, "farming", "fishing");
        Add("Hameau de la Pierre Blanche", "Ouest", "Hameau", 620, .71, "quarry", "goats");
        Add("Mont Wudang", "Hubei", "Montagne de secte", 12_000, .91, "sect", "taoist", "forest");
        Add("Mont Hua", "Shaanxi", "Montagne de secte", 9_000, .84, "sect", "sword", "cliffs");
        Add("Mont Song", "Henan", "Montagne de secte", 16_000, .92, "sect", "monastery", "pilgrims");
        Add("Mont Emei", "Sichuan", "Montagne de secte", 10_500, .88, "sect", "herbs", "pilgrims");
        Add("Mont Kunlun", "Extrême Ouest", "Montagne de secte", 5_000, .63, "sect", "snow", "remote");
        Add("Domaine Namgung", "Anhui", "Domaine familial", 14_000, .91, "great_family", "sword", "estate");
        Add("Domaine Tang", "Sichuan", "Domaine familial", 13_000, .86, "great_family", "medicine", "hidden_weapons");
        Add("Domaine Peng", "Hebei", "Domaine familial", 11_000, .85, "great_family", "saber", "estate");
        Add("Domaine Zhuge", "Hubei", "Domaine familial", 9_500, .93, "great_family", "scholarship", "formations");
        Add("Domaine Murong", "Nord-est", "Domaine familial", 10_000, .80, "great_family", "estate", "trade");
        Add("Forêt des Pins Noirs", "Ouest", "Forêt sauvage", 1_000, .31, "forest", "monsters", "bandits");
        Add("Vallée de Jade", "Sud", "Vallée sauvage", 2_500, .57, "herbs", "mines", "monsters");
        Add("Marais de la Brume Basse", "Sud-est", "Marais", 900, .28, "swamp", "monsters", "herbs");
        Add("Monts du Ciel Fendu", "Nord", "Haute montagne", 450, .18, "mountain", "monsters", "ruins");
        Add("Désert de Garan", "Ouest", "Désert", 3_000, .24, "desert", "caravan", "ruins");
        Add("Archipel des Brumes", "Est", "Îles", 6_500, .46, "sea", "fishing", "pirates");
        Add("Palais de Glace du Nord", "Extrême Nord", "Palais extérieur", 8_000, .76, "palace", "snow", "outer_region");
        Add("Palais des Cent Bêtes", "Jungles du Sud", "Palais extérieur", 7_500, .65, "palace", "beasts", "outer_region");
        Add("Fort des Corbeaux Verts", "Routes occidentales", "Forteresse de bandits", 2_100, .19, "bandits", "black_market");
        Add("Marché des Lanternes Grises", "Sous Hanyang", "Quartier clandestin", 5_500, .37, "underworld", "market", "rumors");
        Add("Académie Cheolhan", "Plaine centrale", "Académie", 7_000, .94, "school", "imperial", "martial");
        Add("Agence de la Rivière Azurée", "Hanyang", "Agence d'escorte", 1_100, .82, "escort", "trade", "martial");
        Add("Grand Relais de Wolmun", "Route impériale", "Relais", 1_800, .73, "inn", "horses", "rumors");
        Add("Ruines de Cheongsa", "Frontière nord", "Ruines", 50, .12, "ruins", "artifacts", "monsters");
        Add("Mine de Fer Bleu", "Hebei", "Mine", 2_400, .61, "mine", "ore", "workers");
        Add("Terrasses du Thé de Seon", "Sud", "Région agricole", 8_000, .88, "tea", "farming", "villages");
    }

    private static void AddHouseholds(WorldState world, int seed)
    {
        var random = new Random(seed + 7);
        var locations = world.Locations.Values.ToArray();
        var commonNames = new[] { "Han", "Seo", "Baek", "Yun", "Kang", "Jin", "Choi", "Moon", "Im", "Jang", "Park", "Heo", "Ryu", "Shin", "Oh", "Kwon", "Ahn", "Song", "Jeong", "Lim" };

        for (var i = 0; i < 1_200; i++)
        {
            var location = locations[random.Next(locations.Length)];
            var wealthRoll = Math.Pow(random.NextDouble(), 2.1);
            world.Households[Guid.NewGuid()] = new Household
            {
                Id = Guid.NewGuid(),
                FamilyName = commonNames[random.Next(commonNames.Length)],
                HomeLocationId = location.Id,
                Wealth = Math.Clamp(5 + wealthRoll * 85 + random.NextDouble() * 10, 0, 100),
                Prestige = Math.Clamp(random.NextDouble() * 45 + wealthRoll * 20, 0, 70)
            };
        }

        AddFactionHousehold(world, "namgung", "Namgung", "Domaine Namgung", 94, 96);
        AddFactionHousehold(world, "sichuan_tang", "Tang", "Domaine Tang", 92, 94);
        AddFactionHousehold(world, "hebei_peng", "Peng", "Domaine Peng", 88, 90);
        AddFactionHousehold(world, "zhuge", "Zhuge", "Domaine Zhuge", 90, 92);
        AddFactionHousehold(world, "murong", "Murong", "Domaine Murong", 89, 90);

        foreach (var sectCode in new[] { "shaolin", "wudang", "mount_hua", "emei", "kunlun", "qingcheng", "azure_pine", "silent_river" })
        {
            var faction = FactionCatalog.ByCode(sectCode)!;
            var nearest = BestFactionLocation(world, faction);
            for (var i = 0; i < 4; i++)
            {
                var id = Guid.NewGuid();
                world.Households[id] = new Household
                {
                    Id = id,
                    FamilyName = commonNames[random.Next(commonNames.Length)],
                    HomeLocationId = nearest.Id,
                    Wealth = 30 + random.NextDouble() * 45,
                    Prestige = 45 + random.NextDouble() * 35,
                    FactionId = faction.Id
                };
            }
        }
    }

    private static void AddFactionHousehold(WorldState world, string factionCode, string familyName, string locationName, double wealth, double prestige)
    {
        var faction = FactionCatalog.ByCode(factionCode)!;
        var location = world.Locations.Values.First(l => l.Name == locationName);
        var id = Guid.NewGuid();
        world.Households[id] = new Household { Id = id, FamilyName = familyName, HomeLocationId = location.Id, Wealth = wealth, Prestige = prestige, FactionId = faction.Id };
    }

    private static WorldLocation BestFactionLocation(WorldState world, FactionDefinition faction)
    {
        var token = faction.Code switch
        {
            "shaolin" => "Mont Song", "wudang" => "Mont Wudang", "mount_hua" => "Mont Hua", "emei" => "Mont Emei", "kunlun" => "Mont Kunlun",
            _ => "Village de Qinghe"
        };
        return world.Locations.Values.FirstOrDefault(l => l.Name == token) ?? world.Locations.Values.First();
    }

    private static Guid StableLocationId(string name)
    {
        unchecked
        {
            var bytes = new byte[16];
            var h1 = 2166136261u;
            var h2 = 16777619u;
            foreach (var c in name)
            {
                h1 = (h1 ^ c) * 16777619u;
                h2 = (h2 + c) * 2246822519u;
            }
            BitConverter.GetBytes(h1).CopyTo(bytes, 0);
            BitConverter.GetBytes(h2).CopyTo(bytes, 4);
            BitConverter.GetBytes(h1 ^ (h2 << 1)).CopyTo(bytes, 8);
            BitConverter.GetBytes(h2 ^ (h1 >> 1)).CopyTo(bytes, 12);
            return new Guid(bytes);
        }
    }
}

public sealed class PopulationGenerator
{
    private readonly Random random;
    private static readonly string[] Female = ["Mira", "Seol", "Ara", "Hae", "Rin", "Yeon", "Sora", "Nari", "Yuna", "Minseo", "Bora", "Jiha"];
    private static readonly string[] Male = ["Jiwon", "Hyeon", "Min", "Ryu", "Dojin", "Gyeom", "Tae", "Jun", "Seok", "Won", "Hwan", "Jae"];

    public PopulationGenerator(int seed) => random = new Random(seed);

    public void Populate(WorldState world, IReadOnlyList<MonsterDefinition> monsters, int count)
    {
        var households = world.Households.Values.ToArray();
        var locations = world.Locations.Values.ToArray();
        var professions = ProfessionCatalog.All;
        var relationshipSystem = new RelationshipSystem();

        for (var i = 0; i < count; i++)
        {
            var household = WeightedHousehold(households);
            var monsterBorn = random.NextDouble() < 0.0015;
            var location = monsterBorn ? locations[random.Next(locations.Length)] : world.Locations[household.HomeLocationId];
            var age = SampleAge();
            var sex = random.NextDouble() < .5 ? Sex.Female : Sex.Male;
            var species = monsterBorn ? monsters.Where(m => m.EvolutionStageIndex == 0).ElementAt(random.Next(monsters.Count(m => m.EvolutionStageIndex == 0))) : null;
            var family = monsterBorn ? string.Empty : household.FamilyName;
            var given = sex == Sex.Female ? Female[random.Next(Female.Length)] : Male[random.Next(Male.Length)];
            var birthDay = world.Clock.Day - age * 365L - random.Next(365);
            var origin = monsterBorn ? "naissance monstrueuse rare" : household.FactionId is Guid factionId && world.Factions.TryGetValue(factionId, out var fac)
                ? fac.Type == FactionType.GreatFamily ? "branche d'une grande famille" : "foyer lié à une faction"
                : household.Wealth < 20 ? "famille pauvre" : household.Wealth > 75 ? "famille aisée" : "famille ordinaire";
            var npc = new Npc
            {
                Identity = new NpcIdentity(Guid.NewGuid(), given, family, sex, birthDay, location.Id, "Murim central", origin, monsterBorn, species?.Code),
                CurrentLocationId = location.Id,
                HouseholdId = monsterBorn ? null : household.Id,
                PrimaryFactionId = monsterBorn ? null : household.FactionId,
                Personality = Personality()
            };
            npc.Body.Health = 55 + random.NextDouble() * 45;
            npc.Body.Stamina = 45 + random.NextDouble() * 55;
            npc.Body.Qi = monsterBorn ? 3 + random.NextDouble() * 12 : Math.Max(0, random.NextDouble() * (age / 8.0));
            SeedSkills(npc, age, professions);
            world.Npcs[npc.Id] = npc;
            if (!monsterBorn) household.MemberIds.Add(npc.Id);
        }

        foreach (var household in households)
        {
            var members = household.MemberIds.Select(id => world.Npcs.GetValueOrDefault(id)).Where(n => n is not null).Cast<Npc>().OrderByDescending(n => n.AgeYears(world.Clock)).ToArray();
            for (var i = 0; i < members.Length; i++)
            for (var j = i + 1; j < Math.Min(members.Length, i + 8); j++)
            {
                var ageGap = Math.Abs(members[i].AgeYears(world.Clock) - members[j].AgeYears(world.Clock));
                var kin = ageGap > 18 ? .75 : .55;
                relationshipSystem.SetKinship(members[i], members[j], kin, world.Clock.Day);
            }
            AssignFamilyRoles(world, household, members, relationshipSystem);
        }

        world.PlayerNpcId = ChoosePlayerBirthLikeNpc(world);
    }

    private Guid? ChoosePlayerBirthLikeNpc(WorldState world)
    {
        var infants = world.Npcs.Values.Where(n => n.AgeYears(world.Clock) == 0).ToArray();
        return infants.Length > 0 ? infants[random.Next(infants.Length)].Id : world.Npcs.Values.FirstOrDefault()?.Id;
    }

    private void AssignFamilyRoles(WorldState world, Household household, Npc[] members, RelationshipSystem relationships)
    {
        var adults = members.Where(n => n.AgeYears(world.Clock) is >= 18 and <= 70 && !n.Identity.IsMonsterBorn).ToList();
        var young = members.Where(n => n.AgeYears(world.Clock) < 18).ToArray();
        for (var i = 0; i + 1 < adults.Count; i += 2)
        {
            if (random.NextDouble() > .43) continue;
            var a = adults[i]; var b = adults[i + 1];
            a.SpouseId = b.Id; b.SpouseId = a.Id;
            relationships.RecordInteraction(a, b, world.Clock.Day, 35, 28, 18, 30, "Des années de vie commune structurent leur relation.", true);
        }

        foreach (var child in young)
        {
            var parentCandidates = adults.Where(a => a.AgeYears(world.Clock) - child.AgeYears(world.Clock) >= 18).OrderBy(_ => random.Next()).Take(2).ToArray();
            foreach (var parent in parentCandidates)
            {
                parent.ChildIds.Add(child.Id);
                relationships.SetKinship(parent, child, .95, world.Clock.Day);
                relationships.RecordInteraction(parent, child, world.Clock.Day, 22, 20, 8, 0, "Lien parent-enfant construit par les soins et la vie quotidienne.", true);
            }
        }

        if (household.FactionId is Guid factionId && world.Factions.TryGetValue(factionId, out var faction) && faction.Prestige >= 80)
        {
            var elder = adults.OrderByDescending(a => a.AgeYears(world.Clock)).FirstOrDefault();
            if (elder is not null)
            {
                elder.Skills["faction_elder"] = 100;
                elder.Skills["leadership"] = Math.Max(60, elder.Skills.GetValueOrDefault("leadership"));
            }
        }
    }

    private void SeedSkills(Npc npc, int age, IReadOnlyList<ProfessionDefinition> professions)
    {
        npc.Skills["language"] = Math.Clamp(age * 10, 0, 100);
        npc.Skills["mobility"] = Math.Clamp(age * 8, 0, 100);
        npc.Skills["literacy"] = age < 6 ? 0 : random.NextDouble() * Math.Min(65, age * 2.2);
        if (age < 10 || npc.Identity.IsMonsterBorn) return;
        var profession = professions[random.Next(professions.Count)];
        var years = Math.Max(0, age - random.Next(10, 20));
        foreach (var skill in profession.CoreSkills) npc.Skills[skill] = Math.Clamp(years * (1.2 + random.NextDouble() * 1.5), 1, 92);
        if (age >= 14 && random.NextDouble() < .20)
        {
            npc.Skills["combat"] = Math.Clamp((age - 12) * (.5 + random.NextDouble()), 0, 70);
            npc.Body.Qi += random.NextDouble() * npc.Skills["combat"] * .3;
        }
        npc.CareerHistory.Add(new CareerRecord(profession.Code, CareerRank.Novice, worldDayForAge(age)));
    }

    private static long worldDayForAge(int age) => -Math.Max(0, age - 12) * 365L;

    private Household WeightedHousehold(Household[] households)
    {
        // Famous houses are possible but rare because there are very few of them and their prestige reduces demographic weight.
        var weights = households.Select(h => Math.Max(1.0, 115 - h.Prestige)).ToArray();
        var total = weights.Sum(); var roll = random.NextDouble() * total;
        for (var i = 0; i < households.Length; i++) { roll -= weights[i]; if (roll <= 0) return households[i]; }
        return households[^1];
    }

    private int SampleAge()
    {
        var r = random.NextDouble();
        if (r < .07) return random.Next(0, 6);
        if (r < .18) return random.Next(6, 16);
        if (r < .63) return random.Next(16, 45);
        if (r < .88) return random.Next(45, 65);
        return random.Next(65, 91);
    }

    private PersonalityProfile Personality() => new()
    {
        Curiosity = random.NextDouble(), Ambition = random.NextDouble(), Empathy = random.NextDouble(), Loyalty = random.NextDouble(),
        RiskTolerance = random.NextDouble(), Talkativeness = random.NextDouble(), Gullibility = random.NextDouble(), Anxiety = random.NextDouble(), Patience = random.NextDouble()
    };
}
