# RFC-010 Phase 2: Detailed Implementation Plan

## Executive Summary

**Goal**: Integrate the ColorScheme system with `SkiaMapRasterizer` and `MapColor` to replace hardcoded colors with the new color scheme provider.

**Estimated Time**: 2-3 hours

**Status**: Phase 1 COMPLETED ✅

- `ColorScheme.cs` exists in `Map.Core/Domain/`
- `ColorSchemes.cs` exists in `Map.Rendering/`
- `ColorSchemesTests.cs` complete with 15+ tests, all passing

---

## Phase 2 Tasks Overview

| Task       | File(s)                             | Priority | Est. Time | Dependencies |
| ---------- | ----------------------------------- | -------- | --------- | ------------ |
| **Task 1** | `MapColor.cs`                       | P0       | 30 min    | Phase 1 ✅   |
| **Task 2** | `SkiaMapRasterizer.cs`              | P0       | 20 min    | Task 1       |
| **Task 3** | `MapColorIntegrationTests.cs` (new) | P0       | 45 min    | Task 1, 2    |
| **Task 4** | Build/Test validation               | P0       | 15 min    | All above    |
| **Task 5** | Pre-commit hooks                    | P0       | 10 min    | All above    |

**Total**: ~2 hours

---

## Task 1: Update MapColor.ColorForCell ⭐⭐⭐

### Current State

**File**: `dotnet/Map/PigeonPea.Map.Core/Domain/MapColor.cs`

**Current Implementation** (approximate):

```csharp
namespace PigeonPea.Map.Core.Domain;

public static class MapColor
{
    public static (byte r, byte g, byte b) ColorForCell(MapData map, Cell cell, bool biomeColors)
    {
        // Hardcoded color logic:
        if (cell.Height <= 20) return (10, 90, 220);    // Water
        if (biomeColors && cell.Biome >= 0 && cell.Biome < map.Biomes.Count)
        {
            var hex = map.Biomes[cell.Biome].Color;
            return ParseHex(hex, 46, 160, 60);
        }
        if (cell.Height >= 70) return (190, 190, 190);  // Mountains
        if (cell.Height >= 50) return (34, 139, 34);    // Forest
        return (46, 160, 60);                            // Plains
    }

    private static (byte, byte, byte) ParseHex(string hex, byte defR, byte defG, byte defB)
    {
        // ... hex parsing logic ...
    }
}
```

### Changes Required

#### Step 1.1: Add Using Directives

**Add** at top of file (after namespace):

```csharp
using PigeonPea.Map.Rendering; // For ColorScheme enum and ColorSchemes class
using SkiaSharp;                // For SKColor type
```

#### Step 1.2: Update Method Signature

**Replace**:

```csharp
public static (byte r, byte g, byte b) ColorForCell(MapData map, Cell cell, bool biomeColors)
```

**With**:

```csharp
public static (byte r, byte g, byte b) ColorForCell(
    MapData map,
    Cell cell,
    bool biomeColors,
    ColorScheme colorScheme = ColorScheme.Original)
```

**Design Decision**: Default parameter `= ColorScheme.Original` ensures backward compatibility.

#### Step 1.3: Replace Method Body

**Replace entire method body** with:

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

    // For biome colors, check if map provides a custom hex color
    if (useBiomeColor)
    {
        var biome = map.Biomes[cell.Biome];
        if (!string.IsNullOrWhiteSpace(biome.Color))
        {
            // Try to parse the biome's custom hex color
            var parsed = ParseHex(biome.Color, 0, 0, 0);
            if (parsed != (0, 0, 0)) // If parsing succeeded (non-black)
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

**Key Logic**:

1. **Preserve custom biome hex colors**: If user provided a hex color in `map.Biomes[].Color`, use it
2. **Fallback to ColorSchemes**: If no hex or invalid, use scheme-based colors
3. **Height-based rendering**: When `biomeColors=false`, pure height-based coloring
4. **Backward compatible**: Existing code calling without `colorScheme` parameter continues to work

#### Step 1.4: Keep ParseHex Method Unchanged

The `ParseHex()` helper method should remain unchanged. It's still needed for parsing custom biome hex colors.

### Validation Checklist for Task 1

- [ ] Using directives added (`PigeonPea.Map.Rendering`, `SkiaSharp`)
- [ ] Method signature has `colorScheme` parameter with default
- [ ] Hardcoded color values removed (no `(10, 90, 220)`, etc.)
- [ ] Biome hex color logic preserved
- [ ] `ColorSchemes.GetHeightColor()` called correctly
- [ ] ParseHex method still exists (unchanged)
- [ ] File compiles without errors

---

## Task 2: Update SkiaMapRasterizer.Render ⭐⭐

### Current State

**File**: `dotnet/Map/PigeonPea.Map.Rendering/SkiaMapRasterizer.cs`

**Current Method Signature** (approximate line 10):

```csharp
public static Raster Render(
    MapData map,
    Viewport viewport,
    double zoom,
    int ppc,
    bool biomeColors,
    bool rivers,
    double timeSeconds = 0)
{
    // ... rendering logic ...

    // Line ~29: Call to MapColor
    (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors);

    // ... rest of rendering ...
}
```

### Changes Required

#### Step 2.1: Add ColorScheme Parameter

**Update method signature** by adding parameter at end:

```csharp
public static Raster Render(
    MapData map,
    Viewport viewport,
    double zoom,
    int ppc,
    bool biomeColors,
    bool rivers,
    double timeSeconds = 0,
    ColorScheme colorScheme = ColorScheme.Original) // NEW: Add this line
```

**Rationale**:

- Place at end to minimize breaking changes
- Default value preserves backward compatibility
- Matches parameter pattern from Task 1

#### Step 2.2: Pass ColorScheme to MapColor.ColorForCell

**Find** (approximate line 29):

```csharp
(r, g, b) = MapColor.ColorForCell(map, cell, biomeColors);
```

**Replace with**:

```csharp
(r, g, b) = MapColor.ColorForCell(map, cell, biomeColors, colorScheme);
```

**That's the only line that changes in this method!**

### Validation Checklist for Task 2

- [ ] Method signature has `colorScheme` parameter
- [ ] Default value is `ColorScheme.Original`
- [ ] Call to `MapColor.ColorForCell()` includes `colorScheme` argument
- [ ] No other logic changes
- [ ] File compiles without errors

---

## Task 3: Create MapColorIntegrationTests ⭐⭐⭐

### Create New Test File

**File**: `dotnet/Map/PigeonPea.Map.Rendering.Tests/MapColorIntegrationTests.cs` (NEW)

**Purpose**: Test that `MapColor` correctly uses the `ColorSchemes` system.

### Test Implementation

```csharp
using SkiaSharp;
using Xunit;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Domain;
using PigeonPea.Map.Rendering;

namespace PigeonPea.Map.Rendering.Tests;

/// <summary>
/// Integration tests for MapColor using ColorScheme system.
/// Verifies that MapColor.ColorForCell correctly delegates to ColorSchemes.
/// </summary>
public class MapColorIntegrationTests
{
    #region Scheme Selection Tests

    [Fact]
    public void ColorForCell_DefaultScheme_IsOriginal()
    {
        // Arrange
        var map = CreateMinimalMap();
        var cell = CreateCell(height: 50);

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
        var cell = CreateCell(height: 50);

        // Act
        var original = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Original);
        var fantasy = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Fantasy);
        var realistic = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Realistic);

        // Assert - At least two should differ
        bool hasDifferences = original != fantasy || fantasy != realistic || original != realistic;
        Assert.True(hasDifferences, "Different color schemes should produce different colors");
    }

    #endregion

    #region Terrain Type Tests

    [Fact]
    public void ColorForCell_Water_HasBlueAsDominantColor()
    {
        // Arrange
        var map = CreateMinimalMap();
        var cell = CreateCell(height: 10); // Water

        // Act
        var (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Original);

        // Assert - Water should have blue as dominant color
        Assert.True(b > r && b > g, $"Water should have blue dominant. Got RGB({r}, {g}, {b})");
    }

    [Fact]
    public void ColorForCell_Mountains_ProducesExpectedColor()
    {
        // Arrange
        var map = CreateMinimalMap();
        var cell = CreateCell(height: 80); // Mountains

        // Act - Test with Fantasy scheme
        var (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Fantasy);

        // Assert - Fantasy mountains should have warm tones
        Assert.True(r > 100 || g > 100, $"Fantasy mountains should have warm tones. Got RGB({r}, {g}, {b})");
    }

    [Fact]
    public void ColorForCell_Beach_ProducesSandyColors()
    {
        // Arrange
        var map = CreateMinimalMap();
        var cell = CreateCell(height: 25); // Beach

        // Act
        var (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Original);

        // Assert - Beach should have sandy colors (high R and G)
        Assert.True(r > 150 && g > 150, $"Beach should have sandy colors. Got RGB({r}, {g}, {b})");
    }

    #endregion

    #region Biome Color Tests

    [Fact]
    public void ColorForCell_BiomeWithHexColor_UsesHexColor()
    {
        // Arrange
        var map = CreateMinimalMap();
        map.Biomes.Add(new Biome { Id = 0, Name = "CustomBiome", Color = "#FF0000" }); // Red
        var cell = CreateCell(height: 50, biome: 0);

        // Act
        var (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors: true, ColorScheme.Fantasy);

        // Assert - Should use hex color, not scheme color
        Assert.Equal(255, r);
        Assert.Equal(0, g);
        Assert.Equal(0, b);
    }

    [Fact]
    public void ColorForCell_BiomeWithoutHexColor_UsesSchemeColor()
    {
        // Arrange
        var map = CreateMinimalMap();
        map.Biomes.Add(new Biome { Id = 0, Name = "NaturalBiome", Color = null }); // No hex
        var cell = CreateCell(height: 50, biome: 0);

        // Act
        var (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors: true, ColorScheme.Fantasy);

        // Assert - Should use ColorSchemes.GetBiomeColor() for biome 0
        var expectedColor = ColorSchemes.GetHeightColor(50, ColorScheme.Fantasy, isBiome: true, biomeId: 0);
        Assert.Equal((expectedColor.Red, expectedColor.Green, expectedColor.Blue), (r, g, b));
    }

    [Fact]
    public void ColorForCell_BiomeWithEmptyHexColor_UsesSchemeColor()
    {
        // Arrange
        var map = CreateMinimalMap();
        map.Biomes.Add(new Biome { Id = 1, Name = "EmptyBiome", Color = "" }); // Empty string
        var cell = CreateCell(height: 40, biome: 1);

        // Act
        var (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors: true, ColorScheme.Realistic);

        // Assert - Should fallback to scheme-based biome color
        var expectedColor = ColorSchemes.GetHeightColor(40, ColorScheme.Realistic, isBiome: true, biomeId: 1);
        Assert.Equal((expectedColor.Red, expectedColor.Green, expectedColor.Blue), (r, g, b));
    }

    [Fact]
    public void ColorForCell_BiomeColorsDisabled_UsesHeightColor()
    {
        // Arrange
        var map = CreateMinimalMap();
        map.Biomes.Add(new Biome { Id = 0, Name = "Forest", Color = "#00FF00" }); // Green
        var cell = CreateCell(height: 50, biome: 0);

        // Act - biomeColors: false
        var (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Original);

        // Assert - Should use height-based color, NOT biome color
        var expectedColor = ColorSchemes.GetHeightColor(50, ColorScheme.Original, isBiome: false, biomeId: -1);
        Assert.Equal((expectedColor.Red, expectedColor.Green, expectedColor.Blue), (r, g, b));
    }

    #endregion

    #region Monochrome Tests

    [Fact]
    public void ColorForCell_Monochrome_ProducesGrayscale()
    {
        // Arrange
        var map = CreateMinimalMap();
        var cell = CreateCell(height: 128);

        // Act
        var (r, g, b) = MapColor.ColorForCell(map, cell, biomeColors: false, ColorScheme.Monochrome);

        // Assert
        Assert.Equal(r, g);
        Assert.Equal(g, b);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a minimal MapData for testing.
    /// </summary>
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

    /// <summary>
    /// Creates a test Cell with specified properties.
    /// </summary>
    private static Cell CreateCell(byte height, int biome = -1)
    {
        var cell = new Cell(
            id: 0,
            center: new Point(0, 0),
            height: height
        );

        // Set biome if specified (may require reflection or property setter depending on Cell implementation)
        if (biome >= 0)
        {
            // Adjust based on actual Cell API - this is a placeholder
            // Option 1: If Cell has a mutable Biome property:
            // cell.Biome = biome;

            // Option 2: If Cell is immutable, may need to use constructor or with-expression
            // This will need adjustment based on actual Cell implementation
            typeof(Cell).GetProperty("Biome")?.SetValue(cell, biome);
        }

        return cell;
    }

    #endregion
}
```

### Important Notes on Cell/MapData Construction

**⚠️ CRITICAL**: The test assumes certain constructors/properties for `Cell`, `MapData`, and `Biome`. You may need to adjust:

1. **Cell construction**: Check actual constructor signature
2. **Biome property**: Check if `Cell.Biome` is mutable or requires constructor parameter
3. **MapData initialization**: Verify required properties

**Action**: Before running tests, verify the actual API of these types and adjust helper methods accordingly.

### Validation Checklist for Task 3

- [ ] New file created: `MapColorIntegrationTests.cs`
- [ ] All 10 test methods implemented
- [ ] Helper methods (`CreateMinimalMap`, `CreateCell`) work with actual types
- [ ] Using directives correct
- [ ] File compiles without errors

---

## Task 4: Build and Test Validation ⭐

### Step 4.1: Build Solution

```bash
cd D:\lunar-snake\personal-work\yokan-projects\pigeon-pea\dotnet
dotnet build
```

**Expected**: No compilation errors.

**If errors**: Check Task 1 and 2 changes for syntax/reference issues.

### Step 4.2: Run Existing Tests (Regression Check)

```bash
dotnet test Map/PigeonPea.Map.Rendering.Tests/ --filter "FullyQualifiedName~ColorSchemesTests"
```

**Expected**: All Phase 1 tests still pass (15+ tests).

**If failures**: Phase 1 implementation may have broken. Review ColorSchemes.cs.

### Step 4.3: Run New Integration Tests

```bash
dotnet test Map/PigeonPea.Map.Rendering.Tests/ --filter "FullyQualifiedName~MapColorIntegrationTests"
```

**Expected**: All 10 new tests pass.

**Common Failures**:

- **Constructor errors**: Adjust `CreateCell()` helper to match actual Cell API
- **Color mismatches**: Verify ColorSchemes implementation matches expected values
- **Biome property access**: May need to adjust how `cell.Biome` is set in tests

### Step 4.4: Run All Tests

```bash
dotnet test Map/PigeonPea.Map.Rendering.Tests/
```

**Expected**: 25+ tests pass, 0 failures.

### Validation Checklist for Task 4

- [ ] Solution builds successfully
- [ ] ColorSchemesTests (Phase 1) all pass
- [ ] MapColorIntegrationTests (Phase 2) all pass
- [ ] No test failures or warnings
- [ ] Build output shows no analyzer warnings

---

## Task 5: Pre-commit Hooks and Commit ⭐

### Step 5.1: Run Pre-commit Hooks

```bash
pre-commit run --all-files
```

**Expected**: All hooks pass (formatting, linting, secrets detection).

**Common Issues**:

- **Formatting**: Run `dotnet format` if needed
- **Line endings**: Ensure CRLF/LF matches .editorconfig
- **Trailing whitespace**: Auto-fixed by pre-commit

### Step 5.2: Stage Changes

```bash
git add dotnet/Map/PigeonPea.Map.Core/Domain/MapColor.cs
git add dotnet/Map/PigeonPea.Map.Rendering/SkiaMapRasterizer.cs
git add dotnet/Map/PigeonPea.Map.Rendering.Tests/MapColorIntegrationTests.cs
```

### Step 5.3: Commit with Conventional Commit Message

```bash
git commit -m "feat(map-rendering): integrate ColorScheme system with map rasterizer (RFC-010 Phase 2)

- Update MapColor.ColorForCell to accept colorScheme parameter
- Replace hardcoded colors with ColorSchemes.GetHeightColor() calls
- Preserve custom biome hex colors for backward compatibility
- Add colorScheme parameter to SkiaMapRasterizer.Render()
- Add 10 integration tests for MapColor color scheme usage
- All tests passing (25+ total in Map.Rendering.Tests)

Refs: RFC-010 Phase 2
Co-Authored-By: Claude <noreply@anthropic.com>"
```

### Validation Checklist for Task 5

- [ ] Pre-commit hooks pass
- [ ] All modified files staged
- [ ] Commit message follows conventional commits format
- [ ] Commit includes co-author attribution

---

## Success Criteria Summary

### Functional Requirements ✅

- [ ] `MapColor.ColorForCell()` accepts `colorScheme` parameter with default
- [ ] Hardcoded colors removed from `MapColor.cs`
- [ ] `SkiaMapRasterizer.Render()` accepts `colorScheme` parameter
- [ ] Different schemes produce visually distinct outputs
- [ ] Default scheme (Original) produces same colors as before (backward compatible)
- [ ] Custom biome hex colors still work

### Technical Requirements ✅

- [ ] No circular dependencies between projects
- [ ] All Phase 1 tests still pass (ColorSchemesTests)
- [ ] All Phase 2 tests pass (MapColorIntegrationTests)
- [ ] Solution builds without errors
- [ ] No compiler warnings
- [ ] Pre-commit hooks pass

### Code Quality ✅

- [ ] XML documentation on public methods
- [ ] Follows .editorconfig style (Allman braces)
- [ ] No hardcoded "magic numbers" for colors
- [ ] Test coverage ≥85% on modified code

---

## Risk Mitigation

### Risk 1: Cell/Biome API Mismatch

**Symptom**: Tests fail to compile due to constructor/property errors.

**Mitigation**:

1. Read actual `Cell.cs` and `Biome.cs` source files
2. Adjust `CreateCell()` helper in tests to match actual API
3. May need to use reflection or alternative construction pattern

### Risk 2: ParseHex Fallback Issues

**Symptom**: Biome colors not working as expected.

**Mitigation**:

1. Verify `ParseHex()` returns `(0, 0, 0)` for invalid hex
2. Test with both valid hex (`#FF0000`) and invalid hex
3. Add test case for invalid hex fallback to scheme colors

### Risk 3: Performance Regression

**Symptom**: Rendering is slower than before.

**Mitigation**:

1. `ColorSchemes.GetHeightColor()` uses switch expressions (O(1))
2. No caching needed - lookup is fast
3. If concerned, add benchmark test (not in scope for Phase 2)

---

## Next Steps After Phase 2

Once Phase 2 is complete:

1. **Phase 3**: Add `ColorScheme` property to `MapViewModel` in `Map.Control`
   - Wire up ReactiveUI property change notifications
   - Ensure changes trigger re-render

2. **Phase 4**: Add UI controls for scheme selection
   - Terminal.Gui: Add ComboBox in settings/HUD
   - Avalonia: Add ComboBox in parameters panel

3. **Phase 5**: Add persistence and documentation
   - Save selected scheme to config file
   - Add user documentation with screenshots

---

## Appendix: Quick Command Reference

```bash
# Build solution
dotnet build

# Run all Map.Rendering tests
dotnet test Map/PigeonPea.Map.Rendering.Tests/

# Run specific test class
dotnet test --filter "FullyQualifiedName~MapColorIntegrationTests"

# Run single test
dotnet test --filter "FullyQualifiedName~ColorForCell_DefaultScheme_IsOriginal"

# Format code
dotnet format

# Pre-commit hooks
pre-commit run --all-files

# Stage and commit
git add .
git commit -m "feat(map-rendering): integrate ColorScheme system (RFC-010 Phase 2)"
```

---

## Appendix: File Locations Reference

| File                          | Path                                        | Purpose                       |
| ----------------------------- | ------------------------------------------- | ----------------------------- |
| `MapColor.cs`                 | `dotnet/Map/PigeonPea.Map.Core/Domain/`     | Task 1: Add colorScheme param |
| `SkiaMapRasterizer.cs`        | `dotnet/Map/PigeonPea.Map.Rendering/`       | Task 2: Add colorScheme param |
| `MapColorIntegrationTests.cs` | `dotnet/Map/PigeonPea.Map.Rendering.Tests/` | Task 3: NEW FILE              |
| `ColorScheme.cs`              | `dotnet/Map/PigeonPea.Map.Core/Domain/`     | Phase 1 (existing)            |
| `ColorSchemes.cs`             | `dotnet/Map/PigeonPea.Map.Rendering/`       | Phase 1 (existing)            |
| `ColorSchemesTests.cs`        | `dotnet/Map/PigeonPea.Map.Rendering.Tests/` | Phase 1 (existing)            |

---

**Document Version**: 1.0
**Created**: 2025-11-13
**Author**: Claude Code
**RFC**: RFC-010 Phase 2 Implementation Guide
