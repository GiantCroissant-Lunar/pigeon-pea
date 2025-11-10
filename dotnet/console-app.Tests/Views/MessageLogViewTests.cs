using Xunit;
using FluentAssertions;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Console.Views;
using PigeonPea.Shared.ViewModels;
using PigeonPea.Shared.Events;
using System.Threading.Tasks;

namespace PigeonPea.Console.Tests.Views;

/// <summary>
/// Integration tests for MessageLogView that verify reactive subscriptions work correctly.
/// </summary>
public class MessageLogViewTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly MessageLogViewModel _viewModel;
    private readonly MessageLogView _view;

    public MessageLogViewTests()
    {
        // Setup DI container with MessagePipe
        var services = new ServiceCollection();
        services.AddMessagePipe();
        _serviceProvider = services.BuildServiceProvider();

        // Create view model with MessagePipe subscribers
        var combatSubscriber = _serviceProvider.GetRequiredService<ISubscriber<PlayerDamagedEvent>>();
        var inventorySubscriber = _serviceProvider.GetRequiredService<ISubscriber<ItemPickedUpEvent>>();
        var gameStateSubscriber = _serviceProvider.GetRequiredService<ISubscriber<GameStateChangedEvent>>();

        _viewModel = new MessageLogViewModel(combatSubscriber, inventorySubscriber, gameStateSubscriber);
        _view = new MessageLogView(_viewModel);
    }

    [Fact]
    public void MessageLogView_Constructor_InitializesWithViewModel()
    {
        // Assert
        _view.Should().NotBeNull();
        _view.Title.Should().Be("Messages");
    }

    [Fact]
    public void MessageLogView_SubscribesToMessagesAdded()
    {
        // Act
        _viewModel.AddMessage("Test message", MessageType.Combat);

        // Assert
        _viewModel.Messages.Count.Should().Be(1);
        _viewModel.Messages.First().Text.Should().Be("Test message");
        _viewModel.Messages.First().Type.Should().Be(MessageType.Combat);
    }

    [Fact]
    public void MessageLogView_HandlesMultipleMessages()
    {
        // Act
        _viewModel.AddMessage("First message", MessageType.Combat);
        _viewModel.AddMessage("Second message", MessageType.Inventory);
        _viewModel.AddMessage("Third message", MessageType.Level);

        // Assert
        _viewModel.Messages.Count.Should().Be(3);
        _viewModel.Messages.ElementAt(0).Text.Should().Be("First message");
        _viewModel.Messages.ElementAt(1).Text.Should().Be("Second message");
        _viewModel.Messages.ElementAt(2).Text.Should().Be("Third message");
    }

    [Fact]
    public void MessageLogView_HandlesMessageTypes()
    {
        // Act
        _viewModel.AddMessage("Combat message", MessageType.Combat);
        _viewModel.AddMessage("Inventory message", MessageType.Inventory);
        _viewModel.AddMessage("Level message", MessageType.Level);
        _viewModel.AddMessage("Map message", MessageType.Map);
        _viewModel.AddMessage("GameState message", MessageType.GameState);

        // Assert
        _viewModel.Messages.Count.Should().Be(5);
        _viewModel.Messages.ElementAt(0).Type.Should().Be(MessageType.Combat);
        _viewModel.Messages.ElementAt(1).Type.Should().Be(MessageType.Inventory);
        _viewModel.Messages.ElementAt(2).Type.Should().Be(MessageType.Level);
        _viewModel.Messages.ElementAt(3).Type.Should().Be(MessageType.Map);
        _viewModel.Messages.ElementAt(4).Type.Should().Be(MessageType.GameState);
    }

    [Fact]
    public void MessageLogView_LimitsMessagesToMaximum()
    {
        // Act - Add more than 100 messages (MaxMessages constant)
        for (int i = 0; i < 150; i++)
        {
            _viewModel.AddMessage($"Message {i}", MessageType.Combat);
        }

        // Assert - Should keep only the last 100 messages
        _viewModel.Messages.Count.Should().BeLessThanOrEqualTo(100);
        // The oldest messages should be removed
        _viewModel.Messages.First().Text.Should().NotBe("Message 0");
    }

    [Fact]
    public void MessageLogView_TimestampsMessages()
    {
        // Arrange
        var beforeTime = DateTime.UtcNow;

        // Act
        _viewModel.AddMessage("Timestamped message", MessageType.Combat);

        // Assert
        var afterTime = DateTime.UtcNow;
        _viewModel.Messages.First().Timestamp.Should().BeOnOrAfter(beforeTime);
        _viewModel.Messages.First().Timestamp.Should().BeOnOrBefore(afterTime);
    }

    [Fact]
    public async Task MessageLogView_HandlesPlayerDamagedEvents()
    {
        // Arrange
        var publisher = _serviceProvider.GetRequiredService<IPublisher<PlayerDamagedEvent>>();
        var initialCount = _viewModel.Messages.Count;

        // Act
        publisher.Publish(new PlayerDamagedEvent
        {
            Damage = 10,
            RemainingHealth = 90,
            Source = "Goblin"
        });

        // Give time for event to be processed
        await Task.Delay(100);

        // Assert
        _viewModel.Messages.Count.Should().BeGreaterThan(initialCount);
        _viewModel.Messages.Any(m => m.Text.Contains("damage") && m.Type == MessageType.Combat).Should().BeTrue();
    }

    [Fact]
    public async Task MessageLogView_HandlesItemPickedUpEvents()
    {
        // Arrange
        var publisher = _serviceProvider.GetRequiredService<IPublisher<ItemPickedUpEvent>>();
        var initialCount = _viewModel.Messages.Count;

        // Act
        publisher.Publish(new ItemPickedUpEvent
        {
            ItemName = "Health Potion",
            ItemType = "Consumable"
        });

        // Give time for event to be processed
        await Task.Delay(100);

        // Assert
        _viewModel.Messages.Count.Should().BeGreaterThan(initialCount);
        _viewModel.Messages.Any(m => m.Text.Contains("Picked up") && m.Type == MessageType.Inventory).Should().BeTrue();
    }

    [Fact]
    public async Task MessageLogView_HandlesGameStateChangedEvents()
    {
        // Arrange
        var publisher = _serviceProvider.GetRequiredService<IPublisher<GameStateChangedEvent>>();
        var initialCount = _viewModel.Messages.Count;

        // Act
        publisher.Publish(new GameStateChangedEvent
        {
            PreviousState = "Playing",
            NewState = "Paused"
        });

        // Give time for event to be processed
        await Task.Delay(100);

        // Assert
        _viewModel.Messages.Count.Should().BeGreaterThan(initialCount);
        _viewModel.Messages.Any(m => m.Text.Contains("Game state") && m.Type == MessageType.GameState).Should().BeTrue();
    }

    [Fact]
    public void MessageLogView_DisposesSubscriptionsOnDispose()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMessagePipe();
        var provider = services.BuildServiceProvider();

        var combatSubscriber = provider.GetRequiredService<ISubscriber<PlayerDamagedEvent>>();
        var inventorySubscriber = provider.GetRequiredService<ISubscriber<ItemPickedUpEvent>>();
        var gameStateSubscriber = provider.GetRequiredService<ISubscriber<GameStateChangedEvent>>();

        var viewModel = new MessageLogViewModel(combatSubscriber, inventorySubscriber, gameStateSubscriber);
        var view = new MessageLogView(viewModel);

        // Act
        view.Dispose();
        viewModel.Dispose();

        // Assert - No exception should occur when adding messages after disposal
        var act = () => viewModel.AddMessage("Test", MessageType.Combat);
        act.Should().NotThrow();

        // Cleanup
        provider.Dispose();
    }

    public void Dispose()
    {
        _view.Dispose();
        _viewModel.Dispose();
        _serviceProvider.Dispose();
    }
}
