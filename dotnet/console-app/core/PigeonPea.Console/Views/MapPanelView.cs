using PigeonPea.Console.Rendering;
using PigeonPea.Map.Core;
using PigeonPea.Map.Rendering;
using PigeonPea.Shared.Rendering;
using Terminal.Gui;

namespace PigeonPea.Console;

/// <summary>
/// A Terminal.Gui View that renders MapData within its bounds using TerminalGuiRenderer.
/// </summary>
public class MapPanelView : View
{
    private readonly TerminalGuiRenderer _renderer;
    private readonly IRenderTarget _target;

    public MapPanelView(MapData map)
    {
        Map = map;
        // Use ASCII as underlying capability descriptor; TerminalGuiRenderer will draw via Driver
        _renderer = new TerminalGuiRenderer(new AsciiRenderer(true));
        _target = new TerminalGuiRenderTarget(this);
        _renderer.Initialize(_target);
        CanFocus = false;
    }

    public MapData Map { get; set; }
    public int CameraX { get; set; }
    public int CameraY { get; set; }
    public double Zoom { get; set; } = 1.0;

    protected override bool OnDrawingContent()
    {
        // Provide the current driver to the renderer
        _renderer.SetDriver(Driver);

        int w = Viewport.Width;
        int h = Viewport.Height;
        if (w <= 0 || h <= 0 || Map == null) return true;

        _renderer.BeginFrame();
        _renderer.SetViewport(new Viewport(0, 0, w, h));
        // Clear only inside this view
        _renderer.Clear(SadRogue.Primitives.Color.Black);

        // Placeholder while migrating to Map.Rendering pipeline
        _renderer.DrawText(0, 0, "Rendering via Map.Rendering pending migration", SadRogue.Primitives.Color.White, SadRogue.Primitives.Color.Black);
        _renderer.EndFrame();
        return true;
    }
}
