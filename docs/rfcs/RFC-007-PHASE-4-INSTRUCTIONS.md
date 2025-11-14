# RFC-007 Phase 4: Dungeon Domain Implementation - Detailed Instructions

## Status: Ready for Implementation

**Created**: 2025-11-13
**For**: Agent implementing Phase 4 of RFC-007
**Prerequisites**: Phase 1-3 complete (Map domain implemented with proper adapter pattern)

## Overview

This document provides step-by-step instructions for implementing Phase 4 of RFC-007: the Dungeon domain. Follow the same architectural patterns successfully used in the Map domain (Phase 3).

## Current State Assessment

### ✅ Already Implemented

**Dungeon.Core** (~250 lines):

- `DungeonData.cs` - Complete grid model with walls, floors, doors
- `IDungeonGenerator.cs` - Interface for dungeon generation
- `BasicDungeonGenerator.cs` - Working dungeon generator (rooms + corridors + doors)
- GoRogue package reference: ✅ v3.0.0-beta10

**Dungeon.Control** (~140 lines):

- `FovCalculator.cs` - Complete FOV calculation (Bresenham LOS)
- `PathfindingService.cs` - Complete A\* pathfinding with diagonal support
- `DungeonNavigator.cs` - Facade combining pathfinding + FOV
- GoRogue package reference: ✅ v3.0.0-beta10

**Dungeon.Tests**:

- Test project exists with xUnit, FluentAssertions
- References to Dungeon.Core and Dungeon.Control

### ❌ To Be Implemented

**Dungeon.Rendering** (currently only `Class1.cs` placeholder):

- `TilesetRenderer.cs` - Render dungeon to pixel tiles
- `BrailleDungeonRenderer.cs` - Render dungeon to Braille/ASCII
- `FovRenderer.cs` - Apply lighting/shadows based on FOV
- `EntityRenderer.cs` - Render ECS entities (monsters, items)
- `Tiles/DungeonTileSource.cs` - Implement `ITileSource` interface

**Dungeon.Core Enhancements**:

- `Adapters/GoRogueAdapter.cs` - Wrap GoRogue generation (if needed beyond BasicDungeonGenerator)

**Dungeon.Control Enhancements**:

- `ViewModels/DungeonControlViewModel.cs` - Player position, FOV state (ReactiveUI)

**Console App Integration**:

- `Views/DungeonPanelView.cs` - Terminal.Gui view for dungeon
- Initialize dungeon ECS world
- Demo dungeon rendering

**Tests**:

- Unit tests for rendering
- Integration test for full pipeline

## Implementation Tasks

---

## Task 1: Implement Dungeon.Rendering Core

**Priority**: P0
**Estimated Lines**: ~300-400
**Dependencies**: None (uses existing Dungeon.Core)

### 1.1 Create `SkiaDungeonRasterizer.cs`

Create the basic pixel-based renderer for dungeons (analogous to `SkiaMapRasterizer` in Map.Rendering).

**File**: `dotnet/Dungeon/PigeonPea.Dungeon.Rendering/SkiaDungeonRasterizer.cs`

```csharp
using System;
using PigeonPea.Dungeon.Core;

namespace PigeonPea.Dungeon.Rendering;

/// <summary>
/// Renders DungeonData to RGBA raster using simple tile-based coloring.
/// </summary>
public static class SkiaDungeonRasterizer
{
    public sealed record Raster(byte[] Rgba, int WidthPx, int HeightPx);

    /// <summary>
    /// Render dungeon to RGBA buffer.
    /// </summary>
    /// <param name="dungeon">Dungeon data to render</param>
    /// <param name="viewportX">Top-left X coordinate of viewport (in tiles)</param>
    /// <param name="viewportY">Top-left Y coordinate of viewport (in tiles)</param>
    /// <param name="viewportWidth">Viewport width in tiles</param>
    /// <param name="viewportHeight">Viewport height in tiles</param>
    /// <param name="pixelsPerCell">Pixels per tile (e.g., 16 for 16x16 tiles)</param>
    /// <param name="fov">Optional FOV mask (true = visible); if null, all tiles visible</param>
    /// <returns>RGBA raster</returns>
    public static Raster Render(
        DungeonData dungeon,
        int viewportX,
        int viewportY,
        int viewportWidth,
        int viewportHeight,
        int pixelsPerCell = 16,
        bool[,]? fov = null)
    {
        int widthPx = Math.Max(1, viewportWidth * pixelsPerCell);
        int heightPx = Math.Max(1, viewportHeight * pixelsPerCell);

        var rgba = new byte[widthPx * heightPx * 4];

        for (int cy = 0; cy < viewportHeight; cy++)
        {
            for (int cx = 0; cx < viewportWidth; cx++)
            {
                int wx = viewportX + cx;
                int wy = viewportY + cy;

                // Determine tile color
                byte r, g, b, a = 255;
                if (!dungeon.InBounds(wx, wy))
                {
                    // Out of bounds = black
                    r = 0; g = 0; b = 0;
                }
                else if (dungeon.IsDoor(wx, wy))
                {
                    // Doors: brown (closed) or tan (open)
                    if (dungeon.IsDoorClosed(wx, wy))
                    {
                        r = 139; g = 69; b = 19; // Brown
                    }
                    else
                    {
                        r = 210; g = 180; b = 140; // Tan
                    }
                }
                else if (dungeon.IsWalkable(wx, wy))
                {
                    // Floor = light gray
                    r = 200; g = 200; b = 200;
                }
                else
                {
                    // Wall = dark gray
                    r = 80; g = 80; b = 80;
                }

                // Apply FOV dimming (if FOV provided and tile not visible)
                if (fov != null)
                {
                    bool visible = wy >= 0 && wy < fov.GetLength(0) && wx >= 0 && wx < fov.GetLength(1) && fov[wy, wx];
                    if (!visible)
                    {
                        // Dim to 30% brightness
                        r = (byte)(r * 0.3);
                        g = (byte)(g * 0.3);
                        b = (byte)(b * 0.3);
                    }
                }

                // Fill pixelsPerCell x pixelsPerCell block
                int startPxX = cx * pixelsPerCell;
                int startPxY = cy * pixelsPerCell;
                for (int oy = 0; oy < pixelsPerCell; oy++)
                {
                    int py = startPxY + oy;
                    if ((uint)py >= (uint)heightPx) break;
                    int rowIdx = py * widthPx * 4 + startPxX * 4;
                    for (int ox = 0; ox < pixelsPerCell; ox++)
                    {
                        int idx = rowIdx + ox * 4;
                        if (idx + 3 >= rgba.Length) break;
                        rgba[idx] = r;
                        rgba[idx + 1] = g;
                        rgba[idx + 2] = b;
                        rgba[idx + 3] = a;
                    }
                }
            }
        }

        return new Raster(rgba, widthPx, heightPx);
    }
}
```

**Pattern Reference**: This follows `Map.Rendering/SkiaMapRasterizer.cs` pattern.

### 1.2 Create `BrailleDungeonRenderer.cs`

Create console/terminal renderer using Braille characters (analogous to `BrailleMapRenderer` in Map.Rendering).

**File**: `dotnet/Dungeon/PigeonPea.Dungeon.Rendering/BrailleDungeonRenderer.cs`

```csharp
using System;
using System.Text;
using PigeonPea.Dungeon.Core;

namespace PigeonPea.Dungeon.Rendering;

/// <summary>
/// Renders DungeonData to Braille/ASCII for terminal display.
/// </summary>
public static class BrailleDungeonRenderer
{
    /// <summary>
    /// Render dungeon to ASCII grid for console display.
    /// </summary>
    /// <param name="dungeon">Dungeon data</param>
    /// <param name="viewportX">Top-left X (in tiles)</param>
    /// <param name="viewportY">Top-left Y (in tiles)</param>
    /// <param name="viewportWidth">Viewport width (in tiles)</param>
    /// <param name="viewportHeight">Viewport height (in tiles)</param>
    /// <param name="fov">Optional FOV mask</param>
    /// <param name="playerX">Player X position (optional, for '@' marker)</param>
    /// <param name="playerY">Player Y position (optional, for '@' marker)</param>
    /// <returns>Multi-line string with dungeon ASCII art</returns>
    public static string RenderAscii(
        DungeonData dungeon,
        int viewportX,
        int viewportY,
        int viewportWidth,
        int viewportHeight,
        bool[,]? fov = null,
        int? playerX = null,
        int? playerY = null)
    {
        var sb = new StringBuilder(viewportWidth * viewportHeight + viewportHeight);

        for (int cy = 0; cy < viewportHeight; cy++)
        {
            for (int cx = 0; cx < viewportWidth; cx++)
            {
                int wx = viewportX + cx;
                int wy = viewportY + cy;

                // Check if player is at this position
                if (playerX.HasValue && playerY.HasValue && wx == playerX.Value && wy == playerY.Value)
                {
                    sb.Append('@');
                    continue;
                }

                // Check FOV
                bool visible = true;
                if (fov != null)
                {
                    visible = wy >= 0 && wy < fov.GetLength(0) && wx >= 0 && wx < fov.GetLength(1) && fov[wy, wx];
                }

                char ch;
                if (!dungeon.InBounds(wx, wy))
                {
                    ch = ' '; // Out of bounds
                }
                else if (!visible)
                {
                    ch = ' '; // Not in FOV (could use '·' for explored but not visible)
                }
                else if (dungeon.IsDoorClosed(wx, wy))
                {
                    ch = '+'; // Closed door
                }
                else if (dungeon.IsDoorOpen(wx, wy))
                {
                    ch = '/'; // Open door
                }
                else if (dungeon.IsWalkable(wx, wy))
                {
                    ch = '.'; // Floor
                }
                else
                {
                    ch = '#'; // Wall
                }

                sb.Append(ch);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
```

**Pattern Reference**: This follows `Map.Rendering/BrailleMapRenderer.cs` pattern.

### 1.3 Create `FovRenderer.cs`

Provide utilities for applying FOV effects to rasters.

**File**: `dotnet/Dungeon/PigeonPea.Dungeon.Rendering/FovRenderer.cs`

```csharp
using System;

namespace PigeonPea.Dungeon.Rendering;

/// <summary>
/// Utilities for applying FOV (Field of View) effects to rendered rasters.
/// </summary>
public static class FovRenderer
{
    /// <summary>
    /// Darken pixels outside FOV in-place.
    /// </summary>
    /// <param name="rgba">RGBA buffer (modified in place)</param>
    /// <param name="widthPx">Width in pixels</param>
    /// <param name="heightPx">Height in pixels</param>
    /// <param name="fov">FOV mask (tile coordinates)</param>
    /// <param name="viewportX">Viewport top-left X (tiles)</param>
    /// <param name="viewportY">Viewport top-left Y (tiles)</param>
    /// <param name="pixelsPerCell">Pixels per tile</param>
    /// <param name="dimFactor">Brightness factor for non-visible tiles (0.0-1.0)</param>
    public static void ApplyFov(
        byte[] rgba,
        int widthPx,
        int heightPx,
        bool[,] fov,
        int viewportX,
        int viewportY,
        int pixelsPerCell,
        double dimFactor = 0.3)
    {
        int tilesX = widthPx / pixelsPerCell;
        int tilesY = heightPx / pixelsPerCell;

        for (int ty = 0; ty < tilesY; ty++)
        {
            for (int tx = 0; tx < tilesX; tx++)
            {
                int wx = viewportX + tx;
                int wy = viewportY + ty;

                bool visible = wy >= 0 && wy < fov.GetLength(0) && wx >= 0 && wx < fov.GetLength(1) && fov[wy, wx];

                if (!visible)
                {
                    // Dim this tile's pixels
                    int startPxX = tx * pixelsPerCell;
                    int startPxY = ty * pixelsPerCell;

                    for (int oy = 0; oy < pixelsPerCell; oy++)
                    {
                        int py = startPxY + oy;
                        if ((uint)py >= (uint)heightPx) break;

                        for (int ox = 0; ox < pixelsPerCell; ox++)
                        {
                            int px = startPxX + ox;
                            if ((uint)px >= (uint)widthPx) break;

                            int idx = (py * widthPx + px) * 4;
                            if (idx + 3 < rgba.Length)
                            {
                                rgba[idx] = (byte)(rgba[idx] * dimFactor);
                                rgba[idx + 1] = (byte)(rgba[idx + 1] * dimFactor);
                                rgba[idx + 2] = (byte)(rgba[idx + 2] * dimFactor);
                                // Alpha unchanged
                            }
                        }
                    }
                }
            }
        }
    }
}
```

---

## Task 2: Implement ECS Entity Rendering

**Priority**: P1
**Estimated Lines**: ~150-200
**Dependencies**: Shared.ECS components must exist

### 2.1 Verify Shared.ECS Components

Check that these components exist in `dotnet/Shared/PigeonPea.Shared.ECS/Components/`:

- `Position.cs` - 2D/3D position
- `Sprite.cs` - Visual representation
- `Renderable.cs` - Render flags
- `Tags/DungeonEntityTag.cs` - Tag for dungeon entities

If missing, create them following the pattern in RFC-007 lines 131-142.

**Example `Position.cs`** (if missing):

```csharp
namespace PigeonPea.Shared.ECS.Components;

public struct Position
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; } // Optional for 3D

    public Position(int x, int y, int z = 0)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
```

**Example `Sprite.cs`** (if missing):

```csharp
namespace PigeonPea.Shared.ECS.Components;

public struct Sprite
{
    public string TextureId { get; set; }
    public char AsciiChar { get; set; } // For terminal rendering
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }

    public Sprite(string textureId, char asciiChar = '?', byte r = 255, byte g = 255, byte b = 255)
    {
        TextureId = textureId;
        AsciiChar = asciiChar;
        R = r;
        G = g;
        B = b;
    }
}
```

**Example `Tags/DungeonEntityTag.cs`** (if missing):

```csharp
namespace PigeonPea.Shared.ECS.Components.Tags;

/// <summary>
/// Tag component identifying entities that belong to the dungeon domain.
/// </summary>
public struct DungeonEntityTag { }
```

### 2.2 Create `EntityRenderer.cs`

Render Arch ECS entities onto dungeon rasters.

**File**: `dotnet/Dungeon/PigeonPea.Dungeon.Rendering/EntityRenderer.cs`

```csharp
using System;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

namespace PigeonPea.Dungeon.Rendering;

/// <summary>
/// Renders ECS entities (monsters, items, player) onto dungeon rasters.
/// </summary>
public static class EntityRenderer
{
    /// <summary>
    /// Render entities from ECS world onto RGBA buffer.
    /// </summary>
    /// <param name="world">Arch ECS world containing dungeon entities</param>
    /// <param name="rgba">RGBA buffer to draw on</param>
    /// <param name="widthPx">Width in pixels</param>
    /// <param name="heightPx">Height in pixels</param>
    /// <param name="viewportX">Viewport top-left X (tiles)</param>
    /// <param name="viewportY">Viewport top-left Y (tiles)</param>
    /// <param name="pixelsPerCell">Pixels per tile</param>
    /// <param name="fov">Optional FOV mask (only render visible entities)</param>
    public static void RenderEntities(
        World world,
        byte[] rgba,
        int widthPx,
        int heightPx,
        int viewportX,
        int viewportY,
        int pixelsPerCell,
        bool[,]? fov = null)
    {
        var query = new QueryDescription()
            .WithAll<Position, Sprite, DungeonEntityTag>();

        world.Query(in query, (ref Position pos, ref Sprite sprite) =>
        {
            // Check FOV visibility
            if (fov != null)
            {
                if (pos.Y < 0 || pos.Y >= fov.GetLength(0) || pos.X < 0 || pos.X >= fov.GetLength(1))
                    return;
                if (!fov[pos.Y, pos.X])
                    return; // Not visible
            }

            // Calculate pixel position
            int tileX = pos.X - viewportX;
            int tileY = pos.Y - viewportY;

            int tilesX = widthPx / pixelsPerCell;
            int tilesY = heightPx / pixelsPerCell;

            if (tileX < 0 || tileX >= tilesX || tileY < 0 || tileY >= tilesY)
                return; // Outside viewport

            // Draw sprite (simple: fill entire tile with sprite color)
            int startPxX = tileX * pixelsPerCell;
            int startPxY = tileY * pixelsPerCell;

            for (int oy = 0; oy < pixelsPerCell; oy++)
            {
                int py = startPxY + oy;
                if ((uint)py >= (uint)heightPx) break;

                for (int ox = 0; ox < pixelsPerCell; ox++)
                {
                    int px = startPxX + ox;
                    if ((uint)px >= (uint)widthPx) break;

                    int idx = (py * widthPx + px) * 4;
                    if (idx + 3 < rgba.Length)
                    {
                        rgba[idx] = sprite.R;
                        rgba[idx + 1] = sprite.G;
                        rgba[idx + 2] = sprite.B;
                        rgba[idx + 3] = 255;
                    }
                }
            }
        });
    }

    /// <summary>
    /// Render entities as ASCII characters onto a string buffer.
    /// </summary>
    /// <param name="world">Arch ECS world</param>
    /// <param name="asciiBuffer">2D char array to modify</param>
    /// <param name="viewportX">Viewport X</param>
    /// <param name="viewportY">Viewport Y</param>
    /// <param name="fov">Optional FOV</param>
    public static void RenderEntitiesAscii(
        World world,
        char[,] asciiBuffer,
        int viewportX,
        int viewportY,
        bool[,]? fov = null)
    {
        var query = new QueryDescription()
            .WithAll<Position, Sprite, DungeonEntityTag>();

        int bufferHeight = asciiBuffer.GetLength(0);
        int bufferWidth = asciiBuffer.GetLength(1);

        world.Query(in query, (ref Position pos, ref Sprite sprite) =>
        {
            // Check FOV
            if (fov != null)
            {
                if (pos.Y < 0 || pos.Y >= fov.GetLength(0) || pos.X < 0 || pos.X >= fov.GetLength(1))
                    return;
                if (!fov[pos.Y, pos.X])
                    return;
            }

            int localX = pos.X - viewportX;
            int localY = pos.Y - viewportY;

            if (localX >= 0 && localX < bufferWidth && localY >= 0 && localY < bufferHeight)
            {
                asciiBuffer[localY, localX] = sprite.AsciiChar;
            }
        });
    }
}
```

---

## Task 3: Implement Tiles Support (Optional)

**Priority**: P2
**Estimated Lines**: ~100
**Dependencies**: Shared.Rendering/Tiles/ITileSource.cs

If you want tiled rendering (like Map.Rendering uses), create `Tiles/DungeonTileSource.cs`.

**File**: `dotnet/Dungeon/PigeonPea.Dungeon.Rendering/Tiles/DungeonTileSource.cs`

```csharp
using PigeonPea.Dungeon.Core;
using PigeonPea.Shared.Rendering.Tiles;

namespace PigeonPea.Dungeon.Rendering.Tiles;

/// <summary>
/// Tile source for dungeon rendering (implements ITileSource if available).
/// </summary>
public class DungeonTileSource
{
    // TODO: Implement ITileSource interface if needed for tiled rendering
    // For now, this is a placeholder showing the pattern
}
```

**Note**: This is lower priority. Focus on `SkiaDungeonRasterizer` and `BrailleDungeonRenderer` first.

---

## Task 4: Add GoRogueAdapter (Optional Enhancement)

**Priority**: P2
**Estimated Lines**: ~50-100
**Dependencies**: GoRogue package

The existing `BasicDungeonGenerator` is self-contained. If you want to use GoRogue's generators:

**File**: `dotnet/Dungeon/PigeonPea.Dungeon.Core/Adapters/GoRogueAdapter.cs`

```csharp
using System;
using PigeonPea.Dungeon.Core;
using GoRogue.MapGeneration;
using SadRogue.Primitives;

namespace PigeonPea.Dungeon.Core.Adapters;

/// <summary>
/// Adapter wrapping GoRogue's dungeon generation algorithms.
/// </summary>
public class GoRogueAdapter : IDungeonGenerator
{
    public DungeonData Generate(int width, int height, int? seed = null)
    {
        // Example: Use GoRogue's RectangleMapGenerator or similar
        // This is a placeholder showing the pattern

        var rng = seed.HasValue ? new GoRogue.Random.GlobalRandom(seed.Value) : new GoRogue.Random.GlobalRandom();

        // Create GoRogue map
        var goRogueMap = new GoRogue.GameFramework.Map(width, height, 1);

        // TODO: Use GoRogue generation algorithms
        // Example: RectangleMapGenerator, CellularAutomataGenerator, etc.

        // Convert GoRogue map to DungeonData
        var dungeon = new DungeonData(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // TODO: Map GoRogue terrain to DungeonData
                // This is a placeholder
                bool walkable = goRogueMap.GetTerrain(new Point(x, y)).IsWalkable;
                if (walkable)
                    dungeon.SetFloor(x, y);
                else
                    dungeon.SetWall(x, y);
            }
        }

        return dungeon;
    }
}
```

**Note**: `BasicDungeonGenerator` already works well. This adapter is optional if you want GoRogue's specific algorithms.

---

## Task 5: Add ViewModels for ReactiveUI

**Priority**: P1
**Estimated Lines**: ~80-120
**Dependencies**: ReactiveUI (if used in project)

**File**: `dotnet/Dungeon/PigeonPea.Dungeon.Control/ViewModels/DungeonControlViewModel.cs`

```csharp
using System;
using System.Reactive;
using ReactiveUI;
using PigeonPea.Dungeon.Core;

namespace PigeonPea.Dungeon.Control.ViewModels;

/// <summary>
/// ViewModel for dungeon navigation and state (player position, FOV, etc.)
/// </summary>
public class DungeonControlViewModel : ReactiveObject
{
    private readonly DungeonData _dungeon;
    private readonly DungeonNavigator _navigator;

    private int _playerX;
    private int _playerY;
    private int _fovRange = 8;
    private bool[,]? _currentFov;

    public DungeonControlViewModel(DungeonData dungeon)
    {
        _dungeon = dungeon;
        _navigator = new DungeonNavigator(dungeon, diagonals: false);

        // Find initial player position (first walkable tile)
        for (int y = 0; y < dungeon.Height; y++)
        {
            for (int x = 0; x < dungeon.Width; x++)
            {
                if (dungeon.IsWalkable(x, y))
                {
                    _playerX = x;
                    _playerY = y;
                    UpdateFov();
                    return;
                }
            }
        }
    }

    public int PlayerX
    {
        get => _playerX;
        set => this.RaiseAndSetIfChanged(ref _playerX, value);
    }

    public int PlayerY
    {
        get => _playerY;
        set => this.RaiseAndSetIfChanged(ref _playerY, value);
    }

    public int FovRange
    {
        get => _fovRange;
        set
        {
            this.RaiseAndSetIfChanged(ref _fovRange, value);
            UpdateFov();
        }
    }

    public bool[,]? CurrentFov
    {
        get => _currentFov;
        private set => this.RaiseAndSetIfChanged(ref _currentFov, value);
    }

    public ReactiveCommand<(int dx, int dy), Unit> MoveCommand { get; }

    public void Move(int dx, int dy)
    {
        int newX = _playerX + dx;
        int newY = _playerY + dy;

        if (_dungeon.IsWalkable(newX, newY))
        {
            PlayerX = newX;
            PlayerY = newY;
            UpdateFov();
        }
    }

    private void UpdateFov()
    {
        CurrentFov = _navigator.Visible(_playerX, _playerY, _fovRange);
    }
}
```

**Note**: If the project doesn't use ReactiveUI, create a simple POCO ViewModel instead.

---

## Task 6: Console App Integration

**Priority**: P0
**Estimated Lines**: ~150-200
**Dependencies**: Terminal.Gui, Dungeon.Rendering

### 6.1 Create `DungeonPanelView.cs`

**File**: `dotnet/console-app/Views/DungeonPanelView.cs`

```csharp
using Terminal.Gui;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control;
using PigeonPea.Dungeon.Rendering;

namespace PigeonPea.Console.Views;

/// <summary>
/// Terminal.Gui view displaying a dungeon in ASCII.
/// </summary>
public class DungeonPanelView : View
{
    private readonly DungeonData _dungeon;
    private readonly FovCalculator _fov;
    private int _playerX = 10;
    private int _playerY = 10;
    private int _fovRange = 8;

    public DungeonPanelView(DungeonData dungeon)
    {
        _dungeon = dungeon;
        _fov = new FovCalculator(dungeon);

        // Find first walkable tile for player
        for (int y = 0; y < dungeon.Height; y++)
        {
            for (int x = 0; x < dungeon.Width; x++)
            {
                if (dungeon.IsWalkable(x, y))
                {
                    _playerX = x;
                    _playerY = y;
                    break;
                }
            }
        }

        CanFocus = true;
    }

    public override void OnDrawContent(Rect viewport)
    {
        base.OnDrawContent(viewport);

        var fovMask = _fov.ComputeVisible(_playerX, _playerY, _fovRange);

        // Calculate viewport (center on player)
        int viewWidth = (int)viewport.Width;
        int viewHeight = (int)viewport.Height;
        int viewX = _playerX - viewWidth / 2;
        int viewY = _playerY - viewHeight / 2;

        // Render dungeon ASCII
        string asciiDungeon = BrailleDungeonRenderer.RenderAscii(
            _dungeon,
            viewX,
            viewY,
            viewWidth,
            viewHeight,
            fovMask,
            _playerX,
            _playerY);

        Move(0, 0);
        Driver.SetAttribute(new Terminal.Gui.Attribute(Color.White, Color.Black));
        Driver.AddStr(asciiDungeon);
    }

    public override bool ProcessKey(KeyEvent keyEvent)
    {
        int dx = 0, dy = 0;

        switch (keyEvent.Key)
        {
            case Key.CursorUp:
            case Key.k:
                dy = -1;
                break;
            case Key.CursorDown:
            case Key.j:
                dy = 1;
                break;
            case Key.CursorLeft:
            case Key.h:
                dx = -1;
                break;
            case Key.CursorRight:
            case Key.l:
                dx = 1;
                break;
            default:
                return base.ProcessKey(keyEvent);
        }

        int newX = _playerX + dx;
        int newY = _playerY + dy;

        if (_dungeon.IsWalkable(newX, newY))
        {
            _playerX = newX;
            _playerY = newY;
            SetNeedsDisplay();
        }

        return true;
    }
}
```

### 6.2 Add Dungeon Demo to Console App

**File**: `dotnet/console-app/DungeonDemoApplication.cs` (new file)

```csharp
using Terminal.Gui;
using PigeonPea.Dungeon.Core;
using PigeonPea.Console.Views;

namespace PigeonPea.Console;

/// <summary>
/// Simple dungeon demo application for console.
/// </summary>
public static class DungeonDemoApplication
{
    public static void Run()
    {
        Application.Init();

        var generator = new BasicDungeonGenerator();
        var dungeon = generator.Generate(width: 80, height: 40, seed: 12345);

        var top = Application.Top;
        var win = new Window("Dungeon Demo - Use arrow keys or hjkl to move")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var dungeonView = new DungeonPanelView(dungeon)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        win.Add(dungeonView);
        top.Add(win);

        Application.Run();
        Application.Shutdown();
    }
}
```

### 6.3 Update `Program.cs` to Add Dungeon Demo

**File**: `dotnet/console-app/Program.cs`

Add an option to launch the dungeon demo:

```csharp
// Add to command options
var dungeonDemoCommand = new Command("dungeon-demo", "Run dungeon demo");
dungeonDemoCommand.SetHandler(() =>
{
    DungeonDemoApplication.Run();
});
rootCommand.Add(dungeonDemoCommand);
```

---

## Task 7: ECS Integration

**Priority**: P1
**Estimated Lines**: ~100-150
**Dependencies**: Arch, Shared.ECS

### 7.1 Create Dungeon ECS World in Console App

Update `DungeonDemoApplication.cs` or create a new ECS-enabled version:

```csharp
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

// In DungeonDemoApplication or similar
var dungeonWorld = World.Create();

// Add example monster entities
for (int i = 0; i < 10; i++)
{
    // Find random walkable tile
    int x, y;
    do
    {
        x = rng.Next(0, dungeon.Width);
        y = rng.Next(0, dungeon.Height);
    } while (!dungeon.IsWalkable(x, y));

    var entity = dungeonWorld.Create<Position, Sprite, DungeonEntityTag>();
    dungeonWorld.Set(entity, new Position(x, y));
    dungeonWorld.Set(entity, new Sprite("goblin", 'g', 255, 100, 100)); // Red 'g'
    dungeonWorld.Set(entity, new DungeonEntityTag());
}
```

---

## Task 8: Write Tests

**Priority**: P1
**Estimated Lines**: ~200-300
**Dependencies**: xUnit, FluentAssertions

### 8.1 Unit Tests for Rendering

**File**: `dotnet/Dungeon.Tests/SkiaDungeonRasterizerTests.cs`

```csharp
using Xunit;
using FluentAssertions;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Rendering;

namespace PigeonPea.Dungeon.Tests;

public class SkiaDungeonRasterizerTests
{
    [Fact]
    public void Render_ShouldCreateNonEmptyRaster()
    {
        // Arrange
        var generator = new BasicDungeonGenerator();
        var dungeon = generator.Generate(30, 30, seed: 123);

        // Act
        var raster = SkiaDungeonRasterizer.Render(dungeon, 0, 0, 20, 20, pixelsPerCell: 16);

        // Assert
        raster.Should().NotBeNull();
        raster.WidthPx.Should().Be(20 * 16);
        raster.HeightPx.Should().Be(20 * 16);
        raster.Rgba.Length.Should().Be(20 * 16 * 20 * 16 * 4);
    }

    [Fact]
    public void Render_WithFov_ShouldDimNonVisibleTiles()
    {
        // Arrange
        var dungeon = new DungeonData(10, 10);
        for (int y = 0; y < 10; y++)
            for (int x = 0; x < 10; x++)
                dungeon.SetFloor(x, y);

        var fov = new bool[10, 10];
        fov[5, 5] = true; // Only center visible

        // Act
        var raster = SkiaDungeonRasterizer.Render(dungeon, 0, 0, 10, 10, pixelsPerCell: 8, fov: fov);

        // Assert
        raster.Should().NotBeNull();
        // TODO: Add assertions checking dimming
    }
}
```

### 8.2 Integration Test

**File**: `dotnet/Dungeon.Tests/DungeonDomainIntegrationTests.cs`

```csharp
using Xunit;
using FluentAssertions;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Control;
using PigeonPea.Dungeon.Rendering;

namespace PigeonPea.Dungeon.Tests;

public class DungeonDomainIntegrationTests
{
    [Fact]
    public void DungeonDomain_FullPipeline_WorksEndToEnd()
    {
        // Generate dungeon
        var generator = new BasicDungeonGenerator();
        var dungeon = generator.Generate(width: 50, height: 50, seed: 42);

        // Verify dungeon has walkable tiles
        int walkableCount = 0;
        for (int y = 0; y < dungeon.Height; y++)
            for (int x = 0; x < dungeon.Width; x++)
                if (dungeon.IsWalkable(x, y))
                    walkableCount++;

        walkableCount.Should().BeGreaterThan(0);

        // Calculate FOV
        var fov = new FovCalculator(dungeon);
        var visible = fov.ComputeVisible(25, 25, range: 8);
        visible.Should().NotBeNull();

        // Render to raster
        var raster = SkiaDungeonRasterizer.Render(dungeon, 0, 0, 30, 30, pixelsPerCell: 16, fov: visible);
        raster.Should().NotBeNull();
        raster.Rgba.Should().NotBeEmpty();

        // Render to ASCII
        var ascii = BrailleDungeonRenderer.RenderAscii(dungeon, 0, 0, 30, 30, fov: visible);
        ascii.Should().NotBeNullOrEmpty();
        ascii.Should().Contain('#'); // Walls
        ascii.Should().Contain('.'); // Floors
    }

    [Fact]
    public void DungeonDomain_Pathfinding_FindsPath()
    {
        // Generate dungeon
        var generator = new BasicDungeonGenerator();
        var dungeon = generator.Generate(width: 40, height: 40, seed: 123);

        // Find two walkable tiles
        (int x, int y) start = (-1, -1);
        (int x, int y) goal = (-1, -1);

        for (int y = 0; y < dungeon.Height && start.x < 0; y++)
            for (int x = 0; x < dungeon.Width && start.x < 0; x++)
                if (dungeon.IsWalkable(x, y))
                    start = (x, y);

        for (int y = dungeon.Height - 1; y >= 0 && goal.x < 0; y--)
            for (int x = dungeon.Width - 1; x >= 0 && goal.x < 0; x--)
                if (dungeon.IsWalkable(x, y))
                    goal = (x, y);

        start.Should().NotBe((-1, -1));
        goal.Should().NotBe((-1, -1));

        // Find path
        var pathfinder = new PathfindingService(dungeon);
        var path = pathfinder.FindPath(start, goal);

        // Assert path exists
        path.Should().NotBeEmpty();
        path[0].Should().Be(start);
        path[^1].Should().Be(goal);
    }
}
```

---

## Task 9: Clean Up Placeholder Files

**Priority**: P2
**Estimated Lines**: N/A
**Dependencies**: None

Delete placeholder `Class1.cs` files:

```bash
rm dotnet/Dungeon/PigeonPea.Dungeon.Core/Class1.cs
rm dotnet/Dungeon/PigeonPea.Dungeon.Control/Class1.cs
rm dotnet/Dungeon/PigeonPea.Dungeon.Rendering/Class1.cs
```

Also clean up the Map domain placeholders:

```bash
rm dotnet/Map/PigeonPea.Map.Core/Class1.cs
rm dotnet/Map/PigeonPea.Map.Control/Class1.cs
rm dotnet/Map/PigeonPea.Map.Rendering/Class1.cs
```

---

## Task 10: Update Project References

**Priority**: P0
**Estimated Lines**: N/A
**Dependencies**: None

Verify all project references are correct:

**Dungeon.Rendering.csproj should reference**:

- `Dungeon.Core`
- `Shared.Rendering`
- `Shared.ECS`
- SkiaSharp (package)

**Console app .csproj should reference**:

- `Dungeon.Core`
- `Dungeon.Control`
- `Dungeon.Rendering`

---

## Success Criteria

### Build & Test

- [ ] `dotnet build dotnet/PigeonPea.sln` succeeds (0 errors, 0 warnings)
- [ ] `dotnet test dotnet/Dungeon.Tests/` passes all tests
- [ ] All placeholder `Class1.cs` files removed

### Functionality

- [ ] Can generate dungeon using `BasicDungeonGenerator`
- [ ] Can calculate FOV using `FovCalculator`
- [ ] Can find path using `PathfindingService`
- [ ] Can render dungeon to RGBA raster with `SkiaDungeonRasterizer`
- [ ] Can render dungeon to ASCII with `BrailleDungeonRenderer`
- [ ] Can render dungeon with FOV dimming applied
- [ ] Can run console dungeon demo: `dotnet run --project dotnet/console-app dungeon-demo`

### Architecture

- [ ] Dungeon.Rendering uses ONLY `Dungeon.Core` types (no GoRogue types leak)
- [ ] Console app references Dungeon.\* but NOT GoRogue directly
- [ ] Proper dependency flow: Console → Rendering → Control → Core → GoRogue (hidden)

### ECS Integration (Optional for Phase 4, Required for Phase 5)

- [ ] Can create dungeon ECS world
- [ ] Can add monster/item entities with Position + Sprite + DungeonEntityTag
- [ ] Entities render correctly with `EntityRenderer`
- [ ] Entities filtered by FOV when rendering

---

## Verification Commands

After implementation, run these commands to verify success:

```bash
# Build solution
dotnet build dotnet/PigeonPea.sln

# Run tests
dotnet test dotnet/Dungeon.Tests/

# Run dungeon demo (if implemented)
dotnet run --project dotnet/console-app dungeon-demo

# Check for GoRogue leaks (should find ONLY in Dungeon.Core and Dungeon.Control)
grep -r "using GoRogue" dotnet/ --include="*.cs" | grep -v "Dungeon.Core" | grep -v "Dungeon.Control"
```

Expected: No GoRogue references outside Dungeon.Core/Control.

---

## Notes for Implementation

1. **Follow Map Domain Pattern**: The Map domain (Phase 3) is now correctly implemented. Use it as a reference:
   - `Map.Rendering/SkiaMapRasterizer.cs` → `Dungeon.Rendering/SkiaDungeonRasterizer.cs`
   - `Map.Rendering/BrailleMapRenderer.cs` → `Dungeon.Rendering/BrailleDungeonRenderer.cs`
   - `Map.Core/Domain/MapColor.cs` → Could create `Dungeon.Core/Domain/TileColors.cs` if needed

2. **Adapter Pattern**: If using GoRogue generators, wrap them in `Adapters/GoRogueAdapter.cs`. The existing `BasicDungeonGenerator` is fine for now.

3. **Testing**: Write tests as you go. The test project already exists with proper references.

4. **Console Integration**: Terminal.Gui is already referenced. The `DungeonPanelView` will work similarly to existing map views.

5. **ECS**: Phase 5 focuses on ECS, but you can start adding entity rendering in Phase 4 (see Task 7).

6. **Incremental Approach**:
   - Start with Task 1 (rendering)
   - Then Task 6 (console integration) to see it working
   - Then Tasks 2, 7 (ECS)
   - Then Task 8 (tests)
   - Finally Tasks 9, 10 (cleanup)

---

## Timeline Estimate

- **Task 1-3**: Rendering (4-6 hours)
- **Task 4-5**: Optional enhancements (2-3 hours)
- **Task 6**: Console integration (2-3 hours)
- **Task 7**: ECS integration (2-3 hours)
- **Task 8**: Tests (2-3 hours)
- **Task 9-10**: Cleanup (1 hour)

**Total**: ~15-20 hours for complete Phase 4 implementation

---

## Questions or Issues?

If you encounter problems:

1. Check the Map domain implementation for reference patterns
2. Verify all project references are correct
3. Ensure GoRogue package is v3.0.0-beta10 (already in .csproj)
4. Run `dotnet build` frequently to catch errors early
5. Write tests to verify functionality as you implement

---

## Final Checklist

Before marking Phase 4 complete:

- [ ] All files in "To Be Implemented" section created
- [ ] Build succeeds (0 errors, 0 warnings)
- [ ] Tests pass
- [ ] Console dungeon demo runs and is playable
- [ ] No GoRogue references leak outside Core/Control
- [ ] Placeholder files removed
- [ ] Code follows existing project patterns (Map domain)
- [ ] README or docs updated if needed

---

**Implementation Status**: Ready to begin
**Next Phase**: Phase 5 (Arch ECS Integration) - see RFC-007 lines 416-447
