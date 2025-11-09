using Terminal.Gui;
using PigeonPea.Shared;
using PigeonPea.Shared.Components;
using PigeonPea.Console.Rendering;
using System;
using GuiAttribute = Terminal.Gui.Attribute;
using SRColor = SadRogue.Primitives.Color;
using Arch.Core;
using Arch.Core.Extensions;

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
            _ when terminalCaps.SupportsKittyGraphics => new KittyTerminalRenderer(),
            _ when terminalCaps.SupportsSixel => new SixelTerminalRendererStub(),
            _ when terminalCaps.SupportsBraille => new BrailleTerminalRenderer(),
            _ => new AsciiTerminalRenderer()
        };
    }

    protected override bool OnDrawingContent()
    {
        // Clear background
        Driver?.SetAttribute(new GuiAttribute(Color.White, Color.Black));

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
                Driver?.SetAttribute(new GuiAttribute(color, Color.Black));
                Driver?.Move(pos.Point.X, pos.Point.Y);
                Driver?.AddStr(renderable.Glyph.ToString());
            }
        });

        // TODO: Render map tiles
        // TODO: Use _renderer for advanced graphics (Sixel/Kitty/Braille)

        return true;
    }

    private Color ConvertColor(SRColor color)
    {
        // Map a few common colors directly; default to White
        if (color == SRColor.Yellow) return Color.Yellow;
        if (color == SRColor.Red) return Color.Red;
        if (color == SRColor.Green) return Color.Green;
        if (color == SRColor.Blue) return Color.Blue;
        if (color == SRColor.White) return Color.White;
        if (color == SRColor.Black) return Color.Black;
        return Color.White;
    }
}
