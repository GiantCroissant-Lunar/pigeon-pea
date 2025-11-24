using PigeonPea.Shared.Goap.WorldState;

namespace PigeonPea.Shared.Goap.Goals;

/// <summary>
/// Interface for dynamic goal priority calculation.
/// Implemented in ECS integration layer (PigeonPea.Game.AI).
/// </summary>
public interface IGoalEvaluator
{
    /// <summary>
    /// Evaluates the priority of a goal based on current world state.
    /// Returns a priority value (higher = more important).
    /// </summary>
    float Evaluate(WorldState.WorldState currentState, GoapGoal goal);
}
