using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PigeonPea.Map.Core;
using PigeonPea.Map.Core.Adapters;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace PigeonPea.Map.Rendering.Tests;

public class MapSnapshotTests
{
    [Fact]
    public Task FantasyMapGenerator_snapshot_is_stable_for_archipelago_demo_settings()
    {
        // Arrange: use the same settings as the HUD / ConsoleMapDemoRunner archipelago demo
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

        // Act: generate the map once and convert it into a deterministic snapshot
        var map = adapter.Generate(settings);
        var snapshot = MapSnapshot.FromMap(map);

        // Assert via Verify snapshot testing (writes/compares *.verified.json next to this file)
        return Verifier.Verify(snapshot);
    }

    private sealed record MapSnapshot(
        int Width,
        int Height,
        IReadOnlyList<CellSnapshot> Cells,
        IReadOnlyList<BiomeSnapshot> Biomes,
        IReadOnlyList<RiverSnapshot> Rivers)
    {
        public static MapSnapshot FromMap(MapData map)
        {
            int width = map.Inner.Width;
            int height = map.Inner.Height;

            // The raw FMG map can contain thousands of cells, and snapshotting
            // all of them makes the .verified file very large (~MB). For regression
            // purposes we only need a representative, deterministic subset of
            // cells that samples the full range of IDs.

            var orderedCells = map.Cells
                .OrderBy(c => c.Id)
                .ToArray();

            const int targetSampleCount = 128;
            var sampledCells = SampleCells(orderedCells, targetSampleCount);

            var cells = sampledCells
                .Select(c => new CellSnapshot(
                    c.Id,
                    c.Height,
                    c.Biome,
                    c.Center.X,
                    c.Center.Y))
                .ToArray();

            var biomes = map.Biomes
                .Select((b, index) => new BiomeSnapshot(index, b.Id, b.Name ?? string.Empty, b.Color ?? string.Empty))
                .ToArray();

            var rivers = new List<RiverSnapshot>();
            if (map.Rivers != null)
            {
                int rIndex = 0;
                foreach (var r in map.Rivers)
                {
                    if (r?.Cells == null || r.Cells.Count == 0) continue;
                    var cellsList = r.Cells.ToArray();
                    rivers.Add(new RiverSnapshot(rIndex++, cellsList));
                }
            }
            return new MapSnapshot(width, height, cells, biomes, rivers);
        }

        /// <summary>
        /// Deterministically samples up to <paramref name="targetCount"/> cells
        /// from the ordered array, preserving coverage across the full ID range.
        /// </summary>
        private static IEnumerable<Cell> SampleCells(Cell[] orderedCells, int targetCount)
        {
            if (orderedCells.Length <= targetCount)
                return orderedCells;

            var result = new List<Cell>(targetCount);

            // Evenly spaced sampling across the array indices
            double step = (orderedCells.Length - 1) / (double)(targetCount - 1);
            for (int i = 0; i < targetCount; i++)
            {
                int idx = (int)Math.Round(i * step);
                if (idx < 0)
                    idx = 0;
                else if (idx >= orderedCells.Length)
                    idx = orderedCells.Length - 1;

                result.Add(orderedCells[idx]);
            }

            return result;
        }
    }

    private sealed record CellSnapshot(
        int Id,
        double Height,
        int Biome,
        double CenterX,
        double CenterY);

    private sealed record BiomeSnapshot(
        int Index,
        int Id,
        string Name,
        string ColorHex);

    private sealed record RiverSnapshot(
        int Index,
        IReadOnlyList<int> Cells);
}
