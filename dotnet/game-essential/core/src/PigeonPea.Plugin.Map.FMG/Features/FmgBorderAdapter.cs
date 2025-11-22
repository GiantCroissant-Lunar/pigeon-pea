using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Geometry;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core;
using ContractsGeometry = PigeonPea.Map.Contracts.Geometry;

namespace PigeonPea.Plugin.Map.FMG.Features;

internal class FmgBorderAdapter : IMapFeature
{
    private readonly State _state;
    private readonly MapData _mapData;

    public FmgBorderAdapter(State state, MapData mapData)
    {
        _state = state;
        _mapData = mapData;
    }

    public string FeatureId => $"border-{_state.Id}";
    public FeatureKind Kind => FeatureKind.StateBorder;
    public string? Name => _state.Name;

    public IGeometry Geometry => CreatePoint();

    public ZoomLevel MinZoom => 4;

    public IReadOnlyDictionary<string, object> Metadata => new Dictionary<string, object>
    {
        ["color"] = _state.Color,
        ["fullName"] = _state.FullName,
        ["cultureId"] = _state.CultureId,
        ["cellCount"] = _state.CellCount,
        ["population"] = _state.RuralPopulation + _state.UrbanPopulation
    };

    private IGeometry CreatePoint()
    {
        if (_state.CenterCellId >= 0 && _state.CenterCellId < _mapData.Cells.Count)
        {
            var cell = _mapData.Cells[_state.CenterCellId];
            return new ContractsGeometry.Point(cell.Center.X, cell.Center.Y);
        }

        return new ContractsGeometry.Point(0, 0);
    }
}
