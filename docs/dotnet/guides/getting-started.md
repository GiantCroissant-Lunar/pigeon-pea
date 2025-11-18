---
doc_id: 'GUIDE-2025-00001'
title: 'Getting Started with Pigeon Pea .NET'
doc_type: 'guide'
status: 'active'
canonical: true
created: '2025-11-18'
tags: ['dotnet', 'getting-started', 'setup', 'development']
summary: 'Comprehensive guide to setting up and running Pigeon Pea .NET applications'
related: ['ADR-2025-00001', 'ADR-2025-00003', 'ADR-2025-00004', 'REFERENCE-2025-00001', 'GUIDE-2025-00002']
---

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

## Related Documents

### Architecture Documentation
- **[Architecture Overview](../architecture/overview.md)** - High-level system design and ECS patterns
- **[Service Tiers](../architecture/service-tiers.md)** - Four-tier architecture understanding
- **[Services and Plugins](../architecture/services-and-plugins.md)** - Plugin system for extensibility

### Technical Implementation
- **[Game Scale Modes](../architecture/game-scale-modes.md)** - Multi-scale world considerations
- **[Observable Collections](../architecture/observable-collections.md)** - Reactive patterns for UI
- **[Reactive Stack and R3](../architecture/reactive-stack-and-r3.md)** - Reactive extensions integration
- **[GOAP Perception Checklist](../architecture/goap-perception-checklist.md)** - AI system implementation

### Navigation and Reference
- **[.NET Documentation Reference](../README.md)** - Comprehensive index of all .NET documentation
- **[Navigation Guide](../NAVIGATION.md)** - Role-based navigation and learning paths

### Main Project Documentation
- **[Main Documentation Index](../../README.md)** - Project-wide documentation
- **[RFC-005: Project Structure Reorganization](../../rfcs/005-project-structure-reorganization.md)** - Foundation for current structure
- **[RFC-006: Plugin System Architecture](../../rfcs/006-plugin-system-architecture.md)** - Plugin system design

### External Resources
- **[Arch ECS](https://github.com/genaray/Arch)** - Entity Component System framework
- **[GoRogue](https://github.com/Chris3606/GoRogue)** - Roguelike algorithms library
- **[Avalonia UI](https://avaloniaui.net/)** - Cross-platform UI framework
- **[SkiaSharp](https://github.com/mono/SkiaSharp)** - 2D graphics library
- **[Terminal.Gui](https://github.com/gui-cs/Terminal.Gui)** - Terminal UI framework