namespace Murim.World;

public enum TerritorialAuthority { Imperial, Orthodox, NonOrthodox, MartialOrganization, Independent, Uncontrolled }
public enum SiegeStatus { Preparing, Active, Assault, Defended, Concluded }

public sealed class MartialTerritory
{
    public Guid Id { get; } = Guid.NewGuid(); public Guid LocationId { get; init; }
    public TerritorialAuthority Authority { get; set; } = TerritorialAuthority.Uncontrolled; public Guid? ControllerOrganizationId { get; set; }
    public double ControlLevel { get; set; } public double Fortification { get; set; } public double GarrisonStrength { get; set; }
    public double Supply { get; set; } = 100; public bool Contested { get; set; } public long LastChangedDay { get; set; }
}

public sealed class MartialSiege
{
    public Guid Id { get; } = Guid.NewGuid(); public Guid TargetLocationId { get; init; }
    public Guid AttackerOrganizationId { get; init; } public Guid DefenderOrganizationId { get; init; }
    public SiegeStatus Status { get; set; } = SiegeStatus.Preparing; public double Progress { get; set; }
    public double AttackerStrength { get; set; } public double DefenderStrength { get; set; }
    public int AttackerCasualties { get; set; } public int DefenderCasualties { get; set; }
    public long StartedDay { get; init; } public long? EndedDay { get; set; } public string Cause { get; init; } = string.Empty;
}

public sealed class MartialTerritorySystem
{
    private readonly Dictionary<Guid, MartialTerritory> _territories = new(); private readonly Dictionary<Guid, MartialSiege> _sieges = new();
    public IReadOnlyDictionary<Guid, MartialTerritory> Territories => _territories; public IReadOnlyDictionary<Guid, MartialSiege> Sieges => _sieges;

    public MartialTerritory Register(Guid locationId, TerritorialAuthority authority = TerritorialAuthority.Uncontrolled, Guid? controllerOrganizationId = null, double fortification = 0, double garrisonStrength = 0, WorldState? world = null)
    {
        if (_territories.TryGetValue(locationId, out var existing)) return existing;
        var territory = new MartialTerritory { LocationId = locationId, Authority = authority, ControllerOrganizationId = controllerOrganizationId, ControlLevel = controllerOrganizationId is null ? 0 : 100, Fortification = Math.Clamp(fortification, 0, 100), GarrisonStrength = Math.Max(0, garrisonStrength), LastChangedDay = world?.Time.Day ?? 1 };
        _territories[locationId] = territory; return territory;
    }

    public MartialTerritory GetOrRegister(WorldState world, Guid locationId) => _territories.TryGetValue(locationId, out var territory) ? territory : Register(locationId, TerritorialAuthority.Independent, null, 0, 0, world);

    public bool Contest(WorldState world, Guid locationId, MartialOrganization organization)
    {
        var territory = GetOrRegister(world, locationId); if (territory.Authority == TerritorialAuthority.Imperial && territory.ControllerOrganizationId is null) return false;
        territory.Contested = true; territory.ControlLevel = Math.Max(0, territory.ControlLevel - 10); territory.LastChangedDay = world.Time.Day; return true;
    }

    public MartialSiege? StartSiege(WorldState world, Guid locationId, MartialOrganization attacker, MartialOrganization defender, string cause)
    {
        if (attacker.Id == defender.Id) return null;
        var territory = GetOrRegister(world, locationId); if (territory.ControllerOrganizationId != defender.Id) return null;
        if (_sieges.Values.Any(s => (s.Status is SiegeStatus.Preparing or SiegeStatus.Active or SiegeStatus.Assault) && s.TargetLocationId == locationId)) return null;
        var siege = new MartialSiege { TargetLocationId = locationId, AttackerOrganizationId = attacker.Id, DefenderOrganizationId = defender.Id, StartedDay = world.Time.Day, Cause = cause, AttackerStrength = OrganizationStrength(world, attacker), DefenderStrength = Math.Max(1, OrganizationStrength(world, defender) + territory.GarrisonStrength + territory.Fortification * 2) };
        siege.Status = SiegeStatus.Active; territory.Contested = true; _sieges[siege.Id] = siege; return siege;
    }

    public bool ResolveSiegeDay(WorldState world, MartialSiege siege)
    {
        if (siege.Status is SiegeStatus.Concluded or SiegeStatus.Defended) return false;
        if (!world.MartialOrganizations.Organizations.TryGetValue(siege.AttackerOrganizationId, out var attacker) || !world.MartialOrganizations.Organizations.TryGetValue(siege.DefenderOrganizationId, out var defender)) { Conclude(world, siege, false); return false; }
        var attackRoll = siege.AttackerStrength * (0.85 + DeterministicUnit(world.WorldSeed, siege.Id, (int)(world.Time.Day - siege.StartedDay), 1) * 0.3);
        var defenseRoll = siege.DefenderStrength * (0.85 + DeterministicUnit(world.WorldSeed, siege.Id, (int)(world.Time.Day - siege.StartedDay), 2) * 0.3);
        var total = Math.Max(1, attackRoll + defenseRoll); var attackerMembers = ActiveMembers(world, attacker); var defenderMembers = ActiveMembers(world, defender);
        var attackLoss = Math.Min(attackerMembers.Count, Math.Max(0, (int)Math.Round(defenseRoll / total * Math.Max(1, attackerMembers.Count) * 0.02)));
        var defenseLoss = Math.Min(defenderMembers.Count, Math.Max(0, (int)Math.Round(attackRoll / total * Math.Max(1, defenderMembers.Count) * 0.025)));
        siege.AttackerStrength = Math.Max(0, siege.AttackerStrength - attackLoss * 1.5); siege.DefenderStrength = Math.Max(0, siege.DefenderStrength - defenseLoss * 1.5);
        siege.AttackerCasualties += attackLoss; siege.DefenderCasualties += defenseLoss;
        var momentum = (attackRoll - defenseRoll) / Math.Max(1, siege.DefenderStrength + siege.AttackerStrength); siege.Progress = Math.Clamp(siege.Progress + 4 + momentum * 8, 0, 100); siege.Status = siege.Progress >= 70 ? SiegeStatus.Assault : SiegeStatus.Active;
        world.MartialWarConsequences.ApplySiegeConsequences(world, siege.Id, siege.TargetLocationId, attackerMembers, defenderMembers, attackLoss, defenseLoss, siege.Progress);
        if (siege.Progress >= 100 || siege.DefenderStrength <= 1) Conclude(world, siege, true); else if (siege.AttackerStrength <= 1) Conclude(world, siege, false);
        return true;
    }

    public void Conclude(WorldState world, MartialSiege siege, bool attackerWins)
    {
        if (siege.Status == SiegeStatus.Concluded) return; siege.Status = attackerWins ? SiegeStatus.Concluded : SiegeStatus.Defended; siege.EndedDay = world.Time.Day;
        var territory = GetOrRegister(world, siege.TargetLocationId); territory.Contested = false;
        if (attackerWins && world.MartialOrganizations.Organizations.TryGetValue(siege.AttackerOrganizationId, out var attacker))
        {
            territory.Authority = TerritorialAuthority.MartialOrganization; territory.ControllerOrganizationId = attacker.Id; territory.ControlLevel = 35; territory.GarrisonStrength = Math.Max(1, siege.AttackerStrength * 0.35); territory.Supply = Math.Max(25, territory.Supply - 20); territory.LastChangedDay = world.Time.Day;
            world.MartialWarConsequences.ApplyOccupation(world, siege.TargetLocationId, attacker.Id);
        }
        else { territory.ControlLevel = Math.Min(100, territory.ControlLevel + 10); territory.Supply = Math.Max(0, territory.Supply - 10); territory.LastChangedDay = world.Time.Day; }
    }

    public void AdvanceDay(WorldState world)
    {
        foreach (var territory in _territories.Values)
        {
            if (territory.ControllerOrganizationId is Guid controller && !world.MartialOrganizations.Organizations.ContainsKey(controller)) { territory.ControllerOrganizationId = null; territory.Authority = TerritorialAuthority.Independent; territory.ControlLevel = 0; territory.GarrisonStrength = 0; }
            territory.Supply = Math.Clamp(territory.Supply + 2, 0, 100); if (!territory.Contested) territory.ControlLevel = Math.Clamp(territory.ControlLevel + 0.5, 0, 100);
        }
        foreach (var siege in _sieges.Values.Where(s => s.Status is SiegeStatus.Active or SiegeStatus.Assault).ToList()) ResolveSiegeDay(world, siege);
    }

    private static List<Npc> ActiveMembers(WorldState world, MartialOrganization organization) => organization.MemberIds.Select(id => world.Npcs.TryGetValue(id, out var npc) ? npc : null).Where(n => n is not null).Cast<Npc>().Where(n => n.IsAlive).ToList();
    private static double OrganizationStrength(WorldState world, MartialOrganization organization) { var members = ActiveMembers(world, organization); if (members.Count == 0) return 1; var martial = members.Sum(n => n.Martial.CombatExperience + n.Martial.PhysicalDiscipline + n.Martial.Techniques.Sum(t => t.Proficiency) * 0.15); return Math.Max(1, members.Count * 5 + martial * 0.12 + organization.Ranks.Values.Sum(RankValue)); }
    private static double RankValue(MartialRank rank) => rank switch { MartialRank.Leader => 25, MartialRank.Master => 18, MartialRank.Elder => 12, MartialRank.Instructor => 8, MartialRank.InnerDisciple => 5, MartialRank.Disciple => 2, _ => 1 };
    private static double DeterministicUnit(int seed, Guid id, int day, int salt) { unchecked { var hash = seed ^ (salt * 16777619); foreach (var b in id.ToByteArray()) hash = (hash ^ b) * 16777619; hash ^= day * 374761393; hash ^= hash >> 13; hash *= 1274126177; hash ^= hash >> 16; return (uint)hash / (double)uint.MaxValue; } }
}
