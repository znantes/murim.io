namespace Murim.Simulation;

public enum IngredientOrigin { HistoricalInspiration, Fictional }
public enum IngredientKind { Herb, Root, Mushroom, Flower, Fruit, Seed, Mineral, AnimalProduct, SpiritualMaterial }
public enum PillPurpose { Recovery, Fatigue, QiStability, BodyTempering, Focus, BreakthroughSupport, Detoxification, WoundCare, ColdResistance, HeatResistance, Sleep, Nutrition, BeastCare, CraftingAid, FictionalToxin }
public enum ArtifactType { Sword, Saber, Spear, Staff, Bow, HiddenWeaponCase, Armor, Robe, Boots, Belt, Ring, Bracelet, Pendant, Hairpin, Cauldron, Furnace, NeedleCase, Compass, Seal, Zither }

public sealed record IngredientDefinition(
    string Code,
    string Name,
    IngredientKind Kind,
    Rarity Rarity,
    IngredientOrigin Origin,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Habitats,
    string Lore,
    string SafetyNote);

public sealed record PillDefinition(
    int Id,
    string Code,
    string Name,
    Rarity Rarity,
    PillPurpose Purpose,
    double Potency,
    double PurityDifficulty,
    int ShelfLifeDays,
    IReadOnlyList<string> RequiredIngredientTags,
    IReadOnlyList<string> SideEffectTags,
    string Description);

public sealed record ArtifactDefinition(
    int Id,
    string Code,
    string Name,
    ArtifactType Type,
    Rarity Rarity,
    string Material,
    string Aspect,
    double Durability,
    double QiEfficiency,
    double Prestige,
    IReadOnlyList<string> Traits);

public static class IngredientCatalog
{
    public static IReadOnlyList<IngredientDefinition> All { get; } = Build();

    private static IReadOnlyList<IngredientDefinition> Build()
    {
        var list = new List<IngredientDefinition>();
        void Real(string code, string name, IngredientKind kind, Rarity rarity, string[] tags, string[] habitats, string lore)
            => list.Add(new IngredientDefinition(code, name, kind, rarity, IngredientOrigin.HistoricalInspiration, tags, habitats, lore,
                "Inspiration historique/botanique uniquement : aucun effet médical réel ni recette réelle n'est simulé."));
        void Fiction(string code, string name, IngredientKind kind, Rarity rarity, string[] tags, string[] habitats, string lore)
            => list.Add(new IngredientDefinition(code, name, kind, rarity, IngredientOrigin.Fictional, tags, habitats, lore,
                "Ingrédient entièrement fictif destiné au gameplay."));

        Real("ginseng", "Racine de ginseng", IngredientKind.Root, Rarity.Uncommon, ["root", "vigor", "bitter"], ["forêts froides", "cultures spécialisées"], "Plante réelle connue historiquement en Asie ; le jeu lui attribue des propriétés purement fictives.");
        Real("lingzhi", "Lingzhi", IngredientKind.Mushroom, Rarity.Rare, ["mushroom", "longevity_lore", "wood"], ["forêts humides", "troncs anciens"], "Champignon réel doté d'une longue histoire culturelle ; sa puissance de Murim est fictive.");
        Real("goji", "Baie de goji", IngredientKind.Fruit, Rarity.Ordinary, ["fruit", "nutrition", "red"], ["plaines cultivées", "lisières"], "Fruit réel utilisé ici comme matériau de cuisine et d'alchimie fictive.");
        Real("licorice", "Réglisse", IngredientKind.Root, Rarity.Ordinary, ["root", "binder", "sweet"], ["plaines sèches", "cultures"], "Racine réelle ; dans le jeu elle sert surtout de liant alchimique abstrait.");
        Real("peony_root", "Racine de pivoine", IngredientKind.Root, Rarity.Uncommon, ["root", "cool", "balance"], ["vallées", "jardins médicinaux"], "Inspiration de pharmacopées historiques, sans recette réelle.");
        Real("rehmannia", "Rehmannia", IngredientKind.Root, Rarity.Uncommon, ["root", "earth", "dense"], ["champs", "collines"], "Plante réelle transformée en ressource de fiction.");
        Real("poria", "Poria", IngredientKind.Mushroom, Rarity.Uncommon, ["mushroom", "dry", "calm"], ["forêts de pins", "sols boisés"], "Champignon réel ; effets de jeu entièrement inventés.");
        Real("astragalus", "Astragale", IngredientKind.Root, Rarity.Uncommon, ["root", "wind", "vigor"], ["prairies", "pentes sèches"], "Plante réelle utilisée uniquement comme inspiration visuelle et économique.");
        Real("chuanxiong", "Rhizome de chuanxiong", IngredientKind.Root, Rarity.Uncommon, ["rhizome", "aromatic", "movement"], ["vallées humides", "cultures"], "Inspiration historique ; effets Murim inventés.");
        Real("angelica", "Angélique chinoise", IngredientKind.Root, Rarity.Uncommon, ["root", "aromatic", "warm"], ["montagnes", "cultures"], "Plante réelle, représentée sans conseil d'usage réel.");
        Real("chrysanthemum", "Chrysanthème", IngredientKind.Flower, Rarity.Ordinary, ["flower", "tea", "cool"], ["jardins", "fermes"], "Fleur réelle utilisée aussi comme ingrédient de cuisine fictive.");
        Real("lotus_seed", "Graine de lotus", IngredientKind.Seed, Rarity.Ordinary, ["seed", "food", "calm"], ["étangs", "rizières"], "Aliment réel servant ici de ressource culinaire et d'alchimie abstraite.");
        Real("jujube", "Jujube", IngredientKind.Fruit, Rarity.Ordinary, ["fruit", "food", "sweet"], ["vergers", "villages"], "Fruit réel ; bonus de jeu non médical.");
        Real("ginger", "Gingembre", IngredientKind.Root, Rarity.Ordinary, ["root", "food", "warm"], ["fermes", "marchés"], "Épice réelle ; effets de Murim fictifs.");
        Real("mint", "Menthe", IngredientKind.Herb, Rarity.Ordinary, ["herb", "tea", "cool"], ["berges", "jardins"], "Herbe réelle ; mécanique de jeu fictive.");
        Real("hawthorn", "Aubépine", IngredientKind.Fruit, Rarity.Ordinary, ["fruit", "sour", "food"], ["collines", "vergers"], "Fruit réel utilisé comme nourriture et ressource commerciale.");
        Real("lotus_leaf", "Feuille de lotus", IngredientKind.Herb, Rarity.Ordinary, ["leaf", "water", "wrap"], ["étangs", "lacs"], "Plante réelle, utile au jeu pour cuisine et emballage.");
        Real("bamboo_shoot", "Pousse de bambou", IngredientKind.Herb, Rarity.Ordinary, ["food", "wood", "spring"], ["bambouseraies"], "Aliment réel, sans revendication médicale.");
        Real("tea_leaf", "Feuille de thé", IngredientKind.Herb, Rarity.Ordinary, ["tea", "trade", "aromatic"], ["terrasses", "collines"], "Produit commercial réel, important pour auberges et caravanes.");
        Real("sesame", "Sésame", IngredientKind.Seed, Rarity.Ordinary, ["seed", "food", "oil"], ["fermes", "marchés"], "Graine réelle utilisée en cuisine.");

        Fiction("moon_dew", "Rosée de lune condensée", IngredientKind.SpiritualMaterial, Rarity.Superior, ["yin", "water", "purity"], ["falaises nocturnes", "grottes ouvertes au ciel"], "Condensation de qi nocturne récoltable seulement lors de certaines saisons.");
        Fiction("sun_amber", "Ambre solaire", IngredientKind.Mineral, Rarity.Superior, ["yang", "mineral", "heat"], ["déserts", "veines exposées"], "Minéral fictif qui emmagasine lentement le qi chaud.");
        Fiction("jade_marrow", "Moelle de jade", IngredientKind.Mineral, Rarity.Legendary, ["earth", "mineral", "purity"], ["mines spirituelles profondes"], "Gel minéral fictif trouvé au cœur de rares filons de jade.");
        Fiction("cloud_orchid", "Orchidée des nuages", IngredientKind.Flower, Rarity.Rare, ["wind", "flower", "focus"], ["hautes falaises", "pics brumeux"], "Fleur fictive poussant au-dessus des nappes de brouillard.");
        Fiction("ember_ginseng", "Ginseng de braise", IngredientKind.Root, Rarity.Rare, ["fire", "root", "vigor"], ["sols volcaniques", "sources chaudes"], "Parent fantastique du ginseng, sans équivalent réel.");
        Fiction("frost_lotus", "Lotus de givre", IngredientKind.Flower, Rarity.Legendary, ["ice", "flower", "calm"], ["lacs gelés", "palais du nord"], "Lotus fictif fleurissant sous une mince couche de glace spirituelle.");
        Fiction("thunder_moss", "Mousse du tonnerre", IngredientKind.Herb, Rarity.Rare, ["lightning", "herb", "movement"], ["arbres frappés par l'orage", "cols"], "Mousse fictive qui réagit aux variations de qi.");
        Fiction("iron_vine", "Liane de fer", IngredientKind.Herb, Rarity.Uncommon, ["metal", "fiber", "body"], ["ruines", "carrières"], "Liane fictive fibreuse employée en forge légère et remèdes de jeu.");
        Fiction("ghost_pearl", "Perle fantôme", IngredientKind.SpiritualMaterial, Rarity.Mythic, ["yin", "spirit", "rare"], ["cavernes de brume", "ruines scellées"], "Nodule de qi fictif, recherché par alchimistes et collectionneurs.");
        Fiction("dragon_scale_moss", "Mousse écaille-de-dragon", IngredientKind.Herb, Rarity.Superior, ["body", "earth", "scale"], ["falaises anciennes", "grottes de bêtes"], "Mousse dure fictive dont les motifs rappellent des écailles.");
        Fiction("phoenix_ash", "Cendre de phénix minérale", IngredientKind.Mineral, Rarity.Divine, ["fire", "rebirth_lore", "ash"], ["sites légendaires"], "Minéral mythique nommé d'après le phénix ; il ne provient pas nécessairement d'un animal réel.");
        Fiction("void_silk", "Soie du vide", IngredientKind.SpiritualMaterial, Rarity.Mythic, ["space", "fiber", "stealth"], ["cocons de bêtes spirituelles rares"], "Fibre fictive qui atténue la circulation visible du qi.");
        Fiction("black_spring_salt", "Sel de la Source Noire", IngredientKind.Mineral, Rarity.Rare, ["mineral", "water", "preserve"], ["sources souterraines"], "Sel fictif recherché par cuisiniers et alchimistes.");
        Fiction("red_moon_berry", "Baie de lune rouge", IngredientKind.Fruit, Rarity.Rare, ["fruit", "night", "qi"], ["forêts anciennes"], "Fruit fictif dont la maturation dépend du calendrier lunaire du monde.");
        Fiction("spirit_bamboo_pith", "Moelle de bambou spirituel", IngredientKind.SpiritualMaterial, Rarity.Superior, ["wood", "focus", "fiber"], ["bambouseraies protégées"], "Ressource fictive utilisée en cuisine, papier et alchimie.");
        Fiction("stoneheart_root", "Racine cœur-de-pierre", IngredientKind.Root, Rarity.Superior, ["earth", "root", "body"], ["éboulis", "montagnes"], "Racine fictive qui pousse dans les fissures rocheuses.");
        Fiction("mist_pear", "Poire de brume", IngredientKind.Fruit, Rarity.Uncommon, ["fruit", "water", "food"], ["vallées brumeuses"], "Fruit fictif apprécié dans les auberges de montagne.");
        Fiction("silver_reed", "Roseau d'argent", IngredientKind.Herb, Rarity.Rare, ["metal", "water", "fiber"], ["marais", "deltas"], "Plante fictive servant à fabriquer fils et aiguilles légères.");
        Fiction("quiet_flame_seed", "Graine de flamme calme", IngredientKind.Seed, Rarity.Legendary, ["fire", "seed", "alchemy"], ["vallées géothermiques"], "Graine fictive restant tiède sans brûler son contenant.");
        Fiction("blue_stag_antler_shard", "Fragment de bois du cerf azuré", IngredientKind.AnimalProduct, Rarity.Superior, ["beast", "bone", "qi"], ["forêts spirituelles"], "Matériau obtenu dans le monde fictif par commerce, chute naturelle ou chasse.");

        return list;
    }
}

public static class PillGenerator
{
    private static readonly string[] Forms = ["Pilule", "Perle", "Pastille", "Granule"];
    private static readonly string[] Themes = ["Souffle", "Jade", "Brume", "Fer", "Lotus", "Rivière", "Pin", "Aube", "Nuit", "Cœur", "Bambou", "Montagne"];

    public static IReadOnlyList<PillDefinition> Generate()
    {
        var purposes = Enum.GetValues<PillPurpose>();
        var rarities = Enum.GetValues<Rarity>();
        var result = new List<PillDefinition>();
        var id = 1;
        foreach (var purpose in purposes)
        foreach (var rarity in rarities)
        foreach (var theme in Themes)
        {
            // Keep the catalogue broad but finite: 15 × 7 × 12 = 1,260 fictional preparations.
            var form = Forms[(id + (int)purpose) % Forms.Length];
            var potency = RarityPower(rarity) * (0.65 + ((id * 17) % 45) / 100.0);
            var difficulty = Math.Clamp(0.08 + RarityPower(rarity) * 0.14 + ((id * 13) % 25) / 100.0, 0.05, 1.0);
            var shelf = rarity switch { Rarity.Ordinary => 90, Rarity.Uncommon => 180, Rarity.Rare => 360, Rarity.Superior => 720, Rarity.Legendary => 1800, Rarity.Mythic => 3600, Rarity.Divine => 12000, _ => 90 };
            result.Add(new PillDefinition(
                id, $"PILL-{id:0000}", $"{form} du {theme} — {PurposeLabel(purpose)}", rarity, purpose,
                Math.Round(potency, 3), Math.Round(difficulty, 3), shelf,
                TagsFor(purpose), SideEffectsFor(purpose, rarity),
                purpose == PillPurpose.FictionalToxin
                    ? "Préparation toxique entièrement fictive. Le jeu ne fournit ni substance réelle, ni dosage, ni procédé de fabrication réel."
                    : "Préparation d'alchimie fictive dont l'effet dépend de la pureté, de l'état du personnage et de sa compatibilité."));
            id++;
        }
        return result;
    }

    private static double RarityPower(Rarity r) => r switch { Rarity.Ordinary => 0.6, Rarity.Uncommon => 0.8, Rarity.Rare => 1.1, Rarity.Superior => 1.5, Rarity.Legendary => 2.1, Rarity.Mythic => 3.0, Rarity.Divine => 4.5, _ => 1 };
    private static IReadOnlyList<string> TagsFor(PillPurpose p) => p switch
    {
        PillPurpose.Recovery => ["vigor", "nutrition"], PillPurpose.Fatigue => ["calm", "food"], PillPurpose.QiStability => ["purity", "balance"],
        PillPurpose.BodyTempering => ["body", "earth"], PillPurpose.Focus => ["focus", "wind"], PillPurpose.BreakthroughSupport => ["qi", "purity", "rare"],
        PillPurpose.Detoxification => ["cool", "purity"], PillPurpose.WoundCare => ["body", "fiber"], PillPurpose.ColdResistance => ["fire", "warm"],
        PillPurpose.HeatResistance => ["water", "cool"], PillPurpose.Sleep => ["calm", "night"], PillPurpose.Nutrition => ["food", "fruit"],
        PillPurpose.BeastCare => ["beast", "food"], PillPurpose.CraftingAid => ["mineral", "alchemy"], PillPurpose.FictionalToxin => ["fictional_toxin", "stealth"], _ => ["qi"]
    };
    private static IReadOnlyList<string> SideEffectsFor(PillPurpose p, Rarity r)
    {
        var side = new List<string>();
        if (r <= Rarity.Uncommon) side.Add("impuretés possibles");
        if (p is PillPurpose.BodyTempering or PillPurpose.BreakthroughSupport) side.Add("fatigue après effet");
        if (p == PillPurpose.FictionalToxin) side.Add("danger fictif élevé");
        if (r >= Rarity.Mythic) side.Add("compatibilité rare requise");
        return side;
    }
    private static string PurposeLabel(PillPurpose p) => p switch
    {
        PillPurpose.Recovery => "Récupération", PillPurpose.Fatigue => "Repos", PillPurpose.QiStability => "Qi Stable", PillPurpose.BodyTempering => "Corps Trempé",
        PillPurpose.Focus => "Esprit Clair", PillPurpose.BreakthroughSupport => "Seuil", PillPurpose.Detoxification => "Purification", PillPurpose.WoundCare => "Soin",
        PillPurpose.ColdResistance => "Chaleur", PillPurpose.HeatResistance => "Fraîcheur", PillPurpose.Sleep => "Sommeil", PillPurpose.Nutrition => "Satiété",
        PillPurpose.BeastCare => "Bête Paisible", PillPurpose.CraftingAid => "Fourneau", PillPurpose.FictionalToxin => "Ombre", _ => p.ToString()
    };
}

public static class ArtifactGenerator
{
    public const int ExpectedCount = 1_000;
    private static readonly string[] Materials = ["acier noir", "fer météorique", "jade blanc", "bois foudre", "bronze ancien", "soie spirituelle", "argent froid", "or rouge", "cristal de brume", "os de bête spirituelle"];
    private static readonly string[] Aspects = ["Silence", "Aube", "Brume", "Tonnerre", "Rivière"];

    public static IReadOnlyList<ArtifactDefinition> Generate(int seed = 7771)
    {
        var types = Enum.GetValues<ArtifactType>();
        if (types.Length * Materials.Length * Aspects.Length != ExpectedCount) throw new InvalidOperationException("Artifact matrix must produce exactly 1,000 artifacts.");
        var list = new List<ArtifactDefinition>(ExpectedCount);
        var id = 1;
        foreach (var type in types)
        foreach (var material in Materials)
        foreach (var aspect in Aspects)
        {
            var roll = Hash(id, seed) % 10000;
            var rarity = roll switch { < 8 => Rarity.Divine, < 55 => Rarity.Mythic, < 240 => Rarity.Legendary, < 900 => Rarity.Superior, < 2700 => Rarity.Rare, < 5600 => Rarity.Uncommon, _ => Rarity.Ordinary };
            var power = rarity switch { Rarity.Ordinary => 0.7, Rarity.Uncommon => 0.9, Rarity.Rare => 1.2, Rarity.Superior => 1.55, Rarity.Legendary => 2.1, Rarity.Mythic => 3.0, Rarity.Divine => 4.5, _ => 1 };
            list.Add(new ArtifactDefinition(id, $"ART-{id:0000}", $"{TypeLabel(type)} de {material} — {aspect}", type, rarity, material, aspect,
                Math.Round(50 + power * 18 + Hash(id, seed + 1) % 20, 2), Math.Round(0.7 + power * 0.18, 3), Math.Round(power * 15, 2),
                Traits(type, aspect)));
            id++;
        }
        if (list.Select(x => x.Code).Distinct().Count() != ExpectedCount) throw new InvalidOperationException("Artifact codes are not unique.");
        return list;
    }

    private static uint Hash(int id, int seed)
    {
        unchecked
        {
            var x = (uint)id * 2654435761u ^ (uint)seed * 2246822519u;
            x ^= x >> 16; x *= 3266489917u; x ^= x >> 13; return x;
        }
    }
    private static IReadOnlyList<string> Traits(ArtifactType type, string aspect) => [type.ToString().ToLowerInvariant(), aspect.ToLowerInvariant(), type is ArtifactType.Cauldron or ArtifactType.Furnace ? "crafting" : "equipment"];
    private static string TypeLabel(ArtifactType t) => t switch
    {
        ArtifactType.Sword => "Épée", ArtifactType.Saber => "Sabre", ArtifactType.Spear => "Lance", ArtifactType.Staff => "Bâton", ArtifactType.Bow => "Arc",
        ArtifactType.HiddenWeaponCase => "Étui d'armes cachées", ArtifactType.Armor => "Armure", ArtifactType.Robe => "Robe", ArtifactType.Boots => "Bottes",
        ArtifactType.Belt => "Ceinture", ArtifactType.Ring => "Anneau", ArtifactType.Bracelet => "Bracelet", ArtifactType.Pendant => "Pendentif", ArtifactType.Hairpin => "Épingle",
        ArtifactType.Cauldron => "Chaudron", ArtifactType.Furnace => "Fourneau", ArtifactType.NeedleCase => "Étui d'aiguilles", ArtifactType.Compass => "Compas", ArtifactType.Seal => "Sceau", ArtifactType.Zither => "Cithare", _ => t.ToString()
    };
}
