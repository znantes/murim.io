namespace Murim.Simulation;

public enum IngredientOrigin { HistoricalInspiration, Fictional }
public enum IngredientKind { Herb, Root, Mushroom, Flower, Fruit, Seed, Mineral, AnimalProduct, SpiritualMaterial }
public enum PillPurpose { Recovery, Fatigue, QiStability, BodyTempering, Focus, BreakthroughSupport, Detoxification, WoundCare, ColdResistance, HeatResistance, Sleep, Nutrition, BeastCare, CraftingAid, FictionalToxin }
public enum ArtifactType { Sword, Saber, Spear, Staff, Bow, HiddenWeaponCase, Armor, Robe, Boots, Belt, Ring, Bracelet, Pendant, Hairpin, Cauldron, Furnace, NeedleCase, Compass, Seal, Zither }

public sealed record IngredientDefinition(string Code, string Name, IngredientKind Kind, Rarity Rarity, IngredientOrigin Origin, IReadOnlyList<string> Tags, IReadOnlyList<string> Habitats, string Lore, string SafetyNote);
public sealed record PillDefinition(int Id, string Code, string Name, Rarity Rarity, PillPurpose Purpose, double Potency, double PurityDifficulty, int ShelfLifeDays, IReadOnlyList<string> RequiredIngredientTags, IReadOnlyList<string> SideEffectTags, string Description);
public sealed record ArtifactDefinition(int Id, string Code, string Name, ArtifactType Type, Rarity Rarity, string Material, string Aspect, double Durability, double QiEfficiency, double Prestige, IReadOnlyList<string> Traits);

public static class IngredientCatalog
{
    public static IReadOnlyList<IngredientDefinition> All { get; } = Build();
    private static IReadOnlyList<IngredientDefinition> Build()
    {
        var list = new List<IngredientDefinition>();
        void Real(string code, string name, IngredientKind kind, Rarity rarity, string[] tags, string[] habitats) => list.Add(new(code, name, kind, rarity, IngredientOrigin.HistoricalInspiration, tags, habitats,
            "Inspiration culturelle ou botanique réelle ; tous les effets de gameplay sont fictifs.", "Aucune recette, dose ou recommandation médicale réelle n'est fournie."));
        void Fiction(string code, string name, IngredientKind kind, Rarity rarity, string[] tags, string[] habitats) => list.Add(new(code, name, kind, rarity, IngredientOrigin.Fictional, tags, habitats,
            "Ingrédient entièrement fictif du monde Murim.", "Ressource de gameplay uniquement."));

        Real("ginseng", "Racine de ginseng", IngredientKind.Root, Rarity.Uncommon, ["root","vigor"], ["forêts froides","cultures"]);
        Real("lingzhi", "Lingzhi", IngredientKind.Mushroom, Rarity.Rare, ["mushroom","wood"], ["forêts humides","troncs anciens"]);
        Real("goji", "Baie de goji", IngredientKind.Fruit, Rarity.Ordinary, ["fruit","food"], ["plaines cultivées","marchés"]);
        Real("licorice", "Réglisse", IngredientKind.Root, Rarity.Ordinary, ["root","binder"], ["plaines sèches","cultures"]);
        Real("peony_root", "Racine de pivoine", IngredientKind.Root, Rarity.Uncommon, ["root","balance"], ["vallées","jardins"]);
        Real("poria", "Poria", IngredientKind.Mushroom, Rarity.Uncommon, ["mushroom","calm"], ["forêts de pins"]);
        Real("astragalus", "Astragale", IngredientKind.Root, Rarity.Uncommon, ["root","wind"], ["prairies","pentes sèches"]);
        Real("angelica", "Angélique chinoise", IngredientKind.Root, Rarity.Uncommon, ["root","aromatic"], ["montagnes","cultures"]);
        Real("chrysanthemum", "Chrysanthème", IngredientKind.Flower, Rarity.Ordinary, ["flower","tea"], ["jardins","fermes"]);
        Real("lotus_seed", "Graine de lotus", IngredientKind.Seed, Rarity.Ordinary, ["seed","food"], ["étangs","rizières"]);
        Real("jujube", "Jujube", IngredientKind.Fruit, Rarity.Ordinary, ["fruit","food"], ["vergers","villages"]);
        Real("ginger", "Gingembre", IngredientKind.Root, Rarity.Ordinary, ["root","food"], ["fermes","marchés"]);
        Real("tea_leaf", "Feuille de thé", IngredientKind.Herb, Rarity.Ordinary, ["tea","trade"], ["terrasses","collines"]);
        Real("sesame", "Sésame", IngredientKind.Seed, Rarity.Ordinary, ["seed","food"], ["fermes","marchés"]);

        Fiction("moon_dew", "Rosée de lune condensée", IngredientKind.SpiritualMaterial, Rarity.Superior, ["yin","water","purity"], ["falaises nocturnes"]);
        Fiction("sun_amber", "Ambre solaire", IngredientKind.Mineral, Rarity.Superior, ["yang","mineral","heat"], ["déserts"]);
        Fiction("jade_marrow", "Moelle de jade", IngredientKind.Mineral, Rarity.Legendary, ["earth","purity"], ["mines spirituelles"]);
        Fiction("cloud_orchid", "Orchidée des nuages", IngredientKind.Flower, Rarity.Rare, ["wind","focus"], ["pics brumeux"]);
        Fiction("ember_ginseng", "Ginseng de braise", IngredientKind.Root, Rarity.Rare, ["fire","vigor"], ["sols volcaniques"]);
        Fiction("frost_lotus", "Lotus de givre", IngredientKind.Flower, Rarity.Legendary, ["ice","calm"], ["lacs gelés"]);
        Fiction("thunder_moss", "Mousse du tonnerre", IngredientKind.Herb, Rarity.Rare, ["lightning","movement"], ["cols orageux"]);
        Fiction("iron_vine", "Liane de fer", IngredientKind.Herb, Rarity.Uncommon, ["metal","body"], ["ruines","carrières"]);
        Fiction("ghost_pearl", "Perle fantôme", IngredientKind.SpiritualMaterial, Rarity.Mythic, ["yin","spirit"], ["cavernes de brume"]);
        Fiction("dragon_scale_moss", "Mousse écaille-de-dragon", IngredientKind.Herb, Rarity.Superior, ["body","earth"], ["falaises anciennes"]);
        Fiction("phoenix_ash", "Cendre de phénix minérale", IngredientKind.Mineral, Rarity.Divine, ["fire","rebirth_lore"], ["sites légendaires"]);
        Fiction("void_silk", "Soie du vide", IngredientKind.SpiritualMaterial, Rarity.Mythic, ["space","stealth"], ["cocons spirituels"]);
        Fiction("black_spring_salt", "Sel de la Source Noire", IngredientKind.Mineral, Rarity.Rare, ["mineral","preserve"], ["sources souterraines"]);
        Fiction("red_moon_berry", "Baie de lune rouge", IngredientKind.Fruit, Rarity.Rare, ["fruit","night","qi"], ["forêts anciennes"]);
        Fiction("spirit_bamboo_pith", "Moelle de bambou spirituel", IngredientKind.SpiritualMaterial, Rarity.Superior, ["wood","focus"], ["bambouseraies protégées"]);
        Fiction("stoneheart_root", "Racine cœur-de-pierre", IngredientKind.Root, Rarity.Superior, ["earth","body"], ["montagnes"]);
        Fiction("mist_pear", "Poire de brume", IngredientKind.Fruit, Rarity.Uncommon, ["fruit","water"], ["vallées brumeuses"]);
        Fiction("silver_reed", "Roseau d'argent", IngredientKind.Herb, Rarity.Rare, ["metal","fiber"], ["marais","deltas"]);
        Fiction("quiet_flame_seed", "Graine de flamme calme", IngredientKind.Seed, Rarity.Legendary, ["fire","alchemy"], ["vallées géothermiques"]);
        Fiction("blue_stag_antler_shard", "Fragment de bois du cerf azuré", IngredientKind.AnimalProduct, Rarity.Superior, ["beast","bone"], ["forêts spirituelles"]);
        return list;
    }
}

public static class PillGenerator
{
    private static readonly string[] Forms = ["Pilule","Perle","Pastille","Granule"];
    private static readonly string[] Themes = ["Souffle","Jade","Brume","Fer","Lotus","Rivière","Pin","Aube","Nuit","Cœur","Bambou","Montagne"];
    public static IReadOnlyList<PillDefinition> Generate()
    {
        var result = new List<PillDefinition>(1260); var id = 1;
        foreach (var purpose in Enum.GetValues<PillPurpose>())
        foreach (var rarity in Enum.GetValues<Rarity>())
        foreach (var theme in Themes)
        {
            var power = RarityPower(rarity); var form = Forms[(id + (int)purpose) % Forms.Length];
            result.Add(new(id, $"PILL-{id:0000}", $"{form} du {theme} — {PurposeLabel(purpose)}", rarity, purpose,
                Math.Round(power * (.65 + ((id * 17) % 45) / 100.0), 3), Math.Round(Math.Clamp(.08 + power * .14 + ((id * 13) % 25) / 100.0, .05, 1), 3),
                rarity switch { Rarity.Ordinary => 90, Rarity.Uncommon => 180, Rarity.Rare => 360, Rarity.Superior => 720, Rarity.Legendary => 1800, Rarity.Mythic => 3600, Rarity.Divine => 12000, _ => 90 },
                TagsFor(purpose), SideEffectsFor(purpose, rarity), purpose == PillPurpose.FictionalToxin ? "Toxine entièrement fictive, sans substance, dose ou méthode réelle." : "Préparation d'alchimie fictive ; son effet dépend du personnage et de la pureté de jeu."));
            id++;
        }
        if (result.Count != 1260) throw new InvalidOperationException("Pill matrix must produce exactly 1,260 entries.");
        return result;
    }
    private static double RarityPower(Rarity r) => r switch { Rarity.Ordinary => .6, Rarity.Uncommon => .8, Rarity.Rare => 1.1, Rarity.Superior => 1.5, Rarity.Legendary => 2.1, Rarity.Mythic => 3, Rarity.Divine => 4.5, _ => 1 };
    private static IReadOnlyList<string> TagsFor(PillPurpose p) => p switch
    {
        PillPurpose.Recovery => ["vigor","nutrition"], PillPurpose.Fatigue => ["calm","food"], PillPurpose.QiStability => ["purity","balance"], PillPurpose.BodyTempering => ["body","earth"],
        PillPurpose.Focus => ["focus","wind"], PillPurpose.BreakthroughSupport => ["qi","purity","rare"], PillPurpose.Detoxification => ["cool","purity"], PillPurpose.WoundCare => ["body","fiber"],
        PillPurpose.ColdResistance => ["fire","warm"], PillPurpose.HeatResistance => ["water","cool"], PillPurpose.Sleep => ["calm","night"], PillPurpose.Nutrition => ["food","fruit"],
        PillPurpose.BeastCare => ["beast","food"], PillPurpose.CraftingAid => ["mineral","alchemy"], PillPurpose.FictionalToxin => ["fictional_toxin","stealth"], _ => ["qi"]
    };
    private static IReadOnlyList<string> SideEffectsFor(PillPurpose p, Rarity r)
    {
        var side = new List<string>();
        if ((int)r <= (int)Rarity.Uncommon) side.Add("impuretés possibles");
        if (p is PillPurpose.BodyTempering or PillPurpose.BreakthroughSupport) side.Add("fatigue après effet");
        if (p == PillPurpose.FictionalToxin) side.Add("danger fictif élevé");
        if ((int)r >= (int)Rarity.Mythic) side.Add("compatibilité rare requise");
        return side;
    }
    private static string PurposeLabel(PillPurpose p) => p switch
    {
        PillPurpose.Recovery => "Récupération", PillPurpose.Fatigue => "Repos", PillPurpose.QiStability => "Qi stable", PillPurpose.BodyTempering => "Corps trempé", PillPurpose.Focus => "Esprit clair",
        PillPurpose.BreakthroughSupport => "Seuil", PillPurpose.Detoxification => "Purification", PillPurpose.WoundCare => "Soin", PillPurpose.ColdResistance => "Chaleur", PillPurpose.HeatResistance => "Fraîcheur",
        PillPurpose.Sleep => "Sommeil", PillPurpose.Nutrition => "Satiété", PillPurpose.BeastCare => "Bête paisible", PillPurpose.CraftingAid => "Fourneau", PillPurpose.FictionalToxin => "Ombre", _ => p.ToString()
    };
}

public static class ArtifactGenerator
{
    public const int ExpectedCount = 1_000;
    private static readonly string[] Materials = ["acier noir","fer météorique","jade blanc","bois foudre","bronze ancien","soie spirituelle","argent froid","or rouge","cristal de brume","os de bête spirituelle"];
    private static readonly string[] Aspects = ["Silence","Aube","Brume","Tonnerre","Rivière"];
    public static IReadOnlyList<ArtifactDefinition> Generate(int seed = 7771)
    {
        var types = Enum.GetValues<ArtifactType>();
        if (types.Length * Materials.Length * Aspects.Length != ExpectedCount) throw new InvalidOperationException("Artifact matrix must produce exactly 1,000 artifacts.");
        var list = new List<ArtifactDefinition>(ExpectedCount); var id = 1;
        foreach (var type in types) foreach (var material in Materials) foreach (var aspect in Aspects)
        {
            var roll = Hash(id, seed) % 10000; var rarity = roll switch { < 8 => Rarity.Divine, < 55 => Rarity.Mythic, < 240 => Rarity.Legendary, < 900 => Rarity.Superior, < 2700 => Rarity.Rare, < 5600 => Rarity.Uncommon, _ => Rarity.Ordinary };
            var power = rarity switch { Rarity.Ordinary => .7, Rarity.Uncommon => .9, Rarity.Rare => 1.2, Rarity.Superior => 1.55, Rarity.Legendary => 2.1, Rarity.Mythic => 3, Rarity.Divine => 4.5, _ => 1 };
            list.Add(new(id, $"ART-{id:0000}", $"{TypeLabel(type)} de {material} — {aspect}", type, rarity, material, aspect,
                Math.Round(50 + power * 18 + Hash(id, seed + 1) % 20, 2), Math.Round(.7 + power * .18, 3), Math.Round(power * 15, 2), [type.ToString().ToLowerInvariant(), aspect.ToLowerInvariant(), type is ArtifactType.Cauldron or ArtifactType.Furnace ? "crafting" : "equipment"])); id++;
        }
        return list;
    }
    private static uint Hash(int id, int seed) { unchecked { var x = (uint)id * 2654435761u ^ (uint)seed * 2246822519u; x ^= x >> 16; x *= 3266489917u; x ^= x >> 13; return x; } }
    private static string TypeLabel(ArtifactType t) => t switch
    {
        ArtifactType.Sword => "Épée", ArtifactType.Saber => "Sabre", ArtifactType.Spear => "Lance", ArtifactType.Staff => "Bâton", ArtifactType.Bow => "Arc", ArtifactType.HiddenWeaponCase => "Étui d'armes cachées",
        ArtifactType.Armor => "Armure", ArtifactType.Robe => "Robe", ArtifactType.Boots => "Bottes", ArtifactType.Belt => "Ceinture", ArtifactType.Ring => "Anneau", ArtifactType.Bracelet => "Bracelet",
        ArtifactType.Pendant => "Pendentif", ArtifactType.Hairpin => "Épingle", ArtifactType.Cauldron => "Chaudron", ArtifactType.Furnace => "Fourneau", ArtifactType.NeedleCase => "Étui d'aiguilles",
        ArtifactType.Compass => "Compas", ArtifactType.Seal => "Sceau", ArtifactType.Zither => "Cithare", _ => t.ToString()
    };
}
