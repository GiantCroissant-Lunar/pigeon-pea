---
canonical: true
created: '2025-11-19'
doc_id: GUIDE-00002
doc_type: guide
related:
- RFC-00014
- GUIDE-00001
status: active
summary: Detailed step-by-step adjustment plan to complete RFC-014 Scene Management
  implementation, addressing interface mismatches, compilation errors, and missing
  features
supersedes: []
tags:
- rfc-014
- scene-management
- ecs
- adjustment
- implementation
title: 'RFC-014 Scene Management: Adjustment Plan'
updated: '2025-11-19'
---


# RFC-014 Scene Management: Adjustment Plan

**Status:** Active
**Purpose:** Fix critical issues and complete RFC-014 implementation
**Estimated Effort:** 2-3 days

---

## Executive Summary

The RFC-014 implementation is **60% complete** with good infrastructure but critical interface mismatches and compilation errors. This plan addresses:

1. ✅ **Fix compilation errors** (HIGH priority)
2. ✅ **Update interfaces to Entity-based** (HIGH priority)
3. ✅ **Complete dungeon generation integration** (HIGH priority)
4. ⚠️ **Implement player/monster entities** (MEDIUM priority)
5. ⚠️ **Add system pipeline** (FUTURE)

---

## Current State Assessment

### ✅ **What Works**

1. **Scene Infrastructure** - Complete
   - `ISceneManager` interface defined
   - `Scene` class with `World`, `SceneState`
   - `SceneManager` plugin implementation
   - `SceneLoadMode` enum (Single/Additive/Overlay)

2. **ECS Components** - Defined
   - `DungeonMapComponent` (Width, Height, TileData, etc.)
   - `SceneComponent` (SceneId, SceneName)
   - `Position`, `Renderable`

3. **Dungeon Renderer** - Uses ECS
   - Queries entities: `QueryDescription().WithAll<DungeonMapComponent>()`
   - Renders via platform `IRenderer`

### ❌ **What's Broken**

1. **Interface Mismatch**
   ```csharp
   // Current (OLD)
   public interface IDungeonGenerator
   {
       DungeonView Generate(DungeonGenerationOptions options);  // ❌ Returns DTO
   }

   // Needed (NEW)
   public interface IDungeonGenerator
   {
       Entity Generate(World world, DungeonGenerationOptions options);  // ✅ Returns Entity
   }
   ```

2. **BasicDungeonGenerator Won't Compile**
   - Missing `Arch` package reference
   - Missing `PigeonPea.Shared` reference
   - Uses new signature but interface has old signature

3. **ModernEdgarDungeonGenerator Not Updated**
   - Still returns `DungeonView` DTO
   - Needs Entity-based implementation

---

## Adjustment Plan

---

## **Phase 1: Fix Interface and Compilation (HIGH Priority)**

### **Step 1.1: Update IDungeonGenerator Interface**

**File:** `dotnet/game-essential/core/src/PigeonPea.Dungeon.Contracts/IDungeonGenerator.cs`

**Before:**
```csharp
using PigeonPea.Dungeon.Contracts.Models;

namespace PigeonPea.Dungeon.Contracts;

public interface IDungeonGenerator
{
    DungeonView Generate(DungeonGenerationOptions options);
}
```

**After:**
```csharp
using Arch.Core;
using PigeonPea.Dungeon.Contracts.Models;

namespace PigeonPea.Dungeon.Contracts;

public interface IDungeonGenerator
{
    /// <summary>
    /// Generates a dungeon and creates it as an entity in the provided world.
    /// </summary>
    /// <param name="world">The ECS world to create the dungeon entity in</param>
    /// <param name="options">Generation parameters (size, seed, etc.)</param>
    /// <returns>The created dungeon entity</returns>
    Entity Generate(World world, DungeonGenerationOptions options);
}
```

**Changes:**
- ✅ Add `using Arch.Core;`
- ✅ Change return type from `DungeonView` to `Entity`
- ✅ Add `World world` parameter
- ✅ Add XML documentation

**Impact:**
- All implementations must be updated to match new signature

---

### **Step 1.2: Add Package Reference to Dungeon.Contracts**

**File:** `dotnet/game-essential/core/src/PigeonPea.Dungeon.Contracts/PigeonPea.Dungeon.Contracts.csproj`

**Before:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="TheSadRogue.Primitives" Version="1.6.0-rc3" />
  </ItemGroup>
</Project>
```

**After:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Arch" Version="2.0.0" />
    <PackageReference Include="TheSadRogue.Primitives" Version="1.6.0-rc3" />
  </ItemGroup>
</Project>
```

**Changes:**
- ✅ Add `Arch` package reference (needed for `Entity`, `World`)

---

### **Step 1.3: Fix BasicDungeonGenerator Dependencies**

**File:** `dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Basic/PigeonPea.Plugin.Dungeon.Basic.csproj`

**Before:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\..\app-essential\core\src\PigeonPea.Contracts\PigeonPea.Contracts.csproj" />
    <ProjectReference Include="..\..\..\core\src\PigeonPea.Dungeon.Contracts\PigeonPea.Dungeon.Contracts.csproj" />
  </ItemGroup>
</Project>
```

**After:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- ECS Framework -->
    <PackageReference Include="Arch" Version="2.0.0" />

    <!-- Logging -->
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\..\app-essential\core\src\PigeonPea.Contracts\PigeonPea.Contracts.csproj" />
    <ProjectReference Include="..\..\..\core\src\PigeonPea.Dungeon.Contracts\PigeonPea.Dungeon.Contracts.csproj" />
    <ProjectReference Include="..\..\..\core\src\PigeonPea.Shared\PigeonPea.Shared.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="plugin.json" />
  </ItemGroup>

  <Target Name="CopyPluginJson" AfterTargets="Build">
    <Copy SourceFiles="plugin.json" DestinationFolder="$(OutDir)" SkipUnchangedFiles="true" />
  </Target>
</Project>
```

**Changes:**
- ✅ Add `Arch` package (for `Entity`, `World`, `QueryDescription`)
- ✅ Add `Microsoft.Extensions.Logging.Abstractions`
- ✅ Add reference to `PigeonPea.Shared` (for `DungeonMapComponent`, etc.)
- ✅ Add `plugin.json` copy target

---

### **Step 1.4: Update BasicDungeonGenerator Implementation**

**File:** `dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Basic/BasicDungeonGenerator.cs`

**Current Issues:**
- Line 13: Signature matches new interface ✅
- Missing: Create plugin wrapper ❌
- Missing: Create entity at end of generation ❌

**Add Plugin Wrapper:**

**Create new file:** `BasicDungeonGeneratorPlugin.cs`

```csharp
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Dungeon.Contracts;

namespace PigeonPea.Plugin.Dungeon.Basic;

public class BasicDungeonGeneratorPlugin : IPlugin
{
    private ILogger _logger = null!;

    public string Id => "dungeon-generator-basic";
    public string Name => "Basic Dungeon Generator";
    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct)
    {
        _logger = context.Logger;
        _logger.LogInformation("Basic dungeon generator plugin initialized");

        // Register the generator
        context.Registry.Register<IDungeonGenerator>(
            new BasicDungeonGenerator(),
            new ServiceMetadata
            {
                Priority = 50, // Lower priority than ModernEdgar
                Name = "BasicDungeonGenerator",
                Version = Version,
                PluginId = Id
            }
        );

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

**Update BasicDungeonGenerator.cs:**

At the end of the `Generate` method (around line 120), change from:

```csharp
return ToView(d);
```

To:

```csharp
return CreateDungeonEntity(world, d);
```

And add this helper method:

```csharp
private static Entity CreateDungeonEntity(World world, DungeonData d)
{
    // Convert DungeonData arrays to flat byte arrays for ECS component
    var walkableArray = new System.Collections.BitArray(d.Width * d.Height);
    var opaqueArray = new System.Collections.BitArray(d.Width * d.Height);
    var doorStates = new byte[d.Width * d.Height];

    for (int y = 0; y < d.Height; y++)
    {
        for (int x = 0; x < d.Width; x++)
        {
            int index = y * d.Width + x;
            walkableArray[index] = d.Walkable[y, x];
            opaqueArray[index] = d.Opaque[y, x];
            doorStates[index] = (byte)d.Doors[y, x];
        }
    }

    // Create dungeon entity in the world
    var dungeonEntity = world.Create(
        new DungeonMapComponent
        {
            Width = d.Width,
            Height = d.Height,
            TileData = new byte[d.Width * d.Height], // Populate if needed
            DoorStates = doorStates,
            Walkable = walkableArray,
            Opaque = opaqueArray
        },
        new PositionComponent { X = 0, Y = 0 },
        new RenderableComponent
        {
            Glyph = ' ',
            Foreground = SadRogue.Primitives.Color.White,
            Background = SadRogue.Primitives.Color.Black
        }
    );

    return dungeonEntity;
}
```

**Remove or keep as compatibility helper:**

```csharp
// Optional: Keep ToView() for testing/debugging
private static DungeonView ToView(DungeonData d)
{
    var view = new DungeonView
    {
        Width = d.Width,
        Height = d.Height,
        Walkable = d.Walkable,
        Opaque = d.Opaque,
        Doors = new byte[d.Height, d.Width]
    };

    for (int y = 0; y < d.Height; y++)
    {
        for (int x = 0; x < d.Width; x++)
        {
            view.Doors[y, x] = (byte)d.Doors[y, x];
        }
    }

    return view;
}
```

---

### **Step 1.5: Update ModernEdgarDungeonGenerator**

**File:** `dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.ModernEdgar/ModernEdgarDungeonGenerator.cs`

**Current Signature:**
```csharp
public DungeonView Generate(DungeonGenerationOptions options)
```

**New Signature:**
```csharp
public Entity Generate(World world, DungeonGenerationOptions options)
```

**Changes Needed:**

1. **Update method signature:**

```csharp
public Entity Generate(World world, DungeonGenerationOptions options)
{
    _logger.LogInformation("Generating dungeon with Edgar: {Width}x{Height}", options.Width, options.Height);

    try
    {
        // ... existing Edgar generation code ...

        // At the end, instead of:
        // return ConvertToDungeonView(result);

        // Do this:
        return CreateDungeonEntity(world, result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Edgar dungeon generation failed");
        throw;
    }
}
```

2. **Add helper method:**

```csharp
private Entity CreateDungeonEntity(World world, EdgarResult result)
{
    // Convert Edgar result to ECS component data
    var width = result.Width;
    var height = result.Height;

    var walkableArray = new System.Collections.BitArray(width * height);
    var opaqueArray = new System.Collections.BitArray(width * height);
    var doorStates = new byte[width * height];

    // Map Edgar data to arrays
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            int index = y * width + x;

            // Extract Edgar tile data
            var tile = result.GetTile(x, y); // Adjust based on Edgar API

            walkableArray[index] = tile.IsWalkable;
            opaqueArray[index] = tile.BlocksLight;
            doorStates[index] = tile.IsDoor ? (byte)1 : (byte)0;
        }
    }

    // Create dungeon entity
    var dungeonEntity = world.Create(
        new DungeonMapComponent
        {
            Width = width,
            Height = height,
            TileData = new byte[width * height],
            DoorStates = doorStates,
            Walkable = walkableArray,
            Opaque = opaqueArray
        },
        new PositionComponent { X = 0, Y = 0 },
        new RenderableComponent
        {
            Glyph = ' ',
            Foreground = SadRogue.Primitives.Color.White,
            Background = SadRogue.Primitives.Color.Black
        }
    );

    return dungeonEntity;
}
```

3. **Optional: Keep compatibility method for testing:**

```csharp
// Keep ConvertToDungeonView() for backwards compatibility if needed
private DungeonView ConvertToDungeonView(EdgarResult result)
{
    // ... existing implementation ...
}
```

---

### **Step 1.6: Verify Compilation**

**Build all affected projects:**

```bash
# Build contracts
dotnet build dotnet/game-essential/core/src/PigeonPea.Dungeon.Contracts/

# Build plugins
dotnet build dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Basic/
dotnet build dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.ModernEdgar/
dotnet build dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Rendering/

# Build full solution
dotnet build
```

**Expected Result:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## **Phase 2: Integrate Scene Management (MEDIUM Priority)**

### **Step 2.1: Update Console App to Use Scenes**

**File:** `projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/Program.cs`

**Add Scene-Based Initialization:**

```csharp
// After plugin loading, before main loop

var sceneManager = registry.Get<ISceneManager>();

// Load main gameplay scene
var mainScene = await sceneManager.LoadSceneAsync("MainGameplay", SceneLoadMode.Single);
_logger.LogInformation("Main gameplay scene loaded: {SceneId}", mainScene.Id);

// Get dungeon generator from registry
var dungeonGenerator = registry.Get<IDungeonGenerator>("dungeon-generator-modern-edgar");

// Generate dungeon as entity in the scene's world
var dungeonEntity = dungeonGenerator.Generate(
    mainScene.World,
    new DungeonGenerationOptions
    {
        Width = 80,
        Height = 50,
        Seed = 12345
    }
);

_logger.LogInformation("Dungeon entity created: {EntityId}", dungeonEntity.Id);

// Create player entity in the same world
var playerEntity = mainScene.World.Create(
    new PositionComponent { X = 40, Y = 25 },
    new RenderableComponent
    {
        Glyph = '@',
        Foreground = Color.Yellow,
        Background = Color.Black
    },
    new PlayerInputComponent()
);

_logger.LogInformation("Player entity created: {EntityId}", playerEntity.Id);

// Get domain dungeon renderer
var dungeonRenderer = registry.Get<IDungeonRenderer>();

// Get platform renderer (ANSI/Braille/etc.)
var platformRenderer = registry.Get<IRenderer>("ansi-terminal-renderer");

// Initialize domain renderer with platform renderer
dungeonRenderer.Initialize(platformRenderer);

// Game loop
while (running)
{
    // Handle input (update PlayerInputComponent)
    HandleInput(mainScene.World);

    // Update game logic (movement, AI, etc.)
    // TODO: Implement systems in Phase 3

    // Render
    dungeonRenderer.Render(mainScene.World);

    // Frame delay
    await Task.Delay(16); // ~60 FPS
}
```

---

### **Step 2.2: Add Scene Contracts Reference to Console App**

**File:** `projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/PigeonPea.Console.csproj`

**Add:**
```xml
<ItemGroup>
  <!-- ... existing references ... -->

  <!-- Scene Management -->
  <ProjectReference Include="..\..\..\..\..\..\..\dotnet\game-essential\core\src\PigeonPea.Scene.Contracts\PigeonPea.Scene.Contracts.csproj" />

  <!-- ECS Framework -->
  <PackageReference Include="Arch" Version="2.0.0" />
</ItemGroup>
```

---

### **Step 2.3: Register Scene Manager Plugin**

**File:** `projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/PigeonPea.Console.csproj`

**Add plugin reference:**
```xml
<ItemGroup>
  <!-- ... existing plugin references ... -->

  <!-- Scene Manager Plugin -->
  <ProjectReference Include="..\..\..\..\..\..\..\dotnet\game-essential\plugins\src\PigeonPea.Plugin.Scene.Manager\PigeonPea.Plugin.Scene.Manager.csproj" PrivateAssets="all" ReferenceOutputAssembly="false" />
</ItemGroup>
```

---

### **Step 2.4: Add Player Input Handling**

**Create new file:** `projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/InputHandler.cs`

```csharp
using Arch.Core;
using PigeonPea.Shared.Components;

namespace PigeonPea.Console;

public static class InputHandler
{
    public static void HandleInput(World world)
    {
        if (!Console.KeyAvailable)
            return;

        var key = Console.ReadKey(intercept: true);

        // Query player entity
        var playerQuery = new QueryDescription().WithAll<PlayerInputComponent, PositionComponent>();

        world.Query(in playerQuery, (ref PlayerInputComponent input, ref PositionComponent pos) =>
        {
            var (dx, dy) = key.Key switch
            {
                ConsoleKey.W or ConsoleKey.UpArrow => (0, -1),
                ConsoleKey.S or ConsoleKey.DownArrow => (0, 1),
                ConsoleKey.A or ConsoleKey.LeftArrow => (-1, 0),
                ConsoleKey.D or ConsoleKey.RightArrow => (1, 0),
                _ => (0, 0)
            };

            if (dx != 0 || dy != 0)
            {
                // Check if movement is valid
                if (CanMoveTo(world, pos.X + dx, pos.Y + dy))
                {
                    pos.X += dx;
                    pos.Y += dy;
                }
            }
        });
    }

    private static bool CanMoveTo(World world, int x, int y)
    {
        // Query dungeon map
        var dungeonQuery = new QueryDescription().WithAll<DungeonMapComponent>();
        bool walkable = false;

        world.Query(in dungeonQuery, (ref DungeonMapComponent dungeon) =>
        {
            if (x >= 0 && x < dungeon.Width && y >= 0 && y < dungeon.Height)
            {
                int index = y * dungeon.Width + x;
                walkable = dungeon.Walkable[index];
            }
        });

        return walkable;
    }
}
```

---

## **Phase 3: Add Systems (FUTURE - Optional)**

### **Step 3.1: Create Movement System**

**Create:** `dotnet/game-essential/core/src/PigeonPea.Systems/MovementSystem.cs`

```csharp
using Arch.Core;
using PigeonPea.Shared.Components;

namespace PigeonPea.Systems;

public class MovementSystem : ISystem
{
    public void Update(World world, float deltaTime)
    {
        // Query entities with PlayerInputComponent
        var playerQuery = new QueryDescription()
            .WithAll<PlayerInputComponent, PositionComponent>();

        world.Query(in playerQuery, (ref PlayerInputComponent input, ref PositionComponent pos) =>
        {
            if (input.MoveDirection.X != 0 || input.MoveDirection.Y != 0)
            {
                var newX = pos.X + (int)input.MoveDirection.X;
                var newY = pos.Y + (int)input.MoveDirection.Y;

                if (CanMoveTo(world, newX, newY))
                {
                    pos.X = newX;
                    pos.Y = newY;
                }

                // Clear input after processing
                input.MoveDirection = System.Numerics.Vector2.Zero;
            }
        });
    }

    private bool CanMoveTo(World world, int x, int y)
    {
        var dungeonQuery = new QueryDescription().WithAll<DungeonMapComponent>();
        bool walkable = false;

        world.Query(in dungeonQuery, (ref DungeonMapComponent dungeon) =>
        {
            if (x >= 0 && x < dungeon.Width && y >= 0 && y < dungeon.Height)
            {
                int index = y * dungeon.Width + x;
                walkable = dungeon.Walkable[index];
            }
        });

        return walkable;
    }
}

public interface ISystem
{
    void Update(World world, float deltaTime);
}
```

---

## **Testing Strategy**

### **Unit Tests**

**Create:** `dotnet/game-essential/core/tests/PigeonPea.Scene.Tests/SceneManagerTests.cs`

```csharp
using Xunit;
using PigeonPea.Scene.Contracts;
using PigeonPea.Plugin.Scene.Manager;

namespace PigeonPea.Scene.Tests;

public class SceneManagerTests
{
    [Fact]
    public async Task LoadScene_CreatesNewScene()
    {
        // Arrange
        var sceneManager = new SceneManager();

        // Act
        var scene = await sceneManager.LoadSceneAsync("TestScene", SceneLoadMode.Single);

        // Assert
        Assert.NotNull(scene);
        Assert.Equal("TestScene", scene.Name);
        Assert.NotNull(scene.World);
        Assert.Equal(SceneState.Active, scene.State);
    }

    [Fact]
    public async Task LoadScene_SingleMode_UnloadsOtherScenes()
    {
        // Arrange
        var sceneManager = new SceneManager();
        var scene1 = await sceneManager.LoadSceneAsync("Scene1", SceneLoadMode.Single);

        // Act
        var scene2 = await sceneManager.LoadSceneAsync("Scene2", SceneLoadMode.Single);

        // Assert
        Assert.Single(sceneManager.GetAllScenes());
        Assert.Equal("Scene2", sceneManager.GetActiveScene()?.Name);
    }
}
```

**Create:** `dotnet/game-essential/plugins/tests/PigeonPea.Plugin.Dungeon.Tests/DungeonGeneratorTests.cs`

```csharp
using Xunit;
using Arch.Core;
using PigeonPea.Plugin.Dungeon.Basic;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Shared.Components;

namespace PigeonPea.Plugin.Dungeon.Tests;

public class DungeonGeneratorTests
{
    [Fact]
    public void BasicGenerator_CreatesEntityInWorld()
    {
        // Arrange
        var world = World.Create();
        var generator = new BasicDungeonGenerator();
        var options = new DungeonGenerationOptions { Width = 50, Height = 30 };

        // Act
        var dungeonEntity = generator.Generate(world, options);

        // Assert
        Assert.True(world.IsAlive(dungeonEntity));
        Assert.True(world.Has<DungeonMapComponent>(dungeonEntity));
        Assert.True(world.Has<PositionComponent>(dungeonEntity));
        Assert.True(world.Has<RenderableComponent>(dungeonEntity));
    }

    [Fact]
    public void BasicGenerator_DungeonHasCorrectDimensions()
    {
        // Arrange
        var world = World.Create();
        var generator = new BasicDungeonGenerator();
        var options = new DungeonGenerationOptions { Width = 80, Height = 50 };

        // Act
        var dungeonEntity = generator.Generate(world, options);
        var dungeon = world.Get<DungeonMapComponent>(dungeonEntity);

        // Assert
        Assert.Equal(80, dungeon.Width);
        Assert.Equal(50, dungeon.Height);
        Assert.Equal(80 * 50, dungeon.Walkable.Length);
    }
}
```

### **Integration Tests**

**Create:** `projects/dungeon/dotnet/console-app/core/tests/PigeonPea.Console.Tests/SceneIntegrationTests.cs`

```csharp
using Xunit;
using Arch.Core;
using PigeonPea.Scene.Contracts;
using PigeonPea.Dungeon.Contracts;
using PigeonPea.Shared.Components;

namespace PigeonPea.Console.Tests;

public class SceneIntegrationTests
{
    [Fact]
    public async Task FullStack_SceneWithDungeonAndPlayer()
    {
        // Arrange
        var sceneManager = new SceneManager();
        var generator = new BasicDungeonGenerator();

        // Act - Load scene
        var scene = await sceneManager.LoadSceneAsync("TestGameplay", SceneLoadMode.Single);

        // Act - Generate dungeon
        var dungeonEntity = generator.Generate(scene.World, new DungeonGenerationOptions
        {
            Width = 80,
            Height = 50
        });

        // Act - Create player
        var playerEntity = scene.World.Create(
            new PositionComponent { X = 40, Y = 25 },
            new RenderableComponent { Glyph = '@', Foreground = Color.White },
            new PlayerInputComponent()
        );

        // Assert - Scene exists
        Assert.NotNull(scene);
        Assert.Equal("TestGameplay", scene.Name);

        // Assert - Dungeon entity exists
        Assert.True(scene.World.IsAlive(dungeonEntity));
        var dungeon = scene.World.Get<DungeonMapComponent>(dungeonEntity);
        Assert.Equal(80, dungeon.Width);
        Assert.Equal(50, dungeon.Height);

        // Assert - Player entity exists
        Assert.True(scene.World.IsAlive(playerEntity));
        var playerPos = scene.World.Get<PositionComponent>(playerEntity);
        Assert.Equal(40, playerPos.X);
        Assert.Equal(25, playerPos.Y);

        // Assert - Query works
        var query = new QueryDescription().WithAll<DungeonMapComponent>();
        int dungeonCount = 0;
        scene.World.Query(in query, () => dungeonCount++);
        Assert.Equal(1, dungeonCount);
    }
}
```

---

## **Migration Checklist**

### **Phase 1: Critical Fixes** (Day 1)

- [ ] Update `IDungeonGenerator` interface signature
- [ ] Add `Arch` package to `Dungeon.Contracts`
- [ ] Fix `BasicDungeonGenerator` dependencies
- [ ] Update `BasicDungeonGenerator` to create entities
- [ ] Create `BasicDungeonGeneratorPlugin` wrapper
- [ ] Update `ModernEdgarDungeonGenerator` to create entities
- [ ] Verify all plugins compile
- [ ] Run unit tests

### **Phase 2: Scene Integration** (Day 2)

- [ ] Add Scene.Contracts reference to Console app
- [ ] Add Arch package to Console app
- [ ] Register Scene Manager plugin
- [ ] Update Program.cs to use scenes
- [ ] Create player entity in scene
- [ ] Add input handling
- [ ] Test dungeon generation in scene
- [ ] Test player movement
- [ ] Test rendering from scene world

### **Phase 3: Cleanup** (Day 2-3)

- [ ] Stage all new files for commit
- [ ] Remove duplicate rendering contracts from PigeonPea.Shared
- [ ] Update documentation
- [ ] Run integration tests
- [ ] Performance testing (entity queries)

### **Future Enhancements** (Optional)

- [ ] Implement MovementSystem
- [ ] Implement FOVSystem
- [ ] Add monster entities
- [ ] Add AISystem
- [ ] Implement system pipeline (GameLoop)
- [ ] Scene transitions
- [ ] Multi-scene support (dungeon + UI overlay)

---

## **Troubleshooting**

### **Issue: Compilation Error - "Type 'Entity' not found"**

**Solution:**
```bash
# Add Arch package reference
dotnet add package Arch --version 2.0.0
```

### **Issue: "Interface member not implemented"**

**Solution:**
Ensure method signature exactly matches interface:
```csharp
// Must match:
Entity Generate(World world, DungeonGenerationOptions options)
```

### **Issue: "DungeonMapComponent not found"**

**Solution:**
```bash
# Add reference to PigeonPea.Shared
dotnet add reference path/to/PigeonPea.Shared.csproj
```

### **Issue: Player doesn't move**

**Debug:**
```csharp
// Add logging in InputHandler
_logger.LogDebug("Player at ({X}, {Y}), moving to ({NewX}, {NewY}), Walkable: {Walkable}",
    pos.X, pos.Y, newX, newY, walkable);
```

### **Issue: Rendering shows black screen**

**Debug:**
```csharp
// Check dungeon entity exists
var query = new QueryDescription().WithAll<DungeonMapComponent>();
int count = 0;
world.Query(in query, () => count++);
_logger.LogInformation("Dungeon entities in world: {Count}", count);
```

---

## **Success Criteria**

### **Phase 1 Complete When:**
- ✅ All plugins compile without errors
- ✅ `dotnet build` succeeds for entire solution
- ✅ Unit tests pass

### **Phase 2 Complete When:**
- ✅ Console app starts using scenes
- ✅ Dungeon entity created in scene world
- ✅ Player entity created in scene world
- ✅ Dungeon renders on screen
- ✅ Player '@' visible
- ✅ Player can move with WASD/arrows
- ✅ Walls block movement

### **RFC-014 Fully Implemented When:**
- ✅ All Phase 1 & 2 criteria met
- ✅ Systems (Movement, FOV) implemented
- ✅ Scene transitions work
- ✅ Multi-scene support functional
- ✅ All tests pass
- ✅ Documentation updated

---

## **Appendix A: Component Reference**

### **Required Components**

```csharp
// Position in world
public struct PositionComponent
{
    public int X;
    public int Y;
}

// Visual representation
public struct RenderableComponent
{
    public char Glyph;
    public Color Foreground;
    public Color Background;
}

// Dungeon map data
public struct DungeonMapComponent
{
    public int Width;
    public int Height;
    public byte[] TileData;
    public byte[] DoorStates;
    public System.Collections.BitArray Walkable;
    public System.Collections.BitArray Opaque;
}

// Player marker
public struct PlayerInputComponent
{
    public System.Numerics.Vector2 MoveDirection;
}

// Scene membership
public struct SceneComponent
{
    public Guid SceneId;
    public string SceneName;
}
```

---

## **Appendix B: File Checklist**

### **Files to Modify**

```
✏️  dotnet/game-essential/core/src/PigeonPea.Dungeon.Contracts/IDungeonGenerator.cs
✏️  dotnet/game-essential/core/src/PigeonPea.Dungeon.Contracts/PigeonPea.Dungeon.Contracts.csproj
✏️  dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Basic/BasicDungeonGenerator.cs
✏️  dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Basic/PigeonPea.Plugin.Dungeon.Basic.csproj
✏️  dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.ModernEdgar/ModernEdgarDungeonGenerator.cs
✏️  projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/Program.cs
✏️  projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/PigeonPea.Console.csproj
```

### **Files to Create**

```
➕ dotnet/game-essential/plugins/src/PigeonPea.Plugin.Dungeon.Basic/BasicDungeonGeneratorPlugin.cs
➕ projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/InputHandler.cs
➕ dotnet/game-essential/core/tests/PigeonPea.Scene.Tests/SceneManagerTests.cs
➕ dotnet/game-essential/plugins/tests/PigeonPea.Plugin.Dungeon.Tests/DungeonGeneratorTests.cs
```

---

## **Next Steps**

1. **Review this adjustment plan** with team/stakeholders
2. **Create feature branch:** `feature/rfc014-adjustments`
3. **Execute Phase 1** (Critical Fixes) - Day 1
4. **Test compilation** after each step
5. **Execute Phase 2** (Integration) - Day 2
6. **Run all tests** - Day 2-3
7. **Create PR** with completed implementation
8. **Update RFC-014** status to "Implemented"

---

## **Questions or Issues?**

If you encounter problems during implementation:

1. Check **Troubleshooting** section above
2. Verify all **dependencies** are added correctly
3. Ensure **method signatures** match interfaces exactly
4. Check **namespace imports** (especially `Arch.Core`)
5. Review **git status** to ensure no files left uncommitted

---

**Document Version:** 1.0
**Last Updated:** 2025-11-19
**Author:** Claude Agent (Architecture Implementation)
**Related:** RFC-00014, GUIDE-00001
