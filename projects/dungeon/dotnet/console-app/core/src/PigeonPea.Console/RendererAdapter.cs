using PigeonPea.Dungeon.Contracts;
using PigeonPea.Game.Contracts.Models;
using PigeonPea.Game.Contracts.Rendering;
using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;

namespace PigeonPea.Console;

public class RendererAdapter : PigeonPea.Game.Contracts.Rendering.IRenderer
{
    private readonly IDungeonRenderer _dungeonRenderer;
    private readonly PigeonPea.Rendering.Contracts.IRenderer _platformRenderer;
    private RenderContext? _context;

    public string Id => "adapter-renderer";
    public RenderingCapabilities Capabilities => RenderingCapabilities.ANSI;

    public RendererAdapter(
        IDungeonRenderer dungeonRenderer, 
        PigeonPea.Rendering.Contracts.IRenderer platformRenderer)
    {
        _dungeonRenderer = dungeonRenderer;
        _platformRenderer = platformRenderer;
        
        // Wire them up
        _dungeonRenderer.Initialize(_platformRenderer);
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
            // Render dungeon (which calls BeginFrame/EndFrame internally in current implementation)
            // Wait, DungeonRenderer.Render calls BeginFrame/EndFrame.
            // If we want to draw UI on top, we should split them.
            // But for now, let's assume DungeonRenderer handles the whole frame or at least the dungeon part.
            // If we need to draw text on top, we might have issues if EndFrame flushes.
            
            _dungeonRenderer.Render(state.Dungeon, state.PlayerX, state.PlayerY);
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
