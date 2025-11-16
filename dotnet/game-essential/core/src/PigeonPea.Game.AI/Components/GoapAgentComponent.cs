using System.Collections.Generic;
using NexusGoap.Actions;
using NexusGoap.Goals;

namespace PigeonPea.Game.AI.Components;

public struct GoapAgentComponent
{
    public List<GoapGoal> AvailableGoals { get; set; }
    public List<GoapAction> AvailableActions { get; set; }
    public bool NeedsReplan { get; set; }
    public int PlanningFrequency { get; set; }

    public GoapAgentComponent()
    {
        AvailableGoals = new List<GoapGoal>();
        AvailableActions = new List<GoapAction>();
        NeedsReplan = true;
        PlanningFrequency = 5;
    }
}
