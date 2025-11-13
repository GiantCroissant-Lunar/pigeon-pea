using FluentAssertions;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control;
using Xunit;

namespace PigeonPea.Dungeon.Tests;

public class PathfindingDiagonalTests
{
    [Fact]
    public void FindPath_DiagonalsDisabled_TakesLongerOrthogonalPath()
    {
        var d = new DungeonData(3, 3);
        // Make all walkable
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
                d.SetWalkable(x, y, true);

        var pf = new PathfindingService(d, allowDiagonals: false);
        var path = pf.FindPath((0,0), (2,2));

        path.Should().NotBeEmpty();
        path.First().Should().Be((0,0));
        path.Last().Should().Be((2,2));
        // No diagonals: must be 5 steps: (0,0)->(1,0)->(2,0)->(2,1)->(2,2)
        path.Count.Should().Be(5);
    }

    [Fact]
    public void FindPath_DiagonalsEnabled_TakesShorterDiagonalPath()
    {
        var d = new DungeonData(3, 3);
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
                d.SetWalkable(x, y, true);

        var pf = new PathfindingService(d, allowDiagonals: true);
        var path = pf.FindPath((0,0), (2,2));

        path.Should().NotBeEmpty();
        path.First().Should().Be((0,0));
        path.Last().Should().Be((2,2));
        // With diagonals via BFS, one possible shortest path length is 3: (0,0)->(1,1)->(2,2)
        path.Count.Should().Be(3);
    }
}
