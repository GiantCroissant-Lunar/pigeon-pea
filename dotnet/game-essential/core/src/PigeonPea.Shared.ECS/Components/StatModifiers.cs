using System;
using System.Collections.Generic;
using PigeonPea.Game.Contracts.Stats.Models;

namespace PigeonPea.Shared.ECS.Components;

public struct StatModifiers
{
    public List<ActiveModifier> Modifiers;

    public StatModifiers()
    {
        Modifiers = new List<ActiveModifier>();
    }
}

public struct ActiveModifier
{
    public string ModifierId;
    public string StatId;
    public float Value;
    public ModifierType Type;
    public float RemainingDuration;
    public string SourceId;
    public DateTime AppliedAt;
}
