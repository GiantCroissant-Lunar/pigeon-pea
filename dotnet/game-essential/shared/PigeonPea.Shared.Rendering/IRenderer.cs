using SadRogue.Primitives;

namespace PigeonPea.Shared.Rendering;

public interface IRenderer
{
    void Initialize(IRenderTarget target);
    void BeginFrame();
    void EndFrame();
    void DrawTile(int x, int y, Tile tile);
    void DrawText(int x, int y, string text, Color foreground, Color background);
    void Clear(Color color);
    void SetViewport(Viewport viewport);
    RendererCapabilities Capabilities { get; }
}
