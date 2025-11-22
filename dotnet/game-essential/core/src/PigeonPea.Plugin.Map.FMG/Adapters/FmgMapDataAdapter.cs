using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core;
using PigeonPea.Plugin.Map.FMG.Features;

namespace PigeonPea.Plugin.Map.FMG.Adapters;

internal class FmgMapDataAdapter : IMapData
{
    private readonly MapData _fmg;
    private readonly BoundingBox _bounds;

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
        var cell = _fmg.GetCellAt(point.X, point.Y);
        if (cell == null) return null;

        return cell.Biome switch
        {
            0 => TerrainType.Ocean,
            1 => TerrainType.Sea,
            2 => TerrainType.Lake,
            3 => TerrainType.Beach,
            4 => TerrainType.Grassland,
            5 => TerrainType.Forest,
            6 => TerrainType.Desert,
            7 => TerrainType.Hill,
            8 => TerrainType.Mountain,
            9 => TerrainType.Swamp,
            10 => TerrainType.Tundra,
            11 => TerrainType.Ice,
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
        return null;
    }
}
