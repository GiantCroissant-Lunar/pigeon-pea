using Xunit;
using FluentAssertions;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control.WorldManager;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

namespace PigeonPea.Dungeon.Tests;

public class DungeonWorldManagerTests
{
    [Fact]
    public void CreatePlayer_CreatesEntityWithCorrectComponents()
    {
        // Arrange
        var dungeon = new DungeonData(20, 20);
        dungeon.SetFloor(5, 5);

        using var manager = new DungeonWorldManager(dungeon);

        // Act
        var player = manager.CreatePlayer(5, 5, "TestPlayer");

        // Assert
        manager.PlayerEntity.Should().NotBeNull();
        manager.World.IsAlive(player).Should().BeTrue();
        manager.World.Has<Position>(player).Should().BeTrue();
        manager.World.Has<Sprite>(player).Should().BeTrue();
        manager.World.Has<Health>(player).Should().BeTrue();
        manager.World.Has<Name>(player).Should().BeTrue();
        manager.World.Has<PlayerTag>(player).Should().BeTrue();

        var pos = manager.World.Get<Position>(player);
        pos.X.Should().Be(5);
        pos.Y.Should().Be(5);

        var name = manager.World.Get<Name>(player);
        name.Value.Should().Be("TestPlayer");

        var health = manager.World.Get<Health>(player);
        health.Current.Should().Be(100);
        health.Maximum.Should().Be(100);
    }

    [Fact]
    public void CreatePlayer_ThrowsOnInvalidPosition()
    {
        // Arrange
        var dungeon = new DungeonData(20, 20);
        // Don't set position (5, 5) as walkable

        using var manager = new DungeonWorldManager(dungeon);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => manager.CreatePlayer(5, 5, "TestPlayer"));
    }

    [Fact]
    public void SpawnMonster_CreatesEntityWithCorrectComponents()
    {
        // Arrange
        var dungeon = new DungeonData(20, 20);
        dungeon.SetFloor(10, 10);

        using var manager = new DungeonWorldManager(dungeon);

        // Act
        var monster = manager.SpawnMonster(10, 10, "goblin", 25);

        // Assert
        manager.World.IsAlive(monster).Should().BeTrue();
        manager.World.Has<Position>(monster).Should().BeTrue();
        manager.World.Has<Sprite>(monster).Should().BeTrue();
        manager.World.Has<Health>(monster).Should().BeTrue();
        manager.World.Has<Name>(monster).Should().BeTrue();
        manager.World.Has<MonsterTag>(monster).Should().BeTrue();

        var pos = manager.World.Get<Position>(monster);
        pos.X.Should().Be(10);
        pos.Y.Should().Be(10);

        var name = manager.World.Get<Name>(monster);
        name.Value.Should().Be("goblin");

        var health = manager.World.Get<Health>(monster);
        health.Current.Should().Be(25);
        health.Maximum.Should().Be(25);

        var sprite = manager.World.Get<Sprite>(monster);
        sprite.AsciiChar.Should().Be('g');
    }

    [Fact]
    public void SpawnItem_CreatesEntityWithCorrectComponents()
    {
        // Arrange
        var dungeon = new DungeonData(20, 20);
        dungeon.SetFloor(15, 15);

        using var manager = new DungeonWorldManager(dungeon);

        // Act
        var item = manager.SpawnItem(15, 15, "potion");

        // Assert
        manager.World.IsAlive(item).Should().BeTrue();
        manager.World.Has<Position>(item).Should().BeTrue();
        manager.World.Has<Sprite>(item).Should().BeTrue();
        manager.World.Has<Name>(item).Should().BeTrue();
        manager.World.Has<ItemTag>(item).Should().BeTrue();

        var pos = manager.World.Get<Position>(item);
        pos.X.Should().Be(15);
        pos.Y.Should().Be(15);

        var name = manager.World.Get<Name>(item);
        name.Value.Should().Be("potion");

        var sprite = manager.World.Get<Sprite>(item);
        sprite.AsciiChar.Should().Be('!');
    }

    [Fact]
    public void IsPositionOccupied_ReturnsTrueWhenOccupied()
    {
        // Arrange
        var dungeon = new DungeonData(20, 20);
        dungeon.SetFloor(5, 5);
        dungeon.SetFloor(6, 6);

        using var manager = new DungeonWorldManager(dungeon);
        manager.CreatePlayer(5, 5);

        // Act & Assert
        manager.IsPositionOccupied(5, 5).Should().BeTrue();
        manager.IsPositionOccupied(6, 6).Should().BeFalse();
    }

    [Fact]
    public void TryMoveEntity_MovesPlayerSuccessfully()
    {
        // Arrange
        var dungeon = new DungeonData(20, 20);
        dungeon.SetFloor(5, 5);
        dungeon.SetFloor(6, 5);

        using var manager = new DungeonWorldManager(dungeon);
        var player = manager.CreatePlayer(5, 5);

        // Act
        bool moved = manager.TryMoveEntity(player, 6, 5);

        // Assert
        moved.Should().BeTrue();
        var pos = manager.World.Get<Position>(player);
        pos.X.Should().Be(6);
        pos.Y.Should().Be(5);
    }

    [Fact]
    public void TryMoveEntity_FailsOnInvalidPosition()
    {
        // Arrange
        var dungeon = new DungeonData(20, 20);
        dungeon.SetFloor(5, 5);
        // Don't set (6, 5) as walkable

        using var manager = new DungeonWorldManager(dungeon);
        var player = manager.CreatePlayer(5, 5);

        // Act
        bool moved = manager.TryMoveEntity(player, 6, 5);

        // Assert
        moved.Should().BeFalse();
        var pos = manager.World.Get<Position>(player);
        pos.X.Should().Be(5); // Should remain at original position
        pos.Y.Should().Be(5);
    }

    [Fact]
    public void SpawnRandomMonsters_CreatesCorrectCount()
    {
        // Arrange
        var dungeon = new DungeonData(50, 50);
        // Set many positions as walkable
        for (int y = 1; y < 49; y++)
        {
            for (int x = 1; x < 49; x++)
            {
                dungeon.SetFloor(x, y);
            }
        }

        using var manager = new DungeonWorldManager(dungeon);
        var rng = new Random(42);

        // Act
        manager.SpawnRandomMonsters(5, rng);

        // Assert
        var query = new Arch.Core.QueryDescription().WithAll<MonsterTag>();
        int monsterCount = 0;
        manager.World.Query(in query, (Arch.Core.Entity entity) => monsterCount++);
        
        monsterCount.Should().Be(5);
    }

    [Fact]
    public void CleanupDeadMonsters_RemovesDeadEntities()
    {
        // Arrange
        var dungeon = new DungeonData(20, 20);
        dungeon.SetFloor(5, 5);
        dungeon.SetFloor(6, 6);

        using var manager = new DungeonWorldManager(dungeon);
        var monster1 = manager.SpawnMonster(5, 5, "goblin", 10);
        var monster2 = manager.SpawnMonster(6, 6, "orc", 20);

        // Kill monster1
        var health = manager.World.Get<Health>(monster1);
        health.Current = 0;
        manager.World.Set(monster1, health);

        // Act
        manager.CleanupDeadMonsters();

        // Assert
        manager.World.IsAlive(monster1).Should().BeFalse();
        manager.World.IsAlive(monster2).Should().BeTrue();
    }
}
