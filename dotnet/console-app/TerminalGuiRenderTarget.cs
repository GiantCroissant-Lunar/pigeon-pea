using PigeonPea.Shared.Rendering;
using Terminal.Gui;

namespace PigeonPea.Console;

/// <summary>
/// Render target implementation for Terminal.Gui.
/// Wraps a Terminal.Gui View to provide IRenderTarget interface.
/// </summary>
public class TerminalGuiRenderTarget : IRenderTarget
{
    private readonly View _view;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalGuiRenderTarget"/> class.
    /// </summary>
    /// <param name="view">The Terminal.Gui view to render to.</param>
    public TerminalGuiRenderTarget(View view)
    {
        _view = view;
    }

    /// <summary>
    /// Gets the width of the render target in grid cells.
    /// </summary>
    public int Width => _view.Viewport.Width;

    /// <summary>
    /// Gets the height of the render target in grid cells.
    /// </summary>
    public int Height => _view.Viewport.Height;

    /// <summary>
    /// Gets the pixel width of the render target (not applicable for Terminal.Gui).
    /// </summary>
    public int? PixelWidth => null;

    /// <summary>
    /// Gets the pixel height of the render target (not applicable for Terminal.Gui).
    /// </summary>
    public int? PixelHeight => null;

    /// <summary>
    /// Presents the rendered content to the screen.
    /// For Terminal.Gui, this triggers a redraw of the view.
    /// </summary>
    public void Present()
    {
        _view.SetNeedsDraw();
    }
}
