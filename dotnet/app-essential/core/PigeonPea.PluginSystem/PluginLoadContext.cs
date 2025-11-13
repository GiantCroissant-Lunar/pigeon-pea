using System.Reflection;
using System.Runtime.Loader;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Custom assembly load context for plugin isolation.
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginAssemblyPath, bool isCollectible = true)
        : base(isCollectible: isCollectible)
    {
        _resolver = new AssemblyDependencyResolver(pluginAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Important: ensure shared contracts resolve from Default ALC
        if (assemblyName.Name == "PigeonPea.Contracts" ||
            assemblyName.Name == "PigeonPea.Game.Contracts")
        {
            return null; // use Default ALC binding for contracts
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path != null)
        {
            return LoadFromAssemblyPath(path);
        }

        // Use default context for shared assemblies
        return null;
    }

    protected override System.IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (path is not null)
        {
            return LoadUnmanagedDllFromPath(path);
        }
        return base.LoadUnmanagedDll(unmanagedDllName);
    }
}
