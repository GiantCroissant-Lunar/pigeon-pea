using System.Collections.Generic;

namespace PigeonPea.Contracts.Plugin;

/// <summary>
/// Plugin metadata manifest describing capabilities and entry points.
/// </summary>
public class PluginManifest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? Author { get; set; }

    /// <summary>
    /// Multi-profile entry points: e.g., "dotnet.console" → "Assembly.dll,Namespace.Type".
    /// </summary>
    public Dictionary<string, string> EntryPoint { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Legacy single entry point (optional; for backward compat).
    /// </summary>
    public string? EntryAssembly { get; set; }
    public string? EntryType { get; set; }

    public List<PluginDependency> Dependencies { get; set; } = new List<PluginDependency>();
    public List<string> Capabilities { get; set; } = new List<string>();
    public List<string> SupportedProfiles { get; set; } = new List<string>();

    /// <summary>
    /// Higher values indicate earlier selection preference when multiple implementations exist.
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Optional hint for load strategy (eager, lazy, explicit).
    /// </summary>
    public string? LoadStrategy { get; set; }
}

/// <summary>
/// Represents a dependency on another plugin.
/// </summary>
public class PluginDependency
{
    public string Id { get; set; } = string.Empty;
    public string? VersionRange { get; set; }
    public bool Optional { get; set; }
}
