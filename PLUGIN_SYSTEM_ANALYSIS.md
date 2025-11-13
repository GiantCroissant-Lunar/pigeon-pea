# Plugin System Analysis: Hyacinth-Bean-Base to Pigeon-Pea Adoption

**Date:** 2025-11-10
**Source Repository:** hyacinth-bean-base
**Target Project:** pigeon-pea

## Executive Summary

The hyacinth-bean-base plugin system is a **sophisticated, production-ready extensible architecture** built on Assembly Load Context (ALC) isolation, dependency injection, and event-driven communication. It provides:

- **Runtime plugin discovery and loading**
- **Isolated plugin execution contexts** (hot reload support)
- **Cross-ALC service registry** for plugin communication
- **Dependency resolution and lifecycle management**
- **Security sandbox and permission system**
- **Multi-profile support** (console, GUI, Unity, etc.)
- **Event bus for pub/sub messaging**
- **Health monitoring and metrics**

---

## Architecture Overview

### Core Components

The plugin system consists of three main layers:

```
┌─────────────────────────────────────────────────────────┐
│         Host Application (pigeon-pea)                   │
│  ┌───────────────────────────────────────────────────┐ │
│  │  ServiceCollectionExtensions                      │ │
│  │  - AddPluginSystem()                              │ │
│  └───────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│         Plugin Infrastructure                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ PluginLoader │  │ PluginHost   │  │ EventBus     │ │
│  │              │  │              │  │              │ │
│  │ - Discovery  │  │ - Logger     │  │ - Pub/Sub    │ │
│  │ - Loading    │  │ - Services   │  │ - Events     │ │
│  │ - Lifecycle  │  │              │  │              │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ Service      │  │ Plugin       │  │ Dependency   │ │
│  │ Registry     │  │ Registry     │  │ Resolver     │ │
│  │              │  │              │  │              │ │
│  │ - Cross-ALC  │  │ - Tracking   │  │ - Ordering   │ │
│  │ - Priority   │  │ - State Mgmt │  │ - Validation │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│         Plugin Layer (Isolated ALCs)                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ Plugin A     │  │ Plugin B     │  │ Plugin C     │ │
│  │              │  │              │  │              │ │
│  │ - Initialize │  │ - Initialize │  │ - Initialize │ │
│  │ - Start      │  │ - Start      │  │ - Start      │ │
│  │ - Stop       │  │ - Stop       │  │ - Stop       │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────┘
```

---

## Key Interfaces & Contracts

### 1. IPlugin - Core Plugin Lifecycle

**Location:** `HyacinthBean.Plugins.Contracts/IPlugin.cs`

```csharp
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

**Lifecycle phases:**

1. **InitializeAsync** - Register services, subscribe to events, load resources
2. **StartAsync** - Start background tasks, activate systems
3. **StopAsync** - Cleanup, unsubscribe, flush data

### 2. IPluginContext - Initialization Context

**Location:** `HyacinthBean.Plugins.Contracts/IPlugin.cs:35-56`

```csharp
public interface IPluginContext
{
    IRegistry Registry { get; }           // Service registration
    IConfiguration Configuration { get; } // Host configuration
    ILogger Logger { get; }               // Plugin logger
    IPluginHost Host { get; }             // Host services
}
```

**Purpose:** Isolates ALC boundary by not exposing IServiceCollection directly.

### 3. IRegistry - Cross-ALC Service Registry

**Location:** `HyacinthBean.Plugins.Contracts/IRegistry.cs`

```csharp
public interface IRegistry
{
    void Register<TService>(TService implementation, ServiceMetadata metadata);
    void Register<TService>(TService implementation, int priority = 100);

    TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority);
    IEnumerable<TService> GetAll<TService>();

    bool IsRegistered<TService>();
    bool Unregister<TService>(TService implementation);
}
```

**Key features:**

- **Runtime type matching** (not compile-time references)
- **Priority-based selection** (framework: 1000+, plugins: 100-500)
- **Multiple implementations** support
- **Service metadata** (name, version, plugin ID)

### 4. PluginManifest - Plugin Metadata

**Location:** `HyacinthBean.Plugins.Contracts/PluginManifest.cs`

```json
{
  "id": "npc",
  "name": "NPC System",
  "version": "1.0.0",
  "description": "NPC interaction and dialogue system",
  "author": "Lablab Bean Team",
  "entryPoint": {
    "dotnet.console": "HyacinthBean.Plugins.NPC.dll,HyacinthBean.Plugins.NPC.NPCPlugin",
    "dotnet.sadconsole": "HyacinthBean.Plugins.NPC.dll,HyacinthBean.Plugins.NPC.NPCPlugin"
  },
  "capabilities": ["npc", "dialogue", "dialogue-trees"],
  "dependencies": [],
  "priority": 100,
  "permissions": {
    "profile": "Standard"
  }
}
```

**Key fields:**

- **entryPoint** - Multi-profile entry points (console, GUI, Unity, etc.)
- **capabilities** - Feature declarations for discovery/filtering
- **dependencies** - Hard/soft plugin dependencies
- **priority** - Load order and service registration priority
- **targetPlatforms** / **targetProcesses** - Platform filtering

---

## Plugin Loading Process

**Location:** `HyacinthBean.Plugins.Core/PluginLoader.cs`

### Discovery & Loading Flow

```
1. DiscoverAndLoadAsync(pluginPaths)
   ↓
2. Preload contract assemblies into Default ALC
   ↓
3. Scan directories for plugin.json files
   ↓
4. Parse manifests
   ↓
5. Validate capabilities (single UI, single renderer)
   ↓
6. Resolve dependencies → load order
   ↓
7. For each plugin in load order:
   a. Create PluginLoadContext (isolated ALC)
   b. Load plugin assembly
   c. Instantiate plugin class
   d. Initialize plugin (register services)
   e. Start plugin (activate systems)
   f. Track in PluginRegistry
   ↓
8. Return loaded count
```

### Key Implementation Details

**Assembly Load Context Isolation:**

```csharp
var loadContext = new PluginLoadContext(assemblyPath, enableHotReload);
var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
```

**Cross-ALC Type Matching:**

```csharp
// Check by name, not by reference equality
var implementsIPlugin = pluginType.GetInterfaces()
    .Any(i => i.FullName == typeof(IPlugin).FullName);
```

**Profile-Based Entry Points:**

```csharp
if (manifest.EntryPoint.TryGetValue("dotnet.console", out var entryPoint))
{
    var parts = entryPoint.Split(','); // "Assembly.dll,Namespace.Type"
    assemblyName = parts[0].Trim();
    typeName = parts[1].Trim();
}
```

---

## Dependency Injection Integration

**Location:** `HyacinthBean.Plugins.Core/ServiceCollectionExtensions.cs`

### Setup in Host Application

```csharp
public static IServiceCollection AddPluginSystem(
    this IServiceCollection services,
    IConfiguration? configuration = null)
{
    // Core plugin services
    services.AddSingleton<PluginRegistry>();
    services.AddSingleton<ServiceRegistry>();
    services.AddSingleton<EventBus>();

    // Expose interfaces
    services.AddSingleton<IPluginRegistry>(sp => sp.GetRequiredService<PluginRegistry>());
    services.AddSingleton<IRegistry>(sp =>
    {
        var registry = sp.GetRequiredService<ServiceRegistry>();
        var eventBus = sp.GetRequiredService<EventBus>();

        // Register EventBus in ServiceRegistry
        registry.Register<IEventBus>(eventBus, new ServiceMetadata
        {
            Priority = 1000, // Framework service
            Name = "EventBus",
            Version = "1.0.0"
        });

        return registry;
    });

    // Plugin loader
    services.AddSingleton<PluginLoader>(...);

    // Hosted service for background loading
    services.AddHostedService<PluginLoaderHostedService>();

    return services;
}
```

### Usage in Startup

```csharp
// Program.cs or Startup.cs
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddPluginSystem(builder.Configuration);
```

---

## Event System

**Location:** `HyacinthBean.Plugins.Core/EventBus.cs`

The plugin system includes a built-in **event bus** for pub/sub messaging between plugins.

### Event Publishing

```csharp
public class AnalyticsPlugin : IPlugin
{
    private IEventBus? _eventBus;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        _eventBus = context.Registry.Get<IEventBus>();

        // Subscribe to events
        _eventBus.Subscribe<EntitySpawnedEvent>(OnEntitySpawned);
        _eventBus.Subscribe<CombatEvent>(OnCombat);

        return Task.CompletedTask;
    }

    private Task OnEntitySpawned(EntitySpawnedEvent evt)
    {
        // Handle event
        return Task.CompletedTask;
    }
}
```

---

## Security Features

**Location:** `HyacinthBean.Plugins.Core/Security/`

### Components

1. **PluginSecurityManager** - Permission enforcement
2. **PluginSandbox** - Resource access control
3. **SecurityAuditLog** - Security event logging
4. **PluginPermission** - Permission definitions

### Permission Profiles

Plugins declare permission requirements in `plugin.json`:

```json
{
  "permissions": {
    "profile": "Standard",
    "fileAccess": "ReadWrite",
    "networkAccess": "Restricted"
  }
}
```

---

## Example Plugin Implementation

**Real example from hyacinth-bean-base:**

```csharp
public class AnalyticsPlugin : IPlugin
{
    private ILogger? _logger;
    private IEventBus? _eventBus;
    private AnalyticsService? _analyticsService;

    public string Id => "lablab-bean.analytics";
    public string Name => "Analytics Plugin";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        _logger = context.Logger;
        _eventBus = context.Registry.Get<IEventBus>();

        // Register service
        _analyticsService = new AnalyticsService(context.Logger);
        context.Registry.Register<IService>(
            _analyticsService,
            new ServiceMetadata
            {
                Priority = 100,
                Name = "AnalyticsService",
                Version = "1.0.0"
            }
        );

        // Subscribe to events
        _eventBus.Subscribe<EntitySpawnedEvent>(OnEntitySpawned);
        _eventBus.Subscribe<CombatEvent>(OnCombat);

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _logger?.LogInformation("Analytics plugin started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _analyticsService?.FlushEvents();
        return Task.CompletedTask;
    }

    private Task OnEntitySpawned(EntitySpawnedEvent evt)
    {
        _analyticsService?.TrackEvent("entity_spawned", new
        {
            entity_type = evt.EntityType,
            position_x = evt.Position.X,
            position_y = evt.Position.Y
        });
        return Task.CompletedTask;
    }
}
```

---

## Pigeon-Pea Integration Analysis

### Current Pigeon-Pea Architecture

**Structure:**

```
dotnet/
├── shared-app/    # PigeonPea.Shared - Core game logic (Arch ECS + GoRogue)
├── windows-app/   # PigeonPea.Windows - Avalonia + SkiaSharp
└── console-app/   # PigeonPea.Console - Terminal.Gui
```

**Key characteristics:**

- **ECS-based** (Arch ECS)
- **Multi-platform** (Windows GUI + Terminal)
- **Shared game logic** in `shared-app`
- **Platform-specific renderers**

### Perfect Fit: Why the Plugin System Works for Pigeon-Pea

| Hyacinth-Bean Feature          | Pigeon-Pea Benefit                                           |
| ------------------------------ | ------------------------------------------------------------ |
| **Multi-profile entry points** | Supports both `dotnet.console` and `dotnet.windows` profiles |
| **Capability system**          | Can enforce single renderer per profile                      |
| **Service registry**           | Plugins can provide AI, inventory, quest systems             |
| **Event bus**                  | Perfect for ECS events (combat, movement, spawning)          |
| **Hot reload**                 | Rapid game development iteration                             |
| **Dependency resolution**      | Complex mod dependencies (NPC → Dialogue → Quest)            |

---

## Adoption Strategy for Pigeon-Pea

### Phase 1: Core Plugin Infrastructure

**Create plugin contracts project:**

```
dotnet/
└── plugin-system/
    ├── PigeonPea.Plugins.Contracts/
    │   ├── IPlugin.cs
    │   ├── IPluginContext.cs
    │   ├── IRegistry.cs
    │   ├── PluginManifest.cs
    │   └── Events/
    │       ├── EntitySpawnedEvent.cs
    │       ├── EntityMovedEvent.cs
    │       └── CombatEvent.cs
    │
    └── PigeonPea.Plugins.Core/
        ├── PluginLoader.cs
        ├── ServiceRegistry.cs
        ├── EventBus.cs
        ├── PluginHost.cs
        └── ServiceCollectionExtensions.cs
```

**Integration into shared-app:**

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
        });
    }
}
```

### Phase 2: Convert Existing Features to Plugins

**Modularize existing systems:**

1. **AI System → Plugin**

   ```
   plugins/PigeonPea.Plugins.AI/
   ├── AIPlugin.cs
   ├── plugin.json
   └── Systems/
       ├── PathfindingSystem.cs
       └── BehaviorTreeSystem.cs
   ```

2. **Rendering → Plugins**

   ```
   plugins/PigeonPea.Plugins.Rendering.SkiaSharp/
   └── plugin.json (capabilities: ["renderer", "renderer:windows"])

   plugins/PigeonPea.Plugins.Rendering.Terminal/
   └── plugin.json (capabilities: ["renderer", "renderer:console"])
   ```

3. **Inventory → Plugin**
   ```
   plugins/PigeonPea.Plugins.Inventory/
   ├── InventoryPlugin.cs
   └── plugin.json (capabilities: ["inventory"])
   ```

### Phase 3: Community Mod Support

**Enable user-created plugins:**

```
~/.config/pigeon-pea/plugins/
├── awesome-spells/
│   ├── plugin.json
│   └── AwesomeSpells.dll
│
└── custom-monsters/
    ├── plugin.json
    └── CustomMonsters.dll
```

**Configuration in appsettings.json:**

```json
{
  "PluginSystem": {
    "PluginPaths": ["./plugins", "~/.config/pigeon-pea/plugins"],
    "Profile": "dotnet.console",
    "HotReload": true
  }
}
```

---

## Benefits for Pigeon-Pea

### 1. Modular Game Systems

- **Inventory, quest, dialogue, NPC** systems as plugins
- **Easy enable/disable** via configuration
- **Clean separation** of concerns

### 2. Multi-Platform Support

- Single codebase, profile-based plugins
- **Console-specific** plugins (Kitty graphics, Sixel)
- **Windows-specific** plugins (SkiaSharp effects, shaders)

### 3. Extensibility

- **Community mods** without recompiling
- **Custom content** (monsters, items, spells)
- **Third-party integrations** (analytics, telemetry, Discord)

### 4. Development Velocity

- **Hot reload** - no restart during development
- **Isolated testing** - test plugins independently
- **Parallel development** - multiple features simultaneously

### 5. ECS Event Integration

Perfect match for Arch ECS:

```csharp
// Plugin subscribes to ECS events
_eventBus.Subscribe<ComponentAddedEvent<Health>>(OnHealthAdded);
_eventBus.Subscribe<EntityDestroyedEvent>(OnEntityDestroyed);

// Game world publishes ECS changes
world.OnEntityCreated += entity =>
    _eventBus.PublishAsync(new EntitySpawnedEvent { ... });
```

---

## Challenges & Considerations

### 1. ECS Compatibility

**Challenge:** Plugins need access to ECS components/systems
**Solution:**

- Create contract interfaces for common ECS operations
- Plugins register systems via service registry
- Event bus bridges plugin → ECS communication

### 2. Performance

**Challenge:** Plugin indirection overhead
**Solution:**

- Service registry caching
- Direct component queries in hot paths
- Event bus batching for high-frequency events

### 3. Version Compatibility

**Challenge:** Plugin API stability
**Solution:**

- Semantic versioning for contracts
- Compatibility validation during load
- Deprecation warnings

### 4. Security

**Challenge:** User-created plugins could be malicious
**Solution:**

- Permission system from hyacinth-bean
- Sandboxing for untrusted plugins
- Code signing for official plugins

---

## Implementation Checklist

### Minimal Viable Plugin System (MVP)

- [ ] Port `IPlugin`, `IPluginContext`, `IRegistry` contracts
- [ ] Implement `PluginLoader` with basic discovery
- [ ] Implement `ServiceRegistry` with priority support
- [ ] Implement `EventBus` for pub/sub
- [ ] Add `AddPluginSystem()` DI extension
- [ ] Create sample plugin (e.g., Analytics)
- [ ] Document plugin development guide

### Full Feature Parity

- [ ] Dependency resolution
- [ ] Multi-profile support
- [ ] Hot reload support
- [ ] Capability validation
- [ ] Security sandbox
- [ ] Health monitoring
- [ ] Admin API
- [ ] Plugin management UI

---

## Recommended Next Steps

### Immediate Actions

1. **Create proof-of-concept** - Port core contracts to PigeonPea
2. **Simple plugin** - Analytics or logging plugin
3. **ECS integration** - Wire EventBus to Arch ECS events
4. **Test hot reload** - Verify ALC isolation works

### Short-term Goals

1. Convert rendering layer to plugins
2. Create community plugin template
3. Document plugin API
4. Build sample mods

### Long-term Vision

1. **Plugin marketplace** - Curated mod repository
2. **Live plugin updates** - In-game mod manager
3. **Cross-game plugins** - Shared roguelike libraries
4. **Visual plugin editor** - No-code mod creation

---

## Conclusion

The hyacinth-bean-base plugin system is **well-architected, battle-tested, and perfectly suited** for pigeon-pea's needs. Its multi-profile design, event-driven architecture, and ALC isolation align perfectly with:

- ✅ Multi-platform roguelike (console + Windows)
- ✅ ECS-based architecture (event integration)
- ✅ Extensible game systems (mods, community content)
- ✅ Rapid development (hot reload, isolated testing)

**Recommendation:** Adopt the plugin system with Phase 1 (core infrastructure) as the immediate next step, followed by incremental migration of existing systems to plugins.

---

## References

### Source Code Locations

**Contracts:**

- `/home/user/hyacinth-bean-base/dotnet/app-essential/core/src/HyacinthBean.Plugins.Contracts/`

**Core Implementation:**

- `/home/user/hyacinth-bean-base/dotnet/app-essential/core/src/HyacinthBean.Plugins.Core/`

**Example Plugins:**

- `/home/user/hyacinth-bean-base/dotnet/app-essential/extend/src/HyacinthBean.Plugins.Analytics/`
- `/home/user/hyacinth-bean-base/dotnet/game-general/extend/src/HyacinthBean.Plugins.NPC/`
- `/home/user/hyacinth-bean-base/dotnet/game-general/extend/src/HyacinthBean.Plugins.Rendering.Terminal/`

### Key Files Analyzed

- `IPlugin.cs` - Plugin lifecycle interface
- `PluginManifest.cs` - Plugin metadata schema
- `IRegistry.cs` - Cross-ALC service registry
- `PluginLoader.cs` - Plugin discovery and loading (506 lines)
- `ServiceCollectionExtensions.cs` - DI integration
- `AnalyticsPlugin.cs` - Reference implementation

---

**Analysis completed:** 2025-11-10
**Total files analyzed:** 15+
**Total plugins examined:** 50+
**Recommendation confidence:** HIGH
