using PigeonPea.Shared.Goap.Actions;
using PigeonPea.Shared.Goap.Planning;

namespace PigeonPea.Game.AI.Components;

public struct PlanComponent
{
    public Plan? CurrentPlan { get; set; }
    public int CurrentActionIndex { get; set; }
    public float LastPlanningTime { get; set; }

    public GoapAction? CurrentAction =>
        CurrentPlan != null && CurrentActionIndex >= 0 && CurrentActionIndex < CurrentPlan.Actions.Count
            ? CurrentPlan.Actions[CurrentActionIndex]
            : null;

    public bool HasPlan => CurrentPlan != null && !CurrentPlan.IsEmpty;
    public bool IsComplete => CurrentPlan == null || CurrentActionIndex >= CurrentPlan.Actions.Count;
}
