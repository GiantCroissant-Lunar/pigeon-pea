namespace PigeonPea.Platform.Contracts.Rendering;

/// <summary>
/// Options for domain rendering operations.
/// Passed to domain renderers to control rendering behavior.
/// </summary>
public record RenderOptions
{
    /// <summary>
    /// Viewport defining the visible area
    /// </summary>
    public Viewport Viewport { get; init; }

    /// <summary>
    /// Zoom level (1.0 = normal, >1.0 = zoomed in, <1.0 = zoomed out)
    /// </summary>
    public double Zoom { get; init; } = 1.0;

    /// <summary>
    /// Optional scale configuration for multi-scale rendering
    /// </summary>
    public object? ActiveScale { get; init; }

    /// <summary>
    /// Whether to show overlays (minimap, tooltips, etc.)
    /// </summary>
    public bool ShowOverlays { get; init; }

    /// <summary>
    /// Whether to show debug information
    /// </summary>
    public bool ShowDebugInfo { get; init; }

    public RenderOptions(
        Viewport viewport,
        double zoom = 1.0,
        object? activeScale = null,
        bool showOverlays = false,
        bool showDebugInfo = false)
    {
        Viewport = viewport;
        Zoom = zoom;
        ActiveScale = activeScale;
        ShowOverlays = showOverlays;
        ShowDebugInfo = showDebugInfo;
    }
}
