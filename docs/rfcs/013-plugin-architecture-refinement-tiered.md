---
canonical: true
created: '2025-11-19'
doc_id: RFC-00013
doc_type: rfc
related:
- RFC-00006
- RFC-00014
status: draft
summary: Refine plugin architecture to follow tier-based system (Tier 1-4) with proper
  separation between domain plugins and platform plugins, eliminating wrapper projects
  and establishing correct dependency flow
supersedes: []
tags:
- plugins
- architecture
- refactoring
- tiered-architecture
- alc
- dungeon
- rendering
title: 'Plugin Architecture Refinement: Tier-Based System'
updated: '2025-11-19'
---


# RFC-013: Plugin Architecture Refinement: Tier-Based System

- **Status:** Draft
- **Author:** Claude Agent (Architecture Review)
- **Date:** 2025-11-19
- **Supersedes:** N/A
- **Related:** RFC-006 (Plugin System Architecture), RFC-014 (Scene Management with ECS)

## Summary

Refine the plugin architecture to correctly implement a tier-based system (Tier 1-4) with clear separation between:

- **Domain plugins** (know WHAT to render/do)
- **Platform plugins** (know HOW to render/execute)

This eliminates unnecessary wrapper projects, fixes Assembly Load Context (ALC) issues, and establishes the correct architectural flow for the pigeon-pea project.

## Motivation

### Current Problems

1. **Wrapper Projects Breaking Tiered Architecture**
   - `PigeonPea.Dungeon.Core` wraps `modern-edgar-dotnet` unnecessarily
   - `PigeonPea.Dungeon.Rendering` contains domain utilities in wrong layer
   - `PigeonPea.Dungeon.Control` - purpose unclear, pre-plugin artifact
   - These should be plugins (Tier 3/4), not domain layer projects

2. **Plugin Contract Violations**
   - `ANSIRenderer` plugin depends on `Dungeon.Core` AND `Dungeon.Rendering`
   - Plugins converting `DungeonView` → `DungeonData` → back to rendering
   - Creates ALC type identity issues
   - Violates "contracts only" principle

3. **God Object Assembly**
   - `PigeonPea.Shared` has 11+ dependencies
   - Mixes dungeon-specific, map-specific, and truly shared code
   - Violates layering architecture

4. **Dual Rendering Contracts**
   - `PigeonPea.Shared.Rendering.IRenderer` (low-level)
   - `PigeonPea.Game.Contracts.Rendering.IRenderer` (high-level)
   - Created ad-hoc during experimentation
   - Need unified contract

5. **Hardcoded ALC Whitelist**
   - `PluginLoadContext` has hardcoded assembly names
   - Should be configuration-driven

### Goals

1. **Establish Tier-Based Architecture**
   - Tier 1: Service Interfaces (Contracts)
   - Tier 2: Proxy Services (source-gen)
   - Tier 3: Real Services (plugins, implement Tier 1)
   - Tier 4: Providers (plugins, selected by Tier 3)

2. **Separate Domain and Platform Concerns**
   - Domain plugins: Dungeon generation, dungeon rendering logic
   - Platform plugins: ANSI, Braille, SkiaSharp (generic renderers)
   - Domain plugins USE platform plugins via contracts

3. **Eliminate Wrappers**
   - Plugins use external libraries directly (`modern-edgar-dotnet`)
   - No intermediate wrapper layers

4. **Fix ALC Issues**
   - Contracts shared across ALC boundary
   - Implementations isolated per plugin
   - Config-driven sharing policy

5. **Proper Layering**
   - app-essential: Non-gameplay (Input, Resource, Analysis)
   - game-essential: Gameplay (contracts only)
   - project: Platform-specific implementations
   - content-authoring: Game data/content

## Architecture Overview

### Tier-Based System

```
┌─────────────────────────────────────────────────────────┐
│ Tier 1: Contracts (Interfaces)                          │
├─────────────────────────────────────────────────────────┤
│ PigeonPea.Dungeon.Contracts/                            │
│   ├─ IDungeonGenerator.cs                               │
│   ├─ Models/DungeonView.cs (DTO)                        │
│                                                          │
│ PigeonPea.Rendering.Contracts/                          │
│   ├─ IRenderer.cs                                       │
│   ├─ Tile.cs, Color.cs (primitives)                     │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ Tier 2: Proxy Services (source-gen)                     │
├─────────────────────────────────────────────────────────┤
│ Generated proxies for service selection/routing         │
│ (Future: Registry-based selection logic)                │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ Tier 3: Real Services (Plugins)                         │
├─────────────────────────────────────────────────────────┤
│ DOMAIN PLUGINS (know WHAT):                             │
│   PigeonPea.Plugin.Dungeon.ModernEdgar/                 │
│   PigeonPea.Plugin.Dungeon.Rendering/                   │
│                                                          │
│ PLATFORM PLUGINS (know HOW):                            │
│   PigeonPea.Plugin.Rendering.Terminal.ANSI/             │
│   PigeonPea.Plugin.Rendering.Terminal.Braille/          │
│   PigeonPea.Plugin.Rendering.Windows.SkiaSharp/         │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ Tier 4: Providers (Selected by Tier 3)                  │
├─────────────────────────────────────────────────────────┤
│ Alternative implementations selected via registry       │
│ Example: Multiple dungeon generators (Edgar, BSP, etc.) │
└─────────────────────────────────────────────────────────┘
```

### Dependency Rules

**Allowed:**

- Tier 2 → Tier 1 ✅
- Tier 3 → Tier 1 ✅
- Tier 4 → Tier 1, Tier 3 ✅
- game-essential → app-essential ✅
- project → app-essential, game-essential ✅

**Not Allowed:**

- Tier 1 → Any other tier ❌
- Tier 3 → Tier 2 ❌ (except via registry)
- Tier 4 → Tier 2 ❌
- Lower tier → Higher tier ❌

### Double-Plugin Architecture

```
┌─────────────────────────────────────────────────────────┐
│ DOMAIN PLUGIN: Dungeon Rendering                        │
│ (Knows WHAT to render - dungeon cells, doors, walls)    │
├─────────────────────────────────────────────────────────┤
│ PigeonPea.Plugin.Dungeon.Rendering                      │
│   └─ DungeonRenderer.Render(DungeonView, playerPos)     │
│       └─ Calls: IRenderer.DrawTile(x, y, tile)          │
│                                                          │
│ Dependencies:                                            │
│   - PigeonPea.Dungeon.Contracts (Tier 1)                │
│   - PigeonPea.Rendering.Contracts (Tier 1)              │
│   - NO domain implementation dependencies               │
└─────────────────────────────────────────────────────────┘
                        ↓ uses
┌─────────────────────────────────────────────────────────┐
│ RENDERING CONTRACT (Tier 1)                             │
├─────────────────────────────────────────────────────────┤
│ PigeonPea.Rendering.Contracts.IRenderer                 │
│   └─ DrawTile(x, y, tile)                               │
│   └─ DrawText(x, y, text, fg, bg)                       │
│   └─ Clear(), BeginFrame(), EndFrame()                  │
└─────────────────────────────────────────────────────────┘
                        ↑ implements
┌─────────────────────────────────────────────────────────┐
│ PLATFORM PLUGINS: Rendering Implementations             │
│ (Know HOW to render - platform-specific)                │
├─────────────────────────────────────────────────────────┤
│ PigeonPea.Plugin.Rendering.Terminal.ANSI                │
│   └─ Implements IRenderer with ANSI escape codes        │
│                                                          │
│ PigeonPea.Plugin.Rendering.Terminal.Braille             │
│   └─ Implements IRenderer with Braille characters       │
│                                                          │
│ PigeonPea.Plugin.Rendering.Windows.SkiaSharp            │
│   └─ Implements IRenderer with SkiaSharp                │
│                                                          │
│ Dependencies:                                            │
│   - PigeonPea.Rendering.Contracts (Tier 1) ONLY         │
│   - NO dungeon-specific dependencies                    │
└─────────────────────────────────────────────────────────┘
```

**Key Principle:** Domain plugins call IRenderer methods, platform plugins implement IRenderer. They don't know about each other!

## Detailed Design

### Phase 1: Unify Rendering Contracts

**Goal:** One rendering service contract

**Actions:**

1. **Decide on unified location:**

   ```
   Recommendation: dotnet/game-essential/core/src/PigeonPea.Rendering.Contracts/

   Alternative: Rename PigeonPea.Shared.Rendering → PigeonPea.Rendering.Contracts
   ```

2. **Merge IRenderer interfaces:**

   ```csharp
   // Unified interface
   namespace PigeonPea.Rendering.Contracts;

   public interface IRenderer
   {
       // Core rendering primitives
       void BeginFrame();
       void EndFrame();
       void Clear(Color color);
       void SetViewport(Viewport viewport);

       // Drawing operations
       void DrawTile(int x, int y, Tile tile);
       void DrawText(int x, int y, string text, Color foreground, Color background);

       // Capabilities
       RendererCapabilities Capabilities { get; }
   }
   ```

3. **Remove high-level GameState rendering:**
   - Domain plugins should NOT receive `GameState`
   - They query services and call `IRenderer.DrawTile()` directly

### Phase 2: Remove Wrapper Projects

**Projects to DELETE:**

```
❌ dotnet/game-essential/core/src/PigeonPea.Dungeon.Core/
❌ dotnet/game-essential/core/src/PigeonPea.Dungeon.Rendering/
❌ dotnet/game-essential/core/src/PigeonPea.Dungeon.Control/
```

**Projects to KEEP:**

```
✅ dotnet/game-essential/core/src/PigeonPea.Dungeon.Contracts/
   └─ Tier 1 contracts only
```

**Impact Analysis:**

- Find all projects referencing deleted ones
- Update or remove references
- Move any useful utilities to appropriate locations

### Phase 3: Create Dungeon Generation Plugin

**New project:**

```
projects/dungeon/dotnet/console-app/plugins/PigeonPea.Plugin.Dungeon.ModernEdgar/
```

**Structure:**

```
PigeonPea.Plugin.Dungeon.ModernEdgar/
├─ ModernEdgarDungeonGenerator.cs
├─ plugin.json
└─ PigeonPea.Plugin.Dungeon.ModernEdgar.csproj
```

**Dependencies:**

```xml
<ProjectReference Include="dotnet/game-essential/.../PigeonPea.Dungeon.Contracts" />
<!-- Use modern-edgar-dotnet from _lib directly, NO wrapper -->
<ProjectReference Include="dotnet/_lib/modern-edgar-dotnet/.../Edgar.Core.csproj" />
```

**Implementation:**

```csharp
public class ModernEdgarDungeonGenerator : IDungeonGenerator
{
    public DungeonView Generate(DungeonGenerationOptions options)
    {
        // Use modern-edgar-dotnet DIRECTLY
        var edgarConfig = new EdgarConfiguration { ... };
        var edgarResult = EdgarGenerator.Generate(edgarConfig);

        // Convert Edgar result → DungeonView
        return ConvertToDungeonView(edgarResult);
    }

    private DungeonView ConvertToDungeonView(EdgarResult result)
    {
        var view = new DungeonView
        {
            Width = result.Width,
            Height = result.Height,
            Walkable = new bool[result.Height, result.Width],
            Opaque = new bool[result.Height, result.Width],
            Doors = new byte[result.Height, result.Width]
        };

        // Map Edgar data to DungeonView format
        // ...

        return view;
    }
}
```

### Phase 4: Create Dungeon Rendering Plugin

**New project:**

```
projects/dungeon/dotnet/console-app/plugins/PigeonPea.Plugin.Dungeon.Rendering/
```

**Structure:**

```
PigeonPea.Plugin.Dungeon.Rendering/
├─ DungeonRenderer.cs
├─ plugin.json
└─ PigeonPea.Plugin.Dungeon.Rendering.csproj
```

**Dependencies:**

```xml
<ProjectReference Include="dotnet/game-essential/.../PigeonPea.Dungeon.Contracts" />
<ProjectReference Include="dotnet/game-essential/.../PigeonPea.Rendering.Contracts" />
<!-- NO other dependencies -->
```

**Implementation:**

```csharp
public class DungeonRenderer
{
    private readonly IRenderer _platformRenderer; // Injected (ANSI/Braille/SkiaSharp)

    public DungeonRenderer(IRenderer platformRenderer)
    {
        _platformRenderer = platformRenderer;
    }

    public void Render(DungeonView dungeon, Point playerPosition)
    {
        _platformRenderer.BeginFrame();

        // Render dungeon tiles
        for (int y = 0; y < dungeon.Height; y++)
        {
            for (int x = 0; x < dungeon.Width; x++)
            {
                var tile = GetTileForCell(dungeon, x, y, playerPosition);
                _platformRenderer.DrawTile(x, y, tile);
            }
        }

        _platformRenderer.EndFrame();
    }

    private Tile GetTileForCell(DungeonView dungeon, int x, int y, Point playerPos)
    {
        // Player
        if (x == playerPos.X && y == playerPos.Y)
            return new Tile('@', Color.White, Color.Black);

        // Doors
        if (dungeon.Doors[y, x] != 0)
        {
            char glyph = dungeon.Doors[y, x] == 1 ? '+' : '/'; // Closed : Open
            return new Tile(glyph, Color.Brown, Color.Black);
        }

        // Walls
        if (!dungeon.Walkable[y, x])
            return new Tile('#', Color.Gray, Color.Black);

        // Floor
        return new Tile('.', Color.DarkGray, Color.Black);
    }
}
```

### Phase 5: Refactor Platform Renderers

**Goal:** Platform renderers are generic, dungeon-agnostic

**ANSIRenderer refactoring:**

```diff
// PigeonPea.Plugin.Rendering.Terminal.ANSI.csproj

- <ProjectReference Include="PigeonPea.Dungeon.Core" />
- <ProjectReference Include="PigeonPea.Dungeon.Rendering" />
- <ProjectReference Include="PigeonPea.Game.Contracts" />
+ <ProjectReference Include="PigeonPea.Rendering.Contracts" />
```

**ANSIRenderer.cs:**

```csharp
public class ANSIRenderer : IRenderer
{
    private readonly StringBuilder _buffer = new();

    public void BeginFrame()
    {
        _buffer.Clear();
        _buffer.Append("\x1b[2J\x1b[H"); // Clear screen
    }

    public void DrawTile(int x, int y, Tile tile)
    {
        // Move cursor
        _buffer.Append($"\x1b[{y + 1};{x + 1}H");

        // Set colors
        _buffer.Append($"\x1b[38;2;{tile.Foreground.R};{tile.Foreground.G};{tile.Foreground.B}m");
        _buffer.Append($"\x1b[48;2;{tile.Background.R};{tile.Background.G};{tile.Background.B}m");

        // Draw character
        _buffer.Append(tile.Glyph);
    }

    public void DrawText(int x, int y, string text, Color fg, Color bg)
    {
        _buffer.Append($"\x1b[{y + 1};{x + 1}H");
        _buffer.Append($"\x1b[38;2;{fg.R};{fg.G};{fg.B}m");
        _buffer.Append($"\x1b[48;2;{bg.R};{bg.G};{bg.B}m");
        _buffer.Append(text);
    }

    public void EndFrame()
    {
        _buffer.Append("\x1b[0m"); // Reset
        Console.Write(_buffer.ToString());
        Console.Out.Flush();
    }

    // No dungeon-specific code!
}
```

**Same pattern for:**

- `PigeonPea.Plugin.Rendering.Terminal.Braille`
- `PigeonPea.Plugin.Rendering.Windows.SkiaSharp`

### Phase 6: Configuration-Driven ALC Whitelist

**Goal:** Replace hardcoded assembly list with configuration

**New configuration file:**

```json
// appsettings.json or plugin-config.json
{
  "PluginSystem": {
    "SharedAssemblies": [
      "PigeonPea.Contracts",
      "PigeonPea.Game.Contracts",
      "PigeonPea.Rendering.Contracts",
      "PigeonPea.Dungeon.Contracts",
      "PigeonPea.Shared.Inventory",
      "Arch"
    ]
  }
}
```

**PluginLoadContext refactoring:**

```csharp
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly HashSet<string> _sharedAssemblies;

    public PluginLoadContext(string pluginPath, IEnumerable<string> sharedAssemblies, bool isCollectible = true)
        : base(isCollectible: isCollectible)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        _sharedAssemblies = new HashSet<string>(sharedAssemblies, StringComparer.OrdinalIgnoreCase);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Config-driven sharing
        if (_sharedAssemblies.Contains(assemblyName.Name))
        {
            return null; // Load from default ALC
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path != null)
        {
            return LoadFromAssemblyPath(path);
        }

        return null; // Default context
    }
}
```

**Update PluginLoader:**

```csharp
public class PluginLoader
{
    private readonly IConfiguration _configuration;

    public async Task<int> DiscoverAndLoadAsync(...)
    {
        // Read shared assemblies from config
        var sharedAssemblies = _configuration
            .GetSection("PluginSystem:SharedAssemblies")
            .Get<string[]>() ?? Array.Empty<string>();

        // ...

        var alc = new PluginLoadContext(assemblyPath, sharedAssemblies, isCollectible: true);

        // ...
    }
}
```

### Phase 7: Fix PigeonPea.Shared

**Analysis needed:**

```bash
# Categorize contents
ls dotnet/game-essential/core/src/PigeonPea.Shared/
```

**Refactoring strategy:**

1. **Dungeon-specific code** → Move to `Dungeon.Contracts` or delete (if duplicates `.Core`)
2. **Map-specific code** → Move to `Map.Contracts`
3. **Truly shared utilities** → Keep in appropriate shared assembly
4. **Game logic** → Might belong in a new `PigeonPea.Game.Core` (if needed)

**Potential outcome:**

- Delete `PigeonPea.Shared` entirely, or
- Reduce to minimal truly-shared utilities

## Implementation Strategy

### Incremental Approach (App Always Functional)

**Week 1: Preparation (App remains functional)**

1. **Day 1-2: Create new contracts**
   - Create `PigeonPea.Rendering.Contracts` (copy from `Shared.Rendering`)
   - Don't delete old contracts yet
   - Add to solution, build to verify

2. **Day 3-4: Create dungeon plugins (parallel to old code)**
   - Create `PigeonPea.Plugin.Dungeon.ModernEdgar`
   - Create `PigeonPea.Plugin.Dungeon.Rendering`
   - Register plugins alongside old code
   - Verify they load successfully

3. **Day 5: Test rendering with new plugins**
   - Console app can use EITHER old flow OR new plugins (config toggle)
   - Verify new plugins produce same visual output
   - Fix any issues

**Week 2: Transition (App switches to new plugins)**

1. **Day 1-2: Refactor platform renderers**
   - Update ANSIRenderer to use new `Rendering.Contracts`
   - Keep old code commented out
   - Verify rendering still works

2. **Day 3: Switch default to new flow**
   - Console app defaults to new plugins
   - Old flow available via config flag for rollback
   - Test thoroughly

3. **Day 4-5: Update PluginLoadContext**
   - Add configuration support
   - Migrate hardcoded whitelist to config
   - Verify ALC sharing works correctly

**Week 3: Cleanup (Remove old code)**

1. **Day 1: Remove old wrapper projects**
   - Delete `Dungeon.Core`, `Dungeon.Rendering`, `Dungeon.Control`
   - Remove from solution
   - Update any remaining references

2. **Day 2-3: Fix PigeonPea.Shared**
   - Analyze and categorize contents
   - Move code to appropriate locations
   - Delete or minimize assembly

3. **Day 4: Delete old contracts**
   - Remove `Game.Contracts.Rendering.IRenderer` (old one)
   - Ensure all references updated

4. **Day 5: Testing and documentation**
   - Full regression testing
   - Update architecture docs
   - Verify all plugins load and work

### Validation Checkpoints

**After each week:**

- [ ] Console app starts without errors
- [ ] Dungeon generates and displays correctly
- [ ] Player movement works
- [ ] ANSI rendering produces expected output
- [ ] Plugins load successfully
- [ ] No ALC type identity errors

## Testing Strategy

### Unit Tests

1. **Plugin Loading:**

   ```csharp
   [Test]
   public async Task DungeonGenerator_LoadsSuccessfully()
   {
       var loader = CreatePluginLoader();
       var count = await loader.DiscoverAndLoadAsync(pluginPaths, "console");
       Assert.Greater(count, 0);

       var generator = registry.Get<IDungeonGenerator>();
       Assert.IsNotNull(generator);
   }
   ```

2. **Dungeon Generation:**

   ```csharp
   [Test]
   public void DungeonGenerator_ProducesDungeonView()
   {
       var generator = new ModernEdgarDungeonGenerator();
       var dungeon = generator.Generate(new DungeonGenerationOptions { Width = 50, Height = 30 });

       Assert.AreEqual(50, dungeon.Width);
       Assert.AreEqual(30, dungeon.Height);
       Assert.IsNotNull(dungeon.Walkable);
   }
   ```

3. **Rendering:**

   ```csharp
   [Test]
   public void DungeonRenderer_CallsPlatformRenderer()
   {
       var mockRenderer = new Mock<IRenderer>();
       var dungeonRenderer = new DungeonRenderer(mockRenderer.Object);

       dungeonRenderer.Render(testDungeon, new Point(5, 5));

       mockRenderer.Verify(r => r.BeginFrame(), Times.Once);
       mockRenderer.Verify(r => r.DrawTile(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Tile>()), Times.AtLeastOnce);
       mockRenderer.Verify(r => r.EndFrame(), Times.Once);
   }
   ```

### Integration Tests

1. **Full Plugin Stack:**

   ```csharp
   [Test]
   public async Task FullStack_GeneratesAndRenders()
   {
       // Load all plugins
       await LoadPlugins();

       // Get generator
       var generator = registry.Get<IDungeonGenerator>();
       var dungeon = generator.Generate(options);

       // Get renderers
       var platformRenderer = registry.Get<IRenderer>("ansi");
       var dungeonRenderer = new DungeonRenderer(platformRenderer);

       // Render (should not throw)
       Assert.DoesNotThrow(() => dungeonRenderer.Render(dungeon, playerPos));
   }
   ```

### Visual/Manual Tests

1. **Console Output Verification:**
   - Dungeon displays with walls (#), floors (.), doors (+//)
   - Player (@) is visible
   - Colors render correctly in ANSI mode
   - Braille mode produces different output

2. **Mode Switching:**
   - Switch between ANSI/Braille at runtime
   - Verify output changes appropriately

## Migration Path

### For Existing Code

1. **Projects referencing `Dungeon.Core`:**
   - If host code: Use `IDungeonGenerator` service instead
   - If plugin code: Reference `Dungeon.Contracts` only

2. **Projects using `GameState.Dungeon`:**
   - Transition to querying dungeon service directly
   - Or: Keep `GameState` but with `DungeonView` (not `DungeonData`)

3. **Custom rendering code:**
   - Implement `IRenderer` interface
   - Register as platform plugin

### For Future Features

**Adding new domain (e.g., Map):**

```
1. Create Map.Contracts (Tier 1)
2. Create Plugin.Map.Generator (Tier 3)
3. Create Plugin.Map.Rendering (Tier 3)
4. Reuse platform renderers (ANSI, Braille, SkiaSharp)
```

**Adding new platform (e.g., Sixel):**

```
1. Create Plugin.Rendering.Terminal.Sixel
2. Implement IRenderer interface
3. All domain plugins automatically work with Sixel!
```

## Success Criteria

- [ ] No wrapper projects in game-essential layer
- [ ] Plugins reference only Tier 1 contracts
- [ ] Dungeon generation uses `modern-edgar-dotnet` directly
- [ ] Platform renderers are domain-agnostic
- [ ] ALC whitelist is configuration-driven
- [ ] `PigeonPea.Shared` eliminated or minimized
- [ ] Console app functional at each step
- [ ] No ALC type identity errors
- [ ] Dungeon displays correctly in ANSI and Braille modes

## Future Enhancements

1. **Tier 2 Proxy Generation:**
   - Source generators for service selection
   - Registry-based routing

2. **Tier 4 Provider System:**
   - Multiple dungeon generators (Edgar, BSP, Cellular Automata)
   - Selection via configuration or runtime

3. **Hot Reload:**
   - Plugin unload/reload without app restart
   - Development iteration speed

4. **Additional Platforms:**
   - Sixel, iTerm2 inline images, Kitty graphics
   - DirectX, Vulkan for Windows app

## References

- [RFC-006: Plugin System Architecture](./006-plugin-system-architecture.md)
- [RFC-014: Scene Management with ECS](./014-scene-management-ecs.md)
- [Assembly Load Contexts (Microsoft Docs)](https://docs.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
- [modern-edgar-dotnet](https://github.com/OndrejNepozitek/Edgar-DotNet)

## Appendix A: Project Structure

### Before (Current)

```
dotnet/game-essential/core/src/
├─ PigeonPea.Dungeon.Core/           ❌ Wrapper, should be plugin
├─ PigeonPea.Dungeon.Rendering/      ❌ Domain utilities, wrong layer
├─ PigeonPea.Dungeon.Control/        ❌ Pre-plugin artifact
├─ PigeonPea.Dungeon.Contracts/      ✅ Keep
├─ PigeonPea.Shared/                 ⚠️  God object, needs refactoring
└─ PigeonPea.Game.Contracts/
    └─ Rendering/
        └─ IRenderer.cs              ⚠️  Duplicate contract

dotnet/engine/core/src/
└─ PigeonPea.Shared.Rendering/
    └─ IRenderer.cs                  ⚠️  Duplicate contract

projects/dungeon/dotnet/console-app/plugins/
└─ PigeonPea.Plugins.Rendering.Terminal.ANSI/
    ├─ Dependencies:
    │   ├─ Dungeon.Core              ❌ Should not depend on domain
    │   └─ Dungeon.Rendering         ❌ Should not depend on domain
```

### After (Target)

```
dotnet/game-essential/core/src/
├─ PigeonPea.Dungeon.Contracts/      ✅ Tier 1: Interfaces & DTOs
│   ├─ IDungeonGenerator.cs
│   └─ Models/DungeonView.cs
│
├─ PigeonPea.Rendering.Contracts/    ✅ Unified rendering contract
│   ├─ IRenderer.cs
│   ├─ Tile.cs
│   └─ Color.cs
│
└─ PigeonPea.Game.Contracts/         ✅ Clean, no rendering

projects/dungeon/dotnet/console-app/plugins/
├─ PigeonPea.Plugin.Dungeon.ModernEdgar/       ✅ Tier 3: Generation
│   └─ Uses modern-edgar-dotnet directly
│
├─ PigeonPea.Plugin.Dungeon.Rendering/         ✅ Tier 3: Domain rendering
│   └─ Uses IRenderer (platform-agnostic)
│
├─ PigeonPea.Plugin.Rendering.Terminal.ANSI/   ✅ Tier 3: Platform rendering
│   └─ Implements IRenderer (dungeon-agnostic)
│
└─ PigeonPea.Plugin.Rendering.Terminal.Braille/ ✅ Tier 3: Platform rendering
    └─ Implements IRenderer (dungeon-agnostic)
```

## Appendix B: Dependency Graph

### Before (Problematic)

```
Plugin.ANSI ──┬──> Dungeon.Core ──> modern-edgar-dotnet
              └──> Dungeon.Rendering ──> Dungeon.Core
                                    └──> Shared.Rendering

Problem: Plugin depends on domain implementations, creates ALC issues
```

### After (Clean)

```
Plugin.Dungeon.ModernEdgar ──> Dungeon.Contracts ✅
                           └──> modern-edgar-dotnet ✅

Plugin.Dungeon.Rendering ──> Dungeon.Contracts ✅
                         └──> Rendering.Contracts ✅

Plugin.ANSI ──> Rendering.Contracts ✅

Flow:
  Plugin.Dungeon.Rendering.Render()
    └─> calls IRenderer.DrawTile()
         └─> Plugin.ANSI.DrawTile() executes

Separation: Domain and platform plugins don't know about each other!
```

## Appendix C: Configuration Schema

### PluginSystem Configuration

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "PluginSystem": {
      "type": "object",
      "properties": {
        "SharedAssemblies": {
          "type": "array",
          "description": "Assemblies shared across ALC boundary (loaded from default context)",
          "items": {
            "type": "string",
            "pattern": "^[A-Za-z][A-Za-z0-9.]*$"
          },
          "default": [
            "PigeonPea.Contracts",
            "PigeonPea.Game.Contracts",
            "PigeonPea.Rendering.Contracts",
            "Arch"
          ]
        },
        "PluginPaths": {
          "type": "array",
          "description": "Directories to search for plugins",
          "items": {
            "type": "string"
          },
          "default": ["plugins"]
        }
      }
    }
  }
}
```

### Example appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "PigeonPea.PluginSystem": "Debug"
    }
  },
  "PluginSystem": {
    "SharedAssemblies": [
      "PigeonPea.Contracts",
      "PigeonPea.Game.Contracts",
      "PigeonPea.Rendering.Contracts",
      "PigeonPea.Dungeon.Contracts",
      "PigeonPea.Map.Contracts",
      "PigeonPea.Shared.Inventory",
      "Arch"
    ],
    "PluginPaths": ["plugins", "~/PigeonPea/Plugins", "C:/ProgramData/PigeonPea/Plugins"]
  }
}
```
