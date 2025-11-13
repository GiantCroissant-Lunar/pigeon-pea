using FluentAssertions;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control;
using Xunit;

namespace PigeonPea.Dungeon.Tests;

public class FovBlockingTests
{
    [Fact]
    public void ComputeVisible_WallsBlockLos()
    {
        var d = new DungeonData(5, 1);
        d.SetWalkable(0,0, true);
        d.SetWalkable(1,0, false); // wall
        d.SetWalkable(2,0, true);
        d.SetWalkable(3,0, true);
        d.SetWalkable(4,0, true);

        var fov = new FovCalculator(d);
        var vis = fov.ComputeVisible(0, 0, 4);

        vis[0,0].Should().BeTrue();
        vis[0,1].Should().BeFalse("wall blocks LOS");
        vis[0,2].Should().BeFalse("LOS blocked beyond wall");
        vis[0,3].Should().BeFalse("LOS blocked beyond wall");
        vis[0,4].Should().BeFalse("LOS blocked beyond wall");
    }
}
