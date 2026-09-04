namespace Murim.World;

public enum MartialOrganizationType { School, Sect, Clan, Alliance, GuardAcademy, IndependentMaster }
public enum MartialRank { Outsider, Disciple, InnerDisciple, Instructor, Elder, Master, Leader }

public sealed class MartialOrganization
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; init; } = "Organisation inconnue";
    public MartialOrganizationType Type { get; init; }
    public string Doctrine { get; init; } = "";
    public Guid? LocationId { get; init; }
    public List<Guid> MemberIds { get; } = new();
    public Dictionary<Guid, MartialRank> Ranks { get; } = new();
    public List<Guid> CurriculumTechniqueIds { get; } = new();
    public double Reputation { get; set; }
    public MartialRank RankOf(Guid npcId) => Ranks.TryGetValue(npcId, out var rank) ? rank : MartialRank.Outsider;
}

public sealed class MartialOrganizationSystem
{
    private readonly Dictionary<Guid, MartialOrganization> _organizations = new();
    public IReadOnlyDictionary<Guid, MartialOrganization> Organizations => _organizations;

    public MartialOrganization Create(string name, MartialOrganizationType type, string doctrine, Guid? locationId = null)
    {
        var organization = new MartialOrganization { Name = name, Type = type, Doctrine = doctrine, LocationId = locationId };
        _organizations[organization.Id] = organization;
        return organization;
    }

    public bool Recruit(WorldState world, MartialOrganization organization, Npc npc, MartialRank rank = MartialRank.Disciple)
    {
        if (!npc.IsAlive || organization.MemberIds.Contains(npc.Id)) return false;
        if (organization.LocationId is Guid location && npc.CurrentLocationId != location) return false;
        if (organization.Type != MartialOrganizationType.IndependentMaster && npc.AgeYears < 5) return false;
        var resistance = 20 + npc.Personality.Pride * 35 - npc.Personality.Sociability * 20;
        var chance = Math.Clamp(0.55 + (organization.Reputation - resistance) / 160.0, 0.05, 0.95);
        var random = new Random(HashCode.Combine(world.WorldSeed, organization.Id, npc.Id, world.Time.Day));
        if (random.NextDouble() > chance) return false;
        organization.MemberIds.Add(npc.Id);
        organization.Ranks[npc.Id] = rank;
        npc.History.Add("Recrutement martial", npc.AgeYears, $"Rejoint {organization.Name} comme {rank}.");
        return true;
    }

    public bool Leave(MartialOrganization organization, Npc npc, string reason = "Départ volontaire")
    {
        if (!organization.MemberIds.Remove(npc.Id)) return false;
        organization.Ranks.Remove(npc.Id);
        npc.History.Add("Départ martial", npc.AgeYears, $"Quitte {organization.Name} : {reason}.");
        return true;
    }

    public bool Promote(MartialOrganization organization, Npc npc, MartialTrainingSystem martial)
    {
        if (!organization.Ranks.TryGetValue(npc.Id, out var current)) return false;
        var next = current switch
        {
            MartialRank.Disciple => MartialRank.InnerDisciple,
            MartialRank.InnerDisciple => MartialRank.Instructor,
            MartialRank.Instructor => MartialRank.Elder,
            MartialRank.Elder => MartialRank.Master,
            MartialRank.Master => MartialRank.Leader,
            _ => MartialRank.Leader
        };
        if (next == current) return false;
        var relevant = organization.CurriculumTechniqueIds.Select(id => martial.ProfileOf(npc).Get(id)?.Proficiency ?? 0).DefaultIfEmpty(0).Average();
        var threshold = next switch { MartialRank.InnerDisciple => 10, MartialRank.Instructor => 25, MartialRank.Elder => 45, MartialRank.Master => 65, MartialRank.Leader => 85, _ => 1 };
        if (relevant < threshold) return false;
        organization.Ranks[npc.Id] = next;
        organization.Reputation = Math.Clamp(organization.Reputation + 0.5, -100, 100);
        npc.History.Add("Promotion martiale", npc.AgeYears, $"Progresse au rang {next} dans {organization.Name}.");
        return true;
    }

    public void AddToCurriculum(MartialOrganization organization, Guid techniqueId)
    {
        if (!organization.CurriculumTechniqueIds.Contains(techniqueId)) organization.CurriculumTechniqueIds.Add(techniqueId);
    }
}
