using PigeonPea.Dungeon.Contracts;
using PigeonPea.Game.Contracts.Models;
using PigeonPea.Game.Contracts.Rendering;
using PigeonPea.Rendering.Contracts;
using PigeonPea.Shared.Dungeon;
using PigeonPea.Shared.Scale;
using SadRogue.Primitives;

namespace PigeonPea.Console;

public class RendererAdapter : PigeonPea.Game.Contracts.Rendering.IRenderer
{
    private readonly IDungeonRenderer _dungeonRenderer;
    private readonly PigeonPea.Rendering.Contracts.IRenderer _platformRenderer;
    private readonly DungeonGridOverlaySource _overlaySource;
    private readonly IScaleManager? _scaleManager;
    private RenderContext? _context;

    public string Id => "adapter-renderer";
    public RenderingCapabilities Capabilities => RenderingCapabilities.ANSI;

    public RendererAdapter(
        IDungeonRenderer dungeonRenderer,
        PigeonPea.Rendering.Contracts.IRenderer platformRenderer,
        IScaleManager? scaleManager = null)
    {
        _dungeonRenderer = dungeonRenderer;
        _platformRenderer = platformRenderer;
        _overlaySource = new DungeonGridOverlaySource();
        _scaleManager = scaleManager;

        // Wire them up
        _dungeonRenderer.Initialize(_platformRenderer);

        // Set scale manager if available (for scale-aware overlay filtering)
        if (_scaleManager != null && _dungeonRenderer is Plugin.Dungeon.Rendering.DungeonRenderer dr)
        {
            dr.SetScaleManager(_scaleManager);
        }
    }

    public void Initialize(RenderContext context)
    {
        _context = context;
        // Initialize platform renderer with target adapter
        _platformRenderer.Initialize(new ContextRenderTarget(context));
    }

    public void Render(GameState state)
    {
        if (state.Dungeon != null)
        {
            // Use new overlay-based rendering with DungeonGridOverlaySource
            var overlays = _overlaySource.GetOverlays(state.Dungeon);
            _dungeonRenderer.RenderWithOverlays(
                state.Dungeon.Width,
                state.Dungeon.Height,
                state.Dungeon.Walkable,
                overlays,
                state.PlayerX,
                state.PlayerY,
                scale: 1
            );
        }
        else
        {
            _platformRenderer.BeginFrame();
            _platformRenderer.Clear(Color.Black);
            _platformRenderer.DrawText(0, 0, "No Dungeon", Color.White, Color.Black);
            _platformRenderer.EndFrame();
        }
    }

    public void Shutdown()
    {
        _platformRenderer.Shutdown();
    }
}

public class ContextRenderTarget : IRenderTarget
{
    private readonly RenderContext _context;
    public ContextRenderTarget(RenderContext context) => _context = context;
    public int Width => _context.Width;
    public int Height => _context.Height;
    public int? PixelWidth => null;
    public int? PixelHeight => null;
    public void Present() { }
}
