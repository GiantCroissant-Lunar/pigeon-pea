using FluentAssertions;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Shared;
using PigeonPea.Shared.Events;
using Xunit;

namespace PigeonPea.Shared.Tests;

/// <summary>
/// Tests for ServiceCollectionExtensions to verify DI configuration.
/// </summary>
public class ServiceCollectionExtensionsTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public ServiceCollectionExtensionsTests()
    {
        var services = new ServiceCollection();
        services.AddPigeonPeaServices();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    [Fact]
    public void AddPigeonPeaServices_RegistersMessagePipe()
    {
        // Arrange & Act
        var publisher = _serviceProvider.GetService<IPublisher<PlayerDamagedEvent>>();

        // Assert - Verify IPublisher can be resolved
        publisher.Should().NotBeNull("IPublisher should be registered");
    }

    [Fact]
    public void AddPigeonPeaServices_RegistersSubscriber()
    {
        // Arrange & Act
        var subscriber = _serviceProvider.GetService<ISubscriber<PlayerDamagedEvent>>();

        // Assert - Verify ISubscriber can be resolved
        subscriber.Should().NotBeNull("ISubscriber should be registered");
    }

    [Fact]
    public void MessagePipe_PublishSubscribe_Works()
    {
        // Arrange
        var publisher = _serviceProvider.GetRequiredService<IPublisher<PlayerDamagedEvent>>();
        var subscriber = _serviceProvider.GetRequiredService<ISubscriber<PlayerDamagedEvent>>();

        PlayerDamagedEvent? receivedEvent = null;
        var subscription = subscriber.Subscribe(e => receivedEvent = e);

        // Act
        var expectedEvent = new PlayerDamagedEvent
        {
            Damage = 10,
            RemainingHealth = 90,
            Source = "Goblin"
        };
        publisher.Publish(expectedEvent);

        // Assert
        receivedEvent.Should().NotBeNull("Event should be received");
        receivedEvent!.Value.Should().BeEquivalentTo(expectedEvent);

        subscription.Dispose();
    }

    [Fact]
    public void MessagePipe_MultipleEventTypes_Work()
    {
        // Arrange
        var damagePublisher = _serviceProvider.GetRequiredService<IPublisher<PlayerDamagedEvent>>();
        var damageSubscriber = _serviceProvider.GetRequiredService<ISubscriber<PlayerDamagedEvent>>();

        var itemPublisher = _serviceProvider.GetRequiredService<IPublisher<ItemPickedUpEvent>>();
        var itemSubscriber = _serviceProvider.GetRequiredService<ISubscriber<ItemPickedUpEvent>>();

        PlayerDamagedEvent? receivedDamageEvent = null;
        ItemPickedUpEvent? receivedItemEvent = null;

        var damageSub = damageSubscriber.Subscribe(e => receivedDamageEvent = e);
        var itemSub = itemSubscriber.Subscribe(e => receivedItemEvent = e);

        var expectedDamageEvent = new PlayerDamagedEvent { Damage = 5, RemainingHealth = 95, Source = "Trap" };
        var expectedItemEvent = new ItemPickedUpEvent { ItemName = "Health Potion", ItemType = "Consumable" };

        // Act
        damagePublisher.Publish(expectedDamageEvent);
        itemPublisher.Publish(expectedItemEvent);

        // Assert
        receivedDamageEvent.Should().NotBeNull();
        receivedDamageEvent!.Value.Should().BeEquivalentTo(expectedDamageEvent);

        receivedItemEvent.Should().NotBeNull();
        receivedItemEvent!.Value.Should().BeEquivalentTo(expectedItemEvent);

        damageSub.Dispose();
        itemSub.Dispose();
    }

    [Fact]
    public void MessagePipe_MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        var publisher = _serviceProvider.GetRequiredService<IPublisher<GameStateChangedEvent>>();
        var subscriber = _serviceProvider.GetRequiredService<ISubscriber<GameStateChangedEvent>>();

        int receivedCount = 0;
        var sub1 = subscriber.Subscribe(_ => receivedCount++);
        var sub2 = subscriber.Subscribe(_ => receivedCount++);
        var sub3 = subscriber.Subscribe(_ => receivedCount++);

        // Act
        publisher.Publish(new GameStateChangedEvent { NewState = "Playing", PreviousState = "Menu" });

        // Assert
        receivedCount.Should().Be(3, "All three subscribers should receive the event");

        sub1.Dispose();
        sub2.Dispose();
        sub3.Dispose();
    }

    [Fact]
    public void MessagePipe_DisposedSubscription_DoesNotReceiveEvent()
    {
        // Arrange
        var publisher = _serviceProvider.GetRequiredService<IPublisher<PlayerDamagedEvent>>();
        var subscriber = _serviceProvider.GetRequiredService<ISubscriber<PlayerDamagedEvent>>();

        int receivedCount = 0;
        var subscription = subscriber.Subscribe(_ => receivedCount++);

        // Act - Publish first event
        publisher.Publish(new PlayerDamagedEvent { Damage = 10, RemainingHealth = 90, Source = "Enemy" });
        int countAfterFirst = receivedCount;

        // Dispose subscription
        subscription.Dispose();

        // Publish second event
        publisher.Publish(new PlayerDamagedEvent { Damage = 10, RemainingHealth = 80, Source = "Enemy" });
        int countAfterSecond = receivedCount;

        // Assert
        countAfterFirst.Should().Be(1, "First event should be received");
        countAfterSecond.Should().Be(1, "Second event should not be received after disposal");
    }
}
