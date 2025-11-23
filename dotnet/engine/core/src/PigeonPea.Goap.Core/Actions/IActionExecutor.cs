namespace PigeonPea.Goap.Actions;

/// <summary>
/// Interface for runtime action execution.
/// Implemented in ECS integration layer (PigeonPea.Game.AI).
/// </summary>
public interface IActionExecutor
{
    /// <summary>
    /// Executes the action for the given agent.
    /// Returns true if execution succeeded, false if it failed.
    /// </summary>
    bool Execute(object agent, GoapAction action);
}
