---
doc_id: 'GUIDE-2025-00001'
title: 'Architecture Refinement Implementation Guide'
doc_type: 'guide'
status: 'active'
canonical: true
created: '2025-11-19'
updated: '2025-11-19'
tags: ['implementation', 'guide', 'architecture', 'refactoring', 'step-by-step']
summary: 'Step-by-step implementation guide for refining plugin architecture and introducing scene management, with incremental steps that keep the console app functional at each stage'
supersedes: []
related: ['RFC-2025-00013', 'RFC-2025-00014']
---

# Architecture Refinement Implementation Guide

**For Agent Implementation**

This guide provides step-by-step instructions for implementing RFC-013 (Plugin Architecture Refinement) and RFC-014 (Scene Management with ECS). The approach is incremental: **the console app remains functional after each phase**.

## Prerequisites

- Read [RFC-013: Plugin Architecture Refinement](../rfcs/013-plugin-architecture-refinement-tiered.md)
- Read [RFC-014: Scene Management with ECS](../rfcs/014-scene-management-ecs.md)
- Understand current codebase structure
- Backup current working branch

## Overview

### Goals

1. Eliminate wrapper projects (Dungeon.Core, Dungeon.Rendering, Dungeon.Control)
2. Create proper tier-based plugin architecture
3. Separate domain plugins (dungeon logic) from platform plugins (ANSI/Braille/SkiaSharp)
4. Introduce scene management with ECS
5. Configuration-driven ALC whitelist

### Timeline

- **Week 1:** Preparation and new contracts (app remains on old code)
- **Week 2:** Create new plugins in parallel (app can toggle between old/new)
- **Week 3:** Switch default to new plugins (old code as fallback)
- **Week 4:** Remove old code and cleanup

## Week 1: Preparation (App Unchanged)

### Day 1-2: Create Unified Rendering Contract

**Goal:** One rendering contract to replace the two existing ones

**Actions:**

1. **Create new project:**
   ```bash
   dotnet new classlib -n PigeonPea.Rendering.Contracts -o dotnet/game-essential/core/src/PigeonPea.Rendering.Contracts
   ```

2. **Add to solution:**
   ```bash
   dotnet sln add dotnet/game-essential/core/src/PigeonPea.Rendering.Contracts/PigeonPea.Rendering.Contracts.csproj
   ```

3. **Copy interfaces from Shared.Rendering:**
   ```
   PigeonPea.Rendering.Contracts/
   ├─ IRenderer.cs (merge both IRenderer interfaces)
   ├─ IRenderTarget.cs
   ├─ Tile.cs
   ├─ Color.cs (or use SadRogue.Primitives.Color)
   ├─ Viewport.cs
   ├─ RendererCapabilities.cs
   └─ TileFlags.cs
   ```

4. **Unified IRenderer.cs:**
   ```csharp
   namespace PigeonPea.Rendering.Contracts;

   public interface IRenderer
   {
       // Identification
       string Id { get; }
       RendererCapabilities Capabilities { get; }

       // Lifecycle
       void Initialize(IRenderTarget target);
       void Shutdown();

       // Frame management
       void BeginFrame();
       void EndFrame();

       // Drawing operations
       void Clear(Color color);
       void SetViewport(Viewport viewport);
       void DrawTile(int x, int y, Tile tile);
       void DrawText(int x, int y, string text, Color foreground, Color background);
   }
   ```

5. **Build to verify:**
   ```bash
   dotnet build dotnet/game-essential/core/src/PigeonPea.Rendering.Contracts/
   ```

**Validation:**
- [ ] Project compiles
- [ ] No errors in solution
- [ ] Console app still runs (unchanged)

---

### Day 3-4: Create Dungeon Generation Plugin (Parallel)

**Goal:** New plugin using modern-edgar-dotnet directly, alongside old code

**Actions:**

1. **Create plugin project:**
   ```bash
   dotnet new classlib -n PigeonPea.Plugin.Dungeon.ModernEdgar \
     -o projects/dungeon/dotnet/console-app/plugins/PigeonPea.Plugin.Dungeon.ModernEdgar
   ```

2. **Add references:**
   ```xml
   <!-- PigeonPea.Plugin.Dungeon.ModernEdgar.csproj -->
   <ItemGroup>
     <ProjectReference Include="../../../../../../dotnet/app-essential/core/src/PigeonPea.Contracts/PigeonPea.Contracts.csproj" />
     <ProjectReference Include="../../../../../../dotnet/game-essential/core/src/PigeonPea.Dungeon.Contracts/PigeonPea.Dungeon.Contracts.csproj" />
     <ProjectReference Include="../../../../../../dotnet/_lib/modern-edgar-dotnet/src/Edgar.Core/Edgar.Core.csproj" />
   </ItemGroup>
   ```

3. **Implement IDungeonGenerator:**
   ```csharp
   // ModernEdgarDungeonGenerator.cs
   using Edgar.Core;
   using PigeonPea.Dungeon.Contracts;
   using PigeonPea.Dungeon.Contracts.Models;
   using PigeonPea.Contracts.Plugin;

   namespace PigeonPea.Plugin.Dungeon.ModernEdgar;

   public class ModernEdgarDungeonGenerator : IPlugin, IDungeonGenerator
   {
       private ILogger _logger = null!;

       public string Id => "dungeon-generator-modern-edgar";

       public Task InitializeAsync(PluginContext context, CancellationToken ct)
       {
           _logger = context.Logger;
           _logger.LogInformation("ModernEdgar dungeon generator initialized");
           return Task.CompletedTask;
       }

       public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
       public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

       public DungeonView Generate(DungeonGenerationOptions options)
       {
           _logger.LogInformation("Generating dungeon: {Width}x{Height}", options.Width, options.Height);

           // Use Edgar directly (no wrapper)
           var edgarConfig = CreateEdgarConfiguration(options);
           var generator = new EdgarGenerator();
           var edgarResult = generator.Generate(edgarConfig);

           // Convert to DungeonView
           return ConvertToDungeonView(edgarResult, options.Width, options.Height);
       }

       private EdgarConfiguration CreateEdgarConfiguration(DungeonGenerationOptions options)
       {
           // Configure Edgar based on options
           // TODO: Implement Edgar configuration
           return new EdgarConfiguration();
       }

       private DungeonView ConvertToDungeonView(EdgarResult result, int width, int height)
       {
           var view = new DungeonView
           {
               Width = width,
               Height = height,
               Walkable = new bool[height, width],
               Opaque = new bool[height, width],
               Doors = new byte[height, width]
           };

           // Map Edgar result to DungeonView
           // TODO: Implement conversion logic
           // For now, create a simple test dungeon
           for (int y = 0; y < height; y++)
           {
               for (int x = 0; x < width; x++)
               {
                   // Border walls
                   if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                   {
                       view.Walkable[y, x] = false;
                       view.Opaque[y, x] = true;
                   }
                   else
                   {
                       view.Walkable[y, x] = true;
                       view.Opaque[y, x] = false;
                   }
               }
           }

           return view;
       }
   }
   ```

4. **Create plugin.json:**
   ```json
   {
     "id": "dungeon-generator-modern-edgar",
     "name": "Modern Edgar Dungeon Generator",
     "version": "0.1.0",
     "author": "PigeonPea",
     "description": "Dungeon generation using modern-edgar-dotnet library",
     "entryPoint": {
       "console": "PigeonPea.Plugin.Dungeon.ModernEdgar.dll,PigeonPea.Plugin.Dungeon.ModernEdgar.ModernEdgarDungeonGenerator"
     },
     "dependencies": []
   }
   ```

5. **Post-build copy to plugins:**
   ```xml
   <Target Name="CopyPluginToConsoleApp" AfterTargets="Build">
     <PropertyGroup>
       <PluginOutputPath>$(MSBuildThisFileDirectory)bin\$(Configuration)\$(TargetFramework)\</PluginOutputPath>
       <PluginTargetDir>$(MSBuildThisFileDirectory)..\..\core\src\PigeonPea.Console\bin\$(Configuration)\$(TargetFramework)\plugins\$(AssemblyName)\</PluginTargetDir>
     </PropertyGroup>
     <ItemGroup>
       <PluginFiles Include="$(PluginOutputPath)**\*.*" />
     </ItemGroup>
     <MakeDir Directories="$(PluginTargetDir)" />
     <Copy SourceFiles="@(PluginFiles)" DestinationFolder="$(PluginTargetDir)%(RecursiveDir)" SkipUnchangedFiles="true" />
   </Target>
   ```

6. **Build and test:**
   ```bash
   dotnet build projects/dungeon/dotnet/console-app/plugins/PigeonPea.Plugin.Dungeon.ModernEdgar/
   ```

**Validation:**
- [ ] Plugin compiles
- [ ] Plugin copies to console app plugins directory
- [ ] Console app still runs (doesn't use new plugin yet)

---

### Day 5: Add Configuration Toggle

**Goal:** Allow app to switch between old and new dungeon generation

**Actions:**

1. **Add to appsettings.json:**
   ```json
   {
     "DungeonSystem": {
       "UseNewPluginArchitecture": false,  // Start with false (old code)
       "GeneratorPluginId": "dungeon-generator-modern-edgar"
     }
   }
   ```

2. **Update Program.cs to support both:**
   ```csharp
   var useNewArch = configuration.GetValue<bool>("DungeonSystem:UseNewPluginArchitecture");

   if (useNewArch)
   {
       // Use new plugin
       var generatorId = configuration.GetValue<string>("DungeonSystem:GeneratorPluginId");
       var generator = registry.Get<IDungeonGenerator>(generatorId);
       var dungeon = generator.Generate(new DungeonGenerationOptions { Width = 80, Height = 50 });
       Console.WriteLine("Using NEW plugin architecture");
   }
   else
   {
       // Use old code (Dungeon.Core wrapper)
       // ... existing code
       Console.WriteLine("Using OLD architecture");
   }
   ```

**Validation:**
- [ ] App runs with `UseNewPluginArchitecture: false` (old way)
- [ ] App loads but doesn't crash with `UseNewPluginArchitecture: true` (new way)

---

## Week 2: Create New Plugins (Parallel Implementation)

### Day 1-2: Create Dungeon Rendering Plugin

**Goal:** Domain plugin that knows how to render dungeons (calls IRenderer)

**Actions:**

1. **Create plugin project:**
   ```bash
   dotnet new classlib -n PigeonPea.Plugin.Dungeon.Rendering \
     -o projects/dungeon/dotnet/console-app/plugins/PigeonPea.Plugin.Dungeon.Rendering
   ```

2. **Add references:**
   ```xml
   <ItemGroup>
     <ProjectReference Include="../../../../../../dotnet/game-essential/core/src/PigeonPea.Dungeon.Contracts/PigeonPea.Dungeon.Contracts.csproj" />
     <ProjectReference Include="../../../../../../dotnet/game-essential/core/src/PigeonPea.Rendering.Contracts/PigeonPea.Rendering.Contracts.csproj" />
   </ItemGroup>
   ```

3. **Implement DungeonRenderer:**
   ```csharp
   // DungeonRenderer.cs
   using PigeonPea.Dungeon.Contracts.Models;
   using PigeonPea.Rendering.Contracts;

   namespace PigeonPea.Plugin.Dungeon.Rendering;

   public class DungeonRenderer
   {
       private readonly IRenderer _platformRenderer;

       public DungeonRenderer(IRenderer platformRenderer)
       {
           _platformRenderer = platformRenderer;
       }

       public void Render(DungeonView dungeon, int playerX, int playerY)
       {
           _platformRenderer.BeginFrame();
           _platformRenderer.Clear(Color.Black);

           // Render dungeon tiles
           for (int y = 0; y < dungeon.Height; y++)
           {
               for (int x = 0; x < dungeon.Width; x++)
               {
                   var tile = GetTileForCell(dungeon, x, y, playerX, playerY);
                   _platformRenderer.DrawTile(x, y, tile);
               }
           }

           _platformRenderer.EndFrame();
       }

       private Tile GetTileForCell(DungeonView dungeon, int x, int y, int playerX, int playerY)
       {
           // Player
           if (x == playerX && y == playerY)
               return new Tile('@', Color.White, Color.Black);

           // Doors
           if (dungeon.Doors[y, x] != 0)
           {
               char glyph = dungeon.Doors[y, x] == 1 ? '+' : '/';
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

**Validation:**
- [ ] Plugin compiles
- [ ] No dependencies on Dungeon.Core or old Dungeon.Rendering

---

### Day 3: Refactor ANSI Platform Renderer

**Goal:** Make ANSIRenderer generic (dungeon-agnostic)

**Actions:**

1. **Update ANSIRenderer.csproj:**
   ```diff
   - <ProjectReference Include="PigeonPea.Dungeon.Core" />
   - <ProjectReference Include="PigeonPea.Dungeon.Rendering" />
   - <ProjectReference Include="PigeonPea.Game.Contracts" />
   + <ProjectReference Include="PigeonPea.Rendering.Contracts" />
   ```

2. **Simplify ANSIRenderer.cs:**
   ```csharp
   // Remove all dungeon-specific code
   // Keep only IRenderer implementation

   public class ANSIRenderer : IRenderer
   {
       private readonly StringBuilder _buffer = new();
       private int _width;
       private int _height;

       public string Id => "ansi-terminal-renderer";

       public void Initialize(IRenderTarget target)
       {
           _width = target.Width;
           _height = target.Height;
           Console.OutputEncoding = Encoding.UTF8;
           Console.CursorVisible = false;
       }

       public void BeginFrame()
       {
           _buffer.Clear();
           _buffer.Append("\x1b[2J\x1b[H"); // Clear screen, home cursor
       }

       public void DrawTile(int x, int y, Tile tile)
       {
           // Move cursor
           _buffer.Append($"\x1b[{y + 1};{x + 1}H");

           // Set colors
           _buffer.Append($"\x1b[38;2;{tile.Foreground.R};{tile.Foreground.G};{tile.Foreground.B}m");
           _buffer.Append($"\x1b[48;2;{tile.Background.R};{tile.Background.G};{tile.Background.B}m");

           // Draw glyph
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
           _buffer.Append("\x1b[0m"); // Reset colors
           Console.Write(_buffer.ToString());
           Console.Out.Flush();
       }

       // Remove: ToDungeonData(), BrailleDungeonRenderer usage, GameState handling
   }
   ```

**Validation:**
- [ ] ANSIRenderer compiles
- [ ] No dungeon-specific code remains
- [ ] Implements IRenderer interface only

---

### Day 4-5: Wire New Plugins Together

**Goal:** New plugin stack works end-to-end

**Actions:**

1. **Update Program.cs:**
   ```csharp
   if (useNewArch)
   {
       // 1. Get dungeon generator plugin
       var generator = registry.Get<IDungeonGenerator>("dungeon-generator-modern-edgar");
       var dungeon = generator.Generate(new DungeonGenerationOptions { Width = 80, Height = 50 });

       // 2. Get platform renderer plugin
       var platformRenderer = registry.Get<IRenderer>("ansi-terminal-renderer");

       // 3. Create dungeon renderer (domain plugin)
       var dungeonRenderer = new DungeonRenderer(platformRenderer);

       // 4. Render loop
       while (running)
       {
           dungeonRenderer.Render(dungeon, playerX, playerY);

           // Handle input...
           // Update player position...
       }
   }
   ```

2. **Test with new architecture:**
   ```json
   // appsettings.json
   {
     "DungeonSystem": {
       "UseNewPluginArchitecture": true
     }
   }
   ```

3. **Run console app:**
   ```bash
   dotnet run --project projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/
   ```

**Validation:**
- [ ] App starts without errors
- [ ] Dungeon displays (even if simple test pattern)
- [ ] Player '@' visible
- [ ] Walls '#', floors '.' render
- [ ] Can toggle back to old architecture (set to false)

---

## Week 3: Transition (Switch Default to New)

### Day 1-2: Configuration-Driven ALC Whitelist

**Goal:** Remove hardcoded assembly list from PluginLoadContext

**Actions:**

1. **Add to appsettings.json:**
   ```json
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

2. **Refactor PluginLoadContext.cs:**
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
           if (_sharedAssemblies.Contains(assemblyName.Name))
           {
               return null; // Load from default ALC
           }

           var path = _resolver.ResolveAssemblyToPath(assemblyName);
           if (path != null)
           {
               return LoadFromAssemblyPath(path);
           }

           return null;
       }
   }
   ```

3. **Update PluginLoader to read config:**
   ```csharp
   public async Task<int> DiscoverAndLoadAsync(IEnumerable<string> pluginPaths, string profile, CancellationToken ct)
   {
       // Read shared assemblies from config
       var sharedAssemblies = _configuration
           .GetSection("PluginSystem:SharedAssemblies")
           .Get<string[]>() ?? new[]
           {
               "PigeonPea.Contracts",
               "PigeonPea.Rendering.Contracts"
           };

       _logger.LogInformation("Shared assemblies: {Assemblies}", string.Join(", ", sharedAssemblies));

       // ... existing code ...

       var alc = new PluginLoadContext(assemblyPath, sharedAssemblies, isCollectible: true);

       // ... rest of loading code ...
   }
   ```

**Validation:**
- [ ] Plugins load successfully
- [ ] No ALC type identity errors
- [ ] Can add/remove shared assemblies via config
- [ ] Console app runs with new ALC configuration

---

### Day 3: Make New Architecture Default

**Goal:** Default to new plugins, keep old as fallback

**Actions:**

1. **Update appsettings.json:**
   ```diff
   {
     "DungeonSystem": {
   -   "UseNewPluginArchitecture": false,
   +   "UseNewPluginArchitecture": true,
       "GeneratorPluginId": "dungeon-generator-modern-edgar"
     }
   }
   ```

2. **Add logging to show which path:**
   ```csharp
   if (useNewArch)
   {
       _logger.LogInformation("Using NEW plugin architecture");
       // ...
   }
   else
   {
       _logger.LogWarning("Using OLD architecture (fallback)");
       // ...
   }
   ```

**Validation:**
- [ ] App runs on new architecture by default
- [ ] Can still switch to old architecture via config
- [ ] Dungeon renders correctly
- [ ] Player movement works
- [ ] No regressions in functionality

---

## Week 4: Cleanup (Remove Old Code)

### Day 1: Delete Wrapper Projects

**Goal:** Remove old pre-plugin projects

**Actions:**

1. **Verify nothing references them:**
   ```bash
   # Search for project references
   grep -r "Dungeon.Core" dotnet/ projects/ --include="*.csproj"
   grep -r "Dungeon.Rendering" dotnet/ projects/ --include="*.csproj"
   grep -r "Dungeon.Control" dotnet/ projects/ --include="*.csproj"
   ```

2. **Remove from solution:**
   ```bash
   dotnet sln remove dotnet/game-essential/core/src/PigeonPea.Dungeon.Core/PigeonPea.Dungeon.Core.csproj
   dotnet sln remove dotnet/game-essential/core/src/PigeonPea.Dungeon.Rendering/PigeonPea.Dungeon.Rendering.csproj
   dotnet sln remove dotnet/game-essential/core/src/PigeonPea.Dungeon.Control/PigeonPea.Dungeon.Control.csproj
   ```

3. **Delete directories:**
   ```bash
   rm -rf dotnet/game-essential/core/src/PigeonPea.Dungeon.Core/
   rm -rf dotnet/game-essential/core/src/PigeonPea.Dungeon.Rendering/
   rm -rf dotnet/game-essential/core/src/PigeonPea.Dungeon.Control/
   ```

4. **Remove old architecture code from Program.cs:**
   ```diff
   - if (useNewArch)
   - {
       // New architecture (now the only way)
       var generator = registry.Get<IDungeonGenerator>("dungeon-generator-modern-edgar");
       // ...
   - }
   - else
   - {
   -     // OLD CODE - DELETE
   - }
   ```

5. **Remove config option:**
   ```diff
   {
     "DungeonSystem": {
   -   "UseNewPluginArchitecture": true,
       "GeneratorPluginId": "dungeon-generator-modern-edgar"
     }
   }
   ```

**Validation:**
- [ ] Solution builds
- [ ] No broken references
- [ ] Console app runs
- [ ] Dungeon generates and renders

---

### Day 2-3: Analyze and Fix PigeonPea.Shared

**Goal:** Remove god object, proper layering

**Actions:**

1. **List contents:**
   ```bash
   find dotnet/game-essential/core/src/PigeonPea.Shared/ -name "*.cs" | head -20
   ```

2. **Categorize code:**
   - **Dungeon-specific:** Move to Dungeon.Contracts or delete (duplicates)
   - **Map-specific:** Move to Map.Contracts
   - **Truly shared:** Keep in appropriate place

3. **Example refactoring:**
   ```bash
   # If Shared has dungeon utilities that are now obsolete:
   git rm dotnet/game-essential/core/src/PigeonPea.Shared/DungeonHelpers.cs

   # If Shared has useful map utilities:
   git mv dotnet/game-essential/core/src/PigeonPea.Shared/MapHelpers.cs \
          dotnet/game-essential/core/src/PigeonPea.Map.Contracts/Utilities/
   ```

4. **Option: Delete PigeonPea.Shared entirely** (if nothing useful remains)
   ```bash
   dotnet sln remove dotnet/game-essential/core/src/PigeonPea.Shared/PigeonPea.Shared.csproj
   rm -rf dotnet/game-essential/core/src/PigeonPea.Shared/
   ```

**Validation:**
- [ ] All useful code moved to appropriate locations
- [ ] No orphaned code
- [ ] Solution builds
- [ ] Tests pass

---

### Day 4: Delete Old Rendering Contracts

**Goal:** Remove duplicate IRenderer interfaces

**Actions:**

1. **Remove Game.Contracts.Rendering.IRenderer:**
   ```bash
   rm dotnet/game-essential/core/src/PigeonPea.Game.Contracts/Rendering/IRenderer.cs
   # Or delete entire Rendering/ directory if empty
   rmdir dotnet/game-essential/core/src/PigeonPea.Game.Contracts/Rendering/
   ```

2. **Decide on Shared.Rendering:**
   - **Option A:** Delete it (now replaced by Rendering.Contracts)
   - **Option B:** Keep if other code uses it (migrate gradually)

3. **Update all using statements:**
   ```csharp
   // Old
   - using PigeonPea.Game.Contracts.Rendering;
   - using PigeonPea.Shared.Rendering;

   // New
   + using PigeonPea.Rendering.Contracts;
   ```

**Validation:**
- [ ] Single IRenderer interface exists
- [ ] All code references new contract
- [ ] Solution builds

---

### Day 5: Testing and Documentation

**Goal:** Verify everything works, update docs

**Actions:**

1. **Full regression testing:**
   ```bash
   # Run all tests
   dotnet test

   # Run console app
   dotnet run --project projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/

   # Test scenarios:
   # - Dungeon generates
   # - Player moves
   # - Walls block movement
   # - ANSI rendering works
   # - Braille mode (if implemented)
   ```

2. **Update architecture docs:**
   - Document new plugin structure
   - Update dependency diagrams
   - Add examples of adding new domains

3. **Update CLAUDE.md:**
   - Reflect new project structure
   - Update plugin loading instructions

4. **Git commit:**
   ```bash
   git add .
   git commit -m "feat: refine plugin architecture to tier-based system

   - Remove wrapper projects (Dungeon.Core, Dungeon.Rendering, Control)
   - Create proper tier-based plugins (generation, rendering)
   - Separate domain plugins from platform plugins
   - Unify rendering contracts
   - Config-driven ALC whitelist
   - Fix PigeonPea.Shared god object

   Refs: RFC-013, RFC-014"
   ```

**Validation:**
- [ ] All tests pass
- [ ] Console app fully functional
- [ ] No ALC errors
- [ ] Dungeon renders correctly
- [ ] Architecture matches RFCs
- [ ] Documentation updated

---

## Success Criteria Checklist

### Plugin Architecture
- [ ] No wrapper projects exist (Dungeon.Core, Dungeon.Rendering, Control deleted)
- [ ] Plugins reference only Tier 1 contracts
- [ ] Dungeon generation uses modern-edgar-dotnet directly
- [ ] Platform renderers (ANSI, Braille) are domain-agnostic
- [ ] Domain renderer (Dungeon.Rendering plugin) uses IRenderer service
- [ ] ALC whitelist is configuration-driven
- [ ] PigeonPea.Shared eliminated or minimized

### Functional Requirements
- [ ] Console app starts without errors
- [ ] Dungeon generates correctly
- [ ] Dungeon displays in ANSI mode
- [ ] Player '@' renders at correct position
- [ ] Walls '#', floors '.', doors '+' render correctly
- [ ] Player can move (arrow keys/WASD)
- [ ] Walls block movement
- [ ] No ALC type identity errors in logs

### Code Quality
- [ ] Solution builds without warnings
- [ ] All tests pass
- [ ] No TODO comments left unaddressed
- [ ] Code follows project conventions

### Documentation
- [ ] Architecture docs updated
- [ ] CLAUDE.md reflects new structure
- [ ] RFC success criteria met
- [ ] Implementation notes added (if any deviations from plan)

---

## Troubleshooting

### Plugin Not Loading

**Symptom:** Plugin not found in registry

**Check:**
1. Plugin built successfully: `dotnet build <plugin-project>`
2. Plugin copied to output: Check `bin/Debug/net9.0/plugins/<plugin-name>/`
3. plugin.json exists and is valid JSON
4. Post-build copy target executed (check build output)

**Fix:**
```bash
# Manually copy if post-build failed
cp -r projects/.../bin/Debug/net9.0/* \
      projects/.../PigeonPea.Console/bin/Debug/net9.0/plugins/<plugin-name>/
```

---

### ALC Type Identity Error

**Symptom:** `typeof(DungeonView) != typeof(DungeonView)` error

**Check:**
1. Dungeon.Contracts in SharedAssemblies config
2. PluginLoadContext using config correctly
3. Host and plugin both reference same Dungeon.Contracts version

**Fix:**
```json
// appsettings.json
{
  "PluginSystem": {
    "SharedAssemblies": [
      "PigeonPea.Dungeon.Contracts"  // ← Add this
    ]
  }
}
```

---

### Rendering Issues

**Symptom:** Dungeon not displaying or garbled output

**Check:**
1. ANSI renderer BeginFrame() called
2. DrawTile() receiving correct coordinates
3. EndFrame() flushing output
4. Console encoding set to UTF-8

**Debug:**
```csharp
// Add logging to DungeonRenderer
_logger.LogInformation("Rendering {Width}x{Height} dungeon", dungeon.Width, dungeon.Height);

// Add logging to ANSIRenderer
_logger.LogDebug("DrawTile({X}, {Y}, '{Glyph}')", x, y, tile.Glyph);
```

---

## Next Steps (Future Enhancements)

After completing this refactoring:

1. **Implement Scene Management (RFC-014):**
   - Dungeon as entity with components
   - Player/monsters as entities
   - Scene lifecycle management

2. **Add Braille Renderer:**
   - Copy ANSI plugin structure
   - Implement Braille output

3. **Add Additional Dungeon Generators:**
   - BSP (Binary Space Partitioning)
   - Cellular Automata
   - Demonstrate Tier 4 provider selection

4. **Hot Reload Support:**
   - Plugin unload/reload
   - Development iteration

---

## References

- [RFC-013: Plugin Architecture Refinement](../rfcs/013-plugin-architecture-refinement-tiered.md)
- [RFC-014: Scene Management with ECS](../rfcs/014-scene-management-ecs.md)
- [CLAUDE.md](../../CLAUDE.md)
- [Plugin System Configuration](../rfcs/013-plugin-architecture-refinement-tiered.md#appendix-c-configuration-schema)

---

## Appendix: Quick Reference Commands

### Build and Run
```bash
# Build entire solution
dotnet build

# Build specific plugin
dotnet build projects/dungeon/dotnet/console-app/plugins/<plugin-name>/

# Run console app
dotnet run --project projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/

# Run tests
dotnet test
```

### Plugin Development
```bash
# Create new plugin
dotnet new classlib -n PigeonPea.Plugin.<Name> -o projects/.../plugins/PigeonPea.Plugin.<Name>

# Add references
dotnet add reference ../../dotnet/game-essential/core/src/PigeonPea.<Domain>.Contracts/

# Build and copy
dotnet build && <post-build-target-runs>
```

### Git
```bash
# Check status
git status

# Stage changes
git add <files>

# Commit
git commit -m "feat: <description>

<details>

Refs: RFC-013, RFC-014"

# Push
git push origin <branch-name>
```
