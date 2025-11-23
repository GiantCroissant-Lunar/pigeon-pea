using System;
using System.Collections.Generic;
using PigeonPea.Game.Contracts.Stats.Models;

namespace PigeonPea.Game.Contracts.Stats.Models;

public sealed class StatsView
{
    public IReadOnlyDictionary<string, float> BaseStats { get; init; } =
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, float> CurrentStats { get; init; } =
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<StatModifierView> ActiveModifiers { get; init; } =
        Array.Empty<StatModifierView>();

    public static readonly StatsView Empty = new();
}
