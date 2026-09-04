using Murim.World;

namespace Murim.Simulation;

public sealed class ActionSystem
{
    public ActionResult Execute(WorldState world, CommandParseResult command)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(command);
        var player = world.PlayerNpc;
        if (player is null) return Fail(command.Intent, "Aucun personnage joueur n'est initialisé.");
        if (!player.IsAlive) return Fail(command.Intent, "Ton personnage est mort. La partie continue, mais il ne peut plus agir.");
        if (!command.Success) return Fail(command.Intent, command.Feedback);

        try
        {
            return command.Intent switch
            {
                PlayerCommandIntent.Travel => Travel(world, player, command),
                PlayerCommandIntent.Observe => Observe(world, player, command),
                PlayerCommandIntent.Talk => Talk(world, player, command),
                PlayerCommandIntent.Train => Train(world, player, command),
                PlayerCommandIntent.Eat => Consume(world, player, command, false),
                PlayerCommandIntent.Drink => Consume(world, player, command, true),
                PlayerCommandIntent.Sleep => Sleep(world, player),
                PlayerCommandIntent.InspectSelf => Inspect(world, player),
                _ => Fail(command.Intent, "Cette commande n'est pas encore disponible.")
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return Fail(command.Intent, ex.Message);
        }
    }

    private static ActionResult Travel(WorldState world, Npc player, CommandParseResult command)
    {
        if (command.TargetLocationId is not Guid destination) return Fail(command.Intent, "Destination manquante.");
        var plan = world.Travel.Plan(world, player, destination, command.MovementMethod);
        if (plan is null) return Fail(command.Intent, "Impossible de préparer ce voyage depuis ta position actuelle.");
        if (!world.Travel.Execute(world, plan)) return Fail(command.Intent, "Le voyage n'a pas pu être effectué.");
        return Ok(command.Intent, plan.DurationMinutes, $"Tu arrives à {world.Geography.Locations[destination].Name} après environ {plan.DurationMinutes} minutes de voyage.");
    }

    private static ActionResult Observe(WorldState world, Npc player, CommandParseResult command)
    {
        const int minutes = 10;
        world.AdvanceMinutes(minutes);
        var locationName = player.CurrentLocationId is Guid id && world.Geography.Locations.TryGetValue(id, out var location) ? location.Name : "un lieu inconnu";
        player.History.Add("Observation", player.AgeYears, $"Observe les alentours à {locationName}.");
        return Ok(command.Intent, minutes, $"Tu observes attentivement les alentours de {locationName}. Le monde continue d'évoluer pendant ce temps.");
    }

    private static ActionResult Talk(WorldState world, Npc player, CommandParseResult command)
    {
        if (command.TargetNpcId is not Guid targetId || !world.Npcs.TryGetValue(targetId, out var other) || !other.IsAlive)
            return Fail(command.Intent, "La personne n'est plus disponible.");
        if (player.CurrentLocationId != other.CurrentLocationId) return Fail(command.Intent, "Cette personne n'est pas ici.");
        const int minutes = 15;
        world.AdvanceMinutes(minutes);
        var relation = player.Relationships.FirstOrDefault(r => r.ToNpcId == other.Id);
        relation?.Shift(0.02, 0.01, 0.01);
        player.History.Add("Conversation", player.AgeYears, $"Parle avec {other.Identity.DisplayName}.");
        other.History.Add("Conversation", other.AgeYears, $"Échange avec {player.Identity.DisplayName}.");
        return Ok(command.Intent, minutes, $"Tu échanges avec {other.Identity.DisplayName}. Les relations peuvent évoluer selon cette interaction.");
    }

    private static ActionResult Train(WorldState world, Npc player, CommandParseResult command)
    {
        if (command.TargetTechniqueId is not Guid techniqueId) return Fail(command.Intent, "Technique manquante.");
        var result = world.Martial.Train(world, player, techniqueId, 30);
        return Ok(command.Intent, result.MinutesSpent, result.Outcome);
    }

    private static ActionResult Consume(WorldState world, Npc player, CommandParseResult command, bool drink)
    {
        if (command.TargetItemId is not Guid itemId || !world.Inventory.Items.TryGetValue(itemId, out var item)) return Fail(command.Intent, "Objet introuvable.");
        var entry = player.Inventory.Entries.FirstOrDefault(e => e.ItemId == itemId && e.Quantity > 0);
        if (entry is null) return Fail(command.Intent, "Cet objet n'est plus dans ton inventaire.");
        if (!item.Consumable) return Fail(command.Intent, $"{item.Name} n'est pas consommable.");
        if (drink && item.Category != ItemCategory.Food && item.Category != ItemCategory.Miscellaneous)
            return Fail(command.Intent, $"{item.Name} ne peut pas être utilisé comme boisson.");
        if (!drink && item.Category != ItemCategory.Food) return Fail(command.Intent, $"{item.Name} n'est pas un aliment.");

        player.Inventory.Remove(itemId);
        const int minutes = 5;
        world.AdvanceMinutes(minutes);
        if (drink) player.Needs.Drink(35); else player.Needs.Eat(35);
        player.History.Add(drink ? "Boisson" : "Repas", player.AgeYears, $"Consomme {item.Name}.");
        return Ok(command.Intent, minutes, drink ? $"Tu bois {item.Name}." : $"Tu manges {item.Name}.");
    }

    private static ActionResult Sleep(WorldState world, Npc player)
    {
        const int minutes = 480;
        world.AdvanceMinutes(minutes);
        player.Needs.SleepFor(100);
        player.History.Add("Sommeil", player.AgeYears, "Dort et récupère pendant plusieurs heures.");
        return Ok(PlayerCommandIntent.Sleep, minutes, "Tu dors. Le monde continue de vivre et d'évoluer pendant ton sommeil.");
    }

    private static ActionResult Inspect(WorldState world, Npc player)
    {
        const int minutes = 5;
        world.AdvanceMinutes(minutes);
        var location = player.CurrentLocationId is Guid id && world.Geography.Locations.TryGetValue(id, out var l) ? l.Name : "inconnu";
        return Ok(PlayerCommandIntent.InspectSelf, minutes, $"Profil : {player.Identity.DisplayName}, âge {player.AgeYears} ans, lieu {location}, faim {player.Needs.Hunger:0.#}, soif {player.Needs.Thirst:0.#}, fatigue {player.Needs.Fatigue:0.#}, douleur {player.CurrentPain:0.#}.");
    }

    private static ActionResult Ok(PlayerCommandIntent intent, int minutes, string feedback) => new() { Success = true, Intent = intent, MinutesSpent = minutes, Feedback = feedback };
    private static ActionResult Fail(PlayerCommandIntent intent, string feedback) => new() { Success = false, Intent = intent, MinutesSpent = 0, Feedback = feedback };
}
