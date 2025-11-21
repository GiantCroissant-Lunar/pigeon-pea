using PigeonPea.Dungeon.Contracts.Models;
using PigeonPea.Overlays;
using PigeonPea.Rendering.Contracts;

namespace PigeonPea.Dungeon.Contracts;

public interface IDungeonRenderer
{
    void Initialize(IRenderer renderer);
    
    /// <summary>
    /// Renders dungeon using overlay system.
    /// </summary>
    void RenderWithOverlays(int width, int height, System.Collections.BitArray walkable, 
        IEnumerable<IOverlayFeature<GridPosition>> overlays, int playerX, int playerY, int scale = 1);
    
    /// <summary>
    /// Legacy rendering method for backward compatibility.
    /// </summary>
    [Obsolete("Use the overlay-based RenderWithOverlays method instead")]
    void Render(DungeonView dungeon, int playerX, int playerY);
}
