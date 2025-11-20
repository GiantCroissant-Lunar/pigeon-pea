# Nexus Camera2D Integration with Rendering

This document explains how to integrate the Nexus Camera2D system with the rendering pipeline in PigeonPea.

## Overview

The camera system provides automatic viewport management that can be used by renderers to determine what portion of the game world should be displayed. The integration bridges the gap between:

- **Camera System**: Uses `NexusCamera2D.Core.CameraTransform` with float-based world coordinates
- **Rendering System**: Uses `PigeonPea.Shared.Rendering.Viewport` with integer-based screen coordinates

## Quick Start

### Basic Usage in a Renderer

```csharp
using PigeonPea.Game.Camera.Extensions;
using PigeonPea.Shared.Rendering;

public class MyRenderer
{
    public void Render(World world, IRenderer renderer, int screenWidth, int screenHeight)
    {
        // Get the current viewport from the camera system
        var viewport = world.GetMainCameraViewport(screenWidth, screenHeight);
        if (!viewport.HasValue)
        {
            // No camera found - use default viewport
            viewport = new Viewport(0, 0, screenWidth, screenHeight);
        }
        
        // Set the viewport on the renderer
        renderer.SetViewport(viewport.Value);
        
        // Render your content using the viewport
        RenderContent(renderer, viewport.Value);
    }
}
```

### Using CameraUpdateSystem Directly

```csharp
using PigeonPea.Game.Camera.Systems;

public class MyRenderingSystem
{
    private readonly CameraUpdateSystem _cameraSystem = new();
    
    public void Render(World world, IRenderer renderer, int screenWidth, int screenHeight)
    {
        // Update camera first
        _cameraSystem.Update(world, deltaTime);
        
        // Get viewport
        var viewport = CameraUpdateSystem.GetCurrentViewport(world, screenWidth, screenHeight);
        if (!viewport.HasValue) return;
        
        // Use viewport for rendering
        renderer.SetViewport(viewport.Value);
        // ... render content
    }
}
```

## Coordinate System Conversion

The camera system works with world coordinates, while renderers work with screen coordinates. The `CameraViewportConverter` provides utilities for conversion:

### World to Screen Coordinates

```csharp
using NexusCamera2D.Core;

// Convert a world position to screen coordinates
Vector2 worldPos = new Vector2(100.5f, 75.2f);
var (screenX, screenY) = worldPos.WorldToScreen(viewport);

// Check if a world position is visible
bool isVisible = worldPos.IsVisible(viewport);
```

### Screen to World Coordinates

```csharp
// Convert screen coordinates back to world coordinates
Vector2 worldPos = CameraViewportConverter.ScreenToWorld(screenX, screenY, viewport);
```

## Zoom Handling

The camera system supports zoom, which affects how the viewport is calculated:

### Default Zoom Calculation

```csharp
// Simple zoom - camera zoom directly affects viewport size
var viewport = cameraTransform.ToViewport(screenWidth, screenHeight);
```

### Tile-Based Zoom Calculation

```csharp
// For tile-based games where you want to control zoom in terms of tile size
float baseTileSize = 1.0f; // Size of one tile in world units
var viewport = cameraTransform.ToViewport(screenWidth, screenHeight, baseTileSize);
```

## Integration with Existing Renderers

### MapDataRenderer Integration

The `MapDataRenderer` already accepts a `Viewport` parameter, so integration is straightforward:

```csharp
public void RenderMap(IRenderer renderer, MapData map, World world, int screenWidth, int screenHeight)
{
    var viewport = world.GetMainCameraViewport(screenWidth, screenHeight) ?? new Viewport(0, 0, screenWidth, screenHeight);
    
    // Calculate zoom based on camera zoom
    var cameraComponent = world.GetMainCameraComponent();
    double zoom = cameraComponent?.Camera.Transform.Zoom ?? 1.0;
    
    MapDataRenderer.Draw(renderer, map, viewport, showDungeonOverlay: true, zoom: zoom);
}
```

### Custom Renderer Integration

For custom renderers, follow this pattern:

```csharp
public class CustomRenderer
{
    public void RenderEntities(World world, IRenderer renderer, int screenWidth, int screenHeight)
    {
        // Get viewport
        var viewport = world.GetMainCameraViewport(screenWidth, screenHeight);
        if (!viewport.HasValue) return;
        
        renderer.SetViewport(viewport.Value);
        
        // Query for renderable entities
        var renderableQuery = new QueryDescription()
            .WithAll<PositionComponent, RenderableComponent>();
        
        world.Query(in renderableQuery, (Entity entity, ref PositionComponent pos, ref RenderableComponent rend) =>
        {
            // Convert world position to screen coordinates
            var worldPos = new Vector2(pos.X, pos.Y);
            var (screenX, screenY) = worldPos.WorldToScreen(viewport.Value);
            
            // Only render if visible
            if (worldPos.IsVisible(viewport.Value))
            {
                renderer.DrawTile(screenX, screenY, new Tile(rend.Glyph, rend.Foreground, rend.Background));
            }
        });
    }
}
```

## Camera Setup

### Creating a Camera Entity

```csharp
using NexusCamera2D.Core;
using PigeonPea.Game.Camera.Components;

// Create a camera
var camera = new Camera2D();
var cameraComponent = new CameraComponent(camera);

// Add it to the world
world.SetMainCamera(cameraComponent);
```

### Setting Camera Targets

```csharp
using PigeonPea.Game.Camera.Components;
using System.Numerics;

// Add camera target to player entity
playerEntity.Add(new CameraTargetComponent(
    weight: 1.0f,
    offset: Vector2.Zero
));

// The camera will now follow the player
```

## Best Practices

1. **Always check for null viewport**: The camera might not exist, so always handle the null case.
2. **Update camera before rendering**: Call `CameraUpdateSystem.Update()` before getting the viewport.
3. **Use viewport culling**: Only render entities that are visible within the viewport for performance.
4. **Handle coordinate conversion**: Use the provided conversion utilities rather than manual calculations.
5. **Consider zoom**: Remember that zoom affects the relationship between world and screen coordinates.

## Performance Considerations

- The viewport calculation is lightweight and can be called every frame
- Use `IsVisible()` to cull entities outside the viewport
- Cache the viewport if you need it multiple times in the same frame
- Consider using spatial partitioning for large worlds to reduce query overhead

## Troubleshooting

### Camera Not Found
- Ensure you've created a camera entity using `world.SetMainCamera()`
- Check that the camera entity hasn't been destroyed

### Incorrect Viewport Coordinates
- Verify you're using the correct screen dimensions
- Check if zoom is affecting your calculations as expected
- Ensure coordinate conversion is done in the right direction

### Performance Issues
- Implement viewport culling to avoid rendering off-screen entities
- Consider using spatial partitioning for large worlds
- Profile your rendering queries to identify bottlenecks