using FluentAssertions;
using PigeonPea.Dungeon.Control;
using PigeonPea.Dungeon.Core;
using Xunit;

namespace PigeonPea.Dungeon.Tests;

public class FovCalculatorTests
{
    [Fact]
    public void ComputeVisible_OriginOnly_RangeZero()
    {
        var d = new DungeonData(10, 10);
        d.SetWalkable(5, 5, true);
        var fov = new FovCalculator(d);
        var vis = fov.ComputeVisible(5, 5, 0);
        vis[5, 5].Should().BeTrue();
        vis.Cast<bool>().Count(b => b).Should().Be(1);
    }

    [Fact]
    public void ComputeVisible_DiamondShape_Range2()
    {
        var d = new DungeonData(10, 10);
        var fov = new FovCalculator(d);
        var vis = fov.ComputeVisible(5, 5, 2);
        vis[5, 5].Should().BeTrue();
        vis[5, 7].Should().BeTrue(); // right
        vis[5, 3].Should().BeTrue(); // left
        vis[7, 5].Should().BeTrue(); // down
        vis[3, 5].Should().BeTrue(); // up
    }
}
