namespace PigeonPea.Game.Contracts.Rendering;

/// <summary>
/// Renderer contract for game rendering plugins.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Unique identifier for the renderer implementation.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Rendering capabilities supported by this renderer.
    /// </summary>
    RenderingCapabilities Capabilities { get; }

    /// <summary>
    /// Initialize the renderer with the given context.
    /// </summary>
    void Initialize(RenderContext context);

    /// <summary>
    /// Render a frame for the given game state.
    /// </summary>
    void Render(GameState state);

    /// <summary>
    /// Shutdown and cleanup resources.
    /// </summary>
    void Shutdown();
}
