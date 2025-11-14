using System;

namespace PigeonPea.Game.Contracts.Rendering;

/// <summary>
/// Context provided to renderers during initialization.
/// </summary>
public class RenderContext
{
    /// <summary>
    /// Target width in pixels or cells.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Target height in pixels or cells.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Service provider for resolving dependencies.
    /// </summary>
    public IServiceProvider Services { get; set; } = default!;
}
