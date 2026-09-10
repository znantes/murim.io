namespace Murim.Simulation;

public enum ProfessionCategory { Agriculture, Craft, Food, Trade, Scholarship, Medicine, Murim, Transport, Imperial, Entertainment, Service, Underworld, Wilderness, Spiritual }
public enum CareerRank { Novice, Apprentice, Worker, Skilled, Expert, Master, Grandmaster }

public sealed record ProfessionDefinition(
    string Code,
    string Name,
    ProfessionCategory Category,
    ReputationAlignment Alignment,
    bool Illegal,
    IReadOnlyList<TechniqueDomain> TechniqueDomains,
    IReadOnlyList<string> CoreSkills,
    string Description);

public sealed record CareerRecord(string ProfessionCode, CareerRank Rank, long StartDay, long? EndDay = null);

public static class ProfessionCatalog
{
    public static IReadOnlyList<ProfessionDefinition> All { get; } = Build();

    private static IReadOnlyList<ProfessionDefinition> Build()
    {
        var p = new List<ProfessionDefinition>();
        void Add(string code, string name, ProfessionCategory cat, ReputationAlignment rep, bool illegal, TechniqueDomain[] domains, string[] skills, string description)
            => p.Add(new ProfessionDefinition(code, name, cat, rep, illegal, domains, skills, description));

        Add("farmer", "Paysan", ProfessionCategory.Agriculture, ReputationAlignment.Neutral, false, [TechniqueDomain.Farming], ["farming", "weather", "tools"], "Cultive des terres et nourrit les communautés.");
        Add("rice_farmer", "Riziculteur", ProfessionCategory.Agriculture, ReputationAlignment.Neutral, false, [TechniqueDomain.Farming], ["farming", "irrigation"], "Spécialiste des cultures irriguées.");
        Add("tea_grower", "Cultivateur de thé", ProfessionCategory.Agriculture, ReputationAlignment.Respected, false, [TechniqueDomain.Farming, TechniqueDomain.Herbalism], ["farming", "tea"], "Produit et sélectionne les feuilles de thé.");
        Add("herbalist", "Herboriste", ProfessionCategory.Medicine, ReputationAlignment.Respected, false, [TechniqueDomain.Herbalism, TechniqueDomain.Medicine], ["herbalism", "botany"], "Identifie, cultive et prépare des plantes pour l'usage du monde fictif.");
        Add("woodcutter", "Bûcheron", ProfessionCategory.Wilderness, ReputationAlignment.Neutral, false, [TechniqueDomain.Carpentry, TechniqueDomain.BodyTempering], ["woodcutting", "survival"], "Abat et transporte le bois.");
        Add("miner", "Mineur", ProfessionCategory.Wilderness, ReputationAlignment.Neutral, false, [TechniqueDomain.Mining, TechniqueDomain.BodyTempering], ["mining", "ore"], "Extrait minerais, sel, charbon et pierres.");
        Add("fisher", "Pêcheur", ProfessionCategory.Wilderness, ReputationAlignment.Neutral, false, [TechniqueDomain.Fishing], ["fishing", "navigation"], "Vit des rivières, lacs et côtes.");
        Add("hunter", "Chasseur", ProfessionCategory.Wilderness, ReputationAlignment.Neutral, false, [TechniqueDomain.Hunting, TechniqueDomain.Tracking, TechniqueDomain.Archery], ["hunting", "tracking", "survival"], "Chasse le gibier et parfois les bêtes dangereuses.");
        Add("trapper", "Trappeur", ProfessionCategory.Wilderness, ReputationAlignment.Neutral, false, [TechniqueDomain.Hunting, TechniqueDomain.Tracking], ["trapping", "tracking"], "Pose des pièges et lit les traces.");
        Add("beast_tamer", "Dresseur de bêtes", ProfessionCategory.Wilderness, ReputationAlignment.Neutral, false, [TechniqueDomain.BeastTaming], ["taming", "animal_care"], "Éduque montures et bêtes spirituelles.");
        Add("cook", "Cuisinier", ProfessionCategory.Food, ReputationAlignment.Respected, false, [TechniqueDomain.Cooking], ["cooking", "ingredients"], "Prépare des repas ordinaires ou énergétiques.");
        Add("inn_cook", "Cuisinier d'auberge", ProfessionCategory.Food, ReputationAlignment.Neutral, false, [TechniqueDomain.Cooking], ["cooking", "service"], "Cuisine vite pour voyageurs, gardes et marchands.");
        Add("brewer", "Brasseur", ProfessionCategory.Food, ReputationAlignment.Neutral, false, [TechniqueDomain.Brewing], ["brewing", "fermentation"], "Prépare thés fermentés, alcools et boissons du monde.");
        Add("baker", "Boulanger", ProfessionCategory.Food, ReputationAlignment.Neutral, false, [TechniqueDomain.Cooking], ["baking"], "Produit pains, galettes et pâtisseries.");
        Add("butcher", "Boucher", ProfessionCategory.Food, ReputationAlignment.Neutral, false, [TechniqueDomain.Cooking, TechniqueDomain.Saber], ["butchery", "knife"], "Découpe et conserve les viandes.");
        Add("tailor", "Tailleur", ProfessionCategory.Craft, ReputationAlignment.Neutral, false, [TechniqueDomain.Tailoring], ["tailoring", "repair"], "Confectionne et répare les vêtements.");
        Add("weaver", "Tisserand", ProfessionCategory.Craft, ReputationAlignment.Neutral, false, [TechniqueDomain.Weaving], ["weaving", "fiber"], "Transforme fibres et soies en étoffes.");
        Add("dyer", "Teinturier", ProfessionCategory.Craft, ReputationAlignment.Neutral, false, [TechniqueDomain.Weaving], ["dyeing", "chemistry"], "Teint tissus, bannières et uniformes.");
        Add("carpenter", "Charpentier", ProfessionCategory.Craft, ReputationAlignment.Respected, false, [TechniqueDomain.Carpentry], ["carpentry", "construction"], "Travaille le bois, des meubles aux bâtiments.");
        Add("mason", "Maçon", ProfessionCategory.Craft, ReputationAlignment.Neutral, false, [TechniqueDomain.Masonry], ["masonry", "construction"], "Érige murs, ponts et bâtiments.");
        Add("potter", "Potier", ProfessionCategory.Craft, ReputationAlignment.Neutral, false, [TechniqueDomain.Carpentry], ["pottery", "kiln"], "Fabrique jarres, bols et contenants d'alchimie.");
        Add("papermaker", "Papetier", ProfessionCategory.Craft, ReputationAlignment.Neutral, false, [TechniqueDomain.Scholarship], ["papermaking"], "Produit le papier pour registres et manuels.");
        Add("inkmaker", "Fabricant d'encre", ProfessionCategory.Craft, ReputationAlignment.Neutral, false, [TechniqueDomain.Calligraphy], ["inkmaking"], "Prépare encres de qualité variable.");
        Add("blacksmith", "Forgeron", ProfessionCategory.Craft, ReputationAlignment.Respected, false, [TechniqueDomain.Forging], ["forging", "ore"], "Forge outils et pièces métalliques.");
        Add("weaponsmith", "Forgeron d'armes", ProfessionCategory.Craft, ReputationAlignment.Respected, false, [TechniqueDomain.WeaponSmithing, TechniqueDomain.Forging], ["weaponsmithing", "forging"], "Fabrique et entretient armes martiales.");
        Add("armorsmith", "Armurier", ProfessionCategory.Craft, ReputationAlignment.Respected, false, [TechniqueDomain.ArmorSmithing, TechniqueDomain.Forging], ["armorsmithing", "forging"], "Produit protections et armures.");
        Add("jeweler", "Joaillier", ProfessionCategory.Craft, ReputationAlignment.Respected, false, [TechniqueDomain.Forging], ["jewelry", "appraisal"], "Travaille métaux précieux et gemmes.");
        Add("jade_carver", "Sculpteur de jade", ProfessionCategory.Craft, ReputationAlignment.Respected, false, [TechniqueDomain.Carpentry, TechniqueDomain.Calligraphy], ["jade", "carving"], "Fabrique sceaux, plaques et ornements.");
        Add("boatbuilder", "Constructeur de bateaux", ProfessionCategory.Craft, ReputationAlignment.Respected, false, [TechniqueDomain.Carpentry], ["shipbuilding", "carpentry"], "Construit embarcations marchandes et fluviales.");
        Add("merchant", "Marchand", ProfessionCategory.Trade, ReputationAlignment.Neutral, false, [TechniqueDomain.Commerce, TechniqueDomain.Negotiation, TechniqueDomain.Accounting], ["commerce", "negotiation", "accounting"], "Achète, transporte et revend des biens.");
        Add("peddler", "Colporteur", ProfessionCategory.Trade, ReputationAlignment.Neutral, false, [TechniqueDomain.Commerce, TechniqueDomain.Negotiation], ["commerce", "travel"], "Vend de petites marchandises en voyageant.");
        Add("broker", "Courtier", ProfessionCategory.Trade, ReputationAlignment.Neutral, false, [TechniqueDomain.Commerce, TechniqueDomain.Negotiation], ["brokerage", "contacts"], "Met en relation acheteurs, vendeurs et commanditaires.");
        Add("accountant", "Comptable", ProfessionCategory.Trade, ReputationAlignment.Respected, false, [TechniqueDomain.Accounting], ["accounting", "literacy"], "Tient livres de comptes et inventaires.");
        Add("appraiser", "Estimateur", ProfessionCategory.Trade, ReputationAlignment.Respected, false, [TechniqueDomain.Commerce, TechniqueDomain.Scholarship], ["appraisal", "artifacts"], "Évalue objets, armes, antiquités et ingrédients.");
        Add("innkeeper", "Aubergiste", ProfessionCategory.Service, ReputationAlignment.Neutral, false, [TechniqueDomain.Commerce, TechniqueDomain.Cooking], ["hospitality", "commerce"], "Héberge voyageurs et devient souvent un nœud de rumeurs.");
        Add("teahouse_keeper", "Tenancier de maison de thé", ProfessionCategory.Service, ReputationAlignment.Neutral, false, [TechniqueDomain.Commerce, TechniqueDomain.Brewing], ["hospitality", "tea", "gossip"], "Sert boissons et observe les conversations.");
        Add("stablemaster", "Maître d'écurie", ProfessionCategory.Service, ReputationAlignment.Neutral, false, [TechniqueDomain.BeastTaming], ["animal_care", "horses"], "Gère montures et relais.");
        Add("servant", "Serviteur", ProfessionCategory.Service, ReputationAlignment.Neutral, false, [TechniqueDomain.Cooking, TechniqueDomain.Tailoring], ["service", "household"], "Travaille pour une maison, une secte ou une auberge.");
        Add("steward", "Intendant", ProfessionCategory.Service, ReputationAlignment.Respected, false, [TechniqueDomain.Accounting, TechniqueDomain.Leadership], ["administration", "accounting"], "Administre domaine, personnel et ressources.");
        Add("scribe", "Scribe", ProfessionCategory.Scholarship, ReputationAlignment.Respected, false, [TechniqueDomain.Calligraphy, TechniqueDomain.Scholarship], ["literacy", "calligraphy"], "Copie contrats, lettres et manuels.");
        Add("teacher", "Précepteur", ProfessionCategory.Scholarship, ReputationAlignment.Respected, false, [TechniqueDomain.Teaching, TechniqueDomain.Scholarship], ["teaching", "literacy"], "Instruit enfants, disciples et apprentis.");
        Add("scholar", "Érudit", ProfessionCategory.Scholarship, ReputationAlignment.Respected, false, [TechniqueDomain.Scholarship, TechniqueDomain.Calligraphy], ["scholarship", "history"], "Étudie textes, histoire, droit et philosophie.");
        Add("librarian", "Gardien de bibliothèque", ProfessionCategory.Scholarship, ReputationAlignment.Respected, false, [TechniqueDomain.Scholarship, TechniqueDomain.Accounting], ["archives", "cataloguing"], "Protège et classe archives et manuels.");
        Add("calligrapher", "Calligraphe", ProfessionCategory.Entertainment, ReputationAlignment.Respected, false, [TechniqueDomain.Calligraphy], ["calligraphy", "art"], "Maîtrise l'écriture artistique et les sceaux.");
        Add("painter", "Peintre", ProfessionCategory.Entertainment, ReputationAlignment.Neutral, false, [TechniqueDomain.Painting], ["painting", "observation"], "Produit portraits, cartes et œuvres décoratives.");
        Add("musician", "Musicien", ProfessionCategory.Entertainment, ReputationAlignment.Neutral, false, [TechniqueDomain.Music], ["music", "performance"], "Joue pour cours, auberges, cérémonies ou sectes.");
        Add("storyteller", "Conteur", ProfessionCategory.Entertainment, ReputationAlignment.Neutral, false, [TechniqueDomain.Teaching, TechniqueDomain.Negotiation], ["storytelling", "gossip"], "Diffuse histoires, nouvelles et parfois rumeurs.");
        Add("physician", "Médecin", ProfessionCategory.Medicine, ReputationAlignment.Respected, false, [TechniqueDomain.Medicine, TechniqueDomain.Healing], ["medicine", "diagnosis"], "Soigne blessures et maladies selon les règles fictives du jeu.");
        Add("apothecary", "Apothicaire", ProfessionCategory.Medicine, ReputationAlignment.Respected, false, [TechniqueDomain.Medicine, TechniqueDomain.Herbalism], ["medicine", "herbalism", "commerce"], "Vend ingrédients et préparations fictives.");
        Add("alchemist", "Alchimiste de pilules", ProfessionCategory.Spiritual, ReputationAlignment.Respected, false, [TechniqueDomain.Alchemy, TechniqueDomain.Herbalism], ["alchemy", "fire_control", "ingredients"], "Raffine des pilules purement fictives destinées au gameplay.");
        Add("martial_instructor", "Maître d'armes", ProfessionCategory.Murim, ReputationAlignment.Respected, false, [TechniqueDomain.Teaching, TechniqueDomain.BodyTempering], ["combat", "teaching"], "Enseigne des fondations martiales contre paiement ou loyauté.");
        Add("sect_disciple", "Disciple de secte", ProfessionCategory.Murim, ReputationAlignment.Neutral, false, [TechniqueDomain.InternalCultivation, TechniqueDomain.BodyTempering], ["combat", "sect_discipline"], "Membre en formation d'une organisation martiale.");
        Add("sect_steward", "Intendant de secte", ProfessionCategory.Murim, ReputationAlignment.Respected, false, [TechniqueDomain.Accounting, TechniqueDomain.Leadership], ["administration", "sect_law"], "Gère ressources et tâches internes.");
        Add("formation_master", "Maître des formations", ProfessionCategory.Murim, ReputationAlignment.Respected, false, [TechniqueDomain.Formation, TechniqueDomain.Scholarship], ["formations", "geometry"], "Conçoit dispositifs, positions et défenses de groupe.");
        Add("escort_guard", "Garde d'escorte", ProfessionCategory.Transport, ReputationAlignment.Respected, false, [TechniqueDomain.Escorting, TechniqueDomain.Sword, TechniqueDomain.Tracking], ["escort", "combat", "route"], "Protège voyageurs et cargaisons.");
        Add("escort_captain", "Chef d'escorte", ProfessionCategory.Transport, ReputationAlignment.Respected, false, [TechniqueDomain.Escorting, TechniqueDomain.Leadership, TechniqueDomain.Strategy], ["escort", "leadership", "negotiation"], "Dirige convois et négocie les passages dangereux.");
        Add("courier", "Messager", ProfessionCategory.Transport, ReputationAlignment.Neutral, false, [TechniqueDomain.Qinggong, TechniqueDomain.Movement], ["running", "navigation", "memory"], "Transporte lettres et petits paquets.");
        Add("caravan_master", "Maître de caravane", ProfessionCategory.Transport, ReputationAlignment.Respected, false, [TechniqueDomain.Commerce, TechniqueDomain.Leadership], ["commerce", "logistics", "route"], "Organise convois marchands.");
        Add("boatman", "Batelier", ProfessionCategory.Transport, ReputationAlignment.Neutral, false, [TechniqueDomain.Fishing, TechniqueDomain.Movement], ["navigation", "river"], "Transporte personnes et biens sur l'eau.");
        Add("bodyguard", "Garde du corps", ProfessionCategory.Murim, ReputationAlignment.Neutral, false, [TechniqueDomain.Escorting, TechniqueDomain.BodyTempering], ["combat", "protection"], "Protège une personne précise.");
        Add("bounty_hunter", "Chasseur de primes", ProfessionCategory.Murim, ReputationAlignment.Neutral, false, [TechniqueDomain.Tracking, TechniqueDomain.Hunting, TechniqueDomain.Grappling], ["tracking", "combat", "law"], "Poursuit des cibles contre récompense.");
        Add("constable", "Constable", ProfessionCategory.Imperial, ReputationAlignment.Respected, false, [TechniqueDomain.Grappling, TechniqueDomain.Sword], ["law", "investigation", "combat"], "Maintient l'ordre pour une autorité locale.");
        Add("soldier", "Soldat", ProfessionCategory.Imperial, ReputationAlignment.Neutral, false, [TechniqueDomain.Spear, TechniqueDomain.BodyTempering], ["combat", "discipline"], "Sert dans l'armée régulière.");
        Add("officer", "Officier", ProfessionCategory.Imperial, ReputationAlignment.Respected, false, [TechniqueDomain.Leadership, TechniqueDomain.Strategy], ["leadership", "strategy", "law"], "Commande soldats et garnisons.");
        Add("strategist", "Stratège", ProfessionCategory.Imperial, ReputationAlignment.Respected, false, [TechniqueDomain.Strategy, TechniqueDomain.Scholarship], ["strategy", "logistics", "history"], "Planifie campagnes, défenses et intrigues.");
        Add("investigator", "Enquêteur", ProfessionCategory.Imperial, ReputationAlignment.Respected, false, [TechniqueDomain.Tracking, TechniqueDomain.Scholarship], ["investigation", "law", "observation"], "Enquête sur crimes, disparitions et fraudes.");
        Add("information_broker", "Courtier d'informations", ProfessionCategory.Murim, ReputationAlignment.Neutral, false, [TechniqueDomain.Espionage, TechniqueDomain.Negotiation], ["information", "contacts", "gossip"], "Achète, vérifie et revend des informations.");
        Add("spy", "Espion", ProfessionCategory.Underworld, ReputationAlignment.Shunned, true, [TechniqueDomain.Espionage, TechniqueDomain.Movement], ["espionage", "disguise", "memory"], "Infiltre organisations et transmet des secrets.");
        Add("thief", "Voleur", ProfessionCategory.Underworld, ReputationAlignment.Criminal, true, [TechniqueDomain.Thievery, TechniqueDomain.Qinggong], ["thievery", "stealth"], "Dérobe biens et informations.");
        Add("smuggler", "Contrebandier", ProfessionCategory.Underworld, ReputationAlignment.Criminal, true, [TechniqueDomain.Commerce, TechniqueDomain.Espionage], ["smuggling", "route", "contacts"], "Contourne taxes, frontières et interdictions.");
        Add("fence", "Receleur", ProfessionCategory.Underworld, ReputationAlignment.Criminal, true, [TechniqueDomain.Commerce, TechniqueDomain.Negotiation], ["appraisal", "underworld", "commerce"], "Écoule marchandises volées.");
        Add("bandit", "Bandit", ProfessionCategory.Underworld, ReputationAlignment.Criminal, true, [TechniqueDomain.Saber, TechniqueDomain.BodyTempering], ["combat", "intimidation", "survival"], "Vit de pillage, extorsion ou contrôle des routes.");
        Add("river_pirate", "Pirate fluvial", ProfessionCategory.Underworld, ReputationAlignment.Criminal, true, [TechniqueDomain.Saber, TechniqueDomain.Fishing], ["combat", "river", "navigation"], "Attaque transports et contrôle certains passages d'eau.");
        Add("racketeer", "Racketteur", ProfessionCategory.Underworld, ReputationAlignment.Criminal, true, [TechniqueDomain.Negotiation, TechniqueDomain.Fist], ["intimidation", "contacts"], "Impose une protection par menace.");
        Add("assassin", "Assassin", ProfessionCategory.Underworld, ReputationAlignment.Feared, true, [TechniqueDomain.Assassination, TechniqueDomain.HiddenWeapon, TechniqueDomain.Qinggong], ["assassination", "stealth", "tracking"], "Tue sur contrat dans le cadre fictif du jeu.");
        Add("poisoner", "Empoisonneur", ProfessionCategory.Underworld, ReputationAlignment.Feared, true, [TechniqueDomain.Poison, TechniqueDomain.Herbalism], ["poison_lore", "stealth"], "Spécialiste de toxines entièrement fictives et de contre-mesures de gameplay.");
        Add("grave_robber", "Pilleur de tombes", ProfessionCategory.Underworld, ReputationAlignment.Shunned, true, [TechniqueDomain.Mining, TechniqueDomain.Tracking], ["excavation", "traps", "appraisal"], "Explore sépultures et ruines interdites.");
        Add("mercenary", "Mercenaire", ProfessionCategory.Murim, ReputationAlignment.Neutral, false, [TechniqueDomain.Sword, TechniqueDomain.Spear, TechniqueDomain.Escorting], ["combat", "survival"], "Vend ses compétences à différents employeurs.");
        Add("monk", "Moine", ProfessionCategory.Spiritual, ReputationAlignment.Respected, false, [TechniqueDomain.Staff, TechniqueDomain.Palm, TechniqueDomain.InternalCultivation], ["meditation", "discipline", "combat"], "Suit une communauté religieuse ou ascétique.");
        Add("taoist", "Pratiquant taoïste", ProfessionCategory.Spiritual, ReputationAlignment.Respected, false, [TechniqueDomain.InternalCultivation, TechniqueDomain.Sword, TechniqueDomain.Scholarship], ["meditation", "ritual", "combat"], "Combine étude, discipline et pratique martiale selon sa tradition.");
        Add("wandering_healer", "Soigneur itinérant", ProfessionCategory.Medicine, ReputationAlignment.Respected, false, [TechniqueDomain.Medicine, TechniqueDomain.Healing, TechniqueDomain.Herbalism], ["medicine", "travel", "herbalism"], "Voyage de village en village pour soigner.");
        Add("street_performer", "Artiste de rue", ProfessionCategory.Entertainment, ReputationAlignment.Neutral, false, [TechniqueDomain.Movement, TechniqueDomain.Music], ["performance", "acrobatics"], "Gagne sa vie par démonstrations, musique et acrobaties.");
        Add("fortune_teller", "Devin", ProfessionCategory.Service, ReputationAlignment.Neutral, false, [TechniqueDomain.Scholarship, TechniqueDomain.Negotiation], ["observation", "performance", "symbols"], "Interprète signes et présages, sincèrement ou non.");
        Add("beggar", "Mendiant", ProfessionCategory.Service, ReputationAlignment.Neutral, false, [TechniqueDomain.Movement, TechniqueDomain.Espionage], ["survival", "gossip", "contacts"], "Survit dans les rues et peut appartenir à des réseaux d'information.");

        return p;
    }
}

public sealed class ProfessionSystem
{
    public double Train(Npc npc, string skill, double hours, double aptitude = 1.0)
    {
        hours = Math.Clamp(hours, 0, 24 * 30);
        aptitude = Math.Clamp(aptitude, 0.25, 2.0);
        var current = npc.Skills.GetValueOrDefault(skill);
        var diminishing = Math.Max(0.08, 1 - current / 115.0);
        var gain = hours * 0.06 * aptitude * diminishing;
        npc.Skills[skill] = Math.Clamp(current + gain, 0, 100);
        return gain;
    }

    public CareerRank RankFor(Npc npc, ProfessionDefinition profession)
    {
        var score = profession.CoreSkills.Count == 0 ? 0 : profession.CoreSkills.Average(s => npc.Skills.GetValueOrDefault(s));
        return score switch
        {
            < 5 => CareerRank.Novice,
            < 15 => CareerRank.Apprentice,
            < 30 => CareerRank.Worker,
            < 50 => CareerRank.Skilled,
            < 70 => CareerRank.Expert,
            < 90 => CareerRank.Master,
            _ => CareerRank.Grandmaster
        };
    }

    public ProfessionDefinition? BestFit(Npc npc) => ProfessionCatalog.All
        .OrderByDescending(p => p.CoreSkills.Count == 0 ? 0 : p.CoreSkills.Average(s => npc.Skills.GetValueOrDefault(s)))
        .FirstOrDefault();
}
