using BruTile;
using BruTile.MbTiles;
using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Providers;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Encoding;

namespace PigeonPea.Plugin.Map.MBTiles;

/// <summary>
/// Map provider that reads from MBTiles container files using BruTile
/// </summary>
public class MBTilesMapProvider : IMapProvider
{
    private readonly string _filePath;
    private readonly MbTilesTileSource _tileSource;
    private readonly IVectorTileDecoder _decoder;
    private readonly BoundingBox _bounds;
    private readonly int _minZoom;
    private readonly int _maxZoom;
    private readonly int _screenWidth;

    public string ProviderId { get; }

    public MapProviderCapabilities Capabilities =>
        MapProviderCapabilities.FullWorld |
        MapProviderCapabilities.Offline |
        MapProviderCapabilities.Cacheable;

    public MBTilesMapProvider(string filePath, IVectorTileDecoder? decoder = null, int screenWidth = 1024)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"MBTiles file not found: {filePath}");
        }

        _filePath = filePath;
        _decoder = decoder ?? new VectorTileDecoder();
        _screenWidth = screenWidth;
        
        // Use BruTile to read MBTiles
        _tileSource = new MbTilesTileSource(new SQLite.SQLiteConnectionString(filePath, false));
        
        ProviderId = $"mbtiles:{Path.GetFileNameWithoutExtension(filePath)}";

        var extent = _tileSource.Schema.Extent;
        _bounds = new BoundingBox(extent.MinX, extent.MinY, extent.Width, extent.Height);
        
        // BruTile schema resolutions are usually sorted high to low (zoomed out to zoomed in? or vice versa?)
        // Actually resolutions are dictionary int -> Resolution.
        var levels = _tileSource.Schema.Resolutions.Keys.Select(k => Convert.ToInt32(k)).ToList();
        _minZoom = levels.Min();
        _maxZoom = levels.Max();
    }

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default)
    {
        var zoom = CalculateOptimalZoom(bounds);
        var allFeatures = new List<PigeonPea.Map.Contracts.Features.IMapFeature>();

        // Calculate tiles manually based on our custom coordinate system
        // (BruTile schema might not match our custom bounds/resolutions)
        var tileInfos = GetTilesInBounds(bounds, zoom);

        foreach (var tileInfo in tileInfos)
        {
            ct.ThrowIfCancellationRequested();

            var tileData = await _tileSource.GetTileAsync(tileInfo);
            if (tileData != null && tileData.Length > 0)
            {
                var tileBounds = TileInfoToBounds(tileInfo);
                var features = _decoder.Decode(tileData, tileBounds, tileInfo.Index.Level).ToList();
                allFeatures.AddRange(features);
            }
        }

        return new MBTilesMapData(
            allFeatures,
            bounds,
            _minZoom,
            _maxZoom,
            ProviderId);
    }

    private IEnumerable<TileInfo> GetTilesInBounds(BoundingBox bounds, int zoom)
    {
        var tilesPerSide = 1 << zoom;
        var tileWidth = _bounds.Width / tilesPerSide;
        var tileHeight = _bounds.Height / tilesPerSide;

        // Calculate intersecting tile range
        // Note: Y axis in our system goes down (top-left origin)
        var minCol = (int)Math.Floor((bounds.X - _bounds.X) / tileWidth);
        var maxCol = (int)Math.Floor((bounds.X + bounds.Width - _bounds.X - 0.001) / tileWidth); // -epsilon to avoid edge case
        var minRow = (int)Math.Floor((bounds.Y - _bounds.Y) / tileHeight);
        var maxRow = (int)Math.Floor((bounds.Y + bounds.Height - _bounds.Y - 0.001) / tileHeight);

        // Clamp to valid range
        minCol = Math.Max(0, minCol);
        maxCol = Math.Min(tilesPerSide - 1, maxCol);
        minRow = Math.Max(0, minRow);
        maxRow = Math.Min(tilesPerSide - 1, maxRow);

        for (int col = minCol; col <= maxCol; col++)
        {
            for (int row = minRow; row <= maxRow; row++)
            {
                yield return new TileInfo
                {
                    Index = new TileIndex(col, row, zoom),
                    Extent = new Extent(
                        _bounds.X + col * tileWidth,
                        _bounds.Y + row * tileHeight,
                        _bounds.X + (col + 1) * tileWidth,
                        _bounds.Y + (row + 1) * tileHeight
                    )
                };
            }
        }
    }

    public bool CanServe(BoundingBox bounds)
    {
        return _bounds.Intersects(bounds);
    }

    private int CalculateOptimalZoom(BoundingBox bounds)
    {
        // Find the resolution that best matches the requested bounds width
        // Assuming we want to see the whole bounds in roughly one screen width
        // This is a heuristic.
        
        // Simple heuristic: find zoom level where tile width is closest to bounds width / 4
        // (meaning we load ~4x4 tiles)
        
        var targetRes = bounds.Width / (double)_screenWidth;
        
        // Find closest resolution in schema
        var closestZoom = _minZoom;
        var minDiff = double.MaxValue;

        foreach (var res in _tileSource.Schema.Resolutions)
        {
            var diff = Math.Abs(res.Value.UnitsPerPixel - targetRes);
            if (diff < minDiff)
            {
                minDiff = diff;
                closestZoom = Convert.ToInt32(res.Key);
            }
        }

        return closestZoom;
    }

    private BoundingBox TileInfoToBounds(TileInfo tileInfo)
    {
        var extent = tileInfo.Extent;
        return new BoundingBox(
            extent.MinX,
            extent.MinY,
            extent.Width,
            extent.Height);
    }
}
