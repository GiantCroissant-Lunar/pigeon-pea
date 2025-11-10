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
    /// <summary>
    /// Creates a GameWorld with the specified publisher and mock publishers for all other events.
    /// </summary>
    private GameWorld CreateGameWorldWithPublisher<T>(MockPublisher<T> publisher)
    {
        var playerDamagedPublisher = publisher as MockPublisher<PlayerDamagedEvent> ?? new MockPublisher<PlayerDamagedEvent>();
        var enemyDefeatedPublisher = publisher as MockPublisher<EnemyDefeatedEvent> ?? new MockPublisher<EnemyDefeatedEvent>();
        var itemPickedUpPublisher = publisher as MockPublisher<ItemPickedUpEvent> ?? new MockPublisher<ItemPickedUpEvent>();
        var itemUsedPublisher = publisher as MockPublisher<ItemUsedEvent> ?? new MockPublisher<ItemUsedEvent>();
        var itemDroppedPublisher = publisher as MockPublisher<ItemDroppedEvent> ?? new MockPublisher<ItemDroppedEvent>();
        var playerLevelUpPublisher = publisher as MockPublisher<PlayerLevelUpEvent> ?? new MockPublisher<PlayerLevelUpEvent>();
        var doorOpenedPublisher = publisher as MockPublisher<DoorOpenedEvent> ?? new MockPublisher<DoorOpenedEvent>();
        var stairsDescendedPublisher = publisher as MockPublisher<StairsDescendedEvent> ?? new MockPublisher<StairsDescendedEvent>();

        return new GameWorld(
            width: 80,
            height: 50,
            playerDamagedPublisher: playerDamagedPublisher,
            enemyDefeatedPublisher: enemyDefeatedPublisher,
            itemPickedUpPublisher: itemPickedUpPublisher,
            itemUsedPublisher: itemUsedPublisher,
            itemDroppedPublisher: itemDroppedPublisher,
            playerLevelUpPublisher: playerLevelUpPublisher,
            doorOpenedPublisher: doorOpenedPublisher,
            stairsDescendedPublisher: stairsDescendedPublisher);
    }

    [Fact]
    public void EnemyAttacksPlayer_PlayerDamaged_PublishesPlayerDamagedEvent()
    {
        // Arrange
        var playerDamagedPublisher = new MockPublisher<PlayerDamagedEvent>();
        var gameWorld = CreateGameWorldWithPublisher(playerDamagedPublisher);

        // Create an enemy entity
        var enemyEntity = gameWorld.EcsWorld.Create(
            new Position(new Point(5, 5)),
            new Health { Current = 20, Maximum = 20 },
            new CombatStats(attack: 10, defense: 1),
            new AIComponent(AIBehavior.Aggressive)
        );

        // Act - Enemy attacks player using public test helper method
        gameWorld.TestResolveMeleeAttack(enemyEntity, gameWorld.PlayerEntity);

        // Assert
        Assert.Single(playerDamagedPublisher.PublishedEvents);
        var evt = playerDamagedPublisher.GetLastPublishedEvent();
        Assert.True(evt.Damage > 0);
        Assert.True(evt.RemainingHealth < 100); // Player starts with 100 health
        Assert.Equal("Enemy", evt.Source);
    }

    [Fact]
    public void PlayerAttack_EnemyDefeated_PublishesEnemyDefeatedEvent()
    {
        // Arrange
        var enemyDefeatedPublisher = new MockPublisher<EnemyDefeatedEvent>();
        var gameWorld = CreateGameWorldWithPublisher(enemyDefeatedPublisher);

        // Get player position
        var playerPos = gameWorld.PlayerEntity.Get<Position>();

        // Find a position next to the player for the enemy
        var enemyPos = new Point(playerPos.Point.X + 1, playerPos.Point.Y);
        
        // Ensure the enemy position is walkable (enemies spawn on walkable tiles)
        if (enemyPos.X < gameWorld.Width && enemyPos.Y < gameWorld.Height)
        {
            gameWorld.WalkabilityMap[enemyPos.X, enemyPos.Y] = true;
        }

        // Remove any existing floor/wall tiles at the enemy position so GetEntityAt can find the enemy
        // (GetEntityAt returns the first entity found, which might be a floor tile)
        var posQuery = new QueryDescription().WithAll<Position, Components.Tile>();
        gameWorld.EcsWorld.Query(in posQuery, (Entity entity, ref Position pos, ref Components.Tile tile) =>
        {
            if (pos.Point == enemyPos)
            {
                gameWorld.EcsWorld.Destroy(entity);
            }
        });

        // Create a weak enemy entity next to the player that can be killed in one hit
        var weakEnemyEntity = gameWorld.EcsWorld.Create(
            new Position(enemyPos),
            new Health { Current = 1, Maximum = 1 },
            new CombatStats(attack: 1, defense: 0),
            new AIComponent(AIBehavior.Aggressive),
            new ExperienceValue(xp: 50),
            new Renderable('E', SadRogue.Primitives.Color.Red)
        );

        // Act - Player moves into enemy position to attack
        var direction = new Point(1, 0); // Move right into enemy
        gameWorld.TryMovePlayer(direction);

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
        var gameWorld = CreateGameWorldWithPublisher(itemPickedUpPublisher);

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
        var gameWorld = CreateGameWorldWithPublisher(itemUsedPublisher);

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
        var gameWorld = CreateGameWorldWithPublisher(itemDroppedPublisher);

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
        var gameWorld = CreateGameWorldWithPublisher(playerLevelUpPublisher);

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
        var gameWorld = CreateGameWorldWithPublisher(playerDamagedPublisher);

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
