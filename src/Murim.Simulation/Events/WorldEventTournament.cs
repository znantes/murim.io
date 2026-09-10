namespace Murim.Simulation;

public enum WorldEventType
{
    Birth, Death, Marriage, Divorce, Injury, Illness, Recovery, JobChange, Apprenticeship, Promotion,
    Duel, Feud, Raid, BanditAttack, EscortSuccess, EscortFailure, TradeBoom, Shortage, Fire, Flood, Drought,
    Famine, Epidemic, Migration, SectSplit, SectAlliance, Succession, Coup, ImperialEdict, TaxChange, Arrest,
    PrisonEscape, Discovery, ArtifactFound, ManualFound, MonsterSighting, MonsterAttack, TournamentAnnouncement,
    TournamentResult, Festival, Funeral, Wedding, MarketFair, CaravanArrival, MissingPerson, Scandal, RumorWave
}

public sealed record WorldEvent(
    Guid Id,
    WorldEventType Type,
    long Day,
    Guid? LocationId,
    IReadOnlyList<Guid> ActorNpcIds,
    IReadOnlyList<Guid> FactionIds,
    string Summary,
    double Publicity,
    double Importance,
    double Ambiguity,
    IReadOnlyList<Guid> DirectWitnessNpcIds);

public enum TournamentFormat { FriendlyExchange, Bracket, RoundRobin, ChallengeSeats, TeamBattle, Trials, MixedTests, CraftJudging }
public enum TournamentScope { Household, Sect, Local, Regional, Murim, Imperial, Underworld }

public sealed record TournamentArchetype(
    string Code,
    string Name,
    TournamentFormat Format,
    TournamentScope Scope,
    int MinimumAge,
    MartialRealm MinimumRealm,
    MartialRealm MaximumRealm,
    bool LethalAllowed,
    int TypicalParticipants,
    IReadOnlyList<TechniqueDomain> Disciplines,
    IReadOnlyList<string> Rewards,
    string Purpose);

public static class TournamentCatalog
{
    public static IReadOnlyList<TournamentArchetype> All { get; } =
    [
        new("junior_exchange", "Rencontre des Jeunes Disciples", TournamentFormat.FriendlyExchange, TournamentScope.Sect, 12, MartialRealm.Untrained, MartialRealm.FirstRate, false, 32,
            [TechniqueDomain.Sword, TechniqueDomain.Fist, TechniqueDomain.Palm, TechniqueDomain.Movement], ["prestige de génération", "amitiés et rivalités", "droit d'observer des cours avancés"], "Comparer les progrès de deux écoles sans guerre ouverte."),
        new("sect_exam", "Examen Interne de Secte", TournamentFormat.Trials, TournamentScope.Sect, 10, MartialRealm.Untrained, MartialRealm.Peak, false, 80,
            [TechniqueDomain.BodyTempering, TechniqueDomain.InternalCultivation, TechniqueDomain.Movement], ["promotion interne", "accès à un maître", "allocation de ressources"], "Classer les disciples et attribuer ressources et responsabilités."),
        new("nine_arts", "Assemblée des Neuf Arts", TournamentFormat.ChallengeSeats, TournamentScope.Murim, 16, MartialRealm.SecondRate, MartialRealm.Transformation, false, 128,
            [TechniqueDomain.Fist, TechniqueDomain.Leg, TechniqueDomain.Staff, TechniqueDomain.Sword, TechniqueDomain.Saber, TechniqueDomain.Spear, TechniqueDomain.Qinggong, TechniqueDomain.HiddenWeapon, TechniqueDomain.InternalCultivation], ["sièges symboliques", "renommée", "invitations de factions"], "Championnat original inspiré du trope des compétitions multi-disciplines."),
        new("wulin_conference", "Grande Conférence du Murim", TournamentFormat.Bracket, TournamentScope.Murim, 16, MartialRealm.FirstRate, MartialRealm.Profound, false, 96,
            [TechniqueDomain.Sword, TechniqueDomain.Saber, TechniqueDomain.Palm, TechniqueDomain.Fist], ["prestige", "poids politique", "candidature au conseil"], "Rassemblement martial qui sert autant à négocier qu'à combattre."),
        new("alliance_leader", "Épreuve du Chef d'Alliance", TournamentFormat.MixedTests, TournamentScope.Murim, 25, MartialRealm.Peak, MartialRealm.LifeDeath, false, 24,
            [TechniqueDomain.Strategy, TechniqueDomain.Leadership, TechniqueDomain.Sword, TechniqueDomain.Fist], ["mandat politique temporaire", "autorité de coalition"], "Sélection rare d'un chef de coalition ; la victoire martiale seule ne suffit pas."),
        new("succession_trial", "Épreuve de Succession", TournamentFormat.MixedTests, TournamentScope.Household, 16, MartialRealm.SecondRate, MartialRealm.Profound, false, 12,
            [TechniqueDomain.Leadership, TechniqueDomain.Strategy, TechniqueDomain.Sword, TechniqueDomain.InternalCultivation], ["position d'héritier", "soutien d'anciens"], "Départager des héritiers selon combat, gestion et alliances."),
        new("open_arena", "Arène de la Préfecture", TournamentFormat.Bracket, TournamentScope.Local, 16, MartialRealm.Untrained, MartialRealm.TranscendentPeak, false, 64,
            [TechniqueDomain.Fist, TechniqueDomain.Sword, TechniqueDomain.Saber, TechniqueDomain.Spear], ["argent", "contrats", "réputation locale"], "Compétition ouverte attirant gardes, mercenaires et disciples errants."),
        new("escort_trial", "Épreuve des Agences d'Escorte", TournamentFormat.TeamBattle, TournamentScope.Regional, 16, MartialRealm.ThirdRate, MartialRealm.Peak, false, 40,
            [TechniqueDomain.Escorting, TechniqueDomain.Tracking, TechniqueDomain.Strategy], ["contrats marchands", "réputation commerciale"], "Comparer protection, navigation, discipline et résolution de crises simulées."),
        new("imperial_exam", "Épreuves Militaires Impériales", TournamentFormat.MixedTests, TournamentScope.Imperial, 18, MartialRealm.Untrained, MartialRealm.TranscendentPeak, false, 120,
            [TechniqueDomain.Archery, TechniqueDomain.Spear, TechniqueDomain.Strategy, TechniqueDomain.Leadership], ["grade militaire", "emploi impérial"], "Recrutement public mêlant compétences martiales, stratégie et discipline."),
        new("alchemy_meet", "Assemblée des Fourneaux", TournamentFormat.CraftJudging, TournamentScope.Regional, 14, MartialRealm.Untrained, MartialRealm.LifeDeath, false, 48,
            [TechniqueDomain.Alchemy, TechniqueDomain.Herbalism], ["contrats", "ingrédients rares", "réputation artisanale"], "Concours fictif d'alchimie évalué sur pureté de jeu, constance et coût."),
        new("smith_fair", "Foire des Cent Marteaux", TournamentFormat.CraftJudging, TournamentScope.Regional, 14, MartialRealm.Untrained, MartialRealm.LifeDeath, false, 56,
            [TechniqueDomain.Forging, TechniqueDomain.WeaponSmithing, TechniqueDomain.ArmorSmithing], ["commandes prestigieuses", "minerais", "apprentis"], "Foire où artisans vendent, échangent et font juger leur travail."),
        new("cooking_festival", "Festival des Cent Saveurs", TournamentFormat.CraftJudging, TournamentScope.Local, 12, MartialRealm.Untrained, MartialRealm.LifeDeath, false, 36,
            [TechniqueDomain.Cooking, TechniqueDomain.Brewing], ["clientèle", "contrat d'auberge", "recettes de cuisine fictives"], "Concours civil prouvant que le monde ne tourne pas uniquement autour du combat."),
        new("underworld_challenge", "Défi des Lanternes Noires", TournamentFormat.Bracket, TournamentScope.Underworld, 18, MartialRealm.FirstRate, MartialRealm.Profound, true, 32,
            [TechniqueDomain.Saber, TechniqueDomain.HiddenWeapon, TechniqueDomain.Qinggong], ["argent", "contrats clandestins", "peur"], "Événement illégal et dangereux dont l'existence elle-même circule comme rumeur.")
    ];
}

public sealed class WorldEventSystem
{
    private readonly Random random;
    public WorldEventSystem(int seed) => random = new Random(seed);

    public IReadOnlyList<WorldEvent> GenerateDaily(WorldState world)
    {
        if (world.Locations.Count == 0) return [];
        var eventCount = Math.Clamp(1 + world.Npcs.Count / 2500 + random.Next(0, 3), 1, 12);
        var created = new List<WorldEvent>(eventCount);
        var living = world.Npcs.Values.Where(n => n.IsAlive).ToArray();

        for (var i = 0; i < eventCount; i++)
        {
            var location = world.Locations.Values.ElementAt(random.Next(world.Locations.Count));
            var local = living.Where(n => n.CurrentLocationId == location.Id).ToArray();
            var actors = local.OrderBy(_ => random.Next()).Take(random.Next(1, Math.Min(4, local.Length + 1))).Select(n => n.Id).ToArray();
            var type = PickType(location);
            var importance = Math.Clamp(0.1 + random.NextDouble() * 0.75 + (type is WorldEventType.SectSplit or WorldEventType.Coup or WorldEventType.Famine ? 0.15 : 0), 0, 1);
            var publicity = Math.Clamp(random.NextDouble() * 0.8 + (type is WorldEventType.Festival or WorldEventType.MarketFair or WorldEventType.TournamentAnnouncement ? 0.2 : 0), 0, 1);
            var ambiguity = Math.Clamp(0.15 + random.NextDouble() * 0.7 - publicity * 0.25, 0, 1);
            var witnesses = local.OrderBy(_ => random.Next()).Take(Math.Min(local.Length, random.Next(0, 8))).Select(n => n.Id).ToArray();
            var ev = new WorldEvent(Guid.NewGuid(), type, world.Clock.Day, location.Id, actors, [], Describe(type, location.Name), publicity, importance, ambiguity, witnesses);
            world.Events.Add(ev);
            created.Add(ev);
        }
        return created;
    }

    public TournamentArchetype? MaybeScheduleTournament(WorldState world)
    {
        if (random.NextDouble() > 0.018) return null;
        return TournamentCatalog.All[random.Next(TournamentCatalog.All.Count)];
    }

    private WorldEventType PickType(WorldLocation location)
    {
        var roll = random.Next(100);
        if (location.Type.Contains("Ville", StringComparison.OrdinalIgnoreCase) || location.Type.Contains("Cité", StringComparison.OrdinalIgnoreCase))
            return roll switch { < 15 => WorldEventType.MarketFair, < 25 => WorldEventType.CaravanArrival, < 35 => WorldEventType.Arrest, < 43 => WorldEventType.Scandal, < 50 => WorldEventType.TradeBoom, < 56 => WorldEventType.Fire, < 63 => WorldEventType.Wedding, < 70 => WorldEventType.JobChange, < 78 => WorldEventType.TournamentAnnouncement, < 88 => WorldEventType.MissingPerson, _ => WorldEventType.RumorWave };
        if (location.Safety < 0.4)
            return roll switch { < 22 => WorldEventType.BanditAttack, < 38 => WorldEventType.MonsterSighting, < 47 => WorldEventType.MonsterAttack, < 58 => WorldEventType.MissingPerson, < 68 => WorldEventType.Discovery, < 78 => WorldEventType.EscortFailure, < 88 => WorldEventType.Duel, _ => WorldEventType.CaravanArrival };
        return roll switch { < 14 => WorldEventType.Birth, < 24 => WorldEventType.Marriage, < 33 => WorldEventType.Death, < 44 => WorldEventType.JobChange, < 52 => WorldEventType.Apprenticeship, < 62 => WorldEventType.CaravanArrival, < 70 => WorldEventType.Festival, < 78 => WorldEventType.Duel, < 87 => WorldEventType.Discovery, _ => WorldEventType.RumorWave };
    }

    private static string Describe(WorldEventType type, string place) => type switch
    {
        WorldEventType.Birth => $"Une naissance a lieu à {place}.",
        WorldEventType.Death => $"Un habitant de {place} meurt ; seuls ses proches le savent immédiatement.",
        WorldEventType.Marriage => $"Deux foyers de {place} célèbrent une union.",
        WorldEventType.BanditAttack => $"Une attaque est signalée sur une route près de {place}.",
        WorldEventType.MonsterSighting => $"Des traces inhabituelles sont rapportées près de {place}.",
        WorldEventType.MonsterAttack => $"Une bête dangereuse attaque des voyageurs près de {place}.",
        WorldEventType.CaravanArrival => $"Une caravane arrive à {place} et modifie temporairement prix et rumeurs.",
        WorldEventType.MarketFair => $"Un marché important anime {place}.",
        WorldEventType.TournamentAnnouncement => $"Une compétition est annoncée à {place}.",
        WorldEventType.Duel => $"Un duel entre pratiquants a lieu à {place}.",
        WorldEventType.Scandal => $"Une affaire embarrassante commence à circuler à {place}.",
        WorldEventType.RumorWave => $"Plusieurs versions d'une même histoire circulent à {place}.",
        WorldEventType.Discovery => $"Quelqu'un affirme avoir découvert quelque chose près de {place}.",
        _ => $"Un événement de type {type} se produit à {place}."
    };
}
