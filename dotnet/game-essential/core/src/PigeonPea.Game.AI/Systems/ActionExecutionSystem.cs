using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Goap.Actions;
using PigeonPea.Game.AI.Components;

namespace PigeonPea.Game.AI.Systems;

/// <summary>
/// Executes the current GOAP action for agents with an active plan.
/// Uses the action's IActionExecutor to perform the behavior against the ECS world.
/// </summary>
public static class ActionExecutionSystem
{
    /// <summary>
    /// Steps GOAP plans forward by executing the current action for each agent.
    /// </summary>
    /// <param name="world">ECS world.</param>
    public static void Update(World world)
    {
        var query = new QueryDescription()
            .WithAll<GoapAgentComponent, PlanComponent>();

        world.Query(in query, (Entity entity,
            ref GoapAgentComponent agent,
            ref PlanComponent plan) =>
        {
            if (!plan.HasPlan || plan.IsComplete)
            {
                return;
            }

            var action = plan.CurrentAction;
            if (action is null)
            {
                // Defensive: mark plan complete and request replanning.
                plan.CurrentActionIndex = plan.CurrentPlan?.Actions.Count ?? 0;
                agent.NeedsReplan = true;
                return;
            }

            var executor = action.Executor;
            if (executor is null)
            {
                // No executor configured; treat as failure and request replanning.
                plan.CurrentPlan = null;
                plan.CurrentActionIndex = 0;
                agent.NeedsReplan = true;
                return;
            }

            // Execute the action for this agent. We pass the Entity as the agent object.
            var success = executor.Execute(entity, action);

            if (success)
            {
                // Advance to next action in the plan.
                plan.CurrentActionIndex++;

                // When the plan finishes, we typically want to reevaluate goals
                // and potentially plan again on the next tick.
                if (plan.IsComplete)
                {
                    agent.NeedsReplan = true;
                }
            }
            else
            {
                // Execution failed; clear current plan and request replanning.
                plan.CurrentPlan = null;
                plan.CurrentActionIndex = 0;
                agent.NeedsReplan = true;
            }
        });
    }
}
