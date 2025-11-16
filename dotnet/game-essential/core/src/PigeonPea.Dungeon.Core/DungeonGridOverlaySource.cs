using System.Collections.Generic;
using PigeonPea.Overlays;

namespace PigeonPea.Dungeon.Core;

public sealed class DungeonGridOverlaySource : IOverlaySource<DungeonData, GridPosition>
{
    public IEnumerable<IOverlayFeature<GridPosition>> GetOverlays(DungeonData dungeon)
    {
        for (int y = 0; y < dungeon.Height; y++)
        {
            for (int x = 0; x < dungeon.Width; x++)
            {
                if (dungeon.IsDoor(x, y))
                {
                    var state = dungeon.Doors[y, x];
                    string kind = state switch
                    {
                        DoorState.Open => "door_open",
                        DoorState.Closed => "door_closed",
                        _ => "door"
                    };

                    yield return new DungeonOverlayFeature(
                        "dungeon.doors",
                        new GridPosition(x, y),
                        kind,
                        $"Door ({x},{y})",
                        new Dictionary<string, object?>
                        {
                            ["x"] = x,
                            ["y"] = y,
                            ["state"] = state.ToString()
                        });
                }
            }
        }
    }

    private sealed record DungeonOverlayFeature(
        string LayerId,
        GridPosition Position,
        string Kind,
        string Name,
        IReadOnlyDictionary<string, object?> Metadata)
        : IOverlayFeature<GridPosition>;
}
