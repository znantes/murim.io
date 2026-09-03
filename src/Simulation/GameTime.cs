namespace Murim.Simulation;

public enum TimePeriod
{
    Morning,
    Afternoon,
    Evening,
    Night
}

public sealed class GameTime
{
    public long Day { get; private set; } = 1;
    public int MinuteOfDay { get; private set; } = 0;

    public TimePeriod Period => MinuteOfDay switch
    {
        < 30 => TimePeriod.Morning,
        < 60 => TimePeriod.Afternoon,
        < 90 => TimePeriod.Evening,
        _ => TimePeriod.Night
    };

    public void AdvanceMinutes(int minutes)
    {
        if (minutes < 0)
            throw new ArgumentOutOfRangeException(nameof(minutes));

        var total = MinuteOfDay + minutes;
        Day += total / 120;
        MinuteOfDay = total % 120;
    }
}
