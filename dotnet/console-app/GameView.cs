using Terminal.Gui;
using PigeonPea.Shared;
using PigeonPea.Shared.Components;
using PigeonPea.Console.Rendering;
using System;

namespace PigeonPea.Console;

/// <summary>
/// Custom view rendering the game world using terminal graphics.
/// </summary>
public class GameView : View
{
    private readonly GameWorld _gameWorld;
    private readonly TerminalCapabilities _terminalCaps;
    private readonly ITerminalRenderer _renderer;

    public GameView(GameWorld gameWorld, TerminalCapabilities terminalCaps)
    {
        _gameWorld = gameWorld;
        _terminalCaps = terminalCaps;

        // Select best available renderer
        _renderer = terminalCaps switch
        {
            _ when terminalCaps.SupportsKittyGraphics => new KittyGraphicsRenderer(),
            _ when terminalCaps.SupportsSixel => new SixelRenderer(),
            _ when terminalCaps.SupportsBraille => new BrailleRenderer(),
            _ => new AsciiRenderer()
        };
    }

    protected override bool OnDrawingContent()
    {
        // Clear background
        Driver?.SetAttribute(new Attribute(Color.White, Color.Black));

        // Get viewport bounds
        var viewport = Viewport;

        // Render all entities
        var query = new Arch.Core.QueryDescription().WithAll<Position, Renderable>();
        _gameWorld.EcsWorld.Query(in query, (ref Position pos, ref Renderable renderable) =>
        {
            if (pos.Point.X >= 0 && pos.Point.X < viewport.Width &&
                pos.Point.Y >= 0 && pos.Point.Y < viewport.Height)
            {
                var color = ConvertColor(renderable.Foreground);
                Driver?.SetAttribute(new Attribute(color, Color.Black));
                AddRune(pos.Point.X, pos.Point.Y, (Rune)renderable.Glyph);
            }
        });

        // TODO: Render map tiles
        // TODO: Use _renderer for advanced graphics (Sixel/Kitty/Braille)

        return true;
    }

    private Color ConvertColor(SadRogue.Primitives.Color color)
    {
        // Map to nearest Terminal.Gui color
        return color.Name switch
        {
            "Yellow" => Color.Yellow,
            "Red" => Color.Red,
            "Green" => Color.Green,
            "Blue" => Color.Blue,
            "White" => Color.White,
            "Black" => Color.Black,
            _ => Color.White
        };
    }
}
