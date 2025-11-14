using System;

namespace PigeonPea.Dungeon.Rendering;

public static class FovRenderer
{
    public static void ApplyFov(
        byte[] rgba,
        int widthPx,
        int heightPx,
        bool[,] fov,
        int viewportX,
        int viewportY,
        int pixelsPerCell,
        double dimFactor = 0.3)
    {
        ArgumentNullException.ThrowIfNull(rgba);
        ArgumentNullException.ThrowIfNull(fov);
        if (pixelsPerCell <= 0) throw new ArgumentOutOfRangeException(nameof(pixelsPerCell));

        int tilesX = widthPx / pixelsPerCell;
        int tilesY = heightPx / pixelsPerCell;

        for (int ty = 0; ty < tilesY; ty++)
        {
            for (int tx = 0; tx < tilesX; tx++)
            {
                int wx = viewportX + tx;
                int wy = viewportY + ty;

                bool visible = wy >= 0 && wy < fov.GetLength(0) && wx >= 0 && wx < fov.GetLength(1) && fov[wy, wx];
                if (visible) continue;

                int startPxX = tx * pixelsPerCell;
                int startPxY = ty * pixelsPerCell;

                for (int oy = 0; oy < pixelsPerCell; oy++)
                {
                    int py = startPxY + oy;
                    if ((uint)py >= (uint)heightPx) break;

                    for (int ox = 0; ox < pixelsPerCell; ox++)
                    {
                        int px = startPxX + ox;
                        if ((uint)px >= (uint)widthPx) break;

                        int idx = (py * widthPx + px) * 4;
                        if (idx + 3 >= rgba.Length) continue;
                        rgba[idx] = (byte)(rgba[idx] * dimFactor);
                        rgba[idx + 1] = (byte)(rgba[idx + 1] * dimFactor);
                        rgba[idx + 2] = (byte)(rgba[idx + 2] * dimFactor);
                    }
                }
            }
        }
    }
}
