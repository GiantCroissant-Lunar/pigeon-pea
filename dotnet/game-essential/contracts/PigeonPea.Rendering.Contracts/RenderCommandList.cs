using SadRogue.Primitives;

namespace PigeonPea.Rendering.Contracts;

/// <summary>
/// Default implementation of IRenderCommandList that stores commands for backend execution.
/// Backends can use this or implement their own command list for optimization.
/// </summary>
public class RenderCommandList : IRenderCommandList
{
    private readonly List<RenderCommand> _commands = new();
    private readonly IRenderBackend _backend;

    public RenderingCapabilities Capabilities => _backend.Capabilities;

    public RenderCommandList(IRenderBackend backend)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
    }

    public void BeginFrame()
    {
        _commands.Clear();
        _commands.Add(new RenderCommand { Type = RenderCommandType.BeginFrame });
    }

    public void EndFrame()
    {
        _commands.Add(new RenderCommand { Type = RenderCommandType.EndFrame });
    }

    public void Clear(Color color)
    {
        _commands.Add(new RenderCommand
        {
            Type = RenderCommandType.Clear,
            ClearColor = color
        });
    }

    public void DrawTile(int x, int y, Tile tile)
    {
        _commands.Add(new RenderCommand
        {
            Type = RenderCommandType.DrawTile,
            X = x,
            Y = y,
            Tile = tile
        });
    }

    public void DrawTiles(ReadOnlySpan<TileCommand> commands)
    {
        var tiles = commands.ToArray();
        _commands.Add(new RenderCommand
        {
            Type = RenderCommandType.DrawTiles,
            TileCommands = tiles
        });
    }

    public void DrawBuffer(int x, int y, int width, int height, ReadOnlySpan<byte> rgba)
    {
        var buffer = rgba.ToArray();
        _commands.Add(new RenderCommand
        {
            Type = RenderCommandType.DrawBuffer,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            BufferData = buffer
        });
    }

    public void DrawSprite(int x, int y, string spriteId, Color? tint = null)
    {
        _commands.Add(new RenderCommand
        {
            Type = RenderCommandType.DrawSprite,
            X = x,
            Y = y,
            SpriteId = spriteId,
            SpriteTint = tint
        });
    }

    public void DrawText(int x, int y, string text, Color foreground, Color background)
    {
        _commands.Add(new RenderCommand
        {
            Type = RenderCommandType.DrawText,
            X = x,
            Y = y,
            Text = text,
            Foreground = foreground,
            Background = background
        });
    }

    public void SetViewport(Viewport viewport)
    {
        _commands.Add(new RenderCommand
        {
            Type = RenderCommandType.SetViewport,
            Viewport = viewport
        });
    }

    public void SetCamera(int centerX, int centerY, double zoom)
    {
        _commands.Add(new RenderCommand
        {
            Type = RenderCommandType.SetCamera,
            CameraX = centerX,
            CameraY = centerY,
            CameraZoom = zoom
        });
    }

    public void DrawPolygon(ReadOnlySpan<Point> points, Color fillColor, Color? strokeColor = null, int strokeWidth = 1)
    {
        var pts = points.ToArray();
        _commands.Add(new RenderCommand
        {
            Type = RenderCommandType.DrawPolygon,
            Points = pts,
            FillColor = fillColor,
            StrokeColor = strokeColor,
            StrokeWidth = strokeWidth
        });
    }

    public void DrawPolyline(ReadOnlySpan<Point> points, Color strokeColor, int strokeWidth = 1)
    {
        var pts = points.ToArray();
        _commands.Add(new RenderCommand
        {
            Type = RenderCommandType.DrawPolyline,
            Points = pts,
            StrokeColor = strokeColor,
            StrokeWidth = strokeWidth
        });
    }

    /// <summary>
    /// Get the list of commands for backend execution.
    /// Public API for backends.
    /// </summary>
    public IReadOnlyList<RenderCommand> GetCommands() => _commands;
}

/// <summary>
/// Command representation for backend execution
/// </summary>
public class RenderCommand
{
    public RenderCommandType Type { get; set; }

    // Position and dimensions
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    // Vector commands
    public Point[]? Points { get; set; }
    public Color FillColor { get; set; }
    public Color? StrokeColor { get; set; }
    public int StrokeWidth { get; set; }

    // Tile commands
    public Tile Tile { get; set; }
    public TileCommand[]? TileCommands { get; set; }

    // Buffer commands
    public byte[]? BufferData { get; set; }

    // Sprite commands
    public string? SpriteId { get; set; }
    public Color? SpriteTint { get; set; }

    // Text commands
    public string? Text { get; set; }
    public Color Foreground { get; set; }
    public Color Background { get; set; }

    // Clear command
    public Color ClearColor { get; set; }

    // Viewport/Camera
    public Viewport Viewport { get; set; }
    public int CameraX { get; set; }
    public int CameraY { get; set; }
    public double CameraZoom { get; set; }
}

/// <summary>
/// Type of render command
/// </summary>
public enum RenderCommandType
{
    BeginFrame,
    EndFrame,
    Clear,
    DrawTile,
    DrawTiles,
    DrawBuffer,
    DrawSprite,
    DrawText,
    DrawPolygon,
    DrawPolyline,
    SetViewport,
    SetCamera
}
