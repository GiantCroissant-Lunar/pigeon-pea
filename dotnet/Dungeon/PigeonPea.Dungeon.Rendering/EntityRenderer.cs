using Arch.Core;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

namespace PigeonPea.Dungeon.Rendering;

public static class EntityRenderer
{
    public static void RenderEntities(
        World world,
        byte[] rgba,
        int widthPx,
        int heightPx,
        int viewportX,
        int viewportY,
        int pixelsPerCell,
        bool[,]? fov = null)
    {
        var query = new QueryDescription().WithAll<Position, Sprite, DungeonEntityTag>();

        world.Query(in query, (ref Position pos, ref Sprite sprite) =>
        {
            if (fov != null)
            {
                if (pos.Y < 0 || pos.Y >= fov.GetLength(0) || pos.X < 0 || pos.X >= fov.GetLength(1)) return;
                if (!fov[pos.Y, pos.X]) return;
            }

            int tileX = pos.X - viewportX;
            int tileY = pos.Y - viewportY;
            int tilesX = widthPx / pixelsPerCell;
            int tilesY = heightPx / pixelsPerCell;
            if (tileX < 0 || tileX >= tilesX || tileY < 0 || tileY >= tilesY) return;

            int startPxX = tileX * pixelsPerCell;
            int startPxY = tileY * pixelsPerCell;

            for (int oy = 0; oy < pixelsPerCell; oy++)
            {
                int py = startPxY + oy;
                if ((uint)py >= (uint)heightPx) break;
                for (int ox = 0; ox < pixelsPerCell; ox++)
                {
                    int px = startPxX + ox;
                    if ((uint)px >= (uint)widthPx) break;
                    int idx = (py * widthPx + px) * 4;
                    if (idx + 3 >= rgba.Length) break;
                    rgba[idx] = sprite.R;
                    rgba[idx + 1] = sprite.G;
                    rgba[idx + 2] = sprite.B;
                    rgba[idx + 3] = 255;
                }
            }
        });
    }

    public static void RenderEntitiesAscii(
        World world,
        char[,] asciiBuffer,
        int viewportX,
        int viewportY,
        bool[,]? fov = null)
    {
        var query = new QueryDescription().WithAll<Position, Sprite, DungeonEntityTag>();

        int bufferHeight = asciiBuffer.GetLength(0);
        int bufferWidth = asciiBuffer.GetLength(1);

        world.Query(in query, (ref Position pos, ref Sprite sprite) =>
        {
            if (fov != null)
            {
                if (pos.Y < 0 || pos.Y >= fov.GetLength(0) || pos.X < 0 || pos.X >= fov.GetLength(1)) return;
                if (!fov[pos.Y, pos.X]) return;
            }

            int localX = pos.X - viewportX;
            int localY = pos.Y - viewportY;
            if (localX >= 0 && localX < bufferWidth && localY >= 0 && localY < bufferHeight)
            {
                asciiBuffer[localY, localX] = sprite.AsciiChar;
            }
        });
    }
}
