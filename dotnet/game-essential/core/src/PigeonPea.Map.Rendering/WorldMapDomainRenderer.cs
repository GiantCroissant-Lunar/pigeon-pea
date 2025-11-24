using System;
using System.Linq;
using PigeonPea.Map.Contracts;
using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Rendering.Contracts;
using SadRogue.Primitives;
using SkiaSharp;
using SharedViewport = PigeonPea.Shared.Rendering.Viewport;
using Contracts = PigeonPea.Map.Contracts;

namespace PigeonPea.Map.Rendering;

public sealed class WorldMapRenderSettings
{
    public ColorScheme ColorScheme { get; init; } = ColorScheme.Original;
    public bool ShowSettlements { get; init; } = true;
    public bool ShowRivers { get; init; } = true;
    public bool ShowBorders { get; init; } = false;
    public bool ShowMarkers { get; init; } = true;
    public int PixelsPerCell { get; init; } = 2;
    public bool ShowVoronoiOutlines { get; init; } = false;
}

public sealed class WorldMapDomainRenderer : IDomainRenderer
{
    public string Id => "world-map-domain";

    public void Render(object world, IRenderCommandList commands, RenderOptions options)
    {
        if (world is not IMapData map)
        {
            return;
        }

        var settings = options.ActiveScale as WorldMapRenderSettings
                       ?? new WorldMapRenderSettings();

        Render(map, commands, options, settings);
    }

    public void Render(IMapData map, IRenderCommandList commands, RenderOptions options, WorldMapRenderSettings settings)
    {
        var v = options.Viewport;
        int ppc = Math.Max(1, settings.PixelsPerCell);
        int widthPx = Math.Max(1, v.Width * ppc);
        int heightPx = Math.Max(1, v.Height * ppc);

        // 1. Setup viewport transform
        commands.BeginFrame();
        commands.SetViewport(new PigeonPea.Rendering.Contracts.Viewport(v.X, v.Y, v.Width, v.Height));
        commands.SetCamera((int)(v.X + v.Width / 2), (int)(v.Y + v.Height / 2), options.Zoom);

        // Use ocean blue as background instead of black
        commands.Clear(new Color(53, 111, 163));

        // Calculate visible bounds
        // Bounds width/height should be extent in world units (not max coordinate).
        var bounds = new BoundingBox(v.X, v.Y, v.Width * options.Zoom, v.Height * options.Zoom);
        int zoomLevel = CalculateZoomLevel(options.Zoom);

        // 2. Fetch features
        var features = map.GetFeatures(bounds, zoomLevel).ToList();
        byte[]? raster = null;
        try
        {
            raster = map.GetRasterData(bounds, widthPx, heightPx);
        }
        catch
        {
            raster = null;
        }

        // Debug: Log feature counts
        System.Diagnostics.Debug.WriteLine($"Fetched {features.Count} features");
        foreach (var kind in features.GroupBy(f => f.Kind))
        {
            System.Diagnostics.Debug.WriteLine($"  {kind.Key}: {kind.Count()}");
        }

        // 3. Render base terrain
        if (raster != null && raster.Length == widthPx * heightPx * 4)
        {
            // Use pre-rasterized FMG buffer for terrain (matches FMG look)
            commands.DrawBuffer(0, 0, widthPx, heightPx, raster);
        }
        else
        {
            // Fallback to vector polygons
            var landFeatures = features.Where(f => f.Kind == FeatureKind.Land);
            foreach (var feature in landFeatures)
            {
                if (feature.Geometry is Contracts.Geometry.Polygon poly)
                {
                    RenderPolygon(commands, poly, v, options.Zoom, ppc, ResolveFillColor(feature, settings));
                }
            }

            var waterFeatures = features.Where(f => f.Kind == FeatureKind.Ocean || f.Kind == FeatureKind.Lake);
            foreach (var feature in waterFeatures)
            {
                if (feature.Geometry is Contracts.Geometry.Polygon poly)
                {
                    RenderPolygon(commands, poly, v, options.Zoom, ppc, ResolveFillColor(feature, settings));
                }
            }
        }

        // Optional: overlay Voronoi outlines for debugging
        if (settings.ShowVoronoiOutlines)
        {
            var cellPolys = features.Where(f =>
                f.Kind == FeatureKind.Land ||
                f.Kind == FeatureKind.Ocean ||
                f.Kind == FeatureKind.Lake);

            foreach (var feature in cellPolys)
            {
                if (feature.Geometry is Contracts.Geometry.Polygon poly)
                {
                    RenderPolygonOutline(commands, poly, v, options.Zoom, ppc, new Color(30, 30, 30, 120));
                }
            }
        }

        // 4. Render Rivers
        if (settings.ShowRivers)
        {
            var rivers = features.Where(f => f.Kind == FeatureKind.River);
            foreach (var river in rivers)
            {
                if (river.Geometry is Contracts.Geometry.LineString line)
                {
                    RenderRiver(commands, river, line, v, options.Zoom, ppc);
                }
            }
        }

        // 5. Render Borders
        if (settings.ShowBorders)
        {
            var borders = features.Where(f => f.Kind == FeatureKind.StateBorder || f.Kind == FeatureKind.RegionBorder);
            foreach (var border in borders)
            {
                if (border.Geometry is Contracts.Geometry.Polygon poly)
                {
                    // Render border as outline
                    RenderPolygonOutline(commands, poly, v, options.Zoom, ppc, new Color(255, 200, 0));
                }
            }
        }

        // 6. Render Settlements
        if (settings.ShowSettlements)
        {
            var settlements = features.Where(f =>
                f.Kind == FeatureKind.Capital ||
                f.Kind == FeatureKind.City ||
                f.Kind == FeatureKind.Town ||
                f.Kind == FeatureKind.Village);

            foreach (var settlement in settlements)
            {
                if (settlement.Geometry is Contracts.Geometry.Point pt)
                {
                    RenderSettlement(commands, settlement, pt, v, options.Zoom, ppc);
                }
            }
        }

        commands.EndFrame();
    }

    private void RenderPolygon(
        IRenderCommandList commands,
        Contracts.Geometry.Polygon poly,
        PigeonPea.Rendering.Contracts.Viewport v,
        double zoom,
        int ppc,
        Color color)
    {
        // Transform polygon points to screen space
        var screenPoints = poly.ExteriorRing
            .Select(p => new Point(WorldToScreenX(p.X, v, zoom, ppc), WorldToScreenY(p.Y, v, zoom, ppc)))
            .ToArray();

        System.Diagnostics.Debug.WriteLine($"Rendering polygon with {screenPoints.Length} points, color=({color.R},{color.G},{color.B})");
        if (screenPoints.Length > 0)
        {
            System.Diagnostics.Debug.WriteLine($"  First point: world=({poly.ExteriorRing.First().X},{poly.ExteriorRing.First().Y}) -> screen=({screenPoints[0].X},{screenPoints[0].Y})");
        }

        commands.DrawPolygon(screenPoints, color);
    }

    private void RenderPolygonOutline(IRenderCommandList commands, Contracts.Geometry.Polygon poly, PigeonPea.Rendering.Contracts.Viewport v, double zoom, int ppc, Color color)
    {
        var screenPoints = poly.ExteriorRing
            .Select(p => new Point(WorldToScreenX(p.X, v, zoom, ppc),
                                   WorldToScreenY(p.Y, v, zoom, ppc)))
            .ToArray();
        commands.DrawPolyline(screenPoints, color, 2);
    }

    private void RenderRiver(
        IRenderCommandList commands,
        IMapFeature feature,
        Contracts.Geometry.LineString line,
        PigeonPea.Rendering.Contracts.Viewport v,
        double zoom,
        int ppc)
    {
        int cellCount = 0;
        if (feature.Metadata != null && feature.Metadata.TryGetValue("cellCount", out var val))
        {
            int.TryParse(val?.ToString(), out cellCount);
        }

        // Calculate width based on flux/cellCount and zoom
        double baseWidth = cellCount switch
        {
            >= 120 => 3.0,
            >= 60 => 2.0,
            >= 20 => 1.5,
            _ => 1.0
        };

        double zoomScale = zoom switch
        {
            <= 0.4 => 1.5,
            <= 0.8 => 1.2,
            <= 1.6 => 1.0,
            <= 3.0 => 0.8,
            _ => 0.0
        };

        if (zoomScale <= 0) return;

        int strokeWidth = (int)Math.Max(1, baseWidth * zoomScale * ppc);
        var screenPoints = line.Points
            .Select(p => new Point(WorldToScreenX(p.X, v, zoom, ppc), WorldToScreenY(p.Y, v, zoom, ppc)))
            .ToArray();

        commands.DrawPolyline(screenPoints, new Color(30, 144, 200), strokeWidth);
    }

    private void RenderSettlement(
        IRenderCommandList commands,
        IMapFeature feature,
        Contracts.Geometry.Point pt,
        PigeonPea.Rendering.Contracts.Viewport v,
        double zoom,
        int ppc)
    {
        int screenX = WorldToScreenX(pt.X, v, zoom, ppc);
        int screenY = WorldToScreenY(pt.Y, v, zoom, ppc);

        // Simple circle approximation for now using a small polygon or just a sprite if we had them
        // Using a diamond shape for simplicity
        int radius = feature.Kind == FeatureKind.Capital ? Math.Max(4, 3 * ppc) : Math.Max(3, 2 * ppc);

        var points = new Point[]
        {
            new(screenX, screenY - radius),
            new(screenX + radius, screenY),
            new(screenX,screenY + radius),
            new(screenX - radius, screenY)
        };

        Color color = feature.Kind switch
        {
            FeatureKind.Capital => new Color(255, 220, 0),
            FeatureKind.City => new Color(230, 230, 230),
            _ => new Color(180, 180, 180)
        };

        commands.DrawPolygon(points, color);
    }

    private int WorldToScreenX(double worldX, PigeonPea.Rendering.Contracts.Viewport v, double zoom, int ppc = 1)
    {
        return (int)Math.Round(((worldX - v.X) / zoom) * ppc);
    }

    private int WorldToScreenY(double worldY, PigeonPea.Rendering.Contracts.Viewport v, double zoom, int ppc = 1)
    {
        return (int)Math.Round(((worldY - v.Y) / zoom) * ppc);
    }

    private static int CalculateZoomLevel(double zoom)
    {
        return zoom switch
        {
            >= 2.0 => 2,
            >= 1.2 => 3,
            >= 0.6 => 5,
            >= 0.3 => 7,
            _ => 10
        };
    }

    private Color ResolveFillColor(IMapFeature feature, WorldMapRenderSettings settings)
    {
        var scheme = settings.ColorScheme;

        byte? height = null;
        int biomeId = -1;

        if (feature.Metadata != null)
        {
            if (feature.Metadata.TryGetValue("height", out var hVal) && byte.TryParse(hVal?.ToString(), out var hParsed))
            {
                height = hParsed;
            }

            if (feature.Metadata.TryGetValue("biome", out var bVal) && int.TryParse(bVal?.ToString(), out var bParsed))
            {
                biomeId = bParsed;
            }
        }

        // Prefer biome-based color to get variation closer to FMG visuals
        SKColor skColor;
        if (height.HasValue)
        {
            // Use height-based palette (closer to FMG originals) so unknown biome ids
            // do not collapse to flat gray.
            skColor = ColorSchemes.GetHeightColor(height.Value, scheme, isBiome: false, biomeId: biomeId);
        }
        else if (biomeId >= 0)
        {
            skColor = ColorSchemes.GetHeightColor(40, scheme, isBiome: true, biomeId: biomeId);
        }
        else
        {
            skColor = feature.Kind switch
            {
                FeatureKind.Ocean or FeatureKind.Lake => ColorSchemes.GetHeightColor(10, scheme, false, biomeId),
                _ => ColorSchemes.GetHeightColor(40, scheme, false, biomeId)
            };
        }

        return ToColor(skColor);
    }

    private static Color ToColor(SKColor c) => new(c.Red, c.Green, c.Blue, c.Alpha);
}
