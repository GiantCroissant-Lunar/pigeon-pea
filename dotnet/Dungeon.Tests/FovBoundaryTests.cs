using FluentAssertions;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control;
using Xunit;

namespace PigeonPea.Dungeon.Tests;

public class FovBoundaryTests
{
    [Fact]
    public void ComputeVisible_ClipsAtEdges()
    {
        var d = new DungeonData(3, 3);
        var fov = new FovCalculator(d);
        var vis = fov.ComputeVisible(0, 0, 2);

        vis[0,0].Should().BeTrue();
        // Ensure no IndexOutOfRange and corner neighbors within bounds get marked
        vis[0,1].Should().BeTrue();
        vis[1,0].Should().BeTrue();
    }
}
