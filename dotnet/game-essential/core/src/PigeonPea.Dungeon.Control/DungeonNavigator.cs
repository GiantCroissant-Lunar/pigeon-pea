using System.Collections.Generic;
using PigeonPea.Dungeon.Core;

namespace PigeonPea.Dungeon.Control;

public sealed class DungeonNavigator
{
    private readonly DungeonData _dungeon;
    private readonly PathfindingService _path;
    private readonly FovCalculator _fov;

    public DungeonNavigator(DungeonData dungeon, bool diagonals = false)
    {
        _dungeon = dungeon;
        _path = new PathfindingService(dungeon, diagonals);
        _fov = new FovCalculator(dungeon);
    }

    public List<(int x, int y)> Path((int x, int y) start, (int x, int y) goal)
        => _path.FindPath(start, goal);

    public bool[,] Visible(int originX, int originY, int range)
        => _fov.ComputeVisible(originX, originY, range);
}
