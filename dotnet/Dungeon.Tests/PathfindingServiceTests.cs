using FluentAssertions;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control;
using Xunit;

namespace PigeonPea.Dungeon.Tests;

public class PathfindingServiceTests
{
    [Fact]
    public void FindPath_StraightCorridor_ReturnsShortest()
    {
        var d = new DungeonData(5, 1);
        for (int x = 0; x < 5; x++) d.SetWalkable(x, 0, true);
        var pf = new PathfindingService(d);
        var path = pf.FindPath((0,0), (4,0));
        path.Should().NotBeEmpty();
        path.First().Should().Be((0,0));
        path.Last().Should().Be((4,0));
        path.Count.Should().Be(5);
    }

    [Fact]
    public void FindPath_WithObstacle_NoPath()
    {
        var d = new DungeonData(3, 1);
        d.SetWalkable(0,0,true);
        d.SetWalkable(1,0,false);
        d.SetWalkable(2,0,true);
        var pf = new PathfindingService(d);
        pf.FindPath((0,0), (2,0)).Should().BeEmpty();
    }
}
