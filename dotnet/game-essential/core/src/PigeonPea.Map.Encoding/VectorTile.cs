using ProtoBuf;

namespace PigeonPea.Map.Encoding;

/// <summary>
/// Mapbox Vector Tile format (Protocol Buffers)
/// Based on: https://github.com/mapbox/vector-tile-spec
/// </summary>
[ProtoContract]
public class VectorTile
{
    [ProtoMember(3)]
    public List<Layer> Layers { get; set; } = new();

    [ProtoContract]
    public class Layer
    {
        [ProtoMember(1, IsRequired = true)]
        public string Name { get; set; } = string.Empty;

        [ProtoMember(2)]
        public List<Feature> Features { get; set; } = new();

        [ProtoMember(3)]
        public List<string> Keys { get; set; } = new();

        [ProtoMember(4)]
        public List<Value> Values { get; set; } = new();

        [ProtoMember(5)]
        public uint Extent { get; set; } = 4096;

        [ProtoMember(15)]
        public uint Version { get; set; } = 2;
    }

    [ProtoContract]
    public class Feature
    {
        [ProtoMember(1)]
        public ulong Id { get; set; }

        [ProtoMember(2, IsPacked = true)]
        public List<uint> Tags { get; set; } = new();

        [ProtoMember(3)]
        public GeomType Type { get; set; }

        [ProtoMember(4, IsPacked = true)]
        public List<uint> Geometry { get; set; } = new();
    }

    [ProtoContract]
    public class Value
    {
        [ProtoMember(1)]
        public string? StringValue { get; set; }

        [ProtoMember(2)]
        public float FloatValue { get; set; }

        [ProtoMember(3)]
        public double DoubleValue { get; set; }

        [ProtoMember(4)]
        public long IntValue { get; set; }

        [ProtoMember(5)]
        public ulong UintValue { get; set; }

        [ProtoMember(6)]
        public long SintValue { get; set; }

        [ProtoMember(7)]
        public bool BoolValue { get; set; }
    }

    public enum GeomType
    {
        Unknown = 0,
        Point = 1,
        LineString = 2,
        Polygon = 3
    }

    public enum CommandType
    {
        MoveTo = 1,
        LineTo = 2,
        ClosePath = 7
    }
}
