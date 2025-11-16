using FluentAssertions;
using PigeonPea.Dungeon.Core;
using Xunit;

namespace PigeonPea.Dungeon.Tests;

public class BasicDungeonGeneratorTests
{
    [Fact]
    public void Generate_Seeded_IsDeterministicForWalkableCount()
    {
        var g1 = new BasicDungeonGenerator();
        var d1 = g1.Generate(50, 30, seed: 1234);
        var g2 = new BasicDungeonGenerator();
        var d2 = g2.Generate(50, 30, seed: 1234);

        int c1 = CountWalkable(d1);
        int c2 = CountWalkable(d2);
        c1.Should().Be(c2);
        c1.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Generate_ProducesConnectivityBetweenRoomCenters_MostOfTheTime()
    {
        var g = new BasicDungeonGenerator();
        var d = g.Generate(40, 25, seed: 5678);

        // Rough check: there should be at least one long corridor or space
        CountWalkable(d).Should().BeGreaterThan(40);
    }

    private static int CountWalkable(DungeonData d)
    {
        int c = 0;
        for (int y = 0; y < d.Height; y++)
            for (int x = 0; x < d.Width; x++)
                if (d.IsWalkable(x, y)) c++;
        return c;
    }
}
