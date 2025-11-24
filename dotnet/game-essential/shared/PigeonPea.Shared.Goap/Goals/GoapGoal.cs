using PigeonPea.Shared.Goap.WorldState;

namespace PigeonPea.Shared.Goap.Goals;

/// <summary>
/// Represents a desired world state an agent wants to achieve.
/// </summary>
public sealed class GoapGoal
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float Priority { get; set; } = 1f;

    public WorldState.WorldState DesiredState { get; set; } = new();

    /// <summary>
    /// Optional evaluator for dynamic priority calculation.
    /// </summary>
    public IGoalEvaluator? Evaluator { get; set; }

    /// <summary>
    /// Checks if the given state satisfies this goal.
    /// </summary>
    public bool IsSatisfied(WorldState.WorldState state)
    {
        return state.Satisfies(DesiredState);
    }

    /// <summary>
    /// Gets the effective priority, using evaluator if available.
    /// </summary>
    public float GetPriority(WorldState.WorldState currentState)
    {
        return Evaluator?.Evaluate(currentState, this) ?? Priority;
    }

    public override string ToString() => $"{Name} (Priority: {Priority})";
}
