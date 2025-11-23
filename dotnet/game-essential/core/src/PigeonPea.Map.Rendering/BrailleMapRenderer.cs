using PigeonPea.Map.Contracts;
using PigeonPea.Map.Core;
using PigeonPea.Shared.Rendering;
using PigeonPea.Shared.Rendering.Text;

namespace PigeonPea.Map.Rendering;

public static class BrailleMapRenderer
{
    public static char[,] RenderToBraille(MapData map, Viewport viewport, double zoom, int ppc, bool biomeColors = true, bool rivers = true)
    {
        var raster = SkiaMapRasterizer.Render(map, viewport, zoom, ppc, biomeColors, rivers, colorScheme: ColorScheme.Original);
        return BrailleConverter.Convert(raster.Rgba, raster.WidthPx, raster.HeightPx);
    }

    public static char[,] RenderToBraille(IMapData map, Viewport viewport, double zoom, int ppc, ColorScheme colorScheme = ColorScheme.Original, bool showSettlements = true, bool showRivers = true)
    {
        var raster = SkiaMapRasterizer.Render(map, viewport, zoom, ppc, colorScheme, showSettlements, showRivers);
        return BrailleConverter.Convert(raster.Rgba, raster.WidthPx, raster.HeightPx);
    }
}
