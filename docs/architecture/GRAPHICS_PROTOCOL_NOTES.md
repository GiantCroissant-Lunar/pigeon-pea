# Graphics Protocol Notes for WezTerm on Windows

## Tested Protocols

### Kitty Graphics Protocol ? DOES NOT WORK

- Tested: Yes (see test_kitty\*.py files)
- Result: Does not render on WezTerm Windows build 20240203
- Reason: WezTerm on Windows does not support Kitty graphics protocol

### iTerm2 Inline Images ? DOES NOT WORK PROPERLY

- Tested: Yes
- Result: Image appears as thin strip on right side of screen instead of filling panel
- Issue: Raw escape sequences conflict with Terminal.Gui's render cycle
- Symptoms: 2560x400px image sent with both `cell` and `px` units, still appears incorrectly
- Root Cause: Rendering raw escape sequences during Terminal.Gui's render cycle causes Terminal.Gui to overwrite or misposition the image

### Sixel Graphics ? NOW USING Terminal.Gui's API

- WezTerm documentation states Sixel is supported
- **Solution**: Use Terminal.Gui 2.0's built-in `Application.Sixel` API instead of raw escape sequences
- **How**: Create `Terminal.Gui.SixelToRender` objects and add them via `Application.Sixel.Add()`
- **Benefit**: Terminal.Gui manages the render lifecycle and positioning automatically
- Status: Implemented in PixelMapPanelView.cs

## Key Learning

**DO NOT render raw escape sequences during Terminal.Gui's render cycle!**

Instead, use Terminal.Gui's built-in graphics APIs:

- `Application.Sixel` for Sixel graphics
- Let Terminal.Gui handle the rendering timing and positioning

## Final Decision: Use Braille Rendering

**Pixel graphics (Sixel/Kitty/iTerm2) do NOT work reliably in Terminal.Gui v2 on Windows.**

Even Terminal.Gui's own Sixel example fails on Windows 11 + WezTerm. This is a platform limitation, not our bug.

**Solution**: Use **Braille Unicode patterns** (U+2800 - U+28FF) for high-resolution character-based rendering:

- 2x4 dots per character cell = 4x better resolution than ASCII
- Works on ALL terminals (Windows, Mac, Linux)
- No graphics protocol dependencies
- Integrates perfectly with Terminal.Gui v2's character rendering
- Used successfully in terminal image viewers (timg, viu)

## Recommendations

1. ? **Use Braille rendering for maps in Terminal.Gui v2**
2. ? Keep pixel graphics for `--map-demo` mode (direct console access)
3. ? Do NOT attempt Kitty on Windows/WezTerm
4. ? Do NOT use Sixel/iTerm2 inside Terminal.Gui v2 views
5. ? Follow Terminal.Gui examples: use `AddRune()` for character placement

## References

- **ARCHITECTURE_PLAN.md**: Complete plan for Braille rendering + Mapsui integration
- **Snake.cs example**: Shows proper Terminal.Gui v2 character rendering
- **TextEffectsScenario.cs**: Shows LineCanvas usage
