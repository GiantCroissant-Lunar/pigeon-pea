using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;

namespace PigeonPea.Rendering.Contracts.Tests;

public class BrailleBackendTests
{
    [Fact]
    public void BrailleBackend_ShouldHaveCorrectCapabilities()
    {
        // Arrange
        var backend = new MockBrailleBackend();
        backend.Initialize(new RenderContext(80, 40));

        // Assert
        Assert.True(backend.Capabilities.SupportsBuffers);
        Assert.True(backend.Capabilities.SupportsTiles); // Emulated
        Assert.False(backend.Capabilities.SupportsSprites);
        Assert.Equal(RenderMode.Buffer, backend.Capabilities.Mode);
    }

    [Fact]
    public void BrailleBackend_ShouldCalculateCorrectPixelDimensions()
    {
        // Arrange
        var backend = new MockBrailleBackend();
        
        // Act
        backend.Initialize(new RenderContext(80, 40));

        // Assert
        // Each Braille character is 2×4 pixels
        Assert.Equal(160, backend.Capabilities.MaxWidth);  // 80 * 2
        Assert.Equal(160, backend.Capabilities.MaxHeight); // 40 * 4
    }

    [Fact]
    public void BrailleBackend_ShouldAcceptBufferCommands()
    {
        // Arrange
        var backend = new MockBrailleBackend();
        backend.Initialize(new RenderContext(80, 40));
        var commandList = new RenderCommandList(backend);

        // Create a simple 4×4 white buffer
        var buffer = new byte[4 * 4 * 4];
        for (int i = 0; i < buffer.Length; i += 4)
        {
            buffer[i] = 255;     // R
            buffer[i + 1] = 255; // G
            buffer[i + 2] = 255; // B
            buffer[i + 3] = 255; // A
        }

        // Act
        commandList.BeginFrame();
        commandList.DrawBuffer(0, 0, 4, 4, buffer);
        commandList.EndFrame();

        // Assert
        var commands = commandList.GetCommands();
        Assert.Contains(commands, c => c.Type == RenderCommandType.DrawBuffer);
    }

    [Fact]
    public void BrailleBackend_ShouldAcceptTileCommands()
    {
        // Arrange
        var backend = new MockBrailleBackend();
        backend.Initialize(new RenderContext(80, 40));
        var commandList = new RenderCommandList(backend);

        // Act
        commandList.BeginFrame();
        commandList.DrawTile(5, 10, new Tile('#', Color.White, Color.Black));
        commandList.EndFrame();

        // Assert
        var commands = commandList.GetCommands();
        Assert.Contains(commands, c => c.Type == RenderCommandType.DrawTile);
    }
}

/// <summary>
/// Mock Braille backend for testing
/// Simulates Braille backend without actual console output
/// </summary>
internal class MockBrailleBackend : IRenderBackend
{
    private int _cellWidth;
    private int _cellHeight;
    private int _pixelWidth;
    private int _pixelHeight;
    private const int DotsX = 2;
    private const int DotsY = 4;

    public string Id => "mock-braille-backend";

    public RenderingCapabilities Capabilities { get; private set; } = null!;

    public void Initialize(RenderContext context)
    {
        _cellWidth = context.Width;
        _cellHeight = context.Height;
        _pixelWidth = _cellWidth * DotsX;
        _pixelHeight = _cellHeight * DotsY;

        Capabilities = new RenderingCapabilities(
            supportsTiles: true,
            supportsBuffers: true,
            supportsSprites: false,
            supportsAntialiasing: false,
            maxWidth: _pixelWidth,
            maxHeight: _pixelHeight,
            mode: RenderMode.Buffer
        );
    }

    public void Shutdown() { }
    public void Execute(IRenderCommandList commands) { }
    public void Present() { }
    public void Dispose() { }
}
