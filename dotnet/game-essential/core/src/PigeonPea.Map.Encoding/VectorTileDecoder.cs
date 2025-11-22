using System.IO.Compression;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Geometry;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Encoding;

/// <summary>
/// Decodes Mapbox Vector Tiles into IMapFeatures
/// </summary>
public class VectorTileDecoder : IVectorTileDecoder
{
    private const int TileExtent = 4096;

    public IEnumerable<IMapFeature> Decode(byte[] data, BoundingBox tileBounds, int zoom)
    {
        if (data.Length == 0)
        {
            yield break;
        }

        var decompressed = TryDecompress(data, out var uncompressed) ? uncompressed : data;

        VectorTile tile;
        try
        {
            using var ms = new MemoryStream(decompressed);
            tile = ProtoBuf.Serializer.Deserialize<VectorTile>(ms);
        }
        catch
        {
            yield break;
        }

        foreach (var layer in tile.Layers)
        {
            foreach (var vtFeature in layer.Features)
            {
                var feature = DecodeFeature(vtFeature, layer, tileBounds, zoom);
                if (feature != null)
                {
                    yield return feature;
                }
            }
        }
    }

    private DecodedMapFeature? DecodeFeature(
        VectorTile.Feature vtFeature,
        VectorTile.Layer layer,
        BoundingBox tileBounds,
        int zoom)
    {
        var geometry = DecodeGeometry(vtFeature, tileBounds, layer.Extent);
        if (geometry == null)
        {
            return null;
        }

        var metadata = new Dictionary<string, object>();
        string? name = null;
        var kind = FeatureKind.Point;

        for (int i = 0; i < vtFeature.Tags.Count; i += 2)
        {
            if (i + 1 >= vtFeature.Tags.Count) break;

            var keyIdx = (int)vtFeature.Tags[i];
            var valueIdx = (int)vtFeature.Tags[i + 1];

            if (keyIdx >= layer.Keys.Count || valueIdx >= layer.Values.Count)
                continue;

            var key = layer.Keys[keyIdx];
            var value = GetValue(layer.Values[valueIdx]);

            if (key == "name")
            {
                name = value?.ToString();
            }
            else if (key == "kind" && value != null)
            {
                if (Enum.TryParse<FeatureKind>(value.ToString(), out var parsedKind))
                {
                    kind = parsedKind;
                }
            }
            else if (value != null)
            {
                metadata[key] = value;
            }
        }

        return new DecodedMapFeature
        {
            FeatureId = vtFeature.Id.ToString(),
            Kind = kind,
            Name = name,
            Geometry = geometry,
            MinZoom = new ZoomLevel(zoom),
            Metadata = metadata
        };
    }

    private IGeometry? DecodeGeometry(VectorTile.Feature vtFeature, BoundingBox tileBounds, uint extent)
    {
        if (vtFeature.Geometry.Count == 0)
        {
            return null;
        }

        return vtFeature.Type switch
        {
            VectorTile.GeomType.Point => DecodePoint(vtFeature.Geometry, tileBounds, extent),
            VectorTile.GeomType.LineString => DecodeLineString(vtFeature.Geometry, tileBounds, extent),
            VectorTile.GeomType.Polygon => DecodePolygon(vtFeature.Geometry, tileBounds, extent),
            _ => null
        };
    }

    private Point? DecodePoint(List<uint> geometry, BoundingBox tileBounds, uint extent)
    {
        if (geometry.Count < 3)
        {
            return null;
        }

        var x = DecodeParameter(geometry[1]);
        var y = DecodeParameter(geometry[2]);

        var worldPoint = TileToWorld(x, y, tileBounds, extent);
        return new Point(worldPoint.X, worldPoint.Y);
    }

    private LineString? DecodeLineString(List<uint> geometry, BoundingBox tileBounds, uint extent)
    {
        var points = new List<GeoPoint>();
        int x = 0, y = 0;
        int i = 0;

        while (i < geometry.Count)
        {
            var cmdAndCount = geometry[i++];
            var cmd = cmdAndCount & 0x7;
            var count = (int)(cmdAndCount >> 3);

            if (cmd == (int)VectorTile.CommandType.MoveTo || cmd == (int)VectorTile.CommandType.LineTo)
            {
                for (int j = 0; j < count && i + 1 < geometry.Count; j++)
                {
                    x += DecodeParameter(geometry[i++]);
                    y += DecodeParameter(geometry[i++]);

                    var worldPoint = TileToWorld(x, y, tileBounds, extent);
                    points.Add(worldPoint);
                }
            }
        }

        return points.Count >= 2 ? new LineString(points) : null;
    }

    private Polygon? DecodePolygon(List<uint> geometry, BoundingBox tileBounds, uint extent)
    {
        var points = new List<GeoPoint>();
        int x = 0, y = 0;
        int i = 0;

        while (i < geometry.Count)
        {
            var cmdAndCount = geometry[i++];
            var cmd = cmdAndCount & 0x7;
            var count = (int)(cmdAndCount >> 3);

            if (cmd == (int)VectorTile.CommandType.MoveTo || cmd == (int)VectorTile.CommandType.LineTo)
            {
                for (int j = 0; j < count && i + 1 < geometry.Count; j++)
                {
                    x += DecodeParameter(geometry[i++]);
                    y += DecodeParameter(geometry[i++]);

                    var worldPoint = TileToWorld(x, y, tileBounds, extent);
                    points.Add(worldPoint);
                }
            }
            else if (cmd == (int)VectorTile.CommandType.ClosePath)
            {
                break;
            }
        }

        return points.Count >= 3 ? new Polygon(points) : null;
    }

    private GeoPoint TileToWorld(int tileX, int tileY, BoundingBox tileBounds, uint extent)
    {
        var relX = (double)tileX / extent;
        var relY = (double)tileY / extent;

        return new GeoPoint(
            tileBounds.X + (relX * tileBounds.Width),
            tileBounds.Y + (relY * tileBounds.Height)
        );
    }

    private int DecodeParameter(uint value)
    {
        return (int)((value >> 1) ^ (-(value & 1)));
    }

    private object? GetValue(VectorTile.Value value)
    {
        if (value.StringValue != null) return value.StringValue;
        if (value.BoolValue) return value.BoolValue;
        if (value.IntValue != 0) return value.IntValue;
        if (Math.Abs(value.DoubleValue) > 0.0001) return value.DoubleValue;
        if (Math.Abs(value.FloatValue) > 0.0001) return value.FloatValue;
        if (value.UintValue != 0) return value.UintValue;
        if (value.SintValue != 0) return value.SintValue;

        return null;
    }

    private bool TryDecompress(byte[] data, out byte[] result)
    {
        try
        {
            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();

            gzip.CopyTo(output);
            result = output.ToArray();
            return true;
        }
        catch
        {
            result = Array.Empty<byte>();
            return false;
        }
    }
}
