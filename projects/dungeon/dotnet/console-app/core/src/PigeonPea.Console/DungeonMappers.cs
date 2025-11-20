using PigeonPea.Dungeon.Contracts;
using PigeonPea.Dungeon.Contracts.Models;
using Riok.Mapperly.Abstractions;

namespace PigeonPea.Console;

[Mapper]
public partial class DungeonMappers
{
    [MapProperty(nameof(DungeonData.Walkable), nameof(DungeonView.Walkable), Use = nameof(CloneBoolGrid))]
    [MapProperty(nameof(DungeonData.Opaque), nameof(DungeonView.Opaque), Use = nameof(CloneBoolGrid))]
    [MapProperty(nameof(DungeonData.Doors), nameof(DungeonView.Doors), Use = nameof(CloneDoorGrid))]
    public partial DungeonView ToView(DungeonData source);

    private bool[,] CloneBoolGrid(bool[,] source)
    {
        var height = source.GetLength(0);
        var width = source.GetLength(1);
        var result = new bool[height, width];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                result[y, x] = source[y, x];
            }
        }

        return result;
    }

    private byte[,] CloneDoorGrid(DoorState[,] source)
    {
        var height = source.GetLength(0);
        var width = source.GetLength(1);
        var result = new byte[height, width];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                result[y, x] = (byte)source[y, x];
            }
        }

        return result;
    }
}
