namespace Murim.World;

public enum MartialWarStatus { Preparing, Active, Ceasefire, Concluded }

public sealed class MartialWar
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid AttackerOrganizationId { get; init; }
    public Guid DefenderOrganizationId { get; init; }
    public MartialWarStatus Status { get; set; } = MartialWarStatus.Preparing;
    public long StartedDay { get; init; }
    public long? EndedDay { get; set; }
    public double AttackerStrength { get; set; }
    public double DefenderStrength { get; set; }
    public int AttackerCasualties { get; set; }
    public int DefenderCasualties { get; set; }
    public int Battles { get; set; }
    public string Cause { get; init; } = string.Empty;
}

public sealed class MartialWarSystem
{
    private readonly Dictionary<Guid, MartialWar> _wars = new();
    public IReadOnlyDictionary<Guid, MartialWar> Wars => _wars;

    public MartialWar? StartWar(WorldState world, MartialOrganization attacker, MartialOrganization defender, string cause)
    {
        if (attacker.Id == defender.Id) return null;
        if (_wars.Values.Any(w => (w.Status is MartialWarStatus.Preparing or MartialWarStatus.Active) && ((w.AttackerOrganizationId == attacker.Id && w.DefenderOrganizationId == defender.Id) || (w.AttackerOrganizationId == defender.Id && w.DefenderOrganizationId == attacker.Id)))) return null;
        var war = new MartialWar { AttackerOrganizationId = attacker.Id, DefenderOrganizationId = defender.Id, StartedDay = world.Time.Day, Cause = cause, AttackerStrength = CalculateStrength(world, attacker), DefenderStrength = CalculateStrength(world, defender) };
        _wars[war.Id] = war;
        war.Status = MartialWarStatus.Active;
        world.MartialConflicts.Create(attacker, defender, MartialConflictType.War, world, cause, 100);
        return war;
    }

    public bool ResolveBattle(WorldState world, MartialWar war)
    {
        if (war.Status != MartialWarStatus.Active) return false;
        if (!world.MartialOrganizations.Organizations.TryGetValue(war.AttackerOrganizationId, out var attacker) || !world.MartialOrganizations.Organizations.TryGetValue(war.DefenderOrganizationId, out var defender)) { Conclude(war, world); return false; }
        war.Battles++;
        var attackerRoll = war.AttackerStrength * (0.8 + DeterministicUnit(world.WorldSeed, war.Id, war.Battles, 1) * 0.4);
        var defenderRoll = war.DefenderStrength * (0.8 + DeterministicUnit(world.WorldSeed, war.Id, war.Battles, 2) * 0.4);
        var total = Math.Max(1, attackerRoll + defenderRoll);
        var attackerMembers = ActiveMembers(world, attacker);
        var defenderMembers = ActiveMembers(world, defender);
        var attackerLosses = Math.Min(attackerMembers.Count, Math.Max(1, (int)Math.Round(defenderRoll / total * Math.Max(1, attackerMembers.Count) * 0.08)));
        var defenderLosses = Math.Min(defenderMembers.Count, Math.Max(1, (int)Math.Round(attackerRoll / total * Math.Max(1, defenderMembers.Count) * 0.08)));
        war.AttackerCasualties += attackerLosses;
        war.DefenderCasualties += defenderLosses;
        war.AttackerStrength = Math.Max(0, war.AttackerStrength - attackerLosses * 1.5);
        war.DefenderStrength = Math.Max(0, war.DefenderStrength - defenderLosses * 1.5);
        world.MartialWarConsequences.ApplyBattleConsequences(world, war.Id, attackerMembers, defenderMembers, attackerLosses, defenderLosses);
        world.AdvanceMinutes(60);
        if (war.AttackerStrength <= 1 || war.DefenderStrength <= 1 || war.Battles >= 20) Conclude(war, world);
        return true;
    }

    public void Conclude(MartialWar war, WorldState world)
    {
        if (war.Status == MartialWarStatus.Concluded) return;
        war.Status = MartialWarStatus.Concluded;
        war.EndedDay = world.Time.Day;
        var conflict = world.MartialConflicts.Conflicts.Values.FirstOrDefault(c => c.Active && ((c.FirstOrganizationId == war.AttackerOrganizationId && c.SecondOrganizationId == war.DefenderOrganizationId) || (c.FirstOrganizationId == war.DefenderOrganizationId && c.SecondOrganizationId == war.AttackerOrganizationId)));
        if (conflict is not null) world.MartialConflicts.Resolve(conflict, world, "Fin de la guerre martiale");
    }

    private static List<Npc> ActiveMembers(WorldState world, MartialOrganization organization) => organization.MemberIds.Select(id => world.Npcs.TryGetValue(id, out var npc) ? npc : null).Where(n => n is not null).Cast<Npc>().Where(n => n.IsAlive).ToList();

    private static double CalculateStrength(WorldState world, MartialOrganization organization)
    {
        var members = ActiveMembers(world, organization);
        if (members.Count == 0) return 1;
        var mastery = members.Sum(n => n.Martial.CombatExperience + n.Martial.PhysicalDiscipline + n.Martial.Techniques.Sum(t => t.Proficiency) * 0.2);
        var ranks = organization.Ranks.Values.Sum(r => r switch { MartialRank.Leader => 25, MartialRank.Master => 18, MartialRank.Elder => 12, MartialRank.Instructor => 8, MartialRank.InnerDisciple => 5, _ => 2 });
        return Math.Max(1, members.Count * 5 + mastery * 0.12 + ranks + organization.Reputation * 0.3);
    }

    private static double DeterministicUnit(int seed, Guid id, int battle, int salt)
    {
        unchecked { var hash = seed ^ (salt * 16777619); foreach (var b in id.ToByteArray()) hash = (hash ^ b) * 16777619; hash ^= battle * 374761393; hash ^= hash >> 13; hash *= 1274126177; hash ^= hash >> 16; return (uint)hash / (double)uint.MaxValue; }
    }
}
