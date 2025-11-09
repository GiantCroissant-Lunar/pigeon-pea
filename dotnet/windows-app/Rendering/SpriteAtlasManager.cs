using System.Text.Json;
using SkiaSharp;

namespace PigeonPea.Windows.Rendering;

/// <summary>
/// Manages texture atlases and sprite extraction.
/// Loads PNG atlases with JSON sprite definitions and provides cached sprite access.
/// </summary>
public class SpriteAtlasManager : IDisposable
{
    private readonly Dictionary<int, SKImage> _sprites = new();
    private readonly Dictionary<string, SKBitmap> _atlases = new();
    private readonly Dictionary<string, List<SpriteDefinition>> _atlasDefinitions = new();
    private bool _disposed;

    /// <summary>
    /// Loads a texture atlas from a PNG file and its corresponding JSON sprite definitions.
    /// </summary>
    /// <param name="atlasPath">Path to the PNG atlas file.</param>
    /// <param name="definitionPath">Path to the JSON definition file.</param>
    /// <exception cref="FileNotFoundException">Thrown when the atlas or definition file is not found.</exception>
    /// <exception cref="JsonException">Thrown when the JSON definition is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when sprite extraction fails.</exception>
    public void LoadAtlas(string atlasPath, string definitionPath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!File.Exists(atlasPath))
            throw new FileNotFoundException($"Atlas file not found: {atlasPath}");

        if (!File.Exists(definitionPath))
            throw new FileNotFoundException($"Definition file not found: {definitionPath}");

        // Load the atlas image
        var bitmap = SKBitmap.Decode(atlasPath);
        if (bitmap == null)
            throw new InvalidOperationException($"Failed to decode atlas image: {atlasPath}");

        _atlases[atlasPath] = bitmap;

        // Load sprite definitions from JSON
        var jsonText = File.ReadAllText(definitionPath);
        var definitions = JsonSerializer.Deserialize<List<SpriteDefinition>>(jsonText);

        if (definitions == null)
            throw new JsonException($"Failed to parse sprite definitions from: {definitionPath}");

        _atlasDefinitions[atlasPath] = definitions;

        // Extract and cache sprites
        foreach (var sprite in definitions)
        {
            ExtractSprite(bitmap, sprite);
        }
    }

    /// <summary>
    /// Checks if a sprite with the specified ID has been loaded.
    /// </summary>
    /// <param name="spriteId">The sprite ID to check.</param>
    /// <returns>True if the sprite exists, false otherwise.</returns>
    public bool HasSprite(int spriteId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _sprites.ContainsKey(spriteId);
    }

    /// <summary>
    /// Retrieves a sprite by its ID.
    /// </summary>
    /// <param name="spriteId">The sprite ID to retrieve.</param>
    /// <returns>The sprite image if found, null otherwise.</returns>
    public SKImage? GetSprite(int spriteId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _sprites.GetValueOrDefault(spriteId);
    }

    /// <summary>
    /// Gets all loaded sprite IDs.
    /// </summary>
    /// <returns>Collection of sprite IDs.</returns>
    public IEnumerable<int> GetLoadedSpriteIds()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _sprites.Keys;
    }

    /// <summary>
    /// Gets the number of loaded atlases.
    /// </summary>
    public int AtlasCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _atlases.Count;
        }
    }

    /// <summary>
    /// Gets the number of loaded sprites.
    /// </summary>
    public int SpriteCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _sprites.Count;
        }
    }

    /// <summary>
    /// Extracts a sprite from the atlas bitmap and caches it.
    /// </summary>
    private void ExtractSprite(SKBitmap atlas, SpriteDefinition definition)
    {
        // Validate sprite bounds
        if (definition.X < 0 || definition.Y < 0 ||
            definition.X + definition.Width > atlas.Width ||
            definition.Y + definition.Height > atlas.Height)
        {
            throw new InvalidOperationException(
                $"Sprite {definition.Id} bounds ({definition.X},{definition.Y},{definition.Width},{definition.Height}) " +
                $"exceed atlas dimensions ({atlas.Width}x{atlas.Height})");
        }

        // Create a subset bitmap for the sprite
        var subset = new SKBitmap();
        if (!atlas.ExtractSubset(subset, SKRectI.Create(definition.X, definition.Y, definition.Width, definition.Height)))
        {
            throw new InvalidOperationException($"Failed to extract sprite {definition.Id}");
        }

        // Convert to image and cache
        var image = SKImage.FromBitmap(subset);
        _sprites[definition.Id] = image;

        // Dispose the temporary subset bitmap
        subset.Dispose();
    }

    /// <summary>
    /// Disposes all loaded resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose all cached sprites
            foreach (var sprite in _sprites.Values)
            {
                sprite.Dispose();
            }
            _sprites.Clear();

            // Dispose all atlases
            foreach (var atlas in _atlases.Values)
            {
                atlas.Dispose();
            }
            _atlases.Clear();

            _atlasDefinitions.Clear();
        }

        _disposed = true;
    }
}
