using FantasyMapGenerator.Core.Generators;

namespace PigeonPea.Map.Core.Adapters;

public class FantasyMapGeneratorAdapter : IMapGenerator
{
    private readonly MapGenerator _generator = new();

    public MapData Generate(MapGenerationSettings settings)
        => new MapData(_generator.Generate(FmgSettingsMapper.ToFmg(settings)));
}
