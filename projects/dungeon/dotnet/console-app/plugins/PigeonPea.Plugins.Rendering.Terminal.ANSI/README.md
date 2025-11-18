# ANSI Terminal Renderer Plugin

This plugin provides basic ANSI escape code based rendering for terminal applications.

## Features

- ANSI color support (24-bit RGB)
- Cursor positioning via ANSI escape sequences
- Screen clearing and buffer management
- UTF-8 text rendering

## Capabilities

- `renderer` - Provides rendering functionality
- `renderer:terminal` - Terminal-specific renderer
- `ansi` - ANSI escape code support

## Requirements

- Terminal that supports ANSI escape sequences
- UTF-8 encoding support

## Implementation

### ANSIRenderer

Implements `IRenderer` from `PigeonPea.Game.Contracts.Rendering`:

- `Initialize(RenderContext)` - Sets up console for ANSI rendering
- `Render(GameState)` - Renders the current game state using ANSI codes
- `Shutdown()` - Cleans up and resets console state

### ANSIRendererPlugin

Implements `IPlugin` from `PigeonPea.Contracts.Plugin`:

- Registers the `ANSIRenderer` with the service registry during initialization
- Handles plugin lifecycle (Initialize, Start, Stop)

## Usage

The plugin is automatically loaded by the plugin system if specified in the plugin manifest configuration. The renderer is registered with priority 100 and can be retrieved from the service registry:

```csharp
var renderer = registry.Get<IRenderer>();
renderer.Initialize(new RenderContext { Width = 80, Height = 24, Services = serviceProvider });
renderer.Render(gameState);
renderer.Shutdown();
```

## Configuration

Plugin manifest (`plugin.json`):

```json
{
  "id": "rendering-terminal-ansi",
  "name": "ANSI Terminal Renderer",
  "version": "1.0.0",
  "capabilities": ["renderer", "renderer:terminal", "ansi"],
  "supportedProfiles": ["dotnet.console"],
  "entryPoint": {
    "dotnet.console": "PigeonPea.Plugins.Rendering.Terminal.ANSI.dll,PigeonPea.Plugins.Rendering.Terminal.ANSI.ANSIRendererPlugin"
  }
}
```

## Future Enhancements

- Full game state rendering with tiles and entities
- Advanced ANSI features (bold, italic, underline)
- Color palette optimization for 256-color terminals
- Performance optimizations with dirty region tracking
