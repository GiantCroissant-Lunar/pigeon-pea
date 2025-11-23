using Microsoft.Extensions.Caching.Memory;
using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Providers;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Composition;

public class CachedMapProvider : IMapProvider
{
    private readonly IMapProvider _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _expiration;

    public string ProviderId => $"cached:{_inner.ProviderId}";

    public MapProviderCapabilities Capabilities => _inner.Capabilities;

    public CachedMapProvider(
        IMapProvider inner,
        IMemoryCache cache,
        TimeSpan? expiration = null)
    {
        _inner = inner;
        _cache = cache;
        _expiration = expiration ?? TimeSpan.FromMinutes(5);
    }

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default)
    {
        var key = $"{_inner.ProviderId}:{bounds}";

        return await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _expiration;
            return await _inner.GetMapAsync(bounds, ct);
        }) ?? throw new InvalidOperationException("Cache returned null");
    }

    public bool CanServe(BoundingBox bounds) => _inner.CanServe(bounds);
}
