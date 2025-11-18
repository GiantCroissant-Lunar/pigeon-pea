# SkiaSharp Windows Renderer Plugin

This plugin provides hardware-accelerated SkiaSharp-based rendering for Windows desktop applications using Avalonia UI integration.

## Features

- Hardware-accelerated 2D graphics rendering via SkiaSharp
- Avalonia UI control integration for easy embedding
- GPU acceleration support
- Anti-aliased graphics and text rendering
- Resource management and cleanup
- Comprehensive logging and error handling

## Capabilities

- `renderer` - Provides rendering functionality
- `renderer:windows` - Windows-specific renderer
- `skiasharp` - SkiaSharp rendering engine
- `gpu-accelerated` - Hardware acceleration support

## Requirements

- Windows operating system
- .NET 9.0 or later
- SkiaSharp libraries (included as dependencies)
- Avalonia UI application host

## Implementation

### SkiaSharpRenderer

Implements `IRenderer` from `PigeonPea.Game.Contracts.Rendering`:

- `Initialize(RenderContext)` - Sets up SkiaSharp rendering control
- `Render(GameState)` - Triggers rendering of the current game state
- `Shutdown()` - Cleans up SkiaSharp resources
- `GetRenderControl()` - Returns the Avalonia SkiaRenderView for embedding

#### Key Features:

- **Hardware Acceleration**: Uses SkiaSharp's GPU backend when available
- **Double Buffering**: Automatic surface management for smooth rendering
- **Resource Management**: Proper disposal of SkiaSharp resources
- **Error Handling**: Comprehensive exception handling and logging
- **Test Pattern**: Includes a visual test pattern for validation

### SkiaSharpRendererPlugin

Implements `IPlugin` from `PigeonPea.Contracts.Plugin`:

- Registers the `SkiaSharpRenderer` with the service registry during initialization
- Registers the renderer both as `IRenderer` and as `SkiaSharpRenderer` for direct control access
- Handles plugin lifecycle (Initialize, Start, Stop)

## Usage

The plugin is automatically loaded by the plugin system if specified in the plugin manifest configuration. The renderer is registered with priority 100 and can be retrieved from the service registry:

```csharp
// Get the renderer interface
var renderer = registry.Get<IRenderer>();
renderer.Initialize(new RenderContext { Width = 800, Height = 600, Services = serviceProvider });
renderer.Render(gameState);

// Get the direct renderer for Avalonia control access
var skiaRenderer = registry.Get<SkiaSharpRenderer>();
var renderControl = skiaRenderer.GetRenderControl();
// Embed renderControl in your Avalonia application
```

### Avalonia Integration Example:

```csharp
public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private SkiaSharpRenderer? _renderer;

    public MainWindow(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();
        InitializeRenderer();
    }

    private void InitializeRenderer()
    {
        var registry = _serviceProvider.GetRequiredService<IServiceRegistry>();
        _renderer = registry.Get<SkiaSharpRenderer>();
        
        if (_renderer != null)
        {
            _renderer.Initialize(new RenderContext 
            { 
                Width = (int)Bounds.Width, 
                Height = (int)Bounds.Height,
                Services = _serviceProvider
            });

            var renderControl = _renderer.GetRenderControl();
            if (renderControl != null)
            {
                Content = renderControl;
            }
        }
    }

    protected override void OnResized(WindowResizedEventArgs e)
    {
        base.OnResized(e);
        
        if (_renderer?.Context != null)
        {
            _renderer.Context.Width = (int)Bounds.Width;
            _renderer.Context.Height = (int)Bounds.Height;
        }
    }
}
```

## Configuration

Plugin manifest (`plugin.json`):

```json
{
  "id": "rendering-windows-skiasharp",
  "name": "SkiaSharp Windows Renderer",
  "version": "1.0.0",
  "capabilities": ["renderer", "renderer:windows", "skiasharp", "gpu-accelerated"],
  "supportedProfiles": ["dotnet.windows"],
  "entryPoint": {
    "dotnet.windows": "PigeonPea.Plugins.Rendering.Windows.SkiaSharp.dll,PigeonPea.Plugins.Rendering.Windows.SkiaSharp.SkiaSharpRendererPlugin"
  }
}
```

## Performance Considerations

- **GPU Acceleration**: Automatically uses GPU backend when available
- **Resource Pooling**: SkiaSharp manages GPU resources efficiently
- **Dirty Region Tracking**: Can be implemented for selective updates
- **Frame Rate Control**: Render calls should be throttled to target FPS

## Future Enhancements

- Full game state rendering with tiles and entities
- Advanced SkiaSharp features (shaders, blend modes, masks)
- Performance optimizations with dirty region tracking
- Multi-threaded rendering support
- Custom shader support
- Texture atlasing and sprite rendering
- Font rendering integration

## Dependencies

- `SkiaSharp` - Core SkiaSharp rendering library
- `Avalonia` - Avalonia UI framework
- `Avalonia.Skia` - Avalonia SkiaSharp integration
- `Microsoft.Extensions.Logging.Abstractions` - Logging abstraction
- `PigeonPea.Contracts` - Plugin system contracts
- `PigeonPea.Game.Contracts` - Game rendering contracts

## Build Integration

The plugin includes a post-build target that automatically copies the compiled plugin to the Windows application's plugins directory, ensuring it's available for loading at runtime.
