using SadRogue.Primitives;

namespace PigeonPea.Shared.Rendering;

/// <summary>
/// Defines the camera viewport for rendering.
/// </summary>
public struct Viewport
{
    /// <summary>
    /// Gets or sets the X position of the viewport in world coordinates.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Gets or sets the Y position of the viewport in world coordinates.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Gets or sets the width of the viewport in grid cells.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the viewport in grid cells.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Viewport"/> struct.
    /// </summary>
    /// <param name="x">The X position of the viewport.</param>
    /// <param name="y">The Y position of the viewport.</param>
    /// <param name="width">The width of the viewport.</param>
    /// <param name="height">The height of the viewport.</param>
    public Viewport(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets the bounding rectangle of the viewport.
    /// </summary>
    public Rectangle Bounds => new(X, Y, Width, Height);

    /// <summary>
    /// Checks if the specified coordinates are within the viewport bounds.
    /// </summary>
    /// <param name="x">The X coordinate to check.</param>
    /// <param name="y">The Y coordinate to check.</param>
    /// <returns>True if the coordinates are within the viewport; otherwise, false.</returns>
    public bool Contains(int x, int y)
        => x >= X && x < X + Width && y >= Y && y < Y + Height;
}
