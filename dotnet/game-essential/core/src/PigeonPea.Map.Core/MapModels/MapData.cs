using System.Collections.Generic;
using System.Linq;

namespace PigeonPea.Map.Core;

public sealed class MapData
{
    private readonly FantasyMapGenerator.Core.Models.MapData _inner;

    internal FantasyMapGenerator.Core.Models.MapData Inner => _inner;

    internal MapData(FantasyMapGenerator.Core.Models.MapData inner)
        => _inner = inner;

    public IReadOnlyList<Cell> Cells => _cells ??= _inner.Cells.Select(c => new Cell(c)).ToList();
    public IReadOnlyList<Biome> Biomes => _biomes ??= _inner.Biomes.Select(b => new Biome(b)).ToList();
    public IReadOnlyList<River> Rivers => _rivers ??= (_inner.Rivers?.Select(r => new River(r)).ToList() ?? new List<River>());

    private List<Cell>? _cells;
    private List<Biome>? _biomes;
    private List<River>? _rivers;

    public Cell? GetCellAt(double x, double y)
    {
        var c = _inner.GetCellAt(x, y);
        return c == null ? null : new Cell(c);
    }
}
