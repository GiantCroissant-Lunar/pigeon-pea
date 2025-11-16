namespace PigeonPea.Shared.Rendering;

public interface IRenderTarget
{
    int Width { get; }
    int Height { get; }
    int? PixelWidth { get; }
    int? PixelHeight { get; }
    void Present();
}
