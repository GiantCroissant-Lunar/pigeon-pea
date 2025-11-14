# Temporary Commit Plan (Safe to delete after use)

## Commit 1 – Docs & changelog for RFC-007 / architecture

- **Message**: `docs: document domain-driven architecture and RFC-007`
- **Key files**:
  - `CHANGELOG.md`
  - `CLAUDE.md`
  - `README.md`
  - `docs/README.md`
  - `docs/architecture/ARCHITECTURE_MAP_RENDERING.md`
  - `docs/architecture/ARCHITECTURE_PLAN.md`
  - `docs/examples/ecs-usage.md`
  - `docs/architecture/domain-organization.md`
  - `docs/migrations/sharedapp-to-domains.md`

Suggested command:

```bash
git add CHANGELOG.md CLAUDE.md README.md \
  docs/README.md \
  docs/architecture/ARCHITECTURE_MAP_RENDERING.md \
  docs/architecture/ARCHITECTURE_PLAN.md \
  docs/examples/ecs-usage.md \
  docs/architecture/domain-organization.md \
  docs/migrations/sharedapp-to-domains.md
# pre-commit run --all-files (optional but recommended)
git commit -m "docs: document domain-driven architecture and RFC-007"
```

---

## Commit 2 – RFC docs, issues, devtools, determinism workflow

- **Message**: `docs: add RFC plans, issue templates, and determinism reference`
- **Key files**:
  - `.github/issue-templates/**`
  - `docs/GITHUB_ISSUES.md`
  - `docs/GITHUB_ISSUES_DEVTOOLS.md`
  - `docs/issues/**`
  - `docs/rfcs/README.md`
  - `docs/rfcs/010-color-scheme-configuration-*.md`
  - `docs/rfcs/013-yazi-integrated-rust-cli.md`
  - `docs/determinism-reference.md`
  - `.github/workflows/determinism-tests.yml`

Suggested command (example):

```bash
git add .github/issue-templates \
  docs/GITHUB_ISSUES.md docs/GITHUB_ISSUES_DEVTOOLS.md \
  docs/issues \
  docs/rfcs/README.md docs/rfcs/010-color-scheme-configuration-*.md \
  docs/rfcs/013-yazi-integrated-rust-cli.md \
  docs/determinism-reference.md \
  .github/workflows/determinism-tests.yml
# pre-commit run --all-files
git commit -m "docs: add RFC plans, issue templates, and determinism reference"
```

---

## Commit 3 – Rendering/domain refactor (Map, Dungeon, Shared)

- **Message**: `refactor: split rendering into map, dungeon, and shared domains`
- **Key files (examples)**:
  - `dotnet/Dungeon/PigeonPea.Dungeon.Control/PathfindingService.cs`
  - `dotnet/Dungeon/PigeonPea.Dungeon.Control/PigeonPea.Dungeon.Control.csproj`
  - `dotnet/Dungeon/PigeonPea.Dungeon.Core/DungeonData.cs`
  - `dotnet/Dungeon/PigeonPea.Dungeon.Rendering/BrailleDungeonRenderer.cs`
  - `dotnet/Dungeon/PigeonPea.Dungeon.Rendering/EntityRenderer.cs`
  - `dotnet/Dungeon/PigeonPea.Dungeon.Rendering/FovRenderer.cs`
  - `dotnet/Dungeon/PigeonPea.Dungeon.Rendering/PigeonPea.Dungeon.Rendering.csproj`
  - `dotnet/Map/PigeonPea.Map.Core/Domain/MapColor.cs` (deleted)
  - `dotnet/Map/PigeonPea.Map.Core/Domain/ColorScheme.cs`
  - `dotnet/Map/PigeonPea.Map.Rendering/BrailleMapRenderer.cs`
  - `dotnet/Map/PigeonPea.Map.Rendering/SkiaMapRasterizer.cs`
  - `dotnet/Map/PigeonPea.Map.Rendering/Tiles/MapTileSource.cs`
  - `dotnet/Map/PigeonPea.Map.Rendering/ColorSchemes.cs`
  - `dotnet/Map/PigeonPea.Map.Rendering/MapColor.cs`
  - `dotnet/Shared/PigeonPea.Shared.Rendering/Tile.cs`
  - `dotnet/Shared/PigeonPea.Shared.Rendering/TileFlags.cs`
  - `dotnet/shared-app/Rendering/**`
  - `dotnet/shared-app/Performance/FrameRateMetrics.cs`
  - `dotnet/shared-app/PigeonPea.Shared.csproj`
  - `dotnet/shared-app/ViewModels/MapViewModel.cs`
  - `dotnet/shared-app/ViewModels/MapRenderViewModel.cs` (deleted)

Suggested command (adjust as needed):

```bash
git add dotnet/Dungeon/PigeonPea.Dungeon.Control/PathfindingService.cs \
  dotnet/Dungeon/PigeonPea.Dungeon.Control/PigeonPea.Dungeon.Control.csproj \
  dotnet/Dungeon/PigeonPea.Dungeon.Core/DungeonData.cs \
  dotnet/Dungeon/PigeonPea.Dungeon.Rendering/BrailleDungeonRenderer.cs \
  dotnet/Dungeon/PigeonPea.Dungeon.Rendering/EntityRenderer.cs \
  dotnet/Dungeon/PigeonPea.Dungeon.Rendering/FovRenderer.cs \
  dotnet/Dungeon/PigeonPea.Dungeon.Rendering/PigeonPea.Dungeon.Rendering.csproj \
  dotnet/Map/PigeonPea.Map.Core/Domain/MapColor.cs \
  dotnet/Map/PigeonPea.Map.Core/Domain/ColorScheme.cs \
  dotnet/Map/PigeonPea.Map.Rendering/BrailleMapRenderer.cs \
  dotnet/Map/PigeonPea.Map.Rendering/SkiaMapRasterizer.cs \
  dotnet/Map/PigeonPea.Map.Rendering/Tiles/MapTileSource.cs \
  dotnet/Map/PigeonPea.Map.Rendering/ColorSchemes.cs \
  dotnet/Map/PigeonPea.Map.Rendering/MapColor.cs \
  dotnet/Shared/PigeonPea.Shared.Rendering \
  dotnet/shared-app/Rendering \
  dotnet/shared-app/Performance/FrameRateMetrics.cs \
  dotnet/shared-app/PigeonPea.Shared.csproj \
  dotnet/shared-app/ViewModels/MapViewModel.cs \
  dotnet/shared-app/ViewModels/MapRenderViewModel.cs
# pre-commit run --all-files
git commit -m "refactor: split rendering into map, dungeon, and shared domains"
```

---

## Commit 4 – Console app and tests for new rendering

- **Message**: `test: update console and shared-app tests for new rendering pipeline`
- **Key files**:
  - `dotnet/console-app/ConsoleMapDemoRunner.cs`
  - `dotnet/console-app/PigeonPea.Console.csproj`
  - `dotnet/console-app/Rendering/BrailleRenderer.cs`
  - `dotnet/console-app/TerminalHudApplication.cs`
  - `dotnet/console-app/Views/BrailleMapPanelView.cs`
  - `dotnet/console-app/Views/PixelMapPanelView.cs`
  - `dotnet/console-app.Tests/PigeonPea.Console.Tests.csproj`
  - `dotnet/console-app.Tests/Rendering/AsciiRendererTests.cs`
  - `dotnet/console-app.Tests/Rendering/BrailleRendererTests.cs`
  - `dotnet/console-app.Tests/Rendering/KittyGraphicsRendererTests.cs`
  - `dotnet/console-app.Tests/Rendering/SixelRendererTests.cs`
  - `dotnet/console-app.Tests/Visual/SnapshotTests.cs`
  - `dotnet/shared-app.Tests/Performance/FrameRateMetricsTests.cs`
  - `dotnet/shared-app.Tests/PigeonPea.Shared.Tests.csproj`

Suggested command:

```bash
git add dotnet/console-app/ConsoleMapDemoRunner.cs \
  dotnet/console-app/PigeonPea.Console.csproj \
  dotnet/console-app/Rendering/BrailleRenderer.cs \
  dotnet/console-app/TerminalHudApplication.cs \
  dotnet/console-app/Views/BrailleMapPanelView.cs \
  dotnet/console-app/Views/PixelMapPanelView.cs \
  dotnet/console-app.Tests/PigeonPea.Console.Tests.csproj \
  dotnet/console-app.Tests/Rendering/AsciiRendererTests.cs \
  dotnet/console-app.Tests/Rendering/BrailleRendererTests.cs \
  dotnet/console-app.Tests/Rendering/KittyGraphicsRendererTests.cs \
  dotnet/console-app.Tests/Rendering/SixelRendererTests.cs \
  dotnet/console-app.Tests/Visual/SnapshotTests.cs \
  dotnet/shared-app.Tests/Performance/FrameRateMetricsTests.cs \
  dotnet/shared-app.Tests/PigeonPea.Shared.Tests.csproj
# pre-commit run --all-files
git commit -m "test: update console and shared-app tests for new rendering pipeline"
```

---

## Commit 5 – Benchmarks + .gitignore

- **Message**: `perf: add BenchmarkDotNet harness and performance baseline docs`
- **Key files**:
  - `.gitignore` (BenchmarkDotNet artifacts, Results, \*.diagsession)
  - `benchmarks/**` (if you choose to commit this)
  - `docs/performance-baseline.md`

Suggested command (if `benchmarks/` is committed):

```bash
git add .gitignore benchmarks docs/performance-baseline.md
# pre-commit run --all-files
git commit -m "perf: add BenchmarkDotNet harness and performance baseline docs"
```

---

## Open Decisions / TODOs

- **PR151.html / PR151_reviews2.json**
  - Decide whether to:
    - Delete them, or
    - Add ignore patterns to `.gitignore`, or
    - Move under `docs/issues/` and commit as references.

- **benchmarks/**
  - Decide if the harness should be permanently tracked. If yes, include in Commit 5.

You can edit this file freely while committing and delete it once the repo is clean.
