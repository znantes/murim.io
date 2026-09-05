using Murim.Simulation;

namespace Murim.World;

public sealed class ScheduledWorldEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public long DueDay { get; }
    public int DueMinuteOfDay { get; }
    public string Type { get; }
    public string Description { get; }
    internal Action<WorldState>? Handler { get; }
    internal bool Cancelled { get; set; }

    public ScheduledWorldEvent(long dueDay, int dueMinuteOfDay, string type, string description, Action<WorldState>? handler)
    {
        if (dueDay < 1) throw new ArgumentOutOfRangeException(nameof(dueDay));
        if (dueMinuteOfDay < 0 || dueMinuteOfDay >= 120) throw new ArgumentOutOfRangeException(nameof(dueMinuteOfDay));
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Le type d'événement est obligatoire.", nameof(type));
        DueDay = dueDay;
        DueMinuteOfDay = dueMinuteOfDay;
        Type = type;
        Description = description ?? string.Empty;
        Handler = handler;
    }

    internal long AbsoluteMinute => (DueDay - 1) * 120L + DueMinuteOfDay;
}

public sealed class WorldEventScheduler
{
    private readonly PriorityQueue<ScheduledWorldEvent, (long Minute, long Order)> _queue = new();
    private long _order;

    public IReadOnlyCollection<ScheduledWorldEvent> Pending => _queue.UnorderedItems.Select(x => x.Element).ToArray();
    public List<ScheduledWorldEvent> Executed { get; } = new();

    public ScheduledWorldEvent Schedule(GameTime time, int minutesFromNow, string type, string description, Action<WorldState>? handler = null)
    {
        if (minutesFromNow < 0) throw new ArgumentOutOfRangeException(nameof(minutesFromNow));
        var absolute = (time.Day - 1) * 120L + time.MinuteOfDay + minutesFromNow;
        var day = absolute / 120L + 1;
        var minute = (int)(absolute % 120L);
        var scheduled = new ScheduledWorldEvent(day, minute, type, description, handler);
        _queue.Enqueue(scheduled, (scheduled.AbsoluteMinute, _order++));
        return scheduled;
    }

    public bool Cancel(Guid eventId)
    {
        foreach (var item in _queue.UnorderedItems)
        {
            if (item.Element.Id != eventId) continue;
            item.Element.Cancelled = true;
            return true;
        }
        return false;
    }

    internal void ProcessDue(WorldState world)
    {
        var now = (world.Time.Day - 1) * 120L + world.Time.MinuteOfDay;
        var safety = 0;
        while (_queue.Count > 0 && _queue.Peek().AbsoluteMinute <= now)
        {
            if (++safety > 10000) throw new InvalidOperationException("Trop d'événements mondiaux à exécuter au même instant.");
            var scheduled = _queue.Dequeue();
            if (scheduled.Cancelled) continue;
            scheduled.Handler?.Invoke(world);
            Executed.Add(scheduled);
        }
    }
}
