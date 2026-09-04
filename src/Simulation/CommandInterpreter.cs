using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Murim.World;

namespace Murim.Simulation;

public sealed class CommandInterpreter
{
    public CommandParseResult Parse(WorldState world, string input)
    {
        ArgumentNullException.ThrowIfNull(world);
        input ??= string.Empty;
        var raw = input.Trim();
        var text = Normalize(raw);
        if (text.Length == 0) return Fail(raw, "Commande vide.");

        if (Matches(text, "observe", "regarde", "regarder", "examine", "examiner"))
            return new() { Success = true, Intent = PlayerCommandIntent.Observe, RawInput = raw, TargetText = ExtractAfter(text, "observe", "regarde", "regarder", "examine", "examiner"), Feedback = "Observation." };

        if (Matches(text, "inspecte ton profil", "consulte ton profil", "regarde ton profil", "mon profil", "profil"))
            return new() { Success = true, Intent = PlayerCommandIntent.InspectSelf, RawInput = raw, Feedback = "Consultation du profil." };

        if (Matches(text, "dors", "dormir", "dors ici", "repose toi", "reposes toi", "va dormir"))
            return new() { Success = true, Intent = PlayerCommandIntent.Sleep, RawInput = raw, Feedback = "Repos." };

        var travel = ExtractAfter(text, "va a", "vas a", "aller a", "rends toi a", "rends toi au", "rends toi a la", "dirige toi vers", "pars pour", "voyage vers");
        if (travel is not null)
        {
            var method = text.Contains("a cheval", StringComparison.Ordinal) ? MovementMethod.Horse :
                text.Contains("en charrette", StringComparison.Ordinal) ? MovementMethod.Cart :
                text.Contains("en bateau", StringComparison.Ordinal) ? MovementMethod.Boat :
                text.Contains("mouvement martial", StringComparison.Ordinal) ? MovementMethod.MartialMovement : MovementMethod.Walk;
            travel = CleanTarget(travel);
            var location = ResolveLocation(world, travel);
            return location is null
                ? Fail(raw, $"Je ne connais pas le lieu « {travel} », ou ton personnage ne le connaît pas.", PlayerCommandIntent.Travel, travel)
                : new() { Success = true, Intent = PlayerCommandIntent.Travel, RawInput = raw, TargetText = travel, TargetLocationId = location.Id, MovementMethod = method, Feedback = $"Destination : {location.Name}." };
        }

        var talk = ExtractAfter(text, "parle a", "parle au", "parle avec", "parler a", "parler avec", "discute avec", "discute a");
        if (talk is not null)
        {
            talk = CleanTarget(talk);
            var npc = ResolveNpc(world, talk);
            return npc is null
                ? Fail(raw, $"Je ne trouve pas de personne correspondante : « {talk} ».", PlayerCommandIntent.Talk, talk)
                : new() { Success = true, Intent = PlayerCommandIntent.Talk, RawInput = raw, TargetText = talk, TargetNpcId = npc.Id, Feedback = $"Conversation avec {npc.Identity.DisplayName}." };
        }

        var train = ExtractAfter(text, "entraine toi", "entraine toi a", "entraine toi avec", "pratique", "entraînement", "entrainement");
        if (train is not null)
        {
            train = CleanTarget(train);
            var technique = world.Martial.Techniques.Values.FirstOrDefault(t => Normalize(t.Name) == train || Normalize(t.Name).Contains(train, StringComparison.Ordinal));
            return technique is null
                ? Fail(raw, $"Technique inconnue : « {train} ».", PlayerCommandIntent.Train, train)
                : new() { Success = true, Intent = PlayerCommandIntent.Train, RawInput = raw, TargetText = train, TargetTechniqueId = technique.Id, Feedback = $"Entraînement : {technique.Name}." };
        }

        var eat = ExtractAfter(text, "mange", "manger", "consomme");
        if (eat is not null)
        {
            eat = CleanTarget(eat);
            var item = ResolveItem(world, eat, ItemCategory.Food);
            return item is null ? Fail(raw, $"Aucun aliment correspondant à « {eat} » dans ton inventaire.", PlayerCommandIntent.Eat, eat) : new() { Success = true, Intent = PlayerCommandIntent.Eat, RawInput = raw, TargetText = eat, TargetItemId = item.Id, Feedback = $"Consommer : {item.Name}." };
        }

        var drink = ExtractAfter(text, "bois", "boire");
        if (drink is not null)
        {
            drink = CleanTarget(drink);
            var item = ResolveItem(world, drink, null);
            return item is null ? Fail(raw, $"Aucune boisson correspondante à « {drink} » dans ton inventaire.", PlayerCommandIntent.Drink, drink) : new() { Success = true, Intent = PlayerCommandIntent.Drink, RawInput = raw, TargetText = drink, TargetItemId = item.Id, Feedback = $"Boire : {item.Name}." };
        }

        return Fail(raw, "Commande non comprise. Essaie : « Va au Bourg de la Rivière », « Observe », « Parle à… », « Entraîne-toi », « Mange… », « Bois… » ou « Dors ». ");
    }

    private static Npc? ResolveNpc(WorldState world, string target)
    {
        var player = world.PlayerNpc;
        return world.Npcs.Values.Where(n => n.IsAlive && n.Id != player?.Id && (player?.CurrentLocationId is null || n.CurrentLocationId == player.CurrentLocationId))
            .FirstOrDefault(n => Normalize(n.Identity.DisplayName) == target || Normalize(n.Identity.GivenName) == target || Normalize(n.Identity.DisplayName).Contains(target, StringComparison.Ordinal));
    }

    private static Location? ResolveLocation(WorldState world, string target)
    {
        var player = world.PlayerNpc;
        return world.Geography.Locations.Values.Where(l => player?.KnownLocationIds.Contains(l.Id) == true)
            .FirstOrDefault(l => Normalize(l.Name) == target || Normalize(l.Name).Contains(target, StringComparison.Ordinal));
    }

    private static ItemDefinition? ResolveItem(WorldState world, string target, ItemCategory? category)
    {
        var player = world.PlayerNpc;
        if (player is null) return null;
        return player.Inventory.Entries.Select(e => world.Inventory.Items.TryGetValue(e.ItemId, out var item) ? item : null).Where(i => i is not null && (!category.HasValue || i.Category == category.Value))
            .FirstOrDefault(i => Normalize(i!.Name) == target || Normalize(i.Name).Contains(target, StringComparison.Ordinal));
    }

    private static bool Matches(string text, params string[] phrases) => phrases.Any(p => text == p || text.StartsWith(p + " ", StringComparison.Ordinal));

    private static string? ExtractAfter(string text, params string[] prefixes)
    {
        foreach (var prefix in prefixes)
            if (text == prefix) return string.Empty;
            else if (text.StartsWith(prefix + " ", StringComparison.Ordinal)) return text[(prefix.Length + 1)..].Trim();
        return null;
    }

    private static string CleanTarget(string target) => target.Trim().Trim('.', '!', '?', ',');

    private static CommandParseResult Fail(string raw, string message, PlayerCommandIntent intent = PlayerCommandIntent.Unknown, string target = "") =>
        new() { Success = false, Intent = intent, RawInput = raw, TargetText = target, Feedback = message };

    public static string Normalize(string value)
    {
        var form = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(form.Length);
        foreach (var c in form)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(c);
        var normalized = builder.ToString().Normalize(NormalizationForm.FormC);
        normalized = Regex.Replace(normalized, @"[^\p{L}\p{Nd}\s]", " ");
        return Regex.Replace(normalized, @"\s+", " ").Trim();
    }
}
