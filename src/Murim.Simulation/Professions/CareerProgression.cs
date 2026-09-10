namespace Murim.Simulation;

public sealed class CareerStanding
{
    public string ProfessionCode { get; init; } = string.Empty;
    public int GradeIndex { get; set; }
    public long EnteredGradeDay { get; set; }
    public double EmployerTrust { get; set; } = 50;
    public double Reliability { get; set; } = 50;
    public double SeniorityDays { get; set; }
    public string Title { get; set; } = string.Empty;
}

public sealed record CareerLadderDefinition(string ProfessionCode, IReadOnlyList<string> Titles, IReadOnlyList<double> SkillThresholds, IReadOnlyList<int> MinimumDaysInCareer);

public static class CareerLadderCatalog
{
    public static IReadOnlyDictionary<string, CareerLadderDefinition> All { get; } = Build();

    private static IReadOnlyDictionary<string, CareerLadderDefinition> Build()
    {
        var map = new Dictionary<string, CareerLadderDefinition>(StringComparer.OrdinalIgnoreCase);
        void Add(string code, string[] titles, double[] thresholds, int[] days) => map[code] = new CareerLadderDefinition(code, titles, thresholds, days);
        Add("soldier", ["Recrue impériale", "Soldat impérial", "Caporal impérial", "Chef d'escouade impérial", "Lieutenant impérial", "Capitaine impérial", "Commandant impérial"], [0, 8, 20, 34, 50, 68, 84], [0, 90, 365, 730, 1460, 2555, 4380]);
        Add("blacksmith", ["Aide de forge", "Apprenti forgeron", "Forgeron", "Forgeron expérimenté", "Expert forgeron", "Maître forgeron", "Grand maître forgeron"], [0, 7, 18, 34, 52, 72, 91], [0, 120, 540, 1200, 2555, 4380, 7300]);
        Add("weaponsmith", ["Aide armurier", "Apprenti forgeur d'armes", "Forgeur d'armes", "Forgeur confirmé", "Expert des armes", "Maître des armes", "Grand maître des forges martiales"], [0, 9, 22, 38, 58, 76, 93], [0, 180, 730, 1460, 2920, 4745, 8030]);
        Add("cook", ["Aide de cuisine", "Apprenti cuisinier", "Cuisinier", "Cuisinier expérimenté", "Chef", "Maître cuisinier", "Grand maître des saveurs"], [0, 6, 16, 31, 50, 71, 90], [0, 90, 365, 900, 2000, 3650, 6570]);
        Add("physician", ["Assistant soigneur", "Apprenti médecin", "Médecin", "Médecin expérimenté", "Médecin expert", "Maître médecin", "Médecin de renom"], [0, 10, 24, 40, 60, 78, 94], [0, 180, 730, 1825, 3650, 5475, 9125]);
        Add("alchemist", ["Aide du fourneau", "Apprenti alchimiste", "Alchimiste", "Alchimiste confirmé", "Expert du fourneau", "Maître alchimiste", "Grand maître alchimiste"], [0, 10, 25, 42, 62, 80, 95], [0, 180, 730, 1825, 3650, 5840, 9490]);
        Add("escort_guard", ["Porteur d'escorte", "Garde junior", "Garde d'escorte", "Garde expérimenté", "Chef de groupe", "Capitaine d'escorte", "Maître d'agence"], [0, 8, 20, 36, 55, 72, 89], [0, 120, 540, 1200, 2555, 4380, 7300]);
        Add("sect_disciple", ["Disciple extérieur", "Disciple intérieur", "Disciple confirmé", "Disciple d'élite", "Disciple direct", "Instructeur", "Ancien"], [0, 12, 27, 43, 60, 76, 90], [0, 365, 1095, 2190, 3650, 5475, 8030]);
        Add("assassin", ["Informateur", "Auxiliaire de l'ombre", "Opérateur clandestin", "Spécialiste", "Vétéran de l'ombre", "Maître de cellule", "Figure de l'ombre"], [0, 12, 26, 43, 61, 79, 93], [0, 180, 730, 1825, 3285, 5475, 8760]);
        return map;
    }

    public static string GenericTitle(ProfessionDefinition profession, int grade) => grade switch
    {
        0 => $"Novice {profession.Name.ToLowerInvariant()}", 1 => $"Apprenti {profession.Name.ToLowerInvariant()}", 2 => profession.Name,
        3 => $"{profession.Name} expérimenté", 4 => $"Expert {profession.Name.ToLowerInvariant()}", 5 => $"Maître {profession.Name.ToLowerInvariant()}",
        _ => $"Grand maître {profession.Name.ToLowerInvariant()}"
    };
}

public sealed class CareerProgressionSystem
{
    public CareerStanding GetOrCreate(Npc npc, ProfessionDefinition profession, long day)
    {
        if (npc.CareerStandings.TryGetValue(profession.Code, out var standing)) return standing;
        standing = new CareerStanding { ProfessionCode = profession.Code, EnteredGradeDay = day, Title = CareerLadderCatalog.GenericTitle(profession, 0) };
        npc.CareerStandings[profession.Code] = standing; return standing;
    }

    public bool EvaluatePromotion(Npc npc, ProfessionDefinition profession, long day)
    {
        var standing = GetOrCreate(npc, profession, day); if (standing.GradeIndex >= 6) return false;
        var ladder = CareerLadderCatalog.All.GetValueOrDefault(profession.Code);
        var score = profession.CoreSkills.Count == 0 ? 0 : profession.CoreSkills.Average(s => npc.Skills.GetValueOrDefault(s));
        var next = standing.GradeIndex + 1;
        var threshold = ladder?.SkillThresholds.ElementAtOrDefault(next) ?? new[] { 0d, 7, 18, 34, 52, 72, 90 }[next];
        var minDays = ladder?.MinimumDaysInCareer.ElementAtOrDefault(next) ?? new[] { 0, 120, 540, 1200, 2555, 4380, 7300 }[next];
        var totalDays = npc.CareerHistory.Where(c => c.ProfessionCode.Equals(profession.Code, StringComparison.OrdinalIgnoreCase)).Sum(c => Math.Max(0, (c.EndDay ?? day) - c.StartDay));
        standing.SeniorityDays = totalDays;
        if (score < threshold || totalDays < minDays || standing.EmployerTrust < 35 || standing.Reliability < 35) return false;
        standing.GradeIndex = next; standing.EnteredGradeDay = day;
        standing.Title = ladder is null ? CareerLadderCatalog.GenericTitle(profession, next) : ladder.Titles[next];
        return true;
    }

    public void RecordWorkOutcome(Npc npc, ProfessionDefinition profession, bool success, double importance, long day)
    {
        var s = GetOrCreate(npc, profession, day);
        s.Reliability = Math.Clamp(s.Reliability + (success ? 1.5 : -3.5) * Math.Clamp(importance, .1, 1), 0, 100);
        s.EmployerTrust = Math.Clamp(s.EmployerTrust + (success ? 1 : -2.5) * Math.Clamp(importance, .1, 1), 0, 100);
    }
}
