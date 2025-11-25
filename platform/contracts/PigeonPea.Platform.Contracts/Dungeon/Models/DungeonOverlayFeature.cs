using System.Collections.Generic;
using PigeonPea.Platform.Contracts.Overlays;

namespace PigeonPea.Platform.Contracts.Dungeon.Models;

/// <summary>
/// Implementation of IOverlayFeature for dungeon features.
/// </summary>
public sealed record DungeonOverlayFeature(
    string LayerId,
    GridPosition Position,
    string Kind,
    string Name,
    IReadOnlyDictionary<string, object?> Metadata) : IOverlayFeature<GridPosition>;
