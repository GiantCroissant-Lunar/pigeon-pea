using System.Collections.Generic;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Options for configuring the plugin system.
/// </summary>
public class PluginSystemOptions
{
    public List<string> PluginPaths { get; set; } = new();
    public string Profile { get; set; } = "dotnet.console";
    public bool HotReload { get; set; } = false;
}
