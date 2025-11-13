using Arch.Core;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

namespace PigeonPea.Map.Rendering;

/// <summary>
/// Renders ECS entities (cities, markers) onto map rasters.
/// </summary>
public static class MapEntityRenderer
{
    /// <summary>
    /// Render map entities (cities, markers) onto RGBA buffer.
    /// </summary>
    /// <param name="world">Arch ECS world containing map entities</param>
    /// <param name="rgba">RGBA buffer to draw on</param>
    /// <param name="widthPx">Width in pixels</param>
    /// <param name="heightPx">Height in pixels</param>
    /// <param name="viewportX">Viewport top-left X (world coordinates)</param>
    /// <param name="viewportY">Viewport top-left Y (world coordinates)</param>
    /// <param name="viewportWidth">Viewport width (world coordinates)</param>
    /// <param name="viewportHeight">Viewport height (world coordinates)</param>
    /// <param name="zoom">Zoom level (pixels per world unit)</param>
    public static void RenderEntities(
        World world,
        byte[] rgba,
        int widthPx,
        int heightPx,
        double viewportX,
        double viewportY,
        double viewportWidth,
        double viewportHeight,
        double zoom)
    {
        var query = new QueryDescription()
            .WithAll<Position, Sprite, MapEntityTag, Renderable>();

        world.Query(in query, (ref Position pos, ref Sprite sprite, ref Renderable renderable) =>
        {
            if (!renderable.Visible)
                return;

            // Convert world position to viewport local
            double localX = (pos.X - viewportX) / viewportWidth * widthPx;
            double localY = (pos.Y - viewportY) / viewportHeight * heightPx;

            // Check if in viewport
            if (localX < 0 || localX >= widthPx || localY < 0 || localY >= heightPx)
                return;

            // Draw entity as a small circle/marker (5x5 pixels centered on position)
            int centerPx = (int)localX;
            int centerPy = (int)localY;
            int radius = 3;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > radius * radius)
                        continue; // Circle shape

                    int px = centerPx + dx;
                    int py = centerPy + dy;

                    if (px < 0 || px >= widthPx || py < 0 || py >= heightPx)
                        continue;

                    int idx = (py * widthPx + px) * 4;
                    if (idx + 3 < rgba.Length)
                    {
                        rgba[idx] = sprite.R;
                        rgba[idx + 1] = sprite.G;
                        rgba[idx + 2] = sprite.B;
                        rgba[idx + 3] = 255;
                    }
                }
            }
        });
    }
}
