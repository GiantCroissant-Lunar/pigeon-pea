namespace PigeonPea.Platform.Plugin.Management;

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PigeonPea.Platform.Contracts.Core;
using PigeonPea.Platform.Plugin.Core;

/// <summary>
/// Plugin loader with AssemblyLoadContext isolation and lifecycle orchestration.
/// </summary>
public sealed class PluginLoader : IDisposable
{
    private readonly ILogger<PluginLoader> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly ServiceRegistry _serviceRegistry;
    private readonly IPluginHost _pluginHost;
    private readonly Dictionary<string, LoadedPlugin> _loadedPlugins = new();
    private readonly string _profile;
    private bool _disposed;

    public PluginLoader(
        ILogger<PluginLoader> logger,
        ILoggerFactory loggerFactory,
        IConfiguration configuration,
        ServiceRegistry serviceRegistry,
        IPluginHost pluginHost,
        string profile = "dotnet.console")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _serviceRegistry = serviceRegistry ?? throw new ArgumentNullException(nameof(serviceRegistry));
        _pluginHost = pluginHost ?? throw new ArgumentNullException(nameof(pluginHost));
        _profile = profile;
    }

    /// <summary>
    /// Discover and load plugins from specified directories.
    /// </summary>
    public async Task<int> DiscoverAndLoadAsync(IEnumerable<string> pluginPaths, CancellationToken ct = default)
    {
        var manifests = new List<(string path, PluginManifest manifest)>();

        // Phase 1: Discover all plugins
        foreach (var pluginPath in pluginPaths)
        {
            var absolutePath = Path.GetFullPath(pluginPath);

            if (!Directory.Exists(absolutePath))
            {
                _logger.LogWarning("Plugin path does not exist: {PluginPath}", absolutePath);
                continue;
            }

            var manifestFiles = Directory.GetFiles(absolutePath, "plugin.json", SearchOption.AllDirectories);
            
            foreach (var manifestFile in manifestFiles)
            {
                try
                {
                    var manifest = ManifestParser.ParseFile(manifestFile);
                    manifests.Add((Path.GetDirectoryName(manifestFile)!, manifest));
                    _logger.LogInformation("Discovered plugin: {PluginId} v{Version}", 
                        manifest.Id, manifest.Version);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse manifest: {ManifestFile}", manifestFile);
                }
            }
        }

        // Phase 2: Load plugins
        int loadedCount = 0;
        foreach (var (path, manifest) in manifests)
        {
            try
            {
                await LoadPluginAsync(path, manifest, ct);
                loadedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin: {PluginId}", manifest.Id);
            }
        }

        _logger.LogInformation("Loaded {Count}/{Total} plugins", loadedCount, manifests.Count);
        return loadedCount;
    }

    /// <summary>
    /// Load a single plugin from manifest.
    /// </summary>
    private async Task LoadPluginAsync(string pluginPath, PluginManifest manifest, CancellationToken ct)
    {
        if (_loadedPlugins.ContainsKey(manifest.Id))
        {
            _logger.LogWarning("Plugin already loaded: {PluginId}", manifest.Id);
            return;
        }

        _logger.LogInformation("Loading plugin: {PluginId} v{Version}", manifest.Id, manifest.Version);

        // Determine entry point
        string? entryAssembly = null;
        string? entryType = null;

        if (manifest.EntryPoint.TryGetValue(_profile, out var profileEntry))
        {
            // Modern multi-profile entry
            var parts = profileEntry.Split("::");
            entryAssembly = parts[0];
            entryType = parts.Length > 1 ? parts[1] : null;
        }
        else
        {
            // Legacy single entry
            entryAssembly = manifest.EntryAssembly;
            entryType = manifest.EntryType;
        }

        if (string.IsNullOrWhiteSpace(entryAssembly))
        {
            throw new InvalidOperationException($"No entry assembly found for profile '{_profile}' in plugin {manifest.Id}");
        }

        // Create ALC and load plugin assembly
        var alc = new PluginLoadContext(Path.Combine(pluginPath, entryAssembly), manifest.Id);
        var assembly = alc.LoadFromAssemblyPath(Path.Combine(pluginPath, entryAssembly));

        // Find and instantiate plugin
        Type? pluginType = null;
        
        if (!string.IsNullOrWhiteSpace(entryType))
        {
            pluginType = assembly.GetType(entryType);
        }
        else
        {
            // Auto-discover IPlugin implementation
            pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
        }

        if (pluginType == null)
        {
            throw new InvalidOperationException($"No IPlugin implementation found in {manifest.Id}");
        }

        var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;

        // Create plugin context
        var pluginLogger = _loggerFactory.CreateLogger($"Plugin.{manifest.Id}");
        var context = new PluginContext(_serviceRegistry, _configuration, pluginLogger, _pluginHost);

        // Initialize and start plugin
        await plugin.InitializeAsync(context, ct);
        await plugin.StartAsync(ct);

        // Track loaded plugin
        _loadedPlugins[manifest.Id] = new LoadedPlugin(manifest, plugin, alc);

        _logger.LogInformation("Plugin loaded successfully: {PluginId}", manifest.Id);
    }

    /// <summary>
    /// Unload all plugins gracefully.
    /// </summary>
    public async Task UnloadAllAsync()
    {
        foreach (var (pluginId, loaded) in _loadedPlugins.ToList())
        {
            try
            {
                await loaded.Plugin.StopAsync();
                loaded.LoadContext.Unload();
                _loadedPlugins.Remove(pluginId);
                _logger.LogInformation("Unloaded plugin: {PluginId}", pluginId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading plugin: {PluginId}", pluginId);
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    public void Dispose()
    {
        if (_disposed) return;

        UnloadAllAsync().GetAwaiter().GetResult();
        _disposed = true;
    }

    private sealed record LoadedPlugin(
        PluginManifest Manifest,
        IPlugin Plugin,
        PluginLoadContext LoadContext);
}
