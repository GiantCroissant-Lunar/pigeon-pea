# RFC-010 Phase 2: Integration with SkiaMapRasterizer

## Overview

**Goal**: Integrate the ColorScheme system (Phase 1) with the map rendering pipeline by modifying `SkiaMapRasterizer` and `MapColor` classes to use the new `ColorSchemes` provider instead of hardcoded colors.

**Estimated Time**: 1.5-2 hours

**Prerequisites**:
- ✅ Phase 1 completed (ColorScheme enum and ColorSchemes class exist)
- ✅ All Phase 1 tests passing
- ✅ `dotnet/Map/PigeonPea.Map.Rendering/ColorScheme.cs` exists
- ✅ `dotnet/Map/PigeonPea.Map.Rendering/ColorSchemes.cs` exists

---

## Architecture Understanding

### Current Flow (Before Phase 2)

```
SkiaMapRasterizer.Render()
  └─> MapColor.ColorForCell(map, cell, biomeColors)
        └─> Returns hardcoded (r, g, b) tuple
              ├─> Water: (10, 90, 220)
              ├─> Mountains: (190, 190, 190)
              ├─> Forest: (34, 139, 34)
              └─> Plains: (46, 160, 60)
```

**Files Involved**:
- `dotnet/Map/PigeonPea.Map.Rendering/SkiaMapRasterizer.cs` (calls MapColor)
- `dotnet/Map/PigeonPea.Map.Core/Domain/MapColor.cs` (contains hardcoded colors)

### New Flow (After Phase 2)

```
SkiaMapRasterizer.Render(colorScheme: ColorScheme.Fantasy)
  └─> MapColor.ColorForCell(map, cell, biomeColors, colorScheme)
        └─> ColorSchemes.GetHeightColor(height, scheme, isBiome, biomeId)
              └─> Returns SKColor based on selected scheme
```

---

## Task Breakdown

### Task 1: Update MapColor.ColorForCell Method ⭐⭐⭐

**File**: `dotnet/Map/PigeonPea.Map.Core/Domain/MapColor.cs`

**Current Code** (lines 8-19):
```csharp
public static (byte r, byte g, byte b) ColorForCell(MapData map, Cell cell, bool biomeColors)
{
    if (cell.Height <= 20) return (10, 90, 220);
    if (biomeColors && cell.Biome >= 0 && cell.Biome < map.Biomes.Count)
    {
        var hex = map.Biomes[cell.Biome].Color;
        return ParseHex(hex, 46, 160, 60);
    }
    if (cell.Height >= 70) return (190, 190, 190);
    if (cell.Height >= 50) return (34, 139, 34);
    return (46, 160, 60);
}
```

**Changes Required**:

#### 1.1: Add Using Directives
Add to top of file (after line 2):
```csharp
using PigeonPea.Map.Rendering; // For ColorScheme enum and ColorSchemes class
using SkiaSharp;                // For SKColor type
```

#### 1.2: Update Method Signature
**OLD**:
```csharp
public static (byte r, byte g, byte b) ColorForCell(MapData map, Cell cell, bool biomeColors)
```

**NEW**:
```csharp
public static (byte r, byte g, byte b) ColorForCell(
    MapData map,
    Cell cell,
    bool biomeColors,
    ColorScheme colorScheme = ColorScheme.Original)
```

**Rationale**:
- Add `colorScheme` parameter with default value `ColorScheme.Original`
- Default preserves backward compatibility (existing callers don't break)
- Keep return type as tuple for now (minimal API change)

#### 1.3: Replace Hardcoded Color Logic

**REPLACE** the entire method body with:

```csharp
public static (byte r, byte g, byte b) ColorForCell(
    MapData map,
    Cell cell,
    bool biomeColors,
    ColorScheme colorScheme = ColorScheme.Original)
{
    // Determine if we should use biome-specific coloring
    bool useBiomeColor = biomeColors &&
                         cell.Biome >= 0 &&
                         cell.Biome < map.Biomes.Count;

    // For biome colors, check if map provides a hex color
    if (useBiomeColor)
    {
        var hex = map.Biomes[cell.Biome].Color;
        if (!string.IsNullOrWhiteSpace(hex))
        {
            // Try to use the biome's custom hex color first
            var parsed = ParseHex(hex, 0, 0, 0);
            if (parsed != (0, 0, 0)) // If parsing succeeded
            {
                return parsed;
            }
        }

        // Fall through to use scheme-based biome colors if no valid hex
    }

    // Use the ColorSchemes provider for consistent coloring
    SKColor color = ColorSchemes.GetHeightColor(
        cell.Height,
        colorScheme,
        isBiome: useBiomeColor,
        biomeId: useBiomeColor ? cell.Biome : -1);

    return (color.Red, color.Green, color.Blue);
}
```

**Key Design Decisions**:
1. **Preserve biome hex colors**: If `map.Biomes[].Color` has a valid hex, use it (respects user-provided colors)
2. **Fallback to schemes**: If no hex or invalid hex, use `ColorSchemes.GetBiomeColor()` for consistency
3. **Height-based rendering**: When `biomeColors=false`, uses `ColorSchemes.GetHeightColor()` with height-based logic
4. **Backward compatible**: Default parameter means existing code doesn't break

#### 1.4: Update ParseHex Fallback (Optional Improvement)

**Current** (line 14):
```csharp
return ParseHex(hex, 46, 160, 60); // Fallback to hardcoded green
```

**After changes**: The fallback is now handled by `ColorSchemes.GetHeightColor()`, so `ParseHex` is only used when biomes have valid hex colors.

**No changes needed** to `ParseHex()` method itself - it stays as-is for hex parsing.

---

### Task 2: Update SkiaMapRasterizer.Render Method ⭐⭐

**File**: `dotnet/Map/PigeonPea.Map.Rendering/SkiaMapRasterizer.cs`

**Current Method Signature** (line 10):
```csharp
public static Raster Render(
    MapData map,
    PigeonPea.Shared.Rendering.Viewport viewport,
    double zoom,
    int ppc,
    bool biomeColors,
    bool rivers,
    double timeSeconds = 0)
```

**Changes Required**:

#### 2.1: Add ColorScheme Parameter

**NEW SIGNATURE**:
```csharp
public static Raster Render(
    MapData map,
    PigeonPea.Shared.Rendering.Viewport viewport,
    double zoom,
    int ppc,
    bool biomeColors,
    bool rivers,
    double timeSeconds = 0,
    ColorScheme colorScheme = ColorScheme.Original) // NEW: Add this line
```

**Rationale**:
- Add as last parameter with default value
- Default to `ColorScheme.Original` for backward compatibility
- Existing callers continue to work without changes

#### 2.2: Pass ColorScheme to MapColor

**FIND** (line 29):
```csharp
(r, g, b) = MapColor.ColorForCell(map, cell, biomeColors);
```

**REPLACE WITH**:
```csharp
(r, g, b) = MapColor.ColorForCell(map, cell, biomeColors, colorScheme);
```

**That's it!** Only one line changes in this file.

---

### Task 3: Add Unit Tests for Integration ⭐⭐⭐

**File**: `dotnet/Map/PigeonPea.Map.Rendering.Tests/MapColorIntegrationTests.cs` (NEW FILE)

**Purpose**: Test that MapColor correctly uses ColorSchemes.

#### 3.1: Create Test File

```csharp
using SkiaSharp;
using Xunit;
using PigeonPea.Map.Core;
using PigeonPea.Map.Rendering;

namespace PigeonPea.Map.Rendering.Tests;

/// <summary>
/// Integration tests for MapColor using ColorScheme system.
/// </summary>
public class MapColorIntegrationTests
{
    [Fact]
    public void ColorForCell_UsesColorScheme_ForWater()
    {
        // Arrange
        var map = CreateMinimalMap();
        var cell = new Cell(0, new Point(0, 0), height: 10); // Water

        // Act
        var (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Original);

        // Assert
        // Water in Original scheme should be blue-dominant
        Assert.True(b > r && b > g, "Water should have blue as dominant color");
    }

    [Fact]
    public void ColorForCell_UsesColorScheme_ForMountains()
    {
        // Arrange
        var map = CreateMinimalMap();
        var cell = new Cell(0, new Point(0, 0), height: 80); // Mountains

        // Act - Test with Fantasy scheme
        var (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Fantasy);

        // Assert
        // Fantasy mountains should have orange/red tones (from ColorSchemes.GetFantasyHeightColor)
        Assert.True(r > 100, "Fantasy mountains should have red component");
    }

    [Fact]
    public void ColorForCell_DefaultScheme_IsOriginal()
    {
        // Arrange
        var map = CreateMinimalMap();
        var cell = new Cell(0, new Point(0, 0), height: 50);

        // Act - Don't specify scheme, use default
        var (r1, g1, b1) = MapColor.ColorForCell(map, cell, biomeColors: false);
        var (r2, g2, b2) = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Original);

        // Assert - Default should match Original scheme
        Assert.Equal((r1, g1, b1), (r2, g2, b2));
    }

    [Fact]
    public void ColorForCell_DifferentSchemes_ProduceDifferentColors()
    {
        // Arrange
        var map = CreateMinimalMap();
        var cell = new Cell(0, new Point(0, 0), height: 50);

        // Act
        var original = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Original);
        var fantasy = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Fantasy);
        var realistic = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Realistic);

        // Assert - At least two should differ
        bool hasDifferences = original != fantasy || fantasy != realistic || original != realistic;
        Assert.True(hasDifferences, "Different color schemes should produce different colors");
    }

    [Fact]
    public void ColorForCell_BiomeWithHexColor_UsesHex()
    {
        // Arrange
        var map = CreateMinimalMap();
        map.Biomes.Add(new Biome { Id = 0, Name = "CustomBiome", Color = "#FF0000" }); // Red
        var cell = new Cell(0, new Point(0, 0), height: 50, biome: 0);

        // Act
        var (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors: true, ColorScheme.Fantasy);

        // Assert - Should use hex color, not scheme color
        Assert.Equal(255, r);
        Assert.Equal(0, g);
        Assert.Equal(0, b);
    }

    [Fact]
    public void ColorForCell_BiomeWithoutHexColor_UsesScheme()
    {
        // Arrange
        var map = CreateMinimalMap();
        map.Biomes.Add(new Biome { Id = 0, Name = "NaturalBiome", Color = null }); // No hex
        var cell = new Cell(0, new Point(0, 0), height: 50, biome: 0);

        // Act
        var (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors: true, ColorScheme.Fantasy);

        // Assert - Should use ColorSchemes.GetBiomeColor() for biome 0
        var expectedColor = ColorSchemes.GetHeightColor(50, ColorScheme.Fantasy, isBiome: true, biomeId: 0);
        Assert.Equal((expectedColor.Red, expectedColor.Green, expectedColor.Blue), (r, g, b));
    }

    [Fact]
    public void ColorForCell_Monochrome_ProducesGrayscale()
    {
        // Arrange
        var map = CreateMinimalMap();
        var cell = new Cell(0, new Point(0, 0), height: 128);

        // Act
        var (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Monochrome);

        // Assert
        Assert.Equal(r, g);
        Assert.Equal(g, b);
    }

    // Helper method to create minimal MapData
    private static MapData CreateMinimalMap()
    {
        return new MapData
        {
            Seed = 12345,
            Info = new MapInfo { Width = 100, Height = 100 },
            Cells = new List<Cell>(),
            Biomes = new List<Biome>()
        };
    }
}
```

**Test Coverage**:
- ✅ Water colors use scheme
- ✅ Mountain colors use scheme
- ✅ Default parameter works
- ✅ Different schemes produce different colors
- ✅ Biome hex colors are preserved
- ✅ Biome fallback uses scheme
- ✅ Monochrome produces grayscale

#### 3.2: Verify MapData and Cell Constructors

**IMPORTANT**: The test code above assumes `MapData`, `Cell`, and `Biome` classes have appropriate constructors/initializers. If they don't, you'll need to adjust the `CreateMinimalMap()` helper.

**Check**:
```csharp
// Does this compile?
var cell = new Cell(id: 0, center: new Point(0, 0), height: 50, biome: 0);
```

If not, use the actual constructor/property initialization pattern from the codebase.

---

### Task 4: Manual Testing and Validation ⭐

#### 4.1: Build and Run Tests

```bash
# Navigate to solution root
cd dotnet

# Build solution
dotnet build

# Run all Map.Rendering tests
dotnet test Map/PigeonPea.Map.Rendering.Tests/

# Expected: All tests pass (including 7 new integration tests)
```

**Expected Output**:
```
Passed! - Failed: 0, Passed: 16+, Skipped: 0
```

#### 4.2: Visual Verification (Optional but Recommended)

**Create a simple console test harness** to visually verify color changes:

**File**: `dotnet/Map/PigeonPea.Map.Rendering.Tests/ManualColorSchemeTest.cs`

```csharp
using PigeonPea.Map.Core;
using PigeonPea.Map.Rendering;
using System;

namespace PigeonPea.Map.Rendering.Tests;

/// <summary>
/// Manual test to visualize different color schemes.
/// Run with: dotnet test --filter "FullyQualifiedName~ManualColorSchemeTest"
/// </summary>
public class ManualColorSchemeTest
{
    // [Fact] // Uncomment to run manually
    public void PrintColorSchemeComparison()
    {
        var map = new MapData
        {
            Seed = 12345,
            Info = new MapInfo { Width = 100, Height = 100 },
            Cells = new List<Cell>(),
            Biomes = new List<Biome>()
        };

        Console.WriteLine("Color Scheme Comparison");
        Console.WriteLine("======================");
        Console.WriteLine();

        // Test different heights across all schemes
        byte[] testHeights = { 10, 25, 40, 60, 80, 100 };
        string[] heightLabels = { "Water", "Beach", "Lowlands", "Hills", "Mountains", "Peaks" };

        foreach (ColorScheme scheme in Enum.GetValues(typeof(ColorScheme)))
        {
            Console.WriteLine($"--- {scheme} Scheme ---");
            for (int i = 0; i < testHeights.Length; i++)
            {
                var cell = new Cell(0, new Point(0, 0), testHeights[i]);
                var (r, g, b) = MapColor.ColorForCell(map, cell, false, scheme);
                Console.WriteLine($"  {heightLabels[i],10} ({testHeights[i],3}): RGB({r,3}, {g,3}, {b,3})");
            }
            Console.WriteLine();
        }
    }
}
```

**Run manually** by uncommenting `[Fact]` and running:
```bash
dotnet test --filter "FullyQualifiedName~ManualColorSchemeTest"
```

**Expected Output Example**:
```
--- Original Scheme ---
     Water ( 10): RGB(  0, 119, 190)
     Beach ( 25): RGB(238, 203, 173)
  Lowlands ( 40): RGB( 34, 139,  34)
     Hills ( 60): RGB( 85, 107,  47)
 Mountains ( 80): RGB(139,  90,  43)
     Peaks (100): RGB(255, 255, 255)

--- Fantasy Scheme ---
     Water ( 10): RGB(  0, 150, 255)
     Beach ( 25): RGB(255, 220, 150)
  Lowlands ( 40): RGB( 50, 255,  50)
     Hills ( 60): RGB(150, 255, 100)
 Mountains ( 80): RGB(255, 150,  50)
     Peaks (100): RGB(255, 100, 255)
```

---

### Task 5: Update Project References (If Needed) ⭐

**Check**: Does `PigeonPea.Map.Core` project reference `PigeonPea.Map.Rendering`?

#### 5.1: Verify Project Dependency

```bash
# Read Map.Core project file
cat dotnet/Map/PigeonPea.Map.Core/PigeonPea.Map.Core.csproj
```

**Check for**:
```xml
<ProjectReference Include="..\PigeonPea.Map.Rendering\PigeonPea.Map.Rendering.csproj" />
```

**If MISSING**, add it:

**File**: `dotnet/Map/PigeonPea.Map.Core/PigeonPea.Map.Core.csproj`

```xml
<ItemGroup>
  <ProjectReference Include="..\PigeonPea.Map.Rendering\PigeonPea.Map.Rendering.csproj" />
</ItemGroup>
```

**HOWEVER**: This creates a **potential circular dependency**:
- `Map.Rendering` depends on `Map.Core` (for MapData, Cell types)
- `Map.Core` would depend on `Map.Rendering` (for ColorScheme enum)

#### 5.2: **Recommended Solution - Move ColorScheme to Map.Core**

**To avoid circular dependency**, consider moving `ColorScheme.cs` to `Map.Core`:

**Option A: Move ColorScheme Enum Only**
```
FROM: dotnet/Map/PigeonPea.Map.Rendering/ColorScheme.cs
  TO: dotnet/Map/PigeonPea.Map.Core/Domain/ColorScheme.cs
```

Update namespace:
```csharp
namespace PigeonPea.Map.Core; // Changed from PigeonPea.Map.Rendering
```

**Keep** `ColorSchemes.cs` in `Map.Rendering` (it has SkiaSharp dependency).

**Update** `ColorSchemes.cs`:
```csharp
using PigeonPea.Map.Core; // For ColorScheme enum
```

**Option B: Accept the Dependency**
If `Map.Core` is truly domain-only and `Map.Rendering` is a higher-level layer, then `Map.Core` referencing `Map.Rendering` violates clean architecture. **Option A is preferred**.

**Decision**: Implement **Option A** to maintain clean dependencies.

---

## Implementation Checklist

### Pre-Implementation Checks
- [ ] Phase 1 complete (ColorScheme and ColorSchemes exist)
- [ ] All Phase 1 tests passing
- [ ] Git branch created: `claude/rfc-010-phase-2-integration-<session-id>`

### Implementation Steps
- [ ] **Task 1**: Update `MapColor.ColorForCell()` method
  - [ ] Add using directives
  - [ ] Add `colorScheme` parameter with default
  - [ ] Replace hardcoded color logic with `ColorSchemes.GetHeightColor()`
  - [ ] Preserve biome hex color fallback
- [ ] **Task 2**: Update `SkiaMapRasterizer.Render()` method
  - [ ] Add `colorScheme` parameter to signature
  - [ ] Pass `colorScheme` to `MapColor.ColorForCell()`
- [ ] **Task 3**: Create `MapColorIntegrationTests.cs`
  - [ ] Add 7+ integration tests
  - [ ] Verify MapData/Cell constructors work
- [ ] **Task 4**: Run tests and verify
  - [ ] `dotnet build` succeeds
  - [ ] All tests pass
  - [ ] Optional: Run manual visual test
- [ ] **Task 5**: Handle project references
  - [ ] Move `ColorScheme.cs` to `Map.Core` (recommended)
  - [ ] Update namespaces
  - [ ] Rebuild and retest

### Post-Implementation Validation
- [ ] No circular dependencies
- [ ] All existing code still compiles (backward compatible)
- [ ] No breaking changes to public APIs
- [ ] Pre-commit hooks pass
- [ ] Git commit with message: `feat(map-rendering): integrate ColorScheme system with map rasterizer (RFC-010 Phase 2)`

---

## Success Criteria

### Functional
- ✅ `SkiaMapRasterizer.Render()` accepts `colorScheme` parameter
- ✅ Different schemes produce visually distinct outputs
- ✅ Default scheme (Original) matches previous hardcoded colors
- ✅ Biome hex colors still work (backward compatible)
- ✅ All 6 schemes render without errors

### Technical
- ✅ No circular dependencies between projects
- ✅ All unit tests pass (Phase 1 + Phase 2 integration tests)
- ✅ No performance regression (color lookup is O(1))
- ✅ Code follows project style (.editorconfig Allman style)

### Code Quality
- ✅ XML documentation on modified public methods
- ✅ No compiler warnings
- ✅ Pre-commit hooks pass (formatting, linting, secrets)
- ✅ Test coverage ≥85% on modified code

---

## Troubleshooting

### Issue 1: Circular Dependency Error
```
error CS0246: The type or namespace name 'ColorScheme' could not be found
```

**Solution**: Move `ColorScheme.cs` to `Map.Core` as described in Task 5.2.

### Issue 2: Cell/MapData Constructor Errors in Tests
```
error CS7036: There is no argument given that corresponds to required parameter
```

**Solution**: Check actual `Cell` and `MapData` constructors in the codebase and adjust test helper methods accordingly.

### Issue 3: SkiaSharp Not Found in Map.Core
```
error CS0246: The type or namespace name 'SKColor' could not be found
```

**Solution**: Don't use `SKColor` directly in `Map.Core`. Convert to tuple `(byte, byte, byte)` in `MapColor.ColorForCell()`.

### Issue 4: Tests Fail Due to Color Mismatch
```
Assert.Equal() Failure: Expected: (34, 139, 34), Actual: (50, 255, 50)
```

**Solution**: Verify you're testing the correct color scheme. Original vs Fantasy have different colors.

---

## Next Steps (Phase 3)

After Phase 2 completes:
1. **Phase 3**: Add `ColorScheme` property to `MapRenderViewModel` in `Map.Control`
2. **Phase 4**: Add UI controls (Terminal.Gui + Avalonia) for scheme selection
3. **Phase 5**: Add persistence and documentation

---

## Appendix: Quick Reference

### Files Modified in Phase 2
| File | Location | Changes |
|------|----------|---------|
| `MapColor.cs` | `Map.Core/Domain/` | Add colorScheme param, use ColorSchemes |
| `SkiaMapRasterizer.cs` | `Map.Rendering/` | Add colorScheme param, pass to MapColor |
| `ColorScheme.cs` | Move to `Map.Core/Domain/` | Change namespace (optional) |
| `MapColorIntegrationTests.cs` | `Map.Rendering.Tests/` | NEW FILE - 7 tests |

### Command Quick Reference
```bash
# Build solution
dotnet build

# Run tests
dotnet test Map/PigeonPea.Map.Rendering.Tests/

# Run specific test
dotnet test --filter "FullyQualifiedName~ColorForCell_UsesColorScheme"

# Pre-commit hooks
pre-commit run --all-files

# Git commit (after tests pass)
git add .
git commit -m "feat(map-rendering): integrate ColorScheme system with map rasterizer (RFC-010 Phase 2)"
```

### Color Scheme Reference
| Scheme | Water | Mountains | Aesthetic |
|--------|-------|-----------|-----------|
| Original | Blue | Gray/White | Balanced fantasy |
| Realistic | Dark Blue | Brown/Gray | Satellite-like |
| Fantasy | Bright Blue | Orange/Magenta | Vibrant/dramatic |
| HighContrast | Dark Blue | Brown/White | Accessibility |
| Monochrome | Dark Gray | Light Gray | Grayscale |
| Parchment | Dark Brown | Cream | Antique map |

---

**Document Version**: 1.0
**Last Updated**: 2025-11-13
**Author**: Claude Code (RFC-010 Implementation Guide)
