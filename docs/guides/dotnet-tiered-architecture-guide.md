---
canonical: true
created: '2025-11-19'
doc_id: GUIDE-00002
doc_type: guide
related:
- RFC-00013
- RFC-00014
- ADR-00001
- ADR-00003
- ADR-00004
status: active
summary: Comprehensive guide for implementing the four-tier service architecture and
  domain layering in .NET projects
tags:
- dotnet
- architecture
- tiered-architecture
- plugins
- layers
- services
title: .NET Tiered Architecture and Layer Implementation Guide
updated: '2025-11-19'
---


# .NET Tiered Architecture and Layer Implementation Guide

This guide provides comprehensive instructions for implementing the tiered architecture and domain layering system in Pigeon Pea .NET projects. **All agents working on .NET code MUST follow these rules.**

## Table of Contents

1. [Architecture Principles](#architecture-principles)
2. [Four-Tier Service Architecture](#four-tier-service-architecture)
3. [Domain Layer Organization](#domain-layer-organization)
4. [Project Organization](#project-organization)
5. [Dependency Rules](#dependency-rules)
6. [Plugin Architecture](#plugin-architecture)
7. [Implementation Patterns](#implementation-patterns)
8. [Common Mistakes to Avoid](#common-mistakes-to-avoid)
9. [Examples and Templates](#examples-and-templates)

---

## Architecture Principles

### Core Philosophy

Pigeon Pea follows a **plugin-based, tier-separated architecture** that enforces:

1. **Separation of Concerns**: Domain knowledge (WHAT) vs Platform implementation (HOW)
2. **Dependency Inversion**: High-level policies don't depend on low-level details
3. **Interface Segregation**: Small, focused service contracts
4. **Plugin Isolation**: Implementations loaded in separate Assembly Load Contexts
5. **Contract Stability**: Shared interfaces across all plugins

### Key Terminology

- **Tier**: Level in the service architecture (1-4)
- **Layer**: Functional layer in domain organization (Contracts, Core, Control, Rendering)
- **Domain**: Feature area (Map, Dungeon, Inventory, Input, etc.)
- **Service**: Contract-based capability exposed to consumers
- **Plugin**: Dynamically loaded implementation of a service
- **Provider**: Internal implementation strategy selected by a service

---

## Four-Tier Service Architecture

Every service category (Audio, Input, Inventory, GAS, Perception, AI, etc.) follows a **four-tier architecture**:

```
┌─────────────────────────────────────────────────────────┐
│ Tier 1: Service Interface (Contracts)                  │
│   Location: PigeonPea.Contracts.<Domain>.Services      │
│   Purpose: Define WHAT the service does                │
│   Dependencies: None (except primitives)                │
├─────────────────────────────────────────────────────────┤
│ Tier 2: Proxy Services (Source-Generated)              │
│   Location: PigeonPea.Contracts.<Domain>.Services.Proxy│
│   Purpose: Route calls to selected implementation      │
│   Dependencies: Tier 1, IRegistry                       │
├─────────────────────────────────────────────────────────┤
│ Tier 3: Real Services (Plugin Implementations)         │
│   Location: PigeonPea.Plugins.<Domain>.<Name>          │
│   Purpose: Implement HOW the service works             │
│   Dependencies: Tier 1, Shared libraries, external deps│
├─────────────────────────────────────────────────────────┤
│ Tier 4: Providers (Optional Internal Strategies)       │
│   Location: Inside Tier 3 plugins                      │
│   Purpose: Alternative backends/strategies             │
│   Dependencies: Tier 1, selected by Tier 3             │
└─────────────────────────────────────────────────────────┘
```

### Tier 1: Service Interface (Contracts)

**Purpose**: Define the stable API that consumers use.

**Location**:
- App-level: `dotnet/app-essential/core/src/PigeonPea.Contracts/<Domain>/Services/IService.cs`
- Game-level: `dotnet/game-essential/core/src/PigeonPea.Game.Contracts/<Domain>/Services/IService.cs`

**Rules**:
- ✅ **MUST** be small and focused
- ✅ **MUST** use only primitives or DTOs from the same Contracts assembly
- ✅ **MUST** remain stable (breaking changes require versioning)
- ❌ **MUST NOT** depend on implementations
- ❌ **MUST NOT** depend on heavy external libraries
- ❌ **MUST NOT** expose plugin-specific types

**Example**:
```csharp
namespace PigeonPea.Contracts.Input.Services;

public interface IService
{
    /// <summary>
    /// Check if an input action is currently pressed.
    /// </summary>
    bool IsActionPressed(string actionId);

    /// <summary>
    /// Get the current value of an axis (e.g., joystick).
    /// </summary>
    float GetAxis(string axisId);

    /// <summary>
    /// Get input value for an action (supports button, axis, vector).
    /// </summary>
    InputValue GetActionValue(string actionId);
}
```

### Tier 2: Proxy Service (Generated or Manual)

**Purpose**: Delegate to the plugin registry to select and call the implementation.

**Location**: Next to Tier 1 interface in `Services.Proxy` namespace

**Rules**:
- ✅ **MUST** be decorated with `[RealizeService(typeof(IService))]`
- ✅ **MUST** delegate all calls to `IRegistry.Get<T>()`
- ✅ **CAN** be source-generated (planned) or hand-written (current)
- ❌ **MUST NOT** contain business logic
- ❌ **MUST NOT** cache implementation references (registry may change)

**Example**:
```csharp
namespace PigeonPea.Contracts.Input.Services.Proxy;

[RealizeService(typeof(IService))]
public class Service : IService
{
    private readonly IRegistry _registry;

    public Service(IRegistry registry)
    {
        _registry = registry;
    }

    public bool IsActionPressed(string actionId)
    {
        var impl = _registry.Get<IService>();
        return impl?.IsActionPressed(actionId) ?? false;
    }

    public float GetAxis(string axisId)
    {
        var impl = _registry.Get<IService>();
        return impl?.GetAxis(axisId) ?? 0f;
    }

    public InputValue GetActionValue(string actionId)
    {
        var impl = _registry.Get<IService>();
        return impl?.GetActionValue(actionId) ?? InputValue.None;
    }
}
```

### Tier 3: Real Service (Plugin Implementation)

**Purpose**: Provide the actual implementation of the service.

**Location**:
- App-level: `dotnet/app-essential/plugins/src/PigeonPea.Plugins.<Domain>.<Implementation>/`
- Game-level: `dotnet/game-essential/plugins/src/PigeonPea.Plugins.<Domain>.<Implementation>/`

**Rules**:
- ✅ **MUST** implement the Tier 1 interface
- ✅ **MUST** register into `IRegistry` during plugin initialization
- ✅ **CAN** depend on Shared libraries (`PigeonPea.Shared.<Domain>`)
- ✅ **CAN** depend on heavy external libraries
- ✅ **CAN** use Tier 4 providers internally
- ❌ **MUST NOT** depend on other Tier 3 implementations
- ❌ **MUST NOT** depend on Tier 2 proxies

**Example**:
```csharp
namespace PigeonPea.Plugins.Input.UniInputSystem;

public class UniInputSystemService : IService
{
    private readonly InputSystem _inputSystem;
    private readonly Dictionary<string, InputActionReference> _actions;

    public UniInputSystemService(/* dependencies */)
    {
        _inputSystem = new InputSystem();
        _actions = new Dictionary<string, InputActionReference>();
        // Initialize input system...
    }

    public bool IsActionPressed(string actionId)
    {
        if (!_actions.TryGetValue(actionId, out var actionRef))
            return false;

        return actionRef.action.IsPressed();
    }

    public float GetAxis(string axisId)
    {
        if (!_actions.TryGetValue(axisId, out var actionRef))
            return 0f;

        return actionRef.action.ReadValue<float>();
    }

    public InputValue GetActionValue(string actionId)
    {
        if (!_actions.TryGetValue(actionId, out var actionRef))
            return InputValue.None;

        return actionRef.action.ReadValue<InputValue>();
    }
}
```

**Plugin Class**:
```csharp
public class UniInputSystemPlugin : IPlugin
{
    public string Id => "input-uniinputsystem";
    public string Name => "UniInputSystem Input Plugin";
    public Version Version => new(1, 0, 0);

    public Task InitializeAsync(IPluginContext context)
    {
        var service = new UniInputSystemService(/* ... */);

        context.Registry.Register<IService>(
            service,
            new ServiceMetadata
            {
                PluginId = Id,
                Priority = 100
            });

        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        // Cleanup
        return Task.CompletedTask;
    }
}
```

### Tier 4: Providers (Optional)

**Purpose**: Alternative backends or strategies used internally by Tier 3.

**Location**: Inside the Tier 3 plugin project

**Rules**:
- ✅ **CAN** provide alternative implementations (e.g., different FOV algorithms)
- ✅ **CAN** be selected by Tier 3 via registry or configuration
- ✅ **CAN** be swapped based on profile or runtime conditions
- ❌ **MUST NOT** be directly exposed to consumers
- ❌ **MUST NOT** bypass Tier 3 service

**Example**:
```csharp
// Inside PigeonPea.Plugins.Perception.Basic

internal interface IFovProvider
{
    HashSet<Point> Calculate(Point origin, int radius, ITransparencyView map);
}

internal class RecursiveShadowcastingProvider : IFovProvider
{
    public HashSet<Point> Calculate(Point origin, int radius, ITransparencyView map)
    {
        // GoRogue implementation
    }
}

internal class RaycastingProvider : IFovProvider
{
    public HashSet<Point> Calculate(Point origin, int radius, ITransparencyView map)
    {
        // Alternative algorithm
    }
}

// Tier 3 service selects provider
public class BasicPerceptionService : IService
{
    private readonly IFovProvider _fovProvider;

    public BasicPerceptionService(IConfiguration config, IRegistry registry)
    {
        // Select provider based on configuration or registry
        var providerType = config["Perception:FovProvider"];
        _fovProvider = providerType switch
        {
            "raycast" => new RaycastingProvider(),
            _ => new RecursiveShadowcastingProvider()
        };
    }
}
```

---

## Domain Layer Organization

Domains are organized into **functional layers**. Not all domains need all layers.

### Layer Types

```
┌──────────────────────────────────────────────────────────┐
│ Contracts Layer (Tier 1)                                │
│   Location: <Domain>.Contracts/                         │
│   Purpose: Service interfaces + DTOs                    │
│   Dependencies: None (minimal)                          │
├──────────────────────────────────────────────────────────┤
│ Shared Layer (Algorithms & Models)                      │
│   Location: PigeonPea.Shared.<Domain>/                  │
│   Purpose: Reusable domain logic                        │
│   Dependencies: No plugin knowledge                     │
├──────────────────────────────────────────────────────────┤
│ Game/Engine Layer (ECS Integration)                     │
│   Location: PigeonPea.Game.<Domain>/                    │
│   Purpose: ECS components and systems                   │
│   Dependencies: Arch, Shared layer                      │
├──────────────────────────────────────────────────────────┤
│ Plugins Layer (Tier 3 Implementations)                  │
│   Location: PigeonPea.Plugins.<Domain>.<Name>/          │
│   Purpose: Concrete service implementations             │
│   Dependencies: Contracts, Shared, external libs        │
└──────────────────────────────────────────────────────────┘
```

### Layer Rules by Domain Type

#### App-Essential Domains (Input, Audio, Config, Resource)

These are **non-gameplay** capabilities used by any application.

**Contracts Location**: `dotnet/app-essential/core/src/PigeonPea.Contracts.<Domain>/`

**Shared Location**: `dotnet/app-essential/core/src/PigeonPea.Shared.<Domain>/` or `dotnet/engine/core/src/PigeonPea.Shared.<Domain>/` (transitional)

**Plugins Location**: `dotnet/app-essential/plugins/src/PigeonPea.Plugins.<Domain>.<Name>/`

**Dependencies**:
- ✅ Contracts → (none)
- ✅ Shared → (domain primitives only, no plugin system)
- ✅ Plugins → Contracts + Shared + external libraries

**Example: Input**
```
dotnet/app-essential/
├── core/src/
│   ├── PigeonPea.Contracts/Input/Services/IService.cs     (Tier 1)
│   └── PigeonPea.Contracts/Input/Services/Proxy/Service.cs (Tier 2)
├── plugins/src/
│   └── PigeonPea.Plugins.Input.UniInputSystem/            (Tier 3)
│       ├── UniInputSystemPlugin.cs
│       ├── UniInputSystemService.cs
│       └── plugin.json
dotnet/engine/core/src/  # Transitional location
└── PigeonPea.Shared.Input/                                (Shared algorithms)
    ├── InputSystem.cs
    ├── Actions/
    ├── Bindings/
    └── Controls/
```

#### Game-Essential Domains (Inventory, GAS, Perception, AI)

These are **gameplay** capabilities that most games need.

**Contracts Location**: `dotnet/game-essential/core/src/PigeonPea.Game.Contracts.<Domain>/`

**Shared Location**: `dotnet/game-essential/core/src/PigeonPea.Shared.<Domain>/`

**Game Integration Location**: `dotnet/game-essential/core/src/PigeonPea.Game.<Domain>/`

**Plugins Location**: `dotnet/game-essential/plugins/src/PigeonPea.Plugins.<Domain>.<Name>/`

**Dependencies**:
- ✅ Contracts → (none or minimal shared types)
- ✅ Shared → (domain models and algorithms, no ECS)
- ✅ Game Integration → Shared + Arch ECS
- ✅ Plugins → Contracts + Shared + Game Integration (optional) + external libraries

**Example: Inventory**
```
dotnet/game-essential/
├── core/src/
│   ├── PigeonPea.Game.Contracts/Inventory/
│   │   ├── Services/IService.cs                           (Tier 1)
│   │   └── Services/Proxy/Service.cs                      (Tier 2)
│   ├── PigeonPea.Shared.Inventory/                        (Shared models)
│   │   ├── Core/Inventory.cs
│   │   ├── Core/InventorySlot.cs
│   │   ├── Items/ItemDefinition.cs
│   │   └── Items/ItemInstance.cs
│   └── PigeonPea.Game.Inventory/                          (ECS integration, DEPRECATED - use components in game.contracts instead)
│       └── InventoryComponent.cs
└── plugins/src/
    └── PigeonPea.Plugins.Inventory.Basic/                 (Tier 3)
        ├── InventoryBasicPlugin.cs
        ├── BasicInventoryService.cs
        └── plugin.json
```

#### Content Domains (Map, Dungeon)

These have **special rendering and content** requirements.

**IMPORTANT**: Content domains should **NOT** have wrapper projects. They follow the **Double-Plugin Architecture**:

1. **Domain Plugin**: Knows WHAT to render/generate (domain logic)
2. **Platform Plugin**: Knows HOW to render (ANSI, Braille, SkiaSharp)

**Contracts Location**: `dotnet/game-essential/core/src/PigeonPea.<Domain>.Contracts/`

**Shared Location**: N/A (should be plugins or minimal DTOs in contracts)

**Domain Plugins Location**: `projects/<domain>/dotnet/console-app/plugins/PigeonPea.Plugin.<Domain>.<Feature>/`

**Platform Plugins Location**: `projects/<domain>/dotnet/console-app/plugins/PigeonPea.Plugin.Rendering.<Platform>.<Tech>/`

**Example: Dungeon**
```
dotnet/game-essential/core/src/
└── PigeonPea.Dungeon.Contracts/
    ├── IDungeonGenerator.cs                               (Domain contract)
    ├── Models/DungeonView.cs                              (DTO)
    └── Services/                                          (Optional service tier)

dotnet/game-essential/core/src/
└── PigeonPea.Rendering.Contracts/                         (Unified rendering contract)
    ├── IRenderer.cs
    ├── Tile.cs
    └── Color.cs

projects/dungeon/dotnet/console-app/plugins/
├── PigeonPea.Plugin.Dungeon.ModernEdgar/                  (Domain plugin: generation)
│   ├── ModernEdgarDungeonGenerator.cs                     (Uses modern-edgar-dotnet DIRECTLY)
│   └── plugin.json
├── PigeonPea.Plugin.Dungeon.Rendering/                    (Domain plugin: rendering)
│   ├── DungeonRenderer.cs                                 (Calls IRenderer.DrawTile)
│   └── plugin.json
├── PigeonPea.Plugin.Rendering.Terminal.ANSI/              (Platform plugin)
│   ├── ANSIRenderer.cs                                    (Implements IRenderer)
│   └── plugin.json
└── PigeonPea.Plugin.Rendering.Terminal.Braille/           (Platform plugin)
    ├── BrailleRenderer.cs                                 (Implements IRenderer)
    └── plugin.json
```

**❌ ANTI-PATTERN: Do NOT create these wrapper projects**:
```
❌ PigeonPea.Dungeon.Core/            # NO! Use plugin instead
❌ PigeonPea.Dungeon.Rendering/       # NO! Use plugin instead
❌ PigeonPea.Dungeon.Control/         # NO! Use plugin instead
```

**Why?**
- Wrapper projects violate the tiered architecture
- They create ALC type identity issues
- They prevent proper plugin isolation
- External libraries (like modern-edgar-dotnet) should be used DIRECTLY in plugins

---

## Project Organization

### Directory Structure

```
dotnet/
├── app-essential/              # Non-gameplay application infrastructure
│   ├── core/src/
│   │   ├── PigeonPea.Contracts/           # Tier 1 contracts for app services
│   │   ├── PigeonPea.PluginSystem/        # Plugin loader infrastructure
│   │   └── PigeonPea.AppComposition.PureDi/  # DI composition
│   └── plugins/src/
│       ├── PigeonPea.Plugins.Audio.LibVlc/
│       ├── PigeonPea.Plugins.Config/
│       └── PigeonPea.Plugins.Input.UniInputSystem/
│
├── game-essential/             # Gameplay infrastructure (reusable across games)
│   ├── core/src/
│   │   ├── PigeonPea.Game.Contracts/      # Tier 1 contracts for game services
│   │   ├── PigeonPea.Shared.<Domain>/     # Shared domain libraries
│   │   └── PigeonPea.Game.<Domain>/       # ECS integration
│   └── plugins/src/
│       ├── PigeonPea.Plugins.Inventory.Basic/
│       └── PigeonPea.Plugin.Dungeon.Basic/
│
├── engine/core/src/            # Legacy/transitional (being phased out)
│   ├── PigeonPea.Shared.ECS/
│   └── PigeonPea.Shared.Rendering/
│
└── _lib/                       # External library integrations (use directly in plugins)
    ├── modern-edgar-dotnet/
    └── fantasy-map-generator/

projects/
├── dungeon/dotnet/console-app/
│   ├── core/src/PigeonPea.Console/        # Host application
│   └── plugins/                           # Project-specific plugins
│       ├── PigeonPea.Plugin.Dungeon.ModernEdgar/
│       ├── PigeonPea.Plugin.Dungeon.Rendering/
│       └── PigeonPea.Plugin.Rendering.Terminal.ANSI/
│
└── map/dotnet/                            # Similar structure for map project
```

### Project Naming Conventions

**Contracts (Tier 1)**:
- App-level: `PigeonPea.Contracts.<Domain>`
- Game-level: `PigeonPea.Game.Contracts.<Domain>`

**Shared Libraries**:
- `PigeonPea.Shared.<Domain>`

**ECS Integration**:
- `PigeonPea.Game.<Domain>`

**Plugins (Tier 3)**:
- App-level: `PigeonPea.Plugins.<Domain>.<Implementation>`
- Game-level: `PigeonPea.Plugins.<Domain>.<Implementation>`
- Project-specific: `PigeonPea.Plugin.<Domain>.<Implementation>` (note: singular "Plugin")

**Examples**:
- ✅ `PigeonPea.Contracts.Input`
- ✅ `PigeonPea.Shared.Inventory`
- ✅ `PigeonPea.Plugins.Audio.LibVlc`
- ✅ `PigeonPea.Plugin.Dungeon.ModernEdgar`
- ❌ `PigeonPea.Dungeon.Core` (wrapper, not allowed)

---

## Dependency Rules

### Allowed Dependencies

**Tier-based rules**:
- ✅ Tier 2 → Tier 1
- ✅ Tier 3 → Tier 1
- ✅ Tier 3 → Shared libraries
- ✅ Tier 4 → Tier 1 (for contracts)
- ✅ Tier 4 → Shared libraries
- ❌ Tier 1 → Any other tier
- ❌ Tier 2 → Tier 3 or Tier 4 (except via registry)
- ❌ Tier 3 → Tier 2
- ❌ Tier 3 → Other Tier 3 implementations

**Layer-based rules**:
- ✅ game-essential → app-essential
- ✅ projects → app-essential + game-essential
- ✅ Plugins → Contracts + Shared
- ❌ app-essential → game-essential
- ❌ Contracts → Shared
- ❌ Shared → Contracts (but can reference common primitives)

### Project Reference Examples

**Good ✅**:
```xml
<!-- Plugin referencing contracts and shared -->
<ItemGroup>
  <ProjectReference Include="..\..\..\..\dotnet\app-essential\core\src\PigeonPea.Contracts\PigeonPea.Contracts.csproj" />
  <ProjectReference Include="..\..\..\..\dotnet\engine\core\src\PigeonPea.Shared.Input\PigeonPea.Shared.Input.csproj" />
</ItemGroup>

<!-- Plugin using external library DIRECTLY -->
<ItemGroup>
  <ProjectReference Include="..\..\..\..\dotnet\_lib\modern-edgar-dotnet\src\Edgar.Core\Edgar.Core.csproj" />
</ItemGroup>
```

**Bad ❌**:
```xml
<!-- Plugin depending on another plugin -->
<ProjectReference Include="..\PigeonPea.Plugins.Other\PigeonPea.Plugins.Other.csproj" />  ❌

<!-- Plugin depending on proxy -->
<ProjectReference Include="..\..\Contracts\Proxy\Proxy.csproj" />  ❌

<!-- Contracts depending on implementations -->
<ProjectReference Include="..\PigeonPea.Shared.Input\PigeonPea.Shared.Input.csproj" />  ❌

<!-- Using a wrapper instead of external lib directly -->
<ProjectReference Include="..\PigeonPea.Dungeon.Core\PigeonPea.Dungeon.Core.csproj" />  ❌
```

---

## Plugin Architecture

### Assembly Load Context (ALC) Isolation

Plugins are loaded in isolated ALCs to:
- Allow different versions of dependencies
- Enable plugin unload/reload (planned)
- Isolate failures and conflicts

**Critical Rule**: **Shared contracts MUST be loaded from the Default ALC** to ensure type identity across plugins.

### ALC Configuration

**Shared Assemblies** (config-driven whitelist):
```json
{
  "PluginSystem": {
    "SharedAssemblies": [
      "PigeonPea.Contracts",
      "PigeonPea.Game.Contracts",
      "PigeonPea.Rendering.Contracts",
      "PigeonPea.Dungeon.Contracts",
      "PigeonPea.Map.Contracts",
      "PigeonPea.Shared.Inventory",
      "Arch"
    ]
  }
}
```

**PluginLoadContext Implementation**:
```csharp
protected override Assembly? Load(AssemblyName assemblyName)
{
    // Force shared contracts to load from Default ALC
    if (_sharedAssemblies.Contains(assemblyName.Name))
    {
        return null; // Use Default ALC
    }

    // Plugin-specific assemblies load in plugin ALC
    var path = _resolver.ResolveAssemblyToPath(assemblyName);
    if (path != null)
    {
        return LoadFromAssemblyPath(path);
    }

    return null; // Fallback to Default ALC
}
```

### Plugin Manifest (plugin.json)

**Required fields**:
```json
{
  "id": "unique-plugin-id",
  "name": "Human Readable Name",
  "version": "1.0.0",
  "capabilities": ["feature-tag", "renderer", "input"],
  "supportedProfiles": ["dotnet.console", "dotnet.windows"],
  "entryPoint": {
    "dotnet.console": "AssemblyName.dll,Namespace.PluginClass"
  }
}
```

**Profile matching**: Host loads only plugins with matching `supportedProfiles`.

---

## Implementation Patterns

### Creating a New Service Category

**Step 1: Define Tier 1 Contract**

Location: `dotnet/[app|game]-essential/core/src/PigeonPea[.Game].Contracts/<Domain>/Services/IService.cs`

```csharp
namespace PigeonPea.Contracts.YourDomain.Services;

/// <summary>
/// Service contract for YourDomain functionality.
/// </summary>
public interface IService
{
    /// <summary>
    /// Does something important.
    /// </summary>
    Task<YourResult> DoSomethingAsync(YourRequest request);
}

// DTOs in same assembly
public record YourRequest(string Parameter);
public record YourResult(string Value);
```

**Step 2: Create Tier 2 Proxy**

Location: `Services/Proxy/Service.cs` (next to interface)

```csharp
namespace PigeonPea.Contracts.YourDomain.Services.Proxy;

[RealizeService(typeof(IService))]
public class Service : IService
{
    private readonly IRegistry _registry;

    public Service(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public async Task<YourResult> DoSomethingAsync(YourRequest request)
    {
        var impl = _registry.Get<IService>();
        if (impl == null)
            throw new InvalidOperationException("No implementation registered for IService");

        return await impl.DoSomethingAsync(request);
    }
}
```

**Step 3: Create Shared Library (if needed)**

Location: `dotnet/[app|game]-essential/core/src/PigeonPea.Shared.<Domain>/`

```csharp
namespace PigeonPea.Shared.YourDomain;

// Domain models, algorithms, utilities
public class YourDomainModel
{
    // Rich domain logic here
}

public static class YourAlgorithms
{
    public static Result Calculate(Input input)
    {
        // Reusable algorithm
    }
}
```

**Step 4: Create Plugin Implementation**

Location: `dotnet/[app|game]-essential/plugins/src/PigeonPea.Plugins.<Domain>.<Name>/`

**Service implementation**:
```csharp
namespace PigeonPea.Plugins.YourDomain.Basic;

public class BasicYourDomainService : IService
{
    private readonly YourDomainModel _model;

    public BasicYourDomainService(/* dependencies */)
    {
        _model = new YourDomainModel();
    }

    public async Task<YourResult> DoSomethingAsync(YourRequest request)
    {
        // Implementation using shared library
        var result = YourAlgorithms.Calculate(new Input(request.Parameter));
        return new YourResult(result.Value);
    }
}
```

**Plugin class**:
```csharp
public class YourDomainPlugin : IPlugin
{
    public string Id => "yourdomain-basic";
    public string Name => "Basic YourDomain Plugin";
    public Version Version => new(1, 0, 0);

    public Task InitializeAsync(IPluginContext context)
    {
        var service = new BasicYourDomainService(/* ... */);

        context.Registry.Register<IService>(
            service,
            new ServiceMetadata
            {
                PluginId = Id,
                Priority = 100,
                Tags = new[] { "basic", "yourdomain" }
            });

        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        return Task.CompletedTask;
    }
}
```

**plugin.json**:
```json
{
  "id": "yourdomain-basic",
  "name": "Basic YourDomain Plugin",
  "version": "1.0.0",
  "capabilities": ["yourdomain"],
  "supportedProfiles": ["dotnet.console", "dotnet.windows"],
  "entryPoint": {
    "dotnet.console": "PigeonPea.Plugins.YourDomain.Basic.dll,PigeonPea.Plugins.YourDomain.Basic.YourDomainPlugin"
  }
}
```

### Double-Plugin Architecture (Content Domains)

For domains with **rendering** (Map, Dungeon), use two types of plugins:

**Domain Plugin** (knows WHAT to render):
```csharp
// PigeonPea.Plugin.Dungeon.Rendering
public class DungeonRenderer
{
    private readonly IRenderer _platformRenderer; // Injected

    public DungeonRenderer(IRenderer platformRenderer)
    {
        _platformRenderer = platformRenderer;
    }

    public void Render(DungeonView dungeon, Point playerPos)
    {
        _platformRenderer.BeginFrame();

        for (int y = 0; y < dungeon.Height; y++)
        {
            for (int x = 0; x < dungeon.Width; x++)
            {
                var tile = GetTileForCell(dungeon, x, y, playerPos);
                _platformRenderer.DrawTile(x, y, tile); // Platform-agnostic!
            }
        }

        _platformRenderer.EndFrame();
    }

    private Tile GetTileForCell(DungeonView dungeon, int x, int y, Point playerPos)
    {
        // Domain logic: what is this cell?
        if (x == playerPos.X && y == playerPos.Y)
            return new Tile('@', Color.White, Color.Black);

        if (dungeon.Doors[y, x] != 0)
            return new Tile('+', Color.Brown, Color.Black);

        if (!dungeon.Walkable[y, x])
            return new Tile('#', Color.Gray, Color.Black);

        return new Tile('.', Color.DarkGray, Color.Black);
    }
}
```

**Platform Plugin** (knows HOW to render):
```csharp
// PigeonPea.Plugin.Rendering.Terminal.ANSI
public class ANSIRenderer : IRenderer
{
    private readonly StringBuilder _buffer = new();

    public void BeginFrame()
    {
        _buffer.Clear();
        _buffer.Append("\x1b[2J\x1b[H"); // Clear screen
    }

    public void DrawTile(int x, int y, Tile tile)
    {
        // ANSI escape sequences
        _buffer.Append($"\x1b[{y + 1};{x + 1}H");
        _buffer.Append($"\x1b[38;2;{tile.Foreground.R};{tile.Foreground.G};{tile.Foreground.B}m");
        _buffer.Append($"\x1b[48;2;{tile.Background.R};{tile.Background.G};{tile.Background.B}m");
        _buffer.Append(tile.Glyph);
    }

    public void EndFrame()
    {
        _buffer.Append("\x1b[0m");
        Console.Write(_buffer.ToString());
        Console.Out.Flush();
    }

    // No dungeon-specific knowledge!
}
```

**Key Point**: Domain and platform plugins don't know about each other! They only share the `IRenderer` contract.

---

## Common Mistakes to Avoid

### ❌ Mistake 1: Creating Wrapper Projects

**Wrong**:
```
PigeonPea.Dungeon.Core/
└── ModernEdgarWrapper.cs  // Wraps modern-edgar-dotnet
```

**Right**:
```
PigeonPea.Plugin.Dungeon.ModernEdgar/
└── ModernEdgarDungeonGenerator.cs  // Uses modern-edgar-dotnet DIRECTLY
```

**Why**: Wrappers violate tier architecture and create ALC issues.

### ❌ Mistake 2: Plugin Depending on Another Plugin

**Wrong**:
```xml
<!-- In PigeonPea.Plugin.A -->
<ProjectReference Include="..\PigeonPea.Plugin.B\PigeonPea.Plugin.B.csproj" />
```

**Right**:
```csharp
// Both plugins implement same contract
// Host/consumer uses registry to get implementation
var service = _registry.Get<IService>();
```

**Why**: Plugins must be isolated; they communicate only via shared contracts.

### ❌ Mistake 3: Contracts Depending on Heavy Libraries

**Wrong**:
```csharp
// In PigeonPea.Contracts
using Newtonsoft.Json; // Heavy external dependency

public interface IService
{
    JObject GetData(); // ❌ Exposes library-specific type
}
```

**Right**:
```csharp
// In PigeonPea.Contracts
public interface IService
{
    Dictionary<string, object> GetData(); // ✅ Framework type
}
```

**Why**: Contracts must remain lightweight and stable.

### ❌ Mistake 4: Putting Business Logic in Proxies

**Wrong**:
```csharp
[RealizeService(typeof(IService))]
public class Service : IService
{
    public Result DoSomething()
    {
        // ❌ Business logic in proxy
        var calculated = ComplexCalculation();
        return new Result(calculated);
    }
}
```

**Right**:
```csharp
[RealizeService(typeof(IService))]
public class Service : IService
{
    private readonly IRegistry _registry;

    public Result DoSomething()
    {
        var impl = _registry.Get<IService>();
        return impl.DoSomething(); // ✅ Delegate only
    }
}
```

**Why**: Proxies are routing only; logic belongs in Tier 3.

### ❌ Mistake 5: Game Logic in App-Essential

**Wrong**:
```
dotnet/app-essential/core/src/
└── PigeonPea.Contracts/Combat/  // ❌ Combat is game-specific
```

**Right**:
```
dotnet/game-essential/core/src/
└── PigeonPea.Game.Contracts/Combat/  // ✅ Game contracts
```

**Why**: App-essential is for non-gameplay infrastructure only.

### ❌ Mistake 6: Mixing Domain and Platform Concerns

**Wrong**:
```csharp
public class DungeonANSIRenderer // ❌ Coupled to both dungeon and ANSI
{
    public void Render(DungeonView dungeon)
    {
        Console.Write("\x1b[2J"); // ANSI escape codes
        // Dungeon-specific rendering...
    }
}
```

**Right**:
```csharp
// Domain plugin
public class DungeonRenderer
{
    private readonly IRenderer _renderer; // ✅ Platform-agnostic

    public void Render(DungeonView dungeon)
    {
        _renderer.BeginFrame();
        // Call IRenderer methods...
        _renderer.EndFrame();
    }
}

// Platform plugin
public class ANSIRenderer : IRenderer
{
    // ✅ No dungeon knowledge
}
```

**Why**: Separation allows reusing platform renderers across domains.

---

## Examples and Templates

### Example 1: Complete Service Stack (Inventory)

**1. Contract (Tier 1)**:
```csharp
// dotnet/game-essential/core/src/PigeonPea.Game.Contracts/Inventory/Services/IService.cs
namespace PigeonPea.Game.Contracts.Inventory.Services;

public interface IService
{
    Task<bool> TryAddItemAsync(EntityId entityId, ItemStack item);
    Task<bool> TryRemoveItemAsync(EntityId entityId, ItemId itemId, int quantity);
    Task<InventoryView?> GetInventoryAsync(EntityId entityId);
}

public record ItemStack(ItemId ItemId, int Quantity);
public record InventoryView(EntityId OwnerId, IReadOnlyList<InventorySlotView> Slots);
public record InventorySlotView(int Index, ItemStack? Item, SlotConstraints Constraints);
```

**2. Proxy (Tier 2)**:
```csharp
// Services/Proxy/Service.cs
namespace PigeonPea.Game.Contracts.Inventory.Services.Proxy;

[RealizeService(typeof(IService))]
public class Service : IService
{
    private readonly IRegistry _registry;

    public Service(IRegistry registry) => _registry = registry;

    public Task<bool> TryAddItemAsync(EntityId entityId, ItemStack item)
    {
        var impl = _registry.Get<IService>();
        return impl?.TryAddItemAsync(entityId, item) ?? Task.FromResult(false);
    }

    public Task<bool> TryRemoveItemAsync(EntityId entityId, ItemId itemId, int quantity)
    {
        var impl = _registry.Get<IService>();
        return impl?.TryRemoveItemAsync(entityId, itemId, quantity) ?? Task.FromResult(false);
    }

    public Task<InventoryView?> GetInventoryAsync(EntityId entityId)
    {
        var impl = _registry.Get<IService>();
        return impl?.GetInventoryAsync(entityId) ?? Task.FromResult<InventoryView?>(null);
    }
}
```

**3. Shared Library**:
```csharp
// dotnet/game-essential/core/src/PigeonPea.Shared.Inventory/Core/Inventory.cs
namespace PigeonPea.Shared.Inventory.Core;

public class Inventory
{
    private readonly List<InventorySlot> _slots;

    public Inventory(int slotCount)
    {
        _slots = Enumerable.Range(0, slotCount)
            .Select(_ => new InventorySlot())
            .ToList();
    }

    public bool TryAdd(ItemInstance item)
    {
        // Find empty or stackable slot
        var slot = FindAvailableSlot(item);
        if (slot == null) return false;

        slot.Add(item);
        return true;
    }

    public bool TryRemove(Guid itemId, int quantity)
    {
        var slot = _slots.FirstOrDefault(s => s.Item?.Id == itemId);
        if (slot == null) return false;

        return slot.TryRemove(quantity);
    }

    private InventorySlot? FindAvailableSlot(ItemInstance item)
    {
        // Stacking logic
        var stackable = _slots.FirstOrDefault(s =>
            s.Item?.DefinitionId == item.DefinitionId &&
            s.CanAdd(item));

        return stackable ?? _slots.FirstOrDefault(s => s.IsEmpty);
    }
}
```

**4. Plugin (Tier 3)**:
```csharp
// dotnet/game-essential/plugins/src/PigeonPea.Plugins.Inventory.Basic/BasicInventoryService.cs
namespace PigeonPea.Plugins.Inventory.Basic;

public class BasicInventoryService : IService
{
    private readonly ConcurrentDictionary<EntityId, Inventory> _inventories = new();
    private readonly IItemDatabase _itemDatabase;

    public BasicInventoryService(IItemDatabase itemDatabase)
    {
        _itemDatabase = itemDatabase;
    }

    public Task<bool> TryAddItemAsync(EntityId entityId, ItemStack item)
    {
        var inventory = _inventories.GetOrAdd(entityId, _ => new Inventory(slotCount: 20));

        var definition = _itemDatabase.GetDefinition(item.ItemId);
        if (definition == null) return Task.FromResult(false);

        var instance = new ItemInstance(Guid.NewGuid(), definition, item.Quantity);
        return Task.FromResult(inventory.TryAdd(instance));
    }

    public Task<bool> TryRemoveItemAsync(EntityId entityId, ItemId itemId, int quantity)
    {
        if (!_inventories.TryGetValue(entityId, out var inventory))
            return Task.FromResult(false);

        return Task.FromResult(inventory.TryRemove(itemId.Value, quantity));
    }

    public Task<InventoryView?> GetInventoryAsync(EntityId entityId)
    {
        if (!_inventories.TryGetValue(entityId, out var inventory))
            return Task.FromResult<InventoryView?>(null);

        var slots = inventory.Slots
            .Select((slot, index) => new InventorySlotView(
                index,
                slot.Item != null ? new ItemStack(new ItemId(slot.Item.DefinitionId), slot.Item.Quantity) : null,
                slot.Constraints))
            .ToList();

        return Task.FromResult<InventoryView?>(new InventoryView(entityId, slots));
    }
}
```

**5. Plugin Registration**:
```csharp
public class InventoryBasicPlugin : IPlugin
{
    public string Id => "inventory-basic";
    public string Name => "Basic Inventory Plugin";
    public Version Version => new(1, 0, 0);

    public Task InitializeAsync(IPluginContext context)
    {
        var itemDatabase = new JsonItemDatabase(); // Tier 4 provider
        var service = new BasicInventoryService(itemDatabase);

        context.Registry.Register<IService>(
            service,
            new ServiceMetadata
            {
                PluginId = Id,
                Priority = 100
            });

        return Task.CompletedTask;
    }

    public Task ShutdownAsync() => Task.CompletedTask;
}
```

### Example 2: Double-Plugin (Dungeon Rendering)

**Contracts**:
```csharp
// PigeonPea.Dungeon.Contracts/IDungeonGenerator.cs
public interface IDungeonGenerator
{
    DungeonView Generate(DungeonGenerationOptions options);
}

// PigeonPea.Rendering.Contracts/IRenderer.cs
public interface IRenderer
{
    void BeginFrame();
    void EndFrame();
    void DrawTile(int x, int y, Tile tile);
}
```

**Domain Plugin (Generation)**:
```csharp
// PigeonPea.Plugin.Dungeon.ModernEdgar/ModernEdgarDungeonGenerator.cs
public class ModernEdgarDungeonGenerator : IDungeonGenerator
{
    public DungeonView Generate(DungeonGenerationOptions options)
    {
        // Use modern-edgar-dotnet DIRECTLY (no wrapper)
        var edgarConfig = new EdgarConfiguration
        {
            Width = options.Width,
            Height = options.Height
        };

        var result = EdgarGenerator.Generate(edgarConfig);

        // Convert to DungeonView DTO
        return ConvertToDungeonView(result);
    }
}
```

**Domain Plugin (Rendering)**:
```csharp
// PigeonPea.Plugin.Dungeon.Rendering/DungeonRenderer.cs
public class DungeonRenderer
{
    private readonly IRenderer _renderer; // Platform-agnostic!

    public void Render(DungeonView dungeon, Point playerPos)
    {
        _renderer.BeginFrame();

        for (int y = 0; y < dungeon.Height; y++)
        {
            for (int x = 0; x < dungeon.Width; x++)
            {
                _renderer.DrawTile(x, y, GetTile(dungeon, x, y, playerPos));
            }
        }

        _renderer.EndFrame();
    }

    private Tile GetTile(DungeonView dungeon, int x, int y, Point playerPos)
    {
        // Domain-specific tile selection
        // No platform knowledge!
    }
}
```

**Platform Plugins** (multiple options, all implement IRenderer):
```csharp
// PigeonPea.Plugin.Rendering.Terminal.ANSI/ANSIRenderer.cs
public class ANSIRenderer : IRenderer { /* ANSI implementation */ }

// PigeonPea.Plugin.Rendering.Terminal.Braille/BrailleRenderer.cs
public class BrailleRenderer : IRenderer { /* Braille implementation */ }

// PigeonPea.Plugin.Rendering.Windows.SkiaSharp/SkiaRenderer.cs
public class SkiaRenderer : IRenderer { /* SkiaSharp implementation */ }
```

**Result**: Domain plugin can use ANY platform renderer without code changes!

---

## Quick Reference Checklist

### Before Creating New Code

- [ ] Is this an app-level or game-level feature?
- [ ] Does a contract already exist for this category?
- [ ] Can I reuse an existing shared library?
- [ ] Do I need a wrapper project? (Answer: NO!)
- [ ] Should this be a plugin or shared library?

### Creating Tier 1 Contract

- [ ] Defined in correct location (`PigeonPea[.Game].Contracts.<Domain>/Services/`)
- [ ] Uses only primitives or DTOs from same assembly
- [ ] No dependencies on implementations
- [ ] No heavy external library dependencies
- [ ] Documented with XML comments

### Creating Tier 2 Proxy

- [ ] Located next to Tier 1 interface (`Services/Proxy/`)
- [ ] Decorated with `[RealizeService(typeof(IService))]`
- [ ] Delegates all calls to `IRegistry.Get<T>()`
- [ ] No business logic
- [ ] No caching of implementation references

### Creating Tier 3 Plugin

- [ ] Located in `dotnet/[app|game]-essential/plugins/src/` or `projects/<domain>/.../plugins/`
- [ ] Implements Tier 1 interface
- [ ] Registers into `IRegistry` during initialization
- [ ] Has `plugin.json` manifest
- [ ] Uses external libraries DIRECTLY (no wrappers)
- [ ] Follows naming convention: `PigeonPea.Plugin[s].<Domain>.<Implementation>`

### Creating Shared Library

- [ ] Located in `PigeonPea.Shared.<Domain>/`
- [ ] Contains domain models, algorithms, utilities
- [ ] No plugin system knowledge
- [ ] No `IRegistry` dependencies
- [ ] Minimal external dependencies

### Plugin Dependencies

- [ ] ✅ References Tier 1 contracts
- [ ] ✅ References shared libraries (if needed)
- [ ] ✅ References external libraries directly
- [ ] ❌ Does NOT reference other plugins
- [ ] ❌ Does NOT reference Tier 2 proxies
- [ ] ❌ Does NOT reference wrapper projects

### ALC Configuration

- [ ] Shared contracts added to `PluginSystem:SharedAssemblies` config
- [ ] Plugin loaded with correct `supportedProfiles`
- [ ] `plugin.json` has correct `entryPoint` format

---

## Summary

**Core Principles**:
1. **Four tiers**: Contracts (1) → Proxies (2) → Implementations (3) → Providers (4)
2. **Layer separation**: Contracts / Shared / Game / Plugins
3. **No wrappers**: Use external libraries directly in plugins
4. **Double-plugin**: Domain plugins (WHAT) + Platform plugins (HOW)
5. **ALC isolation**: Shared contracts from Default ALC, implementations in plugin ALCs

**Remember**:
- Contracts are small and stable
- Proxies delegate only
- Plugins implement
- Shared libraries are reusable building blocks
- External libraries go in plugins, not wrappers

**When in doubt**:
1. Check if a contract exists
2. Check if you can use a shared library
3. Create a plugin, not a wrapper
4. Keep domains and platforms separate

---

## Related Documentation

- [RFC-013: Plugin Architecture Refinement](../../rfcs/013-plugin-architecture-refinement-tiered.md) - Detailed design and migration plan
- [ADR-001: Architecture Overview](../dotnet/architecture/overview.md) - High-level architecture
- [ADR-003: Service Tiers](../dotnet/architecture/service-tiers.md) - Tier definitions and examples
- [ADR-004: Services and Plugins](../dotnet/architecture/services-and-plugins.md) - How components fit together
- [RFC-006: Plugin System Architecture](../../rfcs/006-plugin-system-architecture.md) - Plugin system design
- [RFC-014: Dispose Pattern Generator](../../rfcs/014-adopt-dispose-pattern-generator.md) - Resource management in plugins
