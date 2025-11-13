# Implementation Plan: Project Restructure and Plugin System

**Date:** 2025-11-10
**Related RFCs:** RFC-005, RFC-006
**Status:** Ready for Implementation

## Overview

This document outlines the implementation plan for restructuring the PigeonPea project and adding plugin system support. Work is broken into 5 sequential issues that can be handled by agents in serial or parallel (where dependencies allow).

## Dependency Graph

```
Issue #1: Migrate Project Structure (RFC-005 Phase 1)
    ↓
Issue #2: Create Contract Projects (RFC-005 Phase 2)
    ↓
Issue #3: Implement Core Plugin System (RFC-006 Phase 1)
    ↓
Issue #4: Integrate Game Events (RFC-006 Phase 2)
    ↓
Issue #5: Create Rendering Plugin PoC (RFC-006 Phase 3)
```

**Parallel Execution:**

- Issues #1 and #2 can potentially run in parallel if careful
- Issues #3, #4, #5 must be sequential (each depends on previous)

## Issues to Create

### Issue #1: Migrate Project Structure to New Organization

**Title:** `[RFC-005] Phase 1: Migrate project structure to new organization`

**Labels:** `restructure`, `rfc-005`, `phase-1`, `breaking-change`

**Description:**

Migrate existing projects from flat structure to new tiered organization as defined in RFC-005.

**Related RFC:** RFC-005 Phase 1

**Scope:**

- Create new folder structure (`app-essential/`, `game-essential/`, etc.)
- Move existing projects to new locations
- Update `PigeonPea.sln` with new paths
- Update all `<ProjectReference>` paths in `.csproj` files
- Verify builds and tests pass

**Acceptance Criteria:**

- [ ] New folder structure created:
  - `app-essential/core/`
  - `app-essential/plugins/`
  - `game-essential/core/`
  - `game-essential/plugins/`
  - `windows-app/core/`
  - `windows-app/plugins/`
  - `windows-app/configs/`
  - `console-app/core/`
  - `console-app/plugins/`
  - `console-app/configs/`
- [ ] Projects moved:
  - `shared-app/` → `game-essential/core/PigeonPea.Shared/`
  - `shared-app.Tests/` → `game-essential/core/PigeonPea.Shared.Tests/`
  - `console-app/` → `console-app/core/PigeonPea.Console/`
  - `windows-app/` → `windows-app/core/PigeonPea.Windows/`
- [ ] Solution file updated with new project paths
- [ ] All project references updated
- [ ] `dotnet build` succeeds for all projects
- [ ] `dotnet test` passes all existing tests
- [ ] No functional regressions
- [ ] `ARCHITECTURE.md` updated with new structure
- [ ] `README.md` updated with new paths

**Implementation Notes:**

- Use `git mv` to preserve history
- Update solution file GUIDs if needed
- Test builds incrementally after each move
- Decision needed: Where do `benchmarks/`, `console-app.Tests/`, `windows-app.Tests/` go?

**Estimated Effort:** 1-2 days

---

### Issue #2: Create Contract Projects

**Title:** `[RFC-005] Phase 2: Create contract projects for plugin system`

**Labels:** `contracts`, `rfc-005`, `phase-2`, `infrastructure`

**Description:**

Create contract projects to establish interfaces for plugin system and game events.

**Related RFC:** RFC-005 Phase 2

**Depends On:** Issue #1 (project structure must exist)

**Scope:**

- Create `PigeonPea.Contracts` project
- Create `PigeonPea.Game.Contracts` project
- Define plugin system contracts
- Extract existing interfaces to contracts
- Update project dependencies

**Acceptance Criteria:**

- [ ] `app-essential/core/PigeonPea.Contracts/` project created
  - [ ] `Plugin/IPlugin.cs` interface defined
  - [ ] `Plugin/IPluginContext.cs` interface defined
  - [ ] `Plugin/IRegistry.cs` interface defined
  - [ ] `Plugin/IPluginHost.cs` interface defined
  - [ ] `Plugin/IEventBus.cs` interface defined
  - [ ] `Plugin/PluginManifest.cs` class defined
  - [ ] `Plugin/ServiceMetadata.cs` class defined
  - [ ] `DependencyInjection/` folder with DI contracts
  - [ ] `Services/` folder for service contracts
- [ ] `game-essential/core/PigeonPea.Game.Contracts/` project created
  - [ ] `Events/` folder created (empty for now, populated in Issue #4)
  - [ ] `Services/` folder for game services
  - [ ] `Components/` folder for component contracts
  - [ ] `Rendering/IRenderer.cs` interface defined
  - [ ] `Rendering/RenderingCapabilities.cs` enum defined
- [ ] Projects added to `PigeonPea.sln`
- [ ] `PigeonPea.Shared` references `PigeonPea.Game.Contracts`
- [ ] `PigeonPea.Game.Contracts` references `PigeonPea.Contracts`
- [ ] All projects build successfully
- [ ] XML documentation comments added to all public interfaces
- [ ] README created in each contract project explaining purpose

**Implementation Notes:**

- Use `netstandard2.1` for maximum compatibility
- Keep contracts minimal and stable (they're the API surface)
- Follow hyacinth-bean-base patterns from PLUGIN_SYSTEM_ANALYSIS.md
- No implementation code in contracts projects

**Estimated Effort:** 2-3 days

---

### Issue #3: Implement Core Plugin System

**Title:** `[RFC-006] Phase 1: Implement core plugin infrastructure`

**Labels:** `plugin-system`, `rfc-006`, `phase-1`, `infrastructure`

**Description:**

Implement the core plugin system: PluginLoader, ServiceRegistry, EventBus, and DI integration.

**Related RFC:** RFC-006 Phase 1

**Depends On:** Issue #2 (contracts must exist)

**Scope:**

- Create `PigeonPea.PluginSystem` project
- Implement `PluginLoader` with ALC isolation
- Implement `ServiceRegistry` with priority support
- Implement `EventBus` for pub/sub messaging
- Implement DI integration
- Write comprehensive unit tests

**Acceptance Criteria:**

- [ ] `app-essential/core/PigeonPea.PluginSystem/` project created
- [ ] `PluginLoader.cs` implemented:
  - [ ] `DiscoverAndLoadAsync()` discovers plugins from directories
  - [ ] Parses `plugin.json` manifests
  - [ ] Resolves dependencies and calculates load order
  - [ ] Creates isolated `AssemblyLoadContext` per plugin
  - [ ] Loads plugin assemblies
  - [ ] Instantiates plugin classes via reflection
  - [ ] Calls `InitializeAsync()` and `StartAsync()`
  - [ ] Tracks loaded plugins in `PluginRegistry`
  - [ ] `UnloadPluginAsync()` unloads plugins
  - [ ] `ReloadPluginAsync()` supports hot reload
- [ ] `ServiceRegistry.cs` implemented:
  - [ ] `Register<T>()` with priority support
  - [ ] `Get<T>()` with `SelectionMode` (One, HighestPriority, All)
  - [ ] `GetAll<T>()` returns all implementations
  - [ ] `IsRegistered<T>()` checks registration
  - [ ] `Unregister<T>()` removes services
  - [ ] Cross-ALC type matching by name
  - [ ] Priority-based sorting
- [ ] `EventBus.cs` implemented:
  - [ ] `Subscribe<TEvent>()` registers handlers
  - [ ] `PublishAsync<TEvent>()` invokes all subscribers
  - [ ] `Unsubscribe<TEvent>()` removes handlers
  - [ ] Type-safe event routing
  - [ ] Async handler support
- [ ] `PluginHost.cs` implemented (host services for plugins)
- [ ] `PluginContext.cs` implemented (context passed to plugins)
- [ ] `PluginRegistry.cs` implemented (tracks plugin states)
- [ ] `ManifestParser.cs` implemented (parses plugin.json)
- [ ] `DependencyResolver.cs` implemented (topological sort)
- [ ] `ServiceCollectionExtensions.cs` implemented:
  - [ ] `AddPluginSystem()` registers all services
  - [ ] `PluginLoaderHostedService` for background loading
- [ ] Unit tests written:
  - [ ] PluginLoader discovery tests
  - [ ] PluginLoader loading tests
  - [ ] ServiceRegistry tests (register/get/priority)
  - [ ] EventBus tests (subscribe/publish)
  - [ ] DependencyResolver tests
  - [ ] ManifestParser tests
  - [ ] Test coverage >80%
- [ ] Sample test plugin created for testing
- [ ] All tests pass
- [ ] XML documentation complete

**Implementation Notes:**

- Reference hyacinth-bean-base implementation in PLUGIN_SYSTEM_ANALYSIS.md
- Use `System.Runtime.Loader.AssemblyLoadContext` for ALC isolation
- Use `System.Text.Json` for manifest parsing
- Include detailed logging for debugging
- Hot reload is optional (can be disabled in production)

**Estimated Effort:** 3-5 days

---

### Issue #4: Integrate Game Events with Plugin System

**Title:** `[RFC-006] Phase 2: Integrate game events with plugin system`

**Labels:** `plugin-system`, `rfc-006`, `phase-2`, `game-logic`

**Description:**

Define game events in contracts and integrate EventBus into GameWorld to publish events for plugins.

**Related RFC:** RFC-006 Phase 2

**Depends On:** Issue #3 (plugin system must exist)

**Scope:**

- Define game event contracts
- Integrate `IEventBus` into `GameWorld`
- Publish events from game actions
- Create example plugin that subscribes to events
- Verify events flow correctly

**Acceptance Criteria:**

- [ ] Game events defined in `PigeonPea.Game.Contracts/Events/`:
  - [ ] `EntitySpawnedEvent.cs`
  - [ ] `EntityMovedEvent.cs`
  - [ ] `CombatEvent.cs`
  - [ ] Additional events as needed
- [ ] `GameWorld.cs` updated:
  - [ ] Constructor accepts `IEventBus` parameter
  - [ ] `SpawnEntity()` publishes `EntitySpawnedEvent`
  - [ ] `MoveEntity()` publishes `EntityMovedEvent`
  - [ ] Combat actions publish `CombatEvent`
  - [ ] Events published asynchronously (or synchronously if preferred)
- [ ] Example plugin created: `PigeonPea.Plugins.EventLogger`
  - [ ] Subscribes to all game events
  - [ ] Logs events to console
  - [ ] Located in `game-essential/plugins/PigeonPea.Plugins.EventLogger/`
  - [ ] Has `plugin.json` manifest
- [ ] Integration test verifies event flow:
  - [ ] GameWorld publishes event
  - [ ] EventLogger plugin receives event
  - [ ] Event data is correct
- [ ] Both console and Windows apps updated to use `IEventBus`
- [ ] No functional regressions
- [ ] Documentation updated

**Implementation Notes:**

- Use C# records for immutable events
- Consider synchronous vs asynchronous event publishing (perf implications)
- Ensure events are published _after_ state changes (not before)
- EventLogger is a development/debugging plugin (can be disabled in production)

**Estimated Effort:** 2-3 days

---

### Issue #5: Create Rendering Plugin Proof of Concept

**Title:** `[RFC-006] Phase 3: Create rendering plugin PoC`

**Labels:** `plugin-system`, `rfc-006`, `phase-3`, `rendering`

**Description:**

Convert console app rendering to plugin-based architecture. Create a simple ANSI renderer plugin as proof of concept.

**Related RFC:** RFC-006 Phase 3

**Depends On:** Issue #4 (event integration complete)

**Scope:**

- Define `IRenderer` contract
- Create ANSI renderer plugin
- Update console app to load renderer as plugin
- Verify rendering works identically via plugin
- Create plugin manifest config

**Acceptance Criteria:**

- [ ] `IRenderer` interface finalized in `PigeonPea.Game.Contracts/Rendering/`
  - [ ] `Initialize(RenderContext)` method
  - [ ] `Render(GameState)` method
  - [ ] `Shutdown()` method
  - [ ] `Id`, `Capabilities` properties
- [ ] `RenderingCapabilities` enum defined
- [ ] ANSI renderer plugin created:
  - [ ] `console-app/plugins/PigeonPea.Plugins.Rendering.Terminal.ANSI/`
  - [ ] `ANSIRendererPlugin.cs` implements `IPlugin`
  - [ ] `ANSIRenderer.cs` implements `IRenderer`
  - [ ] `plugin.json` manifest with:
    - `id: "rendering-terminal-ansi"`
    - `capabilities: ["renderer", "renderer:terminal", "ansi"]`
    - `supportedProfiles: ["dotnet.console"]`
  - [ ] Renders game using ANSI escape codes
- [ ] Console app updated:
  - [ ] `PigeonPea.Console` references plugin system
  - [ ] Startup configures plugin system
  - [ ] Loads renderer from `ServiceRegistry`
  - [ ] Uses `IRenderer` interface (not hardcoded rendering)
  - [ ] Falls back gracefully if no renderer found
- [ ] Plugin manifest created:
  - [ ] `console-app/configs/plugin-manifest.json`
  - [ ] Specifies ANSI renderer to load
  - [ ] Documents plugin paths
- [ ] Console app runs with plugin-based rendering
- [ ] Visual output identical to before
- [ ] No performance regressions
- [ ] Documentation updated:
  - [ ] How to create a renderer plugin
  - [ ] How to configure plugins in apps
  - [ ] Example plugin walkthrough

**Implementation Notes:**

- Start with ANSI as it's simplest (no external dependencies)
- Future: Create Kitty, Sixel, SkiaSharp renderer plugins
- Consider renderer capability detection (auto-select best renderer)
- Renderer should be hot-reloadable for development

**Estimated Effort:** 3-4 days

---

## Total Timeline

**Sequential execution:** 11-17 days (2.2-3.4 weeks)

**Parallel execution (where possible):** 9-14 days (1.8-2.8 weeks)

## Post-Implementation

After all issues complete:

1. **Code Review** - Review all changes for quality and consistency
2. **Integration Testing** - End-to-end testing of plugin system
3. **Performance Testing** - Benchmark plugin loading and event overhead
4. **Documentation** - Complete user-facing documentation
5. **Release Notes** - Document breaking changes and migration guide

## Creating GitHub Issues

To create these issues on GitHub:

```bash
# Using GitHub CLI (gh)
gh issue create --title "[RFC-005] Phase 1: Migrate project structure" \
  --body-file docs/rfcs/issues/issue-1-migrate-structure.md \
  --label "restructure,rfc-005,phase-1,breaking-change"

gh issue create --title "[RFC-005] Phase 2: Create contract projects" \
  --body-file docs/rfcs/issues/issue-2-create-contracts.md \
  --label "contracts,rfc-005,phase-2,infrastructure"

gh issue create --title "[RFC-006] Phase 1: Implement core plugin system" \
  --body-file docs/rfcs/issues/issue-3-plugin-system.md \
  --label "plugin-system,rfc-006,phase-1,infrastructure"

gh issue create --title "[RFC-006] Phase 2: Integrate game events" \
  --body-file docs/rfcs/issues/issue-4-game-events.md \
  --label "plugin-system,rfc-006,phase-2,game-logic"

gh issue create --title "[RFC-006] Phase 3: Create rendering plugin PoC" \
  --body-file docs/rfcs/issues/issue-5-rendering-plugin.md \
  --label "plugin-system,rfc-006,phase-3,rendering"
```

Or manually create issues on GitHub using the descriptions above.

## Agent Assignment

**Option 1: Single Agent Sequential**

- One agent handles all issues in order
- Simplest coordination
- Longest timeline

**Option 2: Multiple Agents Parallel (with coordination)**

- Agent A: Issue #1 (structure migration)
- Agent B: Issue #2 (contracts) - starts after #1 completes
- Agent C: Issues #3, #4, #5 (plugin system) - starts after #2 completes
- Faster completion
- Requires coordination

**Option 3: Phased Assignment**

- Phase 1: Agent handles Issues #1-#2
- Phase 2: Agent handles Issues #3-#5
- Balanced approach

## Success Metrics

- [ ] All 5 issues completed and closed
- [ ] All acceptance criteria met
- [ ] Zero failing tests
- [ ] Documentation complete
- [ ] Both Windows and Console apps run with plugin system
- [ ] At least one working plugin (ANSI renderer)
- [ ] RFC-005 and RFC-006 status updated to "Implemented"

## Questions

- Should `benchmarks/` be kept at root or moved?
- Should test projects be separate or merged into main projects?
- Where should `console-app.Tests` and `windows-app.Tests` go?
- What priority should agents tackle issues (sequential vs parallel)?
