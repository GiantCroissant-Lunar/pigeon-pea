using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;

namespace PigeonPea.Shared.Tests.Mocks;

/// <summary>
/// Mock renderer for testing purposes.
/// Tracks all rendering calls without actually rendering anything.
/// </summary>
public class MockRenderer : IRenderer
{
    private IRenderTarget? _target;
    private Viewport _viewport;

    /// <summary>
    /// Gets the list of tiles drawn during rendering.
    /// </summary>
    public List<(int X, int Y, Tile Tile)> DrawnTiles { get; } = new();

    /// <summary>
    /// Gets the list of text drawn during rendering.
    /// </summary>
    public List<(int X, int Y, string Text, Color Foreground, Color Background)> DrawnText { get; } = new();

    /// <summary>
    /// Gets or sets whether BeginFrame was called.
    /// </summary>
    public bool BeginFrameCalled { get; private set; }

    /// <summary>
    /// Gets or sets whether EndFrame was called.
    /// </summary>
    public bool EndFrameCalled { get; private set; }

    /// <summary>
    /// Gets or sets the last clear color used.
    /// </summary>
    public Color? LastClearColor { get; private set; }

    /// <summary>
    /// Gets the current viewport.
    /// </summary>
    public Viewport CurrentViewport => _viewport;

    /// <summary>
    /// Gets the renderer capabilities.
    /// </summary>
    public RendererCapabilities Capabilities { get; } =
        RendererCapabilities.TrueColor |
        RendererCapabilities.CharacterBased;

    /// <summary>
    /// Initializes the renderer with a render target.
    /// </summary>
    public void Initialize(IRenderTarget target)
    {
        _target = target;
    }

    /// <summary>
    /// Begins a new rendering frame.
    /// </summary>
    public void BeginFrame()
    {
        BeginFrameCalled = true;
        EndFrameCalled = false;
        DrawnTiles.Clear();
        DrawnText.Clear();
        LastClearColor = null;
    }

    /// <summary>
    /// Ends the current rendering frame.
    /// </summary>
    public void EndFrame()
    {
        EndFrameCalled = true;
    }

    /// <summary>
    /// Draws a tile at the specified grid position.
    /// </summary>
    public void DrawTile(int x, int y, Tile tile)
    {
        DrawnTiles.Add((x, y, tile));
    }

    /// <summary>
    /// Draws text at the specified grid position.
    /// </summary>
    public void DrawText(int x, int y, string text, Color foreground, Color background)
    {
        DrawnText.Add((x, y, text, foreground, background));
    }

    /// <summary>
    /// Clears the render target with the specified color.
    /// </summary>
    public void Clear(Color color)
    {
        LastClearColor = color;
    }

    /// <summary>
    /// Sets the viewport for rendering.
    /// </summary>
    public void SetViewport(Viewport viewport)
    {
        _viewport = viewport;
    }

    /// <summary>
    /// Resets the mock renderer state for a new test.
    /// </summary>
    public void Reset()
    {
        BeginFrameCalled = false;
        EndFrameCalled = false;
        DrawnTiles.Clear();
        DrawnText.Clear();
        LastClearColor = null;
        _viewport = default;
    }
}
