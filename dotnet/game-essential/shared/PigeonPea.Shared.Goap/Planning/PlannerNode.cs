using System;
using System.Collections.Generic;
using PigeonPea.Shared.Goap.Actions;

namespace PigeonPea.Shared.Goap.Planning;

/// <summary>
/// A* search node representing a world state and the path to reach it.
/// </summary>
internal sealed class PlannerNode : IComparable<PlannerNode>
{
    public WorldState.WorldState State { get; }
    public List<GoapAction> Path { get; }
    public float CostSoFar { get; }
    public float EstimatedTotalCost { get; set; }

    public PlannerNode(WorldState.WorldState state, List<GoapAction> path, float costSoFar)
    {
        State = state;
        Path = path;
        CostSoFar = costSoFar;
        EstimatedTotalCost = costSoFar;
    }

    public int CompareTo(PlannerNode? other)
    {
        if (other == null) return 1;
        return EstimatedTotalCost.CompareTo(other.EstimatedTotalCost);
    }
}
