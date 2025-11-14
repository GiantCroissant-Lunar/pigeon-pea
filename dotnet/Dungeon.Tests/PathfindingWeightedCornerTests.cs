using FluentAssertions;
using PigeonPea.Dungeon.Control;
using PigeonPea.Dungeon.Core;
using Xunit;

namespace PigeonPea.Dungeon.Tests;

public class PathfindingWeightedCornerTests
{
    [Fact]
    public void WeightedCost_PrefersCheaperTiles()
    {
        // Map 3x3, center row expensive
        var d = new DungeonData(3, 3);
        for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) d.SetFloor(x, y);
        double Cost((int x, int y) p) => p.y == 1 ? 10.0 : 1.0;

        var pf = new PathfindingService(d, allowDiagonals: true, tileCost: Cost);
        var path = pf.FindPath((0, 0), (2, 2));

        // Prefer going around the expensive band if diagonals allowed
        path.Should().NotBeEmpty();
        path.Should().Contain((1, 0));
        path.Should().Contain((2, 1));
    }

    [Fact]
    public void CornerCut_Prevented_WhenAdjacentsBlocked()
    {
        var d = new DungeonData(2, 2);
        d.SetFloor(0, 0);
        d.SetWall(1, 0);
        d.SetWall(0, 1);
        d.SetFloor(1, 1);

        var pf = new PathfindingService(d, allowDiagonals: true);
        var path = pf.FindPath((0, 0), (1, 1));
        path.Should().BeEmpty("cannot move diagonally through blocked corner");
    }
}
