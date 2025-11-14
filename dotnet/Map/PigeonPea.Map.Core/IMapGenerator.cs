namespace PigeonPea.Map.Core;

public interface IMapGenerator
{
    MapData Generate(MapGenerationSettings settings);
}
