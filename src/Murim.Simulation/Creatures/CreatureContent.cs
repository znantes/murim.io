namespace Murim.Simulation;

public enum MonsterDiet { Herbivore, Omnivore, Carnivore, Spiritual, Mineral, Parasite }
public enum MonsterDisposition { Timid, Wary, Territorial, Aggressive, Predatory, Curious, Social }

public sealed record MonsterDefinition(
    int Id, string Code, string Name, string BaseFamily, string MutationAspect, int EvolutionStageIndex, string EvolutionStage,
    Rarity Rarity, double PowerRating, double Intelligence, MonsterDiet Diet, MonsterDisposition Disposition,
    IReadOnlyList<string> Habitats, IReadOnlyList<string> MutationTraits, string? EvolvesFromCode, string? EvolvesIntoCode);

public static class MonsterGenerator
{
    public const int ExpectedCount = 5_000;
    private static readonly string[] Families =
    [
        "Loup","Tigre","Serpent","Sanglier","Ours","Grue","Faucon","Corbeau","Renard","Cerf","Singe","Mante","Araignée","Scolopendre","Scorpion","Crapaud","Gecko","Tortue","Carpe","Anguille",
        "Poisson-chat","Papillon","Phalène","Abeille","Fourmi","Scarabée","Chauve-souris","Rat","Chèvre","Yak","Cheval","Chien","Chat sauvage","Léopard","Panthère","Crocodile","Salamandre","Sauterelle","Ver des roches","Escargot",
        "Crabe","Héron","Hibou","Pangolin","Blaireau","Loutre","Lièvre","Buffle","Faisan","Cigale"
    ];
    private static readonly string[] Aspects = ["Braise","Givre","Fer","Jade","Tempête","Crépuscule","Aube","Sang","Brume","Tonnerre","Pierre","Bois","Eau","Venin","Ombre","Lune","Soleil","Cendre","Cristal","Vide"];
    private static readonly string[] Stages = ["Sauvage","Éveillé","Bête spirituelle","Roi","Ancêtre"];

    public static IReadOnlyList<MonsterDefinition> Generate(int seed = 88421)
    {
        if (Families.Length * Aspects.Length * Stages.Length != ExpectedCount) throw new InvalidOperationException("Monster matrix must produce exactly 5,000 definitions.");
        var result = new List<MonsterDefinition>(ExpectedCount); var id = 1;
        for (var familyIndex = 0; familyIndex < Families.Length; familyIndex++)
        for (var aspectIndex = 0; aspectIndex < Aspects.Length; aspectIndex++)
        for (var stage = 0; stage < Stages.Length; stage++)
        {
            var codeRoot = $"MON-{familyIndex + 1:00}-{aspectIndex + 1:00}"; var code = $"{codeRoot}-{stage + 1}";
            var rarity = RarityFor(familyIndex, aspectIndex, stage, seed);
            var power = Math.Round((1 + familyIndex % 7 * .08 + aspectIndex % 5 * .1) * Math.Pow(3.1, stage) * RarityMultiplier(rarity), 2);
            var intelligence = Math.Clamp(.06 + stage * .16 + familyIndex % 9 * .018 + (aspectIndex == 18 ? .08 : 0), .02, .98);
            result.Add(new(id++, code, $"{Families[familyIndex]} de {Aspects[aspectIndex]} — {Stages[stage]}", Families[familyIndex], Aspects[aspectIndex], stage, Stages[stage], rarity,
                power, intelligence, DietFor(familyIndex, aspectIndex), DispositionFor(familyIndex, stage), HabitatsFor(familyIndex, aspectIndex), TraitsFor(aspectIndex, stage),
                stage == 0 ? null : $"{codeRoot}-{stage}", stage == Stages.Length - 1 ? null : $"{codeRoot}-{stage + 2}"));
        }
        Validate(result); return result;
    }

    public static void Validate(IReadOnlyList<MonsterDefinition> monsters)
    {
        if (monsters.Count != ExpectedCount) throw new InvalidOperationException($"Expected {ExpectedCount} monsters, got {monsters.Count}.");
        if (monsters.Select(m => m.Code).Distinct(StringComparer.Ordinal).Count() != ExpectedCount) throw new InvalidOperationException("Monster codes are not unique.");
        foreach (var line in monsters.GroupBy(m => (m.BaseFamily, m.MutationAspect))) if (line.Count() != 5) throw new InvalidOperationException("Every mutation line must contain five evolution stages.");
    }

    public static IReadOnlyList<MonsterDefinition> StartingForms(IReadOnlyList<MonsterDefinition> monsters) => monsters.Where(m => m.EvolutionStageIndex <= 1).ToArray();

    private static Rarity RarityFor(int f, int a, int stage, int seed)
    {
        unchecked
        {
            var hash = (uint)(f + 1) * 73856093u ^ (uint)(a + 1) * 19349663u ^ (uint)(stage + 1) * 83492791u ^ (uint)seed; var roll = hash % 1000;
            if (stage == 4 && roll < 35) return Rarity.Divine; if (stage >= 3 && roll < 120) return Rarity.Mythic; if (stage >= 3 && roll < 280) return Rarity.Legendary;
            if (stage >= 2 && roll < 470) return Rarity.Superior; if (stage >= 1 && roll < 690) return Rarity.Rare; if (roll < 850) return Rarity.Uncommon; return Rarity.Ordinary;
        }
    }

    private static double RarityMultiplier(Rarity rarity) => rarity switch { Rarity.Ordinary => .8, Rarity.Uncommon => 1, Rarity.Rare => 1.2, Rarity.Superior => 1.5, Rarity.Legendary => 2, Rarity.Mythic => 2.8, Rarity.Divine => 4.2, _ => 1 };
    private static MonsterDiet DietFor(int family, int aspect)
    {
        if (aspect is 2 or 10 or 18) return MonsterDiet.Mineral; if (aspect is 3 or 5 or 14 or 15 or 16 or 19) return MonsterDiet.Spiritual;
        return (family % 6) switch { 0 => MonsterDiet.Carnivore, 1 => MonsterDiet.Omnivore, 2 => MonsterDiet.Herbivore, 3 => MonsterDiet.Carnivore, 4 => MonsterDiet.Omnivore, _ => MonsterDiet.Parasite };
    }
    private static MonsterDisposition DispositionFor(int family, int stage) => (MonsterDisposition)((family * 3 + stage * 2) % 7);
    private static IReadOnlyList<string> HabitatsFor(int family, int aspect)
    {
        var primary = (family % 8) switch { 0 => "forêt", 1 => "montagne", 2 => "marais", 3 => "plaine", 4 => "falaises", 5 => "rivières", 6 => "grottes", _ => "bambouseraie" };
        var secondary = aspect switch { 1 => "hautes terres froides", 4 => "cols orageux", 9 => "pics exposés", 12 => "zones humides", 14 => "ruines", 18 => "mines anciennes", 19 => "zones interdites", _ => "lisières sauvages" };
        return [primary, secondary];
    }
    private static IReadOnlyList<string> TraitsFor(int aspect, int stage)
    {
        var elemental = Aspects[aspect] switch
        {
            "Braise" => "chaleur interne", "Givre" => "sang froid", "Fer" => "peau métallique", "Jade" => "os denses", "Tempête" => "réflexes électriques", "Crépuscule" => "camouflage crépusculaire",
            "Aube" => "récupération solaire", "Sang" => "frénésie", "Brume" => "dissimulation", "Tonnerre" => "décharge sonore", "Pierre" => "carapace minérale", "Bois" => "régénération lente",
            "Eau" => "mobilité aquatique", "Venin" => "sécrétion toxique fictive", "Ombre" => "pas silencieux", "Lune" => "activité nocturne", "Soleil" => "endurance diurne", "Cendre" => "résistance à la fumée",
            "Cristal" => "excroissances cristallines", "Vide" => "présence difficile à percevoir", _ => "mutation"
        };
        var evolution = stage switch { 0 => "instinct animal", 1 => "noyau éveillé", 2 => "qi bestial", 3 => "autorité territoriale", _ => "mémoire ancestrale" };
        return [elemental, evolution];
    }
}
