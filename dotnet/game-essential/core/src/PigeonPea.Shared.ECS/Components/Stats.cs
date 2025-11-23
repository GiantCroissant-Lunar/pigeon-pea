using System.Collections.Generic;
using PigeonPea.Game.Contracts.Stats.Models;

namespace PigeonPea.Shared.ECS.Components;

public struct Stats
{
    public Dictionary<string, float> BaseStats;
    public Dictionary<string, float> CurrentStats;

    public Stats()
    {
        BaseStats = new Dictionary<string, float>();
        CurrentStats = new Dictionary<string, float>();
    }
}
