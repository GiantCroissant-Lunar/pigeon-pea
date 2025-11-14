namespace PigeonPea.Map.Rendering;

/// <summary>
/// Defines available color schemes for map rendering, each with distinct aesthetic characteristics.
/// </summary>
public enum ColorScheme
{
    /// <summary>
    /// Original Azgaar-inspired palette with balanced saturation.
    /// Best for general-purpose fantasy maps with clear terrain distinction.
    /// </summary>
    Original,

    /// <summary>
    /// Satellite-like colors mimicking real-world terrain appearance.
    /// Ideal for realistic map visualization with natural earth tones.
    /// </summary>
    Realistic,

    /// <summary>
    /// Vibrant, high-saturation colors for dramatic fantasy maps.
    /// Enhanced visual appeal with bold terrain differentiation.
    /// </summary>
    Fantasy,

    /// <summary>
    /// Accessibility-focused palette with WCAG-compliant contrast ratios.
    /// Designed for maximum readability and visual accessibility.
    /// </summary>
    HighContrast,

    /// <summary>
    /// Pure grayscale gradient from black to white.
    /// Perfect for printing, documentation, and accessibility testing.
    /// </summary>
    Monochrome,

    /// <summary>
    /// Warm sepia tones reminiscent of aged parchment maps.
    /// Creates an antique, historical aesthetic for classic fantasy maps.
    /// </summary>
    Parchment
}
