---
doc_id: 'PLAN-2025-00005'
title: 'Issue #3: [RFC-006] Phase 1: Implement core plugin infrastructure'
doc_type: 'plan'
status: 'active'
canonical: true
created: '2025-11-10'
tags: ['issue', 'plugin-system', 'rfc-006', 'phase-1', 'infrastructure']
summary: 'Implement core plugin infrastructure as defined in RFC-006 Phase 1'
supersedes: []
related: ['RFC-2025-00006', 'PLAN-2025-00001', 'PLAN-2025-00004']
---

# Issue #3: [RFC-006] Phase 1: Implement core plugin infrastructure

**Labels:** `plugin-system`, `rfc-006`, `phase-1`, `infrastructure`

## Related RFC

RFC-006 Phase 1: Plugin System Architecture

## Summary

Implement the core plugin system: PluginLoader, ServiceRegistry, EventBus, and DI integration.

## Depends On

Issue #2 (contracts must exist)

## Scope

- Create `PigeonPea.PluginSystem` project
- Implement `PluginLoader` with ALC isolation
- Implement `ServiceRegistry` with priority support
- Implement `EventBus` for pub/sub messaging
- Implement DI integration
- Write comprehensive unit tests

## Acceptance Criteria

### Project Setup

- [ ] `app-essential/core/PigeonPea.PluginSystem/` project created
- [ ] Project added to `PigeonPea.sln`
- [ ] References `PigeonPea.Contracts`
- [ ] Package references added:
  - `Microsoft.Extensions.DependencyInjection`
  - `Microsoft.Extensions.Logging.Abstractions`
  - `Microsoft.Extensions.Configuration.Abstractions`
  - `System.Text.Json` (for manifest parsing)

### PluginLoader Implementation

- [ ] `PluginLoader.cs` implemented with:
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

### ServiceRegistry Implementation

- [ ] `ServiceRegistry.cs` implemented with:
  - [ ] `Register<T>()` with priority support
  - [ ] `Get<T>()` with `SelectionMode` (One, HighestPriority, All)
  - [ ] `GetAll<T>()` returns all implementations
  - [ ] `IsRegistered<T>()` checks registration
  - [ ] `Unregister<T>()` removes services
  - [ ] Cross-ALC type matching by name
  - [ ] Priority-based sorting

### EventBus Implementation

- [ ] `EventBus.cs` implemented with:
  - [ ] `Subscribe<TEvent>()` registers handlers
  - [ ] `PublishAsync<TEvent>()` invokes all subscribers
  - [ ] `Unsubscribe<TEvent>()` removes handlers
  - [ ] Type-safe event routing
  - [ ] Async handler support

### Supporting Classes

- [ ] `PluginHost.cs` implemented (host services for plugins)
- [ ] `PluginContext.cs` implemented (context passed to plugins)
- [ ] `PluginRegistry.cs` implemented (tracks plugin states)
- [ ] `ManifestParser.cs` implemented (parses plugin.json)
- [ ] `DependencyResolver.cs` implemented (topological sort)
- [ ] `PluginLoadContext.cs` implemented (custom AssemblyLoadContext)

### DI Integration

- [ ] `ServiceCollectionExtensions.cs` implemented with:
  - [ ] `AddPluginSystem()` registers all services
  - [ ] `PluginLoaderHostedService` for background loading
  - [ ] Proper service lifetimes (singleton for registries)

### Unit Tests

- [ ] PluginLoader discovery tests
- [ ] PluginLoader loading tests
- [ ] ServiceRegistry tests (register/get/priority)
- [ ] EventBus tests (subscribe/publish)
- [ ] DependencyResolver tests
- [ ] ManifestParser tests
- [ ] Test coverage >80%
- [ ] Sample test plugin created for testing
- [ ] All tests pass

### Documentation

- [ ] XML documentation complete for all public APIs
- [ ] README in project explaining architecture
- [ ] Code comments for complex logic (ALC, reflection)

## Implementation Notes

- Reference hyacinth-bean-base implementation in PLUGIN_SYSTEM_ANALYSIS.md
- Use `System.Runtime.Loader.AssemblyLoadContext` for ALC isolation
- Use `System.Text.Json` for manifest parsing
- Include detailed logging for debugging
- Hot reload is optional (can be disabled in production)

## Key Implementation Details

### AssemblyLoadContext Isolation

```csharp
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath, bool isCollectible)
        : base(isCollectible: isCollectible)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to resolve from plugin directory first
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
            return LoadFromAssemblyPath(assemblyPath);

        // Fall back to default context for shared assemblies
        return null;
    }
}
```

### Cross-ALC Type Matching

```csharp
// Check by name, not by reference equality
var implementsIPlugin = pluginType.GetInterfaces()
    .Any(i => i.FullName == typeof(IPlugin).FullName);
```

### Dependency Resolution

```csharp
public class DependencyResolver
{
    public ResolveResult Resolve(List<PluginManifest> manifests)
    {
        // Build dependency graph
        // Detect cycles
        // Topological sort
        // Return load order
    }
}
```

## Estimated Effort

3-5 days

## Dependencies

- Issue #2 must be completed (contracts must exist)

## See Also

- [RFC-006: Plugin System Architecture](../006-plugin-system-architecture.md)
- [PLUGIN_SYSTEM_ANALYSIS.md](../../PLUGIN_SYSTEM_ANALYSIS.md)
- [Assembly Load Context Documentation](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
