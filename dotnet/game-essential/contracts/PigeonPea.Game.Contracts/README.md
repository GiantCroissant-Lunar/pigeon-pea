# PigeonPea.Game.Contracts

Game-specific contracts for plugins and host integration.

- Target framework: netstandard2.1
- Purpose: Define contracts for game systems consumable by plugins
- Contains:
  - `Rendering/IRenderer.cs` and `Rendering/RenderingCapabilities.cs`
  - `Rendering/RenderContext.cs`
  - `Events/` (to be populated in later issues)
  - `Services/` (reserved)
  - `Components/` (reserved)

No implementation code should live here. These are stable API surfaces.
