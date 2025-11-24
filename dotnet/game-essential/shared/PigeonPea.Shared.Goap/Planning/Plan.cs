using System.Collections.Generic;
using System.Linq;
using PigeonPea.Shared.Goap.Actions;

namespace PigeonPea.Shared.Goap.Planning;

/// <summary>
/// Ordered sequence of actions to achieve a goal.
/// </summary>
public sealed class Plan
{
    public List<GoapAction> Actions { get; set; } = new();
    public float TotalCost { get; set; }

    public bool IsEmpty => Actions.Count == 0;

    public override string ToString() =>
        $"Plan ({Actions.Count} actions, cost {TotalCost:F2}): " +
        string.Join(" ", Actions.Select(a => a.Name));
}
