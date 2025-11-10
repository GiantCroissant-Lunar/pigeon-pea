using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;
using Terminal.Gui;
using GuiAttribute = Terminal.Gui.Attribute;
using GuiColor = Terminal.Gui.Color;

namespace PigeonPea.Console;

/// <summary>
/// Renderer implementation that uses Terminal.Gui's Driver for rendering.
/// This renderer is compatible with Terminal.Gui's rendering pipeline and works
/// alongside other Terminal.Gui views and components.
/// It wraps the capabilities of the underlying terminal renderer selected by the factory.
/// </summary>
public class TerminalGuiRenderer : IRenderer
{
    private IRenderTarget? _target;
    private Viewport _viewport;
    private IConsoleDriver? _driver;
    private readonly RendererCapabilities _capabilities;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalGuiRenderer"/> class.
    /// </summary>
    /// <param name="underlyingRenderer">The underlying renderer from the factory, used for capabilities detection.</param>
    public TerminalGuiRenderer(IRenderer underlyingRenderer)
    {
        _capabilities = underlyingRenderer.Capabilities;
    }

    /// <summary>
    /// Gets the capabilities of this renderer.
    /// </summary>
    public RendererCapabilities Capabilities => _capabilities;

    /// <summary>
    /// Sets the Terminal.Gui driver to use for rendering.
    /// This must be called before rendering.
    /// </summary>
    /// <param name="driver">The Terminal.Gui console driver.</param>
    public void SetDriver(IConsoleDriver? driver)
    {
        _driver = driver;
    }

    /// <summary>
    /// Initializes the renderer with a render target.
    /// </summary>
    /// <param name="target">The render target to draw to.</param>
    public void Initialize(IRenderTarget target)
    {
        _target = target;
        _viewport = new Viewport(0, 0, target.Width, target.Height);
    }

    /// <summary>
    /// Begins a new rendering frame.
    /// </summary>
    public void BeginFrame()
    {
        if (_target is null)
            throw new InvalidOperationException($"{nameof(TerminalGuiRenderer)} has not been initialized. Call {nameof(Initialize)} before use.");
    }

    /// <summary>
    /// Ends the current rendering frame.
    /// For Terminal.Gui, this is a no-op as the driver manages frame presentation.
    /// </summary>
    public void EndFrame()
    {
        // No-op: Terminal.Gui's driver handles frame presentation
        _target?.Present();
    }

    /// <summary>
    /// Draws a tile at the specified grid position using Terminal.Gui's Driver.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <param name="tile">The tile to draw.</param>
    public void DrawTile(int x, int y, PigeonPea.Shared.Rendering.Tile tile)
    {
        if (!_viewport.Contains(x, y) || _driver is null)
            return;

        var fg = ConvertColor(tile.Foreground);
        var bg = ConvertColor(tile.Background);
        
        _driver.SetAttribute(new GuiAttribute(fg, bg));
        _driver.Move(x - _viewport.X, y - _viewport.Y);
        _driver.AddStr(tile.Glyph.ToString());
    }

    /// <summary>
    /// Draws text at the specified grid position using Terminal.Gui's Driver.
    /// </summary>
    /// <param name="x">The X grid coordinate.</param>
    /// <param name="y">The Y grid coordinate.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    public void DrawText(int x, int y, string text, SadRogue.Primitives.Color foreground, SadRogue.Primitives.Color background)
    {
        if (!_viewport.Contains(x, y) || _driver is null)
            return;

        var fg = ConvertColor(foreground);
        var bg = ConvertColor(background);
        
        _driver.SetAttribute(new GuiAttribute(fg, bg));
        _driver.Move(x - _viewport.X, y - _viewport.Y);
        _driver.AddStr(text);
    }

    /// <summary>
    /// Clears the render target with the specified color.
    /// </summary>
    /// <param name="color">The color to clear with.</param>
    public void Clear(SadRogue.Primitives.Color color)
    {
        if (_driver is null)
            return;

        var bg = ConvertColor(color);
        _driver.SetAttribute(new GuiAttribute(GuiColor.White, bg));
    }

    /// <summary>
    /// Sets the viewport for rendering.
    /// </summary>
    /// <param name="viewport">The viewport to use for rendering.</param>
    public void SetViewport(Viewport viewport)
    {
        _viewport = viewport;
    }

    /// <summary>
    /// Converts a SadRogue.Primitives.Color to a Terminal.Gui.Color.
    /// Maps common colors and defaults to White for unmapped colors.
    /// </summary>
    /// <param name="color">The color to convert.</param>
    /// <returns>The Terminal.Gui color.</returns>
    private static GuiColor ConvertColor(SadRogue.Primitives.Color color)
    {
        // Map common colors
        if (color == SadRogue.Primitives.Color.Yellow) return GuiColor.Yellow;
        if (color == SadRogue.Primitives.Color.Red) return GuiColor.Red;
        if (color == SadRogue.Primitives.Color.Green) return GuiColor.Green;
        if (color == SadRogue.Primitives.Color.Blue) return GuiColor.Blue;
        if (color == SadRogue.Primitives.Color.White) return GuiColor.White;
        if (color == SadRogue.Primitives.Color.Black) return GuiColor.Black;
        if (color == SadRogue.Primitives.Color.Cyan) return GuiColor.Cyan;
        if (color == SadRogue.Primitives.Color.Magenta) return GuiColor.Magenta;
        if (color == SadRogue.Primitives.Color.Gray) return GuiColor.Gray;
        if (color == SadRogue.Primitives.Color.DarkGray) return GuiColor.DarkGray;
        
        // Default to white for unmapped colors
        return GuiColor.White;
    }
}
