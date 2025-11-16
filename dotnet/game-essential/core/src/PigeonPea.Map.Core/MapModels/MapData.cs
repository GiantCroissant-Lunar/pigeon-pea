using System.Collections.Generic;
using System.Linq;

namespace PigeonPea.Map.Core;

public sealed class MapData
{
    private readonly FantasyMapGenerator.Core.Models.MapData _inner;

    public FantasyMapGenerator.Core.Models.MapData Inner => _inner;

    internal MapData(FantasyMapGenerator.Core.Models.MapData inner)
        => _inner = inner;

    public IReadOnlyList<Cell> Cells => _cells ??= _inner.Cells.Select(c => new Cell(c)).ToList();
    public IReadOnlyList<Biome> Biomes => _biomes ??= _inner.Biomes.Select(b => new Biome(b)).ToList();
    public IReadOnlyList<River> Rivers => _rivers ??= (_inner.Rivers?.Select(r => new River(r)).ToList() ?? new List<River>());

    // Burgs list from FMG may contain null entries; preserve index order but avoid
    // constructing wrappers for nulls so callers can safely check for null.
    public IReadOnlyList<Burg> Burgs => _burgs ??=
        (_inner.Burgs?.Select(b => b == null ? null! : new Burg(b)).ToList() ?? new List<Burg>());

    public IReadOnlyList<State> States => _states ??= (_inner.States?.Select(s => new State(s)).ToList() ?? new List<State>());
    public IReadOnlyList<Culture> Cultures => _cultures ??= (_inner.Cultures?.Select(c => new Culture(c)).ToList() ?? new List<Culture>());
    public IReadOnlyList<Religion> Religions => _religions ??= (_inner.Religions?.Select(r => new Religion(r)).ToList() ?? new List<Religion>());
    public IReadOnlyList<Route> Routes => _routes ??= (_inner.Routes?.Select(r => new Route(r)).ToList() ?? new List<Route>());
    public IReadOnlyList<Marker> Markers => _markers ??= (_inner.Markers?.Select(m => new Marker(m)).ToList() ?? new List<Marker>());
    public IReadOnlyList<Dungeon> Dungeons => _dungeons ??=
        (_inner.Dungeons?.Select(d => new Dungeon(d)).ToList() ?? new List<Dungeon>());

    private List<Cell>? _cells;
    private List<Biome>? _biomes;
    private List<River>? _rivers;
    private List<Burg>? _burgs;
    private List<State>? _states;
    private List<Culture>? _cultures;
    private List<Religion>? _religions;
    private List<Route>? _routes;
    private List<Marker>? _markers;
    private List<Dungeon>? _dungeons;

    public Cell? GetCellAt(double x, double y)
    {
        var c = _inner.GetCellAt(x, y);
        return c == null ? null : new Cell(c);
    }
}
