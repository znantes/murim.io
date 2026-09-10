namespace Murim.Simulation;

public sealed class LivingWorldRuntime
{
    public WorldState World { get; }
    public GameContent Content { get; }
    public WorldSimulationEngine BaseEngine { get; }
    public InjurySystem Injuries { get; }
    public MartialProgressionSystem Martial { get; }
    public TechniqueMasterySystem Techniques { get; }
    public ObligationSystem Obligations { get; } = new();
    public GoalSystem Goals { get; } = new();
    public SocialMemorySystem SocialMemory { get; } = new();
    public EconomyEcologySystem EconomyEcology { get; }
    public CareerProgressionSystem Careers { get; } = new();
    public RoutineSystem Routines { get; } = new();
    public DragonPhoenixTournamentSystem DragonPhoenix { get; }

    public LivingWorldRuntime(WorldState world, GameContent content, WorldSimulationEngine baseEngine, int seed)
    {
        World = world; Content = content; BaseEngine = baseEngine;
        Injuries = new InjurySystem(seed + 401); Martial = new MartialProgressionSystem(seed + 403); Techniques = new TechniqueMasterySystem(seed + 409);
        EconomyEcology = new EconomyEcologySystem(seed + 419); DragonPhoenix = new DragonPhoenixTournamentSystem(seed + 421);
    }

    public void AdvanceMinutes(int minutes)
    {
        if (minutes <= 0) return;
        var oldDay = World.Clock.Day;
        BaseEngine.AdvanceMinutes(World, minutes);
        var elapsedDays = Math.Max(0, World.Clock.Day - oldDay);
        for (var d = 0L; d < elapsedDays; d++)
        {
            foreach (var npc in World.Npcs.Values.Where(n => n.IsAlive))
            {
                Injuries.AdvanceDay(npc); Techniques.AdvanceForgetting(npc, World.Clock.Day); Goals.RefreshGoals(World, npc);
                npc.Physiology.Spirit.DecisionFatigue = Math.Max(0, npc.Physiology.Spirit.DecisionFatigue - .7);
            }
            SocialMemory.AdvanceDay(World); EconomyEcology.AdvanceDay(World); Obligations.Advance(World, BaseEngine.Relationships);
            AdvanceCareerPromotions(); AdvanceTournamentCalendar();
        }
        Obligations.Advance(World, BaseEngine.Relationships);
    }

    public void TrainPlayerForDays(int days, double dailyHours = 5, double intensity = .55)
    {
        var player = World.PlayerNpc ?? throw new InvalidOperationException("Aucun PNJ joueur.");
        days = Math.Clamp(days, 1, 3650);
        for (var i = 0; i < days && player.IsAlive; i++)
        {
            Martial.Train(player, dailyHours, intensity, .25); player.Physiology.Spirit.DecisionFatigue = Math.Clamp(player.Physiology.Spirit.DecisionFatigue + 1.2, 0, 100);
            AdvanceMinutes(1440);
        }
    }

    public void PracticeTechniqueForDays(int techniqueId, int days, double dailyHours = 4)
    {
        var player = World.PlayerNpc ?? throw new InvalidOperationException("Aucun PNJ joueur.");
        var technique = Content.Techniques.FirstOrDefault(t => t.Id == techniqueId) ?? throw new ArgumentException("Technique inconnue.", nameof(techniqueId));
        for (var i = 0; i < Math.Clamp(days, 1, 3650) && player.IsAlive; i++)
        {
            Techniques.Practice(player, technique, dailyHours, .2, .35, World.Clock.Day, Injuries); AdvanceMinutes(1440);
        }
    }

    public IReadOnlyList<string> PlayerConditionLines()
    {
        var player = World.PlayerNpc; if (player is null) return ["Aucun personnage."];
        var lines = player.Injuries.Where(i => i.Active || i.Permanent).OrderByDescending(i => i.Severity).Select(i => i.DisplayText).ToList();
        if (lines.Count == 0) lines.Add("Aucune blessure importante observée.");
        lines.Add($"Endurance : {(player.Body.Fatigue < 25 ? "fraîche" : player.Body.Fatigue < 55 ? "fatiguée" : player.Body.Fatigue < 80 ? "très fatiguée" : "épuisée")}");
        lines.Add($"Rang martial connu : {player.Martial.DisplayLabel}");
        return lines;
    }

    private void AdvanceCareerPromotions()
    {
        foreach (var npc in World.Npcs.Values.Where(n => n.IsAlive))
        {
            var record = npc.CareerHistory.LastOrDefault(c => c.EndDay is null); if (record is null) continue;
            var profession = Content.Professions.FirstOrDefault(p => p.Code == record.ProfessionCode); if (profession is null) continue;
            if (Careers.EvaluatePromotion(npc, profession, World.Clock.Day))
                World.Events.Add(new WorldEvent(Guid.NewGuid(), WorldEventType.Promotion, World.Clock.Day, npc.CurrentLocationId, [npc.Id], [], $"{npc.Identity.DisplayName} est désormais {npc.CareerStandings[profession.Code].Title}.", .15, .22, .02, []));
        }
    }

    private void AdvanceTournamentCalendar()
    {
        foreach (var tournament in World.Tournaments.Values.Where(t => t.Phase is not (TournamentPhase.Completed or TournamentPhase.Cancelled)).ToArray())
        {
            if (World.Clock.Day >= tournament.StartDay && (int)tournament.Phase < (int)TournamentPhase.RegionalQualifiers) DragonPhoenix.RunQualifiers(World, tournament);
            if (World.Clock.Day >= tournament.EndDay && tournament.Phase != TournamentPhase.Completed) DragonPhoenix.Resolve(World, tournament);
        }
    }
}

public sealed class PlayerCommandFacade
{
    private readonly LivingWorldRuntime runtime;
    public PlayerCommandFacade(LivingWorldRuntime runtime) => this.runtime = runtime;

    public string Execute(string command)
    {
        command = command.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(command)) return "Aucune action saisie.";
        if (command is "état" or "etat" or "statut") return string.Join(Environment.NewLine, runtime.PlayerConditionLines());
        if (TryDuration(command, "s'entraîner", out var trainingDays) || TryDuration(command, "entrainer", out trainingDays) || TryDuration(command, "entraine", out trainingDays))
        {
            runtime.TrainPlayerForDays(trainingDays); return $"Vous vous entraînez pendant {trainingDays} jour(s). Le monde a avancé d'autant.";
        }
        if (TryDuration(command, "attendre", out var waitDays)) { runtime.AdvanceMinutes(waitDays * 1440); return $"{waitDays} jour(s) passent."; }
        return "Commande non reconnue. Les actions disponibles dépendent du lieu, de l'âge et de ce que votre personnage connaît.";
    }

    private static bool TryDuration(string command, string verb, out int days)
    {
        days = 0; if (!command.Contains(verb, StringComparison.OrdinalIgnoreCase)) return false;
        var numbers = new string(command.Where(c => char.IsDigit(c) || char.IsWhiteSpace(c)).ToArray()).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var amount = numbers.Select(x => int.TryParse(x, out var n) ? n : 0).FirstOrDefault(n => n > 0); if (amount == 0) amount = 1;
        if (command.Contains("semaine")) days = amount * 7; else if (command.Contains("mois")) days = amount * 30; else if (command.Contains("an") || command.Contains("année")) days = amount * 365; else days = amount;
        days = Math.Clamp(days, 1, 3650); return true;
    }
}
