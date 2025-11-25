using System.Reflection;
using System.Runtime.Loader;
using PigeonPea.Platform.Contracts.Core;

namespace PigeonPea.Platform.Core.Plugin;

public class PluginLoader
{
    private readonly Dictionary<string, AssemblyLoadContext> _contexts = new();
    private readonly IPluginContext _pluginContext;

    public PluginLoader(IPluginContext pluginContext)
    {
        _pluginContext = pluginContext;
    }

    public IPlugin LoadPlugin(string assemblyPath)
    {
        var context = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(assemblyPath), isCollectible: true);
        var assembly = context.LoadFromAssemblyPath(assemblyPath);
        
        var pluginType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);
        
        if (pluginType == null)
            throw new InvalidOperationException($"No IPlugin implementation found in {assemblyPath}");
        
        var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
        _contexts[plugin.Name] = context;
        
        plugin.Initialize(_pluginContext);
        return plugin;
    }

    public void UnloadPlugin(string name)
    {
        if (_contexts.TryGetValue(name, out var context))
        {
            context.Unload();
            _contexts.Remove(name);
        }
    }
}
