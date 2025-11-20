# DI Architecture Plan: Pure.DI + Splat + Plugin Registries

This document describes the planned DI architecture for Pigeon Pea across hosts
(console, windows, Unity), with a focus on:

- Using **Pure.DI** as the static composition mechanism for engine/app-essential
  core on .NET hosts (console, windows).
- Using **VContainer** as the composition mechanism for Unity.
- Keeping **Microsoft.Extensions.DependencyInjection** as a configuration
  surface only (e.g. `IServiceCollection`, Scrutor), not as the primary
  runtime container.
- Using **Splat** as a runtime-mutable **service locator** for
  per-scene/per-character containers.
- Keeping **dynamic plugin content** (Resource service, GOAP content, etc.)
  flowing through structured **registries** instead of reconfiguring
  Pure.DI at runtime.

The goal is to preserve the plugin/GOAP flexibility while making the
engine/app-essential composition explicit, static, and host-agnostic.

---

## 1. Goals & Constraints

### 1.1 Goals

- **Host separation**
  - Console & Windows: use **Pure.DI** for engine/app-essential core.
  - Unity: use **VContainer** for engine/app-essential core.
- **Dynamic content via plugins**
  - Content-authoring assemblies provide Resource loaders, GOAP archetypes,
    GOAP executors, etc. via the plugin system.
  - Engine code does _not_ reference content-authoring directly.
- **Multi-container / multi-context support**
  - Support multiple logical containers, e.g.:
    - Per scene.
    - Per dungeon/level.
    - Potentially per character.
- **MS.DI for configuration, not core runtime**
  - Continue to use `IServiceCollection` + Scrutor + `AddPluginSystem()` as the
    configuration surface.
  - Do not rely on MS.DI as the primary runtime container for engine
    composition.

### 1.2 Constraints

- **Pure.DI is static**
  - Bindings are defined in `DI.Setup(...)` at compile time.
  - The generated composition cannot be dynamically mutated like
    `IServiceCollection`.
- **Plugins are dynamic**
  - Assemblies may be loaded/unloaded per profile.
  - Plugin content must be able to register/unregister services at runtime.
- **No map-related code changes**
  - All work must respect existing map/GOAP code boundaries.

---

## 2. Existing Building Blocks

### 2.1 App-level DI contracts (app-essential)

Located in `PigeonPea.Contracts.DependencyInjection`:

- `IAppServiceRoot : IServiceProvider, IAsyncDisposable`
  - Represents the application-level root container.
  - Exposes `IAppServiceScope CreateScope()` for sub-containers.

- `IAppServiceScope : IServiceProvider, IAsyncDisposable`
  - Represents a scoped container (per scene/character/etc.).

- `IAppServiceRootFactory`
  - `IAppServiceRoot Build(IServiceCollection services)`
  - Bridge from configuration (`IServiceCollection`) to a root container.

### 2.2 MS.DI adapter (PluginSystem)

Located in `PigeonPea.PluginSystem`:

- `MsDiAppServiceRoot`, `MsDiAppServiceScope`, `MsDiAppServiceRootFactory`.
- Provides a straightforward mapping from `IServiceCollection` to
  `IAppServiceRoot` using MS.DI.

### 2.3 Pure.DI composition project

Located in `PigeonPea.AppComposition.PureDi`:

- `AppComposition` partial class:

  ```csharp
  public partial class AppComposition
  {
      static void Setup() =>
          DI.Setup(nameof(AppComposition));
  }
  ```

- `PureDiAppServiceRootFactory : IAppServiceRootFactory` (currently delegating
  to `MsDiAppServiceRootFactory` as a placeholder).

### 2.4 Plugin system & resource tiered service

- `PigeonPea.PluginSystem` implements:
  - `PluginLoader`, `PluginHost`, `PluginRegistry`, `ServiceRegistry`,
    `IRegistry`, `EventBus`, `IPluginProfileLoader`.
- Resource tiered service:
  - Tier 1: `PigeonPea.Contracts.Resource.Services.IService`.
  - Tier 2: proxy class using `IRegistry` and source generator.
  - Tier 3: plugin-provided implementation registered into `IRegistry`.

These already use a **registry** abstraction instead of relying on DI runtime
mutation.

---

## 3. High-Level Architecture

We organize DI into three layers:

1. **Static app composition (host-specific)**
   - Pure.DI on console/windows.
   - VContainer on Unity.
   - Responsible for wiring **engine/app-essential** services.

2. **Structured dynamic registries**
   - `IRegistry` for generic services (Resource, renderers, etc.).
   - Future GOAP registries (`IGoapRegistry`, `IGoapActionExecutorFactory`).
   - Mutable objects: plugins can register/unregister content at runtime.

3. **Splat-based service locator** (optional, for flexible dynamic access)
   - An `IAppServiceLocator` abstraction backed by Splat.
   - Supports per-scene, per-character, or other contextual service graphs.

MS.DI remains as a **configuration format** (`IServiceCollection`) and an
optional fallback runtime container, but not the primary engine composition
mechanism for console/windows.

---

## 4. Static Composition: Pure.DI & VContainer

### 4.1 Console & Windows hosts: Pure.DI

- **Composition root**: `AppComposition` in `PigeonPea.AppComposition.PureDi`.
- **Bindings** (to be defined incrementally in `Setup()`):
  - Plugin system core:
    - `PluginRegistry`, `ServiceRegistry`, `EventBus`.
    - `IRegistry` (adapter over `ServiceRegistry` + `EventBus`).
    - `IPluginHost`, `PluginLoader`, `IPluginProfileLoader`.
  - App-level services as needed by engine and hosts.
  - Optionally `IAppServiceLocator` root instance.

- **App root implementation**:

  ```csharp
  public sealed class PureDiAppServiceRoot : IAppServiceRoot
  {
      // Holds AppComposition + optionally a fallback IServiceProvider
      // from MsDiAppServiceRootFactory for host-only / legacy registrations.
  }
  ```

- **Factory**:

  ```csharp
  public sealed class PureDiAppServiceRootFactory : IAppServiceRootFactory
  {
      public IAppServiceRoot Build(IServiceCollection services)
      {
          // 1. Create AppComposition (Pure.DI-generated partial).
          // 2. Construct core engine services via Pure.DI.
          // 3. Optionally build an MS.DI provider from `services` for
          //    host-only or legacy registrations.
          // 4. Wrap them in a PureDiAppServiceRoot.
      }
  }
  ```

In practice, `GetService(Type)` on `PureDiAppServiceRoot` will:

1. Try to resolve via the Pure.DI composition for **engine** services.
2. Optionally fall back to the MS.DI provider for host-only/legacy services.

### 4.2 Unity host: VContainer

- **Separate implementation**: `VContainerAppServiceRootFactory` (Unity-side
  project).
- Uses VContainer to compose engine/app-essential services.
- Exposes `IAppServiceRoot` and `IAppServiceScope` with VContainer-backed
  scopes.

The contracts (`IAppServiceRoot`, `IAppServiceScope`, `IAppServiceRootFactory`)
remain shared across hosts; only the factory/implementation differ.

---

## 5. Structured Dynamic Layer: Registries

Pure.DI does not support runtime mutation of the composition graph, but it **can
inject mutable services** that act as registries.

### 5.1 Existing registry: IRegistry

- `IRegistry` (and `ServiceRegistry`) already serve as a dynamic service
  registry for plugins.
- Pattern (Resource service example):
  - Tier 2 proxy uses `IRegistry` to locate Tier 3 implementations.
  - Tier 3 plugin implementations register themselves in `IRegistry` with
    metadata (`ServiceMetadata`, priorities, etc.).

This pattern stays and is extended, not replaced.

### 5.2 GOAP registries (planned)

From `GOAP.ContentPlugins.md`:

- **Contracts (engine-side)**:

  ```csharp
  public interface IGoapArchetype
  {
      string ArchetypeId { get; }
      void ConfigureAgent(
          Entity entity,
          ref GoapAgentComponent agent,
          ref GoalComponent goal,
          ref PlanComponent plan);
  }

  public interface IGoapContentModule
  {
      void Register(IGoapRegistry registry);
  }

  public interface IGoapRegistry
  {
      void RegisterGoalFactory(string id, Func<GoapGoal> factory);
      void RegisterActionFactory(string id, Func<GoapAction> factory);
  }

  public interface IGoapActionExecutorFactory
  {
      IActionExecutor GetExecutor(string executorId);
  }
  ```

- **Dynamic behavior**:
  - `IGoapRegistry` and `IGoapActionExecutorFactory` are mutable; plugins
    register goals/actions/executors at runtime.
  - GOAP runtime remains static and simply consumes these registries.

Pure.DI’s role:

- Construct the registry instances (`IGoapRegistry`,
  `IGoapActionExecutorFactory`) as part of the engine composition.
- Inject them into systems or services that need them.
- Plugins mutate them at runtime via well-defined methods, not via DI.

---

## 6. Splat-Based Service Locator Layer

For scenarios requiring **per-context containers** and ad hoc runtime
resolution (e.g. per-scene, per-character behavior), we introduce a service
locator abstraction.

### 6.1 App-level service locator contract

Define in `PigeonPea.Contracts.DependencyInjection` (conceptual shape):

```csharp
public interface IAppServiceLocator
{
    T Get<T>();
    object? Get(Type serviceType);

    void Register<T>(Func<T> factory);
    void Register(Type serviceType, Func<object> factory);

    IAppServiceLocator CreateScope();
}
```

Notes:

- `CreateScope()` enables per-scene/per-character locators.
- This interface is intentionally simple and host-agnostic.

### 6.2 Splat-based implementation

In a host-shared project (or per host), implement `IAppServiceLocator` using
**Splat**:

- Use `IMutableDependencyResolver` and/or `Locator.CurrentMutable` underneath.
- For scopes, create child resolvers that fall back to a parent for missing
  entries.
- Pure.DI composes an instance of `IAppServiceLocator` (root locator) into
  engine services that need flexible dynamic access.

Example uses:

- GOAP executors that need to reach host-specific systems (movement,
  pathfinding, ability systems) that are not known at engine compile time.
- Per-scene registries of services that should not live in the global
  plugin/engine registries.

### 6.3 Relationship with registries & DI

- **Registries** (`IRegistry`, `IGoapRegistry`, etc.) are structured, typed
  entry points for plugin content.
- **Service locator** is more ad hoc; useful when per-context or
  host-specific wiring is needed that doesn’t warrant a formal registry.
- Pure.DI injects the locator into components that need this flexibility.

---

## 7. Resource Service & GOAP in the new model

### 7.1 Resource service

- Tier 1/2/3 design remains intact:
  - Tier 1: `IService` contract in `PigeonPea.Contracts.Resource`.
  - Tier 2: proxy class relies on `IRegistry`, not DI.
  - Tier 3: plugin implementation registers in `IRegistry`.
- Under Pure.DI:
  - `IRegistry` and `ServiceRegistry` are constructed statically.
  - Plugin loader populates them at runtime.
- No resource-specific DI hacks are required.

### 7.2 GOAP content plugins

- Engine remains unchanged; it consumes `GoapGoal`, `GoapAction`, and
  `IActionExecutor` as opaque runtime objects.
- Plugins implement `IGoapArchetype` / `IGoapContentModule`, register their
  goals/actions/executors via `IGoapRegistry` and
  `IGoapActionExecutorFactory`.
- Service locator (if needed) provides per-scene/host-specific services to
  executors.

This keeps `game-essential` agnostic of content-authoring and aligns with the
plugin-based GOAP content model captured in `GOAP.ContentPlugins.md`.

---

## 8. Implementation Roadmap

This is the proposed sequence of work for the plugin/DI agent:

1. **Finalize contracts (app-essential)**
   - Ensure `IAppServiceRoot`, `IAppServiceScope`, `IAppServiceRootFactory`
     are stable (already done).
   - Add `IAppServiceLocator` to `PigeonPea.Contracts.DependencyInjection`.

2. **Introduce Splat-based locator implementation**
   - Add a host-shared or host-specific implementation of `IAppServiceLocator`
     using Splat.
   - Wire it into the current MS.DI-based root as a normal singleton so that
     engine code can start using it without waiting for Pure.DI.

3. **Extend Pure.DI composition for console/windows**
   - In `AppComposition.Setup()`, add bindings for:
     - Plugin system core (PluginLoader, PluginHost, registries, etc.).
     - `IAppServiceLocator` root instance.
   - Implement `PureDiAppServiceRoot` that uses `AppComposition` as the
     primary resolver and optionally falls back to MS.DI for host-only
     registrations.

4. **Switch console/windows hosts to PureDiAppServiceRootFactory**
   - Replace usages of `MsDiAppServiceRootFactory` with
     `PureDiAppServiceRootFactory` in console/windows hosts, once the
     composition is stable.
   - Verify Resource service and plugin loading work unchanged.

5. **Unity host: VContainer integration**
   - Implement `VContainerAppServiceRootFactory` and a VContainer-based
     `IAppServiceRoot` / `IAppServiceScope`.
   - Ensure it exposes the same contracts as the Pure.DI root.

6. **GOAP plugin registry implementation (later phase)**
   - Add concrete implementations of `IGoapRegistry` and
     `IGoapActionExecutorFactory` as mutable services.
   - Integrate plugin discovery to populate these registries at runtime.

Throughout all phases, **map-related code remains untouched**, and the
Resource service tiered design remains the flagship example of how plugins and
registries interact with the engine core.
