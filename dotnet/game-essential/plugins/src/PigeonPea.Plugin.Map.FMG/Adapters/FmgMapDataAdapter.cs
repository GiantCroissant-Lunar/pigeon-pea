using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core;
using PigeonPea.Plugin.Map.FMG.Features;
using FantasyMapModels = FantasyMapGenerator.Core.Models;
using FantasyMapGenerator.Rendering;
using SkiaSharp;

namespace PigeonPea.Plugin.Map.FMG.Adapters;

internal class FmgMapDataAdapter : IMapData
{
    private readonly MapData _fmg;
    private readonly BoundingBox _bounds;
    private SKBitmap? _cachedRender;
    private readonly object _renderLock = new();

    public string MapId => $"fmg-{GetHashCode()}";
    public BoundingBox Bounds => _bounds;
    public ZoomRange SupportedZoom => new(0, 14);

    public FmgMapDataAdapter(MapData fmg, BoundingBox bounds)
    {
        _fmg = fmg;
        _bounds = bounds;
    }

    public IEnumerable<IMapFeature> GetFeatures(BoundingBox bounds, ZoomLevel zoom)
    {
        var features = new List<IMapFeature>();

        var inner = _fmg.Inner;

        // Render ALL cells (both water and land) as polygons at low zoom for terrain
        if (zoom <= 6)
        {
            foreach (var cell in inner.Cells)
            {
                var center = cell.Center;
                if (!bounds.Contains(new GeoPoint(center.X, center.Y)))
                {
                    continue;
                }

                // Determine feature kind based on cell type
                FeatureKind kind;
                if (cell.IsOcean)
                    kind = FeatureKind.Ocean;
                else if (cell.IsLake)
                    kind = FeatureKind.Lake;
                else
                    kind = FeatureKind.Land;

                features.Add(new FmgWaterCellAdapter(cell, inner, kind));
            }
        }

        if (zoom >= 2)
        {
            foreach (var burg in _fmg.Burgs.Where(b => b != null && bounds.Contains(new GeoPoint(b.Position.X, b.Position.Y))))
            {
                features.Add(new FmgSettlementAdapter(burg));
            }
        }

        if (zoom >= 4 && _fmg.Rivers != null)
        {
            for (int i = 0; i < _fmg.Rivers.Count; i++)
            {
                var river = _fmg.Rivers[i];
                if (IntersectsBounds(river, bounds))
                {
                    features.Add(new FmgRiverAdapter(river, _fmg, i));
                }
            }
        }

        if (zoom >= 4 && _fmg.States != null)
        {
            foreach (var state in _fmg.States.Where(s => s != null))
            {
                features.Add(new FmgBorderAdapter(state, _fmg));
            }
        }

        if (_fmg.Markers != null)
        {
            foreach (var marker in _fmg.Markers.Where(m => bounds.Contains(new GeoPoint(m.Position.X, m.Position.Y))))
            {
                features.Add(new FmgMarkerAdapter(marker));
            }
        }

        return features;
    }

    public IEnumerable<T> GetFeatures<T>(BoundingBox bounds, ZoomLevel zoom) where T : IMapFeature
    {
        return GetFeatures(bounds, zoom).OfType<T>();
    }

    public double? GetElevation(GeoPoint point)
    {
        var cell = _fmg.GetCellAt(point.X, point.Y);
        return cell?.Height;
    }

    public TerrainType? GetTerrain(GeoPoint point)
    {
        var inner = _fmg.Inner;
        var cell = inner.GetCellAt(point.X, point.Y);
        if (cell == null) return null;

        if (cell.IsWater)
        {
            if (cell.IsLake)
            {
                return TerrainType.Lake;
            }

            return TerrainType.Ocean;
        }

        var biomeId = cell.Biome;

        return biomeId switch
        {
            FantasyMapModels.BiomeTypes.HotDesert or FantasyMapModels.BiomeTypes.ColdDesert
                => TerrainType.Desert,
            FantasyMapModels.BiomeTypes.Savanna
                => TerrainType.Plains,
            FantasyMapModels.BiomeTypes.Grassland
                => TerrainType.Grassland,
            FantasyMapModels.BiomeTypes.TropicalSeasonalForest
                or FantasyMapModels.BiomeTypes.TemperateDeciduousForest
                or FantasyMapModels.BiomeTypes.TropicalRainforest
                or FantasyMapModels.BiomeTypes.TemperateRainforest
                or FantasyMapModels.BiomeTypes.Taiga
                => TerrainType.Forest,
            FantasyMapModels.BiomeTypes.Tundra
                => TerrainType.Tundra,
            FantasyMapModels.BiomeTypes.Glacier
                => TerrainType.Ice,
            FantasyMapModels.BiomeTypes.Wetland
                => TerrainType.Wetland,
            _ => TerrainType.Plains
        };
    }

    private bool IntersectsBounds(River river, BoundingBox bounds)
    {
        foreach (var cellId in river.Cells)
        {
            if (cellId >= 0 && cellId < _fmg.Cells.Count)
            {
                var cell = _fmg.Cells[cellId];
                if (bounds.Contains(new GeoPoint(cell.Center.X, cell.Center.Y)))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public byte[]? GetRasterData(BoundingBox bounds, int width, int height)
    {
        System.Diagnostics.Debug.WriteLine($"[FmgMapDataAdapter] GetRasterData called. Width: {width}, Height: {height}");
        lock (_renderLock)
        {
            if (_cachedRender == null)
            {
                System.Diagnostics.Debug.WriteLine("[FmgMapDataAdapter] Rendering full map to cache...");
                try
                {
                    // Render full map at its native resolution (1 pixel per unit)
                    // This ensures we have a high-quality base to sample from.
                    // If the map is huge, we might want to limit this, but for now we follow the plan.
                    var renderer = new RasterBlurRenderer(TerrainColorSchemes.Classic);
                    int mapW = _fmg.Inner.Width;
                    int mapH = _fmg.Inner.Height;
                    System.Diagnostics.Debug.WriteLine($"[FmgMapDataAdapter] Map dimensions: {mapW}x{mapH}");

                    using var surface = renderer.RenderMap(_fmg.Inner, mapW, mapH);
                    using var image = surface.Snapshot();

                    // Explicitly create bitmap with known format to avoid platform-specific defaults issues
                    var info = new SKImageInfo(mapW, mapH, SKColorType.Rgba8888, SKAlphaType.Premul);
                    _cachedRender = new SKBitmap(info);

                    if (!image.ReadPixels(info, _cachedRender.GetPixels(), info.RowBytes, 0, 0))
                    {
                        System.Diagnostics.Debug.WriteLine("[FmgMapDataAdapter] Failed to read pixels from surface snapshot.");
                        _cachedRender = null;
                        return null;
                    }

                    System.Diagnostics.Debug.WriteLine("[FmgMapDataAdapter] Map rendered and cached successfully.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FmgMapDataAdapter] Error rendering map: {ex}");
                    return null;
                }
            }

            if (_cachedRender == null)
            {
                System.Diagnostics.Debug.WriteLine("[FmgMapDataAdapter] _cachedRender is null after attempt.");
                return null;
            }

            // Create destination bitmap
            using var destBitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using var canvas = new SKCanvas(destBitmap);

            // Calculate source rectangle from world bounds
            // _fmg coordinates are 0,0 to Width,Height
            float sx = (float)bounds.MinX;
            float sy = (float)bounds.MinY;
            float sw = (float)(bounds.MaxX - bounds.MinX);
            float sh = (float)(bounds.MaxY - bounds.MinY);
            var srcRect = new SKRect(sx, sy, sx + sw, sy + sh);
            var destRect = new SKRect(0, 0, width, height);

            // Draw the relevant portion of the cached map to the destination
            // Use medium quality filtering for downscaling/upscaling
            using var paint = new SKPaint { FilterQuality = SKFilterQuality.Medium };
            canvas.DrawBitmap(_cachedRender, srcRect, destRect, paint);
            canvas.Flush();

            return destBitmap.Bytes;
        }
    }
}
