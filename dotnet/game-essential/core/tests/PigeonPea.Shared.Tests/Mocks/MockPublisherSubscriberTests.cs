using PigeonPea.Shared.Events;
using PigeonPea.Shared.Tests.Mocks;
using Xunit;

namespace PigeonPea.Shared.Tests.Mocks;

/// <summary>
/// Tests for MockPublisher and MockSubscriber to ensure they properly track event publishing and subscription.
/// </summary>
public class MockPublisherSubscriberTests
{
    [Fact]
    public void MockPublisherPublishAddsToPublishedEvents()
    {
        // Arrange
        var publisher = new MockPublisher<PlayerDamagedEvent>();
        var evt = new PlayerDamagedEvent
        {
            Damage = 10,
            RemainingHealth = 90,
            Source = "Goblin"
        };

        // Act
        publisher.Publish(evt);

        // Assert
        Assert.Single(publisher.PublishedEvents);
        Assert.Equal(10, publisher.PublishedEvents[0].Damage);
        Assert.Equal("Goblin", publisher.PublishedEvents[0].Source);
    }

    [Fact]
    public void MockPublisherPublishCallCountIncrementsCorrectly()
    {
        // Arrange
        var publisher = new MockPublisher<PlayerDamagedEvent>();
        var evt = new PlayerDamagedEvent { Damage = 5, RemainingHealth = 95, Source = "Trap" };

        // Act
        publisher.Publish(evt);
        publisher.Publish(evt);
        publisher.Publish(evt);

        // Assert
        Assert.Equal(3, publisher.PublishCallCount);
    }

    [Fact]
    public void MockPublisherGetLastPublishedEventReturnsLastEvent()
    {
        // Arrange
        var publisher = new MockPublisher<ItemPickedUpEvent>();
        publisher.Publish(new ItemPickedUpEvent { ItemName = "Sword", ItemType = "Equipment" });
        publisher.Publish(new ItemPickedUpEvent { ItemName = "Potion", ItemType = "Consumable" });

        // Act
        var lastEvent = publisher.GetLastPublishedEvent();

        // Assert
        Assert.Equal("Potion", lastEvent!.ItemName);
        Assert.Equal("Consumable", lastEvent.ItemType);
    }

    [Fact]
    public void MockPublisherGetLastPublishedEventReturnsDefaultWhenEmpty()
    {
        // Arrange
        var publisher = new MockPublisher<ItemPickedUpEvent>();

        // Act
        var lastEvent = publisher.GetLastPublishedEvent();

        // Assert
        Assert.Equal(default(ItemPickedUpEvent), lastEvent);
    }

    [Fact]
    public void MockPublisherResetClearsPublishedEvents()
    {
        // Arrange
        var publisher = new MockPublisher<PlayerDamagedEvent>();
        var evt = new PlayerDamagedEvent { Damage = 10, RemainingHealth = 90, Source = "Enemy" };
        publisher.Publish(evt);
        publisher.Publish(evt);
        Assert.Equal(2, publisher.PublishCallCount);

        // Act
        publisher.Reset();

        // Assert
        Assert.Empty(publisher.PublishedEvents);
        Assert.Equal(0, publisher.PublishCallCount);
    }

    [Fact]
    public void MockSubscriberSubscribeIncrementsSubscriptionCount()
    {
        // Arrange
        var subscriber = new MockSubscriber<PlayerDamagedEvent>();

        // Act
        var subscription1 = subscriber.Subscribe(_ => { });
        var subscription2 = subscriber.Subscribe(_ => { });

        // Assert
        Assert.Equal(2, subscriber.SubscriptionCount);

        subscription1.Dispose();
        subscription2.Dispose();
    }

    [Fact]
    public void MockSubscriberTriggerEventCallsSubscribedHandlers()
    {
        // Arrange
        var subscriber = new MockSubscriber<PlayerDamagedEvent>();
        int callCount = 0;
        PlayerDamagedEvent? receivedEvent = null;

        var subscription = subscriber.Subscribe(e =>
        {
            callCount++;
            receivedEvent = e;
        });

        var evt = new PlayerDamagedEvent
        {
            Damage = 15,
            RemainingHealth = 85,
            Source = "Dragon"
        };

        // Act
        subscriber.TriggerEvent(evt);

        // Assert
        Assert.Equal(1, callCount);
        Assert.NotNull(receivedEvent);
        Assert.Equal(15, receivedEvent!.Value.Damage);
        Assert.Equal("Dragon", receivedEvent.Value.Source);

        subscription.Dispose();
    }

    [Fact]
    public void MockSubscriberTriggerEventCallsAllSubscribers()
    {
        // Arrange
        var subscriber = new MockSubscriber<GameStateChangedEvent>();
        int handler1Count = 0;
        int handler2Count = 0;
        int handler3Count = 0;

        var sub1 = subscriber.Subscribe(_ => handler1Count++);
        var sub2 = subscriber.Subscribe(_ => handler2Count++);
        var sub3 = subscriber.Subscribe(_ => handler3Count++);

        var evt = new GameStateChangedEvent { NewState = "Playing", PreviousState = "Menu" };

        // Act
        subscriber.TriggerEvent(evt);

        // Assert
        Assert.Equal(1, handler1Count);
        Assert.Equal(1, handler2Count);
        Assert.Equal(1, handler3Count);

        sub1.Dispose();
        sub2.Dispose();
        sub3.Dispose();
    }

    [Fact]
    public void MockSubscriptionDisposeUnsubscribesHandler()
    {
        // Arrange
        var subscriber = new MockSubscriber<PlayerDamagedEvent>();
        int callCount = 0;
        var subscription = subscriber.Subscribe(_ => callCount++);
        Assert.Equal(1, subscriber.SubscriptionCount);

        // Act
        subscription.Dispose();

        // Assert
        Assert.Equal(0, subscriber.SubscriptionCount);

        // Verify handler is not called after disposal
        subscriber.TriggerEvent(new PlayerDamagedEvent { Damage = 10, RemainingHealth = 90, Source = "Test" });
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void MockSubscriptionDisposeMultipleTimesIsSafe()
    {
        // Arrange
        var subscriber = new MockSubscriber<PlayerDamagedEvent>();
        var subscription = subscriber.Subscribe(_ => { });

        // Act & Assert - Should not throw
        subscription.Dispose();
        subscription.Dispose();
        subscription.Dispose();

        Assert.Equal(0, subscriber.SubscriptionCount);
    }

    [Fact]
    public void MockSubscriberResetClearsAllSubscriptions()
    {
        // Arrange
        var subscriber = new MockSubscriber<PlayerDamagedEvent>();
        int callCount = 0;

        subscriber.Subscribe(_ => callCount++);
        subscriber.Subscribe(_ => callCount++);
        Assert.Equal(2, subscriber.SubscriptionCount);

        // Act
        subscriber.Reset();

        // Assert
        Assert.Equal(0, subscriber.SubscriptionCount);

        // Verify handlers are not called after reset
        subscriber.TriggerEvent(new PlayerDamagedEvent { Damage = 10, RemainingHealth = 90, Source = "Test" });
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void MockPublisherSubscriberWorkTogetherForTesting()
    {
        // Arrange
        var publisher = new MockPublisher<ItemPickedUpEvent>();
        var subscriber = new MockSubscriber<ItemPickedUpEvent>();

        ItemPickedUpEvent? receivedBySubscriber = null;
        var subscription = subscriber.Subscribe(e => receivedBySubscriber = e);

        var evt = new ItemPickedUpEvent
        {
            ItemName = "Magic Ring",
            ItemType = "Equipment"
        };

        // Act
        publisher.Publish(evt);
        // Manually trigger since mocks are independent
        if (publisher.PublishedEvents.Count > 0)
        {
            subscriber.TriggerEvent(publisher.PublishedEvents[^1]);
        }

        // Assert
        Assert.Single(publisher.PublishedEvents);
        Assert.NotNull(receivedBySubscriber);
        Assert.Equal("Magic Ring", receivedBySubscriber!.Value.ItemName);

        subscription.Dispose();
    }

    [Fact]
    public void MockSubscriberMultipleEventsEachHandlerReceivesAll()
    {
        // Arrange
        var subscriber = new MockSubscriber<PlayerDamagedEvent>();
        var events1 = new List<PlayerDamagedEvent>();
        var events2 = new List<PlayerDamagedEvent>();

        var sub1 = subscriber.Subscribe(e => events1.Add(e));
        var sub2 = subscriber.Subscribe(e => events2.Add(e));

        // Act
        subscriber.TriggerEvent(new PlayerDamagedEvent { Damage = 10, RemainingHealth = 90, Source = "Goblin" });
        subscriber.TriggerEvent(new PlayerDamagedEvent { Damage = 5, RemainingHealth = 85, Source = "Trap" });
        subscriber.TriggerEvent(new PlayerDamagedEvent { Damage = 20, RemainingHealth = 65, Source = "Dragon" });

        // Assert
        Assert.Equal(3, events1.Count);
        Assert.Equal(3, events2.Count);
        Assert.Equal("Goblin", events1[0].Source);
        Assert.Equal("Trap", events1[1].Source);
        Assert.Equal("Dragon", events1[2].Source);

        sub1.Dispose();
        sub2.Dispose();
    }
}
