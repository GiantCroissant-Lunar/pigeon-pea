using System.Collections.Generic;
using System.Linq;
using PigeonPea.Overlays;

namespace PigeonPea.Map.Core;

/// <summary>
/// Produces world-space overlay features (capitals, dungeon entrances, etc.)
/// from a generated map. Rendering layers can consume these overlay features
/// without depending directly on FantasyMapGenerator internals.
/// </summary>
public sealed class FmgWorldOverlaySource : IOverlaySource<MapData, WorldPosition>
{
    public IEnumerable<IOverlayFeature<WorldPosition>> GetOverlays(MapData map)
    {
        if (map.Burgs is not null)
        {
            var burgs = map.Burgs.Where(b => b is not null).ToList();

            // Capitals layer (kept for backward compatibility and dedicated styling)
            foreach (var burg in burgs.Where(b => b.IsCapital))
            {
                yield return new WorldOverlayFeature(
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
            }

            // Non-capital settlements (cities / towns / villages) for LOD-aware rendering
            foreach (var burg in burgs.Where(b => !b.IsCapital))
            {
                var tier = ClassifySettlementTier(burg.Population);
                if (tier is null)
                {
                    continue;
                }

                yield return new WorldOverlayFeature(
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
            }
        }

        if (map.Dungeons is not null)
        {
            foreach (var dungeon in map.Dungeons)
            {
                yield return new WorldOverlayFeature(
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
            }
        }
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
