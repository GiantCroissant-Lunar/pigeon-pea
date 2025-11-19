using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;
using PigeonPea.Shared.Rendering;
using VerifyXunit;
using Xunit;

namespace PigeonPea.Map.Rendering.Tests;

public class CityOverlayTests
{
    [Fact]
    public Task Capitals_overlay_projection_is_stable()
    {
        // Use the same deterministic archipelago settings as other snapshot tests
        var settings = new MapGenerationSettings
        {
            Width = 800,
            Height = 600,
            Seed = 123456,
            NumPoints = 8000,
            RNGMode = RNGMode.Alea,
            SeedString = "demo-seed",
            ReseedAtPhaseStart = true,
            GridMode = GridMode.Jittered,
            HeightmapMode = HeightmapMode.Template,
            UseAdvancedNoise = false,
            HeightmapTemplate = "archipelago"
        };

        var adapter = new FantasyMapGeneratorAdapter();
        var map = adapter.Generate(settings);

        // Match SkiaMapRasterizer: viewport is in world coordinates, zoom is
        // world units per cell, and ppc is pixels-per-cell.
        double zoom = 1.0;
        int ppc = 8;
        var viewport = new Viewport(0, 0, map.Inner.Width, map.Inner.Height);

        int Px(double wx) => (int)System.Math.Round(((wx - viewport.X) / zoom) * ppc);
        int Py(double wy) => (int)System.Math.Round(((wy - viewport.Y) / zoom) * ppc);

        var capitals = map.Burgs
            .Where(b => b != null && b.IsCapital)
            .OrderBy(b => b.Id)
            .Select(b => new CapitalProjectionSnapshot(
                b.Id,
                b.Name,
                b.Position.X,
                b.Position.Y,
                Px(b.Position.X),
                Py(b.Position.Y)))
            .ToList();

        return Verifier.Verify(capitals);
    }

    private sealed record CapitalProjectionSnapshot(
        int Id,
        string Name,
        double WorldX,
        double WorldY,
        int PixelX,
        int PixelY);
}
