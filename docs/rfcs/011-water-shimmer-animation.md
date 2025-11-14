# RFC-011: Water Shimmer Animation Feature

## Status

**Status**: Proposed
**Created**: 2025-11-13
**Author**: Claude Code (based on architecture review)
**Updated**: 2025-11-13 (domain architecture alignment)

## Dependencies

**Requires**:

- **RFC-007 Phase 3** (Map.Rendering must exist)
- **Project**: `Map.Rendering` (SkiaMapRasterizer is in this project)

**Optional**:

- **RFC-010**: Color scheme configuration (shimmer can use ColorSchemes for base water colors)

**Blocks**: None

**Scope**: Map-specific water shimmer for ocean and lake cells. Dungeon water (if applicable) is out of scope.

## Summary

Implement a subtle water shimmer animation effect using the existing `timeSeconds` parameter in `SkiaMapRasterizer`, providing visual dynamism to ocean and lake cells through sinusoidal color modulation. The feature is opt-in via environment flag (`PP_WATER_SHIMMER=1`) to avoid performance impact for users who don't need it.

## Motivation

### Current State

**Static Rendering**:

- Water cells (height ≤ 20) are rendered with fixed colors
- No visual indication of "aliveness" or flow
- Time parameter (`timeSeconds`) is plumbed through rendering pipeline but unused

**Time Parameter Exists**:

```csharp
// In SkiaMapRasterizer.cs:18
public static Raster Render(
    MapData map,
    Viewport viewport,
    double zoom,
    int ppc,
    bool biomeColors,
    bool rivers,
    double timeSeconds = 0, // ← Already exists!
    LayersViewModel? layers = null)
```

This was added in anticipation of animation features but never implemented.

### Goals

1. **Visual polish**: Add subtle movement to water to make maps feel more alive
2. **Minimal performance cost**: Shimmer should not significantly slow rendering
3. **Opt-in**: Users can disable if not needed (console use cases may prefer static)
4. **Extensible**: Foundation for future time-based effects (river flow animation, day/night cycle)

### Non-Goals

- **Complex fluid simulation**: Not attempting realistic water physics
- **River flow visualization**: Rivers remain static (future enhancement)
- **Day/night cycle**: Out of scope (separate RFC if desired)
- **Wind/wave effects**: Too complex for this RFC

## Design

### Shimmer Algorithm

Use a simple sinusoidal function to modulate water color brightness:

```csharp
shimmer = amplitude × sin(frequency × timeSeconds + phase(x, y))
```

**Parameters**:

- **Amplitude**: How much brightness varies (e.g., ±10 in RGB)
- **Frequency**: How fast shimmer oscillates (e.g., 1-2 Hz)
- **Phase**: Spatial offset based on position (creates wave pattern)

**Key properties**:

- **Subtle**: Small amplitude prevents distracting flicker
- **Continuous**: No visible jumps between frames
- **Deterministic**: Same position and time → same shimmer value
- **Spatial variation**: Different parts of ocean shimmer slightly out of phase

### Implementation

**Update SkiaMapRasterizer.cs**:

```csharp
// Add near top of file
private static readonly bool WaterShimmerEnabled =
    Environment.GetEnvironmentVariable("PP_WATER_SHIMMER") == "1";

// In the per-cell rendering loop (around line 46):
else if (cell.Height <= 20) // Water
{
    byte baseR = 10, baseG = 90, baseB = 220;

    if (WaterShimmerEnabled && timeSeconds > 0)
    {
        // Subtle shimmer effect
        double shimmer = ComputeWaterShimmer(wx, wy, timeSeconds);

        // Apply shimmer (±10 brightness modulation)
        r = (byte)Math.Clamp(baseR + shimmer, 0, 255);
        g = (byte)Math.Clamp(baseG + shimmer, 0, 255);
        b = (byte)Math.Clamp(baseB + shimmer, 0, 255);
    }
    else
    {
        r = baseR;
        g = baseG;
        b = baseB;
    }
}

// New helper method
private static double ComputeWaterShimmer(double x, double y, double t)
{
    const double amplitude = 10.0;    // ±10 RGB units
    const double frequency = 1.5;     // 1.5 Hz (gentle)
    const double spatialScale = 0.01; // Wave spread

    // Phase offset based on position (creates spatial pattern)
    double phase = (x + y) * spatialScale;

    // Sinusoidal modulation
    double shimmer = amplitude * Math.Sin(frequency * t + phase);

    return shimmer;
}
```

### Advanced Variant (Optional)

For more sophisticated shimmer, use **multiple frequencies** (like Perlin noise):

```csharp
private static double ComputeWaterShimmer(double x, double y, double t)
{
    const double amplitude1 = 8.0;
    const double amplitude2 = 4.0;
    const double freq1 = 1.2;
    const double freq2 = 2.5;
    const double spatialScale = 0.01;

    double phase1 = (x + y) * spatialScale;
    double phase2 = (x - y) * spatialScale * 1.3;

    double shimmer1 = amplitude1 * Math.Sin(freq1 * t + phase1);
    double shimmer2 = amplitude2 * Math.Sin(freq2 * t + phase2);

    return shimmer1 + shimmer2;
}
```

This creates a more organic "lapping waves" effect with overlapping frequencies.

### River Shimmer (Future Enhancement)

For rivers, add directional shimmer that follows the flow:

```csharp
if (cell.IsRiver)
{
    // Get river flow direction (would need to add to Cell model)
    double flowAngle = cell.RiverFlowAngle;

    // Directional shimmer (moves along flow)
    double phase = x * Math.Cos(flowAngle) + y * Math.Sin(flowAngle);
    double shimmer = amplitude * Math.Sin(frequency * t + phase * 0.02);

    // Highlight river with shimmer
    r = (byte)Math.Clamp(baseR + shimmer, 0, 255);
    // ...
}
```

**Deferred to future RFC** (requires river flow direction data).

### Environment Flag

**Flag**: `PP_WATER_SHIMMER=1`

**Rationale**:

- **Console users**: May prefer static rendering for performance or aesthetic reasons
- **Desktop users**: Can enable for visual polish
- **CI/Testing**: Disabled by default (deterministic screenshots)

**Documentation** (in README or env var guide):

```markdown
## Environment Variables

- `PP_WATER_SHIMMER=1`: Enable animated water shimmer effect (default: disabled)
  - Adds subtle brightness modulation to ocean and lake cells
  - Requires time parameter passed to renderer (automatic in real-time apps)
  - Minimal performance impact (~1-2% slower rendering)
```

### Animation Ticker Integration

**Console (Terminal.Gui)**:

Already has animation ticker in `TerminalHudApplication`:

```csharp
// In constructor or initialization:
_animationTimer = new Timer(_ =>
{
    _currentTime += 0.016; // ~60 FPS delta
    Application.MainLoop.Invoke(() =>
    {
        _mapPanel.InvalidateView(); // Triggers re-render with updated time
    });
}, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(16));
```

Pass `_currentTime` to renderer:

```csharp
var raster = SkiaMapRasterizer.Render(
    _mapData,
    _viewport,
    _zoom,
    _ppc,
    biomeColors: true,
    rivers: _showRivers,
    timeSeconds: _currentTime // ← Use accumulated time
);
```

**Desktop (Avalonia)**:

Use `DispatcherTimer` or `RenderLoop`:

```csharp
private double _animationTime = 0;
private DispatcherTimer _animationTimer;

public void StartAnimation()
{
    _animationTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
    };
    _animationTimer.Tick += (s, e) =>
    {
        _animationTime += 0.016;
        InvalidateVisual(); // Trigger redraw
    };
    _animationTimer.Start();
}

protected override void OnRender(DrawingContext context)
{
    var raster = SkiaMapRasterizer.Render(
        _mapData,
        _viewport,
        _zoom,
        _ppc,
        biomeColors: true,
        rivers: false,
        timeSeconds: _animationTime
    );
    // ... draw raster to canvas
}
```

## Implementation Plan

### Phase 1: Core Shimmer Implementation (Week 1, Priority: P2)

**Tasks**:

1. Add `WaterShimmerEnabled` flag check in `SkiaMapRasterizer.cs`
2. Implement `ComputeWaterShimmer()` method (simple single-frequency version)
3. Apply shimmer to water cells (height ≤ 20)
4. Test with static time values (verify shimmer works)

**Files Modified**:

- `dotnet/map-rendering/SkiaMapRasterizer.cs`

**Success Criteria**:

- Shimmer applies when `PP_WATER_SHIMMER=1` and `timeSeconds > 0`
- No shimmer when flag disabled or time = 0
- Visual inspection: water brightness varies subtly

### Phase 2: Animation Integration (Week 1, Priority: P2)

**Tasks**:

1. Verify console app's animation ticker passes `timeSeconds`
2. Add animation ticker to desktop app (if missing)
3. Test live animation (water should shimmer smoothly)
4. Tune parameters (amplitude, frequency) for best visual effect

**Files Modified**:

- `dotnet/console-app/TerminalHudApplication.cs` (verify existing ticker)
- Desktop app (add ticker if needed - location depends on RFC-007 implementation)

**Success Criteria**:

- Water shimmers smoothly in real-time
- No visible jitter or frame drops
- Shimmer pattern is visually pleasing (not distracting)

### Phase 3: Performance Verification (Week 2, Priority: P2)

**Tasks**:

1. Benchmark rendering with and without shimmer
2. Verify performance impact is <5%
3. Profile hot paths (ensure `ComputeWaterShimmer` is not bottleneck)
4. Optimize if necessary (precompute values, use LUT)

**Files Modified**:

- `benchmarks/PigeonPea.Benchmarks/Map/SkiaRasterizerBenchmarks.cs` (add shimmer benchmark - see RFC-009)

**Success Criteria**:

- Shimmer adds <5% rendering overhead
- No allocations per frame (all calculations on stack)
- Benchmarks document performance impact

### Phase 4: Documentation and Configuration (Week 2, Priority: P2)

**Tasks**:

1. Document `PP_WATER_SHIMMER` flag in README or env var guide
2. Add screenshots/GIFs showing shimmer effect
3. Add user guide entry explaining how to enable/disable
4. Update architecture docs with animation system notes

**Files Created/Modified**:

- `docs/environment-variables.md` (add shimmer flag)
- `docs/user-guide.md` (animation features section)
- `README.md` (mention shimmer in features list)

**Success Criteria**:

- Users know how to enable shimmer
- Documentation explains performance trade-offs
- Visual examples show effect

## Implementation Notes for Agents

### Prerequisites

1. **RFC-007 Phase 3** must be completed first
2. Verify `SkiaMapRasterizer.cs` exists in `dotnet/map-rendering/`
3. Verify `timeSeconds` parameter already exists in `Render()` method
4. _Optional_: If RFC-010 is implemented, use `ColorSchemes.GetHeightColor()` instead of hardcoded water colors

### File Modification Order

**Step 1: Add Environment Flag Check**

Location: `dotnet/map-rendering/SkiaMapRasterizer.cs`

Add near top of class (outside methods):

```csharp
private static readonly bool WaterShimmerEnabled =
    Environment.GetEnvironmentVariable("PP_WATER_SHIMMER") == "1";
```

**Step 2: Implement ComputeWaterShimmer() Method**

Add to `SkiaMapRasterizer` class:

```csharp
/// <summary>
/// Computes shimmer effect for water cells based on position and time
/// </summary>
private static double ComputeWaterShimmer(double x, double y, double t)
{
    const double amplitude = 10.0;    // ±10 RGB units
    const double frequency = 1.5;     // 1.5 Hz (gentle)
    const double spatialScale = 0.01; // Wave spread

    // Phase offset based on position (creates spatial pattern)
    double phase = (x + y) * spatialScale;

    // Sinusoidal modulation
    double shimmer = amplitude * Math.Sin(frequency * t + phase);

    return shimmer;
}
```

**Step 3: Apply Shimmer to Water Cells**

Find the water rendering logic (currently around lines 46-54 in existing SkiaMapRasterizer).

**If RFC-010 NOT implemented** (hardcoded colors):

```csharp
else if (cell.Height <= 20) // Water
{
    byte baseR = 10, baseG = 90, baseB = 220;

    if (WaterShimmerEnabled && timeSeconds > 0)
    {
        double shimmer = ComputeWaterShimmer(wx, wy, timeSeconds);
        r = (byte)Math.Clamp(baseR + shimmer, 0, 255);
        g = (byte)Math.Clamp(baseG + shimmer, 0, 255);
        b = (byte)Math.Clamp(baseB + shimmer, 0, 255);
    }
    else
    {
        r = baseR;
        g = baseG;
        b = baseB;
    }
}
```

**If RFC-010 IS implemented** (ColorSchemes):

```csharp
else if (cell.Height <= 20) // Water
{
    var baseColor = ColorSchemes.GetHeightColor(cell.Height, colorScheme);

    if (WaterShimmerEnabled && timeSeconds > 0)
    {
        double shimmer = ComputeWaterShimmer(wx, wy, timeSeconds);
        r = (byte)Math.Clamp(baseColor.Red + shimmer, 0, 255);
        g = (byte)Math.Clamp(baseColor.Green + shimmer, 0, 255);
        b = (byte)Math.Clamp(baseColor.Blue + shimmer, 0, 255);
    }
    else
    {
        r = baseColor.Red;
        g = baseColor.Green;
        b = baseColor.Blue;
    }
}
```

**Step 4: Verify Animation Ticker (Console App)**

Location: `dotnet/console-app/TerminalHudApplication.cs`

Ensure the app passes accumulated time:

```csharp
var raster = SkiaMapRasterizer.Render(
    _mapData,
    _viewport,
    _zoom,
    _ppc,
    biomeColors: true,
    rivers: _showRivers,
    timeSeconds: _currentTime // Must be accumulating time, not 0
);
```

**Step 5: Create Unit Tests**

Location: `dotnet/map-rendering.Tests/WaterShimmerTests.cs`

Copy full test implementation from "Unit Tests" section (lines 329-401).

Verify test project references:

- `xunit`
- `PigeonPea.Map.Rendering` project
- `FantasyMapGenerator.Core` (for MapData if needed)

### Critical Code Locations

| File                          | Location                | Lines        | What to Change                 |
| ----------------------------- | ----------------------- | ------------ | ------------------------------ |
| **SkiaMapRasterizer.cs**      | `dotnet/map-rendering/` | Top of class | Add `WaterShimmerEnabled` flag |
| **SkiaMapRasterizer.cs**      | `dotnet/map-rendering/` | New method   | Add `ComputeWaterShimmer()`    |
| **SkiaMapRasterizer.cs**      | `dotnet/map-rendering/` | ~46-54       | Apply shimmer to water cells   |
| **TerminalHudApplication.cs** | `dotnet/console-app/`   | Render call  | Verify `timeSeconds` is passed |

### Validation Checklist

After implementation, verify:

- [ ] `WaterShimmerEnabled` flag is checked correctly
- [ ] `ComputeWaterShimmer()` method compiles without errors
- [ ] Water cells shimmer when `PP_WATER_SHIMMER=1` and `timeSeconds > 0`
- [ ] Water cells are static when flag is disabled or `timeSeconds = 0`
- [ ] Shimmer is deterministic (same time → same output)
- [ ] No RGB values exceed 0-255 range (Math.Clamp works)
- [ ] Unit tests pass: `dotnet test dotnet/map-rendering.Tests/`
- [ ] Visual inspection: shimmer is subtle, not distracting
- [ ] Performance: rendering FPS drop is <5%

### Common Pitfalls

1. **Flag always disabled**: Ensure environment variable is set: `set PP_WATER_SHIMMER=1` (Windows) or `export PP_WATER_SHIMMER=1` (Linux/Mac)
2. **Time not updating**: Verify animation ticker accumulates time (not always 0)
3. **Math.Clamp missing**: Requires C# 9+ or use `Math.Max(0, Math.Min(255, value))`
4. **Shimmer too strong**: Reduce amplitude if visually distracting
5. **Performance regression**: Profile if shimmer causes >5% slowdown (unlikely with simple sin)

### Integration with Other RFCs

- **RFC-007**: Depends on Phase 3 (Map.Rendering exists)
- **RFC-009**: Add shimmer benchmarks to performance suite
- **RFC-010**: If implemented, use ColorSchemes instead of hardcoded colors

### Tuning Parameters

After initial implementation, consider tuning:

| Parameter        | Current | Effect               | Tuning Suggestion                  |
| ---------------- | ------- | -------------------- | ---------------------------------- |
| **amplitude**    | 10.0    | Brightness variation | Reduce to 5-7 for subtler effect   |
| **frequency**    | 1.5 Hz  | Shimmer speed        | Increase to 2.0 for faster shimmer |
| **spatialScale** | 0.01    | Wave pattern spread  | Increase to 0.02 for tighter waves |

Test with different parameter values and choose what looks best visually.

## Testing Strategy

### Unit Tests

```csharp
using Xunit;
using PigeonPea.Map.Rendering;

public class WaterShimmerTests
{
    [Fact]
    public void Shimmer_IsDisabled_WhenFlagNotSet()
    {
        // Ensure PP_WATER_SHIMMER is not set
        Environment.SetEnvironmentVariable("PP_WATER_SHIMMER", null);

        var map = TestMapGenerator.CreateOceanMap();
        var raster1 = SkiaMapRasterizer.Render(map, viewport, 1.0, 4, false, false, timeSeconds: 0.0);
        var raster2 = SkiaMapRasterizer.Render(map, viewport, 1.0, 4, false, false, timeSeconds: 1.0);

        // Without flag, time should not affect rendering
        Assert.Equal(raster1.Rgba, raster2.Rgba);
    }

    [Fact]
    public void Shimmer_IsEnabled_WhenFlagSet()
    {
        Environment.SetEnvironmentVariable("PP_WATER_SHIMMER", "1");

        var map = TestMapGenerator.CreateOceanMap();
        var raster1 = SkiaMapRasterizer.Render(map, viewport, 1.0, 4, false, false, timeSeconds: 0.0);
        var raster2 = SkiaMapRasterizer.Render(map, viewport, 1.0, 4, false, false, timeSeconds: 1.0);

        // With flag, time should change rendering
        Assert.NotEqual(raster1.Rgba, raster2.Rgba);

        Environment.SetEnvironmentVariable("PP_WATER_SHIMMER", null);
    }

    [Fact]
    public void Shimmer_IsDeterministic()
    {
        Environment.SetEnvironmentVariable("PP_WATER_SHIMMER", "1");

        var map = TestMapGenerator.CreateOceanMap();

        // Same time → same output
        var raster1 = SkiaMapRasterizer.Render(map, viewport, 1.0, 4, false, false, timeSeconds: 0.5);
        var raster2 = SkiaMapRasterizer.Render(map, viewport, 1.0, 4, false, false, timeSeconds: 0.5);

        Assert.Equal(raster1.Rgba, raster2.Rgba);

        Environment.SetEnvironmentVariable("PP_WATER_SHIMMER", null);
    }

    [Fact]
    public void Shimmer_StaysWithinBounds()
    {
        Environment.SetEnvironmentVariable("PP_WATER_SHIMMER", "1");

        var map = TestMapGenerator.CreateOceanMap();

        // Test many time values
        for (double t = 0; t < 10; t += 0.1)
        {
            var raster = SkiaMapRasterizer.Render(map, viewport, 1.0, 4, false, false, timeSeconds: t);

            // All RGB values must be in [0, 255]
            for (int i = 0; i < raster.Rgba.Length; i++)
            {
                Assert.InRange(raster.Rgba[i], 0, 255);
            }
        }

        Environment.SetEnvironmentVariable("PP_WATER_SHIMMER", null);
    }
}
```

### Visual Tests

1. **Static shimmer test**: Render frames at t=0, t=π/2, t=π, t=3π/2, t=2π
   - Verify shimmer completes full cycle
   - Check for visual artifacts

2. **Animation test**: Record 60 frames (1 second at 60 FPS)
   - Verify smooth transition between frames
   - Check for flickering or discontinuities

3. **Performance test**: Measure FPS with and without shimmer
   - Ensure FPS drop is acceptable (<5%)

## Alternatives Considered

### Alternative 1: Shader-Based Shimmer (GPU)

Use Skia shaders or GLSL for shimmer effect.

**Pros**:

- Potentially faster (GPU-accelerated)
- More sophisticated effects possible

**Cons**:

- More complex implementation
- Platform-dependent (not all terminals support GPU)
- Overkill for simple shimmer

**Decision**: Rejected. CPU-based is sufficient and more portable.

### Alternative 2: Pre-Computed Shimmer Texture

Generate shimmer pattern once, tile it over water.

**Pros**:

- No per-frame computation
- Very fast

**Cons**:

- Static pattern (no time variation)
- Requires texture management
- Loses dynamic shimmer feel

**Decision**: Rejected. Defeats purpose of animation.

### Alternative 3: Perlin Noise for Shimmer

Use 3D Perlin noise (x, y, time) for shimmer.

**Pros**:

- More organic, natural-looking
- Industry-standard technique

**Cons**:

- More expensive to compute
- Requires noise library
- Overkill for simple shimmer

**Decision**: Consider for future enhancement; start with simple sin wave.

## Risks and Mitigations

| Risk                                   | Probability | Impact | Mitigation                                                                 |
| -------------------------------------- | ----------- | ------ | -------------------------------------------------------------------------- |
| **Performance degradation**            | Medium      | Medium | Benchmark; optimize if needed; make opt-in via flag                        |
| **Distracting visual effect**          | Low         | Low    | Tune amplitude/frequency; allow users to disable                           |
| **Breaks deterministic rendering**     | Low         | High   | Only apply when `timeSeconds > 0`; tests verify static rendering unchanged |
| **Platform-specific rendering issues** | Low         | Medium | Test on Windows/Linux; ensure Math.Sin behavior is consistent              |

## Success Criteria

1. ✅ **Shimmer implemented**: Water cells shimmer when flag enabled
2. ✅ **Opt-in**: Disabled by default, enabled via `PP_WATER_SHIMMER=1`
3. ✅ **Performance**: <5% overhead when enabled
4. ✅ **Deterministic**: Same time → same output
5. ✅ **Smooth animation**: No flicker or jitter
6. ✅ **Documented**: Users know how to enable/configure

## Timeline

- **Week 1**: Phase 1-2 (core implementation + animation integration) - **Priority P2**
- **Week 2**: Phase 3-4 (performance + documentation) - **Priority P2**

**Total effort**: ~2 weeks (part-time)

## Future Enhancements

### 1. River Flow Animation

Extend shimmer to rivers with directional flow:

- Use river path direction for shimmer phase
- Highlight effect moves downstream
- Creates "flowing water" visual

**Deferred**: Requires river flow direction in Cell model.

### 2. Day/Night Cycle

Modulate all colors based on time-of-day:

- Sunrise: warm tones
- Noon: full brightness
- Sunset: orange/red tones
- Night: desaturated, darkened

**Deferred**: Separate RFC (more complex).

### 3. Seasonal Color Shifts

Change biome colors based on season:

- Winter: more white/blue (snow)
- Spring: vibrant greens
- Summer: warm tones
- Fall: orange/brown

**Deferred**: Requires season parameter.

### 4. Configurable Shimmer Parameters

Expose shimmer amplitude/frequency to UI:

- Slider for shimmer intensity
- Dropdown for shimmer speed (slow/medium/fast)

**Deferred**: Add to color scheme configuration later.

## Open Questions

1. **Shimmer for lakes vs oceans**: Should they shimmer differently?
   - Recommendation: Same for now; could add lake-specific shimmer later (calmer, less amplitude)

2. **Shimmer amplitude**: Is ±10 RGB units too much/little?
   - Recommendation: Start with ±10, tune based on visual feedback

3. **Frame rate target**: Should we support lower FPS modes (30, 15)?
   - Recommendation: Yes, shimmer should work at any FPS (time-based, not frame-based)

4. **Multiple shimmer styles**: Should we offer different shimmer algorithms?
   - Recommendation: Future enhancement; start with one good implementation

## References

- Existing `timeSeconds` parameter in SkiaMapRasterizer (unused)
- Water shader techniques: https://developer.nvidia.com/gpugems/gpugems/part-i-natural-effects/chapter-1-effective-water-simulation-physical-models
- Sinusoidal animation basics: https://easings.net/

## Approval

- [ ] Architecture review
- [ ] Performance review
- [ ] Visual design review (shimmer parameters)
- [ ] Ready for implementation

---

**Next Steps**: Implement `ComputeWaterShimmer()` in `SkiaMapRasterizer.cs` and test with static time values.
