namespace PigeonPea.Contracts.Resource;

/// <summary>
/// Progress information for resource loading.
/// </summary>
/// <param name="ResourceId">Identifier of the resource being loaded.</param>
/// <param name="BytesLoaded">Number of bytes loaded so far.</param>
/// <param name="TotalBytes">Total bytes to load (null if unknown).</param>
/// <param name="PercentComplete">Percentage complete (0-100).</param>
public record LoadProgress(
    string ResourceId,
    long BytesLoaded,
    long? TotalBytes,
    float PercentComplete
);

/// <summary>
/// Resource metadata.
/// </summary>
/// <param name="ResourceId">Unique identifier.</param>
/// <param name="ResourceType">Type of resource (e.g., "texture", "sound", "data").</param>
/// <param name="SizeBytes">Size in bytes.</param>
/// <param name="Path">File path or URI.</param>
public record ResourceMetadata(
    string ResourceId,
    string ResourceType,
    long SizeBytes,
    string Path
);

/// <summary>
/// Resource load result.
/// </summary>
/// <typeparam name="T">Type of loaded resource.</typeparam>
/// <param name="Resource">The loaded resource.</param>
/// <param name="Metadata">Resource metadata.</param>
/// <param name="LoadTimeMs">Time taken to load in milliseconds.</param>
public record ResourceLoadResult<T>(
    T Resource,
    ResourceMetadata Metadata,
    long LoadTimeMs
) where T : class;
