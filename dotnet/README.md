# Pigeon Pea - 2D Dungeon Crawler

A multiplatform roguelike dungeon crawler built with modern C# technologies.

## Architecture

### Technology Stack

- **ECS Framework**: Arch (high-performance Entity Component System)
- **Roguelike Algorithms**: GoRogue (FOV, pathfinding, map generation)
- **Windows Renderer**: SkiaSharp via Avalonia
- **Console Renderer**: Terminal graphics (Kitty/Sixel/Braille) + ASCII fallback
- **Windows HUD**: Avalonia UI

### Project Structure

```
dotnet/
├── app-essential/
│   ├── core/                  # App framework core (future)
│   └── plugins/               # App-level plugins (future)
│
├── game-essential/
│   ├── core/
│   │   └── PigeonPea.Shared/  # Core game logic (ECS + GoRogue)
│   └── plugins/               # Game feature plugins (future)
│
├── windows-app/
│   ├── core/
│   │   └── PigeonPea.Windows/ # Windows desktop app
│   ├── plugins/               # Windows-specific plugins (future)
│   └── configs/               # Plugin manifests and configs (future)
│
└── console-app/
    ├── core/
    │   └── PigeonPea.Console/ # Terminal app
    ├── plugins/               # Terminal renderers/plugins (future)
    └── configs/               # Plugin manifests and configs (future)
```

## Building & Running

### Prerequisites
{{ ... }}

### Run Windows App

```bash
cd dotnet/windows-app/core/PigeonPea.Windows
dotnet run
```

**Controls**:

- Arrow keys / WASD: Move
- ESC: Exit

### Run Console App

```bash
cd dotnet/console-app/core/PigeonPea.Console
dotnet run
```

**Controls**:

{{ ... }}

## Development

### Adding New Components

1. Define component struct in `game-essential/core/PigeonPea.Shared/Components.cs`
2. Create entities with components in `GameWorld.cs`
3. Query components in rendering logic (Windows: `windows-app/core/PigeonPea.Windows/GameCanvas.cs`, Console: `console-app/core/PigeonPea.Console/GameView.cs`)

### Adding New Systems

Create system methods in `GameWorld.cs` and call them in `Update()`:

{{ ... }}
}
```

### Implementing Renderers

**Windows (SkiaSharp)**: Modify `windows-app/core/PigeonPea.Windows/GameCanvas.cs` `RenderGame()` method.

**Console (Terminal)**: Implement `ITerminalRenderer` interface in `console-app/core/PigeonPea.Console/ITerminalRenderer.cs`.

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
