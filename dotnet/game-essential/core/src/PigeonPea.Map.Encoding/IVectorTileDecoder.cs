using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Encoding;

/// <summary>
/// Decodes Mapbox Vector Tile format (Protocol Buffers) into map features
/// </summary>
public interface IVectorTileDecoder
{
    /// <summary>
    /// Decode vector tile bytes into map features
    /// </summary>
    /// <param name="data">Compressed or uncompressed Protobuf bytes</param>
    /// <param name="tileBounds">Tile coordinate bounds</param>
    /// <param name="zoom">Zoom level</param>
    /// <returns>Decoded features</returns>
    IEnumerable<IMapFeature> Decode(byte[] data, BoundingBox tileBounds, int zoom);
}
