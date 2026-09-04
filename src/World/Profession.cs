namespace Murim.World;

public enum ProfessionType
{
    None,
    Farmer,
    Hunter,
    Craftsman,
    Merchant,
    Guard,
    Soldier,
    Healer,
    Scholar,
    Servant,
    Fisher,
    Courier,
    MartialPractitioner,
    Official,
    Teacher,
    Criminal
}

public sealed class Profession
{
    public ProfessionType Type { get; set; } = ProfessionType.None;
    public int Skill { get; set; }
    public double DailyIncome { get; set; }
    public double DailyExpense { get; set; }
    public bool IsActive { get; set; } = true;
}
