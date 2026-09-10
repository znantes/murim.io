using System.Security.Cryptography;
using System.Text;

namespace Murim.Simulation;

public sealed record FactionDefinition(
    Guid Id,
    string Code,
    string Name,
    FactionType Type,
    FactionOrientation Orientation,
    string Region,
    bool Hereditary,
    bool OpenRecruitment,
    double Prestige,
    double BirthWeight,
    IReadOnlyList<TechniqueDomain> Specialties,
    IReadOnlyList<string> Values,
    string Description,
    bool GenreReference = false);

public static class FactionCatalog
{
    public static IReadOnlyList<FactionDefinition> All { get; } = Build();

    private static IReadOnlyList<FactionDefinition> Build()
    {
        var f = new List<FactionDefinition>();
        void Add(string code, string name, FactionType type, FactionOrientation orientation, string region, bool hereditary, bool open, double prestige, double birthWeight, TechniqueDomain[] specialties, string[] values, string description, bool reference = false)
            => f.Add(new FactionDefinition(StableGuid(code), code, name, type, orientation, region, hereditary, open, prestige, birthWeight, specialties, values, description, reference));

        // Traditional/common Murim touchstones. These names recur across many wuxia/muhyeop works;
        // runtime stories, NPCs, conflicts and techniques remain original to this project.
        Add("shaolin", "Temple Shaolin", FactionType.Sect, FactionOrientation.Orthodox, "Henan", false, true, 95, 0.25, [TechniqueDomain.Staff, TechniqueDomain.Palm, TechniqueDomain.BodyTempering], ["discipline", "compassion", "restraint"], "Grande institution monastique orthodoxe, influente mais non omnisciente.", true);
        Add("wudang", "Secte Wudang", FactionType.Sect, FactionOrientation.Orthodox, "Hubei", false, true, 94, 0.24, [TechniqueDomain.Sword, TechniqueDomain.InternalCultivation, TechniqueDomain.Palm], ["balance", "patience", "self-control"], "Tradition taoïste centrée sur l'interne et l'épée.", true);
        Add("mount_hua", "Secte du Mont Hua", FactionType.Sect, FactionOrientation.Orthodox, "Shaanxi", false, true, 91, 0.23, [TechniqueDomain.Sword, TechniqueDomain.Qinggong], ["precision", "honor", "lineage"], "École d'épée de montagne aux générations fortement structurées.", true);
        Add("emei", "Secte Emei", FactionType.Sect, FactionOrientation.Orthodox, "Sichuan", false, true, 88, 0.22, [TechniqueDomain.Sword, TechniqueDomain.Palm, TechniqueDomain.Healing], ["discipline", "clarity", "service"], "Grande tradition de montagne aux écoles internes variées.", true);
        Add("kunlun", "Secte Kunlun", FactionType.Sect, FactionOrientation.Orthodox, "Ouest", false, true, 87, 0.18, [TechniqueDomain.Sword, TechniqueDomain.Qinggong], ["endurance", "distance", "austerity"], "Secte éloignée, façonnée par les routes de haute montagne.", true);
        Add("qingcheng", "Secte Qingcheng", FactionType.Sect, FactionOrientation.Orthodox, "Sichuan", false, true, 84, 0.19, [TechniqueDomain.Sword, TechniqueDomain.Movement], ["discipline", "subtlety"], "Secte orthodoxe connue pour sa mobilité et sa rigueur.", true);
        Add("kongtong", "Secte Kongtong", FactionType.Sect, FactionOrientation.Orthodox, "Gansu", false, true, 82, 0.16, [TechniqueDomain.Fist, TechniqueDomain.Sword], ["fortitude", "tradition"], "École ancienne de l'ouest, moins nombreuse mais respectée.", true);
        Add("zhongnan", "Secte Zhongnan", FactionType.Sect, FactionOrientation.Orthodox, "Shaanxi", false, true, 81, 0.16, [TechniqueDomain.InternalCultivation, TechniqueDomain.Sword], ["study", "balance"], "Communauté de montagne où étude et pratique interne se mêlent.", true);
        Add("diancang", "Secte Diancang", FactionType.Sect, FactionOrientation.Orthodox, "Yunnan", false, true, 79, 0.13, [TechniqueDomain.Sword, TechniqueDomain.Movement], ["adaptation", "regional_pride"], "Secte du sud-ouest, influencée par ses routes et cultures frontalières.", true);
        Add("beggars_union", "Union des Mendiants", FactionType.Alliance, FactionOrientation.Orthodox, "Réseau national", false, true, 86, 0.20, [TechniqueDomain.Staff, TechniqueDomain.Espionage, TechniqueDomain.Movement], ["mutual_aid", "information", "freedom"], "Réseau lâche de mendiants, voyageurs et informateurs ; sa force dépend de ses contacts.", true);

        Add("namgung", "Grande Famille Namgung", FactionType.GreatFamily, FactionOrientation.Orthodox, "Anhui", true, false, 96, 0.035, [TechniqueDomain.Sword, TechniqueDomain.InternalCultivation], ["bloodline", "reputation", "duty"], "Grande lignée d'épéistes. Naître dans cette maison est possible mais rare.", true);
        Add("sichuan_tang", "Famille Tang du Sichuan", FactionType.GreatFamily, FactionOrientation.Orthodox, "Sichuan", true, false, 94, 0.035, [TechniqueDomain.HiddenWeapon, TechniqueDomain.Poison, TechniqueDomain.Medicine], ["family", "secrecy", "craft"], "Lignée de spécialistes d'armes cachées et de toxicologie fictive.", true);
        Add("hebei_peng", "Famille Peng du Hebei", FactionType.GreatFamily, FactionOrientation.Orthodox, "Hebei", true, false, 90, 0.035, [TechniqueDomain.Saber, TechniqueDomain.BodyTempering], ["strength", "family", "directness"], "Grande famille martiale de tradition robuste.", true);
        Add("zhuge", "Grande Famille Zhuge", FactionType.GreatFamily, FactionOrientation.Orthodox, "Hubei", true, false, 91, 0.035, [TechniqueDomain.Strategy, TechniqueDomain.Formation, TechniqueDomain.Scholarship], ["planning", "education", "family"], "Maison influente d'administrateurs, stratèges et experts des formations.", true);
        Add("murong", "Grande Famille Murong", FactionType.GreatFamily, FactionOrientation.Neutral, "Nord-est", true, false, 89, 0.035, [TechniqueDomain.Sword, TechniqueDomain.Negotiation], ["lineage", "adaptation", "prestige"], "Ancienne lignée aristocratique aux alliances changeantes.", true);

        // Original factions: free to expand without tying the simulation to one copyrighted setting.
        Add("azure_pine", "Secte du Pin d'Azur", FactionType.Sect, FactionOrientation.Orthodox, "Monts de Haeryeong", false, true, 62, 0.55, [TechniqueDomain.Sword, TechniqueDomain.Healing], ["service", "restraint", "craft"], "Petite secte régionale vivant de soins, cours d'épée et protection des routes.");
        Add("silent_river", "École de la Rivière Silencieuse", FactionType.Sect, FactionOrientation.Neutral, "Vallée de Seoryeon", false, true, 58, 0.48, [TechniqueDomain.Palm, TechniqueDomain.Movement, TechniqueDomain.Fishing], ["patience", "privacy", "local_loyalty"], "École locale mêlant pêche, batellerie et arts de paume.");
        Add("red_crane", "Pavillon de la Grue Rouge", FactionType.Sect, FactionOrientation.Unorthodox, "Région centrale", false, true, 67, 0.33, [TechniqueDomain.Leg, TechniqueDomain.Qinggong, TechniqueDomain.Espionage], ["freedom", "favors", "survival"], "Association d'itinérants qui vend informations et services, sans allégeance stable.");
        Add("black_lotus", "Culte du Lotus Noir", FactionType.Cult, FactionOrientation.Demonic, "Plateau occidental", false, true, 92, 0.06, [TechniqueDomain.InternalCultivation, TechniqueDomain.Assassination, TechniqueDomain.Saber], ["devotion", "strength", "secrecy"], "Puissance démoniaque originale divisée en branches rivales.");
        Add("heavenly_flame", "Église de la Flamme Céleste", FactionType.Cult, FactionOrientation.Demonic, "Désert de Garan", false, true, 83, 0.05, [TechniqueDomain.Palm, TechniqueDomain.BodyTempering, TechniqueDomain.Alchemy], ["purification", "obedience", "power"], "Culte radical où alchimistes et combattants se disputent l'autorité.");
        Add("blood_scripture", "Secte du Sūtra Sanglant", FactionType.Sect, FactionOrientation.Demonic, "Frontière méridionale", false, false, 74, 0.03, [TechniqueDomain.InternalCultivation, TechniqueDomain.Finger, TechniqueDomain.Poison], ["secrecy", "revenge", "survival"], "Petite organisation interdite dont la réputation dépasse souvent la réalité.");

        Add("north_ice", "Palais de Glace du Nord", FactionType.Palace, FactionOrientation.OuterRegion, "Extrême Nord", true, true, 88, 0.08, [TechniqueDomain.Sword, TechniqueDomain.InternalCultivation], ["clan", "endurance", "sovereignty"], "Puissance frontalière indépendante dont les intérêts ne suivent pas toujours ceux des plaines centrales.");
        Add("southern_beast", "Palais des Cent Bêtes du Sud", FactionType.Palace, FactionOrientation.OuterRegion, "Jungles du Sud", true, true, 86, 0.08, [TechniqueDomain.BeastTaming, TechniqueDomain.Spear, TechniqueDomain.Herbalism], ["tribe", "beasts", "territory"], "Confédération de maisons et dresseurs de bêtes des terres méridionales.");
        Add("desert_sun", "Palais du Soleil de Verre", FactionType.Palace, FactionOrientation.OuterRegion, "Désert occidental", true, true, 80, 0.07, [TechniqueDomain.Saber, TechniqueDomain.Movement, TechniqueDomain.Commerce], ["hospitality", "trade", "retaliation"], "Palais-caravansérail contrôlant puits et routes commerciales.");
        Add("sea_mist", "Palais de la Brume Marine", FactionType.Palace, FactionOrientation.OuterRegion, "Archipels orientaux", true, true, 78, 0.07, [TechniqueDomain.Sword, TechniqueDomain.Fishing, TechniqueDomain.Formation], ["navigation", "family", "secrecy"], "Pouvoir maritime tourné vers navigation, commerce et défense côtière.");

        Add("greenwood", "Alliance de la Forêt Verte", FactionType.BanditStronghold, FactionOrientation.Criminal, "Routes intérieures", false, true, 71, 0.16, [TechniqueDomain.Saber, TechniqueDomain.Hunting, TechniqueDomain.Strategy], ["survival", "tribute", "territory"], "Coalition instable de forteresses de bandits ; certaines pillent, d'autres prélèvent des péages.", true);
        Add("river_eighteen", "Dix-Huit Forts du Grand Fleuve", FactionType.BanditStronghold, FactionOrientation.Criminal, "Grand Fleuve", false, true, 66, 0.12, [TechniqueDomain.Saber, TechniqueDomain.Fishing, TechniqueDomain.Escorting], ["river", "profit", "brotherhood"], "Pirates fluviaux, contrebandiers et bateliers armés réunis par intérêt.");
        Add("ghost_market", "Marché des Lanternes Grises", FactionType.Guild, FactionOrientation.Criminal, "Villes majeures", false, true, 64, 0.10, [TechniqueDomain.Commerce, TechniqueDomain.Espionage, TechniqueDomain.Thievery], ["anonymity", "profit", "information"], "Réseau de marchés clandestins où circulent objets volés et renseignements.");
        Add("hundred_ears", "Réseau des Cent Oreilles", FactionType.IntelligenceNetwork, FactionOrientation.Neutral, "Réseau national", false, true, 76, 0.07, [TechniqueDomain.Espionage, TechniqueDomain.Negotiation, TechniqueDomain.Scholarship], ["information", "verification", "profit"], "Courtiers en informations utilisant auberges, messagers et scribes.");

        Add("imperial_court", "Cour Impériale de Haedong", FactionType.ImperialOffice, FactionOrientation.Imperial, "Capitale", true, false, 100, 0.50, [TechniqueDomain.Leadership, TechniqueDomain.Strategy, TechniqueDomain.Scholarship], ["law", "taxation", "stability"], "Pouvoir civil et dynastique distinct du Murim, parfois partenaire, parfois rival.");
        Add("vermilion_bureau", "Bureau du Sceau Vermillon", FactionType.ImperialOffice, FactionOrientation.Imperial, "Empire", false, true, 85, 0.18, [TechniqueDomain.Sword, TechniqueDomain.Espionage, TechniqueDomain.Grappling], ["law", "investigation", "obedience"], "Corps d'enquêteurs chargés d'affaires sensibles, contrebande et violences interprovinciales.");
        Add("border_army", "Armée des Marches", FactionType.ImperialOffice, FactionOrientation.Imperial, "Frontières", false, true, 79, 0.35, [TechniqueDomain.Spear, TechniqueDomain.Archery, TechniqueDomain.Strategy], ["discipline", "duty", "survival"], "Garnisons et troupes de frontière recrutant surtout des gens ordinaires.");
        Add("imperial_medicine", "Office Médical Impérial", FactionType.ImperialOffice, FactionOrientation.Imperial, "Capitale", false, true, 82, 0.08, [TechniqueDomain.Medicine, TechniqueDomain.Healing, TechniqueDomain.Herbalism], ["study", "service", "standards"], "Institution savante chargée de la médecine de cour et des archives botaniques du monde fictif.");

        Add("azure_escort", "Agence d'Escorte de la Rivière Azurée", FactionType.EscortAgency, FactionOrientation.Commercial, "Région centrale", false, true, 61, 0.25, [TechniqueDomain.Escorting, TechniqueDomain.Sword, TechniqueDomain.Tracking], ["contract", "reputation", "route_knowledge"], "Entreprise martiale protégeant personnes, lettres et marchandises.");
        Add("golden_caravan", "Compagnie de la Caravane d'Or", FactionType.MerchantCompany, FactionOrientation.Commercial, "Routes nationales", true, true, 72, 0.28, [TechniqueDomain.Commerce, TechniqueDomain.Negotiation, TechniqueDomain.Accounting], ["profit", "contracts", "network"], "Grande maison marchande employant courtiers, caravanes, gardes et entrepôts.");
        Add("jade_ledger", "Guilde du Registre de Jade", FactionType.Guild, FactionOrientation.Commercial, "Grandes villes", false, true, 68, 0.22, [TechniqueDomain.Accounting, TechniqueDomain.Commerce, TechniqueDomain.Scholarship], ["credit", "records", "trust"], "Réseau de changeurs, prêteurs et comptables.");

        return f;
    }

    public static FactionDefinition? ByCode(string code) => All.FirstOrDefault(f => string.Equals(f.Code, code, StringComparison.OrdinalIgnoreCase));

    private static Guid StableGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("murim-faction:" + value));
        return new Guid(hash.AsSpan(0, 16));
    }
}
