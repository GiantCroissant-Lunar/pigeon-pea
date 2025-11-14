---
doc_id: "PLAN-2025-00007"
title: "Issue #5: [RFC-006] Phase 3: Create rendering plugin proof of concept"
doc_type: "plan"
status: "active"
canonical: true
created: "2025-11-10"
tags: ["issue", "plugin-system", "rfc-006", "phase-3", "rendering"]
summary: "Create rendering plugin proof of concept as defined in RFC-006 Phase 3"
supersedes: []
related: ["RFC-2025-00006", "PLAN-2025-00001", "PLAN-2025-00006"]
---

# Issue #5: [RFC-006] Phase 3: Create rendering plugin proof of concept

**Labels:** `plugin-system`, `rfc-006`, `phase-3`, `rendering`

## Related RFC

RFC-006 Phase 3: Plugin System Architecture

## Summary

Convert console app rendering to plugin-based architecture. Create a simple ANSI renderer plugin as proof of concept.

## Depends On

Issue #4 (event integration complete)

## Scope

- Define `IRenderer` contract (if not already done in Issue #2)
- Create ANSI renderer plugin
- Update console app to load renderer as plugin
- Verify rendering works identically via plugin
- Create plugin manifest config

## Acceptance Criteria

### IRenderer Contract

- [ ] `IRenderer` interface finalized in `PigeonPea.Game.Contracts/Rendering/`
  - [ ] `Initialize(RenderContext)` method
  - [ ] `Render(GameState)` method
  - [ ] `Shutdown()` method
  - [ ] `Id`, `Capabilities` properties
- [ ] `RenderingCapabilities` enum defined
- [ ] `RenderContext` class defined
- [ ] XML documentation complete

### ANSI Renderer Plugin

- [ ] ANSI renderer plugin created:
  - [ ] `console-app/plugins/PigeonPea.Plugins.Rendering.Terminal.ANSI/`
  - [ ] `ANSIRendererPlugin.cs` implements `IPlugin`
  - [ ] `ANSIRenderer.cs` implements `IRenderer`
  - [ ] Registers renderer in `ServiceRegistry` during `InitializeAsync()`
  - [ ] Renders game using ANSI escape codes
  - [ ] Handles colors, cursor positioning
  - [ ] Clears and redraws efficiently

### Plugin Manifest

- [ ] `plugin.json` manifest created with:
  - `id: "rendering-terminal-ansi"`
  - `name: "ANSI Terminal Renderer"`
  - `version: "1.0.0"`
  - `capabilities: ["renderer", "renderer:terminal", "ansi"]`
  - `supportedProfiles: ["dotnet.console"]`
  - `entryPoint: {"dotnet.console": "PigeonPea.Plugins.Rendering.Terminal.ANSI.dll,PigeonPea.Plugins.Rendering.Terminal.ANSI.ANSIRendererPlugin"}`

### Console App Integration

- [ ] `PigeonPea.Console` updated:
  - [ ] References `PigeonPea.PluginSystem`
  - [ ] Startup configures plugin system via `AddPluginSystem()`
  - [ ] Loads renderer from `ServiceRegistry.Get<IRenderer>()`
  - [ ] Uses `IRenderer` interface (not hardcoded rendering)
  - [ ] Falls back gracefully if no renderer found (error message)
  - [ ] Calls `Initialize()`, `Render()`, `Shutdown()` at appropriate times

### Plugin Configuration

- [ ] Plugin manifest created:
  - [ ] `console-app/configs/plugin-manifest.json`
  - [ ] Specifies ANSI renderer to load
  - [ ] Documents plugin paths
  - [ ] Example:
    ```json
    {
      "profile": "dotnet.console",
      "pluginPaths": ["../../app-essential/plugins", "../../game-essential/plugins", "./plugins"],
      "plugins": [
        {
          "id": "rendering-terminal-ansi",
          "enabled": true,
          "priority": 100
        }
      ]
    }
    ```

### Testing and Verification

- [ ] Console app runs with plugin-based rendering
- [ ] Visual output identical to before migration
- [ ] No performance regressions (measure FPS if applicable)
- [ ] Plugin can be disabled (app shows error if no renderer)
- [ ] Integration test verifies renderer loading

### Documentation

- [ ] README in ANSI renderer plugin explaining implementation
- [ ] How to create a renderer plugin guide
- [ ] How to configure plugins in apps guide
- [ ] Example plugin walkthrough
- [ ] ARCHITECTURE.md updated with renderer plugin flow

## Implementation Notes

- Start with ANSI as it's simplest (no external dependencies)
- Future: Create Kitty, Sixel, SkiaSharp renderer plugins
- Consider renderer capability detection (auto-select best renderer)
- Renderer should be hot-reloadable for development
- Ensure proper cleanup in `Shutdown()` method

## IRenderer Interface

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

## ANSIRendererPlugin Example

```csharp
namespace PigeonPea.Plugins.Rendering.Terminal.ANSI;

public class ANSIRendererPlugin : IPlugin
{
    private ILogger? _logger;
    private ANSIRenderer? _renderer;

    public string Id => "rendering-terminal-ansi";
    public string Name => "ANSI Terminal Renderer";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        _logger = context.Logger;

        // Create and register renderer
        _renderer = new ANSIRenderer(_logger);
        context.Registry.Register<IRenderer>(
            _renderer,
            new ServiceMetadata
            {
                Priority = 100,
                Name = "ANSIRenderer",
                Version = "1.0.0"
            }
        );

        _logger.LogInformation("ANSI terminal renderer registered");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _logger?.LogInformation("ANSI renderer started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _renderer?.Shutdown();
        _logger?.LogInformation("ANSI renderer stopped");
        return Task.CompletedTask;
    }
}
```

## Console App Startup Example

```csharp
// In PigeonPea.Console/Program.cs
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

// Get renderer from plugin system
var registry = host.Services.GetRequiredService<IRegistry>();
var renderer = registry.Get<IRenderer>();

if (renderer == null)
{
    Console.WriteLine("Error: No renderer plugin loaded!");
    return 1;
}

// Initialize and use renderer
renderer.Initialize(new RenderContext { Width = 80, Height = 24 });

// Game loop
while (running)
{
    // Update game state
    gameWorld.Update(deltaTime);

    // Render
    renderer.Render(gameWorld.GetState());
}

renderer.Shutdown();
```

## Future Enhancements

- Auto-detection of best renderer for current terminal
- Fallback chain: Kitty → Sixel → ANSI
- Hot reload of renderers during development
- Renderer configuration options (color scheme, font size, etc.)

## Estimated Effort

3-4 days

## Dependencies

- Issue #4 must be completed (event integration complete)
- Issue #3 must be completed (plugin system must exist)
- Issue #2 must be completed (contracts must exist)

## See Also

- [RFC-006: Plugin System Architecture](../006-plugin-system-architecture.md)
- [PLUGIN_SYSTEM_ANALYSIS.md](../../PLUGIN_SYSTEM_ANALYSIS.md)
