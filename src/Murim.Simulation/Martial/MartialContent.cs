namespace Murim.Simulation;

public enum MartialRealm
{
    Untrained,
    ThirdRate,
    SecondRate,
    FirstRate,
    Peak,
    TranscendentPeak,
    Transformation,
    Profound,
    LifeDeath
}

public enum TechniqueDomain
{
    InternalCultivation, BodyTempering, Sword, Saber, Spear, Staff, Fist, Palm, Finger, Leg,
    Movement, Qinggong, HiddenWeapon, Archery, Grappling, Formation, Healing, Medicine, Poison,
    Alchemy, Forging, WeaponSmithing, ArmorSmithing, Cooking, Brewing, Herbalism, Farming,
    Hunting, Tracking, BeastTaming, Fishing, Mining, Carpentry, Masonry, Tailoring, Weaving,
    Calligraphy, Painting, Music, Scholarship, Teaching, Commerce, Negotiation, Accounting,
    Leadership, Strategy, Espionage, Thievery, Assassination, Escorting
}

public sealed record MartialRealmDefinition(
    MartialRealm Realm,
    string KoreanLabel,
    string FrenchLabel,
    string Description,
    double BaselineQiControl,
    int TypicalYearsOfPractice);

public static class MartialRealmCatalog
{
    public static IReadOnlyList<MartialRealmDefinition> All { get; } =
    [
        new(MartialRealm.Untrained, "비무인", "Non-pratiquant", "Personne sans fondation martiale structurée.", 0.00, 0),
        new(MartialRealm.ThirdRate, "삼류", "Troisième rang", "Fondations, respiration, posture et premiers usages du qi.", 0.08, 1),
        new(MartialRealm.SecondRate, "이류", "Deuxième rang", "Bases solides, circulation interne et techniques fiables.", 0.18, 3),
        new(MartialRealm.FirstRate, "일류", "Premier rang", "Combattant accompli capable de façonner son énergie avec constance.", 0.32, 7),
        new(MartialRealm.Peak, "절정", "Sommet", "Maître reconnu, expression du qi dans l'arme ou le corps.", 0.48, 15),
        new(MartialRealm.TranscendentPeak, "초절정", "Sommet transcendant", "Expert exceptionnel, contrôle énergétique et perception martiale supérieurs.", 0.62, 25),
        new(MartialRealm.Transformation, "화경", "Transformation", "Compréhension profonde : la technique cesse d'être seulement mécanique.", 0.76, 40),
        new(MartialRealm.Profound, "현경", "Mystère profond", "État rarissime où intention, énergie et mouvement deviennent presque indissociables.", 0.89, 60),
        new(MartialRealm.LifeDeath, "생사경", "Vie et Mort", "Domaine légendaire servant de plafond mythique au monde martial.", 0.98, 90)
    ];
}

public sealed record TechniqueDefinition(
    int Id,
    string Code,
    string Name,
    TechniqueDomain Domain,
    Rarity Rarity,
    double Potency,
    double Complexity,
    double QiCost,
    double StaminaCost,
    double InjuryRisk,
    MartialRealm RecommendedRealm,
    IReadOnlyList<string> Tags);

public static class TechniqueGenerator
{
    public const int ExpectedCount = 10_000;
    public static IReadOnlyDictionary<Rarity, int> RarityTargets { get; } = new Dictionary<Rarity, int>
    {
        [Rarity.Ordinary] = 4_500,
        [Rarity.Uncommon] = 2_500,
        [Rarity.Rare] = 1_500,
        [Rarity.Superior] = 850,
        [Rarity.Legendary] = 450,
        [Rarity.Mythic] = 150,
        [Rarity.Divine] = 50
    };

    private static readonly string[] Prefixes =
    [
        "Cendre", "Jade", "Brume", "Fer", "Lotus", "Pin", "Grue", "Tigre", "Dragon", "Lune",
        "Aube", "Crépuscule", "Rivière", "Montagne", "Orage", "Vent", "Étoile", "Bambou", "Neige", "Soleil"
    ];

    private static readonly string[] Motifs =
    [
        "Silencieux", "Inflexible", "Errant", "Brisé", "Pur", "Profond", "Patient", "Écarlate", "Céleste", "Souterrain",
        "des Neuf Pas", "des Cent Souffles", "des Mille Fils", "du Cœur Vide", "du Fourneau Calme", "du Marteau Juste", "de la Main Sûre",
        "du Regard Clair", "de la Porte Fermée", "du Fil Continu", "du Grain Parfait", "du Marché Paisible", "du Bois Vivant", "de l'Eau Dormante", "de l'Écho Lointain"
    ];

    private static readonly string[] Endings =
    [
        "sous la Pluie", "au Sommet", "dans la Nuit", "sans Trace", "de l'Est", "de l'Ouest", "du Nord", "du Sud", "des Sept Retours", "des Trois Cercles",
        "du Pavillon Vide", "de la Cour Ancienne", "du Sentier Caché", "de la Main Gauche", "de la Main Droite", "du Souffle Long", "du Pas Court", "du Ciel Bas", "de la Terre Haute", "de l'Instant Unique"
    ];

    private static readonly string[] DomainLabels = Enum.GetNames<TechniqueDomain>();

    public static IReadOnlyList<TechniqueDefinition> Generate(int seed = 421337)
    {
        if (Prefixes.Length * Motifs.Length * Endings.Length != ExpectedCount)
            throw new InvalidOperationException("Technique naming matrix must produce exactly 10,000 unique combinations.");
        if (RarityTargets.Values.Sum() != ExpectedCount)
            throw new InvalidOperationException("Technique rarity quotas must total exactly 10,000.");

        var rarities = BuildRaritySlots(seed);
        var domains = Enum.GetValues<TechniqueDomain>();
        var result = new List<TechniqueDefinition>(ExpectedCount);
        var id = 1;

        for (var p = 0; p < Prefixes.Length; p++)
        for (var m = 0; m < Motifs.Length; m++)
        for (var e = 0; e < Endings.Length; e++)
        {
            var index = id - 1;
            var domain = domains[PermuteIndex(index, domains.Length, seed + 17)];
            var rarity = rarities[index];
            var power = RarityPower(rarity);
            var complexity = Math.Clamp(0.12 + power * 0.13 + ((index * 37) % 100) / 220.0, 0.05, 1.0);
            var profession = IsProfessionDomain(domain);
            var potency = Math.Round(power * (0.65 + ((index * 53) % 70) / 100.0), 3);
            var qiCost = profession ? Math.Round(power * 0.35, 2) : Math.Round(power * (0.6 + complexity), 2);
            var stamina = Math.Round((profession ? 0.5 : 1.0) * power * (0.45 + complexity), 2);
            var injuryRisk = Math.Round(Math.Clamp((complexity - 0.35) * 0.18 + Math.Max(0, power - 2.5) * 0.025, 0.001, 0.35), 4);
            var name = $"{FrenchDomain(domain)} — {Prefixes[p]} {Motifs[m]} {Endings[e]}";
            result.Add(new TechniqueDefinition(
                id,
                $"TECH-{id:00000}",
                name,
                domain,
                rarity,
                potency,
                complexity,
                qiCost,
                stamina,
                injuryRisk,
                RealmFor(rarity),
                profession ? ["profession", domain.ToString().ToLowerInvariant()] : ["martial", domain.ToString().ToLowerInvariant()]));
            id++;
        }

        Validate(result);
        return result;
    }

    public static void Validate(IReadOnlyList<TechniqueDefinition> techniques)
    {
        if (techniques.Count != ExpectedCount) throw new InvalidOperationException($"Expected {ExpectedCount} techniques, got {techniques.Count}.");
        if (techniques.Select(t => t.Code).Distinct(StringComparer.Ordinal).Count() != ExpectedCount) throw new InvalidOperationException("Technique codes are not unique.");
        foreach (var pair in RarityTargets)
        {
            var actual = techniques.Count(t => t.Rarity == pair.Key);
            if (actual != pair.Value) throw new InvalidOperationException($"Rarity {pair.Key}: expected {pair.Value}, got {actual}.");
        }
    }

    private static Rarity[] BuildRaritySlots(int seed)
    {
        var slots = RarityTargets.SelectMany(kv => Enumerable.Repeat(kv.Key, kv.Value)).ToArray();
        var random = new Random(seed);
        for (var i = slots.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (slots[i], slots[j]) = (slots[j], slots[i]);
        }
        return slots;
    }

    private static int PermuteIndex(int index, int count, int seed)
    {
        unchecked
        {
            var x = (uint)(index + seed * 2654435761u);
            x ^= x >> 16;
            x *= 2246822519u;
            x ^= x >> 13;
            return (int)(x % (uint)count);
        }
    }

    private static double RarityPower(Rarity rarity) => rarity switch
    {
        Rarity.Ordinary => 0.55,
        Rarity.Uncommon => 0.78,
        Rarity.Rare => 1.08,
        Rarity.Superior => 1.45,
        Rarity.Legendary => 2.05,
        Rarity.Mythic => 3.15,
        Rarity.Divine => 5.0,
        _ => 1
    };

    private static MartialRealm RealmFor(Rarity rarity) => rarity switch
    {
        Rarity.Ordinary => MartialRealm.ThirdRate,
        Rarity.Uncommon => MartialRealm.SecondRate,
        Rarity.Rare => MartialRealm.FirstRate,
        Rarity.Superior => MartialRealm.Peak,
        Rarity.Legendary => MartialRealm.TranscendentPeak,
        Rarity.Mythic => MartialRealm.Transformation,
        Rarity.Divine => MartialRealm.Profound,
        _ => MartialRealm.Untrained
    };

    private static bool IsProfessionDomain(TechniqueDomain d) => d is TechniqueDomain.Healing or TechniqueDomain.Medicine or TechniqueDomain.Poison or TechniqueDomain.Alchemy or TechniqueDomain.Forging or TechniqueDomain.WeaponSmithing or TechniqueDomain.ArmorSmithing or TechniqueDomain.Cooking or TechniqueDomain.Brewing or TechniqueDomain.Herbalism or TechniqueDomain.Farming or TechniqueDomain.Hunting or TechniqueDomain.Tracking or TechniqueDomain.BeastTaming or TechniqueDomain.Fishing or TechniqueDomain.Mining or TechniqueDomain.Carpentry or TechniqueDomain.Masonry or TechniqueDomain.Tailoring or TechniqueDomain.Weaving or TechniqueDomain.Calligraphy or TechniqueDomain.Painting or TechniqueDomain.Music or TechniqueDomain.Scholarship or TechniqueDomain.Teaching or TechniqueDomain.Commerce or TechniqueDomain.Negotiation or TechniqueDomain.Accounting or TechniqueDomain.Leadership or TechniqueDomain.Strategy or TechniqueDomain.Espionage or TechniqueDomain.Thievery or TechniqueDomain.Assassination or TechniqueDomain.Escorting;

    private static string FrenchDomain(TechniqueDomain d) => d switch
    {
        TechniqueDomain.InternalCultivation => "Art interne", TechniqueDomain.BodyTempering => "Corps", TechniqueDomain.Sword => "Épée", TechniqueDomain.Saber => "Sabre",
        TechniqueDomain.Spear => "Lance", TechniqueDomain.Staff => "Bâton", TechniqueDomain.Fist => "Poing", TechniqueDomain.Palm => "Paume", TechniqueDomain.Finger => "Doigt",
        TechniqueDomain.Leg => "Jambe", TechniqueDomain.Movement => "Déplacement", TechniqueDomain.Qinggong => "Qinggong", TechniqueDomain.HiddenWeapon => "Arme cachée",
        TechniqueDomain.Archery => "Arc", TechniqueDomain.Grappling => "Lutte", TechniqueDomain.Formation => "Formation", TechniqueDomain.Healing => "Soin",
        TechniqueDomain.Medicine => "Médecine", TechniqueDomain.Poison => "Poison", TechniqueDomain.Alchemy => "Alchimie", TechniqueDomain.Forging => "Forge",
        TechniqueDomain.WeaponSmithing => "Forge d'armes", TechniqueDomain.ArmorSmithing => "Armurerie", TechniqueDomain.Cooking => "Cuisine", TechniqueDomain.Brewing => "Brassage",
        TechniqueDomain.Herbalism => "Herboristerie", TechniqueDomain.Farming => "Agriculture", TechniqueDomain.Hunting => "Chasse", TechniqueDomain.Tracking => "Pistage",
        TechniqueDomain.BeastTaming => "Dressage", TechniqueDomain.Fishing => "Pêche", TechniqueDomain.Mining => "Mine", TechniqueDomain.Carpentry => "Charpenterie",
        TechniqueDomain.Masonry => "Maçonnerie", TechniqueDomain.Tailoring => "Couture", TechniqueDomain.Weaving => "Tissage", TechniqueDomain.Calligraphy => "Calligraphie",
        TechniqueDomain.Painting => "Peinture", TechniqueDomain.Music => "Musique", TechniqueDomain.Scholarship => "Érudition", TechniqueDomain.Teaching => "Enseignement",
        TechniqueDomain.Commerce => "Commerce", TechniqueDomain.Negotiation => "Négociation", TechniqueDomain.Accounting => "Comptabilité", TechniqueDomain.Leadership => "Commandement",
        TechniqueDomain.Strategy => "Stratégie", TechniqueDomain.Espionage => "Espionnage", TechniqueDomain.Thievery => "Vol", TechniqueDomain.Assassination => "Assassinat",
        TechniqueDomain.Escorting => "Escorte", _ => DomainLabels[(int)d]
    };
}
