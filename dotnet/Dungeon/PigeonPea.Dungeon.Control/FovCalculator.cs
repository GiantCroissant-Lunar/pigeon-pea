using PigeonPea.Dungeon.Core;

namespace PigeonPea.Dungeon.Control;

public enum RadiusType { Manhattan, Chebyshev, Euclidean }

public sealed class FovCalculator
{
    private readonly DungeonData _dungeon;

    public FovCalculator(DungeonData dungeon)
    {
        _dungeon = dungeon;
    }

    // Bresenham LOS per target tile, Manhattan range cutoff
    public bool[,] ComputeVisible(int originX, int originY, int range, RadiusType radius = RadiusType.Manhattan, Func<(int x,int y), double>? tileOpacity = null, double blockThreshold = 0.99)
    {
        var vis = new bool[_dungeon.Height, _dungeon.Width];
        if (!_dungeon.InBounds(originX, originY)) return vis;
        vis[originY, originX] = true;

        for (int y = originY - range; y <= originY + range; y++)
        {
            for (int x = originX - range; x <= originX + range; x++)
            {
                if (!_dungeon.InBounds(x, y)) continue;
                if (!WithinRadius(originX, originY, x, y, range, radius)) continue;
                if (HasLineOfSightWeighted(originX, originY, x, y, tileOpacity, blockThreshold)) vis[y, x] = true;
            }
        }
        return vis;
    }

    // Back-compat overload
    public bool[,] ComputeVisible(int originX, int originY, int range)
        => ComputeVisible(originX, originY, range, RadiusType.Manhattan, null, 0.99);

    private static bool WithinRadius(int ox, int oy, int x, int y, int range, RadiusType type)
    {
        int dx = System.Math.Abs(x - ox), dy = System.Math.Abs(y - oy);
        return type switch
        {
            RadiusType.Manhattan => dx + dy <= range,
            RadiusType.Chebyshev => System.Math.Max(dx, dy) <= range,
            RadiusType.Euclidean => (dx*dx + dy*dy) <= range * range,
            _ => dx + dy <= range,
        };
    }

    private bool HasLineOfSightWeighted(int x0, int y0, int x1, int y1, Func<(int x,int y), double>? tileOpacity, double blockThreshold)
    {
        int dx = System.Math.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -System.Math.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        int x = x0, y = y0;
        double accum = 0.0;
        while (true)
        {
            if (!(x == x0 && y == y0))
            {
                double op = tileOpacity?.Invoke((x,y)) ?? (_dungeon.IsOpaque(x, y) ? 1.0 : 0.0);
                accum += op;
                if (accum >= blockThreshold) return false;
            }
            if (x == x1 && y == y1) return true;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x += sx; }
            if (e2 <= dx) { err += dx; y += sy; }
        }
    }
}

