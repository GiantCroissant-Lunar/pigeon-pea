using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Providers;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core;
using PigeonPea.Plugin.Map.FMG.Adapters;

namespace PigeonPea.Plugin.Map.FMG;

public class FmgMapProvider : IMapProvider
{
    private readonly IMapGenerator _generator;
    private readonly Dictionary<string, IMapData> _cache = new();
    private readonly Queue<string> _cacheOrder = new();
    private const int MaxCacheSize = 100;

    public string ProviderId => "fmg";

    public MapProviderCapabilities Capabilities =>
        MapProviderCapabilities.Fantasy |
        MapProviderCapabilities.Generative |
        MapProviderCapabilities.Offline |
        MapProviderCapabilities.Cacheable;

    public FmgMapProvider(IMapGenerator generator)
    {
        _generator = generator;
    }

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default)
    {
        var cacheKey = $"{bounds.X},{bounds.Y},{bounds.Width},{bounds.Height}";

        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        try
        {
            var settings = CreateSettingsFromBounds(bounds);
            var fmgData = await Task.Run(() => _generator.Generate(settings), ct);

            var mapData = new FmgMapDataAdapter(fmgData, bounds);

            if (_cache.Count >= MaxCacheSize)
            {
                var oldestKey = _cacheOrder.Dequeue();
                _cache.Remove(oldestKey);
            }

            _cache[cacheKey] = mapData;
            _cacheOrder.Enqueue(cacheKey);

            return mapData;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate FMG map for bounds {bounds}", ex);
        }
    }

    public bool CanServe(BoundingBox bounds) => true;

    private MapGenerationSettings CreateSettingsFromBounds(BoundingBox bounds)
    {
        // Use reasonable defaults for map size and generation parameters,
        // aligned with the console MapHud demo configuration.
        var width = Math.Max(1024, (int)bounds.Width);
        var height = Math.Max(1024, (int)bounds.Height);

        return new MapGenerationSettings
        {
            Seed = HashBounds(bounds),
            Width = width,
            Height = height,
            NumPoints = 8000,
            RNGMode = RNGMode.Alea,
            SeedString = "fmg-demo",
            ReseedAtPhaseStart = true,
            GridMode = GridMode.Jittered,
            HeightmapMode = HeightmapMode.Template,
            UseAdvancedNoise = false,
            HeightmapTemplate = "continents"
        };
    }

    private int HashBounds(BoundingBox bounds)
    {
        var hash = 17;
        hash = hash * 31 + bounds.X.GetHashCode();
        hash = hash * 31 + bounds.Y.GetHashCode();
        hash = hash * 31 + bounds.Width.GetHashCode();
        hash = hash * 31 + bounds.Height.GetHashCode();
        return Math.Abs(hash);
    }
}
