using Arch.Core;
using PigeonPea.Dungeon.Control;
using PigeonPea.Dungeon.Control.WorldManager;
using PigeonPea.Dungeon.Core;
using PigeonPea.Dungeon.Rendering;
using PigeonPea.Shared.ECS.Components;
using Terminal.Gui;

namespace PigeonPea.Console.Views;

public class DungeonPanelView : View
{
    private readonly DungeonData _dungeon;
    private readonly DungeonWorldManager _worldManager;
    private readonly FovCalculator _fov;
    private int _fovRange = 8;

    public DungeonPanelView(DungeonData dungeon, DungeonWorldManager worldManager)
    {
        _dungeon = dungeon;
        _worldManager = worldManager;
        _fov = new FovCalculator(dungeon);

        CanFocus = true;
        KeyDown += OnKeyDown;
    }

    protected override bool OnDrawingContent()
    {
        var playerPos = _worldManager.GetPlayerPosition();
        if (!playerPos.HasValue)
            return false;

        var fovMask = _fov.ComputeVisible(playerPos.Value.X, playerPos.Value.Y, _fovRange);

        int viewWidth = Viewport.Width;
        int viewHeight = Viewport.Height;
        int viewX = playerPos.Value.X - viewWidth / 2;
        int viewY = playerPos.Value.Y - viewHeight / 2;

        // Render dungeon base
        string asciiDungeon = BrailleDungeonRenderer.RenderAscii(
            _dungeon,
            viewX,
            viewY,
            viewWidth,
            viewHeight,
            fovMask);

        // Render entities on top (convert to char array for modification)
        var lines = asciiDungeon.Split('\n');
        var charBuffer = new char[viewHeight, viewWidth];

        for (int y = 0; y < lines.Length && y < viewHeight; y++)
        {
            string line = lines[y];
            for (int x = 0; x < line.Length && x < viewWidth; x++)
            {
                charBuffer[y, x] = line[x];
            }
        }

        // Render ECS entities
        EntityRenderer.RenderEntitiesAscii(_worldManager.World, charBuffer, viewX, viewY, fovMask);

        // Convert back to string
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < viewHeight; y++)
        {
            for (int x = 0; x < viewWidth; x++)
            {
                sb.Append(charBuffer[y, x]);
            }
            if (y < viewHeight - 1)
                sb.AppendLine();
        }

        Driver.Move(0, 0);
        Driver.SetAttribute(new Terminal.Gui.Attribute(Color.White, Color.Black));
        Driver.AddStr(sb.ToString());

        return true;
    }

    private void OnKeyDown(object? sender, Key e)
    {
        var playerPos = _worldManager.GetPlayerPosition();
        if (!playerPos.HasValue || !_worldManager.PlayerEntity.HasValue)
            return;

        int dx = 0, dy = 0;
        switch (e.KeyCode)
        {
            case KeyCode.CursorUp:
            case KeyCode.W: dy = -1; break;
            case KeyCode.CursorDown:
            case KeyCode.S: dy = 1; break;
            case KeyCode.CursorLeft:
            case KeyCode.A: dx = -1; break;
            case KeyCode.CursorRight:
            case KeyCode.D: dx = 1; break;
            default: return;
        }

        int newX = playerPos.Value.X + dx;
        int newY = playerPos.Value.Y + dy;

        if (_worldManager.TryMoveEntity(_worldManager.PlayerEntity.Value, newX, newY))
        {
            SetNeedsDraw();
        }
    }
}
