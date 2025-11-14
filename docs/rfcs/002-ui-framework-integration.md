---
doc_id: "RFC-2025-00002"
title: "UI Framework Integration"
doc_type: "rfc"
status: "draft"
canonical: true
created: "2025-11-08"
tags: ["ui", "mvvm", "reactive", "framework", "architecture"]
summary: "Integrate reactive programming frameworks (System.Reactive, ReactiveUI, MessagePipe, ObservableCollections) to create a clean MVVM architecture with automatic UI updates from game state changes"
supersedes: []
related: ["RFC-2025-00001"]
---

# RFC-002: UI Framework Integration

## Status

**Status**: Draft
**Created**: 2025-11-08
**Author**: Development Team

## Summary

Integrate reactive programming frameworks (System.Reactive, ReactiveUI, MessagePipe, ObservableCollections) into Pigeon Pea to create a clean MVVM architecture with automatic UI updates from game state changes and event-driven communication between systems.

## Motivation

Currently, the UI layer directly polls game state and manually updates UI elements. This approach has several issues:

1. **Tight Coupling**: UI code is tightly coupled to game state structure
2. **Manual Updates**: Developers must remember to update UI when game state changes
3. **Performance**: Inefficient full-screen redraws even for small changes
4. **Code Duplication**: Similar UI logic duplicated across Windows and Console apps

### Goals

1. **Reactive Data Flow**: Game state changes automatically propagate to UI
2. **Shared View Models**: Platform-agnostic view models in `shared-app`
3. **Type Safety**: Compile-time safety for data bindings
4. **Performance**: Efficient change notifications and minimal GC pressure
5. **Testability**: Easy to test view models in isolation

## Design

### Architecture Overview

```
┌──────────────────────────────────────────────────────┐
│                   Game Logic Layer                   │
│                    (GameWorld.cs)                    │
│         Arch ECS + GoRogue + Game Systems            │
└─────────────────────┬────────────────────────────────┘
                      │
                      │ Entity/Component Updates
                      ▼
┌──────────────────────────────────────────────────────┐
│               View Model Layer (Shared)              │
│                  (PigeonPea.Shared)                  │
│                                                       │
│  ┌────────────────────────────────────────────────┐ │
│  │ GameViewModel (ReactiveObject)                 │ │
│  │  - PlayerViewModel                             │ │
│  │  - InventoryViewModel                          │ │
│  │  - MessageLogViewModel                         │ │
│  │  - MapViewModel                                │ │
│  └────────────────────────────────────────────────┘ │
│                                                       │
│  ┌────────────────────────────────────────────────┐ │
│  │ Event Bus (MessagePipe)                        │ │
│  │  - PlayerDamagedEvent                          │ │
│  │  - ItemPickedUpEvent                           │ │
│  │  - EnemyDefeatedEvent                          │ │
│  └────────────────────────────────────────────────┘ │
│                                                       │
│  ┌────────────────────────────────────────────────┐ │
│  │ Observable Collections                         │ │
│  │  - ObservableList<ItemViewModel>               │ │
│  │  - ObservableList<MessageViewModel>            │ │
│  └────────────────────────────────────────────────┘ │
└─────────────────────┬────────────────────────────────┘
                      │
                      │ Property Change Notifications
                      │ IObservable<T> Streams
                      │ MessagePipe Events
                      ▼
┌──────────────────────────────────────────────────────┐
│              Platform-Specific Views                 │
│                                                       │
│  ┌───────────────────┐      ┌────────────────────┐  │
│  │   Windows App     │      │   Console App      │  │
│  │   (Avalonia)      │      │   (Terminal.Gui)   │  │
│  │                   │      │                    │  │
│  │  - XAML Bindings  │      │  - Manual Bindings │  │
│  │  - ReactiveUI     │      │  - Subscribe to    │  │
│  │    Commands       │      │    IObservable<T>  │  │
│  └───────────────────┘      └────────────────────┘  │
└──────────────────────────────────────────────────────┘
```

### Technology Stack

#### System.Reactive (Rx.NET)

**Purpose**: Foundation for reactive programming

**Key Features**:

- `IObservable<T>` / `IObserver<T>` interfaces
- LINQ operators for event streams
- Schedulers for controlling concurrency
- Composition of asynchronous operations

**Usage Example**:

```csharp
// Stream of player health changes
IObservable<int> healthChanges = Observable
    .Interval(TimeSpan.FromMilliseconds(16)) // 60 FPS
    .Select(_ => GetPlayerHealth())
    .DistinctUntilChanged();

// Subscribe to updates
healthChanges.Subscribe(health =>
{
    Console.WriteLine($"Health: {health}");
});
```

#### ReactiveUI

**Purpose**: MVVM framework with reactive bindings

**Key Features**:

- `ReactiveObject` base class for view models
- Property change notifications via `RaiseAndSetIfChanged`
- `ReactiveCommand` for UI actions
- `WhenAnyValue` for observing property changes
- Platform integration (Avalonia, WPF, etc.)

**Usage Example**:

```csharp
public class PlayerViewModel : ReactiveObject
{
    private int _health;
    public int Health
    {
        get => _health;
        set => this.RaiseAndSetIfChanged(ref _health, value);
    }

    // Automatically recomputes when Health changes
    public string HealthDisplay => this
        .WhenAnyValue(x => x.Health, x => x.MaxHealth)
        .Select(values => $"{values.Item1}/{values.Item2}")
        .ToProperty(this, x => x.HealthDisplay);
}
```

#### MessagePipe (Cysharp)

**Purpose**: High-performance in-memory message bus for event-driven architecture

**Key Features**:

- Zero-allocation pub/sub messaging
- Type-safe message contracts
- Filter support for conditional message handling
- Request/response pattern support
- Async messaging support
- Scoped message publishing (global, scoped, singleton)
- Integration with DI containers

**Usage Example**:

```csharp
// Define game events
public class PlayerDamagedEvent
{
    public int Damage { get; init; }
    public int RemainingHealth { get; init; }
    public string Source { get; init; }
}

public class ItemPickedUpEvent
{
    public string ItemName { get; init; }
    public ItemType Type { get; init; }
}

// Subscribe to events
public class MessageLogViewModel : ReactiveObject
{
    private readonly ISubscriber<PlayerDamagedEvent> _subscriber;

    public MessageLogViewModel(ISubscriber<PlayerDamagedEvent> subscriber)
    {
        _subscriber = subscriber;

        // Subscribe to player damage events
        var bag = DisposableBag.CreateBuilder();
        _subscriber.Subscribe(e =>
        {
            AddMessage($"Took {e.Damage} damage from {e.Source}! ({e.RemainingHealth} HP remaining)",
                MessageType.Combat);
        }).AddTo(bag);
    }
}

// Publish events from game systems
public class CombatSystem
{
    private readonly IPublisher<PlayerDamagedEvent> _publisher;

    public CombatSystem(IPublisher<PlayerDamagedEvent> publisher)
    {
        _publisher = publisher;
    }

    public void DamagePlayer(int damage, string source)
    {
        // Apply damage...
        var remainingHealth = player.Health.Current;

        // Publish event
        _publisher.Publish(new PlayerDamagedEvent
        {
            Damage = damage,
            RemainingHealth = remainingHealth,
            Source = source
        });
    }
}
```

#### ObservableCollections (Cysharp)

**Purpose**: High-performance observable collections for games

**Key Features**:

- Minimal GC allocations
- Change notifications with detailed diffs
- Filter/Sort/Transform views
- Optimized for game scenarios

**Usage Example**:

```csharp
var inventory = new ObservableList<ItemViewModel>();

// Subscribe to changes
inventory.ObserveAdd().Subscribe(e =>
{
    Console.WriteLine($"Item added: {e.Value.Name}");
});

inventory.ObserveRemove().Subscribe(e =>
{
    Console.WriteLine($"Item removed: {e.Value.Name}");
});

// Add item
inventory.Add(new ItemViewModel { Name = "Sword", Damage = 10 });
```

### Dependency Injection Setup

MessagePipe and other reactive services need to be registered with the DI container.

**Program.cs (Console App)**:

```csharp
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Add MessagePipe
services.AddMessagePipe();

// Add game services
services.AddSingleton<GameWorld>();
services.AddSingleton<GameViewModel>();
services.AddSingleton<PlayerViewModel>();
services.AddSingleton<InventoryViewModel>();
services.AddSingleton<MessageLogViewModel>();

// Add game systems with event publishing
services.AddSingleton<CombatSystem>();
services.AddSingleton<InventorySystem>();

var provider = services.BuildServiceProvider();

// Run application
var app = provider.GetRequiredService<GameApplication>();
app.Run();
```

**Program.cs (Windows App)**:

```csharp
public class App : Application
{
    public IServiceProvider Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var services = new ServiceCollection();

        // Add MessagePipe
        services.AddMessagePipe();

        // Add view models
        services.AddSingleton<GameWorld>();
        services.AddSingleton<GameViewModel>();

        Services = services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<GameViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

### Event Definitions

Define game events in `PigeonPea.Shared/Events/`:

```csharp
namespace PigeonPea.Shared.Events
{
    // Combat events
    public readonly struct PlayerDamagedEvent
    {
        public int Damage { get; init; }
        public int RemainingHealth { get; init; }
        public string Source { get; init; }
    }

    public readonly struct EnemyDefeatedEvent
    {
        public string EnemyName { get; init; }
        public int ExperienceGained { get; init; }
    }

    // Inventory events
    public readonly struct ItemPickedUpEvent
    {
        public string ItemName { get; init; }
        public ItemType Type { get; init; }
    }

    public readonly struct ItemUsedEvent
    {
        public string ItemName { get; init; }
        public ItemType Type { get; init; }
    }

    public readonly struct ItemDroppedEvent
    {
        public string ItemName { get; init; }
    }

    // Level events
    public readonly struct PlayerLevelUpEvent
    {
        public int NewLevel { get; init; }
        public int HealthIncrease { get; init; }
    }

    // Map events
    public readonly struct DoorOpenedEvent
    {
        public Position Position { get; init; }
    }

    public readonly struct StairsDescendedEvent
    {
        public int NewFloor { get; init; }
    }
}
```

### Shared View Models

All view models live in `PigeonPea.Shared/ViewModels/` and are platform-agnostic.

#### GameViewModel

Central view model that owns all other view models.

```csharp
namespace PigeonPea.Shared.ViewModels
{
    public class GameViewModel : ReactiveObject
    {
        private readonly GameWorld _world;

        public GameViewModel(GameWorld world)
        {
            _world = world;

            Player = new PlayerViewModel(world);
            Inventory = new InventoryViewModel(world);
            MessageLog = new MessageLogViewModel(world);
            Map = new MapViewModel(world);

            // Set up reactive subscriptions
            InitializeSubscriptions();
        }

        public PlayerViewModel Player { get; }
        public InventoryViewModel Inventory { get; }
        public MessageLogViewModel MessageLog { get; }
        public MapViewModel Map { get; }

        private void InitializeSubscriptions()
        {
            // Update view models when game state changes
            Observable
                .Interval(TimeSpan.FromMilliseconds(16)) // 60 FPS
                .Subscribe(_ =>
                {
                    Player.Update();
                    Inventory.Update();
                    MessageLog.Update();
                    Map.Update();
                });
        }
    }
}
```

#### PlayerViewModel

View model for player state.

```csharp
namespace PigeonPea.Shared.ViewModels
{
    public class PlayerViewModel : ReactiveObject
    {
        private readonly GameWorld _world;

        private int _health;
        private int _maxHealth;
        private int _level;
        private int _experience;
        private string _name;
        private Position _position;

        public PlayerViewModel(GameWorld world)
        {
            _world = world;
            Update();
        }

        // Properties with change notifications
        public int Health
        {
            get => _health;
            set => this.RaiseAndSetIfChanged(ref _health, value);
        }

        public int MaxHealth
        {
            get => _maxHealth;
            set => this.RaiseAndSetIfChanged(ref _maxHealth, value);
        }

        public int Level
        {
            get => _level;
            set => this.RaiseAndSetIfChanged(ref _level, value);
        }

        public int Experience
        {
            get => _experience;
            set => this.RaiseAndSetIfChanged(ref _experience, value);
        }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public Position Position
        {
            get => _position;
            set => this.RaiseAndSetIfChanged(ref _position, value);
        }

        // Computed properties
        public string HealthDisplay => $"{Health}/{MaxHealth}";
        public double HealthPercentage => MaxHealth > 0 ? (double)Health / MaxHealth : 0;
        public string LevelDisplay => $"Level {Level}";

        // Update from ECS
        internal void Update()
        {
            var playerEntity = _world.GetPlayerEntity();
            if (playerEntity == null) return;

            var health = _world.GetComponent<Health>(playerEntity.Value);
            Health = health.Current;
            MaxHealth = health.Maximum;

            var playerComp = _world.GetComponent<PlayerComponent>(playerEntity.Value);
            Name = playerComp.Name;
            Level = playerComp.Level;
            Experience = playerComp.Experience;

            var position = _world.GetComponent<Components.Position>(playerEntity.Value);
            Position = position.Point;
        }
    }
}
```

#### InventoryViewModel

View model for inventory with observable collections.

```csharp
namespace PigeonPea.Shared.ViewModels
{
    public class InventoryViewModel : ReactiveObject
    {
        private readonly GameWorld _world;

        public InventoryViewModel(GameWorld world)
        {
            _world = world;
            Items = new ObservableList<ItemViewModel>();
            Update();
        }

        public ObservableList<ItemViewModel> Items { get; }

        private int _selectedIndex = -1;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set => this.RaiseAndSetIfChanged(ref _selectedIndex, value);
        }

        public ItemViewModel SelectedItem =>
            SelectedIndex >= 0 && SelectedIndex < Items.Count
                ? Items[SelectedIndex]
                : null;

        internal void Update()
        {
            var playerEntity = _world.GetPlayerEntity();
            if (playerEntity == null) return;

            var inventory = _world.GetComponent<Inventory>(playerEntity.Value);

            // Sync items with ECS
            Items.Clear();
            foreach (var item in inventory.Items)
            {
                Items.Add(new ItemViewModel
                {
                    Name = item.Name,
                    Description = item.Description,
                    Type = item.Type,
                    Equipped = item.Equipped,
                });
            }
        }
    }

    public class ItemViewModel : ReactiveObject
    {
        private string _name;
        private string _description;
        private ItemType _type;
        private bool _equipped;

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => this.RaiseAndSetIfChanged(ref _description, value);
        }

        public ItemType Type
        {
            get => _type;
            set => this.RaiseAndSetIfChanged(ref _type, value);
        }

        public bool Equipped
        {
            get => _equipped;
            set => this.RaiseAndSetIfChanged(ref _equipped, value);
        }

        public string DisplayName =>
            Equipped ? $"{Name} (equipped)" : Name;
    }
}
```

#### MessageLogViewModel

View model for game messages.

```csharp
namespace PigeonPea.Shared.ViewModels
{
    public class MessageLogViewModel : ReactiveObject
    {
        private readonly GameWorld _world;
        private const int MaxMessages = 100;

        public MessageLogViewModel(GameWorld world)
        {
            _world = world;
            Messages = new ObservableList<MessageViewModel>();
        }

        public ObservableList<MessageViewModel> Messages { get; }

        public void AddMessage(string text, MessageType type = MessageType.Info)
        {
            var message = new MessageViewModel
            {
                Text = text,
                Type = type,
                Timestamp = DateTime.Now,
            };

            Messages.Add(message);

            // Keep only recent messages
            while (Messages.Count > MaxMessages)
            {
                Messages.RemoveAt(0);
            }
        }

        internal void Update()
        {
            // Poll for new messages from game world
            var newMessages = _world.GetRecentMessages();
            foreach (var msg in newMessages)
            {
                AddMessage(msg.Text, msg.Type);
            }
        }
    }

    public class MessageViewModel : ReactiveObject
    {
        public string Text { get; set; }
        public MessageType Type { get; set; }
        public DateTime Timestamp { get; set; }

        public string FormattedMessage =>
            $"[{Timestamp:HH:mm:ss}] {Text}";
    }

    public enum MessageType
    {
        Info,
        Combat,
        Warning,
        Error,
    }
}
```

#### MapViewModel

View model for map state.

```csharp
namespace PigeonPea.Shared.ViewModels
{
    public class MapViewModel : ReactiveObject
    {
        private readonly GameWorld _world;

        private int _width;
        private int _height;
        private Position _cameraPosition;

        public MapViewModel(GameWorld world)
        {
            _world = world;
            VisibleTiles = new ObservableList<TileViewModel>();
            Update();
        }

        public int Width
        {
            get => _width;
            set => this.RaiseAndSetIfChanged(ref _width, value);
        }

        public int Height
        {
            get => _height;
            set => this.RaiseAndSetIfChanged(ref _height, value);
        }

        public Position CameraPosition
        {
            get => _cameraPosition;
            set => this.RaiseAndSetIfChanged(ref _cameraPosition, value);
        }

        public ObservableList<TileViewModel> VisibleTiles { get; }

        internal void Update()
        {
            Width = _world.Map.Width;
            Height = _world.Map.Height;

            // Center camera on player
            var player = _world.GetPlayerEntity();
            if (player.HasValue)
            {
                var pos = _world.GetComponent<Components.Position>(player.Value);
                CameraPosition = pos.Point;
            }

            // Update visible tiles
            UpdateVisibleTiles();
        }

        private void UpdateVisibleTiles()
        {
            VisibleTiles.Clear();

            // Query ECS for visible entities
            var query = _world.Query<Components.Position, Renderable>();
            foreach (var entity in query)
            {
                ref var pos = ref entity.t1;
                ref var renderable = ref entity.t2;

                VisibleTiles.Add(new TileViewModel
                {
                    X = pos.Point.X,
                    Y = pos.Point.Y,
                    Glyph = renderable.Glyph,
                    Foreground = renderable.ForegroundColor,
                    Background = renderable.BackgroundColor,
                });
            }
        }
    }

    public class TileViewModel : ReactiveObject
    {
        public int X { get; set; }
        public int Y { get; set; }
        public char Glyph { get; set; }
        public Color Foreground { get; set; }
        public Color Background { get; set; }
    }
}
```

### Platform-Specific Integration

#### Windows App (Avalonia + ReactiveUI)

Avalonia has first-class ReactiveUI support.

**MainWindow.axaml**:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:PigeonPea.Shared.ViewModels"
        x:Class="PigeonPea.Windows.MainWindow"
        Title="Pigeon Pea">

    <Design.DataContext>
        <vm:GameViewModel/>
    </Design.DataContext>

    <Grid RowDefinitions="*,Auto">
        <!-- Game Canvas -->
        <local:GameCanvas Grid.Row="0" />

        <!-- HUD -->
        <StackPanel Grid.Row="1" Orientation="Horizontal">
            <!-- Player Info -->
            <TextBlock Text="{Binding Player.Name}" />
            <TextBlock Text="{Binding Player.HealthDisplay}" />
            <ProgressBar Value="{Binding Player.HealthPercentage}" />
            <TextBlock Text="{Binding Player.LevelDisplay}" />
        </StackPanel>
    </Grid>
</Window>
```

**MainWindow.axaml.cs**:

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var world = new GameWorld();
        DataContext = new GameViewModel(world);
    }
}
```

**Reactive Commands**:

```csharp
public class GameViewModel : ReactiveObject
{
    public ReactiveCommand<Unit, Unit> UseItemCommand { get; }
    public ReactiveCommand<Unit, Unit> DropItemCommand { get; }

    public GameViewModel(GameWorld world)
    {
        // Command can execute only when an item is selected
        var canUseItem = this.WhenAnyValue(x => x.Inventory.SelectedItem)
            .Select(item => item != null);

        UseItemCommand = ReactiveCommand.Create(
            () => UseSelectedItem(),
            canUseItem
        );

        DropItemCommand = ReactiveCommand.Create(
            () => DropSelectedItem(),
            canUseItem
        );
    }
}
```

#### Console App (Terminal.Gui + System.Reactive)

Terminal.Gui doesn't have built-in ReactiveUI support, but we can manually subscribe to observables.

**GameApplication.cs**:

```csharp
public class GameApplication
{
    private GameViewModel _viewModel;
    private PlayerView _playerView;
    private InventoryView _inventoryView;
    private MessageLogView _messageLogView;

    public void Run()
    {
        var world = new GameWorld();
        _viewModel = new GameViewModel(world);

        Application.Init();

        var top = Application.Top;

        // Create views
        _playerView = new PlayerView(_viewModel.Player);
        _inventoryView = new InventoryView(_viewModel.Inventory);
        _messageLogView = new MessageLogView(_viewModel.MessageLog);

        top.Add(_playerView, _inventoryView, _messageLogView);

        Application.Run();
    }
}
```

**PlayerView.cs**:

```csharp
public class PlayerView : FrameView
{
    private readonly PlayerViewModel _viewModel;
    private readonly Label _healthLabel;
    private readonly Label _levelLabel;
    private readonly CompositeDisposable _subscriptions;

    public PlayerView(PlayerViewModel viewModel)
    {
        _viewModel = viewModel;
        _subscriptions = new CompositeDisposable();

        Title = "Player";

        _healthLabel = new Label { X = 1, Y = 1 };
        _levelLabel = new Label { X = 1, Y = 2 };

        Add(_healthLabel, _levelLabel);

        // Subscribe to property changes
        _viewModel.WhenAnyValue(x => x.HealthDisplay)
            .Subscribe(health => _healthLabel.Text = $"Health: {health}")
            .DisposeWith(_subscriptions);

        _viewModel.WhenAnyValue(x => x.LevelDisplay)
            .Subscribe(level => _levelLabel.Text = level)
            .DisposeWith(_subscriptions);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _subscriptions?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

**InventoryView.cs**:

```csharp
public class InventoryView : FrameView
{
    private readonly InventoryViewModel _viewModel;
    private readonly ListView _listView;
    private readonly CompositeDisposable _subscriptions;

    public InventoryView(InventoryViewModel viewModel)
    {
        _viewModel = viewModel;
        _subscriptions = new CompositeDisposable();

        Title = "Inventory";

        _listView = new ListView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        Add(_listView);

        // Subscribe to collection changes
        _viewModel.Items.ObserveAdd()
            .Subscribe(_ => UpdateListView())
            .DisposeWith(_subscriptions);

        _viewModel.Items.ObserveRemove()
            .Subscribe(_ => UpdateListView())
            .DisposeWith(_subscriptions);

        UpdateListView();
    }

    private void UpdateListView()
    {
        _listView.SetSource(_viewModel.Items.Select(i => i.DisplayName).ToList());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _subscriptions?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

### CLI Argument Handling

Use `System.CommandLine` for the console app to support renderer selection and other options.

```csharp
using System.CommandLine;

namespace PigeonPea.Console
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Pigeon Pea - Roguelike Dungeon Crawler");

            var rendererOption = new Option<string>(
                name: "--renderer",
                description: "Renderer to use (auto, kitty, sixel, braille, ascii)",
                getDefaultValue: () => "auto"
            );
            rootCommand.AddOption(rendererOption);

            var debugOption = new Option<bool>(
                name: "--debug",
                description: "Enable debug mode"
            );
            rootCommand.AddOption(debugOption);

            var widthOption = new Option<int?>(
                name: "--width",
                description: "Window width in characters"
            );
            rootCommand.AddOption(widthOption);

            var heightOption = new Option<int?>(
                name: "--height",
                description: "Window height in characters"
            );
            rootCommand.AddOption(heightOption);

            rootCommand.SetHandler(
                (renderer, debug, width, height) =>
                {
                    RunGame(renderer, debug, width, height);
                },
                rendererOption,
                debugOption,
                widthOption,
                heightOption
            );

            return await rootCommand.InvokeAsync(args);
        }

        static void RunGame(string renderer, bool debug, int? width, int? height)
        {
            var config = new GameConfig
            {
                RendererType = ParseRenderer(renderer),
                Debug = debug,
                Width = width,
                Height = height,
            };

            var app = new GameApplication(config);
            app.Run();
        }

        static RendererType ParseRenderer(string renderer) => renderer.ToLower() switch
        {
            "kitty" => RendererType.Kitty,
            "sixel" => RendererType.Sixel,
            "braille" => RendererType.Braille,
            "ascii" => RendererType.Ascii,
            "auto" => RendererType.Auto,
            _ => throw new ArgumentException($"Unknown renderer: {renderer}"),
        };
    }
}
```

## Package References

Add to `PigeonPea.Shared.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="System.Reactive" Version="6.0.1" />
  <PackageReference Include="ReactiveUI" Version="20.1.1" />
  <PackageReference Include="MessagePipe" Version="1.8.0" />
  <PackageReference Include="ObservableCollections" Version="3.0.1" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
</ItemGroup>
```

Add to `PigeonPea.Console.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="System.CommandLine" Version="2.0.0-rc.2.25502.107" />
  <PackageReference Include="Terminal.Gui" Version="2.0.0-develop.4611" />
</ItemGroup>
```

## Implementation Plan

### Phase 1: Add Reactive Infrastructure (Week 1)

1. Add NuGet packages to `shared-app` (System.Reactive, ReactiveUI, MessagePipe, ObservableCollections, DI)
2. Set up MessagePipe dependency injection
3. Create `Events/` directory and define game events
4. Create `ViewModels/` directory
5. Implement `GameViewModel`
6. Implement `PlayerViewModel`

### Phase 2: Implement Remaining View Models (Week 2)

1. Implement `InventoryViewModel` with event subscriptions
2. Implement `MessageLogViewModel` with event subscriptions
3. Implement `MapViewModel`
4. Update game systems to publish events via MessagePipe

### Phase 3: Windows App Integration (Week 3)

1. Update MainWindow XAML with bindings
2. Implement ReactiveCommands
3. Test UI updates
4. Remove old manual update code

### Phase 4: Console App Integration (Week 4)

1. Implement System.CommandLine arg parsing
2. Create view classes with reactive subscriptions
3. Update GameApplication to use view models
4. Test UI updates

### Phase 5: Testing & Optimization (Week 5)

1. Unit test view models
2. Integration test reactive bindings
3. Performance profiling
4. Memory leak detection (subscription disposal)

## Testing Strategy

### View Model Unit Tests

```csharp
[Fact]
public void PlayerViewModel_HealthChange_NotifiesProperty()
{
    var world = CreateTestWorld();
    var viewModel = new PlayerViewModel(world);

    var healthChanges = new List<int>();
    viewModel.WhenAnyValue(x => x.Health)
        .Subscribe(h => healthChanges.Add(h));

    // Change health in game world
    world.DamagePlayer(10);
    viewModel.Update();

    Assert.Equal(2, healthChanges.Count); // Initial + change
    Assert.Equal(100, healthChanges[0]);
    Assert.Equal(90, healthChanges[1]);
}
```

### Observable Collection Tests

```csharp
[Fact]
public void InventoryViewModel_AddItem_NotifiesCollection()
{
    var world = CreateTestWorld();
    var viewModel = new InventoryViewModel(world);

    var addEvents = new List<ItemViewModel>();
    viewModel.Items.ObserveAdd()
        .Subscribe(e => addEvents.Add(e.Value));

    world.AddItemToPlayer(new Item { Name = "Sword" });
    viewModel.Update();

    Assert.Single(addEvents);
    Assert.Equal("Sword", addEvents[0].Name);
}
```

## Performance Considerations

### Minimize Allocations

- Use `ref` and `in` parameters where possible
- Pool temporary objects
- Avoid LINQ in hot paths (use `foreach` instead)

### Efficient Updates

- Only update view models when game state actually changes
- Use `DistinctUntilChanged()` to avoid redundant notifications
- Batch collection changes when possible

### Subscription Management

- Always dispose subscriptions in view `Dispose()` methods
- Use `CompositeDisposable` to manage multiple subscriptions
- Be careful with closures capturing large objects

### Target Update Rate

- Update view models at 60 FPS (16ms)
- Use `Observable.Interval` with scheduler control
- Consider throttling for non-critical updates

## Migration Path

### Before (Manual Updates)

```csharp
public class MainWindow : Window
{
    private void UpdateHUD()
    {
        var player = _world.GetPlayer();
        var health = _world.GetComponent<Health>(player);
        _healthLabel.Text = $"{health.Current}/{health.Maximum}";
    }
}
```

### After (Reactive Bindings)

```csharp
public class MainWindow : Window
{
    public MainWindow()
    {
        DataContext = new GameViewModel(_world);
        // XAML binding handles updates automatically
    }
}
```

## Open Questions

1. **Update Strategy**: Should view models poll game state or should game state push to view models?
   - **Proposal**: Polling for now (simpler), push later if needed

2. **Thread Safety**: How to handle game logic on different thread than UI?
   - **Proposal**: Use `ObserveOn(RxApp.MainThreadScheduler)` for UI updates

3. **Large Maps**: How to handle large maps efficiently?
   - **Proposal**: Viewport-based culling, only update visible tiles

## References

- [System.Reactive Documentation](https://github.com/dotnet/reactive)
- [ReactiveUI Documentation](https://www.reactiveui.net/)
- [MessagePipe Documentation](https://github.com/Cysharp/MessagePipe)
- [ObservableCollections Documentation](https://github.com/Cysharp/ObservableCollections)
- [System.CommandLine Documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [Avalonia MVVM Guide](https://docs.avaloniaui.net/docs/concepts/the-mvvm-pattern/)
