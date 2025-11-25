namespace PigeonPea.Map.Contracts.Providers;

/// <summary>
/// Capabilities of a map provider.
/// </summary>
[Flags]
public enum MapProviderCapabilities
{
    None = 0,

    // Data capabilities
    Terrain = 1 << 0,
    Settlements = 1 << 1,
    Rivers = 1 << 2,
    Roads = 1 << 3,
    Borders = 1 << 4,
    PointsOfInterest = 1 << 5,

    // Source capabilities
    Generative = 1 << 10,      // Can generate new regions
    Offline = 1 << 11,         // Works without network
    Streamable = 1 << 12,      // Supports incremental loading
    Cacheable = 1 << 13,       // Results can be cached

    // Common combinations
    FullWorld = Terrain | Settlements | Rivers | Roads | Borders | PointsOfInterest,
    Fantasy = Terrain | Settlements | Rivers | Borders | Generative,
    RealWorld = Terrain | Settlements | Rivers | Roads | Offline | Cacheable
}
