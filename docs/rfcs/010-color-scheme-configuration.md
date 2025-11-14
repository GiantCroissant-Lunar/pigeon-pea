# RFC-010: Color Scheme Configuration System

## Status

**Status**: Proposed
**Created**: 2025-11-13
**Author**: Claude Code (based on architecture review)
**Updated**: 2025-11-13 (domain architecture alignment)

## Dependencies

**Requires**:

- **RFC-007 Phase 3** (Map.Rendering must exist)
- **Projects**: `Map.Rendering`, `Map.Control` (for ViewModels)

**Blocks**: None (but RFC-011 Water Shimmer may want to use color schemes)

**Scope**: Map-specific color schemes for terrain and biomes. Dungeon color schemes (if needed) are out of scope.

## Summary

Implement a configurable color scheme system that allows users and renderers to select from multiple visual styles (Original, Realistic, Fantasy, Monochrome) for **map rendering**, with proper integration into ViewModels and rendering pipelines for both console and desktop applications.

## Motivation

### Current State

Color definitions are **hardcoded** in multiple places:

1. **SkiaMapRasterizer.cs**:

   ```csharp
   // Lines 46-54
   else if (cell.Height <= 20) { r = 10; g = 90; b = 220; } // Water
   else if (cell.Height >= 70) { r = 190; g = 190; b = 190; } // Mountains
   else if (cell.Height >= 50) { r = 34; g = 139; b = 34; } // Forest
   else { r = 46; g = 160; b = 60; } // Plains
   ```

2. **FantasyMapGenerator.Rendering/TerrainColorSchemes.cs** (in port):
   - Multiple color schemes defined but not integrated
   - No way to switch between them at runtime

3. **Biome colors** (from MapData.Biomes):
   ```csharp
   // Currently uses map.Biomes[cell.Biome].Color (hex string)
   // No fallback or scheme selection
   ```

### Problems

1. **No user choice**: Users can't change visual style without code changes
2. **Inconsistency**: Different renderers may use different colors for same height/biome
3. **Limited styles**: Only one hardcoded palette available in production code
4. **Accessibility**: No colorblind-friendly or high-contrast options
5. **Maintenance**: Changing colors requires code edits in multiple places

### Goals

1. **User control**: Allow users to select color schemes via UI or config
2. **Consistency**: Single source of truth for color definitions
3. **Variety**: Support multiple visual styles (realistic, fantasy, artistic)
4. **Extensibility**: Easy to add new color schemes without code changes (eventually)
5. **Integration**: Seamless integration with ViewModels and rendering pipeline

## Design

### Color Scheme Enum

```csharp
namespace PigeonPea.Map.Rendering;

/// <summary>
/// Available color schemes for map rendering
/// </summary>
public enum ColorScheme
{
    /// <summary>
    /// Based on original Azgaar's Fantasy Map Generator palette
    /// </summary>
    Original,

    /// <summary>
    /// Realistic terrain colors (satellite-like)
    /// </summary>
    Realistic,

    /// <summary>
    /// Vibrant, fantasy-style colors
    /// </summary>
    Fantasy,

    /// <summary>
    /// High-contrast, accessibility-friendly
    /// </summary>
    HighContrast,

    /// <summary>
    /// Grayscale (height-based shading only)
    /// </summary>
    Monochrome,

    /// <summary>
    /// Warm tones (sepia/parchment map feel)
    /// </summary>
    Parchment
}
```

### Color Scheme Provider

```csharp
namespace PigeonPea.Map.Rendering;

using SkiaSharp;

/// <summary>
/// Provides color mappings for different map rendering schemes
/// </summary>
public static class ColorSchemes
{
    /// <summary>
    /// Get color for a terrain height (0-255)
    /// </summary>
    public static SKColor GetHeightColor(byte height, ColorScheme scheme, bool isBiome = false, int biomeId = -1)
    {
        // If biome colors enabled and valid biome, use biome palette
        if (isBiome && biomeId >= 0)
        {
            return GetBiomeColor(biomeId, scheme);
        }

        // Otherwise use height-based gradient
        return scheme switch
        {
            ColorScheme.Original => GetOriginalHeightColor(height),
            ColorScheme.Realistic => GetRealisticHeightColor(height),
            ColorScheme.Fantasy => GetFantasyHeightColor(height),
            ColorScheme.HighContrast => GetHighContrastHeightColor(height),
            ColorScheme.Monochrome => GetMonochromeHeightColor(height),
            ColorScheme.Parchment => GetParchmentHeightColor(height),
            _ => GetOriginalHeightColor(height)
        };
    }

    #region Original Scheme (Azgaar-inspired)

    private static SKColor GetOriginalHeightColor(byte height)
    {
        return height switch
        {
            <= 19 => new SKColor(10, 90, 220),    // Deep ocean
            <= 20 => new SKColor(30, 120, 240),   // Shallow ocean
            <= 29 => new SKColor(210, 180, 140),  // Beach
            <= 49 => new SKColor(46, 160, 60),    // Lowlands
            <= 69 => new SKColor(34, 139, 34),    // Hills/forest
            <= 89 => new SKColor(139, 90, 43),    // Mountains
            _ => new SKColor(190, 190, 190)       // High peaks
        };
    }

    #endregion

    #region Realistic Scheme (Satellite-like)

    private static SKColor GetRealisticHeightColor(byte height)
    {
        return height switch
        {
            <= 19 => new SKColor(0, 42, 102),     // Deep ocean (dark blue)
            <= 20 => new SKColor(0, 105, 148),    // Shallow ocean
            <= 29 => new SKColor(238, 214, 175),  // Sand
            <= 39 => new SKColor(107, 142, 35),   // Grassland (olive)
            <= 59 => new SKColor(34, 139, 34),    // Forest (green)
            <= 79 => new SKColor(101, 67, 33),    // Foothills (brown)
            <= 94 => new SKColor(139, 137, 137),  // Rocky mountains (gray)
            _ => new SKColor(255, 250, 250)       // Snow peaks (white)
        };
    }

    #endregion

    #region Fantasy Scheme (Vibrant)

    private static SKColor GetFantasyHeightColor(byte height)
    {
        return height switch
        {
            <= 19 => new SKColor(0, 100, 255),    // Bright blue ocean
            <= 20 => new SKColor(50, 150, 255),   // Aqua
            <= 29 => new SKColor(255, 215, 100),  // Golden sand
            <= 49 => new SKColor(50, 205, 50),    // Lime green plains
            <= 69 => new SKColor(0, 128, 0),      // Emerald forest
            <= 89 => new SKColor(160, 82, 45),    // Sienna mountains
            _ => new SKColor(240, 240, 240)       // Silver peaks
        };
    }

    #endregion

    #region High Contrast Scheme (Accessibility)

    private static SKColor GetHighContrastHeightColor(byte height)
    {
        return height switch
        {
            <= 20 => new SKColor(0, 0, 139),      // Dark blue (water)
            <= 29 => new SKColor(255, 255, 0),    // Yellow (beach)
            <= 49 => new SKColor(0, 200, 0),      // Bright green (lowlands)
            <= 69 => new SKColor(0, 100, 0),      // Dark green (hills)
            <= 89 => new SKColor(139, 69, 19),    // Brown (mountains)
            _ => new SKColor(255, 255, 255)       // White (peaks)
        };
    }

    #endregion

    #region Monochrome Scheme (Grayscale)

    private static SKColor GetMonochromeHeightColor(byte height)
    {
        // Simple grayscale gradient
        byte gray = height;
        return new SKColor(gray, gray, gray);
    }

    #endregion

    #region Parchment Scheme (Warm tones)

    private static SKColor GetParchmentHeightColor(byte height)
    {
        // Sepia/parchment tones for "old map" aesthetic
        return height switch
        {
            <= 20 => new SKColor(180, 160, 120),  // Tan water
            <= 29 => new SKColor(230, 210, 170),  // Light tan beach
            <= 49 => new SKColor(210, 180, 140),  // Wheat lowlands
            <= 69 => new SKColor(180, 140, 100),  // Brown hills
            <= 89 => new SKColor(140, 100, 60),   // Dark brown mountains
            _ => new SKColor(240, 220, 200)       // Cream peaks
        };
    }

    #endregion

    #region Biome Colors

    /// <summary>
    /// Get color for a specific biome
    /// </summary>
    private static SKColor GetBiomeColor(int biomeId, ColorScheme scheme)
    {
        // Biome colors are less dependent on scheme (biomes have natural colors)
        // But we can add scheme-specific variations if desired
        return biomeId switch
        {
            0 => new SKColor(30, 144, 255),   // Marine (ocean)
            1 => new SKColor(70, 130, 180),   // Tundra
            2 => new SKColor(34, 139, 34),    // Boreal Forest
            3 => new SKColor(144, 238, 144),  // Temperate Grassland
            4 => new SKColor(107, 142, 35),   // Temperate Forest
            5 => new SKColor(210, 180, 140),  // Temperate Desert
            6 => new SKColor(255, 215, 0),    // Tropical Desert
            7 => new SKColor(154, 205, 50),   // Tropical Grassland
            8 => new SKColor(0, 128, 0),      // Tropical Rainforest
            _ => new SKColor(128, 128, 128)   // Unknown (gray)
        };
    }

    #endregion

    /// <summary>
    /// Interpolate between two colors based on a factor (0-1)
    /// </summary>
    public static SKColor Lerp(SKColor a, SKColor b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return new SKColor(
            (byte)(a.Red + (b.Red - a.Red) * t),
            (byte)(a.Green + (b.Green - a.Green) * t),
            (byte)(a.Blue + (b.Blue - a.Blue) * t),
            (byte)(a.Alpha + (b.Alpha - a.Alpha) * t)
        );
    }
}
```

### ViewModel Integration

```csharp
namespace PigeonPea.Map.Control.ViewModels;

using ReactiveUI;
using PigeonPea.Map.Rendering;

/// <summary>
/// ViewModel for map rendering settings (extends existing MapRenderViewModel)
/// </summary>
public class MapRenderViewModel : ReactiveObject
{
    // Existing properties...
    private ColorScheme _colorScheme = ColorScheme.Original;

    /// <summary>
    /// Currently selected color scheme
    /// </summary>
    public ColorScheme ColorScheme
    {
        get => _colorScheme;
        set => this.RaiseAndSetIfChanged(ref _colorScheme, value);
    }

    // Available color schemes for UI binding
    public IEnumerable<ColorScheme> AvailableColorSchemes =>
        Enum.GetValues<ColorScheme>();
}
```

### Renderer Integration

**Update SkiaMapRasterizer.cs**:

```csharp
public static Raster Render(
    MapData map,
    Viewport viewport,
    double zoom,
    int ppc,
    bool biomeColors,
    bool rivers,
    double timeSeconds = 0,
    LayersViewModel? layers = null,
    ColorScheme colorScheme = ColorScheme.Original) // NEW parameter
{
    // ... existing setup ...

    for (int cy = 0; cy < rows; cy++)
    {
        for (int cx = 0; cx < cols; cx++)
        {
            double wx = viewport.X + (cx + 0.5) * zoom;
            double wy = viewport.Y + (cy + 0.5) * zoom;
            var cell = map.GetCellAt(wx, wy);

            SKColor color;
            if (cell == null)
            {
                color = ColorSchemes.GetHeightColor(0, colorScheme); // Ocean
            }
            else
            {
                color = ColorSchemes.GetHeightColor(
                    cell.Height,
                    colorScheme,
                    isBiome: biomeColors && cell.Biome >= 0,
                    biomeId: cell.Biome
                );
            }

            byte r = color.Red;
            byte g = color.Green;
            byte b = color.Blue;
            byte a = 255;

            // ... rest of pixel filling ...
        }
    }
}
```

### UI Integration Examples

#### Terminal.Gui (Console)

```csharp
// In TerminalHudApplication or settings dialog
var colorSchemeCombo = new ComboBox
{
    X = 1,
    Y = 5,
    Width = 20,
    Height = 1
};
colorSchemeCombo.SetSource(Enum.GetNames<ColorScheme>());
colorSchemeCombo.SelectedItemChanged += (args) =>
{
    var selected = (ColorScheme)args.Value;
    _mapRenderViewModel.ColorScheme = selected;
    _mapPanel.InvalidateView(); // Trigger re-render
};
```

#### Avalonia (Desktop)

```xaml
<!-- In MapParametersPanel.axaml or settings window -->
<ComboBox
    Grid.Row="3"
    ItemsSource="{Binding AvailableColorSchemes}"
    SelectedItem="{Binding ColorScheme}"
    HorizontalAlignment="Stretch">
    <ComboBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding}" />
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>
```

## Implementation Plan

### Phase 1: Core Color Scheme System (Week 1, Priority: P0)

**Tasks**:

1. Create `ColorScheme` enum in `Map.Rendering/`
2. Implement `ColorSchemes` static class with all scheme methods
3. Add unit tests for color lookup
4. Document each scheme's aesthetic in XML comments

**Files Created**:

- `dotnet/map-rendering/ColorScheme.cs` (enum)
- `dotnet/map-rendering/ColorSchemes.cs` (provider class)
- `dotnet/map-rendering.Tests/ColorSchemesTests.cs` (unit tests)

**Success Criteria**:

- All schemes return valid colors for height range 0-255
- Tests verify distinct colors per scheme
- Code compiles without warnings

### Phase 2: Integrate with SkiaMapRasterizer (Week 1, Priority: P0)

**Tasks**:

1. Add `colorScheme` parameter to `SkiaMapRasterizer.Render()`
2. Replace hardcoded color logic with `ColorSchemes.GetHeightColor()`
3. Test rendering with different schemes
4. Verify visual output matches expectations

**Files Modified**:

- `dotnet/map-rendering/SkiaMapRasterizer.cs`

**Success Criteria**:

- Rendering works with all color schemes
- No performance regression (color lookup should be fast)
- Visual regression tests show distinct palettes

### Phase 3: ViewModel Integration (Week 2, Priority: P1)

**Tasks**:

1. Add `ColorScheme` property to `MapRenderViewModel`
2. Add `AvailableColorSchemes` for UI binding
3. Ensure property change triggers re-render
4. Add to Map.Control ViewModel configuration

**Files Modified**:

- `dotnet/map-control/ViewModels/MapRenderViewModel.cs`

**Success Criteria**:

- ViewModel supports color scheme selection
- ReactiveUI property changes work correctly

### Phase 4: UI Controls (Week 2, Priority: P1)

**Tasks**:

1. Add color scheme selector to console HUD
2. Add color scheme selector to Avalonia settings/parameters panel
3. Wire up to ViewModel
4. Test live switching (change scheme, map updates)

**Files Modified**:

- `dotnet/console-app/TerminalHudApplication.cs` (or settings dialog)
- `dotnet/_lib/fantasy-map-generator-port/src/FantasyMapGenerator.UI/Controls/MapParametersPanel.axaml` (if using this)

**Success Criteria**:

- Users can switch color schemes from UI
- Changes apply immediately (or on next render)

### Phase 5: Persistence and Documentation (Week 3, Priority: P2)

**Tasks**:

1. Save selected color scheme to config file (if config system exists)
2. Load color scheme on startup
3. Document color schemes in user guide
4. Add screenshots of each scheme to docs

**Files Created/Modified**:

- `docs/user-guide-color-schemes.md` (new)
- `docs/screenshots/` (add scheme comparison images)
- Config file handling (if applicable)

**Success Criteria**:

- Color scheme preference persists across sessions
- Documentation explains each scheme's use case

## Implementation Notes for Agents

### Prerequisites

1. **RFC-007 Phase 3** must be completed first (or implement in parallel with Phase 3)
2. Verify these projects exist:
   - `dotnet/map-rendering/PigeonPea.Map.Rendering.csproj`
   - `dotnet/map-control/PigeonPea.Map.Control.csproj`
3. Verify `SkiaMapRasterizer.cs` is in `dotnet/map-rendering/` (not in shared-app)

### File Creation Order

**Step 1: Create ColorScheme.cs**

Location: `dotnet/map-rendering/ColorScheme.cs`

```csharp
namespace PigeonPea.Map.Rendering;

/// <summary>
/// Available color schemes for map rendering
/// </summary>
public enum ColorScheme
{
    Original,
    Realistic,
    Fantasy,
    HighContrast,
    Monochrome,
    Parchment
}
```

**Step 2: Create ColorSchemes.cs**

Location: `dotnet/map-rendering/ColorSchemes.cs`

- Copy the full implementation from "Color Scheme Provider" section above (lines 112-285)
- Ensure namespace is `PigeonPea.Map.Rendering`
- Verify `using SkiaSharp;` is included

**Step 3: Update SkiaMapRasterizer.cs**

Location: `dotnet/map-rendering/SkiaMapRasterizer.cs`

Changes:

1. Add `colorScheme` parameter to `Render()` method signature
2. Replace hardcoded color logic (currently around lines 46-54) with:
   ```csharp
   color = ColorSchemes.GetHeightColor(
       cell.Height,
       colorScheme,
       isBiome: biomeColors && cell.Biome >= 0,
       biomeId: cell.Biome
   );
   ```
3. Default `colorScheme` parameter to `ColorScheme.Original`

**Step 4: Create Unit Tests**

Location: `dotnet/map-rendering.Tests/ColorSchemesTests.cs`

- Copy full test implementation from "Unit Tests" section (lines 497-564)
- Verify test project references:
  - `xunit`
  - `SkiaSharp`
  - `PigeonPea.Map.Rendering` project

**Step 5: Update MapRenderViewModel**

Location: `dotnet/map-control/ViewModels/MapRenderViewModel.cs`

Add:

```csharp
using PigeonPea.Map.Rendering;

private ColorScheme _colorScheme = ColorScheme.Original;

public ColorScheme ColorScheme
{
    get => _colorScheme;
    set => this.RaiseAndSetIfChanged(ref _colorScheme, value);
}

public IEnumerable<ColorScheme> AvailableColorSchemes =>
    Enum.GetValues<ColorScheme>();
```

**Step 6: UI Integration (Optional Phase 4)**

- Console: Update `TerminalHudApplication.cs` or create settings dialog
- Desktop: Update Avalonia controls (if they exist)

### Critical Code Locations

| File                      | Current Location                 | Lines       | What to Change                |
| ------------------------- | -------------------------------- | ----------- | ----------------------------- |
| **SkiaMapRasterizer.cs**  | `dotnet/map-rendering/`          | ~46-54      | Replace hardcoded color logic |
| **MapRenderViewModel.cs** | `dotnet/map-control/ViewModels/` | End of file | Add ColorScheme properties    |

### Validation Checklist

After implementation, verify:

- [ ] `ColorScheme` enum compiles without errors
- [ ] `ColorSchemes.GetHeightColor()` returns valid colors for heights 0-255
- [ ] All 6 schemes are implemented (Original, Realistic, Fantasy, HighContrast, Monochrome, Parchment)
- [ ] Unit tests pass: `dotnet test dotnet/map-rendering.Tests/`
- [ ] `SkiaMapRasterizer.Render()` accepts `colorScheme` parameter
- [ ] Rendering with different schemes produces visually distinct outputs
- [ ] No hardcoded color values remain in SkiaMapRasterizer (except fallback cases)
- [ ] MapRenderViewModel has `ColorScheme` property
- [ ] ReactiveUI property change notifications work

### Common Pitfalls

1. **Wrong namespace**: Ensure `PigeonPea.Map.Rendering` (not `PigeonPea.SharedApp.Rendering`)
2. **Missing SKColor**: `ColorSchemes` must `using SkiaSharp;`
3. **Hardcoded colors still present**: Verify all height-based color logic is replaced
4. **Test project references**: Ensure test project references `PigeonPea.Map.Rendering`
5. **ViewModel location**: Ensure MapRenderViewModel is in `Map.Control` (not `Shared`)

### Integration with Other RFCs

- **RFC-007**: This RFC depends on Phase 3 (Map.Rendering exists)
- **RFC-011**: Water shimmer can optionally use ColorSchemes for base colors

### Performance Notes

- Switch expressions are O(1) constant time
- No caching needed (color lookup is fast enough)
- If performance becomes an issue, consider pre-computing lookup tables

## Testing Strategy

### Unit Tests

```csharp
using Xunit;
using PigeonPea.Map.Rendering;

public class ColorSchemesTests
{
    [Theory]
    [InlineData(ColorScheme.Original)]
    [InlineData(ColorScheme.Realistic)]
    [InlineData(ColorScheme.Fantasy)]
    [InlineData(ColorScheme.HighContrast)]
    [InlineData(ColorScheme.Monochrome)]
    [InlineData(ColorScheme.Parchment)]
    public void AllSchemes_ReturnValidColors_ForAllHeights(ColorScheme scheme)
    {
        for (byte height = 0; height <= 255; height++)
        {
            var color = ColorSchemes.GetHeightColor(height, scheme);

            // Color components must be in valid range
            Assert.InRange(color.Red, 0, 255);
            Assert.InRange(color.Green, 0, 255);
            Assert.InRange(color.Blue, 0, 255);
        }
    }

    [Fact]
    public void Monochrome_ProducesGrayscale()
    {
        for (byte height = 0; height <= 255; height++)
        {
            var color = ColorSchemes.GetHeightColor(height, ColorScheme.Monochrome);

            // In monochrome, R == G == B
            Assert.Equal(color.Red, color.Green);
            Assert.Equal(color.Green, color.Blue);
        }
    }

    [Fact]
    public void DifferentSchemes_ProduceDifferentColors()
    {
        byte testHeight = 50; // Midpoint

        var original = ColorSchemes.GetHeightColor(testHeight, ColorScheme.Original);
        var realistic = ColorSchemes.GetHeightColor(testHeight, ColorScheme.Realistic);
        var fantasy = ColorSchemes.GetHeightColor(testHeight, ColorScheme.Fantasy);

        // At least two should differ
        Assert.True(
            original != realistic || realistic != fantasy || original != fantasy,
            "Different schemes should produce different colors"
        );
    }

    [Fact]
    public void Lerp_InterpolatesCorrectly()
    {
        var colorA = new SKColor(0, 0, 0);     // Black
        var colorB = new SKColor(255, 255, 255); // White

        var mid = ColorSchemes.Lerp(colorA, colorB, 0.5);

        Assert.Equal(127, mid.Red, tolerance: 1);
        Assert.Equal(127, mid.Green, tolerance: 1);
        Assert.Equal(127, mid.Blue, tolerance: 1);
    }
}
```

### Visual Regression Tests

Generate reference images for each color scheme:

```csharp
[Theory]
[InlineData(ColorScheme.Original)]
[InlineData(ColorScheme.Realistic)]
// ... etc
public void GenerateReferenceImage(ColorScheme scheme)
{
    var map = TestMapGenerator.CreateSampleMap(seed: 12345);
    var raster = SkiaMapRasterizer.Render(
        map,
        new Viewport { Width = 200, Height = 200 },
        zoom: 1.0,
        ppc: 4,
        biomeColors: true,
        rivers: false,
        colorScheme: scheme
    );

    // Save to file
    File.WriteAllBytes($"test-output/{scheme}.png", raster.Rgba);
}
```

Then compare against these reference images on future runs.

## Alternatives Considered

### Alternative 1: User-Defined Color Schemes (JSON/YAML)

Allow users to define custom color schemes in config files.

**Pros**:

- Ultimate flexibility
- Community-created schemes

**Cons**:

- Complex implementation (parsing, validation)
- Potential for invalid/ugly schemes
- Harder to test

**Decision**: Phase 2 enhancement, not MVP. Start with predefined schemes.

### Alternative 2: Per-Biome Color Overrides

Allow users to customize individual biome colors.

**Pros**:

- Fine-grained control
- Matches original Azgaar's FMG

**Cons**:

- Complex UI (many inputs)
- Hard to create coherent palettes
- Out of scope for this RFC

**Decision**: Separate RFC if needed.

### Alternative 3: Gradient-Based Schemes

Define schemes as gradients with stops, then interpolate.

**Pros**:

- Smoother color transitions
- More compact definitions

**Cons**:

- More complex implementation
- May not match discrete terrain types well

**Decision**: Consider for future, but switch statement approach is simpler for MVP.

## Risks and Mitigations

| Risk                                   | Probability | Impact | Mitigation                                                          |
| -------------------------------------- | ----------- | ------ | ------------------------------------------------------------------- |
| **Performance impact of color lookup** | Low         | Low    | Use switch expressions (fast); benchmark if concerned               |
| **UI complexity**                      | Medium      | Medium | Keep UI simple (dropdown only); don't overwhelm users               |
| **Accessibility issues**               | Low         | Medium | HighContrast scheme addresses this; test with colorblind simulators |
| **Users dislike predefined schemes**   | Medium      | Low    | Document that custom schemes are future enhancement                 |

## Success Criteria

1. ✅ **6 color schemes available**: Original, Realistic, Fantasy, HighContrast, Monochrome, Parchment
2. ✅ **UI integration**: Users can select scheme in both console and desktop apps
3. ✅ **Live updates**: Changing scheme updates map immediately
4. ✅ **Tests pass**: Unit tests verify color validity
5. ✅ **Documentation**: User guide explains each scheme
6. ✅ **No performance regression**: Rendering speed unchanged

## Timeline

- **Week 1**: Phase 1-2 (core system + renderer integration) - **Priority P0**
- **Week 2**: Phase 3-4 (ViewModel + UI controls) - **Priority P1**
- **Week 3**: Phase 5 (persistence + docs) - **Priority P2**

**Total effort**: ~3 weeks (part-time)

## Open Questions

1. **Default scheme**: Which should be default?
   - Recommendation: `Original` (matches existing hardcoded colors)

2. **Biome vs height priority**: If biome colors enabled, should scheme still affect them?
   - Recommendation: No. Biomes have natural colors; scheme only affects height-based rendering.

3. **Animation support**: Should schemes support time-based color shifts (e.g., day/night)?
   - Recommendation: Out of scope for this RFC; address in water shimmer RFC-011.

4. **Colorblind modes**: Should we add deuteranopia/protanopia schemes?
   - Recommendation: Future enhancement; HighContrast is a start.

## References

- Original Azgaar's FMG color schemes (visual reference)
- ColorBrewer (cartographic color schemes): https://colorbrewer2.org/
- Accessibility guidelines: WCAG 2.1 contrast ratios

## Approval

- [ ] Architecture review
- [ ] UI/UX review
- [ ] Accessibility review
- [ ] Ready for implementation

---

**Next Steps**: Implement `ColorScheme` enum and `ColorSchemes` class in `SharedApp.Rendering/`.
