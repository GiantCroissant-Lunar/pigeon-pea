using SadRogue.Primitives;

namespace PigeonPea.Shared.Rendering;

/// <summary>
/// Defines the camera viewport for rendering.
/// </summary>
public struct Viewport
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public Viewport(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public Rectangle Bounds => new(X, Y, Width, Height);

    public bool Contains(int x, int y)
        => x >= X && x < X + Width && y >= Y && y < Y + Height;
}
