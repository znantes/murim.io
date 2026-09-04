namespace Murim.World;

public enum MentorshipRole
{
    Master,
    SeniorDisciple,
    Disciple
}

public sealed class MartialMentorship
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid MasterId { get; init; }
    public Guid StudentId { get; init; }
    public MentorshipRole Role { get; set; } = MentorshipRole.Master;
    public long StartedDay { get; init; }
    public long LastLessonDay { get; set; }
    public double Trust { get; set; } = 50;
    public double TeachingQuality { get; set; } = 50;
    public bool Active { get; set; } = true;
}

public sealed class MartialMentorshipSystem
{
    private readonly Dictionary<Guid, MartialMentorship> _relationships = new();
    public IReadOnlyDictionary<Guid, MartialMentorship> Relationships => _relationships;

    public MartialMentorship? Get(Npc master, Npc student) =>
        _relationships.Values.FirstOrDefault(x => x.Active && x.MasterId == master.Id && x.StudentId == student.Id);

    public MartialMentorship Establish(Npc master, Npc student, WorldState world, double teachingQuality = 50)
    {
        if (!master.IsAlive || !student.IsAlive) throw new InvalidOperationException("Un mentor ou élève est mort.");
        if (master.Id == student.Id) throw new InvalidOperationException("Un NPC ne peut pas être son propre maître.");
        var existing = Get(master, student);
        if (existing is not null) return existing;

        var mentorship = new MartialMentorship
        {
            MasterId = master.Id,
            StudentId = student.Id,
            StartedDay = world.Time.Day,
            LastLessonDay = world.Time.Day,
            TeachingQuality = Math.Clamp(teachingQuality, 0, 100)
        };
        _relationships[mentorship.Id] = mentorship;
        student.History.Add("Lien maître-élève", student.AgeYears, $"Commence son apprentissage auprès de {master.Identity.DisplayName}.");
        master.History.Add("Lien maître-élève", master.AgeYears, $"Prend {student.Identity.DisplayName} sous son enseignement.");
        return mentorship;
    }

    public bool TeachTechnique(WorldState world, Npc master, Npc student, Guid techniqueId, int minutes = 60)
    {
        var relationship = Get(master, student) ?? Establish(master, student, world);
        var technique = world.Martial.Techniques.TryGetValue(techniqueId, out var found) ? found : null;
        if (technique is null) return false;

        var teacherProgress = master.Martial.Get(techniqueId);
        if (teacherProgress is null || teacherProgress.Proficiency < 20) return false;
        if (student.AgeYears < 4) return false;

        var before = student.Martial.Get(techniqueId)?.Proficiency ?? 0;
        var learned = world.Martial.Teach(master, student, techniqueId, world);
        if (!learned) return false;

        minutes = Math.Clamp(minutes, 15, 240);
        world.AdvanceMinutes(minutes);
        relationship.LastLessonDay = world.Time.Day;
        relationship.Trust = Math.Clamp(relationship.Trust + 0.15, 0, 100);
        relationship.TeachingQuality = Math.Clamp(relationship.TeachingQuality + (teacherProgress.Proficiency > 60 ? 0.1 : 0), 0, 100);

        var progress = student.Martial.Get(techniqueId)!;
        var bonus = Math.Max(0, relationship.TeachingQuality - 50) / 500.0;
        progress.Proficiency = Math.Min(progress.Potential, Math.Max(progress.Proficiency, before + bonus));
        student.History.Add("Leçon martiale", student.AgeYears, $"Reçoit une leçon de {master.Identity.DisplayName} sur {technique.Name}.");
        return true;
    }

    public bool Break(Npc master, Npc student, string reason = "Rupture de transmission")
    {
        var relationship = Get(master, student);
        if (relationship is null) return false;
        relationship.Active = false;
        student.History.Add("Rupture maître-élève", student.AgeYears, $"Fin de l'enseignement de {master.Identity.DisplayName} : {reason}.");
        return true;
    }

    public void AdvanceDay(WorldState world)
    {
        foreach (var relationship in _relationships.Values.Where(x => x.Active))
        {
            if (!world.Npcs.TryGetValue(relationship.MasterId, out var master) || !world.Npcs.TryGetValue(relationship.StudentId, out var student) || !master.IsAlive || !student.IsAlive)
            {
                relationship.Active = false;
                continue;
            }

            var distance = master.CurrentLocationId == student.CurrentLocationId ? 0 : 1;
            relationship.Trust = Math.Clamp(relationship.Trust - distance * 0.05, 0, 100);
        }
    }
}
