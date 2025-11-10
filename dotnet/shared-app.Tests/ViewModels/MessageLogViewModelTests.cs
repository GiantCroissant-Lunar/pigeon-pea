using System;
using System.Linq;
using FluentAssertions;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Shared.Events;
using PigeonPea.Shared.ViewModels;
using Xunit;

namespace PigeonPea.Shared.Tests.ViewModels;

/// <summary>
/// Tests for MessageLogViewModel to verify event subscriptions and message management.
/// </summary>
public class MessageLogViewModelTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IPublisher<PlayerDamagedEvent> _combatPublisher;
    private readonly IPublisher<ItemPickedUpEvent> _inventoryPublisher;
    private readonly IPublisher<GameStateChangedEvent> _gameStatePublisher;
    private readonly MessageLogViewModel _viewModel;

    public MessageLogViewModelTests()
    {
        // Setup DI container with MessagePipe
        var services = new ServiceCollection();
        services.AddMessagePipe();
        _serviceProvider = services.BuildServiceProvider();

        // Get publishers and subscribers
        _combatPublisher = _serviceProvider.GetRequiredService<IPublisher<PlayerDamagedEvent>>();
        _inventoryPublisher = _serviceProvider.GetRequiredService<IPublisher<ItemPickedUpEvent>>();
        _gameStatePublisher = _serviceProvider.GetRequiredService<IPublisher<GameStateChangedEvent>>();

        var combatSubscriber = _serviceProvider.GetRequiredService<ISubscriber<PlayerDamagedEvent>>();
        var inventorySubscriber = _serviceProvider.GetRequiredService<ISubscriber<ItemPickedUpEvent>>();
        var gameStateSubscriber = _serviceProvider.GetRequiredService<ISubscriber<GameStateChangedEvent>>();

        _viewModel = new MessageLogViewModel(
            combatSubscriber,
            inventorySubscriber,
            gameStateSubscriber);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
        _serviceProvider.Dispose();
    }

    [Fact]
    public void Constructor_InheritsFromReactiveObject()
    {
        // Assert
        _viewModel.Should().BeAssignableTo<ReactiveUI.ReactiveObject>();
    }

    [Fact]
    public void Messages_ReturnsObservableCollection()
    {
        // Assert
        _viewModel.Messages.Should().NotBeNull();
        _viewModel.Messages.Should().BeAssignableTo<ObservableCollections.IObservableCollection<MessageViewModel>>();
    }



    [Fact]
    public void PlayerDamagedEvent_AddsMessageWithCombatType()
    {
        // Arrange
        var damageEvent = new PlayerDamagedEvent
        {
            Damage = 10,
            RemainingHealth = 90,
            Source = "Goblin"
        };

        // Act
        _combatPublisher.Publish(damageEvent);

        // Assert
        _viewModel.Messages.Should().HaveCount(1);
        var message = _viewModel.Messages.First();
        message.Text.Should().Be("Took 10 damage from Goblin! (90 HP remaining)");
        message.Type.Should().Be(MessageType.Combat);
    }

    [Fact]
    public void ItemPickedUpEvent_AddsMessageWithInventoryType()
    {
        // Arrange
        var pickupEvent = new ItemPickedUpEvent
        {
            ItemName = "Health Potion",
            ItemType = "Consumable"
        };

        // Act
        _inventoryPublisher.Publish(pickupEvent);

        // Assert
        _viewModel.Messages.Should().HaveCount(1);
        var message = _viewModel.Messages.First();
        message.Text.Should().Be("Picked up Health Potion (Consumable)");
        message.Type.Should().Be(MessageType.Inventory);
    }

    [Fact]
    public void GameStateChangedEvent_AddsMessageWithGameStateType()
    {
        // Arrange
        var stateEvent = new GameStateChangedEvent
        {
            PreviousState = "Playing",
            NewState = "Paused"
        };

        // Act
        _gameStatePublisher.Publish(stateEvent);

        // Assert
        _viewModel.Messages.Should().HaveCount(1);
        var message = _viewModel.Messages.First();
        message.Text.Should().Be("Game state changed from Playing to Paused");
        message.Type.Should().Be(MessageType.GameState);
    }

    [Fact]
    public void AddMessage_AddsMessageToCollection()
    {
        // Act
        _viewModel.AddMessage("Test message", MessageType.Combat);

        // Assert
        _viewModel.Messages.Should().HaveCount(1);
        _viewModel.Messages.First().Text.Should().Be("Test message");
        _viewModel.Messages.First().Type.Should().Be(MessageType.Combat);
    }

    [Fact]
    public void AddMessage_WithMultipleMessages_AddsThemInOrder()
    {
        // Act
        _viewModel.AddMessage("First message", MessageType.Combat);
        _viewModel.AddMessage("Second message", MessageType.Inventory);
        _viewModel.AddMessage("Third message", MessageType.GameState);

        // Assert
        _viewModel.Messages.Should().HaveCount(3);
        _viewModel.Messages.ElementAt(0).Text.Should().Be("First message");
        _viewModel.Messages.ElementAt(1).Text.Should().Be("Second message");
        _viewModel.Messages.ElementAt(2).Text.Should().Be("Third message");
    }

    [Fact]
    public void AddMessage_WhenExceeding100Messages_RemovesOldestMessages()
    {
        // Arrange - Add 105 messages
        for (int i = 0; i < 105; i++)
        {
            _viewModel.AddMessage($"Message {i}", MessageType.Combat);
        }

        // Assert
        _viewModel.Messages.Should().HaveCount(100);
        _viewModel.Messages.First().Text.Should().Be("Message 5"); // First 5 should be removed
        _viewModel.Messages.Last().Text.Should().Be("Message 104");
    }

    [Fact]
    public void AddMessage_Exactly100Messages_DoesNotRemoveAny()
    {
        // Arrange - Add exactly 100 messages
        for (int i = 0; i < 100; i++)
        {
            _viewModel.AddMessage($"Message {i}", MessageType.Combat);
        }

        // Assert
        _viewModel.Messages.Should().HaveCount(100);
        _viewModel.Messages.First().Text.Should().Be("Message 0");
        _viewModel.Messages.Last().Text.Should().Be("Message 99");
    }

    [Fact]
    public void AddMessage_With99Messages_DoesNotRemoveAny()
    {
        // Arrange - Add 99 messages
        for (int i = 0; i < 99; i++)
        {
            _viewModel.AddMessage($"Message {i}", MessageType.Combat);
        }

        // Assert
        _viewModel.Messages.Should().HaveCount(99);
        _viewModel.Messages.First().Text.Should().Be("Message 0");
    }

    [Fact]
    public void AddMessage_SupportsDifferentMessageTypes()
    {
        // Act
        _viewModel.AddMessage("Combat message", MessageType.Combat);
        _viewModel.AddMessage("Inventory message", MessageType.Inventory);
        _viewModel.AddMessage("Level message", MessageType.Level);
        _viewModel.AddMessage("Map message", MessageType.Map);
        _viewModel.AddMessage("GameState message", MessageType.GameState);

        // Assert
        _viewModel.Messages.Should().HaveCount(5);
        _viewModel.Messages.ElementAt(0).Type.Should().Be(MessageType.Combat);
        _viewModel.Messages.ElementAt(1).Type.Should().Be(MessageType.Inventory);
        _viewModel.Messages.ElementAt(2).Type.Should().Be(MessageType.Level);
        _viewModel.Messages.ElementAt(3).Type.Should().Be(MessageType.Map);
        _viewModel.Messages.ElementAt(4).Type.Should().Be(MessageType.GameState);
    }

    [Fact]
    public void MultipleEvents_AddMessagesInOrder()
    {
        // Act
        _combatPublisher.Publish(new PlayerDamagedEvent
        {
            Damage = 5,
            RemainingHealth = 95,
            Source = "Rat"
        });

        _inventoryPublisher.Publish(new ItemPickedUpEvent
        {
            ItemName = "Gold Coin",
            ItemType = "Currency"
        });

        _gameStatePublisher.Publish(new GameStateChangedEvent
        {
            PreviousState = "Menu",
            NewState = "Playing"
        });

        // Assert
        _viewModel.Messages.Should().HaveCount(3);
        _viewModel.Messages.ElementAt(0).Type.Should().Be(MessageType.Combat);
        _viewModel.Messages.ElementAt(1).Type.Should().Be(MessageType.Inventory);
        _viewModel.Messages.ElementAt(2).Type.Should().Be(MessageType.GameState);
    }

    [Fact]
    public void Dispose_DisposesSubscriptions()
    {
        // Act
        _viewModel.Dispose();

        // Publish an event after disposal
        _combatPublisher.Publish(new PlayerDamagedEvent
        {
            Damage = 5,
            RemainingHealth = 95,
            Source = "Rat"
        });

        // Assert
        // No new messages should be added after disposal.
        _viewModel.Messages.Should().BeEmpty();
    }

    [Fact]
    public void Messages_IsEmptyInitially()
    {
        // Assert
        _viewModel.Messages.Should().BeEmpty();
    }
}
