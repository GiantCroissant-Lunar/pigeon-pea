using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Contracts.Providers;

/// <summary>
/// A source of map data. Can be generative (FMG), imported (OSM), or static.
/// </summary>
public interface IMapProvider
{
    /// <summary>Provider identifier</summary>
    string ProviderId { get; }

    /// <summary>Provider capabilities</summary>
    MapProviderCapabilities Capabilities { get; }

    /// <summary>Get map data for region</summary>
    Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default);

    /// <summary>Check if provider can serve this region</summary>
    bool CanServe(BoundingBox bounds);
}
