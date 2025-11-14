using Arch.Core;
using FluentAssertions;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Events;
using PigeonPea.Shared.ViewModels;
using Xunit;

namespace PigeonPea.Shared.Tests.ViewModels;

/// <summary>
/// Tests for InventoryViewModel to verify collection updates, event subscriptions, and ECS synchronization.
/// </summary>
public class InventoryViewModelTests : IDisposable
{
    private readonly World _world;
    private readonly Entity _playerEntity;
    private readonly Entity _itemEntity1;
    private readonly Entity _itemEntity2;
    private readonly IPublisher<ItemPickedUpEvent> _itemPickedUpPublisher;
    private readonly ISubscriber<ItemPickedUpEvent> _itemPickedUpSubscriber;
    private readonly IPublisher<ItemUsedEvent> _itemUsedPublisher;
    private readonly ISubscriber<ItemUsedEvent> _itemUsedSubscriber;
    private readonly IPublisher<ItemDroppedEvent> _itemDroppedPublisher;
    private readonly ISubscriber<ItemDroppedEvent> _itemDroppedSubscriber;

    public InventoryViewModelTests()
    {
        _world = World.Create();

        // Create test items
        _itemEntity1 = _world.Create(
            new Item("Health Potion", ItemType.Consumable)
        );
        _itemEntity2 = _world.Create(
            new Item("Iron Sword", ItemType.Equipment)
        );

        // Create player with inventory
        var inventory = new Inventory(10);
        inventory.Items.Add(_itemEntity1);
        inventory.Items.Add(_itemEntity2);

        _playerEntity = _world.Create(inventory);

        // Set up MessagePipe
        var services = new ServiceCollection();
        services.AddMessagePipe();
        var provider = services.BuildServiceProvider();

        _itemPickedUpPublisher = provider.GetRequiredService<IPublisher<ItemPickedUpEvent>>();
        _itemPickedUpSubscriber = provider.GetRequiredService<ISubscriber<ItemPickedUpEvent>>();

        _itemUsedPublisher = provider.GetRequiredService<IPublisher<ItemUsedEvent>>();
        _itemUsedSubscriber = provider.GetRequiredService<ISubscriber<ItemUsedEvent>>();

        _itemDroppedPublisher = provider.GetRequiredService<IPublisher<ItemDroppedEvent>>();
        _itemDroppedSubscriber = provider.GetRequiredService<ISubscriber<ItemDroppedEvent>>();
    }

    public void Dispose()
    {
        World.Destroy(_world);
    }

    [Fact]
    public void ConstructorSubscribesToEvents()
    {
        // Arrange & Act
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );

        // Assert - should not throw and subscriptions should be set up
        viewModel.Items.Should().NotBeNull();
        viewModel.SelectedIndex.Should().Be(-1);
        viewModel.SelectedItem.Should().BeNull();

        viewModel.Dispose();
    }

    [Fact]
    public void ItemsIsObservableList()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );

        // Act & Assert
        viewModel.Items.Should().BeOfType<ObservableCollections.ObservableList<ItemViewModel>>();

        viewModel.Dispose();
    }

    [Fact]
    public void SelectedIndexWhenChangedRaisesNotification()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );
        bool propertyChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(InventoryViewModel.SelectedIndex))
            {
                propertyChanged = true;
            }
        };

        // Act
        viewModel.SelectedIndex = 0;

        // Assert
        propertyChanged.Should().BeTrue("SelectedIndex property change should raise PropertyChanged");
        viewModel.SelectedIndex.Should().Be(0);

        viewModel.Dispose();
    }

    [Fact]
    public void SelectedItemReturnsNullWhenNoItemSelected()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );
        var testEntity = _world.Create(new Item("Test Item", ItemType.Consumable));
        viewModel.Items.Add(new ItemViewModel { SourceEntity = testEntity, Name = "Test Item", Type = ItemType.Consumable });

        // Act
        var selectedItem = viewModel.SelectedItem;

        // Assert
        selectedItem.Should().BeNull("No item is selected when SelectedIndex is -1");

        viewModel.Dispose();
    }

    [Fact]
    public void SelectedItemReturnsItemWhenValidIndexSelected()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );
        var testEntity = _world.Create(new Item("Test Item", ItemType.Consumable));
        var testItem = new ItemViewModel { SourceEntity = testEntity, Name = "Test Item", Type = ItemType.Consumable };
        viewModel.Items.Add(testItem);
        viewModel.SelectedIndex = 0;

        // Act
        var selectedItem = viewModel.SelectedItem;

        // Assert
        selectedItem.Should().BeSameAs(testItem);
        selectedItem!.Name.Should().Be("Test Item");

        viewModel.Dispose();
    }

    [Fact]
    public void SelectedItemReturnsNullWhenIndexOutOfRange()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );
        var testEntity = _world.Create(new Item("Test Item", ItemType.Consumable));
        viewModel.Items.Add(new ItemViewModel { SourceEntity = testEntity, Name = "Test Item", Type = ItemType.Consumable });
        viewModel.SelectedIndex = 10; // Out of range

        // Act
        var selectedItem = viewModel.SelectedItem;

        // Assert
        selectedItem.Should().BeNull("Index is out of range");

        viewModel.Dispose();
    }

    [Fact]
    public void UpdateSyncsItemsFromECS()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );

        // Act
        viewModel.Update(_world, _playerEntity);

        // Assert
        viewModel.Items.Should().HaveCount(2);
        viewModel.Items[0].Name.Should().Be("Health Potion");
        viewModel.Items[0].Type.Should().Be(ItemType.Consumable);
        viewModel.Items[1].Name.Should().Be("Iron Sword");
        viewModel.Items[1].Type.Should().Be(ItemType.Equipment);

        viewModel.Dispose();
    }

    [Fact]
    public void UpdateWithDeadEntityDoesNotThrow()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );
        var deadWorld = World.Create();
        var deadEntity = deadWorld.Create();
        deadWorld.Destroy(deadEntity);

        // Act
        Action act = () => viewModel.Update(deadWorld, deadEntity);

        // Assert
        act.Should().NotThrow("Update should handle dead entities gracefully");

        World.Destroy(deadWorld);
        viewModel.Dispose();
    }

    [Fact]
    public void UpdateWithMissingInventoryComponentDoesNotThrow()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );
        var testEntity = _world.Create(new Item("Old Item", ItemType.Consumable));
        viewModel.Items.Add(new ItemViewModel { SourceEntity = testEntity, Name = "Old Item", Type = ItemType.Consumable });

        var emptyWorld = World.Create();
        var emptyEntity = emptyWorld.Create(); // Entity with no components

        // Act
        Action act = () => viewModel.Update(emptyWorld, emptyEntity);

        // Assert
        act.Should().NotThrow("Update should handle missing components gracefully");
        viewModel.Items.Should().HaveCount(1, "Items should remain unchanged");

        World.Destroy(emptyWorld);
        viewModel.Dispose();
    }

    [Fact]
    public void UpdateMultipleUpdatesSyncsCorrectly()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );

        // Act - First update
        viewModel.Update(_world, _playerEntity);
        var firstCount = viewModel.Items.Count;

        // Add a new item to inventory
        var newItemEntity = _world.Create(new Item("Magic Ring", ItemType.Equipment));
        ref var inventory = ref _world.Get<Inventory>(_playerEntity);
        inventory.Items.Add(newItemEntity);

        // Act - Second update
        viewModel.Update(_world, _playerEntity);

        // Assert
        firstCount.Should().Be(2, "First update should sync initial items");
        viewModel.Items.Should().HaveCount(3, "Second update should sync new item");
        viewModel.Items[2].Name.Should().Be("Magic Ring");

        viewModel.Dispose();
    }

    [Fact]
    public void ItemPickedUpEventDoesNotModifyItems()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );
        var initialCount = viewModel.Items.Count;

        // Act - Publish event
        _itemPickedUpPublisher.Publish(new ItemPickedUpEvent
        {
            ItemName = "Test Item",
            ItemType = "Consumable"
        });

        // Assert - Items collection should remain unchanged
        // Event handlers are currently placeholders that don't modify state
        viewModel.Items.Count.Should().Be(initialCount, "Event handler is a placeholder and should not modify Items collection");

        viewModel.Dispose();
    }

    [Fact]
    public void ItemUsedEventDoesNotModifyItems()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );
        viewModel.Update(_world, _playerEntity);
        var initialCount = viewModel.Items.Count;

        // Act - Publish event
        _itemUsedPublisher.Publish(new ItemUsedEvent
        {
            ItemName = "Health Potion",
            ItemType = "Consumable"
        });

        // Assert - Items collection should remain unchanged
        // Event handlers are currently placeholders that don't modify state
        viewModel.Items.Count.Should().Be(initialCount, "Event handler is a placeholder and should not modify Items collection");

        viewModel.Dispose();
    }

    [Fact]
    public void ItemDroppedEventDoesNotModifyItems()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );
        viewModel.Update(_world, _playerEntity);
        var initialCount = viewModel.Items.Count;

        // Act - Publish event
        _itemDroppedPublisher.Publish(new ItemDroppedEvent
        {
            ItemName = "Old Sword"
        });

        // Assert - Items collection should remain unchanged
        // Event handlers are currently placeholders that don't modify state
        viewModel.Items.Count.Should().Be(initialCount, "Event handler is a placeholder and should not modify Items collection");

        viewModel.Dispose();
    }

    [Fact]
    public void DisposeDisposesSubscriptions()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );

        int eventReceivedAfterDispose = 0;
        // Add a test item to track if events are still received after dispose
        viewModel.Update(_world, _playerEntity);

        // Act - Dispose the view model
        viewModel.Dispose();

        // Set up a separate subscription to verify the event is published
        var testSub = _itemPickedUpSubscriber.Subscribe(e => eventReceivedAfterDispose++);

        // Publish an event after disposal
        _itemPickedUpPublisher.Publish(new ItemPickedUpEvent
        {
            ItemName = "Test",
            ItemType = "Consumable"
        });

        // Assert - The viewModel's state should not have changed after disposal
        // If subscriptions were properly disposed, the viewModel won't receive the event
        viewModel.Items.Count.Should().Be(2, "Items should remain unchanged after disposal");
        eventReceivedAfterDispose.Should().Be(1, "Test subscription should still receive events");

        testSub.Dispose();
    }

    [Fact]
    public void UpdateWithDeadItemEntitiesSkipsThem()
    {
        // Arrange
        var viewModel = new InventoryViewModel(
            _itemPickedUpSubscriber,
            _itemUsedSubscriber,
            _itemDroppedSubscriber
        );

        // Destroy one of the items
        _world.Destroy(_itemEntity2);

        // Act
        viewModel.Update(_world, _playerEntity);

        // Assert
        viewModel.Items.Should().HaveCount(1, "Dead item entities should be skipped");
        viewModel.Items[0].Name.Should().Be("Health Potion");

        viewModel.Dispose();
    }
}
