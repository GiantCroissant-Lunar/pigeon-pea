using System.Collections.Generic;
using System.Linq;
using PigeonPea.Overlays;
using PigeonPea.Shared.Scale;

namespace PigeonPea.Map.Core;

/// <summary>
/// Produces world-space overlay features (capitals, dungeon entrances, etc.)
/// from a generated map. Rendering layers can consume these overlay features
/// without depending directly on FantasyMapGenerator internals.
/// Supports scale-aware filtering when IScaleManager is provided.
/// </summary>
public sealed class FmgWorldOverlaySource : IOverlaySource<MapData, WorldPosition>
{
    private readonly IScaleManager? _scaleManager;

    public FmgWorldOverlaySource(IScaleManager? scaleManager = null)
    {
        _scaleManager = scaleManager;
    }

    public IEnumerable<IOverlayFeature<WorldPosition>> GetOverlays(MapData map)
    {
        if (map.Burgs is not null)
        {
            var burgs = map.Burgs.Where(b => b is not null).ToList();

            // Capitals layer (kept for backward compatibility and dedicated styling)
            foreach (var burg in burgs.Where(b => b.IsCapital))
            {
                var feature = new WorldOverlayFeature(
                    LayerId: "world.capitals",
                    Position: new WorldPosition(burg.Position.X, burg.Position.Y),
                    Kind: "capital_city",
                    Name: burg.Name,
                    Metadata: new Dictionary<string, object?>
                    {
                        ["id"] = burg.Id,
                        ["population"] = burg.Population,
                        ["isPort"] = burg.IsPort
                    });

                if (IsFeatureVisible(feature))
                {
                    yield return feature;
                }
            }

            // Non-capital settlements (cities / towns / villages) for LOD-aware rendering
            foreach (var burg in burgs.Where(b => !b.IsCapital))
            {
                var tier = ClassifySettlementTier(burg.Population);
                if (tier is null)
                {
                    continue;
                }

                var feature = new WorldOverlayFeature(
                    LayerId: "world.settlements",
                    Position: new WorldPosition(burg.Position.X, burg.Position.Y),
                    Kind: tier,
                    Name: burg.Name,
                    Metadata: new Dictionary<string, object?>
                    {
                        ["id"] = burg.Id,
                        ["population"] = burg.Population,
                        ["isPort"] = burg.IsPort,
                        ["tier"] = tier
                    });

                if (IsFeatureVisible(feature))
                {
                    yield return feature;
                }
            }
        }

        if (map.Dungeons is not null)
        {
            foreach (var dungeon in map.Dungeons)
            {
                var feature = new WorldOverlayFeature(
                    LayerId: "world.dungeons",
                    Position: new WorldPosition(dungeon.Origin.X, dungeon.Origin.Y),
                    Kind: "dungeon_entrance",
                    Name: dungeon.Name,
                    Metadata: new Dictionary<string, object?>
                    {
                        ["id"] = dungeon.Id,
                        ["anchorCellId"] = dungeon.AnchorCellId,
                        ["width"] = dungeon.Width,
                        ["height"] = dungeon.Height
                    });

                if (IsFeatureVisible(feature))
                {
                    yield return feature;
                }
            }
        }
    }

    private bool IsFeatureVisible(WorldOverlayFeature feature)
    {
        if (_scaleManager == null)
        {
            return true;
        }

        var activeScale = _scaleManager.ActiveScale;
        var currentZoom = _scaleManager.CurrentZoom;

        if (activeScale.OverlayLayers != null && !activeScale.OverlayLayers.Contains(feature.LayerId))
        {
            return false;
        }

        if (activeScale.OverlayRules != null && 
            activeScale.OverlayRules.TryGetValue(feature.LayerId, out var rule))
        {
            if (currentZoom < rule.MinZoom || currentZoom > rule.MaxZoom)
            {
                return false;
            }

            if (rule.Filter != null)
            {
                return EvaluateFilter(rule.Filter, feature);
            }
        }

        return true;
    }

    private static bool EvaluateFilter(string filter, WorldOverlayFeature feature)
    {
        if (filter == "tier == 'city'" && feature.Metadata.TryGetValue("tier", out var tier))
        {
            return tier?.ToString() == "city";
        }

        return true;
    }

    private static string? ClassifySettlementTier(double populationThousands)
    {
        // Population in thousands; thresholds are approximate and can be tuned.
        if (populationThousands >= 30) return "city";
        if (populationThousands >= 8) return "town";
        if (populationThousands >= 2) return "village";
        // Below this threshold we skip tiny hamlets for world-map overlays
        return null;
    }

    private sealed record WorldOverlayFeature(
        string LayerId,
        WorldPosition Position,
        string Kind,
        string Name,
        IReadOnlyDictionary<string, object?> Metadata)
        : IOverlayFeature<WorldPosition>;
}
