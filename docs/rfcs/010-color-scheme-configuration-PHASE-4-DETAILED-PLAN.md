# RFC-010 Phase 4: UI Controls - Detailed Implementation Plan

**Status**: Ready for Implementation
**Created**: 2025-11-14
**Phase**: 4 of 5
**Priority**: P1
**Estimated Effort**: 2-3 days

## Prerequisites

**Must be completed first**:
- ✅ Phase 1: Core Color Scheme System (`ColorScheme` enum, `ColorSchemes` class)
- ✅ Phase 2: Renderer Integration (`SkiaMapRasterizer` uses `ColorSchemes`)
- ✅ Phase 3: ViewModel Integration (`MapViewModel.ColorScheme` property exists)

**Verify before starting**:
```bash
# Verify ColorScheme enum exists
grep -r "enum ColorScheme" dotnet/Map/

# Verify MapViewModel has ColorScheme property
grep -r "ColorScheme ColorScheme" dotnet/Map/PigeonPea.Map.Control/

# Run tests to ensure Phase 1-3 work
dotnet test dotnet/Map/PigeonPea.Map.Rendering.Tests/
```

## Overview

Phase 4 adds **user-facing controls** to select color schemes in both console (Terminal.Gui) and desktop (Avalonia) applications, wiring them to the ViewModels implemented in Phase 3.

## Goals

1. Add color scheme selector to **console application** (Terminal.Gui HUD)
2. Add color scheme selector to **desktop application** (Avalonia UI)
3. Wire controls to `MapViewModel.ColorScheme` property
4. Ensure live updates (changing scheme triggers map re-render)
5. Persist selected scheme across sessions (if config system exists)

## Architecture

### Console Application Integration

**Component**: `TerminalHudApplication.cs`
**Location**: `dotnet/console-app/TerminalHudApplication.cs`
**UI Framework**: Terminal.Gui

**Current State**:
- HUD displays map with various controls (navigation, zoom, layers)
- Settings are managed through `MapViewModel`
- No color scheme selector exists yet

**Target State**:
- Add `ComboBox` or `RadioGroup` control for color scheme selection
- Position control in HUD layout (likely in settings panel or toolbar)
- Bind control to `MapViewModel.ColorScheme` property
- Trigger map re-render when scheme changes

### Desktop Application Integration

**Component**: Avalonia UI controls
**Location**: TBD (need to identify correct view/control file)
**UI Framework**: Avalonia

**Possible Integration Points**:
1. Main map view settings panel
2. Preferences/Settings window
3. Map parameters panel (if exists)

**Target State**:
- Add `ComboBox` control with XAML data binding
- Bind to `MapViewModel.AvailableColorSchemes` and `MapViewModel.ColorScheme`
- Ensure ReactiveUI triggers re-render on change

## Implementation Tasks

### Task 4.1: Console Application - Add Color Scheme Selector

**File**: `dotnet/console-app/TerminalHudApplication.cs`
**Estimated Time**: 3-4 hours

#### Subtasks

1. **Identify HUD layout structure** (30 min)
   - Read `TerminalHudApplication.cs` to understand current layout
   - Find where settings controls are placed (buttons, checkboxes, etc.)
   - Determine best location for color scheme selector

2. **Add ComboBox control** (1 hour)
   ```csharp
   // Pseudocode - actual implementation depends on Terminal.Gui version
   var colorSchemeLabel = new Label("Color Scheme:")
   {
       X = 1,
       Y = Pos.Bottom(_previousControl) + 1,
       Width = 15
   };

   var colorSchemeCombo = new ComboBox
   {
       X = Pos.Right(colorSchemeLabel) + 1,
       Y = Pos.Top(colorSchemeLabel),
       Width = 20,
       Height = 1
   };

   // Populate with enum values
   colorSchemeCombo.SetSource(Enum.GetNames<ColorScheme>());

   // Set initial selection from ViewModel
   colorSchemeCombo.SelectedItem = (int)_mapViewModel.ColorScheme;
   ```

3. **Wire to ViewModel** (1 hour)
   ```csharp
   colorSchemeCombo.SelectedItemChanged += (args) =>
   {
       if (args.Value >= 0)
       {
           var selectedScheme = (ColorScheme)args.Value;
           _mapViewModel.ColorScheme = selectedScheme;

           // Trigger re-render
           _mapPanel.InvalidateView(); // Or appropriate re-render method
           _mapPanel.SetNeedsDisplay();
       }
   };
   ```

4. **Test in console** (30 min)
   - Run console app: `dotnet run --project dotnet/console-app/`
   - Verify control appears in HUD
   - Test switching between schemes
   - Verify map updates with new colors

5. **Handle edge cases** (1 hour)
   - Test what happens if ViewModel is null
   - Ensure control doesn't crash on invalid selection
   - Add tooltips/help text (if Terminal.Gui supports)

#### Acceptance Criteria

- [ ] Color scheme selector appears in console HUD
- [ ] Dropdown shows all available schemes (Original, Realistic, Fantasy, etc.)
- [ ] Selecting a scheme updates `MapViewModel.ColorScheme`
- [ ] Map re-renders with new color scheme
- [ ] Initial selection matches ViewModel default (Original)
- [ ] No crashes or exceptions when switching schemes

#### Files Modified

- `dotnet/console-app/TerminalHudApplication.cs`

#### Testing Steps

```bash
# Run console app
cd dotnet/console-app
dotnet run

# Manual test checklist:
# 1. Launch app, verify color scheme dropdown visible
# 2. Default should be "Original"
# 3. Switch to "Monochrome" - map should turn grayscale
# 4. Switch to "Fantasy" - map should have vibrant colors
# 5. Switch back to "Original" - map should restore default colors
# 6. Verify no lag or crashes when switching
```

---

### Task 4.2: Desktop Application - Identify Integration Point

**Estimated Time**: 1-2 hours

#### Subtasks

1. **Locate Avalonia UI files** (30 min)
   ```bash
   # Search for Avalonia view files
   find dotnet/ -name "*.axaml" -o -name "*.xaml"

   # Likely locations:
   # - dotnet/_lib/fantasy-map-generator-port/src/FantasyMapGenerator.UI/
   # - dotnet/desktop-app/ (if exists)
   ```

2. **Identify map view or settings panel** (30 min)
   - Look for files like:
     - `MapView.axaml`
     - `MapParametersPanel.axaml`
     - `SettingsWindow.axaml`
     - `MainWindow.axaml`
   - Find where `MapViewModel` is used as DataContext

3. **Review existing controls** (30 min)
   - Identify how other settings are bound (zoom, layers, biome toggle)
   - Follow same pattern for color scheme selector

4. **Document integration point** (30 min)
   - Write down exact file path
   - Note DataContext binding path
   - Identify where to insert color scheme control in XAML

#### Output

Document findings in this section:

**Integration Point**: `<FILE_PATH_HERE>`
**DataContext**: `<BINDING_PATH_HERE>`
**Insertion Location**: `<GRID_ROW_OR_STACKPANEL_HERE>`

---

### Task 4.3: Desktop Application - Add Color Scheme Selector (XAML)

**File**: `<TO_BE_DETERMINED_IN_TASK_4.2>`
**Estimated Time**: 2-3 hours

#### Subtasks

1. **Add ComboBox to XAML** (1 hour)
   ```xaml
   <!-- Example XAML - adjust based on actual layout -->
   <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
       <TextBlock Text="Color Scheme:"
                  VerticalAlignment="Center"
                  Margin="0,0,8,0" />

       <ComboBox ItemsSource="{Binding AvailableColorSchemes}"
                 SelectedItem="{Binding ColorScheme}"
                 Width="150"
                 HorizontalAlignment="Left">
           <ComboBox.ItemTemplate>
               <DataTemplate>
                   <TextBlock Text="{Binding}" />
               </DataTemplate>
           </ComboBox.ItemTemplate>
       </ComboBox>
   </StackPanel>
   ```

2. **Verify DataContext binding** (30 min)
   - Ensure parent control has `DataContext="{Binding MapViewModel}"` (or similar)
   - If DataContext is not MapViewModel, adjust binding path:
     ```xaml
     ItemsSource="{Binding MapViewModel.AvailableColorSchemes}"
     SelectedItem="{Binding MapViewModel.ColorScheme}"
     ```

3. **Add converter if needed** (30 min)
   - If enum display names need formatting (e.g., "HighContrast" → "High Contrast")
   - Create `EnumToDisplayStringConverter` if not exists
   - Apply converter in XAML:
     ```xaml
     <TextBlock Text="{Binding Converter={StaticResource EnumToDisplayString}}" />
     ```

4. **Test in desktop app** (1 hour)
   - Build and run: `dotnet run --project <DESKTOP_PROJECT>`
   - Verify control appears
   - Test switching schemes
   - Verify map updates (may require ReactiveUI subscription)

#### Acceptance Criteria

- [ ] Color scheme selector appears in desktop UI
- [ ] Dropdown shows all available schemes
- [ ] Binding to `MapViewModel.ColorScheme` works (two-way binding)
- [ ] Selecting a scheme updates map rendering
- [ ] UI updates when ViewModel property changes programmatically
- [ ] No XAML binding errors in debug output

#### Files Modified

- `<AVALONIA_VIEW_FILE>.axaml`
- Possibly `<AVALONIA_VIEW_FILE>.axaml.cs` (code-behind, if needed)

#### Testing Steps

```bash
# Run desktop app
cd dotnet/<DESKTOP_PROJECT>
dotnet run

# Manual test checklist:
# 1. Launch app, find color scheme dropdown
# 2. Verify default selection matches ViewModel (Original)
# 3. Switch between schemes, verify map updates
# 4. Check debug output for binding errors
# 5. Test edge cases (close/reopen window, change while zooming, etc.)
```

---

### Task 4.4: Ensure Re-Render Triggers Work

**Estimated Time**: 2-3 hours

#### Problem Statement

Changing `MapViewModel.ColorScheme` must trigger map re-render. This may require:
- ReactiveUI subscriptions
- Manual `InvalidateVisual()` calls
- Updating render pipelines to pass `colorScheme` parameter

#### Subtasks

1. **Console App Re-Render** (1 hour)
   - Verify `_mapPanel.SetNeedsDisplay()` or equivalent is called
   - Test that map updates immediately after scheme change
   - If not working, add explicit re-render call in `ColorScheme` setter:
     ```csharp
     // In MapViewModel
     public ColorScheme ColorScheme
     {
         get => _colorScheme;
         set
         {
             this.RaiseAndSetIfChanged(ref _colorScheme, value);
             OnColorSchemeChanged(); // Custom hook
         }
     }
     ```

2. **Desktop App Re-Render** (1 hour)
   - Verify ReactiveUI subscription exists:
     ```csharp
     // In view code-behind or ViewModel
     this.WhenAnyValue(x => x.ViewModel.ColorScheme)
         .Subscribe(_ => InvalidateMapRender());
     ```
   - If map is rendered in a `Canvas` or `Image`, update its data source
   - Check if `SkiaMapRasterizer.Render()` is called with updated `colorScheme`

3. **Pass colorScheme to Renderer** (1 hour)
   - Find where `SkiaMapRasterizer.Render()` is called
   - Ensure `colorScheme` parameter is passed from ViewModel:
     ```csharp
     var raster = SkiaMapRasterizer.Render(
         map,
         viewport,
         zoom,
         ppc,
         biomeColors,
         rivers,
         timeSeconds,
         layers,
         colorScheme: _mapViewModel.ColorScheme // <-- ADD THIS
     );
     ```

4. **Test end-to-end** (30 min)
   - Change scheme in UI
   - Verify `SkiaMapRasterizer.Render()` receives updated scheme
   - Use breakpoints or logging to confirm
   - Verify rendered map has correct colors

#### Acceptance Criteria

- [ ] Changing color scheme in console app triggers immediate re-render
- [ ] Changing color scheme in desktop app triggers immediate re-render
- [ ] `SkiaMapRasterizer.Render()` receives correct `colorScheme` parameter
- [ ] No duplicate renders or performance issues
- [ ] Logging/debugging confirms color scheme propagation

#### Files Modified

- `dotnet/console-app/Views/BrailleMapPanelView.cs` (or similar)
- `dotnet/console-app/Views/PixelMapPanelView.cs` (or similar)
- Desktop app view code-behind files

---

### Task 4.5: Add Tooltips and Help Text

**Estimated Time**: 1 hour

#### Subtasks

1. **Console App Tooltips** (30 min)
   - If Terminal.Gui supports tooltips, add:
     ```csharp
     colorSchemeCombo.Tooltip = "Select map color palette (Original, Realistic, Fantasy, etc.)";
     ```
   - Or add a `Label` with help text below the control

2. **Desktop App Tooltips** (30 min)
   ```xaml
   <ComboBox ToolTip.Tip="Choose a color scheme for the map">
       <!-- ... -->
   </ComboBox>
   ```

#### Acceptance Criteria

- [ ] Users can hover/focus to see tooltip explaining control purpose
- [ ] Tooltip text is clear and concise

---

### Task 4.6: Persistence (Optional - depends on config system)

**Estimated Time**: 2-3 hours (if config system exists)

#### Subtasks

1. **Check if config system exists** (30 min)
   - Look for `appsettings.json`, `config.yaml`, or similar
   - Check if app uses .NET Configuration API
   - Example:
     ```bash
     grep -r "IConfiguration" dotnet/console-app/
     grep -r "appsettings" dotnet/console-app/
     ```

2. **Add ColorScheme to config** (1 hour)
   ```json
   // appsettings.json
   {
     "MapSettings": {
       "ColorScheme": "Original",
       "BiomeColors": true,
       "Rivers": false
     }
   }
   ```

3. **Load on startup** (30 min)
   ```csharp
   // In app initialization
   var config = Configuration.GetSection("MapSettings");
   var schemeName = config.GetValue<string>("ColorScheme", "Original");
   if (Enum.TryParse<ColorScheme>(schemeName, out var scheme))
   {
       _mapViewModel.ColorScheme = scheme;
   }
   ```

4. **Save on change** (1 hour)
   ```csharp
   // In ColorScheme setter or event handler
   private void OnColorSchemeChanged()
   {
       Configuration["MapSettings:ColorScheme"] = _colorScheme.ToString();
       ConfigurationManager.Save(); // Or appropriate save method
   }
   ```

#### Acceptance Criteria (if implemented)

- [ ] Selected color scheme is saved to config file
- [ ] On app restart, last selected scheme is restored
- [ ] Config file is valid JSON/YAML (no syntax errors)

#### Fallback (if no config system)

- Document in code comments that persistence is not implemented
- Log message: "Color scheme persistence not implemented yet"
- This is acceptable for Phase 4; can be added in Phase 5

---

## Testing Plan

### Manual Testing

**Console App**:
1. Run `dotnet run --project dotnet/console-app/`
2. Find color scheme dropdown in HUD
3. Test all schemes:
   - Original → default colors
   - Realistic → earth tones
   - Fantasy → vibrant colors
   - HighContrast → accessible colors
   - Monochrome → grayscale
   - Parchment → sepia tones
4. Verify map updates immediately after selection
5. Test edge cases:
   - Switch while panning
   - Switch while zooming
   - Switch multiple times rapidly

**Desktop App**:
1. Run desktop application
2. Find color scheme control
3. Test all schemes (same as console)
4. Verify ReactiveUI updates work
5. Check for XAML binding errors in debug output

### Automated Testing (Optional)

If UI testing framework exists:
```csharp
[Fact]
public void ColorSchemeSelector_ChangesViewModel()
{
    var viewModel = new MapViewModel();
    var comboBox = CreateColorSchemeComboBox(viewModel);

    comboBox.SelectedIndex = 2; // Fantasy

    Assert.Equal(ColorScheme.Fantasy, viewModel.ColorScheme);
}
```

### Visual Regression Testing

Capture screenshots of each color scheme for comparison:
```bash
# Generate reference images (manual or automated)
dotnet run --project dotnet/console-app/ -- --screenshot --scheme Original
dotnet run --project dotnet/console-app/ -- --screenshot --scheme Realistic
# ... etc
```

---

## Implementation Order

**Recommended sequence**:

1. **Task 4.1** → Console app selector (easier to test)
2. **Task 4.4** → Ensure re-render triggers work (critical path)
3. **Task 4.2** → Identify desktop integration point
4. **Task 4.3** → Desktop app selector
5. **Task 4.5** → Tooltips/help text
6. **Task 4.6** → Persistence (optional, can be deferred to Phase 5)

---

## Critical Files to Modify

| File                                   | Purpose                              | Estimated Lines Changed |
| -------------------------------------- | ------------------------------------ | ----------------------- |
| `console-app/TerminalHudApplication.cs` | Add ComboBox control                 | +30-50                  |
| `console-app/Views/BrailleMapPanelView.cs` | Ensure re-render on scheme change   | +5-10                   |
| `console-app/Views/PixelMapPanelView.cs`   | Ensure re-render on scheme change   | +5-10                   |
| `<desktop-view>.axaml`                 | Add XAML ComboBox                    | +10-20                  |
| `<desktop-view>.axaml.cs`              | ReactiveUI subscription (if needed)  | +10-20                  |

---

## Dependencies and Blockers

**Dependencies**:
- ✅ Phase 3 completed (`MapViewModel.ColorScheme` property exists)
- ✅ `ColorScheme` enum defined
- ✅ `ColorSchemes` class implemented

**Potential Blockers**:
1. **Terminal.Gui version compatibility**: ComboBox API may differ in older versions
   - Mitigation: Check Terminal.Gui documentation for correct API
2. **Avalonia view files don't exist**: Desktop app may not be fully implemented yet
   - Mitigation: Focus on console app first; document desktop integration for later
3. **ReactiveUI subscriptions not working**: ViewModel changes don't trigger re-render
   - Mitigation: Add explicit `INotifyPropertyChanged` or manual re-render calls

---

## Definition of Done

**Phase 4 is complete when**:

- [ ] Console app has color scheme selector in HUD
- [ ] Selecting a scheme in console app updates the map
- [ ] Desktop app has color scheme selector (or documented as "not yet implemented")
- [ ] Selecting a scheme in desktop app updates the map (if app exists)
- [ ] All 6 color schemes are selectable and functional
- [ ] Re-render triggers work reliably (no lag, no crashes)
- [ ] Tooltips/help text added to controls
- [ ] Manual testing completed for all schemes
- [ ] No regressions (existing features still work)
- [ ] Code passes pre-commit hooks (formatting, linting)
- [ ] Code reviewed (or self-review documented)

---

## Risks and Mitigations

| Risk                                      | Impact | Mitigation                                                  |
| ----------------------------------------- | ------ | ----------------------------------------------------------- |
| Terminal.Gui API changed                  | Medium | Check documentation; use reflection if needed               |
| Desktop app not ready for integration     | Low    | Focus on console app; defer desktop to Phase 5              |
| Re-render performance issues              | Medium | Profile rendering; optimize if needed (unlikely)            |
| Users confused by multiple schemes        | Low    | Add tooltips; document schemes in user guide (Phase 5)      |
| ViewModel binding broken                  | High   | Add unit tests for ViewModel; verify ReactiveUI setup       |

---

## Next Steps After Phase 4

**Phase 5: Persistence and Documentation**
- Save/load color scheme preference
- Write user guide with screenshots
- Add scheme comparison images to docs
- Publish release notes

**Future Enhancements** (not in this RFC):
- Custom user-defined schemes (JSON/YAML)
- Per-biome color overrides
- Animation support (day/night cycles)
- Colorblind-specific schemes (deuteranopia, protanopia)

---

## References

- **RFC-010 Main Document**: `docs/rfcs/010-color-scheme-configuration.md`
- **Phase 3 Instructions**: `docs/rfcs/010-color-scheme-configuration-PHASE-3-DETAILED-PLAN.md`
- **Terminal.Gui Docs**: https://gui-cs.github.io/Terminal.Gui/
- **Avalonia Docs**: https://docs.avaloniaui.net/
- **ReactiveUI Docs**: https://www.reactiveui.net/

---

## Approval Checklist

- [ ] Phase 1-3 completed and verified
- [ ] Console app codebase reviewed
- [ ] Desktop app codebase reviewed (or N/A documented)
- [ ] Terminal.Gui version confirmed compatible
- [ ] Avalonia version confirmed compatible (or N/A)
- [ ] Ready for implementation

---

**Author**: Claude Code
**Approved By**: _Awaiting Review_
**Implementation Start Date**: _TBD_
**Target Completion Date**: _TBD (2-3 days after start)_
