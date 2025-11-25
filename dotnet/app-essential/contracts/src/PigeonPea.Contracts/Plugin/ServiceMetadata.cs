namespace PigeonPea.Contracts.Plugin;

/// <summary>
/// Metadata describing a registered service implementation.
/// </summary>
public class ServiceMetadata
{
    /// <summary>
    /// Higher priority is preferred when selecting a single implementation.
    /// Framework services typically use 1000+; plugins 100-500.
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Optional friendly name for the implementation.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional version of the implementation.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Optional plugin identifier if the service is provided by a plugin.
    /// </summary>
    public string? PluginId { get; set; }
}
