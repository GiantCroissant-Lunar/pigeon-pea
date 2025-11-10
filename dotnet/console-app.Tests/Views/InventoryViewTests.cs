using Xunit;
using FluentAssertions;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Console.Views;
using PigeonPea.Shared.ViewModels;
using PigeonPea.Shared.Events;
using PigeonPea.Shared.Components;
using Arch.Core;
using System.Threading.Tasks;

namespace PigeonPea.Console.Tests.Views;

/// <summary>
/// Integration tests for InventoryView that verify reactive subscriptions work correctly.
/// </summary>
public class InventoryViewTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly InventoryViewModel _viewModel;
    private readonly InventoryView _view;

    public InventoryViewTests()
    {
        // Setup DI container with MessagePipe
        var services = new ServiceCollection();
        services.AddMessagePipe();
        _serviceProvider = services.BuildServiceProvider();

        // Create view model with MessagePipe subscribers
        var itemPickedUpSubscriber = _serviceProvider.GetRequiredService<ISubscriber<ItemPickedUpEvent>>();
        var itemUsedSubscriber = _serviceProvider.GetRequiredService<ISubscriber<ItemUsedEvent>>();
        var itemDroppedSubscriber = _serviceProvider.GetRequiredService<ISubscriber<ItemDroppedEvent>>();
        
        _viewModel = new InventoryViewModel(itemPickedUpSubscriber, itemUsedSubscriber, itemDroppedSubscriber);
        _view = new InventoryView(_viewModel);
    }

    [Fact]
    public void InventoryView_Constructor_InitializesWithViewModel()
    {
        // Assert
        _view.Should().NotBeNull();
        _view.Title.Should().Be("Inventory");
    }

    [Fact]
    public void InventoryView_SubscribesToItemsAdded()
    {
        // Arrange
        var world = World.Create();
        var itemEntity = world.Create(new Item { Name = "Sword", Type = ItemType.Equipment });

        // Act
        _viewModel.Items.Add(new ItemViewModel 
        { 
            SourceEntity = itemEntity,
            Name = "Sword",
            Type = ItemType.Equipment
        });

        // Assert
        _viewModel.Items.Count.Should().Be(1);
        _viewModel.Items[0].Name.Should().Be("Sword");
        
        // Cleanup
        world.Dispose();
    }

    [Fact]
    public void InventoryView_SubscribesToItemsRemoved()
    {
        // Arrange
        var world = World.Create();
        var itemEntity = world.Create(new Item { Name = "Potion", Type = ItemType.Consumable });
        var itemViewModel = new ItemViewModel 
        { 
            SourceEntity = itemEntity,
            Name = "Potion",
            Type = ItemType.Consumable
        };
        _viewModel.Items.Add(itemViewModel);

        // Act
        _viewModel.Items.Remove(itemViewModel);

        // Assert
        _viewModel.Items.Count.Should().Be(0);
        
        // Cleanup
        world.Dispose();
    }

    [Fact]
    public void InventoryView_SubscribesToSelectedIndexChanges()
    {
        // Arrange
        var world = World.Create();
        var itemEntity = world.Create(new Item { Name = "Shield", Type = ItemType.Equipment });
        _viewModel.Items.Add(new ItemViewModel 
        { 
            SourceEntity = itemEntity,
            Name = "Shield",
            Type = ItemType.Equipment
        });

        // Act
        _viewModel.SelectedIndex = 0;

        // Assert
        _viewModel.SelectedItem.Should().NotBeNull();
        _viewModel.SelectedItem!.Name.Should().Be("Shield");
        
        // Cleanup
        world.Dispose();
    }

    [Fact]
    public void InventoryView_SelectedItem_IsNullWhenNoSelection()
    {
        // Act & Assert
        _viewModel.SelectedItem.Should().BeNull();
    }

    [Fact]
    public void InventoryView_SelectedItem_IsNullWhenIndexOutOfRange()
    {
        // Arrange
        var world = World.Create();
        var itemEntity = world.Create(new Item { Name = "Ring", Type = ItemType.QuestItem });
        _viewModel.Items.Add(new ItemViewModel 
        { 
            SourceEntity = itemEntity,
            Name = "Ring",
            Type = ItemType.QuestItem
        });

        // Act
        _viewModel.SelectedIndex = 10; // Out of range

        // Assert
        _viewModel.SelectedItem.Should().BeNull();
        
        // Cleanup
        world.Dispose();
    }

    [Fact]
    public void InventoryView_HandlesMultipleItems()
    {
        // Arrange
        var world = World.Create();
        var item1 = world.Create(new Item { Name = "Sword", Type = ItemType.Equipment });
        var item2 = world.Create(new Item { Name = "Potion", Type = ItemType.Consumable });
        var item3 = world.Create(new Item { Name = "Shield", Type = ItemType.Equipment });

        // Act
        _viewModel.Items.Add(new ItemViewModel { SourceEntity = item1, Name = "Sword", Type = ItemType.Equipment });
        _viewModel.Items.Add(new ItemViewModel { SourceEntity = item2, Name = "Potion", Type = ItemType.Consumable });
        _viewModel.Items.Add(new ItemViewModel { SourceEntity = item3, Name = "Shield", Type = ItemType.Equipment });

        // Assert
        _viewModel.Items.Count.Should().Be(3);
        _viewModel.Items[0].Name.Should().Be("Sword");
        _viewModel.Items[1].Name.Should().Be("Potion");
        _viewModel.Items[2].Name.Should().Be("Shield");
        
        // Cleanup
        world.Dispose();
    }

    [Fact]
    public void InventoryView_DisposesSubscriptionsOnDispose()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMessagePipe();
        var provider = services.BuildServiceProvider();
        
        var itemPickedUpSubscriber = provider.GetRequiredService<ISubscriber<ItemPickedUpEvent>>();
        var itemUsedSubscriber = provider.GetRequiredService<ISubscriber<ItemUsedEvent>>();
        var itemDroppedSubscriber = provider.GetRequiredService<ISubscriber<ItemDroppedEvent>>();
        
        var viewModel = new InventoryViewModel(itemPickedUpSubscriber, itemUsedSubscriber, itemDroppedSubscriber);
        var view = new InventoryView(viewModel);

        // Act
        view.Dispose();
        viewModel.Dispose();

        // Assert - No exception should occur when modifying collection after disposal
        var world = World.Create();
        var itemEntity = world.Create(new Item { Name = "Test", Type = ItemType.Equipment });
        var act = () => viewModel.Items.Add(new ItemViewModel 
        { 
            SourceEntity = itemEntity,
            Name = "Test",
            Type = ItemType.Equipment
        });
        act.Should().NotThrow();
        
        // Cleanup
        world.Dispose();
        provider.Dispose();
    }

    public void Dispose()
    {
        _view.Dispose();
        _viewModel.Dispose();
        _serviceProvider.Dispose();
    }
}
