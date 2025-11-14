using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PigeonPea.Contracts.Plugin;

namespace PigeonPea.PluginSystem;

/// <summary>
/// Discovers, loads, and manages plugins.
/// </summary>
public class PluginLoader
{
    private readonly ILogger<PluginLoader> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly IRegistry _registry;
    private readonly PluginRegistry _pluginRegistry;
    private readonly IPluginHost _host;

    public PluginLoader(
        ILogger<PluginLoader> logger,
        ILoggerFactory loggerFactory,
        IConfiguration configuration,
        IRegistry registry,
        PluginRegistry pluginRegistry,
        IPluginHost host)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _configuration = configuration;
        _registry = registry;
        _pluginRegistry = pluginRegistry;
        _host = host;
    }

    /// <summary>
    /// Discover plugins from the given directories and load them for the specified profile.
    /// </summary>
    public async Task<int> DiscoverAndLoadAsync(IEnumerable<string> pluginPaths, string profile, CancellationToken ct = default)
    {
        var discovered = new List<(PluginManifest manifest, string dir)>();

        foreach (var root in pluginPaths.Distinct())
        {
            if (ct.IsCancellationRequested) break;
            if (string.IsNullOrWhiteSpace(root)) continue;

            if (!Directory.Exists(root))
            {
                _logger.LogDebug("Plugin path does not exist: {Path}", root);
                continue;
            }

            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                var manifestPath = Path.Combine(dir, "plugin.json");
                if (!File.Exists(manifestPath)) continue;

                try
                {
                    var manifest = ManifestParser.Parse(manifestPath);
                    // Filter: require entry point for this profile
                    if (manifest.EntryPoint?.ContainsKey(profile) != true)
                    {
                        _logger.LogDebug("Plugin {Id} does not support profile {Profile}", manifest.Id, profile);
                        continue;
                    }
                    discovered.Add((manifest, dir));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse manifest at {Path}", manifestPath);
                }
            }
        }

        if (discovered.Count == 0)
        {
            _logger.LogInformation("No plugins discovered.");
            return 0;
        }

        // Resolve dependency order
        var ordered = DependencyResolver.ResolveLoadOrder(discovered.Select(d => d.manifest));
        var byId = discovered.ToDictionary(x => x.manifest.Id, x => x.dir, StringComparer.OrdinalIgnoreCase);

        var loaded = 0;
        foreach (var manifest in ordered)
        {
            if (ct.IsCancellationRequested) break;

            PluginLoadContext? alc = null;
            IPlugin? instance = null;
            var added = false;

            try
            {
                if (!manifest.EntryPoint.TryGetValue(profile, out var entry))
                {
                    _logger.LogWarning("Manifest for {Id} missing entry point for profile {Profile}", manifest.Id, profile);
                    continue;
                }

                var parts = entry.Split(',', 2);
                if (parts.Length != 2)
                {
                    _logger.LogWarning("Invalid entry point format for {Id}: {Entry}", manifest.Id, entry);
                    continue;
                }

                var assemblyName = parts[0].Trim();
                var typeName = parts[1].Trim();
                var dir = byId[manifest.Id];
                var assemblyPath = Path.Combine(dir, assemblyName);

                if (!File.Exists(assemblyPath))
                {
                    _logger.LogWarning("Assembly not found for plugin {Id}: {Assembly}", manifest.Id, assemblyPath);
                    continue;
                }

                alc = new PluginLoadContext(assemblyPath, isCollectible: true);
                var asm = alc.LoadFromAssemblyPath(assemblyPath);
                var pluginType = asm.GetType(typeName, throwOnError: true)!;

                // Validate implements IPlugin by FullName (cross-ALC safe check)
                var implementsIPlugin = pluginType.GetInterfaces().Any(i => i.FullName == typeof(IPlugin).FullName);
                if (!implementsIPlugin)
                {
                    _logger.LogWarning("Type {Type} in plugin {Id} does not implement IPlugin", typeName, manifest.Id);
                    continue;
                }

                instance = Activator.CreateInstance(pluginType) as IPlugin;
                if (instance is null)
                {
                    _logger.LogWarning("Failed to instantiate plugin type {Type} for {Id}", typeName, manifest.Id);
                    continue;
                }

                var pluginLogger = _loggerFactory.CreateLogger($"Plugin:{manifest.Id}");
                var context = new PluginContext(_registry, _configuration, pluginLogger, _host);

                await instance.InitializeAsync(context, ct).ConfigureAwait(false);
                await instance.StartAsync(ct).ConfigureAwait(false);

                var record = new PluginRegistry.PluginRecord(
                    manifest.Id,
                    dir,
                    manifest,
                    alc,
                    instance
                );

                if (!_pluginRegistry.TryAdd(record))
                {
                    // Duplicate ID; clean up this instance and ALC
                    _logger.LogWarning("Plugin with id {Id} already loaded; unloading duplicate instance.", manifest.Id);
                    try { await instance.StopAsync(ct).ConfigureAwait(false); } catch { }
                    alc.Unload();
                    continue;
                }

                added = true;
                loaded++;
                _logger.LogInformation("Loaded plugin {Id} ({Name} {Version})", manifest.Id, manifest.Name, manifest.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin {Id}", manifest.Id);
            }
            finally
            {
                if (!added)
                {
                    // Ensure cleanup of partially loaded plugin/context
                    if (instance is not null)
                    {
                        try { instance.StopAsync(ct).GetAwaiter().GetResult(); } catch { }
                    }
                    if (alc is not null)
                    {
                        try { alc.Unload(); } catch { }
                    }
                }
            }
        }

        return loaded;
    }

    public Task<bool> UnloadPluginAsync(string pluginId, CancellationToken ct = default)
    {
        if (_pluginRegistry.TryGet(pluginId, out var record))
        {
            try
            {
                // Best-effort stop
                try { record.Instance.StopAsync(ct).GetAwaiter().GetResult(); } catch { /* ignore */ }
                _pluginRegistry.Remove(pluginId);
                record.LoadContext.Unload();
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
        return Task.FromResult(false);
    }

    public async Task<bool> ReloadPluginAsync(string pluginId, string profile, CancellationToken ct = default)
    {
        var ok = await UnloadPluginAsync(pluginId, ct).ConfigureAwait(false);
        if (!ok) return false;

        // Re-discover only the directory of that plugin
        // MVP: perform a full discover/load to simplify
        var paths = GetConfiguredPluginPaths();
        var count = await DiscoverAndLoadAsync(paths, profile, ct).ConfigureAwait(false);
        return count > 0;
    }

    public IEnumerable<string> GetConfiguredPluginPaths()
    {
        var section = _configuration.GetSection("PluginSystem:PluginPaths");
        if (!section.Exists()) yield break;
        foreach (var child in section.GetChildren())
        {
            var value = child.Value;
            if (!string.IsNullOrWhiteSpace(value)) yield return ExpandPath(value);
        }
    }

    private static string ExpandPath(string path)
    {
        if (path.StartsWith("~"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.GetFullPath(Path.Combine(home, path.TrimStart('~', '/', '\\')));
        }
        if (!Path.IsPathRooted(path))
        {
            // Anchor relative paths to the app content root to avoid dependence on Environment.CurrentDirectory
            return Path.GetFullPath(path, AppContext.BaseDirectory);
        }
        return path;
    }
}
