namespace PigeonPea.Rendering.Contracts;

/// <summary>
/// Platform-specific rendering backend.
/// Executes rendering commands for specific output (console, window, etc.)
/// </summary>
public interface IRenderBackend : IDisposable
{
    /// <summary>
    /// Unique identifier for this backend
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Capabilities of this rendering backend
    /// </summary>
    RenderingCapabilities Capabilities { get; }

    // Lifecycle

    /// <summary>
    /// Initialize the backend with the specified render context
    /// </summary>
    void Initialize(RenderContext context);

    /// <summary>
    /// Shutdown the backend and release resources
    /// </summary>
    void Shutdown();

    // Execute command list

    /// <summary>
    /// Execute the commands from the render command list
    /// </summary>
    void Execute(IRenderCommandList commands);

    // Platform-specific present

    /// <summary>
    /// Present the rendered frame to the output device
    /// </summary>
    void Present();
}
