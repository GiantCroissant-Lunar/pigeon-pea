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

        // Clear and rebuild the items list from ECS
        Items.Clear();
        foreach (var itemEntity in inventory.Items)
        {
            if (world.IsAlive(itemEntity) && world.TryGet<Item>(itemEntity, out var item))
            {
                Items.Add(new ItemViewModel
                {
                    Name = item.Name,
                    Type = item.Type
                });
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
