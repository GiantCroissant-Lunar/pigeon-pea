using PigeonPea.Game.Contracts.Models;
using GameRenderContext = PigeonPea.Game.Contracts.Rendering.RenderContext;
using GameRenderer = PigeonPea.Game.Contracts.Rendering.IRenderer;
using RenderSurface = PigeonPea.Game.Contracts.Rendering.IRenderSurface;
using SharedRenderer = PigeonPea.Shared.Rendering.IRenderer;
using SharedRenderTarget = PigeonPea.Shared.Rendering.IRenderTarget;
using SadRogue.Primitives;
using Terminal.Gui;

namespace PigeonPea.Console;

public class PluginRendererPanelView : View
{
    private readonly GameRenderer _pluginRenderer;
    private readonly GameRenderContext _renderContext;
    private readonly GameState _gameState;
    private readonly SharedRenderer _surfaceRenderer;
    private readonly SharedRenderTarget _renderTarget;
    private readonly RenderSurface _surfaceAdapter;
    private bool _initialized;

    public PluginRendererPanelView(GameRenderer pluginRenderer, GameRenderContext renderContext, GameState gameState)
    {
        _pluginRenderer = pluginRenderer;
        _renderContext = renderContext;
        _gameState = gameState;

        _surfaceRenderer = new TerminalGuiRenderer(new PigeonPea.Console.Rendering.AsciiRenderer(true));
        _renderTarget = new TerminalGuiRenderTarget(this);
        _surfaceRenderer.Initialize(_renderTarget);

        _surfaceAdapter = new PluginRenderSurfaceAdapter(_surfaceRenderer);
        _renderContext.Surface = _surfaceAdapter;
        _initialized = false;
    }

    protected override bool OnDrawingContent()
    {
        var width = Viewport.Width;
        var height = Viewport.Height;
        if (width <= 0 || height <= 0)
        {
            return true;
        }

        _renderContext.Width = width;
        _renderContext.Height = height;

        if (_surfaceRenderer is TerminalGuiRenderer tgui)
        {
            tgui.SetDriver(Driver);
        }

        if (!_initialized)
        {
            _pluginRenderer.Initialize(_renderContext);
            _initialized = true;
        }

        _pluginRenderer.Render(_gameState);
        return true;
    }
}

public class PluginRenderSurfaceAdapter : RenderSurface
{
    private readonly SharedRenderer _renderer;

    public PluginRenderSurfaceAdapter(SharedRenderer renderer)
    {
        _renderer = renderer;
    }

    public void BeginFrame()
    {
        _renderer.BeginFrame();
    }

    public void EndFrame()
    {
        _renderer.EndFrame();
    }

    public void Clear(byte r, byte g, byte b)
    {
        _renderer.Clear(new SadRogue.Primitives.Color(r, g, b));
    }

    public void SetViewport(int x, int y, int width, int height)
    {
        _renderer.SetViewport(new PigeonPea.Shared.Rendering.Viewport(x, y, width, height));
    }

    public void DrawText(int x, int y, string text, byte foregroundR, byte foregroundG, byte foregroundB, byte backgroundR, byte backgroundG, byte backgroundB)
    {
        var fg = new SadRogue.Primitives.Color(foregroundR, foregroundG, foregroundB);
        var bg = new SadRogue.Primitives.Color(backgroundR, backgroundG, backgroundB);
        _renderer.DrawText(x, y, text, fg, bg);
    }
}
