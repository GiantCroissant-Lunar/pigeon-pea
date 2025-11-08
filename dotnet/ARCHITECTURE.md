# Architecture Overview

## Design Philosophy

Pigeon Pea is designed with **platform-agnostic game logic** at its core, with platform-specific rendering layers on top.

```
┌─────────────────────────────────────────────────────┐
│                  Platform Layer                     │
│  ┌──────────────────┐      ┌───────────────────┐  │
│  │  Windows App     │      │   Console App     │  │
│  │  (Avalonia +     │      │   (Terminal.Gui + │  │
│  │   SkiaSharp)     │      │    Kitty/Sixel)   │  │
│  └──────────────────┘      └───────────────────┘  │
└─────────────────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────┐
│              Shared Game Logic                      │
│  ┌──────────────────────────────────────────────┐  │
│  │ Arch ECS (Entities & Components)             │  │
│  │ - Position, Renderable, Health, FOV          │  │
│  │                                               │  │
│  │ GoRogue (Roguelike Algorithms)               │  │
│  │ - Field of View, Pathfinding, Map Gen        │  │
│  │                                               │  │
│  │ Game World & Systems                         │  │
│  │ - Player, Monsters, Items, Map               │  │
│  └──────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

## Component-Based Design

### Core ECS Components

All game entities are composed of components:

| Component         | Purpose               | Fields                       |
| ----------------- | --------------------- | ---------------------------- |
| `Position`        | Grid location         | `Point Point`                |
| `Renderable`      | Visual representation | `char Glyph, Color FG/BG`    |
| `Health`          | Hit points            | `int Current, int Maximum`   |
| `FieldOfView`     | Visibility tracking   | `int Radius, HashSet<Point>` |
| `PlayerComponent` | Marks player entity   | `string Name`                |

### Entity Creation Pattern

```csharp
// Create player
var player = world.Create(
    new Position(startX, startY),
    new Renderable('@', Color.Yellow),
    new PlayerComponent { Name = "Hero" },
    new Health { Current = 100, Maximum = 100 },
    new FieldOfView(8)
);

// Create monster
var goblin = world.Create(
    new Position(x, y),
    new Renderable('g', Color.Green),
    new Health { Current = 20, Maximum = 20 },
    new AIComponent { Behavior = AIBehavior.Aggressive }
);
```

## Rendering Pipeline

### Windows App (SkiaSharp)

```
MainWindow.axaml
    ↓
GameCanvas (SKCanvas)
    ↓
Query ECS for (Position, Renderable)
    ↓
SKCanvas.DrawText(glyph, x*tileSize, y*tileSize)
```

**Advantages**:

- Hardware-accelerated
- Smooth animations
- Sprite/texture support
- Rich visual effects

### Console App (Terminal Graphics)

```
GameApplication (Terminal.Gui)
    ↓
GameView (Custom View)
    ↓
Terminal Capability Detection
    ↓
┌─────────────────────────┐
│ Kitty Graphics Protocol │ (best: full pixel graphics)
│ Sixel Protocol          │ (good: pixel graphics)
│ Unicode Braille         │ (ok: 2x4 dot matrix)
│ ASCII Fallback          │ (basic: single chars)
└─────────────────────────┘
```

**Advantages**:

- Runs over SSH
- Low bandwidth
- Lightweight
- Accessible

## GoRogue Integration

### Field of View

```csharp
// In GameWorld.cs
var fov = new RecursiveShadowcastingFOV(transparencyView);
var player = GetPlayer();
var visibleCells = fov.CalculateFOV(player.Position, player.FOVRadius);
```

### Pathfinding

```csharp
var pathfinder = new AStar(walkabilityMap, Distance.Chebyshev);
var path = pathfinder.ShortestPath(start, goal);
```

### Map Generation

```csharp
var mapGen = new Generator(width, height)
    .ConfigAndGenerateSafe(gen =>
    {
        gen.AddSteps(DefaultAlgorithms.RectangleMapSteps());
    });
var map = mapGen.Context.GetFirst<ISettableGridView<bool>>("WallFloor");
```

## Data Flow

### Input → Update → Render Loop

```
User Input (Keyboard/Mouse)
    ↓
Platform App (MainWindow / GameApplication)
    ↓
Player.Move(direction)  ← Updates ECS component
    ↓
GameWorld.Update(deltaTime)
    ├─ Update Movement
    ├─ Update FOV
    ├─ Update AI
    ├─ Update Combat
    └─ Update Systems
    ↓
Renderer.Render()  ← Queries ECS for drawable entities
    ↓
Screen Update
```

## Future Extensions

### Adding New Platforms

To add a new platform (e.g., mobile, web):

1. Create new project referencing `PigeonPea.Shared`
2. Implement platform-specific renderer
3. Wire up input handling
4. Call `GameWorld.Update()` in game loop

### Adding New Game Features

1. **Define component** in `Components.cs`
2. **Create system** in `GameWorld.cs`
3. **Update renderer** if new visuals needed
4. No platform-specific code required!

## Performance Considerations

- **Arch ECS**: Zero-allocation queries, cache-friendly memory layout
- **GoRogue**: Optimized algorithms (recursive shadowcasting, A\*, etc.)
- **SkiaSharp**: Hardware-accelerated GPU rendering
- **Terminal.Gui**: Minimal redraws, efficient text rendering

## Dependencies Graph

```
PigeonPea.Windows ──┐
                    ├──→ PigeonPea.Shared ──→ Arch + GoRogue
PigeonPea.Console ──┘
```

No circular dependencies, clean separation of concerns.
