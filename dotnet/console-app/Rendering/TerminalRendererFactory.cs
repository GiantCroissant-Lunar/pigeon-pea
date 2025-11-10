using PigeonPea.Shared.Rendering;

namespace PigeonPea.Console.Rendering;

/// <summary>
/// Factory for creating the best available terminal renderer based on terminal capabilities.
/// </summary>
public static class TerminalRendererFactory
{
    /// <summary>
    /// Renderer type options for manual override.
    /// </summary>
    public enum RendererType
    {
        /// <summary>
        /// Automatically detect the best available renderer.
        /// </summary>
        Auto,

        /// <summary>
        /// Kitty Graphics Protocol renderer.
        /// </summary>
        Kitty,

        /// <summary>
        /// Sixel graphics protocol renderer.
        /// </summary>
        Sixel,

        /// <summary>
        /// Unicode Braille pattern renderer.
        /// </summary>
        Braille,

        /// <summary>
        /// Basic ASCII renderer (most compatible).
        /// </summary>
        Ascii
    }

    /// <summary>
    /// Creates the best available terminal renderer based on detected capabilities.
    /// </summary>
    /// <param name="rendererType">Optional renderer type override. Use RendererType.Auto for automatic detection.</param>
    /// <returns>An IRenderer instance for the best available terminal graphics mode.</returns>
    public static IRenderer CreateRenderer(RendererType rendererType = RendererType.Auto)
    {
        // Detect terminal capabilities
        var capabilities = TerminalCapabilities.Detect();

        return CreateRenderer(capabilities, rendererType);
    }

    /// <summary>
    /// Creates the best available terminal renderer based on provided capabilities.
    /// This overload is useful for testing or when capabilities are already known.
    /// </summary>
    /// <param name="capabilities">Terminal capabilities to use for renderer selection.</param>
    /// <param name="rendererType">Optional renderer type override. Use RendererType.Auto for automatic detection.</param>
    /// <returns>An IRenderer instance for the best available terminal graphics mode.</returns>
    public static IRenderer CreateRenderer(TerminalCapabilities capabilities, RendererType rendererType = RendererType.Auto)
    {
        if (capabilities == null)
        {
            throw new ArgumentNullException(nameof(capabilities));
        }

        // If manual override is specified, create that renderer directly
        if (rendererType != RendererType.Auto)
        {
            return CreateSpecificRenderer(rendererType);
        }

        // Select renderer based on capabilities (priority order)
        return SelectRendererByCapabilities(capabilities);
    }

    /// <summary>
    /// Selects the best renderer based on terminal capabilities.
    /// </summary>
    /// <param name="capabilities">Terminal capabilities to evaluate.</param>
    /// <returns>An IRenderer instance for the best available terminal graphics mode.</returns>
    private static IRenderer SelectRendererByCapabilities(TerminalCapabilities capabilities)
    {
        if (capabilities.SupportsKittyGraphics)
        {
            return new KittyGraphicsRenderer();
        }

        if (capabilities.SupportsSixel)
        {
            return new SixelRenderer();
        }

        if (capabilities.SupportsBraille)
        {
            return new BrailleRenderer();
        }

        // Final fallback to ASCII renderer
        return new AsciiRenderer();
    }

    /// <summary>
    /// Creates a specific renderer type, ignoring capability detection.
    /// </summary>
    /// <param name="rendererType">The type of renderer to create.</param>
    /// <returns>An IRenderer instance of the specified type.</returns>
    private static IRenderer CreateSpecificRenderer(RendererType rendererType)
    {
        return rendererType switch
        {
            RendererType.Kitty => new KittyGraphicsRenderer(),
            RendererType.Sixel => new SixelRenderer(),
            RendererType.Braille => new BrailleRenderer(),
            RendererType.Ascii => new AsciiRenderer(),
            RendererType.Auto => throw new ArgumentException("Auto renderer type should be handled before calling this method.", nameof(rendererType)),
            _ => throw new ArgumentOutOfRangeException(nameof(rendererType), rendererType, "Unknown renderer type.")
        };
    }
}
