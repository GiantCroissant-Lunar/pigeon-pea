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
├── engine/                               # Engine-level shared libraries (no game/content deps)
│   ├── core/
│   │   └── src/
│   │       ├── PigeonPea.Shared.ECS/     # ECS foundation built on Arch
│   │       └── PigeonPea.Shared.Rendering/ # Rendering primitives (tiles, viewports, targets, text)
│
├── app-essential/
│   ├── core/
│   │   ├── src/
│   │   │   ├── PigeonPea.Contracts/       # Plugin, DI, services contracts
│   │   │   └── PigeonPea.PluginSystem/    # Plugin loader + registry + EventBus
│   │   └── tests/                         # App framework tests
│   └── plugins/                           # App-level plugins (future)
│
├── game-essential/
│   ├── core/
│   │   ├── src/
│   │   │   ├── PigeonPea.Game.Contracts/  # Game events, services, components
│   │   │   └── PigeonPea.Shared/          # Core game logic built on Shared.ECS/Rendering
│   │   └── tests/
│   │       └── PigeonPea.Shared.Tests/    # Game framework tests
│   └── plugins/                           # Game feature plugins (future)
│
├── windows-app/
│   ├── core/
│   │   ├── src/
│   │   │   └── PigeonPea.Windows/         # Windows desktop app
│   │   └── tests/
│   │       └── PigeonPea.Windows.Tests/   # Windows app tests
│   ├── plugins/                           # Windows-specific plugins (future)
│   └── configs/                           # Plugin manifests and configs (future)
│
└── console-app/
    ├── core/
    │   ├── src/
    │   │   └── PigeonPea.Console/         # Terminal app
    │   └── tests/
    │       └── PigeonPea.Console.Tests/   # Console app tests
    ├── plugins/                           # Terminal renderers/plugins (future)
    └── configs/                           # Plugin manifests and configs (future)
```

## Building & Running

### Prerequisites

- .NET SDK 9.0.x
- macOS, Windows, or Linux with a compatible terminal
- dotnet CLI in PATH (verify with `dotnet --info`)
- Optional for Windows app:
  - .NET Desktop Runtime 9.0
  - A GPU driver compatible with SkiaSharp via Avalonia
- Recommended IDEs: VS Code with C# Dev Kit or JetBrains Rider

### Run Windows App

```bash
cd dotnet/windows-app/core/src/PigeonPea.Windows
dotnet run
```

**Controls**:

- Arrow keys / WASD: Move
- ESC: Exit

### Run Console App

```bash
cd dotnet/console-app/core/src/PigeonPea.Console
dotnet run
```

**Controls**:

- Arrow keys / WASD: Move
- ESC/Q: Exit
- Space/Enter: Interact
- H: Toggle help

## Development

### Adding New Components

1. Define component struct in `game-essential/core/PigeonPea.Shared/Components.cs`
2. Create entities with components in `GameWorld.cs`
3. Query components in rendering logic (Windows: `windows-app/core/src/PigeonPea.Windows/GameCanvas.cs`, Console: `console-app/core/src/PigeonPea.Console/GameView.cs`)

### Adding New Systems

Create a system method in `game-essential/core/PigeonPea.Shared/GameWorld.cs` and call it from `Update()`:

```csharp
public partial class GameWorld
{
    // Example: apply regen to all entities with Health
    private void HealthRegenSystem(float dt)
    {
        foreach (ref var health in _healthQuery)
        {
            health.Value = Math.Min(health.Max, health.Value + health.RegenRate * dt);
        }
    }

    public void Update(float dt)
    {
        // Existing systems...
        MovementSystem(dt);
        CombatSystem(dt);
        // New system
        HealthRegenSystem(dt);
    }
}
```

### Implementing Renderers

**Windows (SkiaSharp)**: Modify `windows-app/core/PigeonPea.Windows/GameCanvas.cs` `RenderGame()` method.

**Console (Terminal)**: Implement `ITerminalRenderer` interface in `console-app/core/src/PigeonPea.Console/ITerminalRenderer.cs`.

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
