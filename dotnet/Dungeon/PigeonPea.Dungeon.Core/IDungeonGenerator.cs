namespace PigeonPea.Dungeon.Core;

public interface IDungeonGenerator
{
    DungeonData Generate(int width, int height, int? seed = null);
}
