using FantasyMapGenerator.Core.Generators;
// NOTE: MapGenerationHooks was removed from FantasyMapGenerator library
// using FmgMapGenerationHooks = FantasyMapGenerator.Core.Models.MapGenerationHooks;

namespace PigeonPea.Map.Core.Adapters;

public class FantasyMapGeneratorAdapter : IMapGenerator
{
    private readonly MapGenerator _generator = new();

    public MapData Generate(MapGenerationSettings settings)
        => new MapData(_generator.Generate(FmgSettingsMapper.ToFmg(settings)));

    public MapData Generate(MapGenerationSettings settings, MapGenerationHooks? hooks)
    {
        // TODO: Re-enable hooks once MapGenerationHooks is added back to FantasyMapGenerator library
        // For now, just ignore hooks and generate without them
        return Generate(settings);

        /* DISABLED - needs MapGenerationHooks in FantasyMapGenerator library
        if (hooks is null)
        {
            return Generate(settings);
        }

        MapData? wrapped = null;
        var fmgHooks = new FmgMapGenerationHooks
        {
            AfterBaseMapGenerated = hooks.AfterBaseMapGenerated is null
                ? null
                : map =>
                {
                    wrapped ??= new MapData(map);
                    hooks.AfterBaseMapGenerated(wrapped);
                },
            AfterPoliticalMapGenerated = hooks.AfterPoliticalMapGenerated is null
                ? null
                : map =>
                {
                    wrapped ??= new MapData(map);
                    hooks.AfterPoliticalMapGenerated(wrapped);
                },
            AfterFinalization = hooks.AfterFinalization is null
                ? null
                : map =>
                {
                    wrapped ??= new MapData(map);
                    hooks.AfterFinalization(wrapped);
                }
        };

        var inner = _generator.Generate(FmgSettingsMapper.ToFmg(settings), fmgHooks);
        return wrapped ?? new MapData(inner);
        */
    }
}
