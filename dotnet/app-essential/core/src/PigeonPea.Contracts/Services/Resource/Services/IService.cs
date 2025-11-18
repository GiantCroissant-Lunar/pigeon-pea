using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PigeonPea.Contracts.Resource.Services;

/// <summary>
/// Resource loader service for async loading with progress tracking.
/// </summary>
public interface IService
{
    /// <summary>
    /// Load a resource asynchronously.
    /// </summary>
    /// <typeparam name="T">Type of resource to load.</typeparam>
    /// <param name="resourceId">Unique identifier for the resource.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Loaded resource.</returns>
    Task<T> LoadAsync<T>(string resourceId, IProgress<LoadProgress>? progress = null, CancellationToken ct = default)
        where T : class;

    /// <summary>
    /// Preload multiple resources asynchronously.
    /// </summary>
    /// <param name="resourceIds">Collection of resource identifiers.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task that completes when all resources are preloaded.</returns>
    Task PreloadAsync(IEnumerable<string> resourceIds, IProgress<LoadProgress>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Unload a resource from cache.
    /// </summary>
    /// <param name="resourceId">Unique identifier for the resource.</param>
    void Unload(string resourceId);

    /// <summary>
    /// Check if a resource is currently loaded.
    /// </summary>
    /// <param name="resourceId">Unique identifier for the resource.</param>
    /// <returns>True if loaded, false otherwise.</returns>
    bool IsLoaded(string resourceId);

    /// <summary>
    /// Get the current cache size in bytes.
    /// </summary>
    /// <returns>Cache size in bytes.</returns>
    long GetCacheSize();

    /// <summary>
    /// Clear all cached resources.
    /// </summary>
    void ClearCache();
}
