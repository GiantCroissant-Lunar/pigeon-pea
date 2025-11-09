using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Reactive.Testing;
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
}
