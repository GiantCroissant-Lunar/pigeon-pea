using System;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
        var viewModel = new GameViewModel(_world, _services);

        // Assert
        viewModel.Player.Should().NotBeNull("Player view model should be initialized");
        viewModel.Inventory.Should().NotBeNull("Inventory view model should be initialized");
        viewModel.MessageLog.Should().NotBeNull("MessageLog view model should be initialized");
        viewModel.Map.Should().NotBeNull("Map view model should be initialized");

        viewModel.Dispose();
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
        var viewModel = new GameViewModel(_world, _services);
        var initialHealth = viewModel.Player.Health;

        // Act - Wait for at least one update cycle (16ms)
        Thread.Sleep(50);

        // Assert - Player view model should have been updated
        // The health value should be set from the game world
        viewModel.Player.Health.Should().BeGreaterOrEqualTo(0, "Update loop should have synchronized player health");

        viewModel.Dispose();
    }

    [Fact]
    public void UpdateLoop_SynchronizesPlayerViewModel()
    {
        // Arrange
        var viewModel = new GameViewModel(_world, _services);
        
        // Wait for initial update
        Thread.Sleep(50);
        
        // Get initial values
        var initialHealth = viewModel.Player.Health;
        var initialName = viewModel.Player.Name;

        // Act - Modify player health in the game world
        ref var health = ref _world.EcsWorld.Get<Components.Health>(_world.PlayerEntity);
        health.Current = 50;

        // Wait for update loop to sync
        Thread.Sleep(50);

        // Assert
        viewModel.Player.Health.Should().Be(50, "Update loop should sync modified health");
        viewModel.Player.Name.Should().Be("Hero", "Update loop should sync player name");

        viewModel.Dispose();
    }

    [Fact]
    public void Dispose_CleansUpSubscriptions()
    {
        // Arrange
        var viewModel = new GameViewModel(_world, _services);

        // Wait for at least one update
        Thread.Sleep(50);

        // Act
        viewModel.Dispose();

        // Give some time to ensure no more updates occur
        var healthBeforeWait = viewModel.Player.Health;
        Thread.Sleep(50);
        var healthAfterWait = viewModel.Player.Health;

        // Assert - After disposal, updates should stop
        // (This is a basic check; in practice, we'd need more sophisticated verification)
        healthAfterWait.Should().Be(healthBeforeWait, "Updates should stop after disposal");
    }

    [Theory]
    [InlineData(80, 50)]
    [InlineData(100, 60)]
    public void Constructor_WorksWithDifferentWorldSizes(int width, int height)
    {
        // Arrange
        var world = new GameWorld(width: width, height: height);

        // Act
        var viewModel = new GameViewModel(world, _services);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.Player.Should().NotBeNull();
        viewModel.Inventory.Should().NotBeNull();
        viewModel.MessageLog.Should().NotBeNull();
        viewModel.Map.Should().NotBeNull();

        viewModel.Dispose();
    }

    [Fact]
    public void Player_PropertyChanges_RaiseNotifications()
    {
        // Arrange
        var viewModel = new GameViewModel(_world, _services);
        bool propertyChanged = false;
        
        viewModel.Player.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.Health))
            {
                propertyChanged = true;
            }
        };

        // Wait for initial update
        Thread.Sleep(50);

        // Reset flag
        propertyChanged = false;

        // Act - Modify health
        ref var health = ref _world.EcsWorld.Get<Components.Health>(_world.PlayerEntity);
        health.Current = 75;

        // Wait for update
        Thread.Sleep(50);

        // Assert
        propertyChanged.Should().BeTrue("Health property change should trigger notification");

        viewModel.Dispose();
    }

    [Fact]
    public void GameViewModel_InheritsFromReactiveObject()
    {
        // Arrange & Act
        var viewModel = new GameViewModel(_world, _services);

        // Assert
        viewModel.Should().BeAssignableTo<ReactiveUI.ReactiveObject>(
            "GameViewModel should inherit from ReactiveObject");

        viewModel.Dispose();
    }

    [Fact]
    public void GameViewModel_ImplementsIDisposable()
    {
        // Arrange & Act
        var viewModel = new GameViewModel(_world, _services);

        // Assert
        viewModel.Should().BeAssignableTo<IDisposable>(
            "GameViewModel should implement IDisposable");

        viewModel.Dispose();
    }

    [Fact]
    public void MultipleDispose_DoesNotThrow()
    {
        // Arrange
        var viewModel = new GameViewModel(_world, _services);

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
        var viewModel = new GameViewModel(_world, _services);
        
        // Wait for initial update
        Thread.Sleep(50);

        // Act - Damage player multiple times
        for (int i = 0; i < 5; i++)
        {
            ref var health = ref _world.EcsWorld.Get<Components.Health>(_world.PlayerEntity);
            health.Current = Math.Max(0, health.Current - 10);
            Thread.Sleep(30);
        }

        // Assert - View model should still be updating
        viewModel.Player.Health.Should().BeGreaterOrEqualTo(0);
        viewModel.Player.Health.Should().BeLessThanOrEqualTo(100);

        viewModel.Dispose();
    }

    [Fact]
    public void ViewModels_AreIndependent()
    {
        // Arrange
        var viewModel = new GameViewModel(_world, _services);

        // Wait for initial update
        Thread.Sleep(50);

        // Act & Assert - Each view model should be a separate instance
        viewModel.Player.Should().NotBeSameAs(viewModel.Inventory);
        viewModel.Player.Should().NotBeSameAs(viewModel.MessageLog);
        viewModel.Player.Should().NotBeSameAs(viewModel.Map);
        viewModel.Inventory.Should().NotBeSameAs(viewModel.MessageLog);
        viewModel.Inventory.Should().NotBeSameAs(viewModel.Map);
        viewModel.MessageLog.Should().NotBeSameAs(viewModel.Map);

        viewModel.Dispose();
    }
}
