namespace PigeonPea.Platform.Plugin.Management;

using System.Reflection;
using System.Runtime.Loader;

/// <summary>
/// Isolated AssemblyLoadContext for plugin loading with unload support.
/// </summary>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginPath;

    public PluginLoadContext(string pluginPath, string pluginName, bool isCollectible = true)
        : base(name: $"Plugin-{pluginName}", isCollectible: isCollectible)
    {
        _pluginPath = pluginPath ?? throw new ArgumentNullException(nameof(pluginPath));
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Check if assembly is already loaded in Default ALC (contracts)
        var defaultAssembly = AssemblyLoadContext.Default.Assemblies
            .FirstOrDefault(a => a.GetName().Name == assemblyName.Name);
        
        if (defaultAssembly != null)
        {
            return null; // Use Default ALC version
        }

        // Resolve from plugin directory
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
