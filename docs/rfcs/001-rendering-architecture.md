---
doc_id: 'RFC-2025-00001'
title: 'Rendering Architecture'
doc_type: 'rfc'
status: 'draft'
canonical: true
created: '2025-11-08'
tags: ['rendering', 'architecture', 'graphics', 'terminal', 'skiasharp']
summary: 'Unified rendering architecture supporting both high-fidelity desktop graphics (Windows/SkiaSharp) and multi-tier terminal graphics (Console/Kitty/Sixel/Braille/ASCII) with a shared abstraction layer'
supersedes: []
related: []
---

# RFC-001: Rendering Architecture

## Status

**Status**: Draft
**Created**: 2025-11-08
**Author**: Development Team

## Summary

Establish a unified rendering architecture that supports both high-fidelity desktop graphics (Windows/SkiaSharp) and multi-tier terminal graphics (Console/Kitty/Sixel/Braille/ASCII) while maintaining a shared abstraction layer.

## Motivation

The current rendering implementation is tightly coupled to platform-specific code. As we add advanced rendering features (sprites, particles, animations for Windows; advanced terminal graphics for Console), we need:

1. **Abstraction**: Clean separation between game logic and rendering
2. **Flexibility**: Easy to add new renderers or rendering modes
3. **Performance**: Minimal overhead from abstractions
4. **Testability**: Ability to verify rendering output programmatically

## Design

### Rendering Abstraction Layers

```
┌─────────────────────────────────────────────────────┐
│                  Game Logic Layer                   │
│              (shared-app/GameWorld.cs)              │
└────────────────────┬────────────────────────────────┘
                     │
                     │ Render Commands / State Queries
                     │
┌────────────────────▼────────────────────────────────┐
│             Rendering Abstraction Layer             │
│               (shared-app/Rendering/)               │
│  ┌────────────────────────────────────────────┐    │
│  │  IRenderer                                 │    │
│  │  IRenderTarget                             │    │
│  │  RenderCommand (struct)                    │    │
│  │  Camera / Viewport                         │    │
│  └────────────────────────────────────────────┘    │
└────────────────────┬────────────────────────────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
┌────────▼────────┐    ┌─────────▼────────┐
│  Windows Impl   │    │   Console Impl   │
│                 │    │                  │
│  SkiaSharp      │    │  Terminal        │
│  Renderer       │    │  Graphics        │
│                 │    │  Renderers       │
│  - Sprites      │    │  - Kitty         │
│  - Particles    │    │  - Sixel         │
│  - Animations   │    │  - Braille       │
│                 │    │  - ASCII         │
└─────────────────┘    └──────────────────┘
```

### Core Abstractions

#### IRenderer Interface

```csharp
namespace PigeonPea.Shared.Rendering
{
    public interface IRenderer
    {
        /// <summary>
        /// Initialize the renderer with a render target
        /// </summary>
        void Initialize(IRenderTarget target);

        /// <summary>
        /// Begin a new frame
        /// </summary>
        void BeginFrame();

        /// <summary>
        /// End the current frame and present
        /// </summary>
        void EndFrame();

        /// <summary>
        /// Draw a tile at the specified grid position
        /// </summary>
        void DrawTile(int x, int y, Tile tile);

        /// <summary>
        /// Draw text at the specified position
        /// </summary>
        void DrawText(int x, int y, string text, Color foreground, Color background);

        /// <summary>
        /// Clear the render target
        /// </summary>
        void Clear(Color color);

        /// <summary>
        /// Set the camera/viewport for rendering
        /// </summary>
        void SetViewport(Viewport viewport);

        /// <summary>
        /// Get renderer capabilities
        /// </summary>
        RendererCapabilities Capabilities { get; }
    }
}
```

#### IRenderTarget Interface

```csharp
namespace PigeonPea.Shared.Rendering
{
    public interface IRenderTarget
    {
        /// <summary>
        /// Width in grid cells
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Height in grid cells
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Pixel width (if applicable)
        /// </summary>
        int? PixelWidth { get; }

        /// <summary>
        /// Pixel height (if applicable)
        /// </summary>
        int? PixelHeight { get; }

        /// <summary>
        /// Notify that rendering is complete
        /// </summary>
        void Present();
    }
}
```

#### Renderer Capabilities

```csharp
namespace PigeonPea.Shared.Rendering
{
    [Flags]
    public enum RendererCapabilities
    {
        None = 0,
        TrueColor = 1 << 0,          // 24-bit RGB color
        Sprites = 1 << 1,            // Sprite/texture rendering
        Particles = 1 << 2,          // Particle effects
        Animation = 1 << 3,          // Animated tiles
        PixelGraphics = 1 << 4,      // Pixel-perfect graphics
        CharacterBased = 1 << 5,     // Character/glyph based
        MouseInput = 1 << 6,         // Mouse interaction
    }

    public static class RendererCapabilitiesExtensions
    {
        public static bool Supports(this RendererCapabilities caps, RendererCapabilities feature)
            => (caps & feature) == feature;
    }
}
```

#### Tile Structure

```csharp
namespace PigeonPea.Shared.Rendering
{
    public struct Tile
    {
        // Character representation (always available)
        public char Glyph { get; set; }
        public Color Foreground { get; set; }
        public Color Background { get; set; }

        // Optional sprite representation
        public int? SpriteId { get; set; }
        public int? SpriteFrame { get; set; }

        // Rendering hints
        public TileFlags Flags { get; set; }
    }

    [Flags]
    public enum TileFlags
    {
        None = 0,
        Animated = 1 << 0,
        Particle = 1 << 1,
        Transparent = 1 << 2,
    }
}
```

### Windows Renderer (SkiaSharp)

#### Class Structure

```csharp
namespace PigeonPea.Windows.Rendering
{
    public class SkiaSharpRenderer : IRenderer
    {
        private readonly SpriteAtlasManager _atlasManager;
        private readonly ParticleSystem _particleSystem;
        private readonly AnimationSystem _animationSystem;

        public RendererCapabilities Capabilities =>
            RendererCapabilities.TrueColor |
            RendererCapabilities.Sprites |
            RendererCapabilities.Particles |
            RendererCapabilities.Animation |
            RendererCapabilities.PixelGraphics |
            RendererCapabilities.MouseInput;

        public void DrawTile(int x, int y, Tile tile)
        {
            if (tile.SpriteId.HasValue && _atlasManager.HasSprite(tile.SpriteId.Value))
            {
                DrawSprite(x, y, tile.SpriteId.Value, tile.SpriteFrame ?? 0);
            }
            else
            {
                DrawGlyph(x, y, tile.Glyph, tile.Foreground, tile.Background);
            }
        }

        // Implementation details...
    }
}
```

#### Sprite Atlas Manager

```csharp
public class SpriteAtlasManager
{
    private Dictionary<int, SKImage> _sprites = new();
    private Dictionary<string, SKBitmap> _atlases = new();

    public void LoadAtlas(string path)
    {
        // Load texture atlas from file
        // Parse sprite definitions (JSON sidecar)
        // Extract individual sprites
    }

    public bool HasSprite(int spriteId)
        => _sprites.ContainsKey(spriteId);

    public SKImage GetSprite(int spriteId, int frame = 0)
    {
        // Return sprite image for rendering
    }
}
```

#### Particle System

```csharp
public class ParticleSystem
{
    private List<Particle> _particles = new();
    private ObjectPool<Particle> _particlePool;

    public void Emit(ParticleEmitter emitter)
    {
        // Spawn particles based on emitter configuration
    }

    public void Update(float deltaTime)
    {
        // Update particle positions, lifetimes, colors
    }

    public void Render(SKCanvas canvas)
    {
        // Render all active particles
    }
}
```

### Console Renderer (Terminal Graphics)

#### Multi-Tier Renderer Selection

```csharp
namespace PigeonPea.Console.Rendering
{
    public static class TerminalRendererFactory
    {
        public static IRenderer CreateBestRenderer()
        {
            var capabilities = TerminalCapabilities.Detect();

            if (capabilities.SupportsKittyGraphics)
                return new KittyGraphicsRenderer();

            if (capabilities.SupportsSixel)
                return new SixelRenderer();

            if (capabilities.SupportsUnicodeBraille)
                return new BrailleRenderer();

            return new AsciiRenderer();
        }
    }
}
```

#### Terminal Capability Detection

```csharp
public class TerminalCapabilities
{
    public bool SupportsKittyGraphics { get; set; }
    public bool SupportsSixel { get; set; }
    public bool SupportsUnicodeBraille { get; set; }
    public bool SupportsTrueColor { get; set; }
    public bool Supports256Color { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public static TerminalCapabilities Detect()
    {
        var caps = new TerminalCapabilities();

        // Query terminal capabilities using escape sequences
        // Check TERM environment variable
        // Test graphics protocols with probe sequences

        // Kitty Graphics Protocol detection
        caps.SupportsKittyGraphics = DetectKittyGraphics();

        // Sixel detection
        caps.SupportsSixel = DetectSixel();

        // Color support detection
        caps.SupportsTrueColor = DetectTrueColor();

        // Size detection
        (caps.Width, caps.Height) = GetTerminalSize();

        return caps;
    }

    private static bool DetectKittyGraphics()
    {
        // Send query: ESC_Gi=31,s=1,v=1,a=q,t=d,f=24;AAAA ESC\
        // Wait for response: ESC_Gi=31;OK ESC\
    }

    private static bool DetectSixel()
    {
        // Check DA1 response for Sixel support
        // Or query TERM variable for xterm-*, mlterm
    }
}
```

#### Kitty Graphics Protocol Renderer

```csharp
public class KittyGraphicsRenderer : IRenderer
{
    private byte[] _imageBuffer;
    private int _imageId = 1;

    public RendererCapabilities Capabilities =>
        RendererCapabilities.TrueColor |
        RendererCapabilities.PixelGraphics |
        RendererCapabilities.Sprites;

    public void DrawTile(int x, int y, Tile tile)
    {
        if (tile.SpriteId.HasValue)
        {
            // Transmit sprite using Kitty protocol
            TransmitImage(tile.SpriteId.Value, x, y);
        }
        else
        {
            // Fall back to character rendering with colors
            DrawGlyph(x, y, tile.Glyph, tile.Foreground);
        }
    }

    private void TransmitImage(int spriteId, int x, int y)
    {
        // Format: ESC_Ga=T,f=24,t=d,s=<w>,v=<h>,i=<id>;<base64 data>ESC\
        var imageData = GetSpriteData(spriteId);
        var base64 = Convert.ToBase64String(imageData);
        Console.Write($"\x1b_Ga=T,f=24,t=d,i={_imageId};{base64}\x1b\\");

        // Display: ESC_Ga=p,i=<id>,X=<x>,Y=<y>ESC\
        Console.Write($"\x1b_Ga=p,i={_imageId},X={x},Y={y}\x1b\\");
    }
}
```

#### Sixel Renderer

```csharp
public class SixelRenderer : IRenderer
{
    public RendererCapabilities Capabilities =>
        RendererCapabilities.TrueColor |
        RendererCapabilities.PixelGraphics;

    public void DrawTile(int x, int y, Tile tile)
    {
        if (tile.SpriteId.HasValue)
        {
            // Convert sprite to Sixel format
            var sixelData = ConvertToSixel(GetSpriteData(tile.SpriteId.Value));
            PositionCursor(x, y);
            Console.Write(sixelData);
        }
        else
        {
            DrawGlyph(x, y, tile.Glyph, tile.Foreground);
        }
    }

    private string ConvertToSixel(byte[] imageData)
    {
        // Convert RGB image to Sixel format
        // Format: DCS Pa; Pb; Ph q s...s ST
    }
}
```

#### Unicode Braille Renderer

```csharp
public class BrailleRenderer : IRenderer
{
    // Braille patterns provide 2x4 dot resolution per character
    private const int DotsPerCharX = 2;
    private const int DotsPerCharY = 4;

    public RendererCapabilities Capabilities =>
        RendererCapabilities.TrueColor |
        RendererCapabilities.CharacterBased;

    public void DrawTile(int x, int y, Tile tile)
    {
        // Render tile as Braille pattern
        // Each tile can be 2x4 dots for higher resolution
        var pattern = ConvertToBraille(tile);
        DrawBraillePattern(x, y, pattern, tile.Foreground);
    }

    private char ConvertToBraille(Tile tile)
    {
        // Convert glyph or sprite to Braille dots
        // Unicode Braille: U+2800 to U+28FF
        // Bits map to dots:
        //   0 3
        //   1 4
        //   2 5
        //   6 7
    }
}
```

#### ASCII Fallback Renderer

```csharp
public class AsciiRenderer : IRenderer
{
    public RendererCapabilities Capabilities =>
        RendererCapabilities.CharacterBased;

    public void DrawTile(int x, int y, Tile tile)
    {
        // Pure ASCII rendering - maximum compatibility
        PositionCursor(x, y);

        // Use ANSI color codes if available
        if (SupportsAnsiColors)
        {
            var fg = ColorToAnsi(tile.Foreground);
            var bg = ColorToAnsi(tile.Background);
            Console.Write($"\x1b[{fg};{bg}m{tile.Glyph}\x1b[0m");
        }
        else
        {
            Console.Write(tile.Glyph);
        }
    }
}
```

### Color Gradient System

```csharp
namespace PigeonPea.Shared.Rendering
{
    public static class ColorGradient
    {
        public static Color Lerp(Color a, Color b, float t)
        {
            return new Color(
                (byte)(a.R + (b.R - a.R) * t),
                (byte)(a.G + (b.G - a.G) * t),
                (byte)(a.B + (b.B - a.B) * t)
            );
        }

        public static Color[] CreateGradient(Color start, Color end, int steps)
        {
            var gradient = new Color[steps];
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                gradient[i] = Lerp(start, end, t);
            }
            return gradient;
        }

        /// <summary>
        /// Apply distance-based color fade for fog of war
        /// </summary>
        public static Color ApplyDistanceFade(Color baseColor, float distance, float maxDistance)
        {
            float t = Math.Clamp(distance / maxDistance, 0f, 1f);
            return Lerp(baseColor, Color.Black, t);
        }
    }
}
```

## Implementation Plan

### Phase 1: Abstraction Layer (Week 1-2)

1. Create `PigeonPea.Shared/Rendering/` directory
2. Define `IRenderer`, `IRenderTarget`, `Tile` interfaces/structs
3. Define `RendererCapabilities` enum
4. Create `Viewport` and `Camera` classes
5. Refactor `GameWorld` to use `IRenderer` instead of platform-specific code

### Phase 2: Windows Renderer (Week 3-4)

1. Implement `SkiaSharpRenderer : IRenderer`
2. Create `SpriteAtlasManager`
3. Implement `ParticleSystem`
4. Implement `AnimationSystem`
5. Update `GameCanvas` to use new renderer

### Phase 3: Console Renderers (Week 5-6)

1. Implement `TerminalCapabilities.Detect()`
2. Create `TerminalRendererFactory`
3. Implement `KittyGraphicsRenderer`
4. Implement `SixelRenderer`
5. Implement `BrailleRenderer`
6. Implement `AsciiRenderer`

### Phase 4: Polish & Optimization (Week 7-8)

1. Add color gradient utilities
2. Optimize render batching
3. Add renderer benchmarks
4. Documentation and examples

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void Renderer_DrawTile_CallsCorrectMethod()
{
    var mockTarget = new MockRenderTarget(80, 24);
    var renderer = new AsciiRenderer();
    renderer.Initialize(mockTarget);

    var tile = new Tile { Glyph = '@', Foreground = Color.Yellow };
    renderer.BeginFrame();
    renderer.DrawTile(5, 10, tile);
    renderer.EndFrame();

    Assert.Equal('@', mockTarget.GetChar(5, 10));
    Assert.Equal(Color.Yellow, mockTarget.GetForeground(5, 10));
}
```

### Integration Tests

- Test capability detection on various terminals
- Verify image transmission for Kitty/Sixel
- Benchmark rendering performance
- Visual regression tests (see RFC-003)

## Performance Considerations

### Render Batching

- Batch draw calls to minimize state changes
- Use dirty rectangles to update only changed regions
- Cache rendered tiles for static content

### Memory Management

- Pool particle objects to avoid GC pressure
- Reuse sprite/texture buffers
- Limit animation frame cache size

### Target Frame Rates

- **Windows App**: 60 FPS target
- **Console App**: 30 FPS target (higher latency tolerance)

## Backward Compatibility

The new rendering architecture is a breaking change for the platform-specific projects, but not for the shared game logic. Migration path:

1. Shared game logic continues to work unchanged
2. Windows app: Replace direct SkiaSharp calls with `IRenderer`
3. Console app: Replace direct Terminal.Gui drawing with `IRenderer`

## Alternatives Considered

### Alternative 1: Direct Platform Rendering

Keep platform-specific rendering without abstraction.

**Pros**: Simpler, less indirection
**Cons**: Code duplication, harder to test, harder to add new platforms

**Decision**: Rejected - abstraction provides too many benefits

### Alternative 2: Command Pattern Rendering

Use render command buffers instead of direct `IRenderer` calls.

**Pros**: Better for deferred rendering, easier to record/replay
**Cons**: More complex, potential performance overhead

**Decision**: Deferred - can add later if needed

## Open Questions

1. **Asset Loading**: Should asset loading be part of the renderer or separate?
   - **Proposal**: Separate asset manager, renderer only handles rendering

2. **Multi-threaded Rendering**: Should we support multi-threaded rendering?
   - **Proposal**: Not in initial version, add if needed

3. **Render State Management**: How to handle render state (blending, z-order, etc.)?
   - **Proposal**: Add `RenderState` parameter to draw calls as needed

## References

- [Kitty Graphics Protocol Documentation](https://sw.kovidgoyal.net/kitty/graphics-protocol/)
- [Sixel Format Specification](https://vt100.net/docs/vt3xx-gp/chapter14.html)
- [SkiaSharp Documentation](https://learn.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/)
- [Terminal.Gui Rendering](https://gui-cs.github.io/Terminal.Gui/)
- [Unicode Braille Patterns](https://en.wikipedia.org/wiki/Braille_Patterns)
