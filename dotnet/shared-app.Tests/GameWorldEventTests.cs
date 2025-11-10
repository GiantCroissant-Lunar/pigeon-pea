using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Events;
using PigeonPea.Shared.Tests.Mocks;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Shared.Tests;

/// <summary>
/// Tests for GameWorld event publishing via MessagePipe.
/// </summary>
public class GameWorldEventTests
{
    [Fact]
    public void ResolveMeleeAttack_PlayerDamaged_PublishesPlayerDamagedEvent()
    {
        // Arrange
        var playerDamagedPublisher = new MockPublisher<PlayerDamagedEvent>();
        var gameWorld = new GameWorld(
            width: 80,
            height: 50,
            playerDamagedPublisher: playerDamagedPublisher);

        // Create an enemy entity manually
        var enemyEntity = gameWorld.EcsWorld.Create(
            new Position(new Point(5, 5)),
            new Health { Current = 20, Maximum = 20 },
            new CombatStats(attack: 10, defense: 1),
            new AIComponent(AIBehavior.Aggressive)
        );

        // Act - Enemy attacks player (TryMovePlayer simulates this by calling ResolveMeleeAttack)
        // We'll use reflection to call ResolveMeleeAttack directly
        var method = typeof(GameWorld).GetMethod("ResolveMeleeAttack",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(gameWorld, new object[] { enemyEntity, gameWorld.PlayerEntity });

        // Assert
        Assert.Single(playerDamagedPublisher.PublishedEvents);
        var evt = playerDamagedPublisher.GetLastPublishedEvent();
        Assert.True(evt.Damage > 0);
        Assert.True(evt.RemainingHealth < 100); // Player starts with 100 health
        Assert.Equal("Enemy", evt.Source);
    }

    [Fact]
    public void ResolveMeleeAttack_EnemyDefeated_PublishesEnemyDefeatedEvent()
    {
        // Arrange
        var enemyDefeatedPublisher = new MockPublisher<EnemyDefeatedEvent>();
        var gameWorld = new GameWorld(
            width: 80,
            height: 50,
            enemyDefeatedPublisher: enemyDefeatedPublisher);

        // Create a weak enemy entity that can be killed in one hit
        var weakEnemyEntity = gameWorld.EcsWorld.Create(
            new Position(new Point(5, 5)),
            new Health { Current = 1, Maximum = 1 },
            new CombatStats(attack: 1, defense: 0),
            new AIComponent(AIBehavior.Aggressive),
            new ExperienceValue(xp: 50)
        );

        // Act - Player attacks weak enemy
        var method = typeof(GameWorld).GetMethod("ResolveMeleeAttack",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(gameWorld, new object[] { gameWorld.PlayerEntity, weakEnemyEntity });

        // Assert
        Assert.Single(enemyDefeatedPublisher.PublishedEvents);
        var evt = enemyDefeatedPublisher.GetLastPublishedEvent();
        Assert.Equal("Enemy", evt.EnemyName);
        Assert.Equal(50, evt.ExperienceGained);
    }

    [Fact]
    public void TryPickupItem_ItemPickedUp_PublishesItemPickedUpEvent()
    {
        // Arrange
        var itemPickedUpPublisher = new MockPublisher<ItemPickedUpEvent>();
        var gameWorld = new GameWorld(
            width: 80,
            height: 50,
            itemPickedUpPublisher: itemPickedUpPublisher);

        // Get player position
        var playerPos = gameWorld.PlayerEntity.Get<Position>();

        // Create an item at player's position
        var itemEntity = gameWorld.EcsWorld.Create(
            new Position(playerPos.Point),
            new Renderable('!', Color.Red),
            new Item("Test Potion", ItemType.Consumable),
            new Consumable(healthRestore: 25),
            new Pickup()
        );

        // Act
        var result = gameWorld.TryPickupItem();

        // Assert
        Assert.True(result);
        Assert.Single(itemPickedUpPublisher.PublishedEvents);
        var evt = itemPickedUpPublisher.GetLastPublishedEvent();
        Assert.Equal("Test Potion", evt.ItemName);
        Assert.Equal("Consumable", evt.ItemType);
    }

    [Fact]
    public void TryUseItem_ConsumableUsed_PublishesItemUsedEvent()
    {
        // Arrange
        var itemUsedPublisher = new MockPublisher<ItemUsedEvent>();
        var gameWorld = new GameWorld(
            width: 80,
            height: 50,
            itemUsedPublisher: itemUsedPublisher);

        // Create and add item to player's inventory
        var itemEntity = gameWorld.EcsWorld.Create(
            new Item("Health Potion", ItemType.Consumable),
            new Consumable(healthRestore: 25)
        );

        ref var inventory = ref gameWorld.PlayerEntity.Get<Inventory>();
        inventory.Items.Add(itemEntity);

        // Act
        var result = gameWorld.TryUseItem(0);

        // Assert
        Assert.True(result);
        Assert.Single(itemUsedPublisher.PublishedEvents);
        var evt = itemUsedPublisher.GetLastPublishedEvent();
        Assert.Equal("Health Potion", evt.ItemName);
        Assert.Equal("Consumable", evt.ItemType);
    }

    [Fact]
    public void TryDropItem_ItemDropped_PublishesItemDroppedEvent()
    {
        // Arrange
        var itemDroppedPublisher = new MockPublisher<ItemDroppedEvent>();
        var gameWorld = new GameWorld(
            width: 80,
            height: 50,
            itemDroppedPublisher: itemDroppedPublisher);

        // Create and add item to player's inventory
        var itemEntity = gameWorld.EcsWorld.Create(
            new Item("Old Sword", ItemType.Equipment),
            new Renderable('/', Color.Gray)
        );

        ref var inventory = ref gameWorld.PlayerEntity.Get<Inventory>();
        inventory.Items.Add(itemEntity);

        // Act
        var result = gameWorld.TryDropItem(0);

        // Assert
        Assert.True(result);
        Assert.Single(itemDroppedPublisher.PublishedEvents);
        var evt = itemDroppedPublisher.GetLastPublishedEvent();
        Assert.Equal("Old Sword", evt.ItemName);
    }

    [Fact]
    public void GainExperience_PlayerLevelsUp_PublishesPlayerLevelUpEvent()
    {
        // Arrange
        var playerLevelUpPublisher = new MockPublisher<PlayerLevelUpEvent>();
        var gameWorld = new GameWorld(
            width: 80,
            height: 50,
            playerLevelUpPublisher: playerLevelUpPublisher);

        // Get initial player level
        var initialLevel = gameWorld.PlayerEntity.Get<Experience>().Level;

        // Act - Give player enough XP to level up (level 1 -> 2 requires 283 XP based on formula: 100 * level^1.5)
        gameWorld.GainExperience(gameWorld.PlayerEntity, 283);

        // Assert
        Assert.Single(playerLevelUpPublisher.PublishedEvents);
        var evt = playerLevelUpPublisher.GetLastPublishedEvent();
        Assert.Equal(initialLevel + 1, evt.NewLevel);
        Assert.Equal(10, evt.HealthIncrease);
    }

    [Fact]
    public void GameWorld_WithoutPublishers_DoesNotThrow()
    {
        // Arrange & Act - Create GameWorld without any publishers
        var gameWorld = new GameWorld(width: 80, height: 50);

        // Get player position
        var playerPos = gameWorld.PlayerEntity.Get<Position>();

        // Create an item at player's position
        var itemEntity = gameWorld.EcsWorld.Create(
            new Position(playerPos.Point),
            new Renderable('!', Color.Red),
            new Item("Test Item", ItemType.Consumable),
            new Consumable(healthRestore: 25),
            new Pickup()
        );

        // Assert - Should not throw when publishers are null
        var result = gameWorld.TryPickupItem();
        Assert.True(result);
    }

    [Fact]
    public void ResolveMeleeAttack_NonPlayerDamaged_DoesNotPublishPlayerDamagedEvent()
    {
        // Arrange
        var playerDamagedPublisher = new MockPublisher<PlayerDamagedEvent>();
        var gameWorld = new GameWorld(
            width: 80,
            height: 50,
            playerDamagedPublisher: playerDamagedPublisher);

        // Create two enemy entities
        var enemy1 = gameWorld.EcsWorld.Create(
            new Position(new Point(5, 5)),
            new Health { Current = 20, Maximum = 20 },
            new CombatStats(attack: 10, defense: 1),
            new AIComponent(AIBehavior.Aggressive)
        );

        var enemy2 = gameWorld.EcsWorld.Create(
            new Position(new Point(6, 6)),
            new Health { Current = 20, Maximum = 20 },
            new CombatStats(attack: 10, defense: 1),
            new AIComponent(AIBehavior.Aggressive)
        );

        // Act - Enemy attacks another enemy
        var method = typeof(GameWorld).GetMethod("ResolveMeleeAttack",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(gameWorld, new object[] { enemy1, enemy2 });

        // Assert - No event should be published when non-player is damaged
        Assert.Empty(playerDamagedPublisher.PublishedEvents);
    }
}
