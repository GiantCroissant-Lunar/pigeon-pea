---
doc_id: 'RFC-2025-00006'
title: 'Plugin System Architecture'
doc_type: 'rfc'
status: 'active'
canonical: true
created: '2025-11-10'
updated: '2025-11-13'
tags: ['plugins', 'architecture', 'extensibility', 'alc', 'modularity']
summary: 'Plugin system for PigeonPea based on the hyacinth-bean-base architecture, enabling runtime plugin discovery, loading, and lifecycle management with Assembly Load Context (ALC) isolation'
supersedes: []
related: ['RFC-2025-00005']
---

# RFC-006: Plugin System Architecture

- **Status:** Implemented
- **Author:** Claude Agent
- **Date:** 2025-11-10
- **Implemented:** 2025-11-13
- **Supersedes:** N/A
- **Related:** RFC-005 (Project Structure Reorganization)
- **Depends On:** RFC-005 Phase 2 (Contract projects must exist)

## Summary

Implement a plugin system for PigeonPea based on the hyacinth-bean-base architecture, enabling runtime plugin discovery, loading, and lifecycle management with Assembly Load Context (ALC) isolation.

## Motivation

### Current State

PigeonPea currently has monolithic applications with tightly coupled features:

- Rendering is hardcoded into each application
- Game features (AI, inventory, combat) are embedded in `PigeonPea.Shared`
- No way to add features without modifying core code
- Difficult to support platform-specific extensions

### Goals

1. **Runtime Extensibility** - Load features as plugins without recompiling
2. **Platform-Specific Rendering** - Different renderers per platform (Sixel, Kitty, SkiaSharp, DirectX)
3. **Feature Modularity** - Game features as optional plugins (AI, inventory, quests)
4. **Community Mods** - Enable user-created content and features
5. **Hot Reload** - Rapid development iteration (reload plugins without restart)

### Non-Goals

- Plugin security/sandboxing (future enhancement)
- Plugin versioning/compatibility (future enhancement)
- Plugin marketplace (future enhancement)

## Architecture Overview

### Core Components

```
┌─────────────────────────────────────────────────┐
│         Host Application (PigeonPea.Windows/Console)
│  ┌───────────────────────────────────────────┐ │
│  │  AddPluginSystem() - DI Registration     │ │
│  └───────────────────────────────────────────┘ │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│         PigeonPea.PluginSystem                  │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐     │
│  │ Plugin   │  │ Service  │  │ Event    │     │
│  │ Loader   │  │ Registry │  │ Bus      │     │
│  └──────────┘  └──────────┘  └──────────┘     │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│         Plugins (Isolated ALCs)                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐     │
│  │ AI       │  │ Renderer │  │ Inventory│     │
│  │ Plugin   │  │ Plugin   │  │ Plugin   │     │
│  └──────────┘  └──────────┘  └──────────┘     │
└─────────────────────────────────────────────────┘
```

## Detailed Design

### 1. Plugin Contracts (PigeonPea.Contracts)

Location: `app-essential/core/PigeonPea.Contracts/`

#### IPlugin Interface

```csharp
namespace PigeonPea.Contracts.Plugin;

public interface IPlugin
{
    string Id { get; }
    string Name { get; }
    string Version { get; }

    Task InitializeAsync(IPluginContext context, CancellationToken ct = default);
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
}
```

**Lifecycle:**

1. **InitializeAsync** - Register services, subscribe to events
2. **StartAsync** - Start background tasks, activate systems
3. **StopAsync** - Cleanup, unsubscribe, flush data

#### IPluginContext Interface

```csharp
namespace PigeonPea.Contracts.Plugin;

public interface IPluginContext
{
    IRegistry Registry { get; }           // Service registration
    IConfiguration Configuration { get; } // Host configuration
    ILogger Logger { get; }               // Plugin logger
    IPluginHost Host { get; }             // Host services
}
```

**Purpose:** Isolates ALC boundary - doesn't expose `IServiceCollection` directly

#### IRegistry Interface

```csharp
namespace PigeonPea.Contracts.Plugin;

public interface IRegistry
{
    void Register<TService>(TService implementation, ServiceMetadata metadata) where TService : class;
    void Register<TService>(TService implementation, int priority = 100) where TService : class;

    TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class;
    IEnumerable<TService> GetAll<TService>() where TService : class;

    bool IsRegistered<TService>() where TService : class;
    bool Unregister<TService>(TService implementation) where TService : class;
}

public enum SelectionMode
{
    One,              // Exactly one implementation required
    HighestPriority,  // Return highest priority (default)
    All               // Throw - use GetAll() instead
}

public class ServiceMetadata
{
    public int Priority { get; set; } = 100;  // Higher = preferred
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? PluginId { get; set; }
}
```

**Priority Guidelines:**

- Framework services: 1000+
- Game plugins: 100-500
- Utility plugins: 50-99

#### PluginManifest Class

```csharp
namespace PigeonPea.Contracts.Plugin;

public class PluginManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }

    public string? Description { get; init; }
    public string? Author { get; init; }

    // Multi-profile entry points: "dotnet.console", "dotnet.windows", etc.
    public Dictionary<string, string> EntryPoint { get; init; } = new();

    // Legacy single entry point (for backward compatibility)
    public string? EntryAssembly { get; init; }
    public string? EntryType { get; init; }

    public List<PluginDependency> Dependencies { get; init; } = new();
    public List<string> Capabilities { get; init; } = new();
    public List<string> SupportedProfiles { get; init; } = new();

    public int Priority { get; init; } = 100;
    public string? LoadStrategy { get; init; }  // "eager", "lazy", "explicit"
}

public class PluginDependency
{
    public required string Id { get; init; }
    public string? VersionRange { get; init; }
    public bool Optional { get; init; }
}
```

#### plugin.json Example

```json
{
  "id": "rendering-terminal-kitty",
  "name": "Kitty Graphics Renderer",
  "version": "1.0.0",
  "description": "Terminal renderer using Kitty graphics protocol",
  "author": "PigeonPea Team",
  "entryPoint": {
    "dotnet.console": "PigeonPea.Plugins.Rendering.Terminal.Kitty.dll,PigeonPea.Plugins.Rendering.Terminal.Kitty.KittyRendererPlugin"
  },
  "capabilities": ["renderer", "renderer:terminal", "graphics:kitty"],
  "supportedProfiles": ["dotnet.console"],
  "dependencies": [],
  "priority": 100
}
```

### 2. Plugin System (PigeonPea.PluginSystem)

Location: `app-essential/core/PigeonPea.PluginSystem/`

#### PluginLoader

```csharp
namespace PigeonPea.PluginSystem;

public class PluginLoader : IDisposable
{
    public async Task<int> DiscoverAndLoadAsync(
        IEnumerable<string> pluginPaths,
        CancellationToken ct = default)
    {
        // 1. Preload contract assemblies into Default ALC
        // 2. Scan directories for plugin.json files
        // 3. Parse manifests
        // 4. Resolve dependencies → load order
        // 5. For each plugin:
        //    a. Create PluginLoadContext (isolated ALC)
        //    b. Load plugin assembly
        //    c. Instantiate plugin class
        //    d. Initialize plugin (register services)
        //    e. Start plugin (activate systems)
        // 6. Return loaded count
    }

    public Task UnloadPluginAsync(string pluginId, CancellationToken ct = default);
    public Task ReloadPluginAsync(string pluginId, CancellationToken ct = default);
}
```

**Key Features:**

- **ALC Isolation** - Each plugin loads in its own `AssemblyLoadContext`
- **Dependency Resolution** - Topological sort for load order
- **Hot Reload** - Unload and reload plugins without restart (development mode)

#### ServiceRegistry

```csharp
namespace PigeonPea.PluginSystem;

public class ServiceRegistry : IRegistry
{
    private readonly Dictionary<Type, List<ServiceRegistration>> _services = new();

    public void Register<TService>(TService implementation, ServiceMetadata metadata)
    {
        // Store by runtime type name (cross-ALC compatible)
        var serviceType = typeof(TService);
        var registration = new ServiceRegistration
        {
            Implementation = implementation,
            Metadata = metadata
        };

        if (!_services.ContainsKey(serviceType))
            _services[serviceType] = new List<ServiceRegistration>();

        _services[serviceType].Add(registration);
        _services[serviceType] = _services[serviceType]
            .OrderByDescending(r => r.Metadata.Priority)
            .ToList();
    }

    public TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority)
    {
        // Get by priority or throw if mode requires specific count
    }
}
```

**Cross-ALC Type Matching:**

- Uses runtime type name matching, not reference equality
- Works across plugin assembly boundaries

#### EventBus

```csharp
namespace PigeonPea.PluginSystem;

public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    public void Subscribe<TEvent>(Func<TEvent, Task> handler)
    {
        var eventType = typeof(TEvent);
        if (!_subscribers.ContainsKey(eventType))
            _subscribers[eventType] = new List<Delegate>();

        _subscribers[eventType].Add(handler);
    }

    public async Task PublishAsync<TEvent>(TEvent evt, CancellationToken ct = default)
    {
        var eventType = typeof(TEvent);
        if (!_subscribers.TryGetValue(eventType, out var handlers))
            return;

        foreach (var handler in handlers)
        {
            if (handler is Func<TEvent, Task> typedHandler)
                await typedHandler(evt);
        }
    }
}
```

**Features:**

- Pub/sub messaging between plugins
- Type-safe event handling
- Async event handlers

#### DI Integration

```csharp
namespace PigeonPea.PluginSystem;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPluginSystem(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddSingleton<PluginRegistry>();
        services.AddSingleton<ServiceRegistry>();
        services.AddSingleton<EventBus>();

        services.AddSingleton<IPluginRegistry>(sp => sp.GetRequiredService<PluginRegistry>());
        services.AddSingleton<IRegistry>(sp =>
        {
            var registry = sp.GetRequiredService<ServiceRegistry>();
            var eventBus = sp.GetRequiredService<EventBus>();

            // Register EventBus in ServiceRegistry so plugins can access it
            registry.Register<IEventBus>(eventBus, new ServiceMetadata
            {
                Priority = 1000,  // Framework service
                Name = "EventBus",
                Version = "1.0.0"
            });

            return registry;
        });
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<EventBus>());

        services.AddSingleton<PluginLoader>();
        services.AddHostedService<PluginLoaderHostedService>();

        return services;
    }
}
```

### 3. Game Event Contracts (PigeonPea.Game.Contracts)

Location: `game-essential/core/PigeonPea.Game.Contracts/Events/`

```csharp
namespace PigeonPea.Game.Contracts.Events;

public record EntitySpawnedEvent
{
    public required int EntityId { get; init; }
    public required string EntityType { get; init; }
    public required Point Position { get; init; }
}

public record EntityMovedEvent
{
    public required int EntityId { get; init; }
    public required Point OldPosition { get; init; }
    public required Point NewPosition { get; init; }
}

public record CombatEvent
{
    public required int AttackerId { get; init; }
    public required int TargetId { get; init; }
    public required int DamageDealt { get; init; }
    public required bool IsHit { get; init; }
    public required bool IsKill { get; init; }
}
```

### 4. Rendering Contracts (PigeonPea.Game.Contracts)

Location: `game-essential/core/PigeonPea.Game.Contracts/Rendering/`

```csharp
namespace PigeonPea.Game.Contracts.Rendering;

public interface IRenderer
{
    string Id { get; }
    RenderingCapabilities Capabilities { get; }

    void Initialize(RenderContext context);
    void Render(GameState state);
    void Shutdown();
}

public enum RenderingCapabilities
{
    ANSI,           // Basic ANSI text
    Braille,        // Unicode braille
    Sixel,          // Sixel graphics
    Kitty,          // Kitty graphics protocol
    SkiaSharp,      // 2D canvas
    DirectX,        // 3D hardware
    Vulkan          // 3D cross-platform
}

public class RenderContext
{
    public int Width { get; set; }
    public int Height { get; set; }
    public IServiceProvider Services { get; set; }
}
```

## Plugin Implementation Example

### Example: Kitty Graphics Renderer Plugin

**Location:** `console-app/plugins/PigeonPea.Plugins.Rendering.Terminal.Kitty/`

```csharp
namespace PigeonPea.Plugins.Rendering.Terminal.Kitty;

public class KittyRendererPlugin : IPlugin
{
    private ILogger? _logger;
    private IEventBus? _eventBus;
    private KittyRenderer? _renderer;

    public string Id => "rendering-terminal-kitty";
    public string Name => "Kitty Graphics Renderer";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _logger = context.Logger;
        _eventBus = context.Registry.Get<IEventBus>();

        // Create and register renderer
        _renderer = new KittyRenderer(_logger);
        context.Registry.Register<IRenderer>(
            _renderer,
            new ServiceMetadata
            {
                Priority = 100,
                Name = "KittyRenderer",
                Version = "1.0.0"
            }
        );

        _logger.LogInformation("Kitty graphics renderer registered");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Kitty graphics renderer started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _renderer?.Shutdown();
        _logger?.LogInformation("Kitty graphics renderer stopped");
        return Task.CompletedTask;
    }
}
```

### Example: AI Plugin

**Location:** `game-essential/plugins/PigeonPea.Plugins.AI/`

```csharp
namespace PigeonPea.Plugins.AI;

public class AIPlugin : IPlugin
{
    private ILogger? _logger;
    private IEventBus? _eventBus;
    private AISystem? _aiSystem;

    public string Id => "ai";
    public string Name => "AI System";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _logger = context.Logger;
        _eventBus = context.Registry.Get<IEventBus>();

        // Register AI service
        _aiSystem = new AISystem(_logger);
        context.Registry.Register<IAIService>(_aiSystem, priority: 100);

        // Subscribe to game events
        _eventBus.Subscribe<EntitySpawnedEvent>(OnEntitySpawned);
        _eventBus.Subscribe<EntityMovedEvent>(OnEntityMoved);

        _logger.LogInformation("AI system initialized");
        return Task.CompletedTask;
    }

    private Task OnEntitySpawned(EntitySpawnedEvent evt)
    {
        _logger?.LogInformation("AI tracking new entity: {EntityId}", evt.EntityId);
        _aiSystem?.TrackEntity(evt.EntityId, evt.Position);
        return Task.CompletedTask;
    }

    private Task OnEntityMoved(EntityMovedEvent evt)
    {
        _aiSystem?.UpdateEntityPosition(evt.EntityId, evt.NewPosition);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default)
    {
        _aiSystem?.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _aiSystem?.Stop();
        return Task.CompletedTask;
    }
}
```

## Integration with GameWorld

```csharp
// In PigeonPea.Shared/GameWorld.cs
public class GameWorld
{
    private readonly IEventBus _eventBus;

    public GameWorld(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public void SpawnEntity(Entity entity)
    {
        // ... ECS entity creation ...

        // Publish event for plugins
        _eventBus.PublishAsync(new EntitySpawnedEvent
        {
            EntityId = entity.Id,
            EntityType = GetEntityType(entity),
            Position = entity.Get<Position>().Point
        }).Wait();  // Or await in async context
    }

    public void MoveEntity(Entity entity, Point newPosition)
    {
        var oldPosition = entity.Get<Position>().Point;
        entity.Get<Position>().Point = newPosition;

        // Publish event
        _eventBus.PublishAsync(new EntityMovedEvent
        {
            EntityId = entity.Id,
            OldPosition = oldPosition,
            NewPosition = newPosition
        }).Wait();
    }
}
```

## Application Startup

### Console App

```csharp
// In console-app/core/PigeonPea.Console/Program.cs
var builder = Host.CreateApplicationBuilder(args);

// Add plugin system
builder.Services.AddPluginSystem(builder.Configuration);

// Add game world with event bus
builder.Services.AddSingleton<GameWorld>(sp =>
{
    var eventBus = sp.GetRequiredService<IEventBus>();
    return new GameWorld(eventBus);
});

var host = builder.Build();

// Plugin loader will auto-discover and load from configured paths
await host.RunAsync();
```

### Plugin Manifest

```json
// console-app/configs/plugin-manifest.json
{
  "profile": "dotnet.console",
  "pluginPaths": ["../../app-essential/plugins", "../../game-essential/plugins", "./plugins"],
  "plugins": [
    {
      "id": "rendering-terminal-kitty",
      "enabled": true,
      "priority": 100
    },
    {
      "id": "ai",
      "enabled": true
    }
  ],
  "autoDiscovery": true
}
```

## Migration Strategy

### Phase 1: Core Plugin Infrastructure

**Tasks:**

1. Create `PigeonPea.Contracts/Plugin/`
   - `IPlugin.cs`, `IPluginContext.cs`, `IRegistry.cs`
   - `PluginManifest.cs`, `ServiceMetadata.cs`
2. Create `PigeonPea.PluginSystem/`
   - `PluginLoader.cs`, `ServiceRegistry.cs`, `EventBus.cs`
   - `ServiceCollectionExtensions.cs`
3. Unit tests for plugin system

**Deliverables:**

- Plugin contracts compile
- Plugin loader can discover and load simple test plugin
- Service registry works with priority selection
- Event bus publishes and subscribes

### Phase 2: Game Event Integration

**Tasks:**

1. Create `PigeonPea.Game.Contracts/Events/`
2. Add events: `EntitySpawnedEvent`, `EntityMovedEvent`, `CombatEvent`
3. Integrate `IEventBus` into `GameWorld`
4. Publish events from game actions

**Deliverables:**

- Events published from `GameWorld`
- Plugins can subscribe to game events
- No impact on existing functionality

### Phase 3: Rendering Plugin (Proof of Concept)

**Tasks:**

1. Create `PigeonPea.Game.Contracts/Rendering/IRenderer.cs`
2. Create `PigeonPea.Plugins.Rendering.Terminal.ANSI/` (simple fallback)
3. Update `PigeonPea.Console` to use plugin-based renderer
4. Test plugin loading and rendering

**Deliverables:**

- Console app loads renderer as plugin
- Rendering works identically to before
- Can swap renderers by changing config

### Phase 4: Additional Plugins (Optional)

**Tasks:**

1. Convert AI system to plugin
2. Convert inventory system to plugin
3. Create SkiaSharp renderer plugin for Windows

**Deliverables:**

- Multiple game system plugins working
- Platform-specific rendering plugins

## Testing Strategy

### Unit Tests

1. **PluginLoader Tests**
   - Discovery from directories
   - Manifest parsing
   - Dependency resolution
   - Load order calculation

2. **ServiceRegistry Tests**
   - Register/Get/Unregister
   - Priority selection
   - Cross-ALC type matching

3. **EventBus Tests**
   - Subscribe/Publish
   - Multiple subscribers
   - Async handlers

### Integration Tests

1. **Plugin Loading**
   - Load test plugin from disk
   - Initialize and start
   - Verify services registered
   - Unload and verify cleanup

2. **Event Flow**
   - GameWorld publishes event
   - Plugin receives event
   - Plugin responds correctly

3. **Renderer Plugin**
   - Load renderer plugin
   - Render game state
   - Verify output correct

## Performance Considerations

1. **Plugin Loading** - One-time cost at startup
2. **Service Registry** - O(1) lookup with priority caching
3. **Event Bus** - O(n) where n = number of subscribers (acceptable for game events)
4. **Cross-ALC Calls** - Minimal overhead for service access

**Mitigation:**

- Cache registry lookups
- Batch event publishing for high-frequency events
- Profile hot paths and optimize as needed

## Security Considerations

**Out of Scope for Initial Implementation:**

- Plugin sandboxing
- Permission system
- Code signing
- Malicious plugin protection

**Future Enhancement:**

- Add security layer from hyacinth-bean-base
- Implement permission profiles
- Add audit logging

## Alternatives Considered

### Alternative 1: MEF (Managed Extensibility Framework)

**Pros:**

- Built into .NET
- Mature and stable

**Cons:**

- Less control over loading
- No ALC isolation
- More complex attribute-based discovery

**Decision:** Rejected - want full control and ALC isolation

### Alternative 2: Simple Factory Pattern

**Pros:**

- Simpler implementation
- No plugin loading complexity

**Cons:**

- No runtime extensibility
- No community mods
- Features still coupled to core

**Decision:** Rejected - doesn't meet extensibility goals

## Success Criteria

1. ✅ Plugin loader discovers and loads plugins from disk
2. ✅ Plugins can register services with priority
3. ✅ Plugins can subscribe to game events
4. ✅ Renderer loads as plugin and works identically
5. ✅ Multiple plugins can coexist without conflicts
6. ✅ Documentation complete (README, examples)
7. ✅ Unit tests achieve >80% coverage
8. ✅ Integration tests verify end-to-end scenarios

## Timeline

- **Phase 1:** Core plugin infrastructure (3-5 days)
- **Phase 2:** Game event integration (2-3 days)
- **Phase 3:** Rendering plugin PoC (3-4 days)
- **Phase 4:** Additional plugins (optional, 1-2 days each)

**Total:** 8-12 days (1.5-2 weeks)

## Future Enhancements

- Plugin dependency management with version constraints
- Plugin marketplace/repository
- Hot reload in production (with safety checks)
- Plugin performance metrics
- Security sandboxing
- Plugin update mechanism
- Visual plugin configuration UI

## References

- [PLUGIN_SYSTEM_ANALYSIS.md](/home/user/pigeon-pea/PLUGIN_SYSTEM_ANALYSIS.md) - Detailed analysis
- [hyacinth-bean-base](https://github.com/GiantCroissant-Lunar/hyacinth-bean-base) - Reference implementation
- RFC-005: Project Structure Reorganization
- [Assembly Load Context Documentation](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
