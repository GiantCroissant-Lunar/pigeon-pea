using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;

namespace PigeonPea.Windows.MapHud;

public sealed class OffscreenBufferBackend : IRenderBackend
{
    private RasterSurface? _surface;

    public string Id => "offscreen-buffer-backend";

    public RenderingCapabilities Capabilities { get; private set; } = null!;

    public void Initialize(RenderContext context)
    {
        Capabilities = new RenderingCapabilities(
            supportsTiles: false,
            supportsBuffers: true,
            supportsSprites: false,
            supportsAntialiasing: false,
            maxWidth: context.Width,
            maxHeight: context.Height,
            mode: RenderMode.Buffer);

        _surface = new RasterSurface(context.Width, context.Height);
    }

    public void Shutdown()
    {
        _surface = null;
    }

    public void Execute(IRenderCommandList commands)
    {
        if (_surface == null) return;
        if (commands is not RenderCommandList list) return;

        foreach (var cmd in list.GetCommands())
        {
            switch (cmd.Type)
            {
                case RenderCommandType.BeginFrame:
                    _surface.Clear(Color.Black);
                    break;
                case RenderCommandType.DrawBuffer:
                    if (cmd.BufferData != null)
                    {
                        _surface.Blit(cmd.X, cmd.Y, cmd.Width, cmd.Height, cmd.BufferData);
                    }
                    break;
            }
        }
    }

    public void Present()
    {
        // No-op: the caller reads the buffer directly.
    }

    public RasterSurface? GetSurface() => _surface;

    public void Dispose()
    {
        Shutdown();
    }
}

public sealed class RasterSurface
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Rgba { get; }

    public RasterSurface(int width, int height)
    {
        Width = width;
        Height = height;
        Rgba = new byte[width * height * 4];
    }

    public void Clear(Color color)
    {
        for (int i = 0; i < Rgba.Length; i += 4)
        {
            Rgba[i] = color.R;
            Rgba[i + 1] = color.G;
            Rgba[i + 2] = color.B;
            Rgba[i + 3] = color.A;
        }
    }

    public void Blit(int x, int y, int width, int height, byte[] src)
    {
        int stride = Width * 4;
        for (int row = 0; row < height; row++)
        {
            int srcIndex = row * width * 4;
            int dstY = y + row;
            if (dstY < 0 || dstY >= Height) continue;
            int dstIndex = dstY * stride + x * 4;
            int copy = Math.Min(width * 4, Rgba.Length - dstIndex);
            if (copy <= 0) continue;
            Buffer.BlockCopy(src, srcIndex, Rgba, dstIndex, copy);
        }
    }
}
