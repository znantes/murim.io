using Murim.World;

namespace Murim.Simulation;

public sealed class PlayerCommandService
{
    public CommandInterpreter Interpreter { get; } = new();
    public ActionSystem Actions { get; } = new();

    public ActionResult Execute(WorldState world, string input)
    {
        ArgumentNullException.ThrowIfNull(world);
        var parsed = Interpreter.Parse(world, input);
        return Actions.Execute(world, parsed);
    }
}
