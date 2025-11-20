using PigeonPea.Dungeon.Contracts.Models;
using PigeonPea.Rendering.Contracts;

namespace PigeonPea.Dungeon.Contracts;

public interface IDungeonRenderer
{
    void Initialize(IRenderer renderer);
    void Render(DungeonView dungeon, int playerX, int playerY);
}
