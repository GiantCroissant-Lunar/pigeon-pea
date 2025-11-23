using System.Collections.Generic;
using PigeonPea.Gas.Effects;

namespace PigeonPea.Game.Abilities.Components;

/// <summary>
/// Tracks active gameplay effects on an entity.
/// </summary>
public struct ActiveEffectsComponent
{
    public List<ActiveEffect> Effects { get; set; }

    public ActiveEffectsComponent()
    {
        Effects = new List<ActiveEffect>();
    }
}
