using Murim.World;

namespace Murim.Simulation;

public sealed class ActionSystem
{
    private readonly ObservationSystem _observations = new();

    public ActionResult Execute(WorldState world, CommandParseResult command)
    {
        ArgumentNullException.ThrowIfNull(world); ArgumentNullException.ThrowIfNull(command);
        var player = world.PlayerNpc;
        if (player is null) return Fail(command.Intent, "Aucun personnage joueur n'est initialisé.");
        if (!player.IsAlive) return Fail(command.Intent, "Ton personnage est mort. La partie continue, mais il ne peut plus agir.");
        if (!command.Success) return Fail(command.Intent, command.Feedback);
        try
        {
            return command.Intent switch
            {
                PlayerCommandIntent.Travel => Travel(world, player, command),
                PlayerCommandIntent.Observe => Observe(world, player),
                PlayerCommandIntent.Talk => Talk(world, player, command),
                PlayerCommandIntent.AskDirections => Social(world, player, command, "Demande son chemin", "Vous échangez des indications.", 8, 0.01),
                PlayerCommandIntent.Follow => Social(world, player, command, "Suit", "Tu suis cette personne pendant un moment.", 20, 0.005),
                PlayerCommandIntent.Help => Social(world, player, command, "Aide", "Tu apportes ton aide. La personne peut s'en souvenir.", 20, 0.03),
                PlayerCommandIntent.Refuse => Social(world, player, command, "Refus", "Tu refuses d'aider. Cette décision peut affecter la relation.", 5, -0.03),
                PlayerCommandIntent.Train => Train(world, player, command),
                PlayerCommandIntent.Eat => Consume(world, player, command, false),
                PlayerCommandIntent.Drink => Consume(world, player, command, true),
                PlayerCommandIntent.Sleep => Sleep(world, player),
                PlayerCommandIntent.InspectSelf => Inspect(world, player),
                PlayerCommandIntent.Examine => Examine(world, player),
                PlayerCommandIntent.Enter => Enter(world, player),
                PlayerCommandIntent.Work => Work(world, player),
                PlayerCommandIntent.Buy => Market(world, player, false),
                PlayerCommandIntent.Sell => Market(world, player, true),
                PlayerCommandIntent.Investigate => Investigate(world, player),
                PlayerCommandIntent.DetectDanger => DetectDanger(world, player),
                _ => Fail(command.Intent, "Cette commande n'est pas encore disponible.")
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException) { return Fail(command.Intent, ex.Message); }
    }

    private static ActionResult Travel(WorldState world, Npc player, CommandParseResult command)
    {
        if (command.TargetLocationId is not Guid destination) return Fail(command.Intent, "Destination manquante.");
        var plan = world.Travel.Plan(world, player, destination, command.MovementMethod);
        if (plan is null || !world.Travel.Execute(world, plan)) return Fail(command.Intent, "Le voyage n'a pas pu être effectué.");
        return Ok(command.Intent, plan.DurationMinutes, $"Tu arrives à {world.Geography.Locations[destination].Name} après environ {plan.DurationMinutes} minutes.");
    }
    private ActionResult Observe(WorldState world, Npc player) { const int m = 10; var o = _observations.Observe(world, player); world.AdvanceMinutes(m); player.History.Add("Observation", player.AgeYears, $"Observe les alentours à {o.LocationName}."); return Ok(PlayerCommandIntent.Observe, m, ObservationFormatter.ToText(o)); }
    private static ActionResult Talk(WorldState world, Npc player, CommandParseResult command) => Social(world, player, command, "Conversation", "Tu échanges avec cette personne.", 15, 0.02);
    private static ActionResult Social(WorldState world, Npc player, CommandParseResult command, string title, string text, int minutes, double affinity)
    {
        if (command.TargetNpcId is not Guid id || !world.Npcs.TryGetValue(id, out var other) || !other.IsAlive) return Fail(command.Intent, "La personne n'est plus disponible.");
        if (player.CurrentLocationId != other.CurrentLocationId) return Fail(command.Intent, "Cette personne n'est pas ici.");
        world.AdvanceMinutes(minutes);
        var relation = player.Relationships.FirstOrDefault(r => r.ToNpcId == other.Id);
        relation?.Shift(affinity, affinity > 0 ? 0.01 : -0.01, affinity > 0 ? 0.005 : 0);
        player.History.Add(title, player.AgeYears, $"{title} avec {other.Identity.DisplayName}.");
        other.History.Add(title, other.AgeYears, $"Interaction avec {player.Identity.DisplayName}.");
        return Ok(command.Intent, minutes, text);
    }
    private static ActionResult Train(WorldState world, Npc player, CommandParseResult command) { if (command.TargetTechniqueId is not Guid id) return Fail(command.Intent, "Technique manquante."); var r = world.Martial.Train(world, player, id, 30); return Ok(command.Intent, r.MinutesSpent, r.Outcome); }
    private static ActionResult Consume(WorldState world, Npc player, CommandParseResult command, bool drink)
    {
        if (command.TargetItemId is not Guid id || !world.Inventory.Items.TryGetValue(id, out var item)) return Fail(command.Intent, "Objet introuvable.");
        var entry = player.Inventory.Entries.FirstOrDefault(e => e.ItemId == id && e.Quantity > 0); if (entry is null || !item.Consumable) return Fail(command.Intent, "Cet objet ne peut pas être consommé.");
        if (!drink && item.Category != ItemCategory.Food) return Fail(command.Intent, "Ce n'est pas un aliment.");
        player.Inventory.Remove(id); const int m = 5; world.AdvanceMinutes(m); if (drink) player.Needs.Drink(35); else player.Needs.Eat(35); return Ok(command.Intent, m, drink ? $"Tu bois {item.Name}." : $"Tu manges {item.Name}.");
    }
    private static ActionResult Sleep(WorldState world, Npc player) { const int m = 480; world.AdvanceMinutes(m); player.Needs.SleepFor(100); player.History.Add("Sommeil", player.AgeYears, "Dort plusieurs heures."); return Ok(PlayerCommandIntent.Sleep, m, "Tu dors. Le monde continue pendant ton sommeil."); }
    private static ActionResult Inspect(WorldState world, Npc player) { const int m = 5; world.AdvanceMinutes(m); var loc = player.CurrentLocationId is Guid id && world.Geography.Locations.TryGetValue(id, out var l) ? l.Name : "inconnu"; return Ok(PlayerCommandIntent.InspectSelf, m, $"Profil : {player.Identity.DisplayName}, âge {player.AgeYears} ans, lieu {loc}, faim {player.Needs.Hunger:0.#}, soif {player.Needs.Thirst:0.#}, fatigue {player.Needs.Fatigue:0.#}, douleur {player.CurrentPain:0.#}."); }
    private static ActionResult Examine(WorldState world, Npc player) { const int m = 8; if (player.CurrentLocationId is not Guid id || !world.Geography.Locations.TryGetValue(id, out var l)) return Fail(PlayerCommandIntent.Examine, "Le lieu est inconnu."); world.AdvanceMinutes(m); return Ok(PlayerCommandIntent.Examine, m, $"Tu examines {l.Name}. Type : {l.Type}. Région : {l.Region}. Population : {l.Population}. Danger connu : {l.DangerLevel}/100."); }
    private static ActionResult Enter(WorldState world, Npc player) { const int m = 5; if (player.CurrentLocationId is not Guid id || !world.Geography.Locations.TryGetValue(id, out var l)) return Fail(PlayerCommandIntent.Enter, "Aucun bâtiment identifiable ici."); if (l.Type is not (LocationType.Sect or LocationType.Temple or LocationType.Fortress or LocationType.Estate)) return Fail(PlayerCommandIntent.Enter, "Il n'y a pas de lieu intérieur accessible ici."); world.AdvanceMinutes(m); return Ok(PlayerCommandIntent.Enter, m, $"Tu entres dans {l.Name}. L'intérieur pourra devenir un contexte distinct au fil du développement du monde."); }
    private static ActionResult Work(WorldState world, Npc player) { const int m = 60; world.AdvanceMinutes(m); var income = Math.Max(0, player.Profession.DailyIncome / 8.0); player.ApplyWealthChange(income); player.History.Add("Travail", player.AgeYears, $"Travaille pendant une heure et gagne environ {income:0.##} unités monétaires."); return Ok(PlayerCommandIntent.Work, m, $"Tu travailles pendant une heure. Gain : {income:0.##}. Wealth : {player.Wealth:0.##}."); }
    private static ActionResult Market(WorldState world, Npc player, bool sell) { const int m = 15; world.AdvanceMinutes(m); return Ok(sell ? PlayerCommandIntent.Sell : PlayerCommandIntent.Buy, m, sell ? "Tu cherches un acheteur. Aucun prix n'est inventé : le marché sera déterminé par l'offre, la demande et les commerces présents." : "Tu cherches un vendeur. Les prix dépendront du marché local, des stocks et de ta capacité à négocier."); }
    private static ActionResult Investigate(WorldState world, Npc player) { const int m = 20; if (player.CurrentLocationId is not Guid id || !world.Geography.Locations.TryGetValue(id, out var l)) return Fail(PlayerCommandIntent.Investigate, "Tu n'as pas de lieu exploitable à enquêter."); world.AdvanceMinutes(m); player.History.Add("Enquête", player.AgeYears, $"Enquête autour de {l.Name}."); return Ok(PlayerCommandIntent.Investigate, m, $"Tu enquêtes autour de {l.Name}. Les informations réellement découvrables dépendront des témoins, traces et connaissances disponibles."); }
    private static ActionResult DetectDanger(WorldState world, Npc player) { const int m = 10; if (player.CurrentLocationId is not Guid id || !world.Geography.Locations.TryGetValue(id, out var l)) return Fail(PlayerCommandIntent.DetectDanger, "Impossible d'évaluer le danger ici."); world.AdvanceMinutes(m); var e = world.Environment.Get(id); var road = world.Geography.RoadsFrom(id).MaxBy(r => r.DangerLevel); var danger = Math.Max(l.DangerLevel, road?.DangerLevel ?? 0); return Ok(PlayerCommandIntent.DetectDanger, m, $"Évaluation : danger connu {danger}/100. Météo {e.Weather}, visibilité {e.VisibilityKm:0.#} km, vent {e.WindKph:0.#} km/h."); }
    private static ActionResult Ok(PlayerCommandIntent i, int m, string f) => new() { Success = true, Intent = i, MinutesSpent = m, Feedback = f };
    private static ActionResult Fail(PlayerCommandIntent i, string f) => new() { Success = false, Intent = i, MinutesSpent = 0, Feedback = f };
}
