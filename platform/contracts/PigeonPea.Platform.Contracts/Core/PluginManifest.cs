namespace PigeonPea.Platform.Contracts.Core;

/// <summary>
/// Plugin manifest schema (parsed from plugin.json).
/// Supports multi-profile entry points, capabilities, and load strategies.
/// </summary>
public sealed class PluginManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }

    public string? Description { get; init; }
    public string? Author { get; init; }
    public string? License { get; init; }

    /// <summary>
    /// Multi-profile entry points: "dotnet.console", "dotnet.sadconsole", "unity", etc.
    /// Single-profile compatibility: use EntryAssembly/EntryType if this is empty.
    /// </summary>
    public Dictionary<string, string> EntryPoint { get; init; } = new();

    /// <summary>
    /// Legacy single entry point (for backward compatibility). Prefer EntryPoint dictionary.
    /// </summary>
    public string? EntryAssembly { get; init; }

    /// <summary>
    /// Legacy single entry type. Prefer EntryPoint dictionary.
    /// </summary>
    public string? EntryType { get; init; }

    /// <summary>
    /// Plugin dependencies (hard and soft).
    /// </summary>
    public List<PluginDependency> Dependencies { get; init; } = new();

    /// <summary>
    /// Feature capabilities exposed by this plugin (e.g., "inventory", "ecs", "audio").
    /// Used for discovery and filtering.
    /// </summary>
    public List<string> Capabilities { get; init; } = new();

    /// <summary>
    /// Supported profiles (e.g., "Console", "Unity", "SadConsole"). Empty = all profiles.
    /// </summary>
    public List<string> SupportedProfiles { get; init; } = new();

    /// <summary>
    /// Plugin priority for load order and service registration. Higher = preferred. Default: 100.
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Load strategy: "eager" (load at startup), "lazy" (load on demand), "explicit" (manual load).
    /// </summary>
    public string? LoadStrategy { get; init; }

    /// <summary>
    /// Target processes for filtering (e.g., "Console", "Unity"). Empty = all processes.
    /// </summary>
    public List<string> TargetProcesses { get; init; } = new();

    /// <summary>
    /// Target platforms (e.g., "Windows", "Linux", "OSX"). Empty = all platforms.
    /// </summary>
    public List<string> TargetPlatforms { get; init; } = new();
}

/// <summary>
/// Plugin dependency declaration (hard vs soft).
/// </summary>
public sealed class PluginDependency
{
    public required string Id { get; init; }

    /// <summary>
    /// Semantic version range (e.g., ">=1.0.0 &lt;2.0.0", "[1.0.0,2.0.0)").
    /// </summary>
    public string? VersionRange { get; init; }

    /// <summary>
    /// If true, plugin loads with reduced features if dependency is missing (soft dep).
    /// If false, plugin fails to load if dependency is missing (hard dep).
    /// </summary>
    public bool Optional { get; init; }
}
