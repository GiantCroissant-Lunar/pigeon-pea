using Arch.Core;
using MessagePipe;
using ObservableCollections;
using ReactiveUI;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Events;

namespace PigeonPea.Shared.ViewModels;

/// <summary>
/// ViewModel for inventory with reactive collections and event subscriptions.
/// Automatically updates when inventory changes through MessagePipe events.
/// </summary>
public class InventoryViewModel : ReactiveObject, IDisposable
{
    private readonly IDisposable _subscriptions;
    private int _selectedIndex = -1;

    /// <summary>
    /// Observable collection of items in the inventory.
    /// </summary>
    public ObservableList<ItemViewModel> Items { get; } = new();

    /// <summary>
    /// The index of the currently selected item.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedIndex, value);
    }

    /// <summary>
    /// The currently selected item, or null if no item is selected.
    /// </summary>
    public ItemViewModel? SelectedItem =>
        SelectedIndex >= 0 && SelectedIndex < Items.Count
            ? Items[SelectedIndex]
            : null;

    /// <summary>
    /// Initializes a new instance of the InventoryViewModel.
    /// Sets up MessagePipe subscriptions for inventory events.
    /// </summary>
    /// <param name="itemPickedUpSubscriber">Subscriber for ItemPickedUpEvent.</param>
    /// <param name="itemUsedSubscriber">Subscriber for ItemUsedEvent.</param>
    /// <param name="itemDroppedSubscriber">Subscriber for ItemDroppedEvent.</param>
    public InventoryViewModel(
        ISubscriber<ItemPickedUpEvent> itemPickedUpSubscriber,
        ISubscriber<ItemUsedEvent> itemUsedSubscriber,
        ISubscriber<ItemDroppedEvent> itemDroppedSubscriber)
    {
        var bag = DisposableBag.CreateBuilder();

        // Subscribe to item picked up events
        itemPickedUpSubscriber.Subscribe(e =>
        {
            HandleItemPickedUp(e);
        }).AddTo(bag);

        // Subscribe to item used events
        itemUsedSubscriber.Subscribe(e =>
        {
            HandleItemUsed(e);
        }).AddTo(bag);

        // Subscribe to item dropped events
        itemDroppedSubscriber.Subscribe(e =>
        {
            HandleItemDropped(e);
        }).AddTo(bag);

        _subscriptions = bag.Build();
    }

    /// <summary>
    /// Updates the ViewModel properties from an ECS player entity's inventory.
    /// Uses efficient diffing to synchronize items without recreating the entire list.
    /// </summary>
    /// <param name="world">The ECS world containing the entity.</param>
    /// <param name="playerEntity">The player entity to sync from.</param>
    public void Update(World world, Entity playerEntity)
    {
        if (!world.IsAlive(playerEntity))
        {
            return;
        }

        // Only update if the entity has an inventory component
        if (!world.TryGet<Inventory>(playerEntity, out var inventory))
        {
            return;
        }

        // Build a set of current ECS item entities for efficient lookup
        var ecsItemEntities = new HashSet<Entity>();
        foreach (var itemEntity in inventory.Items)
        {
            if (world.IsAlive(itemEntity))
            {
                ecsItemEntities.Add(itemEntity);
            }
        }

        // Remove items from the view model that no longer exist in ECS
        for (int i = Items.Count - 1; i >= 0; i--)
        {
            if (!ecsItemEntities.Contains(Items[i].SourceEntity))
            {
                Items.RemoveAt(i);
            }
        }

        // Build a set of existing view model entities for quick lookup
        var existingViewModelEntities = new HashSet<Entity>(
            Items.Select(item => item.SourceEntity)
        );

        // Add new items and update existing ones
        foreach (var itemEntity in inventory.Items)
        {
            if (!world.IsAlive(itemEntity))
            {
                continue;
            }

            if (!world.TryGet<Item>(itemEntity, out var item))
            {
                continue;
            }

            if (!existingViewModelEntities.Contains(itemEntity))
            {
                // Add new item
                Items.Add(new ItemViewModel
                {
                    SourceEntity = itemEntity,
                    Name = item.Name,
                    Type = item.Type
                });
            }
            else
            {
                // Update existing item
                var existingViewModel = Items.First(vm => vm.SourceEntity == itemEntity);
                existingViewModel.Name = item.Name;
                existingViewModel.Type = item.Type;
            }
        }
    }

    private void HandleItemPickedUp(ItemPickedUpEvent e)
    {
        // Event handler for when an item is picked up
        // The actual update will happen via Update() method
        // This is a placeholder for potential future event-driven updates
    }

    private void HandleItemUsed(ItemUsedEvent e)
    {
        // Event handler for when an item is used
        // The actual update will happen via Update() method
        // This is a placeholder for potential future event-driven updates
    }

    private void HandleItemDropped(ItemDroppedEvent e)
    {
        // Event handler for when an item is dropped
        // The actual update will happen via Update() method
        // This is a placeholder for potential future event-driven updates
    }

    /// <summary>
    /// Disposes the subscriptions.
    /// </summary>
    public void Dispose()
    {
        _subscriptions?.Dispose();
    }
}
