using PigeonPea.Shared.Rendering;

namespace PigeonPea.Shared.Tests.Mocks;

/// <summary>
/// Mock render target for testing purposes.
/// Tracks method calls and provides configurable dimensions.
/// </summary>
public class MockRenderTarget : IRenderTarget
{
    /// <summary>
    /// Gets or sets the width of the render target in grid cells.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the render target in grid cells.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the pixel width of the render target.
    /// </summary>
    public int? PixelWidth { get; set; }

    /// <summary>
    /// Gets or sets the pixel height of the render target.
    /// </summary>
    public int? PixelHeight { get; set; }

    /// <summary>
    /// Gets whether Present was called.
    /// </summary>
    public bool PresentCalled { get; private set; }

    /// <summary>
    /// Gets the number of times Present was called.
    /// </summary>
    public int PresentCallCount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MockRenderTarget"/> class.
    /// </summary>
    /// <param name="width">The width in grid cells.</param>
    /// <param name="height">The height in grid cells.</param>
    /// <param name="pixelWidth">The optional pixel width.</param>
    /// <param name="pixelHeight">The optional pixel height.</param>
    public MockRenderTarget(int width = 80, int height = 50, int? pixelWidth = null, int? pixelHeight = null)
    {
        Width = width;
        Height = height;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
    }

    /// <summary>
    /// Presents the rendered content to the screen.
    /// </summary>
    public void Present()
    {
        PresentCalled = true;
        PresentCallCount++;
    }

    /// <summary>
    /// Resets the mock render target state for a new test.
    /// </summary>
    public void Reset()
    {
        PresentCalled = false;
        PresentCallCount = 0;
    }
}
