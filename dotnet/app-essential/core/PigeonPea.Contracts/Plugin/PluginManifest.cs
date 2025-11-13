using System.Collections.Generic;

namespace PigeonPea.Contracts.Plugin;

/// <summary>
/// Plugin metadata manifest describing capabilities and entry points.
/// </summary>
public class PluginManifest
{
    /// <summary>
    /// Unique plugin identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-friendly plugin name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Semantic version string (e.g., 1.0.0).
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the plugin.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional author information.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Multi-profile entry points: e.g., "dotnet.console" → "Assembly.dll,Namespace.Type".
    /// </summary>
    public Dictionary<string, string> EntryPoint { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Legacy single entry point (optional; for backward compat).
    /// </summary>
    /// <summary>
    /// Optional legacy entry assembly name.
    /// </summary>
    public string? EntryAssembly { get; set; }

    /// <summary>
    /// Optional legacy entry type name (fully-qualified).
    /// </summary>
    public string? EntryType { get; set; }

    /// <summary>
    /// List of dependent plugins required (or optional) for this plugin to operate.
    /// </summary>
    public List<PluginDependency> Dependencies { get; set; } = new List<PluginDependency>();

    /// <summary>
    /// Capabilities provided by this plugin (e.g., "renderer", "renderer:console").
    /// </summary>
    public List<string> Capabilities { get; set; } = new List<string>();

    /// <summary>
    /// Profiles this plugin supports (e.g., "dotnet.console").
    /// </summary>
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
    /// <summary>
    /// Identifier of the dependency plugin.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Optional semantic version range (e.g., <c>&gt;=1.0.0 &lt; 2.0.0</c>).
    /// </summary>
    public string? VersionRange { get; set; }

    /// <summary>
    /// If true, the dependency is optional.
    /// </summary>
    public bool Optional { get; set; }
}
