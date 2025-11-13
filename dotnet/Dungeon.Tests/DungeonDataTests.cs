using FluentAssertions;
using PigeonPea.Dungeon.Core;
using Xunit;

namespace PigeonPea.Dungeon.Tests;

public class DungeonDataTests
{
    [Fact]
    public void InBounds_WorksForCornersAndOutside()
    {
        var d = new DungeonData(2, 2);
        d.InBounds(0,0).Should().BeTrue();
        d.InBounds(1,1).Should().BeTrue();
        d.InBounds(-1,0).Should().BeFalse();
        d.InBounds(2,1).Should().BeFalse();
    }

    [Fact]
    public void Walkable_SetAndGet_Works()
    {
        var d = new DungeonData(2, 1);
        d.SetWalkable(0,0,true);
        d.IsWalkable(0,0).Should().BeTrue();
        d.IsWalkable(1,0).Should().BeFalse();
        d.SetWalkable(1,0,true);
        d.IsWalkable(1,0).Should().BeTrue();
    }
}
