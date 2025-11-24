using SadRogue.Primitives;

namespace PigeonPea.Rendering.Contracts;

public interface IRenderer
{
    // Identification
    string Id { get; }
    RendererCapabilities Capabilities { get; }

    // Lifecycle
    void Initialize(IRenderTarget target);
    void Shutdown();

    // Frame management
    void BeginFrame();
    void EndFrame();

    // Drawing operations
    void Clear(Color color);
    void SetViewport(Viewport viewport);
    void DrawTile(int x, int y, Tile tile);
    void DrawText(int x, int y, string text, Color foreground, Color background);
}
