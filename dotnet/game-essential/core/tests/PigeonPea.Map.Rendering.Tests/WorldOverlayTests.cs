using System.Linq;
using System.Threading.Tasks;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;
using PigeonPea.Overlays;
using PigeonPea.Shared.Rendering;
using VerifyXunit;
using Xunit;

namespace PigeonPea.Map.Rendering.Tests;

public class WorldOverlayTests
{
    [Fact]
    public Task FmgWorld_overlays_are_stable()
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

        double zoom = 1.0;
        int ppc = 8;
        var viewport = new Viewport(0, 0, map.Inner.Width, map.Inner.Height);

        int Px(double wx) => (int)System.Math.Round(((wx - viewport.X) / zoom) * ppc);
        int Py(double wy) => (int)System.Math.Round(((wy - viewport.Y) / zoom) * ppc);

        var source = new FmgWorldOverlaySource();
        var snapshots = source
            .GetOverlays(map)
            .OrderBy(o => o.LayerId)
            .ThenBy(o => o.Name)
            .Select(o => new WorldOverlayProjectionSnapshot(
                o.LayerId,
                o.Kind,
                o.Name,
                o.Position.X,
                o.Position.Y,
                Px(o.Position.X),
                Py(o.Position.Y)))
            .ToList();

        return Verifier.Verify(snapshots);
    }

    private sealed record WorldOverlayProjectionSnapshot(
        string LayerId,
        string Kind,
        string Name,
        double WorldX,
        double WorldY,
        int PixelX,
        int PixelY);
}
