using System.Reflection;
using System.Runtime.Loader;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Custom assembly load context for plugin isolation.
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly HashSet<string> _sharedAssemblies;

    public PluginLoadContext(string pluginAssemblyPath, IEnumerable<string> sharedAssemblies, bool isCollectible = true)
        : base(isCollectible: isCollectible)
    {
        _resolver = new AssemblyDependencyResolver(pluginAssemblyPath);
        _sharedAssemblies = new HashSet<string>(sharedAssemblies, StringComparer.OrdinalIgnoreCase);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Important: ensure shared contracts and core game libraries resolve from Default ALC
        // so that types like Arch.Core.Entity and inventory components are identical
        // between host and plugins.
        if (assemblyName.Name != null && _sharedAssemblies.Contains(assemblyName.Name))
        {
            return null; // use Default ALC binding for shared assemblies
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
