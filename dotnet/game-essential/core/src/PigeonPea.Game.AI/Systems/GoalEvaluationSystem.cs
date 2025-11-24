using System;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Shared.Goap.Goals;
using PigeonPea.Game.AI.Adapters;
using PigeonPea.Game.AI.Components;
using PigeonPea.Game.Perception.Components;

namespace PigeonPea.Game.AI.Systems;

/// <summary>
/// Evaluates GOAP goals for AI agents based on current world state and perception.
/// Chooses the highest-priority goal and marks agents for replanning when the goal changes.
/// </summary>
public static class GoalEvaluationSystem
{
    /// <summary>
    /// Updates goal selection for all GOAP agents.
    /// </summary>
    /// <param name="world">ECS world.</param>
    /// <param name="currentTime">Current game time.</param>
    /// <param name="evaluationInterval">
    /// Minimum time between evaluations for a single agent. Use 0 to evaluate every call.
    /// </param>
    public static void Update(World world, float currentTime, float evaluationInterval)
    {
        var query = new QueryDescription()
            .WithAll<GoapAgentComponent, GoalComponent, PerceptionComponent>();

        world.Query(in query, (Entity entity,
            ref GoapAgentComponent agent,
            ref GoalComponent goalComponent,
            ref PerceptionComponent perception) =>
        {
            // Respect per-agent evaluation interval
            if (evaluationInterval > 0f &&
                currentTime - goalComponent.LastEvaluationTime < evaluationInterval)
            {
                return;
            }

            // If there are no goals configured, skip
            if (agent.AvailableGoals is null || agent.AvailableGoals.Count == 0)
            {
                goalComponent.LastEvaluationTime = currentTime;
                return;
            }

            // Build GOAP world state from perception + self entity
            var worldState = PerceptionToWorldStateAdapter.Convert(perception.Data, entity);

            GoapGoal? bestGoal = null;
            var bestPriority = float.NegativeInfinity;

            foreach (var goal in agent.AvailableGoals)
            {
                if (goal is null)
                    continue;

                var priority = goal.GetPriority(worldState);
                if (priority > bestPriority)
                {
                    bestPriority = priority;
                    bestGoal = goal;
                }
            }

            var previousGoal = goalComponent.CurrentGoal;

            // If we found a better goal, switch and mark for replanning
            if (!ReferenceEquals(previousGoal, bestGoal))
            {
                goalComponent.CurrentGoal = bestGoal;

                // Only request replanning if the effective goal changed
                agent.NeedsReplan = true;
            }

            goalComponent.LastEvaluationTime = currentTime;
        });
    }
}
