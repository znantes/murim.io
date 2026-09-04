namespace Murim.World;

public enum MartialTechniqueCategory
{
    Strike,
    Defense,
    Footwork,
    Grappling,
    Weapon,
    Internal,
    External,
    BodyConditioning
}

public sealed class MartialTechnique
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; init; } = "Technique inconnue";
    public string School { get; init; } = "Transmission libre";
    public MartialTechniqueCategory Category { get; init; }
    public double Difficulty { get; init; } = 25;
    public double StaminaCost { get; init; } = 5;
    public double InternalEnergyCost { get; init; }
    public double StrengthRequirement { get; init; }
    public double SpeedRequirement { get; init; }
    public double CoordinationRequirement { get; init; }
    public Guid? PrerequisiteTechniqueId { get; init; }
}

public sealed class MartialTechniqueProgress
{
    public Guid TechniqueId { get; init; }
    public double Proficiency { get; set; }
    public double Potential { get; set; }
    public long LastTrainingDay { get; set; }
    public bool Known => Proficiency >= 1;
}

public sealed class MartialProfile
{
    public double PhysicalDiscipline { get; set; }
    public double CombatExperience { get; set; }
    public double InternalEnergyCapacity { get; set; }
    public double InternalEnergyControl { get; set; }
    public List<MartialTechniqueProgress> Techniques { get; } = new();

    public MartialTechniqueProgress? Get(Guid techniqueId) => Techniques.FirstOrDefault(t => t.TechniqueId == techniqueId);
}

public sealed class MartialTrainingResult
{
    public Guid NpcId { get; init; }
    public Guid TechniqueId { get; init; }
    public bool Learned { get; init; }
    public double ProgressGained { get; init; }
    public int MinutesSpent { get; init; }
    public string Outcome { get; init; } = string.Empty;
}

public sealed class MartialTrainingSystem
{
    private readonly Dictionary<Guid, MartialProfile> _profiles = new();
    private readonly Dictionary<Guid, MartialTechnique> _techniques = new();

    public IReadOnlyDictionary<Guid, MartialTechnique> Techniques => _techniques;

    public MartialProfile ProfileOf(Npc npc)
    {
        if (!_profiles.TryGetValue(npc.Id, out var profile))
        {
            profile = new MartialProfile
            {
                PhysicalDiscipline = npc.Personality.Discipline,
                InternalEnergyCapacity = Math.Clamp(npc.Inheritance.InternalEnergyPotential, 0, 100),
                InternalEnergyControl = npc.Mind.Concentration * 0.5
            };
            _profiles[npc.Id] = profile;
        }
        return profile;
    }

    public MartialTechnique Register(string name, string school, MartialTechniqueCategory category, double difficulty,
        double staminaCost = 5, double internalEnergyCost = 0, double strengthRequirement = 0,
        double speedRequirement = 0, double coordinationRequirement = 0, Guid? prerequisiteTechniqueId = null)
    {
        var technique = new MartialTechnique
        {
            Name = name,
            School = school,
            Category = category,
            Difficulty = Math.Clamp(difficulty, 1, 100),
            StaminaCost = Math.Max(0, staminaCost),
            InternalEnergyCost = Math.Max(0, internalEnergyCost),
            StrengthRequirement = Math.Max(0, strengthRequirement),
            SpeedRequirement = Math.Max(0, speedRequirement),
            CoordinationRequirement = Math.Max(0, coordinationRequirement),
            PrerequisiteTechniqueId = prerequisiteTechniqueId
        };
        _techniques[technique.Id] = technique;
        return technique;
    }

    public bool Teach(Npc teacher, Npc student, Guid techniqueId, WorldState world)
    {
        if (!_techniques.TryGetValue(techniqueId, out var technique)) return false;
        var teacherProgress = ProfileOf(teacher).Get(techniqueId);
        if (teacherProgress is null || teacherProgress.Proficiency < 20) return false;
        if (!MeetsRequirements(student, technique)) return false;

        var profile = ProfileOf(student);
        var progress = profile.Get(techniqueId) ?? new MartialTechniqueProgress
        {
            TechniqueId = techniqueId,
            Potential = PotentialFor(student, technique)
        };
        if (!profile.Techniques.Contains(progress)) profile.Techniques.Add(progress);
        progress.Proficiency = Math.Max(progress.Proficiency, 1);
        progress.LastTrainingDay = world.Time.Day;
        student.History.Add("Apprentissage martial", student.AgeYears, $"Apprend {technique.Name} auprès de {teacher.Identity.DisplayName}.");
        return true;
    }

    public MartialTrainingResult Train(WorldState world, Npc npc, Guid techniqueId, int minutes = 30)
    {
        if (!npc.IsAlive) throw new InvalidOperationException("Un NPC mort ne peut pas s'entraîner.");
        if (!_techniques.TryGetValue(techniqueId, out var technique)) throw new KeyNotFoundException("Technique inconnue.");
        if (!MeetsRequirements(npc, technique)) throw new InvalidOperationException("Le corps ou les capacités actuelles ne permettent pas cet entraînement.");

        var profile = ProfileOf(npc);
        var progress = profile.Get(techniqueId) ?? new MartialTechniqueProgress
        {
            TechniqueId = techniqueId,
            Potential = PotentialFor(npc, technique)
        };
        if (!profile.Techniques.Contains(progress)) profile.Techniques.Add(progress);

        minutes = Math.Clamp(minutes, 5, 240);
        var conditionPenalty = npc.Conditions.Sum(c => c.Severity * (0.4 + c.Pain / 100.0));
        var fatiguePenalty = npc.Needs.Fatigue / 160.0;
        var aptitude = 0.25 + npc.Mind.LearningAbility / 200.0 + npc.Mind.Concentration / 250.0 + npc.Body.Endurance / 300.0;
        var teacherBonus = progress.Known ? 1.0 : 0.8;
        var plateau = 1.0 - Math.Clamp(progress.Proficiency / Math.Max(1, progress.Potential), 0, 0.9);
        var gain = Math.Max(0.01, minutes / 30.0 * aptitude * teacherBonus * plateau * (1 - Math.Min(0.75, conditionPenalty + fatiguePenalty)));

        progress.Proficiency = Math.Min(progress.Potential, progress.Proficiency + gain);
        progress.LastTrainingDay = world.Time.Day;
        ProfileOf(npc).PhysicalDiscipline = Math.Min(100, ProfileOf(npc).PhysicalDiscipline + gain * 0.2);
        world.AdvanceMinutes(minutes);

        var learned = progress.Proficiency >= 1;
        var outcome = learned ? $"{npc.Identity.DisplayName} progresse dans {technique.Name} (+{gain:0.00})." : $"{npc.Identity.DisplayName} découvre les bases de {technique.Name}.";
        npc.History.Add("Entraînement martial", npc.AgeYears, outcome);
        return new MartialTrainingResult
        {
            NpcId = npc.Id,
            TechniqueId = techniqueId,
            Learned = learned,
            ProgressGained = gain,
            MinutesSpent = minutes,
            Outcome = outcome
        };
    }

    public double CombatModifier(Npc npc, Guid? techniqueId, bool offensive)
    {
        if (techniqueId is not Guid id || !_techniques.TryGetValue(id, out var technique)) return 0;
        var progress = ProfileOf(npc).Get(id);
        if (progress is null || progress.Proficiency < 1) return 0;
        var categoryBonus = offensive
            ? technique.Category is MartialTechniqueCategory.Strike or MartialTechniqueCategory.Weapon or MartialTechniqueCategory.Grappling ? 1 : 0.4
            : technique.Category is MartialTechniqueCategory.Defense or MartialTechniqueCategory.Footwork ? 1 : 0.35;
        return Math.Min(35, progress.Proficiency * 0.35 * categoryBonus);
    }

    private static bool MeetsRequirements(Npc npc, MartialTechnique technique) =>
        npc.Body.Strength >= technique.StrengthRequirement &&
        npc.Body.Speed >= technique.SpeedRequirement &&
        npc.Body.Coordination >= technique.CoordinationRequirement;

    private static double PotentialFor(Npc npc, MartialTechnique technique)
    {
        var basePotential = npc.Inheritance.PhysicalPotential * 0.45 + npc.Inheritance.LearningPotential * 0.35 + npc.Mind.LearningAbility * 0.20;
        var categoryFit = technique.Category switch
        {
            MartialTechniqueCategory.Grappling => npc.Body.Strength * 0.15,
            MartialTechniqueCategory.Footwork => npc.Body.Speed * 0.15,
            MartialTechniqueCategory.Weapon => npc.Body.Coordination * 0.15,
            MartialTechniqueCategory.Internal => npc.Inheritance.InternalEnergyPotential * 0.2,
            _ => npc.Body.Coordination * 0.08
        };
        return Math.Clamp(basePotential + categoryFit - technique.Difficulty * 0.15, 5, 100);
    }
}
