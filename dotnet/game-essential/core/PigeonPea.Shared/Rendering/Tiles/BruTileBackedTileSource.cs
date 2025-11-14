using FantasyMapGenerator.Core.Models;
using PigeonPea.Shared.Rendering;

namespace PigeonPea.SharedApp.Rendering.Tiles;

/// <summary>
/// Placeholder for a BruTile-backed tile source. For now, delegates to SkiaMapRasterizer
/// to preserve our custom art/biome/river rendering while enabling future swap with BruTile.
/// </summary>
public sealed class BruTileBackedTileSource : ITileSource
{
    private sealed record CacheKey(int MapHash, int ViewX, int ViewY, int TileX, int TileY, int TileCols, int TileRows, int Ppc, int Zoom100, bool Biome, bool Rivers, int TimeBucket);
    private sealed class LruCache
    {
        private readonly int _capacity;
        private readonly LinkedList<CacheKey> _lru = new();
        private readonly Dictionary<CacheKey, (TileImage Image, LinkedListNode<CacheKey> Node)> _map = new();

        public LruCache(int capacity) { _capacity = System.Math.Max(8, capacity); }
        public bool TryGet(CacheKey key, out TileImage image)
        {
            if (_map.TryGetValue(key, out var entry))
            {
                _lru.Remove(entry.Node);
                _lru.AddFirst(entry.Node);
                image = entry.Image;
                return true;
            }
            image = default!;
            return false;
        }
        public void Add(CacheKey key, TileImage image)
        {
            if (_map.ContainsKey(key)) return;
            var node = new LinkedListNode<CacheKey>(key);
            _lru.AddFirst(node);
            _map[key] = (image, node);
            if (_map.Count > _capacity)
            {
                var last = _lru.Last;
                if (last != null)
                {
                    _lru.RemoveLast();
                    _map.Remove(last.Value);
                }
            }
        }
    }

    private static readonly LruCache _cache = new(capacity: 128);

    public TileImage GetTile(MapData map, Viewport worldViewport, TileRequest req, double zoom, bool biomeColors, bool rivers)
    {
        int offsetCellsX = req.TileX * req.TileCols;
        int offsetCellsY = req.TileY * req.TileRows;
        var tileViewport = new Viewport(
            worldViewport.X + (int)System.Math.Round(offsetCellsX * zoom),
            worldViewport.Y + (int)System.Math.Round(offsetCellsY * zoom),
            req.TileCols,
            req.TileRows);

        int ppc = System.Math.Max(1, req.PixelsPerCell);
        var key = new CacheKey(map.GetHashCode(), tileViewport.X, tileViewport.Y, req.TileX, req.TileY,
            req.TileCols, req.TileRows, ppc, (int)System.Math.Round(zoom * 100), biomeColors, rivers, 0);
        if (_cache.TryGet(key, out var cached)) return cached;

        var raster = SkiaMapRasterizer.Render(map, tileViewport, zoom, ppc, biomeColors, rivers, 0);
        var img = new TileImage(raster.Rgba, raster.WidthPx, raster.HeightPx);
        _cache.Add(key, img);
        return img;
    }

    public TileImage GetTile(MapData map, Viewport worldViewport, TileRequest req, double zoom, bool biomeColors, bool rivers, double timeSeconds)
    {
        int offsetCellsX = req.TileX * req.TileCols;
        int offsetCellsY = req.TileY * req.TileRows;
        var tileViewport = new Viewport(
            worldViewport.X + (int)System.Math.Round(offsetCellsX * zoom),
            worldViewport.Y + (int)System.Math.Round(offsetCellsY * zoom),
            req.TileCols,
            req.TileRows);

        int ppc = System.Math.Max(1, req.PixelsPerCell);
        int tBucket = (int)System.Math.Round(timeSeconds * 10);
        var key = new CacheKey(map.GetHashCode(), tileViewport.X, tileViewport.Y, req.TileX, req.TileY, req.TileCols, req.TileRows, ppc, (int)System.Math.Round(zoom * 100), biomeColors, rivers, tBucket);
        if (_cache.TryGet(key, out var cached)) return cached;

        var raster = SkiaMapRasterizer.Render(map, tileViewport, zoom, ppc, biomeColors, rivers, timeSeconds);
        var img = new TileImage(raster.Rgba, raster.WidthPx, raster.HeightPx);
        _cache.Add(key, img);
        return img;
    }
}
