using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using PigeonPea.Perception.Models;
using Xunit;

namespace PigeonPea.Perception.Core.Tests.Visual;

public class VisualPerceptionDataTests
{
    [Fact]
    public void GetEntitiesOfType_FiltersCorrectly()
    {
        var visual = new VisualPerceptionData
        {
            VisibleEntities = new List<PerceivedEntity>
            {
                new() { EntityId = 1, EntityType = "Enemy", Position = (5, 5) },
                new() { EntityId = 2, EntityType = "Item", Position = (6, 6) },
                new() { EntityId = 3, EntityType = "Enemy", Position = (7, 7) }
            }
        };

        var enemies = visual.GetEntitiesOfType("Enemy").ToList();

        enemies.Should().HaveCount(2);
        enemies.All(e => e.EntityType == "Enemy").Should().BeTrue();
    }

    [Fact]
    public void GetClosestEntity_ReturnsNearestEntity()
    {
        var visual = new VisualPerceptionData
        {
            VisibleEntities = new List<PerceivedEntity>
            {
                new() { EntityId = 1, EntityType = "Enemy", Distance = 5.0f },
                new() { EntityId = 2, EntityType = "Enemy", Distance = 3.0f },
                new() { EntityId = 3, EntityType = "Enemy", Distance = 7.0f }
            }
        };

        var closest = visual.GetClosestEntity("Enemy");

        closest.Should().NotBeNull();
        closest!.EntityId.Should().Be(2);
        closest.Distance.Should().Be(3.0f);
    }

    [Fact]
    public void IsPositionVisible_ChecksCorrectly()
    {
        var visual = new VisualPerceptionData
        {
            VisibleTiles = new HashSet<(int X, int Y)>
            {
                (1, 1), (2, 2)
            }
        };

        visual.IsPositionVisible((1, 1)).Should().BeTrue();
        visual.IsPositionVisible((9, 9)).Should().BeFalse();
    }
}
