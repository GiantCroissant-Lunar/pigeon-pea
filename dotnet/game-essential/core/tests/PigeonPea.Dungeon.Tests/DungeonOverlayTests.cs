using System.Linq;
using System.Threading.Tasks;
using PigeonPea.Dungeon.Core;
using PigeonPea.Overlays;
using VerifyXunit;
using Xunit;

namespace PigeonPea.Dungeon.Tests;

public class DungeonOverlayTests
{
    [Fact]
    public Task Dungeon_doors_overlay_is_stable()
    {
        var generator = new BasicDungeonGenerator();
        var dungeon = generator.Generate(width: 64, height: 48, seed: 12345);

        var source = new DungeonGridOverlaySource();
        var overlays = source
            .GetOverlays(dungeon)
            .OrderBy(o => o.Position.Y)
            .ThenBy(o => o.Position.X)
            .Select(o => new DungeonDoorSnapshot(
                o.Position.X,
                o.Position.Y,
                o.Kind,
                o.Metadata.TryGetValue("state", out var state) ? state?.ToString() ?? "" : ""))
            .ToList();

        return Verifier.Verify(overlays);
    }

    private sealed record DungeonDoorSnapshot(
        int X,
        int Y,
        string Kind,
        string State);
}
