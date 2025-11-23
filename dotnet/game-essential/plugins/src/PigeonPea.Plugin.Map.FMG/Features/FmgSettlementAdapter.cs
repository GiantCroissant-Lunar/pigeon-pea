using PigeonPea.Map.Contracts.Features;
using PigeonPea.Map.Contracts.Geometry;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core;
using ContractsGeometry = PigeonPea.Map.Contracts.Geometry;

namespace PigeonPea.Plugin.Map.FMG.Features;

internal class FmgSettlementAdapter : IMapFeature
{
    private readonly Burg _burg;

    public FmgSettlementAdapter(Burg burg) => _burg = burg;

    public string FeatureId => $"burg-{_burg.Id}";

    public FeatureKind Kind => _burg.IsCapital
        ? FeatureKind.Capital
        : _burg.Population > 10000 ? FeatureKind.City :
          _burg.Population > 1000 ? FeatureKind.Town : FeatureKind.Village;

    public string? Name => _burg.Name;

    public IGeometry Geometry => new ContractsGeometry.Point(_burg.Position.X, _burg.Position.Y);

    public ZoomLevel MinZoom => _burg.IsCapital ? 2 :
                                 _burg.Population > 10000 ? 6 : 10;

    public IReadOnlyDictionary<string, object> Metadata => new Dictionary<string, object>
    {
        ["population"] = _burg.Population,
        ["isCapital"] = _burg.IsCapital,
        ["isPort"] = _burg.IsPort,
        ["stateId"] = _burg.StateId,
        ["cultureId"] = _burg.CultureId
    };
}
