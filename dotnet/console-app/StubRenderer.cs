using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;

namespace PigeonPea.Console;

/// <summary>
/// Stub renderer for console app until Terminal.Gui rendering is migrated to IRenderer.
/// This is a temporary implementation that does nothing.
/// </summary>
internal class StubRenderer : IRenderer
{
    public RendererCapabilities Capabilities => RendererCapabilities.CharacterBased;

    public void Initialize(IRenderTarget target) { }
    public void BeginFrame() { }
    public void EndFrame() { }
    public void DrawTile(int x, int y, Tile tile) { }
    public void DrawText(int x, int y, string text, Color foreground, Color background) { }
    public void Clear(Color color) { }
    public void SetViewport(Viewport viewport) { }
}
