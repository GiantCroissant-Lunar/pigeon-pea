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
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPigeonPeaServices_RegistersMessagePipe()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPigeonPeaServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify IPublisher can be resolved
        var publisher = serviceProvider.GetService<IPublisher<PlayerDamagedEvent>>();
        publisher.Should().NotBeNull("IPublisher should be registered");
    }

    [Fact]
    public void AddPigeonPeaServices_RegistersSubscriber()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPigeonPeaServices();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Verify ISubscriber can be resolved
        var subscriber = serviceProvider.GetService<ISubscriber<PlayerDamagedEvent>>();
        subscriber.Should().NotBeNull("ISubscriber should be registered");
    }

    [Fact]
    public void MessagePipe_PublishSubscribe_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPigeonPeaServices();
        var serviceProvider = services.BuildServiceProvider();

        var publisher = serviceProvider.GetRequiredService<IPublisher<PlayerDamagedEvent>>();
        var subscriber = serviceProvider.GetRequiredService<ISubscriber<PlayerDamagedEvent>>();

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
        receivedEvent!.Value.Damage.Should().Be(10);
        receivedEvent!.Value.RemainingHealth.Should().Be(90);
        receivedEvent!.Value.Source.Should().Be("Goblin");

        subscription.Dispose();
    }

    [Fact]
    public void MessagePipe_MultipleEventTypes_Work()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPigeonPeaServices();
        var serviceProvider = services.BuildServiceProvider();

        var damagePublisher = serviceProvider.GetRequiredService<IPublisher<PlayerDamagedEvent>>();
        var damageSubscriber = serviceProvider.GetRequiredService<ISubscriber<PlayerDamagedEvent>>();

        var itemPublisher = serviceProvider.GetRequiredService<IPublisher<ItemPickedUpEvent>>();
        var itemSubscriber = serviceProvider.GetRequiredService<ISubscriber<ItemPickedUpEvent>>();

        PlayerDamagedEvent? receivedDamageEvent = null;
        ItemPickedUpEvent? receivedItemEvent = null;

        var damageSub = damageSubscriber.Subscribe(e => receivedDamageEvent = e);
        var itemSub = itemSubscriber.Subscribe(e => receivedItemEvent = e);

        // Act
        damagePublisher.Publish(new PlayerDamagedEvent { Damage = 5, RemainingHealth = 95, Source = "Trap" });
        itemPublisher.Publish(new ItemPickedUpEvent { ItemName = "Health Potion", ItemType = "Consumable" });

        // Assert
        receivedDamageEvent.Should().NotBeNull();
        receivedDamageEvent!.Value.Damage.Should().Be(5);

        receivedItemEvent.Should().NotBeNull();
        receivedItemEvent!.Value.ItemName.Should().Be("Health Potion");

        damageSub.Dispose();
        itemSub.Dispose();
    }

    [Fact]
    public void MessagePipe_MultipleSubscribers_AllReceiveEvent()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPigeonPeaServices();
        var serviceProvider = services.BuildServiceProvider();

        var publisher = serviceProvider.GetRequiredService<IPublisher<GameStateChangedEvent>>();
        var subscriber = serviceProvider.GetRequiredService<ISubscriber<GameStateChangedEvent>>();

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
        var services = new ServiceCollection();
        services.AddPigeonPeaServices();
        var serviceProvider = services.BuildServiceProvider();

        var publisher = serviceProvider.GetRequiredService<IPublisher<PlayerDamagedEvent>>();
        var subscriber = serviceProvider.GetRequiredService<ISubscriber<PlayerDamagedEvent>>();

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
