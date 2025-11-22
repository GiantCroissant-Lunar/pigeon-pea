using SQLite;

namespace PigeonPea.Map.Export;

[Table("tiles")]
internal class MbTile
{
    [Column("zoom_level")]
    [Indexed(Name = "tile_index", Order = 1, Unique = true)]
    public int ZoomLevel { get; set; }

    [Column("tile_column")]
    [Indexed(Name = "tile_index", Order = 2, Unique = true)]
    public int TileColumn { get; set; }

    [Column("tile_row")]
    [Indexed(Name = "tile_index", Order = 3, Unique = true)]
    public int TileRow { get; set; }

    [Column("tile_data")]
    public byte[] TileData { get; set; } = Array.Empty<byte>();
}

[Table("metadata")]
internal class MbTileMetadata
{
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("value")]
    public string Value { get; set; } = string.Empty;
}
