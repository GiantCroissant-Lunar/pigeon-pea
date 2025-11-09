namespace PigeonPea.Windows.Rendering;

/// <summary>
/// Defines a sprite's location and dimensions within a texture atlas.
/// </summary>
public record SpriteDefinition
{
    /// <summary>
    /// Unique identifier for the sprite.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// X coordinate of the sprite in the atlas (in pixels).
    /// </summary>
    public required int X { get; init; }

    /// <summary>
    /// Y coordinate of the sprite in the atlas (in pixels).
    /// </summary>
    public required int Y { get; init; }

    /// <summary>
    /// Width of the sprite (in pixels).
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Height of the sprite (in pixels).
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// Optional name for the sprite.
    /// </summary>
    public string? Name { get; init; }
}
