---
doc_id: 'PLAN-2025-00011'
title: 'GitHub Issues Breakdown'
doc_type: 'plan'
status: 'active'
canonical: true
created: '2025-11-08'
tags: ['github-issues', 'implementation', 'rfcs', 'task-breakdown']
summary: 'Comprehensive breakdown of all RFCs into 43 actionable GitHub issues for automated coding agents'
related: ['RFC-2025-00001', 'RFC-2025-00002', 'RFC-2025-00003', 'RFC-2025-00005', 'RFC-2025-00006']
---

# GitHub Issues Breakdown

This document breaks down the RFCs into actionable GitHub issues suitable for automated coding agents.

## Issue Template Format

Each issue follows this structure:

- **Title**: Clear, action-oriented
- **Labels**: Type and scope
- **RFC Reference**: Which RFC this implements
- **Dependencies**: Other issues that must be completed first
- **Description**: What needs to be done
- **Acceptance Criteria**: Definition of done
- **Files to Create/Modify**: Specific file paths
- **Code Examples**: Snippets showing expected implementation

---

# RFC-001: Rendering Architecture Issues

## Phase 1: Abstraction Layer (Week 1-2)

### Issue #1: Create Core Rendering Interfaces

**Title**: Create core rendering interfaces (IRenderer, IRenderTarget, Tile)

**Labels**: `enhancement`, `rendering`, `rfc-001`, `phase-1`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: None

**Description**:
Create the core rendering abstraction interfaces and types in the shared library to enable platform-agnostic rendering.

**Acceptance Criteria**:

- [ ] `IRenderer` interface created with all methods (BeginFrame, EndFrame, DrawTile, Clear, SetViewport)
- [ ] `IRenderTarget` interface created with properties (Width, Height, PixelWidth, PixelHeight, Present)
- [ ] `Tile` struct created with character and sprite representations
- [ ] `TileFlags` enum created
- [ ] `RendererCapabilities` enum created with extension methods
- [ ] All types have XML documentation comments
- [ ] Unit tests for `Tile` and `RendererCapabilities` extensions

**Files to Create**:

- `dotnet/shared-app/Rendering/IRenderer.cs`
- `dotnet/shared-app/Rendering/IRenderTarget.cs`
- `dotnet/shared-app/Rendering/Tile.cs`
- `dotnet/shared-app/Rendering/RendererCapabilities.cs`
- `dotnet/shared-app.Tests/Rendering/TileTests.cs`
- `dotnet/shared-app.Tests/Rendering/RendererCapabilitiesTests.cs`

**Code Example**:

```csharp
namespace PigeonPea.Shared.Rendering
{
    /// <summary>
    /// Platform-agnostic renderer interface
    /// </summary>
    public interface IRenderer
    {
        void Initialize(IRenderTarget target);
        void BeginFrame();
        void EndFrame();
        void DrawTile(int x, int y, Tile tile);
        void DrawText(int x, int y, string text, Color foreground, Color background);
        void Clear(Color color);
        void SetViewport(Viewport viewport);
        RendererCapabilities Capabilities { get; }
    }
}
```

---

### Issue #2: Create Viewport and Camera Classes

**Title**: Create Viewport and Camera classes for rendering

**Labels**: `enhancement`, `rendering`, `rfc-001`, `phase-1`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #1

**Description**:
Create viewport and camera classes to handle view positioning and culling for both Windows and Console renderers.

**Acceptance Criteria**:

- [ ] `Viewport` class created with position, size, and bounds
- [ ] `Camera` class created with position, follow target, and bounds clamping
- [ ] Viewport can calculate visible region based on camera position
- [ ] Camera can smoothly follow entities
- [ ] Unit tests for viewport and camera calculations

**Files to Create**:

- `dotnet/shared-app/Rendering/Viewport.cs`
- `dotnet/shared-app/Rendering/Camera.cs`
- `dotnet/shared-app.Tests/Rendering/ViewportTests.cs`
- `dotnet/shared-app.Tests/Rendering/CameraTests.cs`

**Code Example**:

```csharp
public class Viewport
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public Rectangle Bounds => new Rectangle(X, Y, Width, Height);

    public bool Contains(int x, int y)
        => x >= X && x < X + Width && y >= Y && y < Y + Height;
}

public class Camera
{
    public Point Position { get; set; }
    public Entity? FollowTarget { get; set; }

    public void Update(Rectangle mapBounds)
    {
        if (FollowTarget != null)
        {
            // Center on target with bounds clamping
        }
    }
}
```

---

### Issue #3: Create ColorGradient Utility

**Title**: Create ColorGradient utility for fog of war effects

**Labels**: `enhancement`, `rendering`, `rfc-001`, `phase-1`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: None

**Description**:
Create a color gradient utility class for smooth color transitions, distance-based fog of war, and visual effects.

**Acceptance Criteria**:

- [ ] `ColorGradient` class with Lerp method
- [ ] `CreateGradient` method for generating gradient arrays
- [ ] `ApplyDistanceFade` method for fog of war
- [ ] Unit tests for all gradient operations
- [ ] Tests verify correct color interpolation

**Files to Create**:

- `dotnet/shared-app/Rendering/ColorGradient.cs`
- `dotnet/shared-app.Tests/Rendering/ColorGradientTests.cs`

**Code Example**:

```csharp
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

    public static Color ApplyDistanceFade(Color baseColor, float distance, float maxDistance)
    {
        float t = Math.Clamp(distance / maxDistance, 0f, 1f);
        return Lerp(baseColor, Color.Black, t);
    }
}
```

---

### Issue #4: Refactor GameWorld to Use IRenderer

**Title**: Refactor GameWorld to use IRenderer abstraction

**Labels**: `refactor`, `rendering`, `rfc-001`, `phase-1`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #1, Issue #2

**Description**:
Update GameWorld to accept and use IRenderer instead of platform-specific rendering code. This decouples the game logic from rendering implementation.

**Acceptance Criteria**:

- [ ] GameWorld constructor accepts `IRenderer` parameter
- [ ] Remove platform-specific rendering code from GameWorld
- [ ] Add `Render()` method that uses `IRenderer`
- [ ] Rendering queries ECS for (Position, Renderable) components
- [ ] Viewport culling implemented
- [ ] Unit tests with mock renderer

**Files to Modify**:

- `dotnet/shared-app/GameWorld.cs`

**Files to Create**:

- `dotnet/shared-app.Tests/Mocks/MockRenderer.cs`
- `dotnet/shared-app.Tests/GameWorldRenderingTests.cs`

**Code Example**:

```csharp
public class GameWorld
{
    private readonly IRenderer _renderer;

    public GameWorld(IRenderer renderer)
    {
        _renderer = renderer;
    }

    public void Render(Viewport viewport)
    {
        _renderer.BeginFrame();
        _renderer.Clear(Color.Black);

        // Query ECS for renderable entities
        var query = World.Query<Position, Renderable>();
        foreach (var entity in query)
        {
            ref var pos = ref entity.t1;
            ref var renderable = ref entity.t2;

            if (viewport.Contains(pos.Point.X, pos.Point.Y))
            {
                _renderer.DrawTile(pos.Point.X, pos.Point.Y, new Tile
                {
                    Glyph = renderable.Glyph,
                    Foreground = renderable.ForegroundColor,
                    Background = renderable.BackgroundColor
                });
            }
        }

        _renderer.EndFrame();
    }
}
```

---

## Phase 2: Windows Renderer (Week 3-4)

### Issue #5: Implement SkiaSharpRenderer

**Title**: Implement SkiaSharpRenderer with basic tile rendering

**Labels**: `enhancement`, `rendering`, `windows`, `rfc-001`, `phase-2`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #1, Issue #2

**Description**:
Create the SkiaSharp-based renderer for the Windows app with basic character/tile rendering support.

**Acceptance Criteria**:

- [ ] `SkiaSharpRenderer` implements `IRenderer`
- [ ] Character glyph rendering with SkiaSharp
- [ ] Color support for foreground/background
- [ ] Tile size configuration
- [ ] Frame timing tracking
- [ ] Capabilities reports TrueColor | CharacterBased
- [ ] Integration tests with actual SKCanvas

**Files to Create**:

- `dotnet/windows-app/Rendering/SkiaSharpRenderer.cs`
- `dotnet/windows-app/Rendering/SkiaRenderTarget.cs`
- `dotnet/windows-app.Tests/Rendering/SkiaSharpRendererTests.cs`

**Code Example**:

```csharp
public class SkiaSharpRenderer : IRenderer
{
    private SKCanvas _canvas;
    private IRenderTarget _target;
    private int _tileSize = 16;

    public RendererCapabilities Capabilities =>
        RendererCapabilities.TrueColor |
        RendererCapabilities.CharacterBased;

    public void Initialize(IRenderTarget target)
    {
        _target = target;
    }

    public void DrawTile(int x, int y, Tile tile)
    {
        var paint = new SKPaint
        {
            Color = ToSKColor(tile.Background),
            Style = SKPaintStyle.Fill
        };

        _canvas.DrawRect(x * _tileSize, y * _tileSize, _tileSize, _tileSize, paint);

        paint.Color = ToSKColor(tile.Foreground);
        _canvas.DrawText(tile.Glyph.ToString(),
            x * _tileSize, (y + 1) * _tileSize, paint);
    }
}
```

---

### Issue #6: Create SpriteAtlasManager

**Title**: Create SpriteAtlasManager for texture atlas loading

**Labels**: `enhancement`, `rendering`, `windows`, `rfc-001`, `phase-2`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #5

**Description**:
Create a sprite atlas manager that can load texture atlases and extract individual sprites for rendering.

**Acceptance Criteria**:

- [ ] Load PNG texture atlases
- [ ] Parse JSON sprite definitions (positions, sizes)
- [ ] Extract sprites by ID
- [ ] Cache loaded sprites
- [ ] Support multiple atlases
- [ ] Handle sprite not found gracefully
- [ ] Unit tests with test atlas

**Files to Create**:

- `dotnet/windows-app/Rendering/SpriteAtlasManager.cs`
- `dotnet/windows-app/Rendering/SpriteDefinition.cs`
- `dotnet/windows-app.Tests/Rendering/SpriteAtlasManagerTests.cs`
- `dotnet/windows-app.Tests/TestData/test-atlas.png`
- `dotnet/windows-app.Tests/TestData/test-atlas.json`

**Code Example**:

```csharp
public class SpriteAtlasManager
{
    private Dictionary<int, SKImage> _sprites = new();
    private Dictionary<string, SKBitmap> _atlases = new();

    public void LoadAtlas(string imagePath, string definitionPath)
    {
        var bitmap = SKBitmap.Decode(imagePath);
        var json = File.ReadAllText(definitionPath);
        var definitions = JsonSerializer.Deserialize<SpriteDefinition[]>(json);

        foreach (var def in definitions)
        {
            var sprite = ExtractSprite(bitmap, def.X, def.Y, def.Width, def.Height);
            _sprites[def.Id] = SKImage.FromBitmap(sprite);
        }
    }

    public bool HasSprite(int spriteId) => _sprites.ContainsKey(spriteId);

    public SKImage GetSprite(int spriteId) => _sprites[spriteId];
}
```

---

### Issue #7: Add Sprite Rendering to SkiaSharpRenderer

**Title**: Add sprite rendering support to SkiaSharpRenderer

**Labels**: `enhancement`, `rendering`, `windows`, `rfc-001`, `phase-2`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #5, Issue #6

**Description**:
Extend SkiaSharpRenderer to render sprites from atlases when Tile has SpriteId set.

**Acceptance Criteria**:

- [ ] DrawTile checks for SpriteId and renders sprite if available
- [ ] Falls back to glyph if sprite not found
- [ ] Sprites scaled to tile size
- [ ] Capabilities updated to include Sprites flag
- [ ] Integration tests with test sprites

**Files to Modify**:

- `dotnet/windows-app/Rendering/SkiaSharpRenderer.cs`

**Code Example**:

```csharp
public void DrawTile(int x, int y, Tile tile)
{
    if (tile.SpriteId.HasValue && _atlasManager.HasSprite(tile.SpriteId.Value))
    {
        DrawSprite(x, y, tile.SpriteId.Value);
    }
    else
    {
        DrawGlyph(x, y, tile.Glyph, tile.Foreground, tile.Background);
    }
}

private void DrawSprite(int x, int y, int spriteId)
{
    var sprite = _atlasManager.GetSprite(spriteId);
    var rect = new SKRect(x * _tileSize, y * _tileSize,
        (x + 1) * _tileSize, (y + 1) * _tileSize);
    _canvas.DrawImage(sprite, rect);
}
```

---

### Issue #8: Implement ParticleSystem

**Title**: Implement particle system for visual effects

**Labels**: `enhancement`, `rendering`, `windows`, `rfc-001`, `phase-2`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #5

**Description**:
Create a particle system for rendering effects like combat hits, spell casting, and environmental effects.

**Acceptance Criteria**:

- [ ] Particle class with position, velocity, lifetime, color
- [ ] ParticleEmitter configuration (rate, direction, spread, etc.)
- [ ] Object pooling for particles
- [ ] Update method advances particle simulation
- [ ] Render method draws all active particles
- [ ] Emit method spawns particles from emitter
- [ ] Unit tests for particle lifecycle

**Files to Create**:

- `dotnet/windows-app/Rendering/Particle.cs`
- `dotnet/windows-app/Rendering/ParticleEmitter.cs`
- `dotnet/windows-app/Rendering/ParticleSystem.cs`
- `dotnet/windows-app.Tests/Rendering/ParticleSystemTests.cs`

**Code Example**:

```csharp
public class ParticleSystem
{
    private List<Particle> _particles = new();
    private ObjectPool<Particle> _pool = new();

    public void Emit(ParticleEmitter emitter)
    {
        for (int i = 0; i < emitter.Rate; i++)
        {
            var particle = _pool.Get();
            particle.Position = emitter.Position;
            particle.Velocity = emitter.GenerateVelocity();
            particle.Lifetime = emitter.Lifetime;
            particle.Color = emitter.Color;
            _particles.Add(particle);
        }
    }

    public void Update(float deltaTime)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Update(deltaTime);
            if (p.IsDead)
            {
                _particles.RemoveAt(i);
                _pool.Return(p);
            }
        }
    }

    public void Render(SKCanvas canvas)
    {
        foreach (var p in _particles)
        {
            p.Render(canvas);
        }
    }
}
```

---

### Issue #9: Implement AnimationSystem

**Title**: Implement animation system for animated tiles

**Labels**: `enhancement`, `rendering`, `windows`, `rfc-001`, `phase-2`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #6

**Description**:
Create an animation system to support animated tiles (water, lava, torches, etc.) with frame-based animation.

**Acceptance Criteria**:

- [ ] Animation class with frames and timing
- [ ] AnimationSystem manages all active animations
- [ ] Update method advances animation frames
- [ ] GetCurrentFrame returns sprite ID for current frame
- [ ] Support for looping and one-shot animations
- [ ] Frame duration configuration
- [ ] Unit tests for animation playback

**Files to Create**:

- `dotnet/windows-app/Rendering/Animation.cs`
- `dotnet/windows-app/Rendering/AnimationSystem.cs`
- `dotnet/windows-app.Tests/Rendering/AnimationSystemTests.cs`

**Code Example**:

```csharp
public class Animation
{
    public int[] Frames { get; set; }
    public float FrameDuration { get; set; }
    public bool Loop { get; set; } = true;

    private float _elapsed;
    private int _currentFrame;

    public void Update(float deltaTime)
    {
        _elapsed += deltaTime;
        if (_elapsed >= FrameDuration)
        {
            _elapsed = 0;
            _currentFrame++;
            if (_currentFrame >= Frames.Length)
            {
                _currentFrame = Loop ? 0 : Frames.Length - 1;
            }
        }
    }

    public int GetCurrentFrame() => Frames[_currentFrame];
}
```

---

## Phase 3: Console Renderers (Week 5-6)

### Issue #10: Create TerminalCapabilities Detection

**Title**: Implement terminal capability detection

**Labels**: `enhancement`, `rendering`, `console`, `rfc-001`, `phase-3`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #1

**Description**:
Create a terminal capability detection system that probes for Kitty Graphics, Sixel, Unicode, and color support.

**Acceptance Criteria**:

- [ ] Detect Kitty Graphics Protocol support
- [ ] Detect Sixel graphics support
- [ ] Detect Unicode Braille support
- [ ] Detect true color (24-bit) support
- [ ] Detect 256 color support
- [ ] Get terminal dimensions
- [ ] Parse TERM environment variable
- [ ] Unit tests with mocked terminal responses

**Files to Create**:

- `dotnet/console-app/Rendering/TerminalCapabilities.cs`
- `dotnet/console-app.Tests/Rendering/TerminalCapabilitiesTests.cs`

**Code Example**:

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

        caps.SupportsKittyGraphics = DetectKittyGraphics();
        caps.SupportsSixel = DetectSixel();
        caps.SupportsUnicodeBraille = true; // Most terminals
        caps.SupportsTrueColor = DetectTrueColor();
        (caps.Width, caps.Height) = GetTerminalSize();

        return caps;
    }
}
```

---

### Issue #11: Implement AsciiRenderer

**Title**: Implement ASCII fallback renderer

**Labels**: `enhancement`, `rendering`, `console`, `rfc-001`, `phase-3`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #1

**Description**:
Create the ASCII fallback renderer for maximum terminal compatibility.

**Acceptance Criteria**:

- [ ] Implements IRenderer
- [ ] Renders character glyphs
- [ ] ANSI color codes for foreground/background
- [ ] Cursor positioning
- [ ] Screen clearing
- [ ] Capabilities reports CharacterBased only
- [ ] Works in basic terminals
- [ ] Unit tests with output capture

**Files to Create**:

- `dotnet/console-app/Rendering/AsciiRenderer.cs`
- `dotnet/console-app.Tests/Rendering/AsciiRendererTests.cs`

**Code Example**:

```csharp
public class AsciiRenderer : IRenderer
{
    public RendererCapabilities Capabilities =>
        RendererCapabilities.CharacterBased;

    public void DrawTile(int x, int y, Tile tile)
    {
        PositionCursor(x, y);

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

    private void PositionCursor(int x, int y)
    {
        Console.SetCursorPosition(x, y);
    }
}
```

---

### Issue #12: Implement BrailleRenderer

**Title**: Implement Unicode Braille renderer for higher resolution

**Labels**: `enhancement`, `rendering`, `console`, `rfc-001`, `phase-3`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #1, Issue #10

**Description**:
Create a renderer that uses Unicode Braille patterns for 2x4 dot resolution per character cell.

**Acceptance Criteria**:

- [ ] Implements IRenderer
- [ ] Converts tiles to Braille patterns
- [ ] 2x4 dot resolution per character
- [ ] Color support via ANSI codes
- [ ] Helper methods for dot manipulation
- [ ] Capabilities reports TrueColor | CharacterBased
- [ ] Unit tests for pattern conversion

**Files to Create**:

- `dotnet/console-app/Rendering/BrailleRenderer.cs`
- `dotnet/console-app/Rendering/BraillePattern.cs`
- `dotnet/console-app.Tests/Rendering/BrailleRendererTests.cs`

**Code Example**:

```csharp
public class BrailleRenderer : IRenderer
{
    private const int DotsPerCharX = 2;
    private const int DotsPerCharY = 4;

    public RendererCapabilities Capabilities =>
        RendererCapabilities.TrueColor |
        RendererCapabilities.CharacterBased;

    public void DrawTile(int x, int y, Tile tile)
    {
        var pattern = ConvertToBraille(tile);
        PositionCursor(x, y);
        Console.Write($"\x1b[38;2;{tile.Foreground.R};{tile.Foreground.G};{tile.Foreground.B}m");
        Console.Write(pattern);
        Console.Write("\x1b[0m");
    }

    private char ConvertToBraille(Tile tile)
    {
        // Unicode Braille: U+2800 to U+28FF
        // Map glyph to dot pattern
        return (char)(0x2800 + CalculateDots(tile.Glyph));
    }
}
```

---

### Issue #13: Implement KittyGraphicsRenderer

**Title**: Implement Kitty Graphics Protocol renderer

**Labels**: `enhancement`, `rendering`, `console`, `rfc-001`, `phase-3`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #1, Issue #10

**Description**:
Create a renderer using the Kitty Graphics Protocol for pixel-perfect graphics in Kitty terminal.

**Acceptance Criteria**:

- [ ] Implements IRenderer
- [ ] Transmit images using Kitty protocol escape sequences
- [ ] Display images at specific grid positions
- [ ] Image caching to avoid retransmission
- [ ] Base64 encoding for image data
- [ ] Capabilities reports TrueColor | PixelGraphics | Sprites
- [ ] Integration tests with Kitty terminal

**Files to Create**:

- `dotnet/console-app/Rendering/KittyGraphicsRenderer.cs`
- `dotnet/console-app.Tests/Rendering/KittyGraphicsRendererTests.cs`

**Code Example**:

```csharp
public class KittyGraphicsRenderer : IRenderer
{
    private int _imageId = 1;
    private Dictionary<int, int> _spriteToImageId = new();

    public RendererCapabilities Capabilities =>
        RendererCapabilities.TrueColor |
        RendererCapabilities.PixelGraphics |
        RendererCapabilities.Sprites;

    public void DrawTile(int x, int y, Tile tile)
    {
        if (tile.SpriteId.HasValue)
        {
            TransmitAndDisplayImage(tile.SpriteId.Value, x, y);
        }
        else
        {
            DrawGlyph(x, y, tile.Glyph, tile.Foreground);
        }
    }

    private void TransmitAndDisplayImage(int spriteId, int x, int y)
    {
        if (!_spriteToImageId.ContainsKey(spriteId))
        {
            var imageData = GetSpriteData(spriteId);
            var base64 = Convert.ToBase64String(imageData);
            Console.Write($"\x1b_Ga=T,f=24,t=d,i={_imageId};{base64}\x1b\\");
            _spriteToImageId[spriteId] = _imageId++;
        }

        var imgId = _spriteToImageId[spriteId];
        Console.Write($"\x1b_Ga=p,i={imgId},X={x},Y={y}\x1b\\");
    }
}
```

---

### Issue #14: Implement SixelRenderer

**Title**: Implement Sixel graphics renderer

**Labels**: `enhancement`, `rendering`, `console`, `rfc-001`, `phase-3`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #1, Issue #10

**Description**:
Create a renderer using Sixel graphics format for terminals with Sixel support (xterm, mlterm, WezTerm).

**Acceptance Criteria**:

- [ ] Implements IRenderer
- [ ] Convert images to Sixel format
- [ ] Output Sixel sequences
- [ ] Color palette optimization
- [ ] Capabilities reports TrueColor | PixelGraphics
- [ ] Integration tests with sixel output

**Files to Create**:

- `dotnet/console-app/Rendering/SixelRenderer.cs`
- `dotnet/console-app/Rendering/SixelEncoder.cs`
- `dotnet/console-app.Tests/Rendering/SixelRendererTests.cs`

**Code Example**:

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
            var imageData = GetSpriteData(tile.SpriteId.Value);
            var sixelData = ConvertToSixel(imageData);
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
        // DCS Pa; Pb; Ph q s...s ST
        // Convert RGB to Sixel format
        var encoder = new SixelEncoder();
        return encoder.Encode(imageData);
    }
}
```

---

### Issue #15: Create TerminalRendererFactory

**Title**: Create factory for selecting best terminal renderer

**Labels**: `enhancement`, `rendering`, `console`, `rfc-001`, `phase-3`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #10, Issue #11, Issue #12, Issue #13, Issue #14

**Description**:
Create a factory that detects terminal capabilities and selects the best available renderer.

**Acceptance Criteria**:

- [ ] Detects terminal capabilities
- [ ] Selects Kitty renderer if supported
- [ ] Falls back to Sixel if Kitty not available
- [ ] Falls back to Braille if neither available
- [ ] Falls back to ASCII as last resort
- [ ] Allows manual override via parameter
- [ ] Unit tests for selection logic

**Files to Create**:

- `dotnet/console-app/Rendering/TerminalRendererFactory.cs`
- `dotnet/console-app.Tests/Rendering/TerminalRendererFactoryTests.cs`

**Code Example**:

```csharp
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

    public static IRenderer CreateRenderer(RendererType type)
    {
        return type switch
        {
            RendererType.Kitty => new KittyGraphicsRenderer(),
            RendererType.Sixel => new SixelRenderer(),
            RendererType.Braille => new BrailleRenderer(),
            RendererType.Ascii => new AsciiRenderer(),
            _ => CreateBestRenderer()
        };
    }
}
```

---

## Phase 4: Integration (Week 7-8)

### Issue #16: Update Windows App to Use New Renderer

**Title**: Update Windows app to use SkiaSharpRenderer

**Labels**: `refactor`, `windows`, `rfc-001`, `phase-4`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #5, Issue #6, Issue #7, Issue #8, Issue #9

**Description**:
Refactor the Windows app to use the new SkiaSharpRenderer instead of direct rendering in GameCanvas.

**Acceptance Criteria**:

- [ ] GameCanvas uses SkiaSharpRenderer
- [ ] Remove old direct rendering code
- [ ] Renderer initialized with proper render target
- [ ] Sprite atlases loaded at startup
- [ ] Particle system integrated
- [ ] Animation system integrated
- [ ] App runs and renders correctly
- [ ] No regression in visual quality

**Files to Modify**:

- `dotnet/windows-app/GameCanvas.cs`
- `dotnet/windows-app/MainWindow.axaml.cs`
- `dotnet/windows-app/App.axaml.cs`

---

### Issue #17: Update Console App to Use New Renderer

**Title**: Update Console app to use terminal renderer factory

**Labels**: `refactor`, `console`, `rfc-001`, `phase-4`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #15, Issue #4

**Description**:
Refactor the Console app to use the terminal renderer factory and support CLI argument for renderer selection.

**Acceptance Criteria**:

- [ ] Use TerminalRendererFactory to create renderer
- [ ] Support --renderer CLI argument
- [ ] Remove old Terminal.Gui rendering code
- [ ] Renderer initialized properly
- [ ] App runs in different terminals
- [ ] Automatic fallback works
- [ ] Manual override works

**Files to Modify**:

- `dotnet/console-app/Program.cs`
- `dotnet/console-app/GameApplication.cs`
- `dotnet/console-app/GameView.cs`

**Code Example**:

```csharp
// In Program.cs
var rendererType = ParseRenderer(rendererArg);
var renderer = rendererType == RendererType.Auto
    ? TerminalRendererFactory.CreateBestRenderer()
    : TerminalRendererFactory.CreateRenderer(rendererType);

var world = new GameWorld(renderer);
var app = new GameApplication(world);
app.Run();
```

---

### Issue #18: Add Performance Benchmarks

**Title**: Add rendering performance benchmarks

**Labels**: `testing`, `performance`, `rfc-001`, `phase-4`

**RFC Reference**: [RFC-001: Rendering Architecture](rfcs/001-rendering-architecture.md)

**Dependencies**: Issue #16, Issue #17

**Description**:
Create performance benchmarks to measure rendering performance and ensure targets are met (60 FPS Windows, 30 FPS Console).

**Acceptance Criteria**:

- [ ] Benchmark for Windows renderer frame time
- [ ] Benchmark for Console renderer frame time
- [ ] Benchmark for large map rendering
- [ ] Benchmark for particle system
- [ ] Results logged and tracked
- [ ] CI integration for regression detection

**Files to Create**:

- `dotnet/benchmarks/RenderingBenchmarks.cs`
- `.github/workflows/benchmarks.yml`

**Code Example**:

```csharp
[MemoryDiagnoser]
public class RenderingBenchmarks
{
    private GameWorld _world;
    private SkiaSharpRenderer _renderer;

    [Benchmark]
    public void RenderFullScreen()
    {
        _renderer.BeginFrame();
        _world.Render(new Viewport { Width = 80, Height = 24 });
        _renderer.EndFrame();
    }

    [Benchmark]
    public void RenderWithParticles()
    {
        // 100 active particles
        for (int i = 0; i < 100; i++)
        {
            _particleSystem.Emit(_emitter);
        }

        _renderer.BeginFrame();
        _world.Render(new Viewport { Width = 80, Height = 24 });
        _renderer.EndFrame();
    }
}
```

---

# RFC-002: UI Framework Integration Issues

## Phase 1: Reactive Infrastructure (Week 1)

### Issue #19: Add Reactive NuGet Packages

**Title**: Add reactive programming NuGet packages to shared-app

**Labels**: `enhancement`, `dependencies`, `rfc-002`, `phase-1`

**RFC Reference**: [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)

**Dependencies**: None

**Description**:
Add all required NuGet packages for reactive programming: System.Reactive, ReactiveUI, MessagePipe, ObservableCollections, and Microsoft.Extensions.DependencyInjection.

**Acceptance Criteria**:

- [ ] System.Reactive 6.0.1 added to shared-app
- [ ] ReactiveUI 20.1.1 added to shared-app
- [ ] MessagePipe 1.8.0 added to shared-app
- [ ] ObservableCollections 3.0.1 added to shared-app
- [ ] Microsoft.Extensions.DependencyInjection 9.0.0 added to shared-app
- [ ] All packages restore successfully
- [ ] No version conflicts
- [ ] dotnet build succeeds

**Files to Modify**:

- `dotnet/shared-app/PigeonPea.Shared.csproj`

**Code Example**:

```xml
<ItemGroup>
  <PackageReference Include="System.Reactive" Version="6.0.1" />
  <PackageReference Include="ReactiveUI" Version="20.1.1" />
  <PackageReference Include="MessagePipe" Version="1.8.0" />
  <PackageReference Include="ObservableCollections" Version="3.0.1" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
</ItemGroup>
```

---

### Issue #20: Define Game Event Structures

**Title**: Define game event structures for MessagePipe

**Labels**: `enhancement`, `events`, `rfc-002`, `phase-1`

**RFC Reference**: [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)

**Dependencies**: Issue #19

**Description**:
Create event structure definitions for all game events that will be published via MessagePipe.

**Acceptance Criteria**:

- [ ] Combat events defined (PlayerDamagedEvent, EnemyDefeatedEvent)
- [ ] Inventory events defined (ItemPickedUpEvent, ItemUsedEvent, ItemDroppedEvent)
- [ ] Level events defined (PlayerLevelUpEvent)
- [ ] Map events defined (DoorOpenedEvent, StairsDescendedEvent)
- [ ] All events are readonly structs
- [ ] All properties use init accessors
- [ ] XML documentation on all events

**Files to Create**:

- `dotnet/shared-app/Events/CombatEvents.cs`
- `dotnet/shared-app/Events/InventoryEvents.cs`
- `dotnet/shared-app/Events/LevelEvents.cs`
- `dotnet/shared-app/Events/MapEvents.cs`

**Code Example**:

```csharp
namespace PigeonPea.Shared.Events
{
    /// <summary>
    /// Published when the player takes damage
    /// </summary>
    public readonly struct PlayerDamagedEvent
    {
        public int Damage { get; init; }
        public int RemainingHealth { get; init; }
        public string Source { get; init; }
    }

    /// <summary>
    /// Published when an enemy is defeated
    /// </summary>
    public readonly struct EnemyDefeatedEvent
    {
        public string EnemyName { get; init; }
        public int ExperienceGained { get; init; }
    }
}
```

---

### Issue #21: Set Up MessagePipe Dependency Injection

**Title**: Configure MessagePipe in dependency injection container

**Labels**: `enhancement`, `di`, `rfc-002`, `phase-1`

**RFC Reference**: [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)

**Dependencies**: Issue #19, Issue #20

**Description**:
Set up MessagePipe in the DI container for both Console and Windows apps.

**Acceptance Criteria**:

- [ ] Console app Program.cs sets up DI with MessagePipe
- [ ] Windows app App.axaml.cs sets up DI with MessagePipe
- [ ] IPublisher<T> and ISubscriber<T> can be injected
- [ ] Services can publish and subscribe to events
- [ ] Example event publish/subscribe works

**Files to Modify**:

- `dotnet/console-app/Program.cs`
- `dotnet/windows-app/App.axaml.cs`

**Files to Create**:

- `dotnet/shared-app/ServiceCollectionExtensions.cs`

**Code Example**:

```csharp
// ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPigeonPeaServices(this IServiceCollection services)
    {
        // Add MessagePipe
        services.AddMessagePipe();

        // Add game services
        services.AddSingleton<GameWorld>();

        // Add view models
        services.AddSingleton<GameViewModel>();
        services.AddSingleton<PlayerViewModel>();
        services.AddSingleton<InventoryViewModel>();
        services.AddSingleton<MessageLogViewModel>();

        return services;
    }
}

// Console Program.cs
var services = new ServiceCollection();
services.AddPigeonPeaServices();
var provider = services.BuildServiceProvider();
```

---

### Issue #22: Create PlayerViewModel

**Title**: Create PlayerViewModel with reactive properties

**Labels**: `enhancement`, `viewmodel`, `rfc-002`, `phase-1`

**RFC Reference**: [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)

**Dependencies**: Issue #19, Issue #21

**Description**:
Create PlayerViewModel that exposes player state with reactive property change notifications.

**Acceptance Criteria**:

- [ ] Inherits from ReactiveObject
- [ ] Properties for Health, MaxHealth, Level, Experience, Name, Position
- [ ] RaiseAndSetIfChanged used for all properties
- [ ] Computed properties (HealthDisplay, HealthPercentage, LevelDisplay)
- [ ] Update() method syncs from ECS
- [ ] Unit tests for property notifications

**Files to Create**:

- `dotnet/shared-app/ViewModels/PlayerViewModel.cs`
- `dotnet/shared-app.Tests/ViewModels/PlayerViewModelTests.cs`

**Code Example**:

```csharp
public class PlayerViewModel : ReactiveObject
{
    private int _health;
    private int _maxHealth;
    private int _level;

    public int Health
    {
        get => _health;
        set => this.RaiseAndSetIfChanged(ref _health, value);
    }

    public int MaxHealth
    {
        get => _maxHealth;
        set => this.RaiseAndSetIfChanged(ref _maxHealth, value);
    }

    public string HealthDisplay => $"{Health}/{MaxHealth}";
    public double HealthPercentage => MaxHealth > 0 ? (double)Health / MaxHealth : 0;

    internal void Update(GameWorld world)
    {
        var player = world.GetPlayerEntity();
        if (player == null) return;

        var health = world.GetComponent<Health>(player.Value);
        Health = health.Current;
        MaxHealth = health.Maximum;
    }
}
```

---

### Issue #23: Create GameViewModel

**Title**: Create central GameViewModel

**Labels**: `enhancement`, `viewmodel`, `rfc-002`, `phase-1`

**RFC Reference**: [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)

**Dependencies**: Issue #22

**Description**:
Create the central GameViewModel that owns all other view models and orchestrates updates.

**Acceptance Criteria**:

- [ ] Inherits from ReactiveObject
- [ ] Properties for Player, Inventory, MessageLog, Map view models
- [ ] Constructor accepts GameWorld and IServiceProvider
- [ ] Update loop using Observable.Interval
- [ ] Disposal properly cleans up subscriptions
- [ ] Unit tests for initialization

**Files to Create**:

- `dotnet/shared-app/ViewModels/GameViewModel.cs`
- `dotnet/shared-app.Tests/ViewModels/GameViewModelTests.cs`

**Code Example**:

```csharp
public class GameViewModel : ReactiveObject, IDisposable
{
    private readonly GameWorld _world;
    private readonly CompositeDisposable _disposables = new();

    public GameViewModel(GameWorld world, IServiceProvider services)
    {
        _world = world;

        Player = services.GetRequiredService<PlayerViewModel>();
        Inventory = services.GetRequiredService<InventoryViewModel>();
        MessageLog = services.GetRequiredService<MessageLogViewModel>();
        Map = services.GetRequiredService<MapViewModel>();

        InitializeUpdateLoop();
    }

    public PlayerViewModel Player { get; }
    public InventoryViewModel Inventory { get; }
    public MessageLogViewModel MessageLog { get; }
    public MapViewModel Map { get; }

    private void InitializeUpdateLoop()
    {
        Observable.Interval(TimeSpan.FromMilliseconds(16)) // 60 FPS
            .Subscribe(_ => Update())
            .DisposeWith(_disposables);
    }

    private void Update()
    {
        Player.Update(_world);
        Inventory.Update(_world);
        MessageLog.Update();
        Map.Update(_world);
    }

    public void Dispose()
    {
        _disposables?.Dispose();
    }
}
```

---

## Phase 2: Remaining View Models (Week 2)

### Issue #24: Create InventoryViewModel with Event Subscriptions

**Title**: Create InventoryViewModel with MessagePipe subscriptions

**Labels**: `enhancement`, `viewmodel`, `rfc-002`, `phase-2`

**RFC Reference**: [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)

**Dependencies**: Issue #19, Issue #20, Issue #21

**Description**:
Create InventoryViewModel with ObservableCollections and event subscriptions for inventory changes.

**Acceptance Criteria**:

- [ ] Inherits from ReactiveObject
- [ ] Uses ObservableList<ItemViewModel> for items
- [ ] Subscribes to ItemPickedUpEvent, ItemUsedEvent, ItemDroppedEvent
- [ ] SelectedIndex and SelectedItem properties
- [ ] Update() method syncs from ECS
- [ ] Events automatically update UI
- [ ] Unit tests for event handling

**Files to Create**:

- `dotnet/shared-app/ViewModels/InventoryViewModel.cs`
- `dotnet/shared-app/ViewModels/ItemViewModel.cs`
- `dotnet/shared-app.Tests/ViewModels/InventoryViewModelTests.cs`

**Code Example**:

```csharp
public class InventoryViewModel : ReactiveObject
{
    private readonly ISubscriber<ItemPickedUpEvent> _pickupSubscriber;
    private readonly CompositeDisposable _disposables = new();

    public InventoryViewModel(ISubscriber<ItemPickedUpEvent> pickupSubscriber)
    {
        _pickupSubscriber = pickupSubscriber;
        Items = new ObservableList<ItemViewModel>();

        _pickupSubscriber.Subscribe(e =>
        {
            // Add item to UI when picked up
            Items.Add(new ItemViewModel { Name = e.ItemName, Type = e.Type });
        }).DisposeWith(_disposables);
    }

    public ObservableList<ItemViewModel> Items { get; }

    private int _selectedIndex = -1;
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedIndex, value);
    }
}
```

---

### Issue #25: Create MessageLogViewModel with Event Subscriptions

**Title**: Create MessageLogViewModel with combat/game event subscriptions

**Labels**: `enhancement`, `viewmodel`, `rfc-002`, `phase-2`

**RFC Reference**: [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)

**Dependencies**: Issue #19, Issue #20, Issue #21

**Description**:
Create MessageLogViewModel that subscribes to all game events and displays messages.

**Acceptance Criteria**:

- [ ] Inherits from ReactiveObject
- [ ] Uses ObservableList<MessageViewModel> for messages
- [ ] Subscribes to combat, inventory, level, map events
- [ ] AddMessage method with message type
- [ ] Keeps last 100 messages
- [ ] Color-coded by message type
- [ ] Unit tests for event subscriptions

**Files to Create**:

- `dotnet/shared-app/ViewModels/MessageLogViewModel.cs`
- `dotnet/shared-app/ViewModels/MessageViewModel.cs`
- `dotnet/shared-app.Tests/ViewModels/MessageLogViewModelTests.cs`

**Code Example**:

```csharp
public class MessageLogViewModel : ReactiveObject
{
    private const int MaxMessages = 100;
    private readonly CompositeDisposable _disposables = new();

    public MessageLogViewModel(
        ISubscriber<PlayerDamagedEvent> damageSubscriber,
        ISubscriber<EnemyDefeatedEvent> defeatSubscriber,
        ISubscriber<ItemPickedUpEvent> pickupSubscriber)
    {
        Messages = new ObservableList<MessageViewModel>();

        damageSubscriber.Subscribe(e =>
        {
            AddMessage($"Took {e.Damage} damage from {e.Source}!", MessageType.Combat);
        }).DisposeWith(_disposables);

        defeatSubscriber.Subscribe(e =>
        {
            AddMessage($"Defeated {e.EnemyName}! Gained {e.ExperienceGained} XP.", MessageType.Combat);
        }).DisposeWith(_disposables);

        pickupSubscriber.Subscribe(e =>
        {
            AddMessage($"Picked up {e.ItemName}.", MessageType.Info);
        }).DisposeWith(_disposables);
    }

    public ObservableList<MessageViewModel> Messages { get; }

    public void AddMessage(string text, MessageType type = MessageType.Info)
    {
        Messages.Add(new MessageViewModel
        {
            Text = text,
            Type = type,
            Timestamp = DateTime.Now
        });

        while (Messages.Count > MaxMessages)
        {
            Messages.RemoveAt(0);
        }
    }
}
```

---

### Issue #26: Create MapViewModel

**Title**: Create MapViewModel for map state

**Labels**: `enhancement`, `viewmodel`, `rfc-002`, `phase-2`

**RFC Reference**: [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)

**Dependencies**: Issue #19

**Description**:
Create MapViewModel to track map dimensions, camera position, and visible tiles.

**Acceptance Criteria**:

- [ ] Inherits from ReactiveObject
- [ ] Properties for Width, Height, CameraPosition
- [ ] ObservableList for VisibleTiles
- [ ] Update() syncs from GameWorld
- [ ] Camera follows player
- [ ] Unit tests for camera tracking

**Files to Create**:

- `dotnet/shared-app/ViewModels/MapViewModel.cs`
- `dotnet/shared-app/ViewModels/TileViewModel.cs`
- `dotnet/shared-app.Tests/ViewModels/MapViewModelTests.cs`

**Code Example**:

```csharp
public class MapViewModel : ReactiveObject
{
    private int _width;
    private int _height;
    private Position _cameraPosition;

    public int Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    public Position CameraPosition
    {
        get => _cameraPosition;
        set => this.RaiseAndSetIfChanged(ref _cameraPosition, value);
    }

    public ObservableList<TileViewModel> VisibleTiles { get; } = new();

    internal void Update(GameWorld world)
    {
        Width = world.Map.Width;
        Height = world.Map.Height;

        // Center camera on player
        var player = world.GetPlayerEntity();
        if (player.HasValue)
        {
            var pos = world.GetComponent<Position>(player.Value);
            CameraPosition = pos.Point;
        }

        UpdateVisibleTiles(world);
    }
}
```

---

### Issue #27: Update Game Systems to Publish Events

**Title**: Update combat and inventory systems to publish MessagePipe events

**Labels**: `refactor`, `events`, `rfc-002`, `phase-2`

**RFC Reference**: [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)

**Dependencies**: Issue #20, Issue #21

**Description**:
Refactor game systems (combat, inventory, etc.) to publish events via MessagePipe when actions occur.

**Acceptance Criteria**:

- [ ] Combat system publishes PlayerDamagedEvent, EnemyDefeatedEvent
- [ ] Inventory system publishes ItemPickedUpEvent, ItemUsedEvent, ItemDroppedEvent
- [ ] Player progression publishes PlayerLevelUpEvent
- [ ] Map interactions publish DoorOpenedEvent, StairsDescendedEvent
- [ ] Events include all relevant data
- [ ] Unit tests verify events are published

**Files to Modify**:

- `dotnet/shared-app/GameWorld.cs` (or create separate system classes)

**Code Example**:

```csharp
public class CombatSystem
{
    private readonly IPublisher<PlayerDamagedEvent> _damagePublisher;
    private readonly IPublisher<EnemyDefeatedEvent> _defeatPublisher;

    public CombatSystem(
        IPublisher<PlayerDamagedEvent> damagePublisher,
        IPublisher<EnemyDefeatedEvent> defeatPublisher)
    {
        _damagePublisher = damagePublisher;
        _defeatPublisher = defeatPublisher;
    }

    public void DamagePlayer(int damage, string source)
    {
        // Apply damage to player...
        var health = GetPlayerHealth();
        health.Current -= damage;

        // Publish event
        _damagePublisher.Publish(new PlayerDamagedEvent
        {
            Damage = damage,
            RemainingHealth = health.Current,
            Source = source
        });
    }

    public void DefeatEnemy(string enemyName, int xpReward)
    {
        // Handle enemy defeat...

        // Publish event
        _defeatPublisher.Publish(new EnemyDefeatedEvent
        {
            EnemyName = enemyName,
            ExperienceGained = xpReward
        });
    }
}
```

---

## Phase 3: Windows App Integration (Week 3)

### Issue #28: Update MainWindow XAML with Data Bindings

**Title**: Update Windows MainWindow XAML with ReactiveUI bindings

**Labels**: `enhancement`, `windows`, `ui`, `rfc-002`, `phase-3`

**RFC Reference**: [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)

**Dependencies**: Issue #23, Issue #24, Issue #25, Issue #26

**Description**:
Update the Windows app MainWindow XAML to use data bindings to GameViewModel.

**Acceptance Criteria**:

- [ ] DataContext set to GameViewModel
- [ ] Player health, level displayed with bindings
- [ ] Inventory list bound to Items collection
- [ ] Message log bound to Messages collection
- [ ] UI updates automatically when properties change
- [ ] No code-behind for UI updates

**Files to Modify**:

- `dotnet/windows-app/MainWindow.axaml`
- `dotnet/windows-app/MainWindow.axaml.cs`

**Code Example**:

```xml
<Window xmlns="https://github.com/avaloniaui"
        x:DataType="vm:GameViewModel"
        Title="Pigeon Pea">
    <Grid RowDefinitions="*,Auto">
        <!-- Game Canvas -->
        <local:GameCanvas Grid.Row="0" />

        <!-- HUD -->
        <StackPanel Grid.Row="1" Orientation="Horizontal">
            <TextBlock Text="{Binding Player.Name}" />
            <TextBlock Text="{Binding Player.HealthDisplay}" />
            <ProgressBar Value="{Binding Player.HealthPercentage}"
                         Minimum="0" Maximum="1" Width="100"/>
            <TextBlock Text="{Binding Player.LevelDisplay}" />
        </StackPanel>
    </Grid>
</Window>
```

---

### Issue #29: Implement ReactiveCommands for User Actions

**Title**: Implement ReactiveCommands for inventory and game actions

**Labels**: `enhancement`, `windows`, `ui`, `rfc-002`, `phase-3`

**RFC Reference**: [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)

**Dependencies**: Issue #23, Issue #24

**Description**:
Create ReactiveCommands for user actions like using items, dropping items, etc.

**Acceptance Criteria**:

- [ ] UseItemCommand created with CanExecute based on selection
- [ ] DropItemCommand created with CanExecute based on selection
- [ ] Commands bound to UI buttons
- [ ] Commands execute game logic
- [ ] Commands publish events
- [ ] Unit tests for command execution

**Files to Modify**:

- `dotnet/shared-app/ViewModels/GameViewModel.cs`

**Code Example**:

```csharp
public class GameViewModel : ReactiveObject
{
    public ReactiveCommand<Unit, Unit> UseItemCommand { get; }
    public ReactiveCommand<Unit, Unit> DropItemCommand { get; }

    public GameViewModel(GameWorld world, IServiceProvider services)
    {
        // ... initialization ...

        var canUseItem = Inventory.WhenAnyValue(x => x.SelectedItem)
            .Select(item => item != null);

        UseItemCommand = ReactiveCommand.Create(
            () => UseSelectedItem(),
            canUseItem
        );

        DropItemCommand = ReactiveCommand.Create(
            () => DropSelectedItem(),
            canUseItem
        );
    }

    private void UseSelectedItem()
    {
        if (Inventory.SelectedItem == null) return;
        // Use item logic...
    }
}
```

---

## Phase 4: Console App Integration (Week 4)

### Issue #30: Add System.CommandLine for CLI Arguments

**Title**: Add System.CommandLine for parsing console app arguments

**Labels**: `enhancement`, `console`, `cli`, `rfc-002`, `phase-4`

**RFC Reference**: [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)

**Dependencies**: None

**Description**:
Add System.CommandLine package and implement CLI argument parsing for renderer selection and other options.

**Acceptance Criteria**:

- [ ] System.CommandLine package added to console-app
- [ ] --renderer option (auto, kitty, sixel, braille, ascii)
- [ ] --debug option
- [ ] --width and --height options
- [ ] Help text displays correctly
- [ ] Arguments parsed and passed to app

**Files to Modify**:

- `dotnet/console-app/PigeonPea.Console.csproj`
- `dotnet/console-app/Program.cs`

**Code Example**:

```csharp
using System.CommandLine;

var rootCommand = new RootCommand("Pigeon Pea - Roguelike Dungeon Crawler");

var rendererOption = new Option<string>(
    name: "--renderer",
    description: "Renderer to use (auto, kitty, sixel, braille, ascii)",
    getDefaultValue: () => "auto"
);
rootCommand.AddOption(rendererOption);

var debugOption = new Option<bool>("--debug", "Enable debug mode");
rootCommand.AddOption(debugOption);

rootCommand.SetHandler((renderer, debug) =>
{
    RunGame(renderer, debug);
}, rendererOption, debugOption);

return await rootCommand.InvokeAsync(args);
```

---

### Issue #31: Create Console Views with Reactive Subscriptions

**Title**: Create Terminal.Gui views with reactive subscriptions

**Labels**: `enhancement`, `console`, `ui`, `rfc-002`, `phase-4`

**RFC Reference**: [RFC-002: UI Framework Integration](rfcs/002-ui-framework-integration.md)

**Dependencies**: Issue #23, Issue #24, Issue #25

**Description**:
Create Terminal.Gui view classes that subscribe to view model property changes.

**Acceptance Criteria**:

- [ ] PlayerView subscribes to PlayerViewModel changes
- [ ] InventoryView subscribes to InventoryViewModel changes
- [ ] MessageLogView subscribes to MessageLogViewModel changes
- [ ] Views update when properties change
- [ ] Subscriptions disposed properly
- [ ] Integration tests

**Files to Create**:

- `dotnet/console-app/Views/PlayerView.cs`
- `dotnet/console-app/Views/InventoryView.cs`
- `dotnet/console-app/Views/MessageLogView.cs`

**Code Example**:

```csharp
public class PlayerView : FrameView
{
    private readonly PlayerViewModel _viewModel;
    private readonly Label _healthLabel;
    private readonly CompositeDisposable _subscriptions = new();

    public PlayerView(PlayerViewModel viewModel)
    {
        _viewModel = viewModel;
        Title = "Player";

        _healthLabel = new Label { X = 1, Y = 1 };
        Add(_healthLabel);

        _viewModel.WhenAnyValue(x => x.HealthDisplay)
            .Subscribe(health => _healthLabel.Text = $"Health: {health}")
            .DisposeWith(_subscriptions);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _subscriptions?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

---

# RFC-003: Testing and Verification Issues

## Phase 1: Unit Test Infrastructure (Week 1)

### Issue #32: Set Up Test Projects

**Title**: Create test projects for all applications

**Labels**: `testing`, `infrastructure`, `rfc-003`, `phase-1`

**RFC Reference**: [RFC-003: Testing and Verification](rfcs/003-testing-verification.md)

**Dependencies**: None

**Description**:
Create xUnit test projects for shared-app, windows-app, and console-app with necessary test packages.

**Acceptance Criteria**:

- [ ] shared-app.Tests project created
- [ ] windows-app.Tests project created
- [ ] console-app.Tests project created
- [ ] xUnit, Moq, FluentAssertions packages added
- [ ] Test projects reference main projects
- [ ] dotnet test runs successfully
- [ ] CI runs tests on every push

**Files to Create**:

- `dotnet/shared-app.Tests/shared-app.Tests.csproj`
- `dotnet/windows-app.Tests/windows-app.Tests.csproj`
- `dotnet/console-app.Tests/console-app.Tests.csproj`

**Code Example**:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.6.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../shared-app/PigeonPea.Shared.csproj" />
  </ItemGroup>
</Project>
```

---

### Issue #33: Create Mock Implementations for Testing

**Title**: Create mock renderer and mock services for testing

**Labels**: `testing`, `mocks`, `rfc-003`, `phase-1`

**RFC Reference**: [RFC-003: Testing and Verification](rfcs/003-testing-verification.md)

**Dependencies**: Issue #1, Issue #32

**Description**:
Create mock implementations of IRenderer, IRenderTarget, and other interfaces for unit testing.

**Acceptance Criteria**:

- [ ] MockRenderer implements IRenderer
- [ ] MockRenderTarget implements IRenderTarget
- [ ] Mocks track method calls
- [ ] Mocks allow verification of drawing operations
- [ ] MockRenderer can return captured frame
- [ ] Unit tests use mocks successfully

**Files to Create**:

- `dotnet/shared-app.Tests/Mocks/MockRenderer.cs`
- `dotnet/shared-app.Tests/Mocks/MockRenderTarget.cs`
- `dotnet/shared-app.Tests/Mocks/MockPublisher.cs`
- `dotnet/shared-app.Tests/Mocks/MockSubscriber.cs`

**Code Example**:

```csharp
public class MockRenderer : IRenderer
{
    public List<(int x, int y, Tile tile)> DrawnTiles { get; } = new();
    public int BeginFrameCalls { get; private set; }
    public int EndFrameCalls { get; private set; }

    public RendererCapabilities Capabilities => RendererCapabilities.CharacterBased;

    public void BeginFrame() => BeginFrameCalls++;
    public void EndFrame() => EndFrameCalls++;

    public void DrawTile(int x, int y, Tile tile)
    {
        DrawnTiles.Add((x, y, tile));
    }

    public Tile? GetTileAt(int x, int y)
    {
        return DrawnTiles.FirstOrDefault(t => t.x == x && t.y == y).tile;
    }
}
```

---

## Phase 2: Console Visual Testing (Week 2-3)

### Issue #34: Install Node.js Dependencies for PTY Testing

**Title**: Set up node-pty for pseudoterminal testing

**Labels**: `testing`, `console`, `dependencies`, `rfc-003`, `phase-2`

**RFC Reference**: [RFC-003: Testing and Verification](rfcs/003-testing-verification.md)

**Dependencies**: None

**Description**:
Install Node.js and node-pty for console app testing via pseudoterminal.

**Acceptance Criteria**:

- [ ] package.json created in tests/pty directory
- [ ] node-pty dependency added
- [ ] npm install succeeds
- [ ] Simple PTY spawn test works
- [ ] CI installs Node.js and dependencies

**Files to Create**:

- `tests/pty/package.json`
- `tests/pty/.gitignore`
- `.github/workflows/console-visual-tests.yml`

**Code Example**:

```json
{
  "name": "pigeon-pea-pty-tests",
  "version": "1.0.0",
  "dependencies": {
    "node-pty": "^1.0.0"
  },
  "devDependencies": {
    "asciinema": "^2.4.0"
  }
}
```

---

### Issue #35: Create PTY Test Runner Script

**Title**: Create Node.js script for running PTY tests

**Labels**: `testing`, `console`, `rfc-003`, `phase-2`

**RFC Reference**: [RFC-003: Testing and Verification](rfcs/003-testing-verification.md)

**Dependencies**: Issue #34

**Description**:
Create a Node.js script that spawns the console app in a PTY, sends inputs, and captures output.

**Acceptance Criteria**:

- [ ] Script spawns console app in PTY
- [ ] Loads test scenario from JSON
- [ ] Sends inputs with delays
- [ ] Captures output
- [ ] Records with asciinema
- [ ] Exits cleanly
- [ ] Returns exit code 0 on success

**Files to Create**:

- `tests/pty/test-pty.js`
- `tests/pty/scenarios/basic-movement.json`

**Code Example**: (See Issue #1 detail in main document)

---

### Issue #36: Create Asciinema Parser for C# Tests

**Title**: Create C# parser for asciinema recordings

**Labels**: `testing`, `console`, `rfc-003`, `phase-2`

**RFC Reference**: [RFC-003: Testing and Verification](rfcs/003-testing-verification.md)

**Dependencies**: Issue #35

**Description**:
Create a C# class to parse asciinema recordings and extract frames for verification.

**Acceptance Criteria**:

- [ ] Parse asciinema v2 format
- [ ] Extract frames with timestamps
- [ ] Find frames at specific timestamps
- [ ] Extract frame content
- [ ] Remove ANSI escape codes for comparison
- [ ] Unit tests for parser

**Files to Create**:

- `dotnet/console-app.Tests/Visual/AsciinemaParser.cs`
- `dotnet/console-app.Tests/Visual/Frame.cs`
- `dotnet/console-app.Tests/Visual/AsciinemaParserTests.cs`

**Code Example**: (See implementation in main document)

---

### Issue #37: Create Console Visual Snapshot Tests

**Title**: Create snapshot-based visual regression tests for console

**Labels**: `testing`, `console`, `visual`, `rfc-003`, `phase-2`

**RFC Reference**: [RFC-003: Testing and Verification](rfcs/003-testing-verification.md)

**Dependencies**: Issue #35, Issue #36

**Description**:
Create visual snapshot tests that compare console output against stored snapshots.

**Acceptance Criteria**:

- [ ] Test runs PTY scenario
- [ ] Compares output with snapshot
- [ ] Creates snapshot if missing
- [ ] Reports diff if mismatch
- [ ] Snapshots stored in git
- [ ] CI fails on visual regression

**Files to Create**:

- `dotnet/console-app.Tests/Visual/SnapshotTests.cs`
- `dotnet/console-app.Tests/snapshots/main-menu.txt`

**Code Example**: (See implementation in main document)

---

## Phase 3: Windows Visual Testing (Week 4-5)

### Issue #38: Create FFmpeg Recording Script

**Title**: Create PowerShell script for FFmpeg window recording

**Labels**: `testing`, `windows`, `visual`, `rfc-003`, `phase-3`

**RFC Reference**: [RFC-003: Testing and Verification](rfcs/003-testing-verification.md)

**Dependencies**: None

**Description**:
Create a PowerShell script that starts the Windows app and records it with FFmpeg.

**Acceptance Criteria**:

- [ ] Script starts Windows app
- [ ] FFmpeg captures window
- [ ] Recording duration configurable
- [ ] Output saved to file
- [ ] App closed after recording
- [ ] Script works on CI

**Files to Create**:

- `tests/windows/record-test.ps1`

**Code Example**: (See script in main document)

---

### Issue #39: Create Frame Extractor

**Title**: Create utility to extract frames from video

**Labels**: `testing`, `windows`, `visual`, `rfc-003`, `phase-3`

**RFC Reference**: [RFC-003: Testing and Verification](rfcs/003-testing-verification.md)

**Dependencies**: Issue #38

**Description**:
Create a C# utility that uses FFmpeg to extract individual frames from recorded video.

**Acceptance Criteria**:

- [ ] Extracts frames at 1 FPS
- [ ] Saves as PNG files
- [ ] Returns list of frame paths
- [ ] Configurable frame rate
- [ ] Works on CI

**Files to Create**:

- `dotnet/windows-app.Tests/Visual/FrameExtractor.cs`

**Code Example**: (See implementation in main document)

---

### Issue #40: Create Image Comparison Utility

**Title**: Create ImageSharp-based image comparison utility

**Labels**: `testing`, `windows`, `visual`, `rfc-003`, `phase-3`

**RFC Reference**: [RFC-003: Testing and Verification](rfcs/003-testing-verification.md)

**Dependencies**: None

**Description**:
Create an image comparison utility using ImageSharp for pixel-by-pixel comparison.

**Acceptance Criteria**:

- [ ] Compare two images
- [ ] Return similarity percentage
- [ ] Configurable threshold
- [ ] Generate diff image
- [ ] Handle dimension mismatches
- [ ] Unit tests

**Files to Create**:

- `dotnet/windows-app.Tests/Visual/ImageComparator.cs`
- `dotnet/windows-app.Tests/Visual/ImageComparisonResult.cs`

**Files to Add Package**:

- `dotnet/windows-app.Tests/windows-app.Tests.csproj` (add SixLabors.ImageSharp)

**Code Example**: (See implementation in main document)

---

### Issue #41: Create Windows Visual Snapshot Tests

**Title**: Create snapshot-based visual tests for Windows app

**Labels**: `testing`, `windows`, `visual`, `rfc-003`, `phase-3`

**RFC Reference**: [RFC-003: Testing and Verification](rfcs/003-testing-verification.md)

**Dependencies**: Issue #38, Issue #39, Issue #40

**Description**:
Create visual snapshot tests that record Windows app, extract frames, and compare with stored snapshots.

**Acceptance Criteria**:

- [ ] Records test scenario
- [ ] Extracts frames
- [ ] Compares with snapshots
- [ ] Creates snapshots if missing
- [ ] Fails on mismatch
- [ ] Snapshots in Git LFS

**Files to Create**:

- `dotnet/windows-app.Tests/Visual/WindowsVisualTests.cs`
- `dotnet/windows-app.Tests/snapshots/.gitattributes` (Git LFS config)

---

## Phase 4: Performance Testing (Week 6)

### Issue #42: Create Frame Rate Monitoring

**Title**: Create frame rate monitoring for performance tests

**Labels**: `testing`, `performance`, `rfc-003`, `phase-4`

**RFC Reference**: [RFC-003: Testing and Verification](rfcs/003-testing-verification.md)

**Dependencies**: Issue #5, Issue #11

**Description**:
Create a frame rate monitor that tracks FPS, minimum, maximum, and average frame times.

**Acceptance Criteria**:

- [ ] FrameRateMetrics class
- [ ] RecordFrame method
- [ ] Calculate average, min, max FPS
- [ ] Calculate percentiles (p50, p95, p99)
- [ ] Reset method
- [ ] Unit tests

**Files to Create**:

- `dotnet/shared-app/Performance/FrameRateMetrics.cs`
- `dotnet/shared-app.Tests/Performance/FrameRateMetricsTests.cs`

**Code Example**:

```csharp
public class FrameRateMetrics
{
    private List<double> _frameTimes = new();
    private Stopwatch _frameTimer = new();

    public void RecordFrame()
    {
        if (_frameTimer.IsRunning)
        {
            _frameTimes.Add(_frameTimer.Elapsed.TotalMilliseconds);
        }
        _frameTimer.Restart();
    }

    public double AverageFPS => 1000.0 / _frameTimes.Average();
    public double MinFPS => 1000.0 / _frameTimes.Max();
    public double MaxFPS => 1000.0 / _frameTimes.Min();
}
```

---

### Issue #43: Create Performance Benchmarks

**Title**: Create BenchmarkDotNet performance benchmarks

**Labels**: `testing`, `performance`, `rfc-003`, `phase-4`

**RFC Reference**: [RFC-003: Testing and Verification](rfcs/003-testing-verification.md)

**Dependencies**: Issue #42

**Description**:
Create BenchmarkDotNet benchmarks for rendering performance.

**Acceptance Criteria**:

- [ ] Benchmark for full screen rendering
- [ ] Benchmark for particle rendering
- [ ] Benchmark for sprite rendering
- [ ] Benchmark for viewport culling
- [ ] Memory diagnostics
- [ ] Results tracked in CI

**Files to Create**:

- `dotnet/benchmarks/RenderingBenchmarks.cs`
- `dotnet/benchmarks/benchmarks.csproj`

**Code Example**: (See implementation in main document)

---

## Summary

Total: **43 issues** across 3 RFCs

- **RFC-001 (Rendering)**: 18 issues
- **RFC-002 (UI Integration)**: 13 issues
- **RFC-003 (Testing)**: 12 issues

Each issue is:

- Moderate in scope (1-3 days of work)
- Has clear acceptance criteria
- Includes code examples
- Specifies file paths
- Lists dependencies
- Suitable for GitHub coding agents
