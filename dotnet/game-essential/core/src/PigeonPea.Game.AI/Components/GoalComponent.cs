using PigeonPea.Shared.Goap.Goals;

namespace PigeonPea.Game.AI.Components;

public struct GoalComponent
{
    public GoapGoal? CurrentGoal { get; set; }
    public float LastEvaluationTime { get; set; }
}
