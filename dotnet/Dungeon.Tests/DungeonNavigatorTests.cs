using FluentAssertions;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control;
using Xunit;

namespace PigeonPea.Dungeon.Tests;

public class DungeonNavigatorTests
{
    [Fact]
    public void Navigator_PathAndFov_WorkOnSmallMap()
    {
        var d = new DungeonData(4, 3);
        // a simple open area 4x3 all walkable
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 4; x++)
                d.SetWalkable(x, y, true);

        var nav = new DungeonNavigator(d);
        var path = nav.Path((0,0), (3,2));
        path.Should().NotBeEmpty();
        path.First().Should().Be((0,0));
        path.Last().Should().Be((3,2));

        var vis = nav.Visible(1,1,1);
        vis[1,1].Should().BeTrue();
        vis[1,0].Should().BeTrue();
        vis[1,2].Should().BeTrue();
        vis[0,1].Should().BeTrue();
        vis[2,1].Should().BeTrue();
    }
}
