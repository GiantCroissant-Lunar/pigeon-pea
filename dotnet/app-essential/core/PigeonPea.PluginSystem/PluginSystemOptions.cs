using System.Collections.Generic;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Options for configuring the plugin system.
/// </summary>
public class PluginSystemOptions
{
    public ICollection<string> PluginPaths { get; init; } = new List<string>();
    public required string Profile { get; init; } = "dotnet.console";
    public bool HotReload { get; set; } = false;
}
