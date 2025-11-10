using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Arch.Core;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Shared.Events;
using ReactiveUI;

namespace PigeonPea.Shared.ViewModels;

/// <summary>
/// Central view model that owns all other view models and orchestrates updates.
/// Provides reactive property notifications for the entire game state.
/// </summary>
public class GameViewModel : ReactiveObject, IDisposable
{
    private const int UpdateIntervalMs = 16;
    
    private readonly GameWorld _world;
    private readonly IServiceProvider _services;
    private readonly CompositeDisposable _subscriptions;
    private readonly IScheduler _scheduler;
    private readonly IPublisher<ItemUsedEvent> _itemUsedPublisher;
    private readonly IPublisher<ItemDroppedEvent> _itemDroppedPublisher;
    private int _fps;

    /// <summary>
    /// Current frames per second for performance monitoring.
    /// </summary>
    public int Fps
    {
        get => _fps;
        set => this.RaiseAndSetIfChanged(ref _fps, value);
    }

    /// <summary>
    /// ViewModel for player state.
    /// </summary>
    public PlayerViewModel Player { get; }

    /// <summary>
    /// ViewModel for inventory state.
    /// </summary>
    public InventoryViewModel Inventory { get; }

    /// <summary>
    /// ViewModel for message log state.
    /// </summary>
    public MessageLogViewModel MessageLog { get; }

    /// <summary>
    /// ViewModel for map state.
    /// </summary>
    public MapViewModel Map { get; }

    /// <summary>
    /// Command to use the selected item from inventory.
    /// Can only execute when an item is selected.
    /// </summary>
    public ReactiveCommand<Unit, Unit> UseItemCommand { get; }

    /// <summary>
    /// Command to drop the selected item from inventory.
    /// Can only execute when an item is selected.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DropItemCommand { get; }

    /// <summary>
    /// Initializes a new instance of the GameViewModel.
    /// </summary>
    /// <param name="world">The game world instance.</param>
    /// <param name="services">The service provider for dependency injection.</param>
    /// <param name="scheduler">The scheduler for the update loop. If null, uses the default scheduler.</param>
    public GameViewModel(GameWorld world, IServiceProvider services, IScheduler? scheduler = null)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _scheduler = scheduler ?? DefaultScheduler.Instance;
        _subscriptions = new CompositeDisposable();

        // Initialize child view models using dependency injection
        Player = _services.GetRequiredService<PlayerViewModel>();
        Inventory = _services.GetRequiredService<InventoryViewModel>();
        MessageLog = _services.GetRequiredService<MessageLogViewModel>();
        Map = _services.GetRequiredService<MapViewModel>();

        // Get MessagePipe publishers
        _itemUsedPublisher = _services.GetRequiredService<IPublisher<ItemUsedEvent>>();
        _itemDroppedPublisher = _services.GetRequiredService<IPublisher<ItemDroppedEvent>>();

        // Set up commands with CanExecute based on item selection
        // Watch SelectedIndex - commands can execute when an item is selected (index >= 0)
        var canExecuteItemAction = this.WhenAnyValue(
            x => x.Inventory.SelectedIndex,
            selectedIndex => selectedIndex >= 0);

        UseItemCommand = ReactiveCommand.Create(UseItem, canExecuteItemAction);
        DropItemCommand = ReactiveCommand.Create(DropItem, canExecuteItemAction);

        // Set up update loop
        InitializeUpdateLoop();
    }

    /// <summary>
    /// Initializes the reactive update loop that synchronizes view models with game state.
    /// </summary>
    private void InitializeUpdateLoop()
    {
        // Update all view models at 60 FPS (16ms interval)
        _subscriptions.Add(Observable
            .Interval(TimeSpan.FromMilliseconds(UpdateIntervalMs), _scheduler)
            .Subscribe(_ => UpdateViewModels()));
    }

    /// <summary>
    /// Updates all child view models from the game world state.
    /// </summary>
    private void UpdateViewModels()
    {
        Player.Update(_world.EcsWorld, _world.PlayerEntity);
        Inventory.Update(_world.EcsWorld, _world.PlayerEntity);
        MessageLog.Update(_world);
        Map.Update(_world);
    }

    /// <summary>
    /// Uses the currently selected item from inventory.
    /// </summary>
    private void UseItem()
    {
        var selectedItem = Inventory.SelectedItem!;

        // Try to use the item in the game world
        if (_world.TryUseItem(Inventory.SelectedIndex))
        {
            // Publish event
            _itemUsedPublisher.Publish(new ItemUsedEvent
            {
                ItemName = selectedItem.Name,
                ItemType = selectedItem.Type.ToString()
            });
        }
    }

    /// <summary>
    /// Drops the currently selected item from inventory.
    /// </summary>
    private void DropItem()
    {
        var selectedItem = Inventory.SelectedItem!;

        // Try to drop the item in the game world
        if (_world.TryDropItem(Inventory.SelectedIndex))
        {
            // Publish event
            _itemDroppedPublisher.Publish(new ItemDroppedEvent
            {
                ItemName = selectedItem.Name
            });
        }
    }

    /// <summary>
    /// Disposes of resources and subscriptions.
    /// </summary>
    public void Dispose()
    {
        _subscriptions?.Dispose();
    }
}
