# PigeonPea.Rendering.Contracts

Multi-backend rendering architecture for PigeonPea. This package provides the core contracts and abstractions for implementing platform-agnostic domain renderers and platform-specific rendering backends.

## Architecture Overview

The rendering architecture consists of three layers:

1. **Domain Layer**: Domain-specific renderers (world map, dungeon, UI) that know _what_ to render
2. **Abstraction Layer**: Command-based interface (`IRenderCommandList`) that bridges domain and backend
3. **Backend Layer**: Platform-specific implementations (ANSI, Braille, SkiaSharp) that know _how_ to render

## Core Interfaces

### IRenderCommandList

Backend-agnostic rendering command list. Domain renderers submit commands to this interface.

```csharp
public interface IRenderCommandList
{
    RenderingCapabilities Capabilities { get; }

    void BeginFrame();
    void EndFrame();
    void Clear(Color color);

    void DrawTile(int x, int y, Tile tile);
    void DrawTiles(ReadOnlySpan<TileCommand> commands);
    void DrawBuffer(int x, int y, int width, int height, ReadOnlySpan<byte> rgba);
    void DrawSprite(int x, int y, string spriteId, Color? tint = null);
    void DrawText(int x, int y, string text, Color foreground, Color background);

    void SetViewport(Viewport viewport);
    void SetCamera(int centerX, int centerY, double zoom);
}
```

### IRenderBackend

Platform-specific rendering backend that executes commands.

```csharp
public interface IRenderBackend : IDisposable
{
    string Id { get; }
    RenderingCapabilities Capabilities { get; }

    void Initialize(RenderContext context);
    void Shutdown();
    void Execute(IRenderCommandList commands);
    void Present();
}
```

### IDomainRenderer

Domain-specific renderer that submits commands.

```csharp
public interface IDomainRenderer
{
    string Id { get; }
    void Render(object world, IRenderCommandList commands, RenderOptions options);
}
```

## Example Usage

### Console Application

```csharp
// Create and initialize backend
var backend = new ANSIBackend();
backend.Initialize(new RenderContext(Width: 120, Height: 40));

// Create command list
var commandList = new RenderCommandList(backend);

// Get domain renderer
var dungeonRenderer = registry.Get<IDomainRenderer>("dungeon-domain-renderer");

// Game loop
while (running)
{
    HandleInput();
    UpdateGameLogic();

    // Render
    dungeonRenderer.Render(world, commandList, renderOptions);
    backend.Execute(commandList);
    backend.Present();
}

backend.Shutdown();
```

### Simple Domain Renderer

```csharp
public class SimpleDungeonRenderer : IDomainRenderer
{
    public string Id => "simple-dungeon-renderer";

    public void Render(object world, IRenderCommandList commands, RenderOptions options)
    {
        commands.BeginFrame();
        commands.Clear(Color.Black);
        commands.SetViewport(options.Viewport);

        // Draw dungeon tiles
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var tile = GetTileAt(x, y);
                commands.DrawTile(x, y, tile);
            }
        }

        // Draw player
        commands.DrawTile(playerX, playerY, playerTile);

        commands.EndFrame();
    }
}
```

## Backend Capabilities

Different backends support different rendering features:

| Backend   | Tile        | Buffer    | Sprite    | Antialiasing | Mode   |
| --------- | ----------- | --------- | --------- | ------------ | ------ |
| ANSI      | ✅ Native   | ❌ No     | ❌ No     | ❌ No        | Tile   |
| Braille   | ⚠️ Emulated | ✅ Native | ❌ No     | ❌ No        | Buffer |
| SkiaSharp | ✅ Emulated | ✅ Native | ✅ Native | ✅ Yes       | Hybrid |

Domain renderers can query `commands.Capabilities` to determine the optimal rendering strategy for each backend.

## Implementation Status

- ✅ Core contracts defined
- ✅ ANSI backend implemented
- ✅ Braille backend implemented
- ⏳ SkiaSharp backend (pending)
- ⏳ Domain renderer migration (pending)

See [RFC-032](../../../../../docs/rfcs/032-multi-backend-rendering-architecture.md) for the full architecture specification.
