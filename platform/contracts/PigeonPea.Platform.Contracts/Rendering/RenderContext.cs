namespace PigeonPea.Platform.Contracts.Rendering;

/// <summary>
/// Context information for initializing a render backend.
/// Provides display dimensions and configuration.
/// </summary>
public record RenderContext
{
    /// <summary>
    /// Width of the rendering surface in backend-specific units (cells or pixels)
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Height of the rendering surface in backend-specific units (cells or pixels)
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Optional title for windowed backends
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Optional configuration data for backend-specific setup
    /// </summary>
    public object? Configuration { get; init; }

    public RenderContext(int width, int height, string? title = null, object? configuration = null)
    {
        Width = width;
        Height = height;
        Title = title;
        Configuration = configuration;
    }
}
