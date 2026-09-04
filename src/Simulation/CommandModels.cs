using Murim.World;

namespace Murim.Simulation;

public enum PlayerCommandIntent
{
    Unknown, Observe, Travel, Talk, Train, Eat, Drink, Sleep, InspectSelf,
    AskDirections, Follow, Help, Refuse, Examine, Enter, Work, Buy, Sell, Investigate, DetectDanger
}

public sealed class CommandParseResult
{
    public bool Success { get; init; }
    public PlayerCommandIntent Intent { get; init; }
    public string RawInput { get; init; } = string.Empty;
    public string TargetText { get; init; } = string.Empty;
    public Guid? TargetLocationId { get; init; }
    public Guid? TargetNpcId { get; init; }
    public Guid? TargetItemId { get; init; }
    public Guid? TargetTechniqueId { get; init; }
    public MovementMethod MovementMethod { get; init; } = MovementMethod.Walk;
    public string Feedback { get; init; } = string.Empty;
}

public sealed class ActionResult
{
    public bool Success { get; init; }
    public PlayerCommandIntent Intent { get; init; }
    public int MinutesSpent { get; init; }
    public string Feedback { get; init; } = string.Empty;
}
