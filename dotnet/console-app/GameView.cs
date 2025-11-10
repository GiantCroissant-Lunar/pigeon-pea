using Terminal.Gui;
using PigeonPea.Shared;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Rendering;
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
    private readonly IRenderer _renderer;
    private readonly IRenderTarget _renderTarget;

    public GameView(GameWorld gameWorld, IRenderer renderer)
    {
        _gameWorld = gameWorld;
        _renderer = renderer;
        _renderTarget = new TerminalGuiRenderTarget(this);
        
        // Initialize the renderer with the render target
        _renderer.Initialize(_renderTarget);
    }

    protected override bool OnDrawingContent()
    {
        // Use the renderer to draw the game world
        _renderer.BeginFrame();
        
        // Clear with black background
        _renderer.Clear(SRColor.Black);

        // Get viewport bounds
        var viewport = Viewport;
        
        // Set the viewport for the renderer
        _renderer.SetViewport(new PigeonPea.Shared.Rendering.Viewport(0, 0, viewport.Width, viewport.Height));

        // Render all entities using the IRenderer
        var query = new Arch.Core.QueryDescription().WithAll<Position, Renderable>();
        _gameWorld.EcsWorld.Query(in query, (ref Position pos, ref Renderable renderable) =>
        {
            if (pos.Point.X >= 0 && pos.Point.X < viewport.Width &&
                pos.Point.Y >= 0 && pos.Point.Y < viewport.Height)
            {
                // Create a tile from the renderable component
                var tile = new PigeonPea.Shared.Rendering.Tile(renderable.Glyph, renderable.Foreground, SRColor.Black);
                _renderer.DrawTile(pos.Point.X, pos.Point.Y, tile);
            }
        });

        _renderer.EndFrame();

        return true;
    }
}
