# Pigeon Pea - 2D Dungeon Crawler

A multiplatform roguelike dungeon crawler built with modern C# technologies.

## Architecture

### Technology Stack

- **ECS Framework**: Arch (high-performance Entity Component System)
- **Roguelike Algorithms**: GoRogue (FOV, pathfinding, map generation)
- **Windows Renderer**: SkiaSharp via Avalonia
- **Console Renderer**: Terminal graphics (Kitty/Sixel/Braille) + ASCII fallback
- **Windows HUD**: Avalonia UI
- **Console HUD**: Terminal.Gui v2

### Project Structure

```
dotnet/
├── shared-app/          # PigeonPea.Shared - Core game logic
│   ├── GameWorld.cs     # Main game world with ECS
│   ├── Components.cs    # ECS components (Position, Renderable, Health, etc.)
│   ├── Player.cs        # Player entity
│   └── IRenderer.cs     # Platform-agnostic renderer interface
│
├── windows-app/         # PigeonPea.Windows - Windows desktop app
│   ├── Program.cs       # Entry point
│   ├── App.axaml        # Avalonia app definition
│   ├── MainWindow.axaml # Main window with HUD
│   └── GameCanvas.cs    # SkiaSharp-based game renderer
│
└── console-app/         # PigeonPea.Console - Terminal app
    ├── Program.cs       # Entry point
    ├── GameApplication.cs         # Terminal.Gui main app
    ├── GameView.cs               # Game rendering view
    ├── TerminalCapabilities.cs   # Terminal feature detection
    └── ITerminalRenderer.cs      # Terminal graphics renderers
```

## Building & Running

### Prerequisites

- .NET 9.0 SDK
- Windows 10+ (for windows-app)
- Any terminal (for console-app; best with Kitty, WezTerm, or Sixel support)

### Build

```bash
cd dotnet
dotnet restore
dotnet build
```

### Run Windows App

```bash
cd dotnet/windows-app
dotnet run
```

**Controls**:
- Arrow keys / WASD: Move
- ESC: Exit

### Run Console App

```bash
cd dotnet/console-app
dotnet run
```

**Controls**:
- Arrow keys / wasd: Move
- q: Quit

## Features (Planned)

### Shared (Core Game Logic)
- [x] Arch ECS integration
- [x] GoRogue integration
- [ ] Procedural dungeon generation (GoRogue)
- [ ] Field of View (FOV) system
- [ ] Pathfinding for enemies
- [ ] Turn-based combat
- [ ] Inventory system
- [ ] Character progression

### Windows App
- [x] SkiaSharp tile renderer
- [x] Avalonia HUD
- [ ] Sprite/texture atlases
- [ ] Particle effects
- [ ] Animated tiles
- [ ] Mouse controls

### Console App
- [x] Terminal.Gui HUD
- [x] Terminal capability detection
- [ ] Kitty Graphics Protocol renderer
- [ ] Sixel graphics renderer
- [ ] Unicode Braille high-res renderer
- [ ] ASCII fallback renderer
- [ ] Color gradient effects

## Development

### Adding New Components

1. Define component struct in `shared-app/Components.cs`
2. Create entities with components in `GameWorld.cs`
3. Query components in rendering logic (Windows: `GameCanvas.cs`, Console: `GameView.cs`)

### Adding New Systems

Create system methods in `GameWorld.cs` and call them in `Update()`:

```csharp
public void Update(double deltaTime)
{
    UpdateMovement();
    UpdateFieldOfView();
    UpdateCombat();
}
```

### Implementing Renderers

**Windows (SkiaSharp)**: Modify `GameCanvas.cs` `RenderGame()` method.

**Console (Terminal)**: Implement `ITerminalRenderer` interface in `ITerminalRenderer.cs`.

## References

- [Arch ECS](https://github.com/genaray/Arch)
- [GoRogue](https://github.com/Chris3606/GoRogue)
- [Avalonia UI](https://avaloniaui.net/)
- [SkiaSharp](https://github.com/mono/SkiaSharp)
- [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui)
- [Kitty Graphics Protocol](https://sw.kovidgoyal.net/kitty/graphics-protocol/)
- [Sixel Graphics](https://en.wikipedia.org/wiki/Sixel)

## License

MIT
