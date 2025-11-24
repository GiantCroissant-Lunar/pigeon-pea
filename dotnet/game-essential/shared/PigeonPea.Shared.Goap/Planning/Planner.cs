using System.Collections.Generic;
using System.Linq;
using PigeonPea.Shared.Goap.Actions;
using PigeonPea.Shared.Goap.Goals;

namespace PigeonPea.Shared.Goap.Planning;

/// <summary>
/// GOAP planner using A* search to find optimal action sequences.
/// Based on Jeff Orkin's FEAR AI architecture.
/// </summary>
public sealed class Planner
{
    public int MaxIterations { get; set; } = 1000;

    /// <summary>
    /// Creates a plan to achieve the given goal from the current state.
    /// </summary>
    public PlanningResult CreatePlan(
        WorldState.WorldState currentState,
        GoapGoal goal,
        List<GoapAction> availableActions)
    {
        if (goal.IsSatisfied(currentState))
        {
            return PlanningResult.Succeeded(new Plan { TotalCost = 0 });
        }

        var openSet = new PriorityQueue<PlannerNode, float>();
        var closedSet = new HashSet<string>(); // State signatures to avoid revisiting

        var startNode = new PlannerNode(currentState, new List<GoapAction>(), 0f);
        startNode.EstimatedTotalCost = Heuristic(currentState, goal.DesiredState);
        openSet.Enqueue(startNode, startNode.EstimatedTotalCost);

        int iterations = 0;

        while (openSet.Count > 0 && iterations < MaxIterations)
        {
            iterations++;

            var currentNode = openSet.Dequeue();

            // Goal check
            if (goal.IsSatisfied(currentNode.State))
            {
                return PlanningResult.Succeeded(new Plan
                {
                    Actions = currentNode.Path.ToList(),
                    TotalCost = currentNode.CostSoFar
                });
            }

            // Mark as visited
            var stateSignature = GetStateSignature(currentNode.State);
            if (!closedSet.Add(stateSignature))
                continue; // Already visited

            // Expand neighbors (applicable actions)
            foreach (var action in availableActions)
            {
                if (!action.CanExecute(currentNode.State))
                    continue;

                var newState = action.ApplyEffects(currentNode.State);
                var newPath = new List<GoapAction>(currentNode.Path) { action };
                var newCost = currentNode.CostSoFar + action.Cost;

                var neighborNode = new PlannerNode(newState, newPath, newCost);
                neighborNode.EstimatedTotalCost = newCost + Heuristic(newState, goal.DesiredState);

                var neighborSignature = GetStateSignature(newState);
                if (!closedSet.Contains(neighborSignature))
                {
                    openSet.Enqueue(neighborNode, neighborNode.EstimatedTotalCost);
                }
            }
        }

        if (iterations >= MaxIterations)
            return PlanningResult.Failed("Max iterations reached");

        return PlanningResult.Failed("No plan found");
    }

    /// <summary>
    /// Heuristic function: number of unsatisfied goal conditions.
    /// Admissible (never overestimates) for A* correctness.
    /// </summary>
    private float Heuristic(WorldState.WorldState currentState, WorldState.WorldState desiredState)
    {
        return currentState.DifferenceCount(desiredState);
    }

    /// <summary>
    /// Creates a unique signature for a world state (for visited set).
    /// </summary>
    private string GetStateSignature(WorldState.WorldState state)
    {
        return state.ToString(); // Simple but effective
    }
}
