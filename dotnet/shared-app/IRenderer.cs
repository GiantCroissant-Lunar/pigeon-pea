using SadRogue.Primitives;

namespace PigeonPea.Shared;

/// <summary>
/// Platform-agnostic renderer interface.
/// Windows app implements with SkiaSharp, Console app with terminal graphics.
/// </summary>
public interface IRenderer
{
    void Clear();
    void DrawGlyph(int x, int y, char glyph, Color foreground, Color background);
    void DrawString(int x, int y, string text, Color foreground);
    void Present();
}

