using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Providers;
using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Composition;

public record RegionRoute(
    BoundingBox Region,
    IMapProvider Provider,
    int Priority = 0);

public class RegionalMapProvider : IMapProvider
{
    private readonly List<RegionRoute> _routes;
    private readonly IMapProvider _fallback;
    private readonly int _gridSize;

    public string ProviderId => $"regional:{_routes.Count}-regions";

    public MapProviderCapabilities Capabilities =>
        _routes.Aggregate(_fallback.Capabilities,
            (caps, route) => caps | route.Provider.Capabilities);

    public RegionalMapProvider(
        IEnumerable<RegionRoute> routes,
        IMapProvider fallback,
        int gridSize = 16)
    {
        _routes = routes.OrderByDescending(r => r.Priority).ToList();
        _fallback = fallback;
        _gridSize = gridSize;
    }

    public async Task<IMapData> GetMapAsync(BoundingBox bounds, CancellationToken ct = default)
    {
        // Find all regions that intersect bounds
        var intersecting = _routes
            .Where(r => r.Region.Intersects(bounds))
            .ToList();

        // Simple case: Single provider covers entire request
        if (intersecting.Count == 1 && Contains(intersecting[0].Region, bounds))
        {
            return await intersecting[0].Provider.GetMapAsync(bounds, ct);
        }

        // Complex case: Multiple providers or partial coverage
        var maps = new System.Collections.Concurrent.ConcurrentBag<(IMapData map, BoundingBox region)>();

        await Parallel.ForEachAsync(intersecting, ct, async (route, token) =>
        {
            var intersection = bounds.Intersection(route.Region);
            if (intersection != null)
            {
                var map = await route.Provider.GetMapAsync(intersection, token);
                maps.Add((map, intersection));
            }
        });

        // Fill gaps with fallback
        var covered = new List<BoundingBox>(maps.Select(m => m.region));
        var gaps = CalculateGaps(bounds, covered);

        await Parallel.ForEachAsync(gaps, ct, async (gap, token) =>
        {
            var fallbackMap = await _fallback.GetMapAsync(gap, token);
            maps.Add((fallbackMap, gap));
        });

        // Merge all maps
        return new CompositeMapData(maps.ToList());
    }

    public bool CanServe(BoundingBox bounds)
    {
        // Can serve if any route or fallback can serve
        return _routes.Any(r => r.Region.Intersects(bounds) && r.Provider.CanServe(bounds)) ||
               _fallback.CanServe(bounds);
    }

    private IEnumerable<BoundingBox> CalculateGaps(
        BoundingBox total,
        List<BoundingBox> covered)
    {
        // Spatial subdivision to find uncovered areas
        // Simplified implementation - production would use R-tree or similar

        var gaps = new List<BoundingBox>();
        var grid = SubdivideGrid(total, _gridSize);

        foreach (var cell in grid)
        {
            if (!covered.Any(c => Contains(c, cell)))
            {
                gaps.Add(cell);
            }
        }

        return MergeAdjacentBoxes(gaps); // Merge continuous gaps
    }

    private IEnumerable<BoundingBox> SubdivideGrid(BoundingBox total, int divisions)
    {
        var width = total.Width / divisions;
        var height = total.Height / divisions;

        for (int x = 0; x < divisions; x++)
        {
            for (int y = 0; y < divisions; y++)
            {
                yield return new BoundingBox(
                    total.X + (x * width),
                    total.Y + (y * height),
                    width,
                    height);
            }
        }
    }

    private IEnumerable<BoundingBox> MergeAdjacentBoxes(List<BoundingBox> boxes)
    {
        // Simplified merge: just return boxes for now as merging is complex
        // A proper implementation would merge adjacent rectangles
        return boxes;
    }

    private bool Contains(BoundingBox outer, BoundingBox inner)
    {
        return outer.MinX <= inner.MinX &&
               outer.MaxX >= inner.MaxX &&
               outer.MinY <= inner.MinY &&
               outer.MaxY >= inner.MaxY;
    }
}
