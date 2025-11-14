# RFC-010 Phase 3: ViewModel Integration - Detailed Implementation Plan

## Status

**Status**: Ready for Implementation
**Phase**: 3 of 5
**Priority**: P1
**Estimated Effort**: 2-3 hours
**Prerequisites**: Phase 1 and Phase 2 must be completed

## Overview

Phase 3 integrates the color scheme selection capability into the ViewModel layer, enabling reactive UI controls to bind to color scheme settings and trigger re-renders when the scheme changes.

## Goals

1. Add `ColorScheme` property to appropriate ViewModel(s)
2. Expose available color schemes for UI binding
3. Ensure property changes trigger reactive updates
4. Maintain clean separation between Map domain and Shared infrastructure
5. Support both console and desktop UI scenarios

## Architecture Context

### Current State

- `MapRenderViewModel` exists in `dotnet/shared-app/ViewModels/MapRenderViewModel.cs`
- It's in the `PigeonPea.Shared.ViewModels` namespace
- Currently tracks viewport/camera state (CenterX, CenterY, Zoom, ViewportCols, ViewportRows)
- Uses ReactiveUI for property change notifications

### Target State

- `MapRenderViewModel` will have `ColorScheme` property for color scheme selection
- `ColorScheme` property will trigger re-renders when changed
- Available color schemes exposed for UI controls (ComboBox, Dropdown, etc.)

### Design Decisions

#### Decision 1: ViewModel Location

**Question**: Should we move `MapRenderViewModel` to `Map.Control`, or keep it in `Shared`?

**Options**:

- **Option A**: Move to `dotnet/Map/PigeonPea.Map.Control/ViewModels/MapRenderViewModel.cs`
  - **Pros**: Aligns with domain-driven organization; Map.Control owns map-specific ViewModels
  - **Cons**: Breaking change; requires updating all references

- **Option B**: Keep in `dotnet/shared-app/ViewModels/MapRenderViewModel.cs` and extend
  - **Pros**: No breaking changes; faster implementation
  - **Cons**: Doesn't follow domain organization; shared-app should be generic

**Recommendation**: **Option A** - Move to Map.Control

**Rationale**:

- RFC-007 established domain-driven organization
- `MapRenderViewModel` is map-specific (not dungeon, not generic rendering)
- Map.Control already exists and is the correct home for map ViewModels
- Better long-term maintainability

#### Decision 2: ColorScheme Property Scope

**Question**: Should ColorScheme be in MapRenderViewModel or a separate MapSettingsViewModel?

**Options**:

- **Option A**: Add to existing `MapRenderViewModel`
  - **Pros**: Simple; all map rendering settings in one place
  - **Cons**: ViewModel might grow too large over time

- **Option B**: Create separate `MapSettingsViewModel` or `MapStyleViewModel`
  - **Pros**: Better separation of concerns; room for future settings
  - **Cons**: More complexity; need to coordinate between ViewModels

**Recommendation**: **Option A** - Add to MapRenderViewModel

**Rationale**:

- Color scheme is fundamentally a rendering setting
- MapRenderViewModel is not yet too large
- Can refactor later if needed (YAGNI principle)

## Implementation Steps

### Step 1: Create ViewModels Directory in Map.Control

**Task**: Set up the directory structure for Map-specific ViewModels.

**Actions**:

```bash
mkdir -p dotnet/Map/PigeonPea.Map.Control/ViewModels
```

**Files Created**:

- `dotnet/Map/PigeonPea.Map.Control/ViewModels/` (directory)

**Success Criteria**:

- Directory exists
- Ready to receive ViewModel files

---

### Step 2: Move MapRenderViewModel to Map.Control

**Task**: Migrate the existing ViewModel from Shared to Map.Control domain.

**Source File**: `dotnet/shared-app/ViewModels/MapRenderViewModel.cs`
**Target File**: `dotnet/Map/PigeonPea.Map.Control/ViewModels/MapRenderViewModel.cs`

**Actions**:

1. **Copy file to new location** (don't delete source yet - we'll verify references first)

2. **Update namespace**:

   ```csharp
   // OLD:
   namespace PigeonPea.Shared.ViewModels;

   // NEW:
   namespace PigeonPea.Map.Control.ViewModels;
   ```

3. **Add XML documentation** (improve existing docs):

   ```csharp
   /// <summary>
   /// Reactive ViewModel for map rendering state, including camera position, zoom,
   /// viewport dimensions, and visual styling (color schemes).
   /// Independent from any specific UI (console/desktop).
   /// </summary>
   /// <remarks>
   /// This ViewModel follows the domain-driven architecture where Map.Control owns
   /// map-specific control logic and ViewModels. It uses ReactiveUI for property
   /// change notifications, enabling seamless integration with both console (Terminal.Gui)
   /// and desktop (Avalonia) UIs.
   /// </remarks>
   ```

4. **Verify the file compiles** in new location:
   ```bash
   dotnet build dotnet/Map/PigeonPea.Map.Control
   ```

**Files Modified**:

- `dotnet/Map/PigeonPea.Map.Control/ViewModels/MapRenderViewModel.cs` (created)

**Success Criteria**:

- File exists in new location
- Namespace is `PigeonPea.Map.Control.ViewModels`
- File compiles successfully

---

### Step 3: Add ColorScheme Property to MapRenderViewModel

**Task**: Extend the ViewModel with color scheme selection capability.

**File**: `dotnet/Map/PigeonPea.Map.Control/ViewModels/MapRenderViewModel.cs`

**Actions**:

1. **Add using directive** (at top of file):

   ```csharp
   using PigeonPea.Map.Core.Domain;
   ```

2. **Add private backing field** (after existing fields):

   ```csharp
   private ColorScheme _colorScheme = ColorScheme.Original;
   ```

3. **Add public property** (after existing properties):

   ```csharp
   /// <summary>
   /// Currently selected color scheme for map rendering.
   /// </summary>
   /// <remarks>
   /// Changing this property will trigger a PropertyChanged event, allowing
   /// UI-bound renderers to re-render the map with the new color scheme.
   /// Default is <see cref="ColorScheme.Original"/>.
   /// </remarks>
   public ColorScheme ColorScheme
   {
       get => _colorScheme;
       set => this.RaiseAndSetIfChanged(ref _colorScheme, value);
   }
   ```

4. **Add AvailableColorSchemes property** (for UI binding):
   ```csharp
   /// <summary>
   /// Gets all available color schemes for UI binding (ComboBox, Dropdown, etc.).
   /// </summary>
   /// <remarks>
   /// This property returns all values from the <see cref="ColorScheme"/> enum,
   /// allowing UI controls to automatically populate selection lists.
   /// </remarks>
   public IEnumerable<ColorScheme> AvailableColorSchemes =>
       Enum.GetValues<ColorScheme>();
   ```

**Code Example** (full updated class):

```csharp
using ReactiveUI;
using PigeonPea.Map.Core.Domain;

namespace PigeonPea.Map.Control.ViewModels;

/// <summary>
/// Reactive ViewModel for map rendering state, including camera position, zoom,
/// viewport dimensions, and visual styling (color schemes).
/// Independent from any specific UI (console/desktop).
/// </summary>
/// <remarks>
/// This ViewModel follows the domain-driven architecture where Map.Control owns
/// map-specific control logic and ViewModels. It uses ReactiveUI for property
/// change notifications, enabling seamless integration with both console (Terminal.Gui)
/// and desktop (Avalonia) UIs.
/// </remarks>
public class MapRenderViewModel : ReactiveObject
{
    private double _centerX;
    private double _centerY;
    private double _zoom = 1.0; // world cells per screen cell
    private int _viewportCols = 80;
    private int _viewportRows = 24;
    private ColorScheme _colorScheme = ColorScheme.Original;

    public double CenterX { get => _centerX; set => this.RaiseAndSetIfChanged(ref _centerX, value); }
    public double CenterY { get => _centerY; set => this.RaiseAndSetIfChanged(ref _centerY, value); }
    public double Zoom { get => _zoom; set => this.RaiseAndSetIfChanged(ref _zoom, value); }
    public int ViewportCols { get => _viewportCols; set => this.RaiseAndSetIfChanged(ref _viewportCols, value); }
    public int ViewportRows { get => _viewportRows; set => this.RaiseAndSetIfChanged(ref _viewportRows, value); }

    /// <summary>
    /// Currently selected color scheme for map rendering.
    /// </summary>
    /// <remarks>
    /// Changing this property will trigger a PropertyChanged event, allowing
    /// UI-bound renderers to re-render the map with the new color scheme.
    /// Default is <see cref="ColorScheme.Original"/>.
    /// </remarks>
    public ColorScheme ColorScheme
    {
        get => _colorScheme;
        set => this.RaiseAndSetIfChanged(ref _colorScheme, value);
    }

    /// <summary>
    /// Gets all available color schemes for UI binding (ComboBox, Dropdown, etc.).
    /// </summary>
    /// <remarks>
    /// This property returns all values from the <see cref="ColorScheme"/> enum,
    /// allowing UI controls to automatically populate selection lists.
    /// </remarks>
    public IEnumerable<ColorScheme> AvailableColorSchemes =>
        Enum.GetValues<ColorScheme>();
}
```

**Files Modified**:

- `dotnet/Map/PigeonPea.Map.Control/ViewModels/MapRenderViewModel.cs`

**Success Criteria**:

- ColorScheme property exists and uses ReactiveUI
- AvailableColorSchemes returns all enum values
- File compiles successfully
- No warnings or errors

---

### Step 4: Update Map.Control Project References

**Task**: Ensure Map.Control has necessary dependencies.

**File**: `dotnet/Map/PigeonPea.Map.Control/PigeonPea.Map.Control.csproj`

**Actions**:

1. **Read current project file** to check existing references

2. **Add missing references** (if needed):

   ```xml
   <ItemGroup>
     <!-- Domain reference for ColorScheme enum -->
     <ProjectReference Include="..\PigeonPea.Map.Core\PigeonPea.Map.Core.csproj" />

     <!-- ReactiveUI for ViewModels -->
     <PackageReference Include="ReactiveUI" Version="20.1.1" />
   </ItemGroup>
   ```

3. **Verify build**:
   ```bash
   dotnet build dotnet/Map/PigeonPea.Map.Control
   ```

**Files Modified**:

- `dotnet/Map/PigeonPea.Map.Control/PigeonPea.Map.Control.csproj`

**Success Criteria**:

- Project references Map.Core (for ColorScheme enum)
- Project references ReactiveUI (for ReactiveObject)
- Project builds successfully

---

### Step 5: Find and Update All References to Old ViewModel

**Task**: Update all code that references the old Shared.ViewModels.MapRenderViewModel.

**Search Strategy**:

```bash
# Search for old namespace usage
grep -r "PigeonPea.Shared.ViewModels" dotnet/
grep -r "using.*Shared.*ViewModels" dotnet/

# Search for MapRenderViewModel instantiation
grep -r "new MapRenderViewModel" dotnet/
grep -r "MapRenderViewModel" dotnet/console-app/
```

**Expected Files to Update**:

- `dotnet/console-app/Views/BrailleMapPanelView.cs` (likely)
- `dotnet/console-app/Views/PixelMapPanelView.cs` (likely)
- `dotnet/console-app/TerminalHudApplication.cs` (likely)
- Any Avalonia UI files (if they exist)

**Actions for Each File**:

1. **Replace using directive**:

   ```csharp
   // OLD:
   using PigeonPea.Shared.ViewModels;

   // NEW:
   using PigeonPea.Map.Control.ViewModels;
   ```

2. **Update project reference** (in .csproj files):

   ```xml
   <!-- Add reference to Map.Control instead of or in addition to shared-app -->
   <ProjectReference Include="..\Map\PigeonPea.Map.Control\PigeonPea.Map.Control.csproj" />
   ```

3. **Test compilation**:
   ```bash
   dotnet build dotnet/console-app
   ```

**Files Modified**:

- All files that reference `MapRenderViewModel` (TBD based on search results)
- Console app project file (likely)
- Any UI project files (likely)

**Success Criteria**:

- All references updated to new namespace
- All projects compile successfully
- No warnings about missing types

---

### Step 6: Wire ColorScheme to Rendering Pipeline (Preparation)

**Task**: Update code that calls `SkiaMapRasterizer.Render()` to pass the color scheme.

**Note**: This step prepares for Phase 4 (UI integration) by ensuring the ViewModel's ColorScheme property is passed through to the renderer.

**Expected Files to Update**:

- `dotnet/console-app/Views/BrailleMapPanelView.cs` (if it uses SkiaMapRasterizer)
- `dotnet/console-app/Views/PixelMapPanelView.cs` (if it uses SkiaMapRasterizer)
- Any other view that renders maps

**Example Update**:

```csharp
// OLD (Phase 2):
var raster = SkiaMapRasterizer.Render(
    mapData,
    viewport,
    zoom,
    pixelsPerCell: 4,
    biomeColors: true,
    rivers: false,
    timeSeconds: 0,
    layers: null,
    colorScheme: ColorScheme.Original  // Hardcoded default
);

// NEW (Phase 3):
var raster = SkiaMapRasterizer.Render(
    mapData,
    viewport,
    zoom,
    pixelsPerCell: 4,
    biomeColors: true,
    rivers: false,
    timeSeconds: 0,
    layers: null,
    colorScheme: _viewModel.ColorScheme  // From ViewModel!
);
```

**Actions**:

1. **Find all calls to SkiaMapRasterizer.Render()**:

   ```bash
   grep -r "SkiaMapRasterizer.Render" dotnet/
   ```

2. **Update each call** to use `_viewModel.ColorScheme` or similar

3. **Subscribe to PropertyChanged** (if not already subscribed):
   ```csharp
   _viewModel.PropertyChanged += (sender, args) =>
   {
       if (args.PropertyName == nameof(MapRenderViewModel.ColorScheme))
       {
           // Trigger re-render
           InvalidateView();
       }
   };
   ```

**Files Modified**:

- View files that call SkiaMapRasterizer.Render() (TBD based on search results)

**Success Criteria**:

- All render calls use ViewModel's ColorScheme property
- Changing ColorScheme triggers re-render
- No hardcoded ColorScheme values remain

---

### Step 7: Delete Old ViewModel File

**Task**: Remove the old file from shared-app after verifying all references are updated.

**Actions**:

1. **Verify no remaining references**:

   ```bash
   grep -r "PigeonPea.Shared.ViewModels" dotnet/
   # Should return no results (except maybe in this plan document)
   ```

2. **Delete old file**:

   ```bash
   rm dotnet/shared-app/ViewModels/MapRenderViewModel.cs
   ```

3. **Verify builds still work**:
   ```bash
   dotnet build
   ```

**Files Deleted**:

- `dotnet/shared-app/ViewModels/MapRenderViewModel.cs`

**Success Criteria**:

- Old file is deleted
- No compilation errors
- All tests pass

---

### Step 8: Update Tests (If Needed)

**Task**: Update any unit tests that reference MapRenderViewModel.

**Search Strategy**:

```bash
grep -r "MapRenderViewModel" dotnet/**/*.Tests/
```

**Actions**:

1. **Update using directives** in test files:

   ```csharp
   using PigeonPea.Map.Control.ViewModels;
   ```

2. **Add tests for ColorScheme property**:

   ```csharp
   [Fact]
   public void ColorScheme_DefaultsToOriginal()
   {
       var vm = new MapRenderViewModel();
       Assert.Equal(ColorScheme.Original, vm.ColorScheme);
   }

   [Fact]
   public void ColorScheme_RaisesPropertyChanged()
   {
       var vm = new MapRenderViewModel();
       bool raised = false;
       vm.PropertyChanged += (s, e) =>
       {
           if (e.PropertyName == nameof(MapRenderViewModel.ColorScheme))
               raised = true;
       };

       vm.ColorScheme = ColorScheme.Fantasy;

       Assert.True(raised);
       Assert.Equal(ColorScheme.Fantasy, vm.ColorScheme);
   }

   [Fact]
   public void AvailableColorSchemes_ReturnsAllSchemes()
   {
       var vm = new MapRenderViewModel();
       var schemes = vm.AvailableColorSchemes.ToList();

       Assert.Contains(ColorScheme.Original, schemes);
       Assert.Contains(ColorScheme.Realistic, schemes);
       Assert.Contains(ColorScheme.Fantasy, schemes);
       Assert.Contains(ColorScheme.HighContrast, schemes);
       Assert.Contains(ColorScheme.Monochrome, schemes);
       Assert.Contains(ColorScheme.Parchment, schemes);
   }
   ```

**Files Modified**:

- Test files that reference MapRenderViewModel (TBD based on search results)

**Success Criteria**:

- All tests pass
- New tests verify ColorScheme property behavior

---

## Validation Checklist

After completing all steps, verify:

- [ ] `MapRenderViewModel` exists in `dotnet/Map/PigeonPea.Map.Control/ViewModels/`
- [ ] Namespace is `PigeonPea.Map.Control.ViewModels`
- [ ] `ColorScheme` property exists with ReactiveUI support
- [ ] `AvailableColorSchemes` property returns all enum values
- [ ] Map.Control project references Map.Core and ReactiveUI
- [ ] All references to old ViewModel location are updated
- [ ] Old file in shared-app is deleted
- [ ] All projects compile successfully
- [ ] All tests pass
- [ ] Render calls use `_viewModel.ColorScheme` (preparation for Phase 4)
- [ ] PropertyChanged events trigger re-renders

## Testing Strategy

### Manual Testing

1. **Instantiate ViewModel**:

   ```csharp
   var vm = new PigeonPea.Map.Control.ViewModels.MapRenderViewModel();
   ```

2. **Verify default value**:

   ```csharp
   Assert.Equal(ColorScheme.Original, vm.ColorScheme);
   ```

3. **Change color scheme**:

   ```csharp
   vm.ColorScheme = ColorScheme.Fantasy;
   // Should trigger PropertyChanged event
   ```

4. **Enumerate available schemes**:
   ```csharp
   var schemes = vm.AvailableColorSchemes.ToList();
   // Should return 6 schemes
   ```

### Automated Testing

Create `MapRenderViewModelTests.cs` in `dotnet/Map/PigeonPea.Map.Control.Tests/` (if test project exists):

```csharp
using Xunit;
using PigeonPea.Map.Control.ViewModels;
using PigeonPea.Map.Core.Domain;

namespace PigeonPea.Map.Control.Tests.ViewModels;

public class MapRenderViewModelTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        var vm = new MapRenderViewModel();

        Assert.Equal(0, vm.CenterX);
        Assert.Equal(0, vm.CenterY);
        Assert.Equal(1.0, vm.Zoom);
        Assert.Equal(80, vm.ViewportCols);
        Assert.Equal(24, vm.ViewportRows);
        Assert.Equal(ColorScheme.Original, vm.ColorScheme);
    }

    [Fact]
    public void ColorScheme_CanBeChanged()
    {
        var vm = new MapRenderViewModel();
        vm.ColorScheme = ColorScheme.Fantasy;
        Assert.Equal(ColorScheme.Fantasy, vm.ColorScheme);
    }

    [Fact]
    public void ColorScheme_RaisesPropertyChanged()
    {
        var vm = new MapRenderViewModel();
        bool raised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MapRenderViewModel.ColorScheme))
                raised = true;
        };

        vm.ColorScheme = ColorScheme.Realistic;

        Assert.True(raised, "PropertyChanged should be raised for ColorScheme");
    }

    [Fact]
    public void AvailableColorSchemes_ContainsAllSchemes()
    {
        var vm = new MapRenderViewModel();
        var schemes = vm.AvailableColorSchemes.ToList();

        Assert.Equal(6, schemes.Count);
        Assert.Contains(ColorScheme.Original, schemes);
        Assert.Contains(ColorScheme.Realistic, schemes);
        Assert.Contains(ColorScheme.Fantasy, schemes);
        Assert.Contains(ColorScheme.HighContrast, schemes);
        Assert.Contains(ColorScheme.Monochrome, schemes);
        Assert.Contains(ColorScheme.Parchment, schemes);
    }

    [Theory]
    [InlineData(ColorScheme.Original)]
    [InlineData(ColorScheme.Realistic)]
    [InlineData(ColorScheme.Fantasy)]
    [InlineData(ColorScheme.HighContrast)]
    [InlineData(ColorScheme.Monochrome)]
    [InlineData(ColorScheme.Parchment)]
    public void ColorScheme_CanBeSetToAnyValidValue(ColorScheme scheme)
    {
        var vm = new MapRenderViewModel();
        vm.ColorScheme = scheme;
        Assert.Equal(scheme, vm.ColorScheme);
    }
}
```

## Common Issues and Solutions

### Issue 1: Namespace Not Found

**Symptom**: `The type or namespace 'Map' does not exist in the namespace 'PigeonPea'`

**Solution**:

- Ensure Map.Control project is built: `dotnet build dotnet/Map/PigeonPea.Map.Control`
- Add project reference in consuming projects
- Clean and rebuild solution

### Issue 2: ColorScheme Type Not Found

**Symptom**: `The type or namespace name 'ColorScheme' could not be found`

**Solution**:

- Ensure Map.Core is referenced in Map.Control
- Verify ColorScheme.cs exists in Map.Core
- Add `using PigeonPea.Map.Core.Domain;` in ViewModel

### Issue 3: ReactiveUI Not Available

**Symptom**: `The type or namespace name 'ReactiveObject' could not be found`

**Solution**:

- Add ReactiveUI package reference to Map.Control.csproj
- Run `dotnet restore`
- Verify package version compatibility

### Issue 4: PropertyChanged Not Firing

**Symptom**: Changing ColorScheme doesn't trigger re-render

**Solution**:

- Ensure using `this.RaiseAndSetIfChanged()` (not manual field set)
- Verify subscription to `PropertyChanged` event in view
- Check that view's `InvalidateView()` or equivalent is called

## Success Criteria

Phase 3 is complete when:

1. ✅ MapRenderViewModel exists in Map.Control.ViewModels namespace
2. ✅ ColorScheme property implemented with ReactiveUI support
3. ✅ AvailableColorSchemes property returns all enum values
4. ✅ All references to old ViewModel location updated
5. ✅ Old shared-app ViewModel file deleted
6. ✅ All projects compile without errors or warnings
7. ✅ Unit tests pass (existing + new ColorScheme tests)
8. ✅ PropertyChanged events work correctly
9. ✅ Render calls prepared to use ViewModel.ColorScheme

## Next Steps

After Phase 3 completion, proceed to:

**Phase 4: UI Controls Integration**

- Add color scheme selector to console HUD (Terminal.Gui)
- Add color scheme selector to desktop settings (Avalonia)
- Wire up to MapRenderViewModel.ColorScheme property
- Test live color scheme switching

## References

- [RFC-010 Main Document](./010-color-scheme-configuration.md)
- [RFC-007 Domain Organization](../architecture/domain-organization.md)
- [ReactiveUI Documentation](https://www.reactiveui.net/docs/)
- [Terminal.Gui Documentation](https://gui-cs.github.io/Terminal.Gui/)
- [Avalonia UI Documentation](https://docs.avaloniaui.net/)

## Timeline

**Estimated Time**: 2-3 hours

**Breakdown**:

- Step 1-2: Create directory + move file (15 min)
- Step 3: Add ColorScheme properties (15 min)
- Step 4: Update project references (10 min)
- Step 5: Find and update references (30-60 min)
- Step 6: Wire to rendering pipeline (30 min)
- Step 7: Delete old file (5 min)
- Step 8: Update tests (30 min)
- Validation and testing (30 min)

---

**Author**: Claude Code
**Created**: 2025-11-13
**Last Updated**: 2025-11-13
