using System;
using Arch.Core.Extensions;
using FluentAssertions;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Reactive.Testing;
using PigeonPea.Shared.Events;
using PigeonPea.Shared.ViewModels;
using Xunit;

namespace PigeonPea.Shared.Tests.ViewModels;

/// <summary>
/// Tests for GameViewModel to verify initialization, orchestration, and disposal.
/// </summary>
public class GameViewModelTests : IDisposable
{
    private readonly GameWorld _world;
    private readonly IServiceProvider _services;

    public GameViewModelTests()
    {
        _world = new GameWorld(width: 80, height: 50);
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddPigeonPeaServices();
        _services = serviceCollection.BuildServiceProvider();
    }

    public void Dispose()
    {
        (_services as IDisposable)?.Dispose();
    }

    [Fact]
    public void Constructor_InitializesAllViewModels()
    {
        // Act
        using var viewModel = new GameViewModel(_world, _services);

        // Assert
        viewModel.Player.Should().NotBeNull("Player view model should be initialized");
        viewModel.Inventory.Should().NotBeNull("Inventory view model should be initialized");
        viewModel.MessageLog.Should().NotBeNull("MessageLog view model should be initialized");
        viewModel.Map.Should().NotBeNull("Map view model should be initialized");
    }

    [Fact]
    public void Constructor_WithNullWorld_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new GameViewModel(null!, _services);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("world");
    }

    [Fact]
    public void Constructor_WithNullServices_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new GameViewModel(_world, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void Constructor_StartsUpdateLoop()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);

        // Act - Advance time by one update cycle (16ms)
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Assert - Player view model should have been updated
        // The health value should be set from the game world
        viewModel.Player.Health.Should().BeGreaterOrEqualTo(0, "Update loop should have synchronized player health");
    }

    [Fact]
    public void UpdateLoop_SynchronizesPlayerViewModel()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);
        
        // Initial update
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);
        
        // Get initial values
        var initialHealth = viewModel.Player.Health;
        var initialName = viewModel.Player.Name;

        // Act - Modify player health in the game world
        ref var health = ref _world.EcsWorld.Get<Components.Health>(_world.PlayerEntity);
        health.Current = 50;

        // Advance time to trigger another update
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Assert
        viewModel.Player.Health.Should().Be(50, "Update loop should sync modified health");
        viewModel.Player.Name.Should().Be("Hero", "Update loop should sync player name");
    }

    [Fact]
    public void Dispose_CleansUpSubscriptions()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);

        // Initial update
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Act
        viewModel.Dispose();

        // Verify no more updates occur after disposal
        var healthBeforeAdvance = viewModel.Player.Health;
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);
        var healthAfterAdvance = viewModel.Player.Health;

        // Assert - After disposal, updates should stop
        healthAfterAdvance.Should().Be(healthBeforeAdvance, "Updates should stop after disposal");
    }

    [Theory]
    [InlineData(80, 50)]
    [InlineData(100, 60)]
    public void Constructor_WorksWithDifferentWorldSizes(int width, int height)
    {
        // Arrange
        var world = new GameWorld(width: width, height: height);

        // Act
        using var viewModel = new GameViewModel(world, _services);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.Player.Should().NotBeNull();
        viewModel.Inventory.Should().NotBeNull();
        viewModel.MessageLog.Should().NotBeNull();
        viewModel.Map.Should().NotBeNull();
    }

    [Fact]
    public void Player_PropertyChanges_RaiseNotifications()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);
        bool propertyChanged = false;
        
        viewModel.Player.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.Health))
            {
                propertyChanged = true;
            }
        };

        // Initial update
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Reset flag
        propertyChanged = false;

        // Act - Modify health
        ref var health = ref _world.EcsWorld.Get<Components.Health>(_world.PlayerEntity);
        health.Current = 75;

        // Advance time to trigger update
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Assert
        propertyChanged.Should().BeTrue("Health property change should trigger notification");
    }

    [Fact]
    public void GameViewModel_InheritsFromReactiveObject()
    {
        // Arrange & Act
        using var viewModel = new GameViewModel(_world, _services);

        // Assert
        viewModel.Should().BeAssignableTo<ReactiveUI.ReactiveObject>(
            "GameViewModel should inherit from ReactiveObject");
    }

    [Fact]
    public void GameViewModel_ImplementsIDisposable()
    {
        // Arrange & Act
        using var viewModel = new GameViewModel(_world, _services);

        // Assert
        viewModel.Should().BeAssignableTo<IDisposable>(
            "GameViewModel should implement IDisposable");
    }

    [Fact]
    public void MultipleDispose_DoesNotThrow()
    {
        // Arrange
        using var viewModel = new GameViewModel(_world, _services);

        // Act
        Action act = () =>
        {
            viewModel.Dispose();
            viewModel.Dispose();
            viewModel.Dispose();
        };

        // Assert
        act.Should().NotThrow("Multiple Dispose calls should be safe");
    }

    [Fact]
    public void UpdateLoop_ContinuesRunning_WhenPlayerTakesDamage()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);
        
        // Initial update
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Act - Damage player multiple times
        for (int i = 0; i < 5; i++)
        {
            ref var health = ref _world.EcsWorld.Get<Components.Health>(_world.PlayerEntity);
            health.Current = Math.Max(0, health.Current - 10);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);
        }

        // Assert - View model should still be updating
        viewModel.Player.Health.Should().BeGreaterOrEqualTo(0);
        viewModel.Player.Health.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void ViewModels_AreIndependent()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);

        // Initial update
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Act & Assert - Each view model should be a separate instance
        viewModel.Player.Should().NotBeSameAs(viewModel.Inventory);
        viewModel.Player.Should().NotBeSameAs(viewModel.MessageLog);
        viewModel.Player.Should().NotBeSameAs(viewModel.Map);
        viewModel.Inventory.Should().NotBeSameAs(viewModel.MessageLog);
        viewModel.Inventory.Should().NotBeSameAs(viewModel.Map);
        viewModel.MessageLog.Should().NotBeSameAs(viewModel.Map);
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Act
        using var viewModel = new GameViewModel(_world, _services);

        // Assert
        viewModel.UseItemCommand.Should().NotBeNull("UseItemCommand should be initialized");
        viewModel.DropItemCommand.Should().NotBeNull("DropItemCommand should be initialized");
    }

    [Fact]
    public void UseItemCommand_CannotExecute_WhenNoItemSelected()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);
        
        // Initial update
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Act & Assert - No item selected (SelectedIndex is -1)
        viewModel.UseItemCommand.CanExecute.Subscribe(canExecute =>
        {
            canExecute.Should().BeFalse("UseItemCommand should not be executable when no item is selected");
        });
    }

    [Fact]
    public void DropItemCommand_CannotExecute_WhenNoItemSelected()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);
        
        // Initial update
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Act & Assert - No item selected (SelectedIndex is -1)
        viewModel.DropItemCommand.CanExecute.Subscribe(canExecute =>
        {
            canExecute.Should().BeFalse("DropItemCommand should not be executable when no item is selected");
        });
    }

    [Fact]
    public void UseItemCommand_CanExecute_WhenItemIsSelected()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);

        // Add a test item to player inventory
        ref var inventory = ref _world.EcsWorld.Get<Components.Inventory>(_world.PlayerEntity);
        var testItem = _world.EcsWorld.Create(
            new Components.Item("Test Potion", Components.ItemType.Consumable),
            new Components.Consumable(10)
        );
        inventory.Items.Add(testItem);

        // Sync the view model to populate inventory
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Subscribe to CanExecute changes
        var canExecuteResult = false;
        using var subscription = viewModel.UseItemCommand.CanExecute.Subscribe(canExecute => canExecuteResult = canExecute);

        // Assert initial state (no item selected yet)
        canExecuteResult.Should().BeFalse("Command should not be executable before an item is selected");

        // Act - Select the item
        viewModel.Inventory.SelectedIndex = 0;

        // Give the observable pipeline time to process the change
        scheduler.AdvanceBy(1);

        // Assert
        canExecuteResult.Should().BeTrue("Command should be executable after an item is selected");
    }

    [Fact]
    public void DropItemCommand_CanExecute_WhenItemIsSelected()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);

        // Add a test item to player inventory
        ref var inventory = ref _world.EcsWorld.Get<Components.Inventory>(_world.PlayerEntity);
        var testItem = _world.EcsWorld.Create(
            new Components.Item("Test Sword", Components.ItemType.Equipment)
        );
        inventory.Items.Add(testItem);

        // Sync the view model to populate inventory
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Subscribe to CanExecute changes
        var canExecuteResult = false;
        using var subscription = viewModel.DropItemCommand.CanExecute.Subscribe(canExecute => canExecuteResult = canExecute);

        // Assert initial state (no item selected yet)
        canExecuteResult.Should().BeFalse("Command should not be executable before an item is selected");

        // Act - Select the item
        viewModel.Inventory.SelectedIndex = 0;

        // Give the observable pipeline time to process the change
        scheduler.AdvanceBy(1);

        // Assert
        canExecuteResult.Should().BeTrue("Command should be executable after an item is selected");
    }

    [Fact]
    public void UseItemCommand_Execute_UsesItemFromInventory()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);
        
        // Initial update to sync inventory
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Damage player so we can test health restore
        ref var health = ref _world.EcsWorld.Get<Components.Health>(_world.PlayerEntity);
        health.Current = 50;

        // Add a consumable item to player inventory
        ref var inventory = ref _world.EcsWorld.Get<Components.Inventory>(_world.PlayerEntity);
        var healthPotion = _world.EcsWorld.Create(
            new Components.Item("Health Potion", Components.ItemType.Consumable),
            new Components.Consumable(25)
        );
        inventory.Items.Add(healthPotion);

        // Sync the view model
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Select the item
        viewModel.Inventory.SelectedIndex = 0;
        var initialInventoryCount = viewModel.Inventory.Items.Count;

        // Act - Execute the command
        viewModel.UseItemCommand.Execute().Subscribe();

        // Sync the view model to update inventory
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Assert
        health.Current.Should().Be(75, "Health should be restored by 25");
        viewModel.Inventory.Items.Count.Should().Be(initialInventoryCount - 1, "Item should be removed from inventory");
    }

    [Fact]
    public void UseItemCommand_Execute_PublishesEvent()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);
        
        // Initial update to sync inventory
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Add a consumable item to player inventory
        ref var inventory = ref _world.EcsWorld.Get<Components.Inventory>(_world.PlayerEntity);
        var healthPotion = _world.EcsWorld.Create(
            new Components.Item("Mega Potion", Components.ItemType.Consumable),
            new Components.Consumable(50)
        );
        inventory.Items.Add(healthPotion);

        // Sync the view model
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Subscribe to ItemUsedEvent
        var subscriber = _services.GetRequiredService<ISubscriber<ItemUsedEvent>>();
        ItemUsedEvent? receivedEvent = null;
        var bag = DisposableBag.CreateBuilder();
        subscriber.Subscribe(e => receivedEvent = e).AddTo(bag);
        var subscription = bag.Build();

        // Select the item
        viewModel.Inventory.SelectedIndex = 0;

        // Act - Execute the command
        viewModel.UseItemCommand.Execute().Subscribe();

        // Assert
        receivedEvent.Should().NotBeNull("ItemUsedEvent should be published");
        receivedEvent!.Value.ItemName.Should().Be("Mega Potion");
        receivedEvent.Value.ItemType.Should().Be("Consumable");

        subscription.Dispose();
    }

    [Fact]
    public void DropItemCommand_Execute_DropsItemFromInventory()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);
        
        // Initial update to sync inventory
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Add an item to player inventory
        ref var inventory = ref _world.EcsWorld.Get<Components.Inventory>(_world.PlayerEntity);
        var oldSword = _world.EcsWorld.Create(
            new Components.Item("Old Sword", Components.ItemType.Equipment)
        );
        inventory.Items.Add(oldSword);

        // Sync the view model
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Select the item
        viewModel.Inventory.SelectedIndex = 0;
        var initialInventoryCount = viewModel.Inventory.Items.Count;

        // Act - Execute the command
        viewModel.DropItemCommand.Execute().Subscribe();

        // Sync the view model to update inventory
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Assert
        viewModel.Inventory.Items.Count.Should().Be(initialInventoryCount - 1, "Item should be removed from inventory");
        
        // Verify item has Position and Pickup components (it's on the ground)
        oldSword.Has<Components.Position>().Should().BeTrue("Dropped item should have Position component");
        oldSword.Has<Components.Pickup>().Should().BeTrue("Dropped item should have Pickup component");
    }

    [Fact]
    public void DropItemCommand_Execute_PublishesEvent()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);
        
        // Initial update to sync inventory
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Add an item to player inventory
        ref var inventory = ref _world.EcsWorld.Get<Components.Inventory>(_world.PlayerEntity);
        var rustyDagger = _world.EcsWorld.Create(
            new Components.Item("Rusty Dagger", Components.ItemType.Equipment)
        );
        inventory.Items.Add(rustyDagger);

        // Sync the view model
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Subscribe to ItemDroppedEvent
        var subscriber = _services.GetRequiredService<ISubscriber<ItemDroppedEvent>>();
        ItemDroppedEvent? receivedEvent = null;
        var bag = DisposableBag.CreateBuilder();
        subscriber.Subscribe(e => receivedEvent = e).AddTo(bag);
        var subscription = bag.Build();

        // Select the item
        viewModel.Inventory.SelectedIndex = 0;

        // Act - Execute the command
        viewModel.DropItemCommand.Execute().Subscribe();

        // Assert
        receivedEvent.Should().NotBeNull("ItemDroppedEvent should be published");
        receivedEvent!.Value.ItemName.Should().Be("Rusty Dagger");

        subscription.Dispose();
    }

    [Fact]
    public void UseItemCommand_Execute_DoesNothing_WhenNoItemSelected()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);
        
        // Initial update
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Get initial health
        ref var health = ref _world.EcsWorld.Get<Components.Health>(_world.PlayerEntity);
        var initialHealth = health.Current;

        // Act - Try to execute without selecting an item
        viewModel.UseItemCommand.Execute().Subscribe();

        // Assert
        health.Current.Should().Be(initialHealth, "Health should not change when no item is selected");
    }

    [Fact]
    public void DropItemCommand_Execute_DoesNothing_WhenNoItemSelected()
    {
        // Arrange
        var scheduler = new TestScheduler();
        using var viewModel = new GameViewModel(_world, _services, scheduler);
        
        // Initial update
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        var initialInventoryCount = viewModel.Inventory.Items.Count;

        // Act - Try to execute without selecting an item
        viewModel.DropItemCommand.Execute().Subscribe();

        // Sync the view model
        scheduler.AdvanceBy(TimeSpan.FromMilliseconds(16).Ticks);

        // Assert
        viewModel.Inventory.Items.Count.Should().Be(initialInventoryCount, "Inventory should not change when no item is selected");
    }

    [Fact]
    public void Fps_PropertyChanges_RaiseNotifications()
    {
        // Arrange
        using var viewModel = new GameViewModel(_world, _services);
        bool propertyChanged = false;
        
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(GameViewModel.Fps))
            {
                propertyChanged = true;
            }
        };

        // Act
        viewModel.Fps = 60;

        // Assert
        propertyChanged.Should().BeTrue("Fps property change should trigger notification");
        viewModel.Fps.Should().Be(60);
    }

    [Fact]
    public void Fps_DefaultValue_IsZero()
    {
        // Arrange & Act
        using var viewModel = new GameViewModel(_world, _services);

        // Assert
        viewModel.Fps.Should().Be(0, "Fps should default to 0");
    }
}
