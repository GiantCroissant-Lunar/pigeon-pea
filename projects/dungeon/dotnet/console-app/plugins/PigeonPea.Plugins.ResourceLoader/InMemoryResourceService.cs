using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Resource;
using PigeonPea.Contracts.Resource.Services;

namespace PigeonPea.Plugins.ResourceLoader;

/// <summary>
/// Minimal in-memory resource service implementing the shared Resource IService contract.
/// Intended as a non-map Tier-3 service for testing the plugin pipeline.
/// </summary>
public class InMemoryResourceService : IService
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, object> _cache = new();
    private readonly Dictionary<string, ResourceMetadata> _metadata = new();

    public InMemoryResourceService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<T> LoadAsync<T>(string resourceId, IProgress<LoadProgress>? progress = null, CancellationToken ct = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            throw new ArgumentException("Resource ID cannot be null or empty", nameof(resourceId));

        if (_cache.TryGetValue(resourceId, out var cached))
        {
            _logger.LogDebug("Resource {ResourceId} loaded from cache", resourceId);
            return Task.FromResult((T)cached);
        }

        // Minimal implementation: create a simple dummy resource, no real I/O.
        var resource = CreateDummyResource<T>(resourceId);

        _cache[resourceId] = resource;
        _metadata[resourceId] = new ResourceMetadata(resourceId, typeof(T).Name, sizeBytes: 0, path: $"/{resourceId}");

        progress?.Report(new LoadProgress(resourceId, bytesLoaded: 0, totalBytes: null, percentComplete: 100f));
        _logger.LogInformation("Resource loaded (in-memory dummy): {ResourceId}", resourceId);

        return Task.FromResult(resource);
    }

    public async Task PreloadAsync(IEnumerable<string> resourceIds, IProgress<LoadProgress>? progress = null, CancellationToken ct = default)
    {
        if (resourceIds == null) throw new ArgumentNullException(nameof(resourceIds));

        var ids = new List<string>(resourceIds);
        for (var i = 0; i < ids.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            await LoadAsync<string>(ids[i], progress, ct).ConfigureAwait(false);

            var overall = (i + 1) * 100f / ids.Count;
            progress?.Report(new LoadProgress($"Batch-{i}", i + 1, ids.Count, overall));
        }

        _logger.LogInformation("Preloaded {Count} resources (in-memory dummy)", ids.Count);
    }

    public void Unload(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            return;

        var removed = _cache.Remove(resourceId);
        _metadata.Remove(resourceId);

        if (removed)
        {
            _logger.LogInformation("Unloaded resource: {ResourceId}", resourceId);
        }
    }

    public bool IsLoaded(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            return false;

        return _cache.ContainsKey(resourceId);
    }

    public long GetCacheSize()
    {
        long total = 0;
        foreach (var meta in _metadata.Values)
        {
            total += meta.SizeBytes;
        }
        return total;
    }

    public void ClearCache()
    {
        var count = _cache.Count;
        _cache.Clear();
        _metadata.Clear();
        _logger.LogInformation("Cleared in-memory resource cache: {Count} entries", count);
    }

    private static T CreateDummyResource<T>(string resourceId) where T : class
    {
        // For now, only string resources are supported. This keeps the implementation simple
        // and avoids any map or engine coupling.
        if (typeof(T) == typeof(string))
        {
            return (T)(object)$"Resource:{resourceId}";
        }

        throw new NotSupportedException($"Dummy resource loader only supports string resources. Requested type: {typeof(T).Name}");
    }
}
