using SadRogue.Primitives;

namespace PigeonPea.Platform.Contracts.Rendering;

/// <summary>
/// Defines the camera viewport for rendering.
/// </summary>
public struct Viewport : System.IEquatable<Viewport>
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

    public override bool Equals(object? obj)
    {
        return obj is Viewport other && Equals(other);
    }

    public bool Equals(Viewport other)
    {
        return X == other.X
               && Y == other.Y
               && Width == other.Width
               && Height == other.Height;
    }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(X, Y, Width, Height);
    }

    public static bool operator ==(Viewport left, Viewport right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Viewport left, Viewport right)
    {
        return !left.Equals(right);
    }
}
