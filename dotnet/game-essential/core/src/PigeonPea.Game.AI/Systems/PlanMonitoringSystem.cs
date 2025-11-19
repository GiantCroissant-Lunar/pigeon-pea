using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Game.AI.Adapters;
using PigeonPea.Game.AI.Components;
using PigeonPea.Game.Perception.Components;

namespace PigeonPea.Game.AI.Systems;

/// <summary>
/// Monitors existing GOAP plans against current world state.
/// Clears or invalidates plans when goals are satisfied or when plans become impossible,
/// and requests replanning as needed.
/// </summary>
public static class PlanMonitoringSystem
{
    /// <summary>
    /// Validates current plans and triggers replanning when necessary.
    /// </summary>
    /// <param name="world">ECS world.</param>
    /// <param name="currentTime">Current game time (currently unused but kept for future heuristics).</param>
    public static void Update(World world, float currentTime)
    {
        var query = new QueryDescription()
            .WithAll<GoapAgentComponent, GoalComponent, PlanComponent, PerceptionComponent>();

        world.Query(in query, (Entity entity,
            ref GoapAgentComponent agent,
            ref GoalComponent goalComponent,
            ref PlanComponent planComponent,
            ref PerceptionComponent perception) =>
        {
            if (!planComponent.HasPlan)
            {
                return;
            }

            // Rebuild world state snapshot
            var worldState = PerceptionToWorldStateAdapter.Convert(perception.Data, entity);

            var goal = goalComponent.CurrentGoal;

            // If there is no active goal but we have a plan, clear it.
            if (goal is null)
            {
                planComponent.CurrentPlan = null;
                planComponent.CurrentActionIndex = 0;
                agent.NeedsReplan = false;
                return;
            }

            // If the current goal is already satisfied, clear the plan and request replanning
            // so that a new goal can be evaluated on the next tick.
            if (goal.IsSatisfied(worldState))
            {
                planComponent.CurrentPlan = null;
                planComponent.CurrentActionIndex = 0;
                agent.NeedsReplan = true;
                return;
            }

            // If we still have remaining actions, but the next action is no longer applicable
            // in the current world state, invalidate the plan and request replanning.
            if (!planComponent.IsComplete)
            {
                var action = planComponent.CurrentAction;
                if (action is not null && !action.CanExecute(worldState))
                {
                    planComponent.CurrentPlan = null;
                    planComponent.CurrentActionIndex = 0;
                    agent.NeedsReplan = true;
                }
            }
        });
    }
}
