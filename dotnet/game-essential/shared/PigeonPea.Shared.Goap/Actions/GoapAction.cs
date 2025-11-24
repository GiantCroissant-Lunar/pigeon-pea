using System.Collections.Generic;
using System.Linq;

namespace PigeonPea.Shared.Goap.Actions;

/// <summary>
/// Represents an action an agent can take to modify world state.
/// Used by GOAP planner to construct action sequences.
/// </summary>
public sealed class GoapAction
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float Cost { get; set; } = 1f;

    public List<Precondition> Preconditions { get; set; } = new();
    public List<Effect> Effects { get; set; } = new();

    /// <summary>
    /// Optional executor for runtime execution (ECS integration layer).
    /// </summary>
    public IActionExecutor? Executor { get; set; }

    /// <summary>
    /// Checks if all preconditions are satisfied by the given state.
    /// </summary>
    public bool CanExecute(WorldState.WorldState state)
    {
        return Preconditions.All(p => p.IsSatisfied(state));
    }

    /// <summary>
    /// Applies all effects to the given state, returning a new state.
    /// </summary>
    public WorldState.WorldState ApplyEffects(WorldState.WorldState state)
    {
        var newState = state;
        foreach (var effect in Effects)
        {
            newState = effect.Apply(newState);
        }
        return newState;
    }

    public override string ToString() => $"{Name} (Cost: {Cost})";
}
