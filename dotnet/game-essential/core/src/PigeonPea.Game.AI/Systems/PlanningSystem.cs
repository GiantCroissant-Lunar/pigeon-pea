using Arch.Core;
using Arch.Core.Extensions;
using NexusGoap.Planning;
using PigeonPea.Game.AI.Adapters;
using PigeonPea.Game.AI.Components;
using PigeonPea.Game.Perception.Components;

namespace PigeonPea.Game.AI.Systems;

/// <summary>
/// Runs the GOAP planner for agents that need a new plan.
/// Uses current perception to build a world state and generates a sequence of actions
/// to achieve the currently selected goal.
/// </summary>
public static class PlanningSystem
{
    /// <summary>
    /// Updates planning for all GOAP agents that have been marked as needing replanning.
    /// </summary>
    /// <param name="world">ECS world.</param>
    /// <param name="currentTime">Current game time.</param>
    /// <param name="planningInterval">
    /// Minimum time between planning attempts per agent. Use 0 to disable throttling.
    /// </param>
    public static void Update(World world, float currentTime, float planningInterval)
    {
        var query = new QueryDescription()
            .WithAll<GoapAgentComponent, GoalComponent, PlanComponent, PerceptionComponent>();

        world.Query(in query, (Entity entity,
            ref GoapAgentComponent agent,
            ref GoalComponent goalComponent,
            ref PlanComponent planComponent,
            ref PerceptionComponent perception) =>
        {
            // Only re-plan when explicitly requested by other systems (goal change, plan invalidated, etc.)
            if (!agent.NeedsReplan)
            {
                return;
            }

            // Throttle planning to avoid running every frame
            if (planningInterval > 0f &&
                currentTime - planComponent.LastPlanningTime < planningInterval)
            {
                return;
            }

            var goal = goalComponent.CurrentGoal;
            if (goal is null)
            {
                // No active goal -> clear any existing plan
                planComponent.CurrentPlan = null;
                planComponent.CurrentActionIndex = 0;
                planComponent.LastPlanningTime = currentTime;
                agent.NeedsReplan = false;
                return;
            }

            // If there are no actions configured, we cannot plan.
            if (agent.AvailableActions is null || agent.AvailableActions.Count == 0)
            {
                planComponent.CurrentPlan = null;
                planComponent.CurrentActionIndex = 0;
                planComponent.LastPlanningTime = currentTime;
                agent.NeedsReplan = false;
                return;
            }

            // Build current world state from perception + self entity
            var worldState = PerceptionToWorldStateAdapter.Convert(perception.Data, entity);

            var planner = new Planner();
            var result = planner.CreatePlan(worldState, goal, agent.AvailableActions);

            if (result.Success && result.Plan != null)
            {
                planComponent.CurrentPlan = result.Plan;
                planComponent.CurrentActionIndex = 0;
                agent.NeedsReplan = false;
            }
            else
            {
                // Planning failed; clear plan but also clear the flag so we don't hammer the planner every tick.
                planComponent.CurrentPlan = null;
                planComponent.CurrentActionIndex = 0;
                agent.NeedsReplan = false;
            }

            planComponent.LastPlanningTime = currentTime;
        });
    }
}
