using System.Collections.Generic;
using PigeonPea.Platform.Contracts.Map.Spatial;
using PigeonPea.Platform.Contracts.Map.Features;

namespace PigeonPea.Platform.Contracts.Map;

/// <summary>
/// Unified map data abstraction - source agnostic.
/// A map is a collection of geographic features with optional terrain.
/// </summary>
public interface IMapData
{
    /// <summary>Map identifier (for caching, references)</summary>
    string MapId { get; }

    /// <summary>Geographic bounds of the map</summary>
    BoundingBox Bounds { get; }

    /// <summary>Available zoom range</summary>
    ZoomRange SupportedZoom { get; }

    /// <summary>Get features within bounds at zoom level</summary>
    IEnumerable<IMapFeature> GetFeatures(BoundingBox bounds, ZoomLevel zoom);

    /// <summary>Get features by type</summary>
    IEnumerable<T> GetFeatures<T>(BoundingBox bounds, ZoomLevel zoom) where T : IMapFeature;

    /// <summary>Optional: terrain elevation at point (null if not available)</summary>
    double? GetElevation(GeoPoint point);

    /// <summary>Optional: terrain type at point</summary>
    TerrainType? GetTerrain(GeoPoint point);

    /// <summary>Optional: Get raw raster data for region</summary>
    byte[]? GetRasterData(BoundingBox bounds, int width, int height);
}
