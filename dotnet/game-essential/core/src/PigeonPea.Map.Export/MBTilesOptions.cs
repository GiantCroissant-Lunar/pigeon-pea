using PigeonPea.Map.Contracts.Spatial;

namespace PigeonPea.Map.Export;

/// <summary>
/// Configuration options for MBTiles export
/// </summary>
public record MBTilesOptions
{
    /// <summary>Map name (metadata)</summary>
    public string? Name { get; init; }

    /// <summary>Map description (metadata)</summary>
    public string? Description { get; init; }

    /// <summary>Attribution text (metadata)</summary>
    public string? Attribution { get; init; }

    /// <summary>Geographic bounds to export</summary>
    public BoundingBox Bounds { get; init; } = new(0, 0, 2048, 2048);

    /// <summary>Minimum zoom level</summary>
    public int MinZoom { get; init; } = 0;

    /// <summary>Maximum zoom level</summary>
    public int MaxZoom { get; init; } = 12;

    /// <summary>Default zoom level for display</summary>
    public int DefaultZoom { get; init; } = 4;

    /// <summary>Enable compression (gzip)</summary>
    public bool Compress { get; init; } = true;

    /// <summary>Maximum parallel tile generation</summary>
    public int MaxParallelism { get; init; } = 4;

    /// <summary>Number of tiles to insert in a single batch</summary>
    public int BatchSize { get; init; } = 100;
}

/// <summary>
/// Progress information for MBTiles export
/// </summary>
public record ExportProgress(
    int CurrentZoom,
    int MaxZoom,
    int TilesProcessed,
    int TotalTiles)
{
    public double Percentage => TotalTiles > 0 ? (double)TilesProcessed / TotalTiles * 100 : 0;

    public string Status => $"Zoom {CurrentZoom}/{MaxZoom} - {TilesProcessed}/{TotalTiles} tiles ({Percentage:F1}%)";
}
