using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Encoding;

/// <summary>
/// Encodes map features into Mapbox Vector Tile format (Protocol Buffers)
/// </summary>
public interface IVectorTileEncoder
{
    /// <summary>
    /// Encode features as Mapbox Vector Tile (Protobuf format)
    /// </summary>
    /// <param name="features">Features to encode</param>
    /// <param name="tileBounds">Tile coordinate bounds</param>
    /// <param name="zoom">Zoom level</param>
    /// <returns>Serialized Protobuf bytes</returns>
    byte[] Encode(
        IEnumerable<IMapFeature> features,
        BoundingBox tileBounds,
        int zoom);
}
