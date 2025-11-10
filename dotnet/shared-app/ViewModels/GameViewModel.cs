using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
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
    /// Disposes of resources and subscriptions.
    /// </summary>
    public void Dispose()
    {
        _subscriptions?.Dispose();
    }
}
