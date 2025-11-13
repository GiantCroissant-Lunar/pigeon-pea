# Issue #2: [RFC-005] Phase 2: Create contract projects for plugin system

**Labels:** `contracts`, `rfc-005`, `phase-2`, `infrastructure`

## Related RFC

RFC-005 Phase 2: Project Structure Reorganization

## Summary

Create contract projects to establish interfaces for plugin system and game events.

## Depends On

Issue #1 (project structure must exist)

## Scope

- Create `PigeonPea.Contracts` project
- Create `PigeonPea.Game.Contracts` project
- Define plugin system contracts
- Extract existing interfaces to contracts
- Update project dependencies

## Acceptance Criteria

### PigeonPea.Contracts Project

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

### PigeonPea.Game.Contracts Project

- [ ] `game-essential/core/PigeonPea.Game.Contracts/` project created
- [ ] `Events/` folder created (empty for now, populated in Issue #4)
- [ ] `Services/` folder for game services
- [ ] `Components/` folder for component contracts
- [ ] `Rendering/IRenderer.cs` interface defined
- [ ] `Rendering/RenderingCapabilities.cs` enum defined

### Integration

- [ ] Projects added to `PigeonPea.sln`
- [ ] `PigeonPea.Shared` references `PigeonPea.Game.Contracts`
- [ ] `PigeonPea.Game.Contracts` references `PigeonPea.Contracts`
- [ ] All projects build successfully

### Documentation

- [ ] XML documentation comments added to all public interfaces
- [ ] README created in each contract project explaining purpose

## Implementation Notes

- Use `netstandard2.1` for maximum compatibility
- Keep contracts minimal and stable (they're the API surface)
- Follow hyacinth-bean-base patterns from PLUGIN_SYSTEM_ANALYSIS.md
- No implementation code in contracts projects

## Interface Examples

### IPlugin

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

### IRegistry

```csharp
public interface IRegistry
{
    void Register<TService>(TService implementation, ServiceMetadata metadata) where TService : class;
    TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class;
    IEnumerable<TService> GetAll<TService>() where TService : class;
    bool IsRegistered<TService>() where TService : class;
    bool Unregister<TService>(TService implementation) where TService : class;
}
```

### IRenderer

```csharp
public interface IRenderer
{
    string Id { get; }
    RenderingCapabilities Capabilities { get; }

    void Initialize(RenderContext context);
    void Render(GameState state);
    void Shutdown();
}
```

## Estimated Effort

2-3 days

## Dependencies

- Issue #1 must be completed (folder structure must exist)

## See Also

- [RFC-005: Project Structure Reorganization](../rfc-005-project-structure-reorganization.md)
- [RFC-006: Plugin System Architecture](../rfc-006-plugin-system-architecture.md)
- [PLUGIN_SYSTEM_ANALYSIS.md](../../PLUGIN_SYSTEM_ANALYSIS.md)
