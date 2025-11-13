using System.Text;
using PigeonPea.Dungeon.Core;

namespace PigeonPea.Dungeon.Rendering;

public static class BrailleDungeonRenderer
{
    public static string RenderAscii(
        DungeonData dungeon,
        int viewportX,
        int viewportY,
        int viewportWidth,
        int viewportHeight,
        bool[,]? fov = null,
        int? playerX = null,
        int? playerY = null)
    {
        var sb = new StringBuilder(viewportWidth * viewportHeight + viewportHeight);

        for (int cy = 0; cy < viewportHeight; cy++)
        {
            for (int cx = 0; cx < viewportWidth; cx++)
            {
                int wx = viewportX + cx;
                int wy = viewportY + cy;

                if (playerX.HasValue && playerY.HasValue && wx == playerX.Value && wy == playerY.Value)
                {
                    sb.Append('@');
                    continue;
                }

                bool visible = true;
                if (fov != null)
                {
                    visible = wy >= 0 && wy < fov.GetLength(0) && wx >= 0 && wx < fov.GetLength(1) && fov[wy, wx];
                }

                char ch;
                if (!dungeon.InBounds(wx, wy)) ch = ' ';
                else if (!visible) ch = ' ';
                else if (dungeon.IsDoorClosed(wx, wy)) ch = '+';
                else if (dungeon.IsDoorOpen(wx, wy)) ch = '/';
                else if (dungeon.IsWalkable(wx, wy)) ch = '.';
                else ch = '#';

                sb.Append(ch);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
