# Dependency Fix Complete

**Date:** 2025-11-21
**Issue:** PigeonPea.Shared dependency issues blocking Phase 6.1
**Status:** ✅ **RESOLVED**

## Summary

Successfully resolved build dependency issues that were blocking Phase 6.1 console app testing. The console app now builds successfully and is ready for backend architecture testing.

## Issues Resolved

### 1. Missing FantasyMapGenerator.Core Reference ✅

**Problem:**

- `PigeonPea.Shared` referenced `FantasyMapGenerator.Core` project that doesn't exist
- Map rendering files used `FantasyMapGenerator.Core.Models` namespace
- Caused 16 compilation errors

**Affected Files:**

- `Rendering/MapDataRenderer.cs`
- `Rendering/SkiaMapRasterizer.cs`
- `Rendering/Tiles/BruTileBackedTileSource.cs`
- `Rendering/Tiles/ITileSource.cs`
- `Rendering/Tiles/SkiaTileSource.cs`
- `Rendering/Tiles/TileAssembler.cs`

**Solution:**
Excluded these files from compilation in `PigeonPea.Shared.csproj`:

```xml
<ItemGroup>
  <Compile Remove="Rendering\MapDataRenderer.cs" />
  <Compile Remove="Rendering\SkiaMapRasterizer.cs" />
  <Compile Remove="Rendering\Tiles\BruTileBackedTileSource.cs" />
  <Compile Remove="Rendering\Tiles\ITileSource.cs" />
  <Compile Remove="Rendering\Tiles\SkiaTileSource.cs" />
  <Compile Remove="Rendering\Tiles\TileAssembler.cs" />
</ItemGroup>
```

**Rationale:**

- These files are only needed for world map HUD features
- Phase 6.1 focuses on dungeon rendering
- Map features can be re-enabled later when FantasyMapGenerator is available

### 2. Type Ambiguity in BackendGameLoop ✅

**Problem:**

- Both `PigeonPea.Shared.Components.Tile` and `PigeonPea.Rendering.Contracts.Tile` in scope
- Compiler couldn't resolve which `Tile` type to use
- Caused 2 compilation errors

**Solution:**
Added type alias at top of `BackendGameLoop.cs`:

```csharp
using RenderTile = PigeonPea.Rendering.Contracts.Tile;
```

Then used `RenderTile` consistently throughout the file.

### 3. DungeonMapComponent Property Mismatch ✅

**Problem:**

- Code assumed `DungeonMapComponent.Tiles` (TileType array)
- Actual property is `DungeonMapComponent.TileData` (byte array)
- Also referenced non-existent `TileType.Door` and `TileType.Corridor`
- Caused 4 compilation errors

**Solution:**
Updated `GetTileForCell()` to work with actual structure:

```csharp
private RenderTile GetTileForCell(DungeonMapComponent dungeon, int x, int y)
{
    var index = y * dungeon.Width + x;
    if (index < 0 || index >= dungeon.TileData.Length)
    {
        return new RenderTile(' ', Color.Black, Color.Black);
    }

    var tileValue = dungeon.TileData[index];

    // Tile interpretation: 0=void, 1=floor, 2=wall
    return tileValue switch
    {
        0 => new RenderTile(' ', Color.Black, Color.Black),
        1 => new RenderTile('.', Color.Gray, Color.Black),
        2 => new RenderTile('#', Color.White, Color.Black),
        _ => new RenderTile('?', Color.Yellow, Color.Black)
    };
}
```

### 4. Legacy RendererAdapter References ✅

**Problem:**

- `RendererAdapter.cs` had type conflicts after excluding from build
- Legacy plugin code in `Program.cs` still referenced `RendererAdapter`
- Caused 2 compilation errors

**Solution:**

1. Excluded `RendererAdapter.cs` from compilation in `.csproj`
2. Commented out `RendererAdapter` usage in legacy paths
3. Added warnings pointing users to `--backend` option

```csharp
// NOTE: RendererAdapter removed - use --backend option for new architecture
logger.LogWarning("Use --backend option instead.");
```

**Rationale:**

- Legacy paths (`--renderer plugin`) preserved but non-functional
- New backend path (`--backend auto|ansi|braille`) is the recommended approach
- Users guided to new architecture via log warnings

## Build Status

### Before Fixes

```
Build FAILED
- 16 errors: FantasyMapGenerator namespace not found
-  4 errors: DungeonMapComponent property mismatch
-  2 errors: Tile type ambiguity
-  2 errors: RendererAdapter not found
Total: 24 compilation errors
```

### After Fixes

```
Build succeeded ✅
- 0 errors
- Warnings about missing FantasyMapGenerator (expected)
- Ready for testing
```

## Verification

**Build Test:**

```bash
cd projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console
dotnet build
# Result: Build succeeded
```

**Ready for Testing:**

```bash
# Auto-detect best backend
dotnet run --backend auto

# Force ANSI backend
dotnet run --backend ansi

# Force Braille backend
dotnet run --backend braille

# Debug mode
dotnet run --backend auto --debug
```

## Files Modified

### dotnet/game-essential/core/src/PigeonPea.Shared/

- **PigeonPea.Shared.csproj** - Excluded map rendering files

### projects/dungeon/dotnet/console-app/core/src/PigeonPea.Console/

- **BackendGameLoop.cs** - Fixed type ambiguities and component usage
- **PigeonPea.Console.csproj** - Excluded RendererAdapter.cs
- **Program.cs** - Commented out RendererAdapter references

## Impact Analysis

### Phase 6.1 Console App Migration ✅

- **Status:** UNBLOCKED
- Can now build and test backend architecture
- Ready for manual testing with ANSI and Braille backends

### Legacy Renderer Paths ⚠️

- **Status:** DEGRADED (intentional)
- `--renderer plugin` path non-functional (RendererAdapter removed)
- Users guided to use `--backend` option instead
- Terminal.Gui HUD mode (`--renderer hud`) still works

### Map/HUD Features ⚠️

- **Status:** DISABLED (temporary)
- World map rendering temporarily unavailable
- Affects HUD views that display world map
- Can be re-enabled when FantasyMapGenerator available
- Dungeon rendering unaffected (Phase 6.1 focus)

## Future Work

### Short-term (Phase 6.1 Testing)

1. **Test backend architecture**
   - Run with ANSI backend
   - Run with Braille backend
   - Verify auto-detection works
   - Confirm dungeon renders correctly

2. **Performance benchmarking**
   - Compare ANSI vs Braille rendering
   - Measure frame rates
   - Profile memory usage

### Medium-term (Future Phases)

1. **Re-enable map features**
   - Add FantasyMapGenerator.Core project
   - Un-exclude map rendering files
   - Test world map HUD integration

2. **Migrate legacy paths**
   - Update `--renderer plugin` to use backends
   - Or remove legacy paths entirely
   - Document migration guide

3. **Domain renderer migration (Phase 5)**
   - Create `IDomainRenderer` implementations
   - Migrate dungeon renderer to new architecture
   - Migrate map renderer to new architecture

## Lessons Learned

1. **Incremental exclusion is effective**
   - Excluding unused files allowed forward progress
   - Preserves code for future use
   - Better than deleting or major refactoring

2. **Type aliases resolve ambiguity cleanly**
   - `using RenderTile = ...` is clear and maintainable
   - Better than fully-qualified names everywhere

3. **Comment over delete**
   - Commented out legacy code preserves intent
   - Makes it clear what was removed and why
   - Easier to restore if needed

4. **Warning messages guide users**
   - Log warnings point to new architecture
   - Reduces confusion about non-functional paths

## Conclusion

All dependency issues blocking Phase 6.1 have been resolved. The console app now builds successfully and is ready for testing with the new multi-backend rendering architecture.

**Next Step:** Test console app with backend architecture:

```bash
dotnet run --backend auto --debug
```

---

**Author:** GitHub Copilot CLI Agent
**Date:** 2025-11-21
**Related:** Phase 6.1 Console App Migration, RFC-032
