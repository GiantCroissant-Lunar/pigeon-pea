namespace PigeonPea.Platform.Contracts.Map.Features;

/// <summary>
/// Type of geographic feature.
/// </summary>
public enum FeatureKind
{
    // Settlements
    Capital,
    City,
    Town,
    Village,
    Hamlet,

    // Water
    Ocean,
    Sea,
    Lake,
    River,
    Stream,

    //Terrain
    Land,      // General land/terrain
    Mountain,
    Hill,
    Forest,
    Desert,
    Swamp,

    // Infrastructure
    Road,
    Path,
    Bridge,
    Port,

    // Boundaries
    CountryBorder,
    StateBorder,
    RegionBorder,

    // Points of Interest
    Dungeon,
    Landmark,
    Marker,

    // Generic
    Area,
    Line,
    Point
}
