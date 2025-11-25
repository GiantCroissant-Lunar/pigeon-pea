using System.Collections.Generic;
using PigeonPea.Platform.Contracts.Map.Geometry;
using PigeonPea.Platform.Contracts.Map.Spatial;

namespace PigeonPea.Platform.Contracts.Map.Features;

/// <summary>
/// Any geographic feature on the map.
/// </summary>
public interface IMapFeature
{
    /// <summary>Unique identifier within map</summary>
    string FeatureId { get; }

    /// <summary>Feature category</summary>
    FeatureKind Kind { get; }

    /// <summary>Display name (localized)</summary>
    string? Name { get; }

    /// <summary>Geometry (point, line, polygon)</summary>
    IGeometry Geometry { get; }

    /// <summary>Minimum zoom level to display</summary>
    ZoomLevel MinZoom { get; }

    /// <summary>Arbitrary metadata (population, culture, etc.)</summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
}
