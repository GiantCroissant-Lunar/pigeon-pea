using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Geometry;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Encoding;

/// <summary>
/// Encodes IMapFeatures into Mapbox Vector Tile format
/// </summary>
public class VectorTileEncoder : IVectorTileEncoder
{
    private const int TileExtent = 4096;

    public byte[] Encode(
        IEnumerable<IMapFeature> features,
        BoundingBox tileBounds,
        int zoom)
    {
        var tile = new VectorTile();

        var layerGroups = features.GroupBy(GetLayerName);

        foreach (var group in layerGroups)
        {
            var layer = new VectorTile.Layer
            {
                Name = group.Key,
                Version = 2,
                Extent = TileExtent
            };

            var keyIndex = new Dictionary<string, int>();
            var valueIndex = new Dictionary<object, int>();

            foreach (var feature in group)
            {
                var vtFeature = EncodeFeature(feature, tileBounds, layer, keyIndex, valueIndex);
                if (vtFeature != null)
                {
                    layer.Features.Add(vtFeature);
                }
            }

            if (layer.Features.Count > 0)
            {
                tile.Layers.Add(layer);
            }
        }

        using var ms = new MemoryStream();
        ProtoBuf.Serializer.Serialize(ms, tile);
        return ms.ToArray();
    }

    private string GetLayerName(IMapFeature feature) => feature.Kind switch
    {
        FeatureKind.Ocean or FeatureKind.Sea or FeatureKind.Lake => VectorTileLayers.Water,
        FeatureKind.River or FeatureKind.Stream => VectorTileLayers.Rivers,
        FeatureKind.Capital or FeatureKind.City or FeatureKind.Town or FeatureKind.Village => VectorTileLayers.Cities,
        FeatureKind.Road or FeatureKind.Path => VectorTileLayers.Roads,
        FeatureKind.CountryBorder or FeatureKind.StateBorder => VectorTileLayers.Borders,
        FeatureKind.Dungeon or FeatureKind.Marker => VectorTileLayers.Markers,
        _ => VectorTileLayers.Land
    };

    private VectorTile.Feature? EncodeFeature(
        IMapFeature feature,
        BoundingBox tileBounds,
        VectorTile.Layer layer,
        Dictionary<string, int> keyIndex,
        Dictionary<object, int> valueIndex)
    {
        var geometry = EncodeGeometry(feature.Geometry, tileBounds);
        if (geometry == null || geometry.Count == 0)
            return null;

        var vtFeature = new VectorTile.Feature
        {
            Id = (ulong)Math.Abs(feature.FeatureId.GetHashCode()),
            Type = GetGeometryType(feature.Geometry),
            Geometry = geometry
        };

        EncodeTags(feature, layer, keyIndex, valueIndex, vtFeature);

        return vtFeature;
    }

    private void EncodeTags(
        IMapFeature feature,
        VectorTile.Layer layer,
        Dictionary<string, int> keyIndex,
        Dictionary<object, int> valueIndex,
        VectorTile.Feature vtFeature)
    {
        if (!string.IsNullOrEmpty(feature.Name))
        {
            AddTag("name", feature.Name, layer, keyIndex, valueIndex, vtFeature);
        }

        AddTag("kind", feature.Kind.ToString(), layer, keyIndex, valueIndex, vtFeature);

        foreach (var (key, value) in feature.Metadata)
        {
            if (value != null)
            {
                AddTag(key, value, layer, keyIndex, valueIndex, vtFeature);
            }
        }
    }

    private void AddTag(
        string key,
        object value,
        VectorTile.Layer layer,
        Dictionary<string, int> keyIndex,
        Dictionary<object, int> valueIndex,
        VectorTile.Feature vtFeature)
    {
        if (!keyIndex.TryGetValue(key, out var keyIdx))
        {
            keyIdx = layer.Keys.Count;
            layer.Keys.Add(key);
            keyIndex[key] = keyIdx;
        }

        if (!valueIndex.TryGetValue(value, out var valueIdx))
        {
            valueIdx = layer.Values.Count;
            layer.Values.Add(CreateValue(value));
            valueIndex[value] = valueIdx;
        }

        vtFeature.Tags.Add((uint)keyIdx);
        vtFeature.Tags.Add((uint)valueIdx);
    }

    private VectorTile.Value CreateValue(object value) => value switch
    {
        string s => new VectorTile.Value { StringValue = s },
        int i => new VectorTile.Value { IntValue = i },
        long l => new VectorTile.Value { IntValue = l },
        float f => new VectorTile.Value { FloatValue = f },
        double d => new VectorTile.Value { DoubleValue = d },
        bool b => new VectorTile.Value { BoolValue = b },
        _ => new VectorTile.Value { StringValue = value.ToString() }
    };

    private List<uint>? EncodeGeometry(IGeometry geometry, BoundingBox tileBounds)
    {
        return geometry.Type switch
        {
            GeometryType.Point => EncodePoint((Point)geometry, tileBounds),
            GeometryType.LineString => EncodeLineString((LineString)geometry, tileBounds),
            GeometryType.Polygon => EncodePolygon((Polygon)geometry, tileBounds),
            _ => null
        };
    }

    private List<uint>? EncodePoint(Point point, BoundingBox tileBounds)
    {
        var (x, y) = WorldToTile(point.X, point.Y, tileBounds);
        if (x < 0 || x >= TileExtent || y < 0 || y >= TileExtent)
            return null;

        var commands = new List<uint>
        {
            EncodeCommand(VectorTile.CommandType.MoveTo, 1),
            EncodeParameter(x),
            EncodeParameter(y)
        };

        return commands;
    }

    private List<uint>? EncodeLineString(LineString line, BoundingBox tileBounds)
    {
        var coords = line.Points
            .Select(c => WorldToTile(c.X, c.Y, tileBounds))
            .Where(p => p.x >= 0 && p.x < TileExtent && p.y >= 0 && p.y < TileExtent)
            .ToList();

        if (coords.Count < 2)
            return null;

        var commands = new List<uint>
        {
            EncodeCommand(VectorTile.CommandType.MoveTo, 1),
            EncodeParameter(coords[0].x),
            EncodeParameter(coords[0].y)
        };

        if (coords.Count > 1)
        {
            commands.Add(EncodeCommand(VectorTile.CommandType.LineTo, coords.Count - 1));

            for (int i = 1; i < coords.Count; i++)
            {
                var dx = coords[i].x - coords[i - 1].x;
                var dy = coords[i].y - coords[i - 1].y;
                commands.Add(EncodeParameter(dx));
                commands.Add(EncodeParameter(dy));
            }
        }

        return commands;
    }

    private List<uint>? EncodePolygon(Polygon polygon, BoundingBox tileBounds)
    {
        var exteriorRing = polygon.ExteriorRing;
        var coords = exteriorRing
            .Select(c => WorldToTile(c.X, c.Y, tileBounds))
            .Where(p => p.x >= 0 && p.x < TileExtent && p.y >= 0 && p.y < TileExtent)
            .ToList();

        if (coords.Count < 3)
            return null;

        var commands = new List<uint>
        {
            EncodeCommand(VectorTile.CommandType.MoveTo, 1),
            EncodeParameter(coords[0].x),
            EncodeParameter(coords[0].y)
        };

        if (coords.Count > 1)
        {
            commands.Add(EncodeCommand(VectorTile.CommandType.LineTo, coords.Count - 1));

            for (int i = 1; i < coords.Count; i++)
            {
                var dx = coords[i].x - coords[i - 1].x;
                var dy = coords[i].y - coords[i - 1].y;
                commands.Add(EncodeParameter(dx));
                commands.Add(EncodeParameter(dy));
            }

            commands.Add(EncodeCommand(VectorTile.CommandType.ClosePath, 1));
        }

        return commands;
    }

    private (int x, int y) WorldToTile(double worldX, double worldY, BoundingBox tileBounds)
    {
        var relX = (worldX - tileBounds.X) / tileBounds.Width;
        var relY = (worldY - tileBounds.Y) / tileBounds.Height;

        return (
            (int)Math.Round(relX * TileExtent),
            (int)Math.Round(relY * TileExtent)
        );
    }

    private VectorTile.GeomType GetGeometryType(IGeometry geometry) => geometry.Type switch
    {
        GeometryType.Point or GeometryType.MultiPoint => VectorTile.GeomType.Point,
        GeometryType.LineString or GeometryType.MultiLineString => VectorTile.GeomType.LineString,
        GeometryType.Polygon or GeometryType.MultiPolygon => VectorTile.GeomType.Polygon,
        _ => VectorTile.GeomType.Unknown
    };

    private uint EncodeCommand(VectorTile.CommandType command, int count)
    {
        return (uint)((int)command & 0x7) | (uint)(count << 3);
    }

    private uint EncodeParameter(int value)
    {
        return (uint)((value << 1) ^ (value >> 31));
    }
}
