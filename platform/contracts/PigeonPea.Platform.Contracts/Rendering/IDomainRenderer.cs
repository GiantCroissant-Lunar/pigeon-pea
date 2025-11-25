namespace PigeonPea.Platform.Contracts.Rendering;

/// <summary>
/// Domain-specific renderer (world map, dungeon, UI, etc.)
/// Knows WHAT to render, submits commands to IRenderCommandList.
/// Domain renderers are backend-agnostic.
/// </summary>
public interface IDomainRenderer
{
    /// <summary>
    /// Unique identifier for this domain renderer
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Render the domain using the provided command list.
    /// The domain renderer queries the ECS world and submits rendering commands.
    /// </summary>
    /// <param name="world">ECS world containing entities to render</param>
    /// <param name="commands">Command list to submit rendering commands to</param>
    /// <param name="options">Rendering options (viewport, zoom, etc.)</param>
    void Render(object world, IRenderCommandList commands, RenderOptions options);
}
