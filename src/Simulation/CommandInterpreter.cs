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

        if (Matches(text, "observe", "regarde", "regarder")) return Result(raw, PlayerCommandIntent.Observe);
        if (Matches(text, "inspecte ton profil", "consulte ton profil", "regarde ton profil", "mon profil", "profil")) return Result(raw, PlayerCommandIntent.InspectSelf);
        if (Matches(text, "dors", "dormir", "dors ici", "repose toi", "reposes toi", "va dormir")) return Result(raw, PlayerCommandIntent.Sleep);
        if (Matches(text, "enquete", "enquete sur les alentours", "investigue", "investigue les alentours")) return Result(raw, PlayerCommandIntent.Investigate);
        if (Matches(text, "detecte les dangers", "detecte le danger", "evalue les dangers", "évalue les dangers")) return Result(raw, PlayerCommandIntent.DetectDanger);
        if (Matches(text, "entre", "entrer")) return Result(raw, PlayerCommandIntent.Enter);
        if (Matches(text, "cherche du travail", "trouve du travail", "travaille", "travail")) return Result(raw, PlayerCommandIntent.Work);
        if (Matches(text, "cherche a acheter quelque chose", "cherche a acheter", "achete quelque chose")) return Result(raw, PlayerCommandIntent.Buy);
        if (Matches(text, "cherche a vendre quelque chose", "cherche a vendre", "vends quelque chose")) return Result(raw, PlayerCommandIntent.Sell);

        var ask = ExtractAfter(text, "demande ton chemin a", "demande son chemin a", "demande le chemin a", "demande une direction a");
        if (ask is not null) return ResolveNpcResult(world, raw, ask, PlayerCommandIntent.AskDirections);
        var follow = ExtractAfter(text, "suis", "suit");
        if (follow is not null) return ResolveNpcResult(world, raw, follow, PlayerCommandIntent.Follow);
        var help = ExtractAfter(text, "aide", "aider", "aide la personne");
        if (help is not null) return ResolveNpcResult(world, raw, help, PlayerCommandIntent.Help);
        var refuse = ExtractAfter(text, "refuse", "refuser", "refuse de l aider", "refuse de laider");
        if (refuse is not null) return ResolveNpcResult(world, raw, refuse, PlayerCommandIntent.Refuse);

        var travel = ExtractAfter(text, "rends toi au", "rends toi a la", "rends toi a", "va au", "va a", "vas au", "vas a", "aller au", "aller a", "dirige toi vers", "pars pour", "voyage vers");
        if (travel is not null)
        {
            var method = text.Contains("a cheval") ? MovementMethod.Horse : text.Contains("en charrette") ? MovementMethod.Cart : text.Contains("en bateau") ? MovementMethod.Boat : text.Contains("mouvement martial") ? MovementMethod.MartialMovement : MovementMethod.Walk;
            travel = CleanTarget(travel.Replace(" a cheval", "").Replace(" en charrette", "").Replace(" en bateau", "").Replace(" mouvement martial", ""));
            var location = ResolveLocation(world, travel);
            return location is null ? Fail(raw, $"Je ne connais pas le lieu « {travel} », ou ton personnage ne le connaît pas.", PlayerCommandIntent.Travel, travel) : new() { Success = true, Intent = PlayerCommandIntent.Travel, RawInput = raw, TargetText = travel, TargetLocationId = location.Id, MovementMethod = method, Feedback = $"Destination : {location.Name}." };
        }

        var talk = ExtractAfter(text, "parle au", "parle a", "parle avec", "parler au", "parler a", "parler avec", "discute avec", "discute a");
        if (talk is not null) return ResolveNpcResult(world, raw, talk, PlayerCommandIntent.Talk);
        var examine = ExtractAfter(text, "examine", "examiner", "regarde", "regarder");
        if (examine is not null && examine.Length > 0) return new() { Success = true, Intent = PlayerCommandIntent.Examine, RawInput = raw, TargetText = examine, Feedback = "Examen du lieu." };

        var train = ExtractAfter(text, "entraine toi avec", "entraine toi a", "entraine toi", "pratique", "entrainement");
        if (train is not null)
        {
            train = CleanTarget(train).Replace("a ", "");
            var technique = world.Martial.Techniques.Values.FirstOrDefault(t => Normalize(t.Name) == train || Normalize(t.Name).Contains(train));
            return technique is null ? Fail(raw, $"Technique inconnue : « {train} ».", PlayerCommandIntent.Train, train) : new() { Success = true, Intent = PlayerCommandIntent.Train, RawInput = raw, TargetText = train, TargetTechniqueId = technique.Id, Feedback = $"Entraînement : {technique.Name}." };
        }

        var eat = ExtractAfter(text, "mange", "manger", "consomme");
        if (eat is not null) { eat = CleanTarget(eat); var item = ResolveItem(world, eat, ItemCategory.Food); return item is null ? Fail(raw, $"Aucun aliment correspondant à « {eat} » dans ton inventaire.", PlayerCommandIntent.Eat, eat) : new() { Success = true, Intent = PlayerCommandIntent.Eat, RawInput = raw, TargetText = eat, TargetItemId = item.Id, Feedback = $"Consommer : {item.Name}." }; }
        var drink = ExtractAfter(text, "bois", "boire");
        if (drink is not null) { drink = CleanTarget(drink); var item = ResolveItem(world, drink, null); return item is null ? Fail(raw, $"Aucune boisson correspondante à « {drink} » dans ton inventaire.", PlayerCommandIntent.Drink, drink) : new() { Success = true, Intent = PlayerCommandIntent.Drink, RawInput = raw, TargetText = drink, TargetItemId = item.Id, Feedback = $"Boire : {item.Name}." }; }
        return Fail(raw, "Commande non comprise. Essaie une action naturelle : observe, parle à quelqu'un, va à un lieu, travaille, enquête ou dors.");
    }

    private static CommandParseResult ResolveNpcResult(WorldState world, string raw, string target, PlayerCommandIntent intent)
    {
        target = CleanTarget(target);
        var npc = ResolveNpc(world, target);
        return npc is null ? Fail(raw, $"Je ne trouve pas de personne correspondante : « {target} ».", intent, target) : new() { Success = true, Intent = intent, RawInput = raw, TargetText = target, TargetNpcId = npc.Id, Feedback = $"Cible : {npc.Identity.DisplayName}." };
    }

    private static CommandParseResult Result(string raw, PlayerCommandIntent intent) => new() { Success = true, Intent = intent, RawInput = raw, Feedback = intent.ToString() };
    private static Npc? ResolveNpc(WorldState world, string target) { var player = world.PlayerNpc; return world.Npcs.Values.Where(n => n.IsAlive && n.Id != player?.Id && (player?.CurrentLocationId is null || n.CurrentLocationId == player.CurrentLocationId)).FirstOrDefault(n => Normalize(n.Identity.DisplayName) == target || Normalize(n.Identity.GivenName) == target || Normalize(n.Identity.DisplayName).Contains(target)); }
    private static Location? ResolveLocation(WorldState world, string target) { var player = world.PlayerNpc; return world.Geography.Locations.Values.Where(l => player?.KnownLocationIds.Contains(l.Id) == true).FirstOrDefault(l => Normalize(l.Name) == target || Normalize(l.Name).Contains(target)); }
    private static ItemDefinition? ResolveItem(WorldState world, string target, ItemCategory? category) { var player = world.PlayerNpc; if (player is null) return null; return player.Inventory.Entries.Select(e => world.Inventory.Items.TryGetValue(e.ItemId, out var item) ? item : null).Where(i => i is not null && (!category.HasValue || i.Category == category.Value)).FirstOrDefault(i => Normalize(i!.Name) == target || Normalize(i.Name).Contains(target)); }
    private static bool Matches(string text, params string[] phrases) => phrases.Any(p => text == p || text.StartsWith(p + " "));
    private static string? ExtractAfter(string text, params string[] prefixes) { foreach (var prefix in prefixes) { if (text == prefix) return string.Empty; if (text.StartsWith(prefix + " ")) return text[(prefix.Length + 1)..].Trim(); } return null; }
    private static string CleanTarget(string target) => target.Trim().Trim('.', '!', '?', ',');
    private static CommandParseResult Fail(string raw, string message, PlayerCommandIntent intent = PlayerCommandIntent.Unknown, string target = "") => new() { Success = false, Intent = intent, RawInput = raw, TargetText = target, Feedback = message };
    public static string Normalize(string value) { var form = value.ToLowerInvariant().Normalize(NormalizationForm.FormD); var builder = new StringBuilder(form.Length); foreach (var c in form) if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(c); var normalized = builder.ToString().Normalize(NormalizationForm.FormC); normalized = Regex.Replace(normalized, @"[^\p{L}\p{Nd}\s]", " "); return Regex.Replace(normalized, @"\s+", " ").Trim(); }
}
