using System.Linq;
using System.Threading.Tasks;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;
using PigeonPea.Shared.Rendering;
using VerifyXunit;
using Xunit;

namespace PigeonPea.Map.Rendering.Tests;

public class DungeonOverlayTests
{
    [Fact]
    public Task Dungeons_overlay_projection_is_stable()
    {
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

        double zoom = 0.5;
        int ppc = 8;
        var viewport = new Viewport(0, 0, map.Inner.Width, map.Inner.Height);

        int Px(double wx) => (int)System.Math.Round(((wx - viewport.X) / zoom) * ppc);
        int Py(double wy) => (int)System.Math.Round(((wy - viewport.Y) / zoom) * ppc);

        var dungeons = map.Dungeons
            .OrderBy(d => d.Id)
            .Select(d => new DungeonProjectionSnapshot(
                d.Id,
                d.Name,
                d.Origin.X,
                d.Origin.Y,
                Px(d.Origin.X),
                Py(d.Origin.Y)))
            .ToList();

        return Verifier.Verify(dungeons);
    }

    private sealed record DungeonProjectionSnapshot(
        int Id,
        string Name,
        double WorldX,
        double WorldY,
        int PixelX,
        int PixelY);
}
