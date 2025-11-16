# Engine tier (.NET)

This folder contains **engine-level shared libraries** that can be referenced from multiple tiers (game-essential, app-essential, console-app, windows-app, and content-authoring):

- `core/src/PigeonPea.Shared.ECS` – ECS foundation built on top of Arch.
- `core/src/PigeonPea.Shared.Rendering` – rendering primitives (tiles, viewports, render targets, capabilities, text primitives).

These projects are intentionally **generic and reusable**:

- They must **not depend on game/content-specific projects**.
- Game-essential projects (e.g. `PigeonPea.Shared`, `PigeonPea.Map.*`, `PigeonPea.Dungeon.*`) and app shells (console/windows) consume them.

For more detail see:

- `docs/adr/ADR-0002-rendering-split-shared-vs-content-rendering.md`
- `docs/adr/ADR-0003-ecs-split-shared-vs-content-ecs.md`
